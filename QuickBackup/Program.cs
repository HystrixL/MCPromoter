using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace QuickBackup
{
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



    class Program
    {
        static void Main(string[] args)
        {
            string mod = args[0];
            string worldName = args[1];
            string slot = args[2];
            string comment = args[3];
            StreamWriter logWriter = File.AppendText(@"CSR\MCP\QuickBackup\qbLog.txt");
            logWriter.WriteLine($"[{DateTime.Now}]Start {mod} {slot} {worldName} {comment}");
            Console.WriteLine("Wait for 20s");
            Thread.Sleep(20000);
            switch (mod)
            {
                case "MAKE":
                    if (Directory.Exists($@"worlds\{worldName}"))
                    {
                        FileInfo backupArchive = new FileInfo($@"CSR\MCP\QuickBackup\{slot}.zip");
                        if (backupArchive.Exists)
                        {
                            Console.WriteLine("已移除旧备份.");
                            backupArchive.Delete();
                        }
                        ZipFile.CreateFromDirectory($@"worlds\{worldName}", $@"CSR\MCP\QuickBackup\{slot}.zip", CompressionLevel.Optimal, true);
                        Console.WriteLine("备份已完成.");
                        IniFile iniFile = new IniFile(@"CSR\MCP\QuickBackup\qbInfo.ini");
                        iniFile.IniWriteValue(slot, "WorldName",worldName);
                        iniFile.IniWriteValue(slot, "BackupTime", DateTime.Now.ToString());
                        iniFile.IniWriteValue(slot, "Comment", comment);
                    }
                    else
                    {
                        Console.WriteLine("无法找到待备份的存档?!");
                    }
                    break;
                case "BACK":
                    if (File.Exists($@"CSR\MCP\QuickBackup\{slot}.zip"))
                    {
                        DirectoryInfo saveDirectory = new DirectoryInfo($@"worlds\{worldName}");
                        if (saveDirectory.Exists)
                        {
                            Console.WriteLine("已移除旧存档.");
                            saveDirectory.Delete(true);
                        }
                        ZipFile.ExtractToDirectory($@"CSR\MCP\QuickBackup\{slot}.zip", $@"worlds\");
                        Console.WriteLine("回档已完成.");
                    }
                    else
                    {
                        Console.WriteLine("无法找到待回档的备份?!");
                    }
                    break;
                case "RESTART":
                    break;
            }
            Console.WriteLine("即将重启服务器.");
            Thread.Sleep(5000);
            Process.Start(@"..\MCModDllExe\debug.bat");

            logWriter.WriteLine($"[{DateTime.Now}]Finish {mod} {slot} {worldName} {comment}");
            logWriter.Close();
        }
    }
}
