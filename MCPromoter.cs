using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSR;
using MCPromoter;

namespace MCPromoter
{
    public static class PluginInfo
    {
        public static string Name => "MinecraftPromoter";
        public static string Version => "V1.7.3";
        public static string Author => "XianYu_Hil";
    }

    public static class GameDatas
    {
        public static string GameDay { get; set; }
        public static string GameTime { get; set; }
        public static DateTime OpeningDate { get; set; }
        public static string TickStatus { get; set; }
        public static string MgStatus { get; set; }
        public static string KiStatus { get; set; }
        public static string EntityCounter { get; set; }
        public static string ItemCounter { get; set; }

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
            return cpuCounter.NextValue().ToString("f2")+"%";
        }
    }


    class PlayerDatas
    {
        public string Name { get; set; }
        public string Uuid { get; set; }
        public string Xuid { get; set; }
        public bool IsSuicide { get; set; }
        public bool DeadEnable { get; set; }
        public string DeadX { get; set; }
        public string DeadY { get; set; }
        public string DeadZ { get; set; }
        public string DeadWorld { get; set; }

        public bool IsOnline { get; set; } = true;
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

    public class MCPromoter
    {
        private static MCCSAPI _mapi;

        private static string[] whitelistNames;
        private static string[] whitelistXuids;
        private static string[] adminNames;
        private static string[] adminCmds;
        private static string[] suicideMsgs;
        private static string[] allowedCmds;
        private static bool antiCheat;
        private static string worldName;
        private static string prefix;

        private static readonly string[] HelpTexts =
        {
            "§2========================",
            "= <表达式>    计算表达式并输出",
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
            "qs     发起快速跳过夜晚投票",
            "qs [accept|refuse]     同意/拒绝快速跳过夜晚",
            //"@qb [make|back|restart]    快速备份/还原/重启服务器",
            //"@qb time   查询上次备份时间",
            "sh <指令>   向控制台注入指令(需特殊授权)",
            "size      获取存档大小",
            "sta <计分板名>    将侧边栏显示调整为特定计分板",
            "sta null      关闭侧边栏显示",
            "system [cpu|memory]    查询服务器CPU/内存占用率",
            "task [add|remove] <任务名>   添加/移除指定任务",
            "tick [倍数|status]      设置/查询随机刻倍数",
            "whitelist [add|remove] <玩家名>     将玩家加入/移出白名单",
            "§2========================"
        };

        public static void StandardizedFeedback(string targetName, string content)
        {
            _mapi.runcmd($"tellraw {targetName} {{\"rawtext\":[{{\"text\":\"{content}\"}}]}}");
        }

        public static void RemoveBot(string botName)
        {
            botName = botName.Remove(0, 4);
            _mapi.runcmd($"tickingarea remove loader_{botName}");
            StandardizedFeedback("@a", $"§ebot_{botName} 退出了游戏");
        }

        public static string FormatSize(double size)
        {
            double d = (double)size;
            int i = 0;
            while ((d > 1024) && (i < 5))
            {
                d /= 1024;
                i++;
            }
            string[] unit = { "B", "KB", "MB", "GB", "TB" };
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
                    length += ((FileInfo)fsi).Length;
                }
                else
                {
                    length += GetWorldSize(fsi.FullName);
                }
            }

            return length;
        }

        public static void Initialize()
        {
            IniFile iniFile = new IniFile(@"CSR\MCP\config.ini");
            if (!Directory.Exists(@"CSR\MCP"))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(@"CSR\MCP");
                directoryInfo.Create();
            }

            FileStream fileStream = new FileStream(@"CSR\MCP\config.ini", FileMode.Create);
            fileStream.Close();
            iniFile.IniWriteValue("Config", "WorldName", "");
            iniFile.IniWriteValue("Config", "AntiCheat", "true");
            iniFile.IniWriteValue("Config", "ServerStartDate", DateTime.Now.ToString());
            iniFile.IniWriteValue("WhiteList", "PlayerNames", "");
            iniFile.IniWriteValue("WhiteList", "PlayerXuids", "");
            iniFile.IniWriteValue("WhiteList", "AdminNames", "");
            iniFile.IniWriteValue("WhiteList", "AdminCmds", "rc;whitelist;mcp setting");
            iniFile.IniWriteValue("WhiteList", "AllowedCmds", "/help");
            iniFile.IniWriteValue("Customization", "SuicideMsgs", "");
            iniFile.IniWriteValue("Customization", "Prefix", "@");

            _mapi.logout(@"[MCP]已完成插件配置文件的初始化。配置文件位于CSR\MCP\config.ini");
            LoadConf();
        }

        public static void LoadConf()
        {
            IniFile iniFile = new IniFile(@"CSR\MCP\config.ini");
            if (File.Exists(@"CSR\MCP\config.ini"))
            {
                worldName = iniFile.IniReadValue("Config", "WorldName");
                antiCheat = Boolean.Parse(iniFile.IniReadValue("Config", "AntiCheat"));

                string _whitelistNames = iniFile.IniReadValue("WhiteList", "PlayerNames");
                whitelistNames = _whitelistNames.Split(';');
                string _whitelistXuids = iniFile.IniReadValue("WhiteList", "PlayerXuids");
                whitelistXuids = _whitelistXuids.Split(';');
                string _adminNames = iniFile.IniReadValue("WhiteList", "AdminNames");
                adminNames = _adminNames.Split(';');
                string _adminCmds = iniFile.IniReadValue("WhiteList", "AdminCmds");
                adminCmds = _adminCmds.Split(';');
                string _allowedCmds = iniFile.IniReadValue("WhiteList", "AllowedCmds");
                allowedCmds = _allowedCmds.Split(';');

                string _suicideMsgs = iniFile.IniReadValue("Customization", "SuicideMsgs");
                suicideMsgs = _suicideMsgs.Split(';');

                prefix = iniFile.IniReadValue("Customization", "Prefix");
                GameDatas.OpeningDate = DateTime.Parse(iniFile.IniReadValue("Config", "ServerStartDate"));
                _mapi.logout("[MCP]已载入配置文件。");
            }
            else
            {
                Initialize();
            }
        }

        public static void Init(MCCSAPI api)
        {
            Dictionary<string, PlayerDatas> playerDatas = new Dictionary<string, PlayerDatas>();

            ArrayList onlinePlayer = new ArrayList();
            ArrayList acceptPlayer = new ArrayList();
            bool IsQuickSleep = false;
            string QuickSleepName = "";

            _mapi = api;

            IniFile iniFile = new IniFile(@"CSR\MCP\config.ini");
            LoadConf();

            api.addAfterActListener(EventKey.onInputText, x =>
            {
                var e = BaseEvent.getFrom(x) as InputTextEvent;
                if (e == null) return true;
                string name = e.playername;
                string msg = e.msg;
                var position = (x: ((int)e.XYZ.x).ToString(), y: ((int)e.XYZ.y).ToString(),
                    z: ((int)e.XYZ.z).ToString(), world: e.dimension);

                if (msg.StartsWith(prefix))
                {
                    api.logout($"[MCP]<{name}>{msg}");
                    string[] argsList = msg.Split(' ');
                    argsList[0] = argsList[0].Replace(prefix, "");
                    foreach (var adminCmd in adminCmds)
                    {
                        if (msg.Replace(prefix, "").StartsWith(adminCmd) && !adminNames.Contains(name))
                        {
                            StandardizedFeedback(name, $"{msg}需要admin权限才可使用，您当前无权使用该指令。");
                            return true;
                        }
                    }

                    switch (argsList[0])
                    {
                        case "mcp":
                            if (argsList.Length > 1)
                            {
                                switch (argsList[1])
                                {
                                    case "status":
                                        StandardizedFeedback(name, "§2========================");
                                        StandardizedFeedback(name, $"§c§l{PluginInfo.Name} - {PluginInfo.Version}");
                                        StandardizedFeedback(name, $"§o作者：{PluginInfo.Author}");
                                        StandardizedFeedback(name, "§2========================");
                                        StandardizedFeedback(name, $"{prefix}mcp help      获取MCP模块帮助");
                                        StandardizedFeedback(name, $"{prefix}mcp initialize        初始化MCP模块");
                                        StandardizedFeedback(name,
                                            $"{prefix}mcp setting [option] [value]       修改MCP模块设置");
                                        StandardizedFeedback(name, $"{prefix}mcp setting reload      重载MCP配置文件");
                                        StandardizedFeedback(name, "§2========================");
                                        break;
                                    case "help":
                                        foreach (var helpText in HelpTexts)
                                        {
                                            StandardizedFeedback(name, prefix + helpText);
                                        }

                                        break;
                                    case "initialize":
                                        api.runcmd("scoreboard objectives add Killed dummy §l§7击杀榜");
                                        api.runcmd("scoreboard objectives add Dig dummy §l§7挖掘榜");
                                        api.runcmd("scoreboard objectives add Dead dummy §l§7死亡榜");
                                        api.runcmd("scoreboard objectives add Placed dummy §l§7放置榜");
                                        // api.runcmd("scoreboard objectives add Attack dummy §l§7伤害榜");
                                        // api.runcmd("scoreboard objectives add Hurt dummy §l§7承伤榜");
                                        api.runcmd("scoreboard objectives add Used dummy §l§7使用榜");
                                        api.runcmd("scoreboard objectives add Tasks dummy §l§e服务器摸鱼指南");
                                        api.runcmd("scoreboard objectives add _CounterCache dummy");
                                        api.runcmd("scoreboard objectives add Counter dummy");
                                        break;
                                    case "setting":
                                        switch (argsList[2])
                                        {
                                            case "prefix":
                                                string newPrefix = argsList[3];
                                                iniFile.IniWriteValue("Customization", "Prefix", newPrefix);
                                                StandardizedFeedback("@a", $"MCP指令前缀已被{name}从{prefix}修改为{newPrefix}");
                                                StandardizedFeedback(name,
                                                    $"已将MCP指令前缀修改为{newPrefix},请使用{prefix}mcp setting reload重载配置文件以生效");
                                                break;
                                            case "anticheat":
                                                string newAnticheat = argsList[3];
                                                if (newAnticheat == "true" || newAnticheat == "false")
                                                {
                                                    iniFile.IniWriteValue("Config", "AntiCheat", newAnticheat);
                                                    StandardizedFeedback("@a",
                                                        newAnticheat == "true" ? $"{name}已开启反作弊系统" : $"{name}已关闭反作弊系统");
                                                    StandardizedFeedback(name,
                                                        $"已将反作弊系统状态调整为{newAnticheat},请使用{prefix}mcp setting reload重载配置文件以生效");
                                                }
                                                else
                                                {
                                                    StandardizedFeedback(name, "仅允许使用true或false来设置反作弊系统状态");
                                                }

                                                break;
                                            case "reload":
                                                LoadConf();
                                                StandardizedFeedback("@a", "[MCP]配置文件已重新载入。");
                                                break;
                                        }

                                        break;
                                    default:
                                        StandardizedFeedback(name, $"无效的{prefix}mcp指令，请使用{prefix}mcp status获取帮助");
                                        break;
                                }
                            }
                            else
                            {
                                StandardizedFeedback(name, $"无效的{prefix}mcp指令，请使用{prefix}mcp status获取帮助");
                            }

                            break;
                        case "=":
                            string expression = msg.Replace($"{prefix}= ", "");
                            string result = new DataTable().Compute(expression, "").ToString();
                            StandardizedFeedback("@a",$"{expression} = {result}");
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
                            if (argsList[1] == "list")
                            {
                                api.runcmd("say 服务器内存在机器人@e[tag=BOT]");
                            }
                            else
                            {
                                string botName = argsList[2];
                                if (argsList[1] == "spawn")
                                {
                                    api.runcmd($"execute {name} ~~~ summon minecraft:player bot_{botName}");
                                    api.runcmd($"tag @e[name=bot_{botName}] add BOT");
                                    api.runcmd($"execute {name} ~~~ tickingarea add circle ~~~ 4 loader_{botName}");
                                    StandardizedFeedback("@a", $"§ebot_{botName} 加入了游戏");
                                }
                                else if (argsList[1] == "kill")
                                {
                                    api.runcmd($"kill @e[name=bot_{botName}]");
                                }
                                else if (argsList[1] == "tp")
                                {
                                    api.runcmd($"tp {name} @e[name=bot_{botName}");
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
                                string serverDay =
                                    ((int)nowDate.Subtract(GameDatas.OpeningDate).TotalDays).ToString();
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
                            for (int i = 0; i < suicideMsgs.Length; i++)
                            {
                                suicideMsgs[i] = suicideMsgs[i].Replace("{}", $"§l{name}§r");
                            }

                            int suicideMsgNum = new Random().Next(0, suicideMsgs.Length);
                            StandardizedFeedback("@a", suicideMsgs[suicideMsgNum]);
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
                        case "rc":
                            string command = msg.Replace($"{prefix}rc ", "");
                            bool cmdResult = api.runcmd(command);
                            StandardizedFeedback("@a", cmdResult ? $"已成功向控制台注入了{command}" : $"{command}运行失败");

                            break;
                        case "size":
                            //string worldSize = ((int) (GetWorldSize($@"worlds\{worldName}") / 1024 / 1024)).ToString();
                            string worldSize = FormatSize(GetWorldSize($@"worlds\{worldName}"));
                            StandardizedFeedback("@a", $"当前服务器的存档大小是§l§6{worldSize}");
                            break;
                        case "sta":
                            string statisName = argsList[1];
                            Dictionary<string, string> cnStatisName = new Dictionary<string, string>
                            {
                                {"Dig", "挖掘榜"},
                                {"Placed", "放置榜"},
                                {"Killed", "击杀榜"},
                                {"Tasks", "待办事项榜"},
                                {"Dead", "死亡榜"},
                                {"Used", "使用榜"}
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
                            string newName = argsList[2];
                            if (argsList[1] == "add")
                            {
                                if (whitelistNames.Contains(newName))
                                {
                                    StandardizedFeedback(name, $"白名单中已存在玩家{newName}");
                                }
                                else
                                {
                                    if (playerDatas.ContainsKey(newName))
                                    {
                                        string _whitelistName = string.Join(";", whitelistNames);
                                        _whitelistName = _whitelistName + ";" + newName;
                                        string _whitelistXuid = string.Join(";", whitelistXuids);
                                        _whitelistXuid = _whitelistXuid + ";" + playerDatas[newName].Xuid;
                                        iniFile.IniWriteValue("WhiteList", "PlayerNames", _whitelistName);
                                        iniFile.IniWriteValue("WhiteList", "PlayerXuids", _whitelistXuid);
                                        StandardizedFeedback("@a", $"{name}已将{newName}加入白名单。");
                                        StandardizedFeedback(name,
                                            $"已将{newName}加入白名单，请使用{prefix}mcp setting reload重载配置文件以生效");
                                    }
                                    else
                                    {
                                        StandardizedFeedback(name, $"{newName}未曾尝试加入过服务器，请让其尝试加入服务器后再添加白名单");
                                    }
                                }
                            }
                            else if (argsList[1] == "remove")
                            {
                                if (whitelistNames.Contains(newName))
                                {
                                    if (playerDatas.ContainsKey(newName))
                                    {
                                        string _whitelistName = string.Join(";", whitelistNames);
                                        _whitelistName = _whitelistName.Replace(";" + newName, "");
                                        string _whitelistXuid = string.Join(";", whitelistXuids);
                                        _whitelistXuid =
                                            _whitelistXuid.Replace(";" + playerDatas[newName].Xuid, "");
                                        iniFile.IniWriteValue("WhiteList", "PlayerNames", _whitelistName);
                                        iniFile.IniWriteValue("WhiteList", "PlayerXuids", _whitelistXuid);
                                        api.runcmd($"kick {newName} 您已被{name}永久封禁。");
                                        StandardizedFeedback("@a", $"{newName}已被{name}永久封禁。");
                                        StandardizedFeedback(name,
                                            $"已将{newName}移出白名单，请使用{prefix}mcp setting reload重载配置文件以生效");
                                    }
                                    else
                                    {
                                        StandardizedFeedback(name, $"{newName}未曾加入过服务器，请让其加入服务器后再移除白名单");
                                    }
                                }
                                else
                                {
                                    StandardizedFeedback(name, $"白名单中不存在玩家{newName}");
                                }
                            }

                            break;
                        case "qs":
                            if (argsList.Length == 1)
                            {
                                if (IsQuickSleep == true)
                                {
                                    StandardizedFeedback("@a", $"现在游戏内已存在一个由{QuickSleepName}发起的快速跳过夜晚投票");
                                    StandardizedFeedback("@a", $"请使用{prefix}qs accept投出支持票，使用{prefix}qs refuse投出反对票");
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
                                        IsQuickSleep = true;
                                        QuickSleepName = name;
                                        acceptPlayer.Clear();
                                        acceptPlayer.Add(QuickSleepName);
                                        StandardizedFeedback("@a", $"§6§l{QuickSleepName}发起快速跳过夜晚投票，请使用{prefix}qs accept投出支持票，使用{prefix}qs refuse投出反对票。");
                                        StandardizedFeedback("@a", $"§6§l当前已有{acceptPlayer.Count}人投出支持票，至少需要{onlinePlayer.Count}人");
                                        StandardizedFeedback("@a", "§6§l投票将在18秒后结束。");
                                        _ = Task.Run(async delegate
                                            {
                                                await Task.Delay(18000);
                                                if (acceptPlayer.Count >= onlinePlayer.Count)
                                                {
                                                    StandardizedFeedback("@a", "§5§l深夜，一阵突如其来的反常疲惫侵袭了你的大脑，你失去意识倒在地上。当你醒来时，太阳正从东方冉冉升起。");
                                                    api.runcmd("time set sunrise");
                                                    api.runcmd("effect @a instant_damage 1 1 true");
                                                    api.runcmd("effect @a blindness 8 1 true");
                                                    api.runcmd("effect @a nausea 8 1 true");
                                                    api.runcmd("effect @a hunger 8 5 true");
                                                    api.runcmd("effect @a slowness 8 5 true");
                                                }
                                                else
                                                {
                                                    StandardizedFeedback("@a", $"§4§l{QuickSleepName}忽然之间厌倦了夜晚。");
                                                    api.runcmd($"effect {QuickSleepName} instant_damage 1 1 true");
                                                    api.runcmd($"effect {QuickSleepName} blindness 8 1 true");
                                                    api.runcmd($"effect {QuickSleepName} nausea 8 1 true");
                                                    api.runcmd($"effect {QuickSleepName} hunger 8 5 true");
                                                    api.runcmd($"effect {QuickSleepName} slowness 8 5 true");
                                                }
                                                IsQuickSleep = false;
                                                QuickSleepName = "";
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
                                if (IsQuickSleep == true)
                                {
                                    if (!acceptPlayer.Contains(name))
                                    {
                                        acceptPlayer.Add(name);
                                        StandardizedFeedback("@a", $"§a{name}向{QuickSleepName}发起的快速跳过夜晚投票投出支持票.已有{acceptPlayer.Count}人投出支持票，至少需要{onlinePlayer.Count}人。");
                                    }
                                    else
                                    {
                                        StandardizedFeedback(name, "请勿重复投票");
                                    }
                                }
                                else
                                {
                                    StandardizedFeedback(name, $"当前暂无快速跳过夜晚的投票，你可通过{prefix}qs进行发起");
                                }
                            }
                            else if (argsList[1] == "refuse")
                            {
                                if (IsQuickSleep == true)
                                {
                                    if (acceptPlayer.Contains(name))
                                    {
                                        acceptPlayer.Remove(name);
                                    }
                                    StandardizedFeedback("@a", $"§c{name}向{QuickSleepName}发起的快速跳过夜晚投票投出拒绝票.");
                                }
                                else
                                {
                                    StandardizedFeedback(name, $"当前暂无快速跳过夜晚的投票，你可通过{prefix}qs进行发起");
                                }
                            }
                            break;
                        default:
                            StandardizedFeedback(name, $"无效的MCP指令，请输入{prefix}mcp help获取帮助");
                            break;
                    }
                }

                return true;
            });

            api.addAfterActListener(EventKey.onMobDie, x =>
            {
                var e = BaseEvent.getFrom(x) as MobDieEvent;
                if (e == null) return true;

                string attackName = e.srcname;
                string attackType = e.srctype;
                string deadName = e.mobname;
                string deadType = e.mobtype;
                if (attackType == "entity.player.name")
                {
                    api.runcmd($"scoreboard players add @a[name={attackName}] Killed 1");
                }

                if (deadType == "entity.player.name")
                {
                    var deadPosition = (x: ((int)e.XYZ.x).ToString(), y: ((int)e.XYZ.y).ToString(),
                        z: ((int)e.XYZ.z).ToString(), world: e.dimension);
                    StandardizedFeedback("@a",
                        $"§r§l§f{deadName}§r§o§4 死于 §r§l§f{deadPosition.world}[{deadPosition.x},{deadPosition.y},{deadPosition.z}]");
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
                        api.runcmd($"scoreboard players add @a[tag=!BOT,name={deadName}] Dead 1");
                    }
                }

                if (deadName.StartsWith("bot_")) RemoveBot(deadName);
                return true;
            });

            api.addAfterActListener(EventKey.onDestroyBlock, x =>
            {
                var e = BaseEvent.getFrom(x) as DestroyBlockEvent;
                if (e == null) return true;

                string name = e.playername;
                if (!string.IsNullOrEmpty(name))
                {
                    api.runcmd($"scoreboard players add @a[name={name}] Dig 1");
                }

                return true;
            });

            api.addAfterActListener(EventKey.onPlacedBlock, x =>
            {
                var e = BaseEvent.getFrom(x) as PlacedBlockEvent;
                if (e == null) return true;

                string name = e.playername;
                if (!string.IsNullOrEmpty(name))
                {
                    api.runcmd($"scoreboard players add @a[name={name}] Placed 1");
                }

                return true;
            });

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

                string[] blockWords = { "Killed", "Dead", "Dig", "Placed", "Used", "Health", "_CounterCache" };
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
                api.logout($"[MCP]<{name}>{cmd}");
                if (antiCheat == false) return true;
                foreach (var allowedCmd in allowedCmds)
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

            api.addAfterActListener(EventKey.onLoadName, x =>
            {
                var e = BaseEvent.getFrom(x) as LoadNameEvent;
                if (e == null) return true;

                string name = e.playername;
                string uuid = e.uuid;
                string xuid = e.xuid;

                if (playerDatas.ContainsKey(name))
                {
                    playerDatas[name].IsOnline = true;
                }
                else
                {
                    playerDatas.Add(name, new PlayerDatas { Name = name, Uuid = uuid, Xuid = xuid });
                    api.logout($"[MCP]新实例化用于存储{name}信息的PlayerDatas类");
                }

                if (!(whitelistNames.Contains(name) && whitelistXuids.Contains(xuid)))
                {
                    Task.Run(async delegate
                    {
                        await Task.Delay(1000);
                        api.runcmd($"kick {name} 您未受邀加入该服务器，详情请咨询Hil。");
                        api.logout($"[MCP]{name}未受邀加入该服务器，已自动踢出。");
                    });
                }

                return true;
            });

            api.addAfterActListener(EventKey.onPlayerLeft, x =>
            {
                var e = BaseEvent.getFrom(x) as PlayerLeftEvent;
                if (e == null) return true;

                string name = e.playername;
                playerDatas[name].IsOnline = false;
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