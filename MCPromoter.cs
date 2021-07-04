using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSR;
using MCPromoter;

namespace MCPromoter
{
    class PlayerDatas
    {
        public string Name { get; set; }
        public string Uuid { get; set; }
        public bool IsSuicide { get; set; }
        public bool DeadEnable { get; set; }
        public string DeadX { get; set; }
        public string DeadY { get; set; }
        public string DeadZ { get; set; }
        public string DeadWorld { get; set; }

        public bool IsOnline { get; set; } = true;
    }

    public class GameDatas
    {
        public string GameDay { get; set; }
        public string GameTime { get; set; }
        public DateTime OpeningDate { get; } = DateTime.Parse("2021-6-14 12:00:00");
        public string TickStatus { get; set; }
        public string MgStatus { get; set; }
        public string KiStatus { get; set; }
        public string EntityCounter { get; set; }
        public string ItemCounter { get; set; }
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
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        
        public void IniWriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, _path);
        }
        
        public string IniReadValue(string section, string key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, "", temp, 255, _path);
            return temp.ToString();
        }
    } 
    
    public class MCPromoter
    {
        private static MCCSAPI _mapi;

        private static string worldName = "InnerWorld";

        private static readonly string[] helpTexts =
        {
            "§2========================",
            "@mcp [status|help]    获取MCP状态/帮助",
            "@mcp initialize      初始化MCP",
            "@= <表达式>    计算表达式并输出",
            "@back      返回死亡地点",
            "@ban <玩家名>     快速禁人(需特殊授权)",
            "@bot [spawn|kill|tp] <BOT名>   召唤/杀死/传送bot",
            "@bot list  列出服务器内存在的bot",
            "@day [game|server]     查询游戏内/开服天数",
            "@entity [count|list]   统计/列出服务器内实体",
            "@here      全服报点",
            "@item      [clear|count|pick]      清除/统计/拾取服务器内掉落物",
            "@ki [true|false|status]   开启/关闭/查询死亡不掉落",
            "@kill      自杀(不计入死亡榜)",
            "@mg [true|false]   开启/关闭生物破坏",
            //"@qb [make|back|restart]    快速备份/还原/重启服务器",
            //"@qb time   查询上次备份时间",
            "@sh <指令>   向控制台注入指令(需特殊授权)",
            "@size      获取存档大小",
            "@sta <计分板名>    将侧边栏显示调整为特定计分板",
            "@sta null      关闭侧边栏显示",
            "@task [add|remove] <任务名>   添加/移除指定任务",
            "@tick [倍数|status]      设置/查询随机刻倍数",
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
        
        public static long GetWorldSize(String path){
            DirectoryInfo directoryInfo=new DirectoryInfo(path);
            long length=0;
            foreach( FileSystemInfo fsi in directoryInfo.GetFileSystemInfos() ) {
                if ( fsi is FileInfo ) {
                    length += ((FileInfo)fsi).Length;
                }
                else {
                    length +=GetWorldSize(fsi.FullName);
                }
            }
            return length;
        }

        public static void LoadConf()
        {
            
        }
        
        public static void Init(MCCSAPI api, (string Name, string Version, string Author) pluginInfo)
        {
            Dictionary<string, PlayerDatas> playerDatas = new Dictionary<string, PlayerDatas>();
            GameDatas gameDatas = new GameDatas();
            
            _mapi = api;
            string opPlayer = "XianYuHil";
            
            LoadConf();

            api.addAfterActListener(EventKey.onInputText, x =>
            {
                var e = BaseEvent.getFrom(x) as InputTextEvent;
                if (e == null) return true;
                string name = e.playername;
                string msg = e.msg;
                var position = (x: ((int) e.XYZ.x).ToString(), y: ((int) e.XYZ.y).ToString(),
                    z: ((int) e.XYZ.z).ToString(), world: e.dimension);
                
                if (msg.ToCharArray()[0] == '@')
                {
                    api.logout($"[MCP]<{name}>{msg}");
                    string[] argsList = msg.Split(' ');
                    switch (argsList[0])
                    {
                        case "@mcp":
                            if (argsList.Length > 1)
                            {
                                switch (argsList[1])
                                {
                                    case "status":
                                        StandardizedFeedback(name, "§2========================");
                                        StandardizedFeedback(name, $"§c§l{pluginInfo.Name} - {pluginInfo.Version}");
                                        StandardizedFeedback(name, $"§o作者：{pluginInfo.Author}");
                                        StandardizedFeedback(name, "§2========================");
                                        StandardizedFeedback(name, "@mcp help      获取MCP模块帮助");
                                        StandardizedFeedback(name, "@mcp initialize        初始化MCP模块");
                                        StandardizedFeedback(name, "@mcp setting [option] [value]       修改MCP模块设置");
                                        StandardizedFeedback(name, "§2========================");
                                        break;
                                    case "help":
                                        foreach (var helpText in helpTexts)
                                        {
                                            StandardizedFeedback(name, helpText);
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
                                        break;
                                    default:
                                        StandardizedFeedback(name, "无效的@mcp指令，请使用@MCP status获取帮助");
                                        break;
                                }
                            }
                            else
                            {
                                StandardizedFeedback(name, "无效的@mcp指令，请使用@MCP status获取帮助");
                            }
                            break;
                        case "@=":
                            string expression = msg.Remove(0, 3);
                            string result = new DataTable().Compute(expression, "").ToString();
                            StandardizedFeedback(name, result);
                            break;
                        case "@here":
                            api.runcmd("playsound random.levelup @a");
                            StandardizedFeedback("@a",
                                $"§e§l{name}§r在{position.world}§e§l[{position.x},{position.y},{position.z}]§r向大家打招呼！");
                            break;
                        case "@back":
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
                        case "@bot":
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
                        case "@ban":
                            string bannedName = argsList[1];
                            if (name == opPlayer)
                            {
                                api.runcmd($"kick {bannedName} {bannedName}，您已被{name}永久封禁。");
                                api.runcmd($"whitelist remove {bannedName}");
                                StandardizedFeedback("@a", $"{bannedName}已被{name}永久封禁。");
                            }
                            else
                            {
                                api.runcmd($"kick {name} 您试图越权使用@ban指令封禁{bannedName}，自动踢出");
                                StandardizedFeedback("@a",$"{name}试图越权使用@ban指令封禁{bannedName}，自动踢出");
                            }
                            break;
                        case "@day":
                            if (argsList[1] == "game")
                            {
                                api.runcmd("time query day");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    string gameDay = gameDatas.GameDay;
                                    StandardizedFeedback(name, $"现在是游戏内的第{gameDay}天.");
                                });
                            }
                            else if (argsList[1] == "server")
                            {
                                DateTime nowDate = DateTime.Now;
                                string serverDay =
                                    ((int) nowDate.Subtract(gameDatas.OpeningDate).TotalDays).ToString();
                                StandardizedFeedback(name, $"今天是开服的第{serverDay}天.");
                            }
                            break;
                        case "@item":
                            if (argsList[1] == "pick")
                            {
                                api.runcmd($"tp @e[type=item] {name}");
                                StandardizedFeedback(name, "您已拾取所有掉落物");
                            }
                            else if (argsList[1] == "clear")
                            {
                                api.runcmd("kill @e[type=item]");
                                StandardizedFeedback(name, "您已清除所有掉落物");
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
                                    string itemCount = gameDatas.ItemCounter;
                                    StandardizedFeedback(name, $"当前的掉落物数为{itemCount}");
                                });
                            }

                            break;
                        case "@entity":
                            if (argsList[1] == "count")
                            {
                                api.runcmd("scoreboard players set @e _CounterCache 1");
                                api.runcmd("scoreboard players set \"entityCounter\" Counter 0");
                                api.runcmd("scoreboard players operation \"entityCounter\" Counter+= @e _CounterCache");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    string entityCount = gameDatas.EntityCounter;
                                    StandardizedFeedback(name, $"当前的实体数为{entityCount}");
                                });
                            }
                            else if (argsList[1] == "list")
                            {
                                api.runcmd("say §r§o§9服务器内的实体列表为§r§l§f @e");
                            }

                            break;
                        case "@ki":
                            if (argsList[1] == "status")
                            {
                                api.runcmd("gamerule keepinventory");

                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    StandardizedFeedback(name,$"当前死亡不掉落{gameDatas.KiStatus}");
                                });
                            }
                            else if(argsList[1]=="true")
                            {
                                api.runcmd("gamerule keepinventory true");
                                StandardizedFeedback(name, "死亡不掉落已开启");
                            }
                            else if(argsList[1]=="false")
                            {
                                api.runcmd("gamerule keepinventory false");
                                StandardizedFeedback(name, "死亡不掉落已关闭");
                            }
                            break;
                        case "@kill":
                            string[] suicideMsg = {
                                
                            };
                            playerDatas[name].IsSuicide = true;
                            api.runcmd($"kill {name}");
                            Random suicideMsgNum = new Random();
                            StandardizedFeedback("@a",suicideMsg[suicideMsgNum.Next(0, 17)]);
                            break;
                        case "@mg":
                            if (argsList[1] == "status")
                            {
                                api.runcmd("gamerule mobGriefing");

                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    StandardizedFeedback(name,$"当前生物破坏{gameDatas.MgStatus}");
                                });
                            }
                            else if(argsList[1]=="true")
                            {
                                api.runcmd("gamerule mobGriefing true");
                                StandardizedFeedback(name, "生物破坏已开启");
                            }
                            else if(argsList[1]=="false")
                            {
                                api.runcmd("gamerule mobGriefing false");
                                StandardizedFeedback(name, "生物破坏已关闭");
                            }
                            break;
                        case "@rc":
                            if (name == opPlayer)
                            {
                                string command = msg.Remove(0, 4);
                                bool cmdResult = api.runcmd(command);
                                StandardizedFeedback(name, cmdResult ? $"已成功向控制台注入了{command}" : $"{command}运行失败");
                            }
                            break;
                        case "@size":
                            string worldSize = ((int)(GetWorldSize($@"worlds\{worldName}")/1024/1024)).ToString();
                            StandardizedFeedback(name,$"当前服务器的存档大小是§l§6{worldSize}§7MB");
                            break;
                        case "@sta":
                            string statisName = argsList[1];
                            Dictionary<string,string> cnStatisName = new Dictionary<string, string>
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
                                StandardizedFeedback(name,
                                    cnStatisName.ContainsKey(statisName)
                                        ? $"已将侧边栏显示修改为{cnStatisName[statisName]}"
                                        : $"已将侧边栏显示修改为{statisName}");
                            }
                            else
                            {
                                api.runcmd("scoreboard objectives setdisplay sidebar");
                                StandardizedFeedback(name, "已关闭侧边栏显示");
                            }
                            break;
                        case "@task":
                            string taskName = argsList[2];
                            if (argsList[1] == "add")
                            {
                                api.runcmd($"scoreboard players set {taskName} Tasks 1");
                                StandardizedFeedback(name, $"已向待办事项板添加§l{taskName}");
                            }
                            else if (argsList[1] == "remove")
                            {
                                api.runcmd($"scoreboard players reset {taskName} Tasks");
                                StandardizedFeedback(name, $"已将§l{taskName}§r从待办事项板上移除");
                            }
                            break;
                        case "@tick":
                            if (argsList[1] == "status")
                            {
                                api.runcmd("gamerule randomtickspeed");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    StandardizedFeedback(name, $"现在的游戏随机刻为{gameDatas.TickStatus}");
                                });
                            }
                            else if (int.TryParse(argsList[1],out int tickSpeed))
                            {
                                api.runcmd($"gamerule randomtickspeed {tickSpeed}");
                                StandardizedFeedback(name, $"已将游戏内随机刻修改为{tickSpeed}。");
                            }
                            break;
                        case "@quicksleep":
                            
                            break;
                        default:
                            StandardizedFeedback(name, "无效的MCP指令，请输入@mcp help获取帮助");
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
                    var deadPosition = (x: ((int) e.XYZ.x).ToString(), y: ((int) e.XYZ.y).ToString(),
                        z: ((int) e.XYZ.z).ToString(), world: e.dimension);
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
                    gameDatas.GameDay = Regex.Replace(output, @"[^0-9]+", "");
                }
                else if (output.StartsWith("Daytime is"))
                {
                    gameDatas.GameTime = Regex.Replace(output, @"[^0-9]+", "");
                }
                else if (output.StartsWith("randomtickspeed = "))
                {
                    gameDatas.TickStatus = Regex.Replace(output, @"[^0-9]+", "");
                }
                else if (output.StartsWith("keepinventory = "))
                {
                    if (output.Contains("keepinventory = true"))
                    {
                        gameDatas.KiStatus = "已开启";
                    }
                    else if (output.Contains("keepinventory = false"))

                    {
                        gameDatas.KiStatus = "已关闭";
                    }
                }
                else if (output.StartsWith("mobGriefing = "))
                {
                    if (output.Contains("mobGriefing = true"))
                    {
                        gameDatas.MgStatus = "已开启";
                    }
                    else if (output.Contains("mobGriefing = false"))

                    {
                        gameDatas.MgStatus = "已关闭";
                    }
                }
                else if (output.Contains("entityCounter"))
                {
                    gameDatas.EntityCounter = Regex.Replace(output, @"[^0-9]+", "");
                }
                else if (output.Contains("itemCounter"))
                {
                    gameDatas.ItemCounter = Regex.Replace(output, @"[^0-9]+", "");
                }

                string[] blockWords = {"Killed", "Dead", "Dig", "Placed", "Used", "Health", "_CounterCache"};
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
                string[] allowedCmds = {"/?", "/help", "/list", "/me", "/mixer", "/msg", "/tell", "/w", "/tickingarea", "/tp"};
                foreach (var allowedCmd in allowedCmds)
                {
                    if (cmd.Contains(allowedCmd))
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

                if (playerDatas.ContainsKey(name))
                {
                    playerDatas[name].IsOnline = true;
                }
                else
                {
                    playerDatas.Add(name, new PlayerDatas {Name = name, Uuid = uuid});
                    api.logout($"[MCP]新实例化用于存储{name}信息的PlayerDatas类");
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
            var pluginInfo = (Name: "MinecraftPromoter", Version: "V1.1.1", Author: "XianYu_Hil");
            MCPromoter.MCPromoter.Init(api, pluginInfo);
            Console.WriteLine($"[{pluginInfo.Name} - {pluginInfo.Version}]Loaded.");
        }
    }
}