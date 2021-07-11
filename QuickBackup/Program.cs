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

    class Program
    {
        static void Main(string[] args)
        {
            string mod = args[0];
            string worldName = args[1];

            StreamWriter logWriter = File.AppendText(@"CSR\MCP\QuickBackup\qbLog.txt");
            logWriter.WriteLine($"[{DateTime.Now}]Start {mod} {worldName}");
            Console.WriteLine("Wait for 20s");
            Thread.Sleep(20000);
            switch (mod)
            {
                case "MAKE":
                    if (Directory.Exists($@"worlds\{worldName}"))
                    {
                        FileInfo backupArchive = new FileInfo($@"CSR\MCP\QuickBackup\{worldName}.zip");
                        if (backupArchive.Exists)
                        {
                            Console.WriteLine("已移除旧备份.");
                            backupArchive.Delete();
                        }
                        ZipFile.CreateFromDirectory($@"worlds\{worldName}", $@"CSR\MCP\QuickBackup\{worldName}.zip", CompressionLevel.Optimal, true);
                        Console.WriteLine("备份已完成.");
                        
                        StreamWriter QBTimeWriter = new StreamWriter(@"CSR\MCP\QuickBackup\qbTime.txt");
                        QBTimeWriter.WriteLine(DateTime.Now);
                        QBTimeWriter.Close();
                    }
                    else
                    {
                        Console.WriteLine("无法找到待备份的存档.");
                    }
                    break;
                case "BACK":
                    if (File.Exists($@"CSR\MCP\QuickBackup\{worldName}.zip"))
                    {
                        DirectoryInfo saveDirectory = new DirectoryInfo($@"worlds\{worldName}");
                        if (saveDirectory.Exists)
                        {
                            Console.WriteLine("已移除旧存档.");
                            saveDirectory.Delete(true);
                        }
                        ZipFile.ExtractToDirectory($@"CSR\MCP\QuickBackup\{worldName}.zip", $@"worlds\");
                        Console.WriteLine("回档已完成.");
                    }
                    else
                    {
                        Console.WriteLine("无法找到待回档的备份.");
                    }
                    break;
                case "RESTART":
                    break;
            }
            Console.WriteLine("即将重启服务器.");
            Thread.Sleep(5000);
            Process.Start(@"bedrock_server.exe");

            logWriter.WriteLine($"[{DateTime.Now}]Finish {mod} {worldName}");
            logWriter.Close();
        }
    }
}
