using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using YamlDotNet.Serialization;
using CSR;
using MCPromoter;
using YamlDotNet.Serialization.NamingConventions;
using System.Web.Script.Serialization;
using WebSocketSharp;

namespace MCPromoter
{
    public class MCPromoter
    {
        private static MCCSAPI _mapi;

        private static Config config;
        private static Dictionary<string, PlayerDatas> playerDatas = new Dictionary<string, PlayerDatas>();
        private static WebSocket webSocket = null;
        private static Timer onlineMinutesAccTimer = null;
        private static Timer forceGamemodeTimer = null;
        private static Timer autoBackupTimer = null;

        private static readonly string[] HelpTexts =
        {
            "§2========================",
            "calc <表达式>    计算表达式/物品数量",
            "back      返回死亡地点",
            "bot [spawn|kill|tp] <BOT名>   召唤/杀死/传送bot",
            "bot list  列出服务器内存在的bot",
            "day [game|server]     查询游戏内/开服天数",
            "entity [count|list]   统计/列出服务器内实体",
            "here      全服报点",
            "item      [clear|count|pick]      清除/统计/拾取服务器内掉落物",
            "ki [true|false|status]   开启/关闭/查询死亡不掉落",
            "kill      自杀(不计入死亡榜)",
            "mg [true|false]   开启/关闭生物破坏",
            "network [ip|port|ping]     查询玩家ip/端口/延迟",
            "om <玩家名> <消息>      向某位离线玩家发送离线消息",
            "qs     发起快速跳过夜晚投票",
            "qs [accept|refuse]     同意/拒绝快速跳过夜晚",
            "qb make <槽位> <注释>      快速备份存档(需指定槽位及注释)(槽位范围1~5)",
            "qb back <槽位>       快速回档服务器(需指定槽位)(槽位范围1~5)",
            "qb restart    快速重启服务器",
            "qb list   查询QuickBackup各槽位信息(存档名、备份时间、注释)",
            "sh <指令>   向控制台注入指令(需特殊授权)",
            "size      获取存档大小",
            "sta <计分板名>    将侧边栏显示调整为特定计分板",
            "sta null      关闭侧边栏显示",
            "sta auto [true|false]      开启/关闭计分板自动切换",
            "system [cpu|memory]    查询服务器CPU/内存占用率",
            "task [add|remove] <任务名>   添加/移除指定任务",
            "tick [倍数|status]      设置/查询随机刻倍数",
            "whitelist [add|remove] <玩家名>     将玩家加入/移出白名单",
            "§2========================"
        };

        private static readonly string[] PluginSettingHelpTexts =
        {
            "§2========================",
            "anticheat [true|false]      开启/关闭反作弊",
            "prefix <newPrefix>     修改插件前缀",
            "staautoswitchesfreq <newFreq>      修改计分板自动切换频率",
            "whitelist [true|false]      开启/关闭插件内置计分板",
            "pluginadmin [true|false]       开启/关闭插件管理员",
            "damagesplash [true|false|int]      开启/关闭/修改剑横扫伤害数量上限",
            "offlinemessage [true|false]    开启/关闭离线消息",
            "§2========================"
        };

        #region https: //github.com/zhkj-liuxiaohua/MGPlugins

        static Hashtable swords = new Hashtable();
        static JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();

        static void initSwordIds()
        {
            swords["wooden_sword"] = true;
            swords["stone_sword"] = true;
            swords["iron_sword"] = true;
            swords["diamond_sword"] = true;
            swords["golden_sword"] = true;
            swords["netherite_sword"] = true;
        }

        #endregion


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

        public static void BotListener(object sender, MessageEventArgs e)
        {
            string receive = e.Data;
            if (receive.Contains("\"type\": \"list\""))
            {
                FakePlayerData.List fakePlayerList = javaScriptSerializer.Deserialize<FakePlayerData.List>(receive);
                string list = string.Join("、", fakePlayerList.data.list);
                StandardizedFeedback("@a",$"服务器内存在假人 {list}");
            }
            else if (receive.Contains("\"type\": \"add\"")||receive.Contains("\"type\": \"remove\"")||receive.Contains("\"type\": \"connect\"")||receive.Contains("\"type\": \"disconnect\""))
            {
                FakePlayerData.Operation fakePlayerOperation =
                    javaScriptSerializer.Deserialize<FakePlayerData.Operation>(receive);
                if (!fakePlayerOperation.data.success)
                {
                    StandardizedFeedback("@a",$"操作假人{fakePlayerOperation.data.name}失败");
                }
            }
        }

        public static void ForceGamemode(Object source, ElapsedEventArgs e)
        {
            _mapi.runcmd("gamemode default @a");
        }

        public static void OnlineMinutesAcc(Object source, ElapsedEventArgs e)
        {
            _mapi.runcmd($"scoreboard players add @a[tag=!BOT] OnlineMinutes 1");
        }

        static string[] StaAutoSwitchesList =
        {
            "Dig", "Placed", "Killed", "Tasks", "Dead", "OnlineMinutes"
        };

        static int StaAutoSwitchesTimer = 0;

        public static void StaAutoSwitches(Object source, ElapsedEventArgs e)
        {
            _mapi.runcmd($"scoreboard objectives setdisplay sidebar {StaAutoSwitchesList[StaAutoSwitchesTimer]}");
            if (StaAutoSwitchesTimer >= StaAutoSwitchesList.Length - 1)
            {
                StaAutoSwitchesTimer = 0;
            }
            else
            {
                StaAutoSwitchesTimer++;
            }
        }

        public static void AutoBackup(Object source, ElapsedEventArgs e)
        {
            if (DateTime.Now.ToString("t") == config.AutoBackupTime)
            {
                StandardizedFeedback("@a", $"服务器将在§l5秒§r后重启进行每日自动备份，预计需要一分钟");
                Task.Run(async delegate
                {
                    await Task.Delay(5000);
                    Process.Start(PluginPath.QbHelperPath,
                        $"MAKE {config.WorldName} AUTO {DateTime.Now:MMdd}EverydayAutoBackup|每日自动备份 {config.PluginLoader.CustomizationPath}");
                    _mapi.runcmd("stop");
                });
            }
        }

        public static string FormatSize(long size)
        {
            double d = (double) size;
            int i = 0;
            while ((d > 1024) && (i < 5))
            {
                d /= 1024;
                i++;
            }

            string[] unit = {"B", "KB", "MB", "GB", "TB"};
            return (string.Format("{0} {1}", Math.Round(d, 2), unit[i]));
        }


        public static long GetWorldSize(String path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            long length = 0;
            foreach (FileSystemInfo fsi in directoryInfo.GetFileSystemInfos())
            {
                if (fsi is FileInfo)
                {
                    length += ((FileInfo) fsi).Length;
                }
                else
                {
                    length += GetWorldSize(fsi.FullName);
                }
            }

            return length;
        }

