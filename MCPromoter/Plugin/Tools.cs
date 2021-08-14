using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MCPromoter
{
    static class Tools
    {
        public static string FormatSize(long size)
        {
            var d = (double)size;
            var i = 0;
            while ((d > 1024) && (i < 5))
            {
                d /= 1024;
                i++;
            }

            string[] unit = { "B", "KB", "MB", "GB", "TB" };
            return ($"{Math.Round(d, 2)} {unit[i]}");
        }


        public static long GetWorldSize(String path)
        {
            var directoryInfo = new DirectoryInfo(path);
            long length = 0;
            foreach (var fsi in directoryInfo.GetFileSystemInfos())
            {
                if (fsi is FileInfo)
                {
                    length += ((FileInfo)fsi).Length;
                }
                else
                {
                    length += GetWorldSize(fsi.FullName);
                }
            }

            return length;
        }
    }


    class SystemInfo
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalMemoryStatusEx(ref MEMORY_INFO mi);

        PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        [StructLayout(LayoutKind.Sequential)]
        struct MEMORY_INFO
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        MEMORY_INFO GetMemoryStatus()
        {
            var mi = new MEMORY_INFO();
            mi.dwLength = (uint)Marshal.SizeOf(mi);
            GlobalMemoryStatusEx(ref mi);
            return mi;
        }

        ulong GetAvailPhys()
        {
            var mi = GetMemoryStatus();
            return mi.ullAvailPhys;
        }

        ulong GetUsedPhys()
        {
            var mi = GetMemoryStatus();
            return (mi.ullTotalPhys - mi.ullAvailPhys);
        }

        ulong GetTotalPhys()
        {
            var mi = GetMemoryStatus();
            return mi.ullTotalPhys;
        }

        public string GetMemoryUsage()
        {
            return ((float)GetUsedPhys() / GetTotalPhys()).ToString("P2");
        }

        public string GetCpuUsage()
        {
            return cpuCounter.NextValue().ToString("f2") + "%";
        }
    }

    class IniFile
    {
        private readonly string _path;

        public IniFile(string iniPath)
        {
            _path = iniPath;
        }

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal,
            int size, string filePath);

        public void IniWriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, _path);
        }

        public string IniReadValue(string section, string key)
        {
            var temp = new StringBuilder(32767);
            GetPrivateProfileString(section, key, "", temp, 32767, _path);
            return temp.ToString();
        }
    }
}