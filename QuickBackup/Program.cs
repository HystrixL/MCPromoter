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
using MCPromoter;

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
            while (args.Length!=5)
            {
                Console.WriteLine("外部调用请手动指明各参数.格式:模式 存档名 槽位 注释 启动器路径");
                args = Console.ReadLine().Split(' ');
            }
            string mode = args[0];
            string worldName = args[1];
            string slot = args[2];
            string comment = args[3];
            string loaderPath = args[4];
            bool isServerStop = false;
            while (!isServerStop)
            {
                Console.WriteLine("Server is still running...");
                Thread.Sleep(5000);
                if (Process.GetProcessesByName("bedrock_server").ToList().Count <= 0)
                {
                    isServerStop = true;
                }
            }
            
            Console.WriteLine("即将开始......");
            Thread.Sleep(20000);
            StreamWriter logWriter = File.AppendText(QuickBackupPath.QbLogPath);
            logWriter.WriteLine($"[{DateTime.Now}]Start {mode} {slot} {worldName} {comment}");

            switch (mode)
            {
                case "MAKE":
                    if (Directory.Exists($@"worlds\{worldName}"))
                    {
                        FileInfo backupArchive = new FileInfo($@"{QuickBackupPath.QbRootPath}\{slot}.zip");
                        if (backupArchive.Exists)
                        {
                            Console.WriteLine("已移除旧备份.");
                            backupArchive.Delete();
                        }
                        ZipFile.CreateFromDirectory($@"worlds\{worldName}", $@"{QuickBackupPath.QbRootPath}\{slot}.zip", CompressionLevel.Optimal, true);
                        Console.WriteLine("备份已完成.");
                        IniFile iniFile = new IniFile(QuickBackupPath.QbInfoPath);
                        iniFile.IniWriteValue(slot, "WorldName",worldName);
                        iniFile.IniWriteValue(slot, "BackupTime", DateTime.Now.ToString());
                        iniFile.IniWriteValue(slot, "Comment", comment);
                        iniFile.IniWriteValue(slot,"Size",new FileInfo($@"{QuickBackupPath.QbRootPath}\{slot}.zip").Length.ToString());
                    }
                    else
                    {
                        Console.WriteLine("无法找到待备份的存档?!");
                    }
                    break;
                case "BACK":
                    if (File.Exists($@"{QuickBackupPath.QbRootPath}\{slot}.zip"))
                    {
                        DirectoryInfo saveDirectory = new DirectoryInfo($@"worlds\{worldName}");
                        if (saveDirectory.Exists)
                        {
                            Console.WriteLine("已移除旧存档.");
                            saveDirectory.Delete(true);
                        }
                        ZipFile.ExtractToDirectory($@"{QuickBackupPath.QbRootPath}\{slot}.zip", $@"worlds\");
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
            Process.Start(loaderPath);

            logWriter.WriteLine($"[{DateTime.Now}]Finish {mode} {slot} {worldName} {comment}");
            logWriter.Close();
        }
    }
}