        public static void InitializePlugin()
        {
            DirectoryInfo pluginRootDirectory = new DirectoryInfo(PluginPath.RootPath);
            if (!pluginRootDirectory.Exists)
            {
                pluginRootDirectory.Create();
            }

            DirectoryInfo logsRootDirectory = new DirectoryInfo(PluginPath.LogsRootPath);
            if (!logsRootDirectory.Exists)
            {
                logsRootDirectory.Create();
            }

            DirectoryInfo qbRootPath = new DirectoryInfo(PluginPath.QbRootPath);
            if (!qbRootPath.Exists)
            {
                qbRootPath.Create();
                File.Create(PluginPath.QbInfoPath);
                File.Create(PluginPath.QbLogPath);
                IniFile qbIniFile = new IniFile(PluginPath.QbInfoPath);
                for (int i = 0; i < 6; i++)
                {
                    string slot;
                    if (i == 0)
                    {
                        slot = "AUTO";
                    }
                    else
                    {
                        slot = i.ToString();
                    }

                    qbIniFile.IniWriteValue(slot, "WorldName", "null");
                    qbIniFile.IniWriteValue(slot, "BackupTime", "null");
                    qbIniFile.IniWriteValue(slot, "Comment", "null");
                    qbIniFile.IniWriteValue(slot, "Size", "0");
                }

                ConsoleOutputter("MCP", $@"请将QuickBackup.exe放入{PluginPath.QbHelperPath}以启用QuickBackup");
                LogsWriter("MCP", $@"请将QuickBackup.exe放入{PluginPath.QbHelperPath}以启用QuickBackup");
            }

            File.WriteAllText(PluginPath.PlayerDatasPath, javaScriptSerializer.Serialize(playerDatas));
            File.WriteAllText(PluginPath.ConfigPath, RawConfig.rawConfig);

            ConsoleOutputter("MCP", $@"已完成插件配置文件的初始化.配置文件位于{PluginPath.ConfigPath} .请完成配置文件后重启服务器.");
            LogsWriter("MCP", $@"已完成插件配置文件的初始化.配置文件位于{PluginPath.ConfigPath} .请完成配置文件后重启服务器.");
        }

        public static void LoadPlugin(bool firstLoad = false)
        {
            if (!File.Exists(PluginPath.ConfigPath)) InitializePlugin();
            string configText = File.ReadAllText(PluginPath.ConfigPath);
            config = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build()
                .Deserialize<Config>(configText);
            
            if (firstLoad)
            {
                string savedPlayerDatas = File.ReadAllText(PluginPath.PlayerDatasPath);
                if (!string.IsNullOrWhiteSpace(savedPlayerDatas))
                    playerDatas = javaScriptSerializer.Deserialize<Dictionary<string, PlayerDatas>>(savedPlayerDatas);
            }

            if (!Directory.Exists(PluginPath.QbRootPath) || !File.Exists(PluginPath.QbHelperPath))
            {
                ConsoleOutputter("MCP", "快速备份QuickBackup核心组件丢失，@qb无法使用");
                LogsWriter("MCP", "快速备份QuickBackup核心组件丢失，@qb无法使用");
                ConsoleOutputter("MCP", $@"请将QuickBackup.exe放入{PluginPath.QbRootPath}以启用QuickBackup");
                LogsWriter("MCP", $@"请将QuickBackup.exe放入{PluginPath.QbRootPath}以启用QuickBackup");
                config.PluginDisable.Futures.QuickBackup = true;
            }

            if (config.WorldName.Contains(" "))
            {
                ConsoleOutputter("MCP", "存档名包含空格,QuickBackup无法工作!请进行修改.");
                LogsWriter("MCP", "存档名包含空格,QuickBackup无法工作!请进行修改.");
                config.PluginDisable.Futures.QuickBackup = true;
            }

            if (!Directory.Exists($@"worlds\{config.WorldName}"))
            {
                ConsoleOutputter("MCP", "找不到指定存档,QuickBackup无法工作!请检查配置文件.");
                LogsWriter("MCP", "找不到指定存档,QuickBackup无法工作!请检查配置文件.");
                config.PluginDisable.Futures.QuickBackup = true;
            }

            if (config.PluginDisable.Futures.Statistics.OnlineMinutes)
            {
                if (onlineMinutesAccTimer != null)
                {
                    onlineMinutesAccTimer.Dispose();
                }
            }
            else
            {
                onlineMinutesAccTimer = new Timer(60000);
                onlineMinutesAccTimer.Elapsed += OnlineMinutesAcc;
                onlineMinutesAccTimer.AutoReset = true;
                onlineMinutesAccTimer.Start();
            }

            if (!config.AntiCheat.Enable || !config.AntiCheat.ForceGamemode)
            {
                if (forceGamemodeTimer != null)
                {
                    forceGamemodeTimer.Dispose();
                }
            }
            else
            {
                forceGamemodeTimer = new Timer(2000);
                forceGamemodeTimer.Elapsed += ForceGamemode;
                forceGamemodeTimer.AutoReset = true;
                forceGamemodeTimer.Start();
            }

            if (config.PluginDisable.Futures.AutoBackup || config.PluginDisable.Futures.QuickBackup)
            {
                if (autoBackupTimer != null)
                {
                    autoBackupTimer.Dispose();
                }
            }
            else
            {
                autoBackupTimer = new Timer(60000);
                autoBackupTimer.Elapsed += AutoBackup;
                autoBackupTimer.AutoReset = true;
                autoBackupTimer.Start();
            }

            switch (config.PluginLoader.Type)
            {
                case "DTConsole":
                    config.PluginLoader.CustomizationPath = @"..\MCModDllExe\debug.bat";
                    break;
                case "LiteLoader":
                case "BedrockX":
                case "BDXCore":
                    config.PluginLoader.CustomizationPath = @"bedrock_server.exe";
                    break;
                case "ElementZero":
                    config.PluginLoader.CustomizationPath = @"bedrock_server_mod.exe";
                    break;
                default:
                    config.PluginLoader.CustomizationPath = config.PluginLoader.CustomizationPath;
                    break;
            }

            if (!File.Exists(config.PluginLoader.CustomizationPath))
            {
                ConsoleOutputter("MCP", "找不到指定的插件加载器,QuickBackup无法重启服务器!请检查配置文件.");
                LogsWriter("MCP", "找不到指定的插件加载器,QuickBackup无法重启服务器!请检查配置文件.");
            }

            bool webSocketStatus;
            if (!config.PluginDisable.Futures.FakePlayer)
            {
                try
                {
                    webSocket = new WebSocket($@"ws://{config.FakePlayer.Address}:{config.FakePlayer.Port}");
                    webSocket.OnMessage += BotListener;
                    webSocket.Connect();
                }
                catch
                {
                    ConsoleOutputter("MCP","无法连接至FakePlayer的WebSocket服务器,请检查设置.");
                    LogsWriter("MCP","无法连接至FakePlayer的WebSocket服务器,请检查设置.");
                    config.PluginDisable.Futures.FakePlayer = true;
                }
            }

            ConsoleOutputter("MCP", "已完成初始化。");
            LogsWriter("MCP", "已完成初始化。");
        }

