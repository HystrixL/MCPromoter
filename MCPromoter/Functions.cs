using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MCPromoter
{
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
            MEMORY_INFO mi = new MEMORY_INFO();
            mi.dwLength = (uint)Marshal.SizeOf(mi);
            GlobalMemoryStatusEx(ref mi);
            return mi;
        }

        ulong GetAvailPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return mi.ullAvailPhys;
        }

        ulong GetUsedPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return (mi.ullTotalPhys - mi.ullAvailPhys);
        }

        ulong GetTotalPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
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
            StringBuilder temp = new StringBuilder(32767);
            GetPrivateProfileString(section, key, "", temp, 32767, _path);
            return temp.ToString();
        }
    }

}
