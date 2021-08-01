using System;
using System.IO;
using System.Text.RegularExpressions;

namespace MCPromoter
{
    partial class MCPromoter
    {
        public static void StandardizedFeedback(string targetName, string content)
                {
                    _mapi.runcmd($"tellraw {targetName} {{\"rawtext\":[{{\"text\":\"{content}\"}}]}}");
                    Regex regex = new Regex("§[\\w]");
                    string rawContent = regex.Replace(content, "");
                    if (config.Logging.Plugin) LogsWriter("MCP", rawContent);
                    if (config.ConsoleOutput.Plugin) ConsoleOutputter("MCP", rawContent);
                }
        
                public static void LogsWriter(string initiators, string content)
                {
                    StreamWriter logsStreamWriter = File.AppendText(PluginPath.LogsPath);
                    logsStreamWriter.WriteLine($@"[{DateTime.Now.ToString()}]<{initiators}>{content}");
                    logsStreamWriter.Close();
                }
        
                public static void ConsoleOutputter(string initiators, string content)
                {
                    Console.WriteLine($@"[{DateTime.Now.ToString()}]<{initiators}>{content}");
                }

    }
}