        public static void Init(MCCSAPI api)
        {
            List<string> onlinePlayer = new List<string>();
            List<string> acceptPlayer = new List<string>();
            Timer staAutoSwitchesTimer = null;
            bool isQuickSleep = false;
            string quickSleepName = "";

            _mapi = api;
            LoadPlugin(true);

            api.addAfterActListener(EventKey.onInputText, x =>
            {
                var e = BaseEvent.getFrom(x) as InputTextEvent;
                if (e == null) return true;
                string name = e.playername;
                string xuid = playerDatas[name].Xuid;
                string msg = e.msg;
                var position = (x: ((int) e.XYZ.x).ToString(), y: ((int) e.XYZ.y).ToString(),
                    z: ((int) e.XYZ.z).ToString(), world: e.dimension);
                CsPlayer csPlayer = new CsPlayer(api, e.playerPtr);

                if (msg.StartsWith(config.CmdPrefix))
                {
                    string[] argsList = msg.Split(' ');
                    argsList[0] = argsList[0].Replace(config.CmdPrefix, "");

                    foreach (var disableCommand in config.PluginDisable.Commands)
                    {
                        if (msg.Replace(config.CmdPrefix, "").StartsWith(disableCommand))
                        {
                            StandardizedFeedback("@a", $"{msg}已被通过配置文件禁用,当前无法使用.详情请咨询Hil.");
                            return true;
                        }
                    }

                    if (config.PluginAdmin.Enable)
                    {
                        bool isAdminCmd = false;
                        bool isLegal = false;
                        foreach (var adminCmd in config.PluginAdmin.AdminCmd)
                        {
                            if (msg.Replace(config.CmdPrefix, "").StartsWith(adminCmd))
                            {
                                isAdminCmd = true;
                                foreach (var player in config.PluginAdmin.AdminList)
                                {
                                    if (player.Name == name && player.Xuid == xuid) isLegal = true;
                                }
                            }
                        }

                        if (isAdminCmd && !isLegal)
                        {
                            StandardizedFeedback(name, $"{msg}需要admin权限才可使用，您当前无权使用该指令。");
                            return true;
                        }
                    }

                    if (config.Logging.Plugin) LogsWriter(name, msg);
                    if (config.ConsoleOutput.Plugin) ConsoleOutputter(name, msg);

                    switch (argsList[0])
                    {
                        case "mcp":
                            if (argsList[1] == "status")
                            {
                                string[] pluginStatusTexts =
                                {
                                    "§2========================",
                                    $"§c§l{PluginInfo.Name} - {PluginInfo.Version}",
                                    $"§o作者：{PluginInfo.Author}",
                                    "§2========================",
                                    $"{config.CmdPrefix}mcp help      获取MCP模块帮助",
                                    $"{config.CmdPrefix}mcp initialize        初始化MCP模块",
                                    $"{config.CmdPrefix}mcp setting [option] [value]       修改MCP模块设置",
                                    $"{config.CmdPrefix}mcp setting reload      重载MCP配置文件",
                                    "§2========================"
                                };
                                foreach (var pluginStatusText in pluginStatusTexts)
                                {
                                    StandardizedFeedback(name, pluginStatusText);
                                }
                            }
                            else if (argsList[1] == "help")
                            {
                                foreach (var helpText in HelpTexts)
                                {
                                    StandardizedFeedback(name, config.CmdPrefix + helpText);
                                }
                            }
                            else if (argsList[1] == "initialize")
                            {
                                api.runcmd("scoreboard objectives add Killed dummy §l§7击杀榜");
                                api.runcmd("scoreboard objectives add Dig dummy §l§7挖掘榜");
                                api.runcmd("scoreboard objectives add Dead dummy §l§7死亡榜");
                                api.runcmd("scoreboard objectives add Placed dummy §l§7放置榜");
                                // api.runcmd("scoreboard objectives add Attack dummy §l§7伤害榜");
                                // api.runcmd("scoreboard objectives add Hurt dummy §l§7承伤榜");
                                // api.runcmd("scoreboard objectives add Used dummy §l§7使用榜");
                                api.runcmd("scoreboard objectives add Tasks dummy §l§e服务器摸鱼指南");
                                api.runcmd("scoreboard objectives add OnlineMinutes dummy §l§7在线时长榜(分钟)");
                                api.runcmd("scoreboard objectives add _CounterCache dummy");
                                api.runcmd("scoreboard objectives add Counter dummy");
                                api.runcmd("gamerule sendCommandFeedback false");
                            }
                            else if (argsList[1] == "setting")
                            {
                                if (argsList[2] == "reload")
                                {
                                    LoadPlugin();
                                    StandardizedFeedback("@a", "[MCP]配置文件已重新载入。");
                                    return true;
                                }

                                if (argsList[2] == "help")
                                {
                                    foreach (var pluginSettingHelpText in PluginSettingHelpTexts)
                                    {
                                        StandardizedFeedback(name, pluginSettingHelpText);
                                    }

                                    return true;
                                }

                                switch (argsList[2])
                                {
                                    case "anticheat":
                                    {
                                        string newConfig = argsList[3];
                                        if (newConfig == "true" || newConfig == "false")
                                        {
                                            config.AntiCheat.Enable = bool.Parse(newConfig);
                                            StandardizedFeedback("@a",
                                                newConfig == "true" ? $"{name}已开启反作弊系统" : $"{name}已关闭反作弊系统");
                                        }
                                        else
                                        {
                                            StandardizedFeedback(name, "仅允许使用true或false来设置反作弊系统状态");
                                        }

                                        break;
                                    }
                                    case "prefix":
                                    {
                                        string newConfig = argsList[3];
                                        StandardizedFeedback("@a",
                                            $"MCP指令前缀已被{name}从{config.CmdPrefix}修改为{newConfig}");
                                        config.CmdPrefix = newConfig;
                                        break;
                                    }
                                    case "staautoswitchesfreq":
                                    {
                                        string newConfig = argsList[3];
                                        StandardizedFeedback("@a",
                                            $"计分板自动切换周期已被{name}从{config.StaAutoSwitchesFreq}修改为{newConfig}");
                                        config.StaAutoSwitchesFreq = int.Parse(newConfig);
                                        break;
                                    }
                                    case "whitelist":
                                    {
                                        string newConfig = argsList[3];
                                        if (newConfig == "true" || newConfig == "false")
                                        {
                                            config.WhiteList.Enable = bool.Parse(newConfig);
                                            StandardizedFeedback("@a",
                                                newConfig == "true" ? $"{name}已开启内置白名单系统" : $"{name}已关闭内置白名单系统");
                                        }
                                        else
                                        {
                                            StandardizedFeedback(name, "仅允许使用true或false来设置内置白名单系统状态");
                                        }

                                        break;
                                    }
                                    case "pluginadmin":
                                    {
                                        string newConfig = argsList[3];
                                        if (newConfig == "true" || newConfig == "false")
                                        {
                                            config.PluginAdmin.Enable = bool.Parse(newConfig);
                                            StandardizedFeedback("@a",
                                                newConfig == "true" ? $"{name}已开启插件管理员系统" : $"{name}已关闭插件管理员系统");
                                        }
                                        else
                                        {
                                            StandardizedFeedback(name, "仅允许使用true或false来设置插件管理员系统状态");
                                        }

                                        break;
                                    }
                                    case "damagesplash":
                                    {
                                        string newConfig = argsList[3];
                                        if (newConfig == "true" || newConfig == "false")
                                        {
                                            StandardizedFeedback("@a",
                                                bool.Parse(newConfig)
                                                    ? $"{name}已开启剑横扫伤害,数量上限为{config.MaxDamageSplash}"
                                                    : $"{name}已关闭剑横扫伤害");
                                        }
                                        else
                                        {
                                            StandardizedFeedback("@a",
                                                $"剑横扫伤害数量上限已被{name}从{config.MaxDamageSplash}修改为{newConfig}");
                                            config.StaAutoSwitchesFreq = int.Parse(newConfig);
                                        }

                                        break;
                                    }
                                    case "offlinemessage":
                                    {
                                        string newConfig = argsList[3];
                                        if (newConfig == "true" || newConfig == "false")
                                        {
                                            config.PluginDisable.Futures.OfflineMessage = bool.Parse(newConfig);
                                            StandardizedFeedback("@a",
                                                newConfig == "true" ? $"{name}已开启离线消息" : $"{name}已关闭离线消息");
                                        }
                                        else
                                        {
                                            StandardizedFeedback(name, "仅允许使用true或false来设置离线消息状态");
                                        }

                                        break;
                                    }
                                }

                                string newYaml = new Serializer().Serialize(config);
                                File.WriteAllText(PluginPath.ConfigPath, newYaml);
                            }
                            else
                            {
                                StandardizedFeedback(name,
                                    $"无效的{config.CmdPrefix}mcp指令，请使用{config.CmdPrefix}mcp status获取帮助");
                            }

                            break;
                        case "calc":
                            string expression = argsList[1];
                            //g个 z组 h盒
                            if (!expression.Contains("h") && !expression.Contains("z") && expression.Contains("g"))
                            {
                                int totalItemNumber = int.Parse(expression.Replace("g", ""));
                                int boxNumber = totalItemNumber / (3 * 9 * 64);
                                int stackNumber = (totalItemNumber - boxNumber * (3 * 9 * 64)) / 64;
                                int itemNumber = totalItemNumber - boxNumber * (3 * 9 * 64) - stackNumber * 64;
                                StandardizedFeedback("@a",
                                    $"§6{totalItemNumber}个§7物品共§e{boxNumber}盒§a{stackNumber}组§b{itemNumber}个§7.");
                            }
                            else if (expression.Contains("h") || expression.Contains("z") || expression.Contains("g"))
                            {
                                int boxNumber = 0;
                                int stackNumber = 0;
                                int itemNumber = 0;
                                if (expression.Contains("h") && expression.Contains("z") && expression.Contains("g"))
                                {
                                    string[] originalNumber = expression.Split(new char[] {'h', 'z', 'g'});
                                    boxNumber = int.Parse(originalNumber[0]);
                                    stackNumber = int.Parse(originalNumber[1]);
                                    itemNumber = int.Parse(originalNumber[2]);
                                }
                                else if (expression.Contains("h") && expression.Contains("z") &&
                                         !expression.Contains("g"))
                                {
                                    string[] originalNumber = expression.Split(new char[] {'h', 'z'});
                                    boxNumber = int.Parse(originalNumber[0]);
                                    stackNumber = int.Parse(originalNumber[1]);
                                }
                                else if (expression.Contains("h") && !expression.Contains("z") &&
                                         expression.Contains("g"))
                                {
                                    string[] originalNumber = expression.Split(new char[] {'h', 'g'});
                                    boxNumber = int.Parse(originalNumber[0]);
                                    itemNumber = int.Parse(originalNumber[1]);
                                }
                                else if (!expression.Contains("h") && expression.Contains("z") &&
                                         expression.Contains("g"))
                                {
                                    string[] originalNumber = expression.Split(new char[] {'z', 'g'});
                                    stackNumber = int.Parse(originalNumber[0]);
                                    itemNumber = int.Parse(originalNumber[1]);
                                }
                                else if (expression.Contains("h") && !expression.Contains("z") &&
                                         !expression.Contains("g"))
                                {
                                    string[] originalNumber = expression.Split(new char[] {'h'});
                                    boxNumber = int.Parse(originalNumber[0]);
                                }
                                else if (!expression.Contains("h") && expression.Contains("z") &&
                                         !expression.Contains("g"))
                                {
                                    string[] originalNumber = expression.Split(new char[] {'z'});
                                    stackNumber = int.Parse(originalNumber[0]);
                                }

                                int totalItemNumber = boxNumber * (3 * 9 * 64) + stackNumber * 64 + itemNumber;
                                StandardizedFeedback("@a",
                                    $"§e{boxNumber}盒§a{stackNumber}组§b{itemNumber}个§7物品共§6{totalItemNumber}个§7.");
                            }
                            else
                            {
                                string result = new DataTable().Compute(expression, "").ToString();
                                StandardizedFeedback("@a", $"{expression} = {result}");
                            }

                            break;
                        case "here":
                            api.runcmd("playsound random.levelup @a");
                            StandardizedFeedback("@a",
                                $"§e§l{name}§r在{position.world}§e§l[{position.x},{position.y},{position.z}]§r向大家打招呼！");
                            break;
                        case "back":
                            if (playerDatas[name].DeadEnable)
                            {
                                if (playerDatas[name].DeadWorld == position.world)
                                {
                                    api.runcmd(
                                        $"tp {name} {playerDatas[name].DeadX} {playerDatas[name].DeadY} {playerDatas[name].DeadZ} ");
                                }
                                else
                                {
                                    StandardizedFeedback(name,
                                        $"你的死亡点在{playerDatas[name].DeadWorld}，但是你现在在{position.world}。暂不支持跨世界传送。");
                                }
                            }
                            else
                            {
                                StandardizedFeedback(name, "暂无死亡记录");
                            }

                            break;
                        case "bot":
                            if (config.PluginDisable.Futures.FakePlayer)
                            {
                                StandardizedFeedback(name, "假人已被管理员禁用,当前无法使用.");
                                return true;
                            }
                            
                            if (argsList[1] == "list")
                            {
                                webSocket.Send("{\"type\": \"list\"}");
                            }
                            else
                            {
                                string botName = "bot_"+argsList[2];
                                if (argsList[1] == "add")
                                {
                                    webSocket.Send($"{{\"type\": \"add\",\"data\": {{\"name\": \"{botName}\",\"skin\": \"steve\"}}}}");
                                    webSocket.Send($"{{\"type\": \"connect\",\"data\": {{\"name\": \"{botName}\"}}}}");
                                }
                                else if (argsList[1] == "remove")
                                {
                                    webSocket.Send($"{{\"type\": \"disconnect\", \"data\": {{ \"name\": \"{botName}\" }}}}");
                                    webSocket.Send($"{{\"type\": \"remove\", \"data\": {{ \"name\": \"{botName}\" }}}}");
                                }
                                else if (argsList[1] == "tp")
                                {
                                    api.runcmd($"tp {name} {botName}");
                                }
                                else if (argsList[1]=="call")
                                {
                                    api.runcmd($"tp {botName} {name}");
                                }
                            }

                            break;
                        case "day":
                            if (argsList[1] == "game")
                            {
                                api.runcmd("time query day");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    string gameDay = GameDatas.GameDay;
                                    StandardizedFeedback("@a", $"现在是游戏内的第{gameDay}天.");
                                });
                            }
                            else if (argsList[1] == "server")
                            {
                                DateTime nowDate = DateTime.Now;
                                DateTime worldStartDate = DateTime.Parse(config.WorldStartDate);
                                string serverDay =
                                    ((int) nowDate.Subtract(worldStartDate).TotalDays).ToString();
                                StandardizedFeedback("@a", $"今天是开服的第{serverDay}天.");
                            }

                            break;
                        case "item":
                            if (argsList[1] == "pick")
                            {
                                api.runcmd($"tp @e[type=item] {name}");
                                StandardizedFeedback("@a", $"{name}已拾取所有掉落物");
                            }
                            else if (argsList[1] == "clear")
                            {
                                api.runcmd("kill @e[type=item]");
                                StandardizedFeedback("@a", $"{name}已清除所有掉落物");
                            }
                            else if (argsList[1] == "count")
                            {
                                api.runcmd("scoreboard players set @e[type=item] _CounterCache 1");
                                api.runcmd("scoreboard players set \"itemCounter\" Counter 0");
                                api.runcmd(
                                    "scoreboard players operation \"itemCounter\" Counter += @e[type=item] _CounterCache");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    string itemCount = GameDatas.ItemCounter;
                                    StandardizedFeedback("@a", $"当前的掉落物数为{itemCount}");
                                });
                            }

                            break;
                        case "entity":
                            if (argsList[1] == "count")
                            {
                                api.runcmd("scoreboard players set @e _CounterCache 1");
                                api.runcmd("scoreboard players set \"entityCounter\" Counter 0");
                                api.runcmd("scoreboard players operation \"entityCounter\" Counter+= @e _CounterCache");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    string entityCount = GameDatas.EntityCounter;
                                    StandardizedFeedback("@a", $"当前的实体数为{entityCount}");
                                });
                            }
                            else if (argsList[1] == "list")
                            {
                                api.runcmd("say §r§o§9服务器内的实体列表为§r§l§f @e");
                            }

                            break;
                        case "ki":
                            if (argsList[1] == "status")
                            {
                                api.runcmd("gamerule keepInventory");

                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    StandardizedFeedback("@a", $"当前死亡不掉落{GameDatas.KiStatus}");
                                });
                            }
                            else if (argsList[1] == "true")
                            {
                                api.runcmd("gamerule keepInventory true");
                                StandardizedFeedback("@a", "死亡不掉落已开启");
                            }
                            else if (argsList[1] == "false")
                            {
                                api.runcmd("gamerule keepInventory false");
                                StandardizedFeedback("@a", "死亡不掉落已关闭");
                            }

                            break;
                        case "kill":
                            playerDatas[name].IsSuicide = true;
                            api.runcmd($"kill {name}");
                            if (!config.PluginDisable.Futures.SuicideMessages)
                            {
                                string[] _suicideMsgs = config.SuicideMessages;
                                for (int i = 0; i < _suicideMsgs.Length; i++)
                                {
                                    _suicideMsgs[i] = _suicideMsgs[i].Replace("{}", $"§l{name}§r");
                                }

                                int suicideMsgNum = new Random().Next(0, _suicideMsgs.Length);
                                StandardizedFeedback("@a", _suicideMsgs[suicideMsgNum]);
                            }

                            break;
                        case "mg":
                            if (argsList[1] == "status")
                            {
                                api.runcmd("gamerule mobGriefing");

                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    StandardizedFeedback("@a", $"当前生物破坏{GameDatas.MgStatus}");
                                });
                            }
                            else if (argsList[1] == "true")
                            {
                                api.runcmd("gamerule mobGriefing true");
                                StandardizedFeedback("@a", "生物破坏已开启");
                            }
                            else if (argsList[1] == "false")
                            {
                                api.runcmd("gamerule mobGriefing false");
                                StandardizedFeedback("@a", "生物破坏已关闭");
                            }

                            break;
                        case "network":
                            string[] ipport = csPlayer.IpPort.Split('|');
                            if (argsList[1] == "ip")
                            {
                                string ip = ipport[0];
                                StandardizedFeedback("@a", $"§l{name}§r的IP地址为§e§l{ip}");
                            }
                            else if (argsList[1] == "port")
                            {
                                string port = ipport[1];
                                StandardizedFeedback("@a", $"§l{name}§r的端口号为§e§l{port}");
                            }
                            else if (argsList[1] == "ping")
                            {
                                Ping ping = new Ping();
                                PingReply pingReply = ping.Send(ipport[0]);
                                if (pingReply.Status == IPStatus.Success)
                                {
                                    StandardizedFeedback("@a",
                                        $"服务器到§l{name}§r的延迟为§e§l{pingReply.RoundtripTime.ToString()}§r§lms");
                                }
                                else
                                {
                                    StandardizedFeedback("@a",
                                        $"服务器到§l{name}§r的延迟测试失败.原因为{pingReply.Status.ToString()}");
                                }
                            }

                            break;
                        case "om":
                        {
                            if (config.PluginDisable.Futures.OfflineMessage)
                            {
                                StandardizedFeedback(name, "离线消息已被管理员禁用,当前无法使用.");
                                return true;
                            }

                            string recipient = argsList[1];
                            string content = argsList[2];
                            if (!playerDatas.ContainsKey(recipient))
                            {
                                StandardizedFeedback("@a", $"无法找到玩家{recipient}的收件地址!!");
                            }
                            else if (playerDatas[recipient].IsOnline)
                            {
                                StandardizedFeedback("@a", $"玩家{recipient}当前在线,无法投递信件.");
                            }
                            else
                            {
                                playerDatas[recipient].OfflineMessage.Add($"- §7[{DateTime.Now}]§r<{name}>:{content}");
                                StandardizedFeedback(name, $"已向玩家{recipient}投递了一份离线消息.");
                            }

                            break;
                        }
                        case "rc":
                            string command = msg.Replace($"{config.CmdPrefix}rc ", "");
                            bool cmdResult = api.runcmd(command);
                            StandardizedFeedback("@a", cmdResult ? $"已成功向控制台注入了{command}" : $"{command}运行失败");

                            break;
                        case "size":
                            //string worldSize = ((int) (GetWorldSize($@"worlds\{worldName}") / 1024 / 1024)).ToString();
                            string worldSize = FormatSize(GetWorldSize($@"worlds\{config.WorldName}"));
                            StandardizedFeedback("@a", $"当前服务器的存档大小是§l§6{worldSize}");
                            break;
                        case "sta":
                            if (argsList[1] == "auto")
                            {
                                if (bool.Parse(argsList[2]))
                                {
                                    if (staAutoSwitchesTimer != null)
                                    {
                                        StandardizedFeedback("@a", "计分板自动切换正在运行中，请勿重复开启.");
                                        return true;
                                    }

                                    staAutoSwitchesTimer = new Timer();
                                    staAutoSwitchesTimer.Interval = config.StaAutoSwitchesFreq * 1000;
                                    staAutoSwitchesTimer.Elapsed += StaAutoSwitches;
                                    staAutoSwitchesTimer.AutoReset = true;
                                    staAutoSwitchesTimer.Start();
                                    StandardizedFeedback("@a", $"已开启计分板自动切换.切换周期:{config.StaAutoSwitchesFreq}秒");
                                }
                                else
                                {
                                    if (staAutoSwitchesTimer == null)
                                    {
                                        StandardizedFeedback("@a", "计分板自动切换未开启，请勿重复关闭.");
                                        return true;
                                    }

                                    staAutoSwitchesTimer.Stop();
                                    staAutoSwitchesTimer.Dispose();
                                    StandardizedFeedback("@a", "已关闭计分板自动切换.");
                                    staAutoSwitchesTimer = null;
                                }
                            }
                            else
                            {
                                string statisName = argsList[1];
                                Dictionary<string, string> cnStatisName = new Dictionary<string, string>
                                {
                                    {"Dig", "挖掘榜"},
                                    {"Placed", "放置榜"},
                                    {"Killed", "击杀榜"},
                                    {"Tasks", "待办事项榜"},
                                    {"Dead", "死亡榜"},
                                    {"OnlineMinutes", "在线时长榜(分钟)"}
                                };
                                if (statisName != "null")
                                {
                                    api.runcmd($"scoreboard objectives setdisplay sidebar {statisName}");
                                    StandardizedFeedback("@a",
                                        cnStatisName.ContainsKey(statisName)
                                            ? $"已将侧边栏显示修改为{cnStatisName[statisName]}"
                                            : $"已将侧边栏显示修改为{statisName}");
                                }
                                else
                                {
                                    api.runcmd("scoreboard objectives setdisplay sidebar");
                                    StandardizedFeedback("@a", "已关闭侧边栏显示");
                                }
                            }


                            break;
                        case "system":
                            SystemInfo systemInfo = new SystemInfo();
                            if (argsList[1] == "cpu")
                            {
                                string cpuUsage = systemInfo.GetCpuUsage();
                                StandardizedFeedback("@a", $"当前服务器CPU占用率为§l§6{cpuUsage}");
                            }
                            else if (argsList[1] == "memory")
                            {
                                string memoryUsage = systemInfo.GetMemoryUsage();
                                StandardizedFeedback("@a", $"当前服务器物理内存占用率为§l§6{memoryUsage}");
                            }

                            break;
                        case "task":
                            string taskName = argsList[2];
                            if (argsList[1] == "add")
                            {
                                api.runcmd($"scoreboard players set {taskName} Tasks 1");
                                StandardizedFeedback("@a", $"已向待办事项板添加§l{taskName}");
                            }
                            else if (argsList[1] == "remove")
                            {
                                api.runcmd($"scoreboard players reset {taskName} Tasks");
                                StandardizedFeedback("@a", $"已将§l{taskName}§r从待办事项板上移除");
                            }

                            break;
                        case "tick":
                            if (argsList[1] == "status")
                            {
                                api.runcmd("gamerule randomtickspeed");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    StandardizedFeedback("@a", $"现在的游戏随机刻为{GameDatas.TickStatus}");
                                });
                            }
                            else if (int.TryParse(argsList[1], out int tickSpeed))
                            {
                                api.runcmd($"gamerule randomtickspeed {tickSpeed}");
                                StandardizedFeedback("@a", $"已将游戏内随机刻修改为{tickSpeed}。");
                            }

                            break;
                        case "whitelist":
                            string pendingName = argsList[2];
                            if (argsList[1] == "add")
                            {
                                foreach (var player in config.WhiteList.PlayerList)
                                {
                                    if (player.Name == pendingName)
                                    {
                                        StandardizedFeedback(name, $"白名单中已存在玩家{pendingName}");
                                        return true;
                                    }
                                }

                                if (playerDatas.ContainsKey(pendingName))
                                {
                                    config.WhiteList.PlayerList.Add(new Player()
                                        {Name = pendingName, Xuid = playerDatas[pendingName].Xuid});
                                    string newConfig = new Serializer().Serialize(config);
                                    File.WriteAllText(PluginPath.ConfigPath, newConfig);
                                    StandardizedFeedback("@a", $"{name}已将{pendingName}加入白名单。");
                                }
                                else
                                {
                                    StandardizedFeedback(name, $"{pendingName}未曾尝试加入过服务器，请让其尝试加入服务器后再添加白名单");
                                }
                            }
                            else if (argsList[1] == "remove")
                            {
                                foreach (var player in config.WhiteList.PlayerList)
                                {
                                    if (player.Name == pendingName)
                                    {
                                        if (playerDatas.ContainsKey(pendingName))
                                        {
                                            config.WhiteList.PlayerList.Remove(new Player()
                                                {Name = pendingName, Xuid = playerDatas[pendingName].Xuid});
                                            api.runcmd($"kick {pendingName} 您已被{name}永久封禁。");
                                            string newConfig = new Serializer().Serialize(config);
                                            File.WriteAllText(PluginPath.ConfigPath, newConfig);
                                            StandardizedFeedback("@a", $"{pendingName}已被{name}永久封禁。");
                                        }
                                        else
                                        {
                                            StandardizedFeedback(name, $"{pendingName}未曾加入过服务器，请让其加入服务器后再移除白名单");
                                        }

                                        return true;
                                    }
                                }

                                StandardizedFeedback(name, $"白名单中不存在玩家{pendingName}");
                            }

                            break;
                        case "qs":
                            if (argsList.Length == 1)
                            {
                                if (isQuickSleep == true)
                                {
                                    StandardizedFeedback("@a", $"现在游戏内已存在一个由{quickSleepName}发起的快速跳过夜晚投票");
                                    StandardizedFeedback("@a",
                                        $"请使用{config.CmdPrefix}qs accept投出支持票，使用{config.CmdPrefix}qs refuse投出反对票");
                                    return true;
                                }

                                onlinePlayer.Clear();
                                foreach (var playerData in playerDatas)
                                {
                                    if (playerData.Value.IsOnline == true)
                                    {
                                        onlinePlayer.Add(playerData.Key);
                                    }
                                }

                                if (onlinePlayer.Count <= 1)
                                {
                                    StandardizedFeedback(name, "孤单的你，害怕一人在野外入睡，你更渴望温暖的被窝。");
                                    return true;
                                }

                                api.runcmd("time query daytime");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    int gameTime = int.Parse(GameDatas.GameTime);
                                    if (gameTime >= 12544 && gameTime <= 23460)
                                    {
                                        isQuickSleep = true;
                                        quickSleepName = name;
                                        acceptPlayer.Clear();
                                        acceptPlayer.Add(quickSleepName);
                                        StandardizedFeedback("@a",
                                            $"§6§l{quickSleepName}发起快速跳过夜晚投票，请使用{config.CmdPrefix}qs accept投出支持票，使用{config.CmdPrefix}qs refuse投出反对票。");
                                        StandardizedFeedback("@a",
                                            $"§6§l当前已有{acceptPlayer.Count}人投出支持票，至少需要{onlinePlayer.Count}人");
                                        StandardizedFeedback("@a", "§6§l投票将在18秒后结束。");
                                        _ = Task.Run(async delegate
                                        {
                                            await Task.Delay(18000);
                                            if (acceptPlayer.Count >= onlinePlayer.Count)
                                            {
                                                StandardizedFeedback("@a",
                                                    "§5§l深夜，一阵突如其来的反常疲惫侵袭了你的大脑，你失去意识倒在地上。当你醒来时，太阳正从东方冉冉升起。");
                                                api.runcmd("time set sunrise");
                                                api.runcmd("effect @a instant_damage 1 1 true");
                                                api.runcmd("effect @a blindness 8 1 true");
                                                api.runcmd("effect @a nausea 8 1 true");
                                                api.runcmd("effect @a hunger 8 5 true");
                                                api.runcmd("effect @a slowness 8 5 true");
                                            }
                                            else
                                            {
                                                StandardizedFeedback("@a", $"§4§l{quickSleepName}忽然之间厌倦了夜晚。");
                                                api.runcmd($"effect {quickSleepName} instant_damage 1 1 true");
                                                api.runcmd($"effect {quickSleepName} blindness 8 1 true");
                                                api.runcmd($"effect {quickSleepName} nausea 8 1 true");
                                                api.runcmd($"effect {quickSleepName} hunger 8 5 true");
                                                api.runcmd($"effect {quickSleepName} slowness 8 5 true");
                                            }

                                            isQuickSleep = false;
                                            quickSleepName = "";
                                        });
                                    }
                                    else
                                    {
                                        StandardizedFeedback("@a", "光线突然变得不可抗拒地明亮了起来，刺眼的阳光令你久久无法入睡。");
                                    }
                                });
                            }
                            else if (argsList[1] == "accept")
                            {
                                if (isQuickSleep == true)
                                {
                                    if (!acceptPlayer.Contains(name))
                                    {
                                        acceptPlayer.Add(name);
                                        StandardizedFeedback("@a",
                                            $"§a{name}向{quickSleepName}发起的快速跳过夜晚投票投出支持票.已有{acceptPlayer.Count}人投出支持票，至少需要{onlinePlayer.Count}人。");
                                    }
                                    else
                                    {
                                        StandardizedFeedback(name, "请勿重复投票");
                                    }
                                }
                                else
                                {
                                    StandardizedFeedback(name, $"当前暂无快速跳过夜晚的投票，你可通过{config.CmdPrefix}qs进行发起");
                                }
                            }
                            else if (argsList[1] == "refuse")
                            {
                                if (isQuickSleep == true)
                                {
                                    if (acceptPlayer.Contains(name))
                                    {
                                        acceptPlayer.Remove(name);
                                    }

                                    StandardizedFeedback("@a", $"§c{name}向{quickSleepName}发起的快速跳过夜晚投票投出拒绝票.");
                                }
                                else
                                {
                                    StandardizedFeedback(name, $"当前暂无快速跳过夜晚的投票，你可通过{config.CmdPrefix}qs进行发起");
                                }
                            }

                            break;
                        case "qb":
                            if (config.PluginDisable.Futures.QuickBackup)
                            {
                                StandardizedFeedback("@a", "@qb已被插件自动禁用,请检查加载提示.");
                                return true;
                            }

                            if (argsList[1] == "make")
                            {
                                string slot = argsList[2];
                                string comment = argsList[3];
                                if (slot == "AUTO")
                                {
                                    StandardizedFeedback("@a", $"[槽位{slot}]为自动备份专用槽位,无法手动指定。槽位必须是1~5的整数，请重新指定槽位。");
                                    return true;
                                }

                                if (int.Parse(slot) < 1 || int.Parse(slot) > 5)
                                {
                                    StandardizedFeedback("@a", $"[槽位{slot}]无效。槽位必须是1~5的整数，请重新指定槽位。");
                                    return true;
                                }

                                if (!Directory.Exists($@"worlds\{config.WorldName}"))
                                {
                                    StandardizedFeedback("@a", "找不到待备份的存档,请检查配置文件!");
                                    return true;
                                }

                                StandardizedFeedback("@a", $"服务器将在§l5秒§r后重启，将存档备份至§l[槽位{slot}]§r，预计需要一分钟");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(5000);
                                    Process.Start(PluginPath.QbHelperPath,
                                        $"MAKE {config.WorldName} {slot} {comment} {config.PluginLoader.CustomizationPath}");
                                    api.runcmd("stop");
                                });
                            }
                            else if (argsList[1] == "back")
                            {
                                string slot = argsList[2];
                                if (!File.Exists($@"{PluginPath.QbRootPath}\{slot}.zip"))
                                {
                                    StandardizedFeedback("@a", $"[槽位{slot}]的备份不存在，请重新指定槽位。");
                                    return true;
                                }

                                StandardizedFeedback("@a", $"服务器将在§l5秒§r后重启，回档至§l[槽位{slot}]§r，预计需要一分钟");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(5000);
                                    Process.Start(PluginPath.QbHelperPath,
                                        $"BACK {config.WorldName} {slot} 0 {config.PluginLoader.CustomizationPath}");
                                    api.runcmd("stop");
                                });
                            }
                            else if (argsList[1] == "restart")
                            {
                                StandardizedFeedback("@a", "服务器将在§l5秒§r后进行重启，预计需要一分钟");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(5000);
                                    Process.Start(PluginPath.QbHelperPath,
                                        $"RESTART {config.WorldName} 0 0 {config.PluginLoader.CustomizationPath}");
                                    api.runcmd("stop");
                                });
                            }
                            else if (argsList[1] == "list")
                            {
                                IniFile qbIniFile = new IniFile(PluginPath.QbInfoPath);
                                StandardizedFeedback("@a", "§d§l【QuickBackup各槽位信息】");
                                for (int i = 0; i < 6; i++)
                                {
                                    string slot;
                                    if (i == 0)
                                    {
                                        slot = "AUTO";
                                    }
                                    else
                                    {
                                        slot = i.ToString();
                                    }

                                    string qbWorldName = qbIniFile.IniReadValue(slot, "WorldName");
                                    string qbTime = qbIniFile.IniReadValue(slot, "BackupTime");
                                    string qbComment = qbIniFile.IniReadValue(slot, "Comment");
                                    string qbSize = qbIniFile.IniReadValue(slot, "Size");

                                    StandardizedFeedback("@a",
                                        $"[槽位{slot}]备份存档:§6{qbWorldName}§r  备份时间:§l{qbTime}§r  注释:{qbComment}  大小:{FormatSize(long.Parse(qbSize))}");
                                }
                            }

                            break;
                        default:
                            StandardizedFeedback(name, $"无效的MCP指令，请输入{config.CmdPrefix}mcp help获取帮助");
                            break;
                    }
                }
                else
                {
                    if (config.Logging.Chat) LogsWriter(name, msg);
                    if (config.ConsoleOutput.Chat) ConsoleOutputter(name, msg);
                }

                return true;
            });

            #region https: //github.com/zhkj-liuxiaohua/MGPlugins

            api.addAfterActListener(EventKey.onAttack, x =>
            {
                if (config.PluginDisable.Futures.SplashDamage) return true;

                var e = BaseEvent.getFrom(x) as AttackEvent;
                if (e == null) return true;

                if (!e.isstand) return true;
                CsPlayer csPlayer = new CsPlayer(api, e.playerPtr);
                CsActor csActor = new CsActor(api, e.attackedentityPtr);
                var hand = javaScriptSerializer.Deserialize<ArrayList>(csPlayer.HandContainer);
                if (hand != null && hand.Count > 0)
                {
                    var mainHand = hand[0] as Dictionary<string, object>;
                    if (mainHand != null)
                    {
                        object oid;
                        if (mainHand.TryGetValue("rawnameid", out oid))
                        {
                            string rid = oid as string;
                            var oisSword = swords[rid];
                            if (oisSword != null && (bool) oisSword)
                            {
                                //开始执行溅射伤害操作
                                var pdata = csActor.Position;
                                var aXYZ = javaScriptSerializer.Deserialize<Vec3>(csActor.Position);
                                var list = CsActor.getsFromAABB(api, csActor.DimensionId, aXYZ.x - 2, aXYZ.y - 1,
                                    aXYZ.z - 2,
                                    aXYZ.x + 2, aXYZ.y + 1, aXYZ.z + 2);
                                if (list != null && list.Count > 0)
                                {
                                    int count = 0;
                                    foreach (IntPtr aptr in list)
                                    {
                                        if (aptr != e.attackedentityPtr)
                                        {
                                            CsActor spa = new CsActor(api, aptr);
                                            if (((spa.TypeId & 0x100) == 0x100))
                                            {
                                                spa.hurt(e.playerPtr, ActorDamageCause.EntityAttack, 1, true, false);
                                                ++count;
                                            }
                                        }

                                        if (count >= config.MaxDamageSplash)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
            });

            #endregion

            initSwordIds();
            api.addAfterActListener(EventKey.onMobDie, x =>
            {
                var e = BaseEvent.getFrom(x) as MobDieEvent;
                if (e == null) return true;

                string attackName = e.srcname;
                string attackType = e.srctype;
                string deadName = e.mobname;
                string deadType = e.mobtype;
                if (!config.PluginDisable.Futures.Statistics.Killed)
                {
                    if (attackType == "entity.player.name")
                    {
                        api.runcmd($"scoreboard players add @a[name={attackName},tag=!BOT] Killed 1");
                    }
                }

                if (deadType == "entity.player.name")
                {
                    var deadPosition = (x: ((int) e.XYZ.x).ToString(), y: ((int) e.XYZ.y).ToString(),
                        z: ((int) e.XYZ.z).ToString(), world: e.dimension);
                    if (!config.PluginDisable.Futures.DeathPointReport)
                    {
                        StandardizedFeedback("@a",
                            $"§r§l§f{deadName}§r§o§4 死于 §r§l§f{deadPosition.world}[{deadPosition.x},{deadPosition.y},{deadPosition.z}]");
                    }

                    if (deadName.StartsWith("bot_")) return true;

                    playerDatas[deadName].DeadX = deadPosition.x;
                    playerDatas[deadName].DeadY = deadPosition.y;
                    playerDatas[deadName].DeadZ = deadPosition.z;
                    playerDatas[deadName].DeadWorld = deadPosition.world;
                    playerDatas[deadName].DeadEnable = true;
                    if (playerDatas[deadName].IsSuicide)
                    {
                        playerDatas[deadName].IsSuicide = false;
                    }
                    else
                    {
                        if (!config.PluginDisable.Futures.Statistics.Death)
                        {
                            api.runcmd($"scoreboard players add @a[tag=!BOT,name={deadName}] Dead 1");
                        }
                    }
                }
                return true;
            });

            api.addAfterActListener(EventKey.onDestroyBlock, x =>
            {
                if (config.PluginDisable.Futures.Statistics.Excavation) return true;
                var e = BaseEvent.getFrom(x) as DestroyBlockEvent;
                if (e == null) return true;

                string name = e.playername;
                if (!string.IsNullOrEmpty(name))
                {
                    api.runcmd($"scoreboard players add @a[name={name},tag=!BOT] Dig 1");
                }

                return true;
            });

            api.addAfterActListener(EventKey.onPlacedBlock, x =>
            {
                if (config.PluginDisable.Futures.Statistics.Placed) return true;
                var e = BaseEvent.getFrom(x) as PlacedBlockEvent;
                if (e == null) return true;

                string name = e.playername;
                if (!string.IsNullOrEmpty(name))
                {
                    api.runcmd($"scoreboard players add @a[name={name},tag=!BOT] Placed 1");
                }

                return true;
            });

            /*
            api.addAfterActListener(EventKey.onUseItem, x =>
            {
                var e = BaseEvent.getFrom(x) as UseItemEvent;
                if (e == null) return true;

                string name = e.playername;
                if (!string.IsNullOrEmpty(name))
                {
                    api.runcmd($"scoreboard players add @a[name={name}] Used 1");
                }

                return true;
            });
            */

            api.addBeforeActListener(EventKey.onServerCmdOutput, x =>
            {
                var e = BaseEvent.getFrom(x) as ServerCmdOutputEvent;
                if (e == null) return true;

                string output = e.output;
                if (output.StartsWith("Day is "))
                {
                    GameDatas.GameDay = Regex.Replace(output, @"[^0-9]+", "");
                }
                else if (output.StartsWith("Daytime is"))
                {
                    GameDatas.GameTime = Regex.Replace(output, @"[^0-9]+", "");
                }
                else if (output.StartsWith("randomtickspeed = "))
                {
                    GameDatas.TickStatus = Regex.Replace(output, @"[^0-9]+", "");
                }
                else if (output.StartsWith("keepInventory = "))
                {
                    if (output.Contains("keepInventory = true"))
                    {
                        GameDatas.KiStatus = "已开启";
                    }
                    else if (output.Contains("keepInventory = false"))

                    {
                        GameDatas.KiStatus = "已关闭";
                    }
                }
                else if (output.StartsWith("mobGriefing = "))
                {
                    if (output.Contains("mobGriefing = true"))
                    {
                        GameDatas.MgStatus = "已开启";
                    }
                    else if (output.Contains("mobGriefing = false"))

                    {
                        GameDatas.MgStatus = "已关闭";
                    }
                }
                else if (output.Contains("entityCounter"))
                {
                    GameDatas.EntityCounter = Regex.Replace(output, @"[^0-9]+", "");
                }
                else if (output.Contains("itemCounter"))
                {
                    GameDatas.ItemCounter = Regex.Replace(output, @"[^0-9]+", "");
                }

                string[] blockWords =
                {
                    "Killed", "Dead", "Dig", "Placed", "Health", "_CounterCache", "Tasks", "OnlineMinutes",
                    "No targets matched selector", "game mode to Default"
                };
                foreach (var blockWord in blockWords)
                {
                    if (output.Contains(blockWord)) return false;
                }

                return true;
            });

            api.addBeforeActListener(EventKey.onInputCommand, x =>
            {
                var e = BaseEvent.getFrom(x) as InputCommandEvent;
                if (e == null) return true;

                string name = e.playername;
                string cmd = e.cmd;
                if (config.Logging.Command) LogsWriter(name, cmd);
                if (config.ConsoleOutput.Command) ConsoleOutputter(name, cmd);

                if (!config.AntiCheat.Enable) return true;
                foreach (var allowedCmd in config.AntiCheat.AllowedCmd)
                {
                    if (cmd.StartsWith(allowedCmd))
                    {
                        return true;
                    }
                }

                api.runcmd($"kick {name} 试图违规使用{cmd}被踢出");
                StandardizedFeedback("@a", $"{name}试图违规使用{cmd}被踢出");
                return false;
            });

            api.addBeforeActListener(EventKey.onServerCmd, x =>
            {
                var e = BaseEvent.getFrom(x) as ServerCmdEvent;
                if (e == null) return true;

                string cmd = e.cmd;

                if (cmd == "mcp setting reload")
                {
                    LoadPlugin();
                    if (config.Logging.Plugin) LogsWriter("MCP", "配置文件已重新载入。");
                    if (config.ConsoleOutput.Plugin) ConsoleOutputter("MCP", "配置文件已重新载入。");
                    return false;
                }

                if (cmd == "stop")
                {
                    foreach (var playerData in playerDatas)
                    {
                        if (playerData.Value.IsOnline)
                        {
                            playerDatas[playerData.Key].IsOnline = false;
                            api.runcmd($"kick {playerData.Value.Name}");
                        }
                    }

                    string savedPlayerDatas = javaScriptSerializer.Serialize(playerDatas);
                    File.WriteAllText(PluginPath.PlayerDatasPath, savedPlayerDatas);
                    return true;
                }

                return true;
            });

            api.addAfterActListener(EventKey.onLoadName, x =>
            {
                var e = BaseEvent.getFrom(x) as LoadNameEvent;
                if (e == null) return true;

                string name = e.playername;
                string uuid = e.uuid;
                string xuid = e.xuid;

                if (name.StartsWith("bot_"))
                {
                    if (config.Logging.PlayerOnlineOffline) LogsWriter(name, " 加入了服务器.");
                    if (config.ConsoleOutput.PlayerOnlineOffline) ConsoleOutputter(name, " 加入了服务器.");
                    api.runcmd($"tag {name} add BOT");
                    return true;
                }

                if (playerDatas.ContainsKey(name))
                {
                    playerDatas[name].Name = name;
                    playerDatas[name].IsOnline = true;
                    playerDatas[name].Uuid = uuid;
                    playerDatas[name].Xuid = xuid;
                }
                else
                {
                    playerDatas.Add(name, new PlayerDatas {Name = name, Uuid = uuid, Xuid = xuid, IsOnline = true});
                    if (config.Logging.Plugin) LogsWriter("MCP", $"新实例化用于存储{name}信息的PlayerDatas类");
                    if (config.ConsoleOutput.Plugin) ConsoleOutputter("MCP", $"新实例化用于存储{name}信息的PlayerDatas类");
                }

                if (!config.PluginDisable.Futures.OfflineMessage)
                {
                    Task.Run(async delegate
                    {
                        await Task.Delay(30000);
                        StandardizedFeedback(name, $"你有 §l{playerDatas[name].OfflineMessage.Count} §r条未读离线消息.");
                        foreach (var offlineMessage in playerDatas[name].OfflineMessage)
                        {
                            StandardizedFeedback(name, offlineMessage);
                        }

                        playerDatas[name].OfflineMessage.Clear();
                    });
                }


                if (!config.WhiteList.Enable)
                {
                    if (config.Logging.PlayerOnlineOffline) LogsWriter(name, " 加入了服务器.");
                    if (config.ConsoleOutput.PlayerOnlineOffline) ConsoleOutputter(name, " 加入了服务器.");
                    return true;
                }

                foreach (var player in config.WhiteList.PlayerList)
                {
                    if (player.Name == name && player.Xuid == xuid)
                    {
                        if (config.Logging.PlayerOnlineOffline) LogsWriter(name, " 加入了服务器.");
                        if (config.ConsoleOutput.PlayerOnlineOffline) ConsoleOutputter(name, " 加入了服务器.");
                        return true;
                    }
                }

                Task.Run(async delegate
                {
                    await Task.Delay(1000);
                    if (config.Logging.PlayerOnlineOffline) LogsWriter(name, " 尝试加入服务器.");
                    if (config.ConsoleOutput.PlayerOnlineOffline) ConsoleOutputter(name, " 尝试加入服务器.");
                    api.runcmd($"kick {name} 您未受邀加入该服务器，详情请咨询Hil。");
                    if (config.ConsoleOutput.Plugin) ConsoleOutputter("MCP", $"{name}未受邀加入该服务器，已自动踢出。");
                    if (config.Logging.Plugin) LogsWriter("MCP", $"{name}未受邀加入该服务器，已自动踢出。");
                });

                return true;
            });

            api.addAfterActListener(EventKey.onPlayerLeft, x =>
            {
                var e = BaseEvent.getFrom(x) as PlayerLeftEvent;
                if (e == null) return true;

                string name = e.playername;
                if (config.Logging.PlayerOnlineOffline) LogsWriter(name, " 离开了服务器.");
                if (config.ConsoleOutput.PlayerOnlineOffline) ConsoleOutputter(name, " 离开了服务器.");
                if(!name.StartsWith("bot_")) playerDatas[name].IsOnline = false;
                return true;
            });
        }
    }
}

namespace CSR
{
    partial class Plugin
    {
        public static void onStart(MCCSAPI api)
        {
            MCPromoter.MCPromoter.Init(api);
            Console.WriteLine($"[{PluginInfo.Name} - {PluginInfo.Version}]Loaded.");
        }
    }
}