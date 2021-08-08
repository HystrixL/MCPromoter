using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Timers;
using CSR;
using YamlDotNet.Serialization;
using static MCPromoter.Output;
using static MCPromoter.MCPromoter;

namespace MCPromoter
{
    public class CommandLibrary
    {
        public static bool cmdBack(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 1) return false;
            string name = e.playername;
            if (playerDatas[name].DeadEnable)
            {
                if (playerDatas[name].DeadWorld == e.dimension)
                {
                    Api.runcmd(
                        $"tp {name} {playerDatas[name].DeadPos.x} {playerDatas[name].DeadPos.y} {playerDatas[name].DeadPos.z}");
                }
                else
                {
                    StandardizedFeedback(name,
                        $"您的死亡点在{playerDatas[name].DeadWorld}，但是您现在在{e.dimension}。暂不支持跨世界传送。");
                }
            }
            else
            {
                StandardizedFeedback(name, "暂无死亡记录");
            }

            return true;
        }

        public static bool cmdBot(string[] args, InputTextEvent e, MCCSAPI api)
        {
            string name = e.playername;
            if (Configs.PluginDisable.Futures.FakePlayer)
            {
                StandardizedFeedback(name, "假人已被管理员禁用,当前无法使用.");
                return true;
            }

            if(args.Length<2) return false;
            if (args[1] == "list")
            {
                if (args.Length > 2) return false;
                webSocket.Send("{\"type\": \"list\"}");
            }
            else
            {
                if (args.Length != 3) return false;
                string botName = "bot_" + args[2];
                if (args[1] == "add")
                {
                    webSocket.Send(
                        $"{{\"type\": \"add\",\"data\": {{\"name\": \"{botName}\",\"skin\": \"steve\"}}}}");
                    webSocket.Send($"{{\"type\": \"connect\",\"data\": {{\"name\": \"{botName}\"}}}}");
                }
                else if (args[1] == "remove")
                {
                    webSocket.Send(
                        $"{{\"type\": \"disconnect\", \"data\": {{ \"name\": \"{botName}\" }}}}");
                    webSocket.Send(
                        $"{{\"type\": \"remove\", \"data\": {{ \"name\": \"{botName}\" }}}}");
                }
                else if (args[1] == "tp")
                {
                    Api.runcmd($"tp {name} {botName}");
                }
                else if (args[1] == "call")
                {
                    Api.runcmd($"tp {botName} {name}");
                }
                else return false;
            }

            return true;
        }

        public static bool cmdCalc(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 2) return false;
            string expression = args[1];
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
                    string[] originalNumber = expression.Split(new char[] { 'h', 'z', 'g' });
                    boxNumber = int.Parse(originalNumber[0]);
                    stackNumber = int.Parse(originalNumber[1]);
                    itemNumber = int.Parse(originalNumber[2]);
                }
                else if (expression.Contains("h") && expression.Contains("z") &&
                         !expression.Contains("g"))
                {
                    string[] originalNumber = expression.Split(new char[] { 'h', 'z' });
                    boxNumber = int.Parse(originalNumber[0]);
                    stackNumber = int.Parse(originalNumber[1]);
                }
                else if (expression.Contains("h") && !expression.Contains("z") &&
                         expression.Contains("g"))
                {
                    string[] originalNumber = expression.Split(new char[] { 'h', 'g' });
                    boxNumber = int.Parse(originalNumber[0]);
                    itemNumber = int.Parse(originalNumber[1]);
                }
                else if (!expression.Contains("h") && expression.Contains("z") &&
                         expression.Contains("g"))
                {
                    string[] originalNumber = expression.Split(new char[] { 'z', 'g' });
                    stackNumber = int.Parse(originalNumber[0]);
                    itemNumber = int.Parse(originalNumber[1]);
                }
                else if (expression.Contains("h") && !expression.Contains("z") &&
                         !expression.Contains("g"))
                {
                    string[] originalNumber = expression.Split(new char[] { 'h' });
                    boxNumber = int.Parse(originalNumber[0]);
                }
                else if (!expression.Contains("h") && expression.Contains("z") &&
                         !expression.Contains("g"))
                {
                    string[] originalNumber = expression.Split(new char[] { 'z' });
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

            return true;
        }

        public static bool cmdDay(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 2) return false;
            if (args[1] == "game")
            {
                Api.runcmd("time query day");
                Task.Run(async delegate
                {
                    await Task.Delay(1000);
                    string gameDay = GameDatas.GameDay;
                    StandardizedFeedback("@a", $"现在是游戏内的第{gameDay}天.");
                });
            }
            else if (args[1] == "server")
            {
                DateTime nowDate = DateTime.Now;
                DateTime worldStartDate = DateTime.Parse(Configs.WorldStartDate);
                string serverDay =
                    ((int)nowDate.Subtract(worldStartDate).TotalDays).ToString();
                StandardizedFeedback("@a", $"今天是开服的第{serverDay}天.");
            }
            else return false;

            return true;
        }

        public static bool cmdEntity(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 2) return false;
            if (args[1] == "count")
            {
                Api.runcmd("scoreboard players set @e _CounterCache 1");
                Api.runcmd("scoreboard players set \"entityCounter\" Counter 0");
                Api.runcmd(
                    "scoreboard players operation \"entityCounter\" Counter+= @e _CounterCache");
                Task.Run(async delegate
                {
                    await Task.Delay(1000);
                    string entityCount = GameDatas.EntityCounter;
                    StandardizedFeedback("@a", $"当前的实体数为{entityCount}");
                });
            }
            else if (args[1] == "list")
            {
                Api.runcmd("say §r§o§9服务器内的实体列表为§r§l§f @e");
            }
            else return false;

            return true;
        }

        public static bool cmdHere(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 1) return false;
            api.runcmd("playsound random.levelup @a");
            StandardizedFeedback("@a",
                $"§e§l{e.playername}§r在{e.dimension}§e§l[{(int)e.XYZ.x},{(int)e.XYZ.y},{(int)e.XYZ.z}]§r向大家打招呼！");
            return true;
        }

        public static bool cmdItem(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 2) return false;
            string name = e.playername;
            if (args[1] == "pick")
            {
                Api.runcmd($"tp @e[type=item] {name}");
                StandardizedFeedback("@a", $"{name}已拾取所有掉落物");
            }
            else if (args[1] == "clear")
            {
                Api.runcmd("kill @e[type=item]");
                StandardizedFeedback("@a", $"{name}已清除所有掉落物");
            }
            else if (args[1] == "count")
            {
                Api.runcmd("scoreboard players set @e[type=item] _CounterCache 1");
                Api.runcmd("scoreboard players set \"itemCounter\" Counter 0");
                Api.runcmd(
                    "scoreboard players operation \"itemCounter\" Counter += @e[type=item] _CounterCache");
                Task.Run(async delegate
                {
                    await Task.Delay(1000);
                    string itemCount = GameDatas.ItemCounter;
                    StandardizedFeedback("@a", $"当前的掉落物数为{itemCount}");
                });
            }
            else return false;

            return true;
        }

        public static bool cmdKi(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 2) return false;
            if (args[1] == "status")
            {
                Api.runcmd("gamerule keepInventory");

                Task.Run(async delegate
                {
                    await Task.Delay(1000);
                    StandardizedFeedback("@a", $"当前死亡不掉落{GameDatas.KiStatus}");
                });
            }
            else if (args[1] == "true")
            {
                Api.runcmd("gamerule keepInventory true");
                StandardizedFeedback("@a", "死亡不掉落已开启");
            }
            else if (args[1] == "false")
            {
                Api.runcmd("gamerule keepInventory false");
                StandardizedFeedback("@a", "死亡不掉落已关闭");
            }
            else return false;

            return true;
        }

        public static bool cmdKill(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 1) return false;
            string name = e.playername;
            playerDatas[name].IsSuicide = true;
            Api.runcmd($"kill {name}");
            if (!Configs.PluginDisable.Futures.SuicideMessages)
            {
                string[] _suicideMsgs = Configs.SuicideMessages;
                for (int i = 0; i < _suicideMsgs.Length; i++)
                {
                    _suicideMsgs[i] = _suicideMsgs[i].Replace("{}", $"§l{name}§r");
                }

                int suicideMsgNum = new Random().Next(0, _suicideMsgs.Length);
                StandardizedFeedback("@a", _suicideMsgs[suicideMsgNum]);
            }

            return true;
        }

        public static bool cmdMCP(string[] args, InputTextEvent e, MCCSAPI api)
        {
            string name = e.playername;
            if (args[1] == "status")
            {
                if (args.Length != 2) return false;
                foreach (var pluginStatusText in HelpResources.Status)
                {
                    StandardizedFeedback(name, pluginStatusText);
                }
            }
            else if (args[1] == "help")
            {
                if (args.Length != 2) return false;
                StandardizedFeedback(name,"§2========================");
                foreach (var helpText in HelpResources.Command)
                {
                    StandardizedFeedback(name, $"{Configs.CmdPrefix}{helpText.Key}          {helpText.Value}");
                }
                StandardizedFeedback(name,"§2========================");
            }
            else if (args[1] == "initialize")
            {
                if (args.Length != 2) return false;
                Api.runcmd("scoreboard objectives add Killed dummy §l§7击杀榜");
                Api.runcmd("scoreboard objectives add Dig dummy §l§7挖掘榜");
                Api.runcmd("scoreboard objectives add Dead dummy §l§7死亡榜");
                Api.runcmd("scoreboard objectives add Placed dummy §l§7放置榜");
                // _mapi.runcmd("scoreboard objectives add Attack dummy §l§7伤害榜");
                // _mapi.runcmd("scoreboard objectives add Hurt dummy §l§7承伤榜");
                // _mapi.runcmd("scoreboard objectives add Used dummy §l§7使用榜");
                Api.runcmd("scoreboard objectives add Tasks dummy §l§e服务器摸鱼指南");
                Api.runcmd("scoreboard objectives add OnlineMinutes dummy §l§7在线时长榜(分钟)");
                Api.runcmd("scoreboard objectives add _CounterCache dummy");
                Api.runcmd("scoreboard objectives add Counter dummy");
                Api.runcmd("gamerule sendCommandFeedback false");
            }
            else if (args[1] == "setting")
            {
                if (args[2] == "reload")
                {
                    if (args.Length != 3) return false;
                    LoadPlugin();
                    StandardizedFeedback("@a", "[MCP]配置文件已重新载入。");
                    return true;
                }

                if (args[2] == "help")
                {
                    if (args.Length != 3) return false;
                    foreach (var pluginSettingHelpText in HelpResources.Setting)
                    {
                        StandardizedFeedback(name, pluginSettingHelpText);
                    }

                    return true;
                }

                switch (args[2])
                {
                    case "anticheat":
                    {
                        if (args.Length != 4) return false;
                        string newConfig = args[3];
                        if (newConfig == "true" || newConfig == "false")
                        {
                            Configs.AntiCheat.Enable = bool.Parse(newConfig);
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
                        if (args.Length != 4) return false;
                        string newConfig = args[3];
                        StandardizedFeedback("@a",
                            $"MCP指令前缀已被{name}从{Configs.CmdPrefix}修改为{newConfig}");
                        Configs.CmdPrefix = newConfig;
                        break;
                    }
                    case "staautoswitchesfreq":
                    {
                        if (args.Length != 4) return false;
                        string newConfig = args[3];
                        StandardizedFeedback("@a",
                            $"计分板自动切换周期已被{name}从{Configs.StaAutoSwitchesFreq}修改为{newConfig}");
                        Configs.StaAutoSwitchesFreq = int.Parse(newConfig);
                        break;
                    }
                    case "whitelist":
                    {
                        if (args.Length != 4) return false;
                        string newConfig = args[3];
                        if (newConfig == "true" || newConfig == "false")
                        {
                            Configs.WhiteList.Enable = bool.Parse(newConfig);
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
                        if (args.Length != 4) return false;
                        string newConfig = args[3];
                        if (newConfig == "true" || newConfig == "false")
                        {
                            Configs.PluginAdmin.Enable = bool.Parse(newConfig);
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
                        if (args.Length != 4) return false;
                        string newConfig = args[3];
                        if (newConfig == "true" || newConfig == "false")
                        {
                            StandardizedFeedback("@a",
                                bool.Parse(newConfig)
                                    ? $"{name}已开启剑横扫伤害,数量上限为{Configs.MaxDamageSplash}"
                                    : $"{name}已关闭剑横扫伤害");
                        }
                        else
                        {
                            StandardizedFeedback("@a",
                                $"剑横扫伤害数量上限已被{name}从{Configs.MaxDamageSplash}修改为{newConfig}");
                            Configs.StaAutoSwitchesFreq = int.Parse(newConfig);
                        }

                        break;
                    }
                    case "offlinemessage":
                    {
                        if (args.Length != 4) return false;
                        string newConfig = args[3];
                        if (newConfig == "true" || newConfig == "false")
                        {
                            Configs.PluginDisable.Futures.OfflineMessage = bool.Parse(newConfig);
                            StandardizedFeedback("@a",
                                newConfig == "true" ? $"{name}已开启离线消息" : $"{name}已关闭离线消息");
                        }
                        else
                        {
                            StandardizedFeedback(name, "仅允许使用true或false来设置离线消息状态");
                        }

                        break;
                    }
                    default:
                    {
                        return false;
                    }
                }

                string newYaml = new Serializer().Serialize(Configs);
                File.WriteAllText(PluginPath.ConfigPath, newYaml);
            }
            else return false;

            return true;
        }


        public static bool cmdMg(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 2) return false;
            if (args[1] == "status")
            {
                Api.runcmd("gamerule mobGriefing");

                Task.Run(async delegate
                {
                    await Task.Delay(1000);
                    StandardizedFeedback("@a", $"当前生物破坏{GameDatas.MgStatus}");
                });
            }
            else if (args[1] == "true")
            {
                Api.runcmd("gamerule mobGriefing true");
                StandardizedFeedback("@a", "生物破坏已开启");
            }
            else if (args[1] == "false")
            {
                Api.runcmd("gamerule mobGriefing false");
                StandardizedFeedback("@a", "生物破坏已关闭");
            }
            else return false;

            return true;
        }

        public static bool cmdNetwork(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 2) return false;
            string name = e.playername;
            CsPlayer csPlayer = new CsPlayer(Api, e.playerPtr);
            string[] ipport = csPlayer.IpPort.Split('|');
            if (args[1] == "ip")
            {
                string ip = ipport[0];
                StandardizedFeedback("@a", $"§l{name}§r的IP地址为§e§l{ip}");
            }
            else if (args[1] == "port")
            {
                string port = ipport[1];
                StandardizedFeedback("@a", $"§l{name}§r的端口号为§e§l{port}");
            }
            else if (args[1] == "ping")
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
            else return false;

            return true;
        }

        public static bool cmdOm(string[] args, InputTextEvent e, MCCSAPI api)
        {
            string name = e.playername;
            if (Configs.PluginDisable.Futures.OfflineMessage)
            {
                StandardizedFeedback(name, "离线消息已被管理员禁用,当前无法使用.");
                return true;
            }

            if (args.Length != 3) return false;
            string recipient = args[1];
            string content = args[2];
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

            return true;
        }

        public static bool cmdQb(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (Configs.PluginDisable.Futures.QuickBackup)
            {
                StandardizedFeedback("@a", "@qb已被插件自动禁用,请检查加载提示.");
                return true;
            }

            if (args[1] == "make")
            {
                if (args.Length != 4) return false;
                string slot = args[2];
                string comment = args[3];
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

                if (!Directory.Exists($@"worlds\{Configs.WorldName}"))
                {
                    StandardizedFeedback("@a", "找不到待备份的存档,请检查配置文件!");
                    return true;
                }

                StandardizedFeedback("@a", $"服务器将在§l5秒§r后重启，将存档备份至§l[槽位{slot}]§r，预计需要一分钟");
                Task.Run(async delegate
                {
                    await Task.Delay(5000);
                    Process.Start(PluginPath.QbHelperPath,
                        $"MAKE {Configs.WorldName} {slot} {comment} {Configs.PluginLoader.CustomizationPath}");
                    Api.runcmd("stop");
                });
            }
            else if (args[1] == "back")
            {
                if (args.Length != 3) return false;
                string slot = args[2];
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
                        $"BACK {Configs.WorldName} {slot} 0 {Configs.PluginLoader.CustomizationPath}");
                    Api.runcmd("stop");
                });
            }
            else if (args[1] == "restart")
            {
                if (args.Length != 2) return false;
                StandardizedFeedback("@a", "服务器将在§l5秒§r后进行重启，预计需要一分钟");
                Task.Run(async delegate
                {
                    await Task.Delay(5000);
                    Process.Start(PluginPath.QbHelperPath,
                        $"RESTART {Configs.WorldName} 0 0 {Configs.PluginLoader.CustomizationPath}");
                    Api.runcmd("stop");
                });
            }
            else if (args[1] == "list")
            {
                if (args.Length != 2) return false;
                IniFile qbIniFile = new IniFile(PluginPath.QbInfoPath);
                StandardizedFeedback("@a", "§d§l【QuickBackup各槽位信息】");
                for (var i = 0; i < 6; i++)
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

                    var qbWorldName = qbIniFile.IniReadValue(slot, "WorldName");
                    var qbTime = qbIniFile.IniReadValue(slot, "BackupTime");
                    var qbComment = qbIniFile.IniReadValue(slot, "Comment");
                    var qbSize = qbIniFile.IniReadValue(slot, "Size");

                    StandardizedFeedback("@a",
                        $"[槽位{slot}]备份存档:§6{qbWorldName}§r  备份时间:§l{qbTime}§r  注释:{qbComment}  大小:{Tools.FormatSize(long.Parse(qbSize))}");
                }
            }
            else return false;

            return true;
        }

        private static List<string> onlinePlayer = new List<string>();
        private static List<string> quickSleepAcceptPlayer = new List<string>();
        private static bool isQuickSleep = false;
        private static string quickSleepInitiator = "";

        public static bool cmdQs(string[] args, InputTextEvent e, MCCSAPI api)
        {
            string name = e.playername;
            if (args.Length == 1)
            {
                if (isQuickSleep == true)
                {
                    StandardizedFeedback("@a", $"现在游戏内已存在一个由{quickSleepInitiator}发起的快速跳过夜晚投票");
                    StandardizedFeedback("@a",
                        $"请使用{Configs.CmdPrefix}qs accept投出支持票，使用{Configs.CmdPrefix}qs refuse投出反对票");
                    return true;
                }

                onlinePlayer.Clear();
                playerDatas.Where(playerData => playerData.Value.IsOnline)
                    .ToList()
                    .ForEach(playerData => onlinePlayer.Add(playerData.Key));

                if (onlinePlayer.Count <= 1)
                {
                    StandardizedFeedback(name, "孤单的您，害怕一人在野外入睡，您更渴望温暖的被窝。");
                    return true;
                }

                Api.runcmd("time query daytime");
                Task.Run(async delegate
                {
                    await Task.Delay(1000);
                    int gameTime = int.Parse(GameDatas.GameTime);
                    if (gameTime >= 12544 && gameTime <= 23460)
                    {
                        isQuickSleep = true;
                        quickSleepInitiator = name;
                        quickSleepAcceptPlayer.Clear();
                        quickSleepAcceptPlayer.Add(quickSleepInitiator);
                        StandardizedFeedback("@a",
                            $"§6§l{quickSleepInitiator}发起快速跳过夜晚投票，请使用{Configs.CmdPrefix}qs accept投出支持票，使用{Configs.CmdPrefix}qs refuse投出反对票。");
                        StandardizedFeedback("@a",
                            $"§6§l当前已有{quickSleepAcceptPlayer.Count}人投出支持票，至少需要{onlinePlayer.Count}人");
                        StandardizedFeedback("@a", "§6§l投票将在18秒后结束。");
                        _ = Task.Run(async delegate
                        {
                            await Task.Delay(18000);
                            if (quickSleepAcceptPlayer.Count >= onlinePlayer.Count)
                            {
                                StandardizedFeedback("@a",
                                    "§5§l深夜，一阵突如其来的反常疲惫侵袭了您的大脑，您失去意识倒在地上。当您醒来时，太阳正从东方冉冉升起。");
                                Api.runcmd("time set sunrise");
                                Api.runcmd("effect @a instant_damage 1 1 true");
                                Api.runcmd("effect @a blindness 8 1 true");
                                Api.runcmd("effect @a nausea 8 1 true");
                                Api.runcmd("effect @a hunger 8 5 true");
                                Api.runcmd("effect @a slowness 8 5 true");
                            }
                            else
                            {
                                StandardizedFeedback("@a", $"§4§l{quickSleepInitiator}忽然之间厌倦了夜晚。");
                                Api.runcmd($"effect {quickSleepInitiator} instant_damage 1 1 true");
                                Api.runcmd($"effect {quickSleepInitiator} blindness 8 1 true");
                                Api.runcmd($"effect {quickSleepInitiator} nausea 8 1 true");
                                Api.runcmd($"effect {quickSleepInitiator} hunger 8 5 true");
                                Api.runcmd($"effect {quickSleepInitiator} slowness 8 5 true");
                            }

                            isQuickSleep = false;
                            quickSleepInitiator = "";
                        });
                    }
                    else
                    {
                        StandardizedFeedback("@a", "光线突然变得不可抗拒地明亮了起来，刺眼的阳光令您久久无法入睡。");
                    }
                });
            }
            else if (args[1] == "accept")
            {
                if (args.Length != 2) return false;
                if (isQuickSleep == true)
                {
                    if (!quickSleepAcceptPlayer.Contains(name))
                    {
                        quickSleepAcceptPlayer.Add(name);
                        StandardizedFeedback("@a",
                            $"§a{name}向{quickSleepInitiator}发起的快速跳过夜晚投票投出支持票.已有{quickSleepAcceptPlayer.Count}人投出支持票，至少需要{onlinePlayer.Count}人。");
                    }
                    else
                    {
                        StandardizedFeedback(name, "请勿重复投票");
                    }
                }
                else
                {
                    StandardizedFeedback(name, $"当前暂无快速跳过夜晚的投票，您可通过{Configs.CmdPrefix}qs进行发起");
                }
            }
            else if (args[1] == "refuse")
            {
                if (args.Length != 2) return false;
                if (isQuickSleep == true)
                {
                    if (quickSleepAcceptPlayer.Contains(name))
                    {
                        quickSleepAcceptPlayer.Remove(name);
                    }

                    StandardizedFeedback("@a", $"§c{name}向{quickSleepInitiator}发起的快速跳过夜晚投票投出拒绝票.");
                }
                else
                {
                    StandardizedFeedback(name, $"当前暂无快速跳过夜晚的投票，您可通过{Configs.CmdPrefix}qs进行发起");
                }
            }
            else return false;

            return true;
        }

        public static bool cmdRc(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length == 1) return false;
            var msg = string.Join(" ", args);
            var command = msg.Replace($"{Configs.CmdPrefix}rc ", "");
            var cmdResult = Api.runcmd(command);
            StandardizedFeedback("@a", cmdResult ? $"已成功向控制台注入了{command}" : $"{command}运行失败");

            return true;
        }

        public static bool cmdSize(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 1) return false;
            var worldSize = Tools.FormatSize(Tools.GetWorldSize($@"worlds\{Configs.WorldName}"));
            StandardizedFeedback("@a", $"当前服务器的存档大小是§l§6{worldSize}");
            return true;
        }

        private static Timer staAutoSwitchesTimer = null;

        public static bool cmdSta(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args[1] == "auto")
            {
                if (args.Length != 3) return false;
                if (bool.Parse(args[2]))
                {
                    if (staAutoSwitchesTimer != null)
                    {
                        StandardizedFeedback("@a", "计分板自动切换正在运行中，请勿重复开启.");
                        return true;
                    }

                    staAutoSwitchesTimer = new Timer();
                    staAutoSwitchesTimer.Interval = Configs.StaAutoSwitchesFreq * 1000;
                    staAutoSwitchesTimer.Elapsed += StaAutoSwitches;
                    staAutoSwitchesTimer.AutoReset = true;
                    staAutoSwitchesTimer.Start();
                    StandardizedFeedback("@a", $"已开启计分板自动切换.切换周期:{Configs.StaAutoSwitchesFreq}秒");
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
                if (args.Length != 2) return false;
                var statisName = args[1];
                var cnStatisName = new Dictionary<string, string>
                {
                    { "Dig", "挖掘榜" },
                    { "Placed", "放置榜" },
                    { "Killed", "击杀榜" },
                    { "Tasks", "待办事项榜" },
                    { "Dead", "死亡榜" },
                    { "OnlineMinutes", "在线时长榜(分钟)" }
                };
                if (statisName != "null")
                {
                    Api.runcmd($"scoreboard objectives setdisplay sidebar {statisName}");
                    StandardizedFeedback("@a",
                        cnStatisName.ContainsKey(statisName)
                            ? $"已将侧边栏显示修改为{cnStatisName[statisName]}"
                            : $"已将侧边栏显示修改为{statisName}");
                }
                else
                {
                    Api.runcmd("scoreboard objectives setdisplay sidebar");
                    StandardizedFeedback("@a", "已关闭侧边栏显示");
                }
            }

            return true;
        }

        public static bool cmdSystem(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 2) return false;
            var systemInfo = new SystemInfo();
            if (args[1] == "cpu")
            {
                string cpuUsage = systemInfo.GetCpuUsage();
                StandardizedFeedback("@a", $"当前服务器CPU占用率为§l§6{cpuUsage}");
            }
            else if (args[1] == "memory")
            {
                string memoryUsage = systemInfo.GetMemoryUsage();
                StandardizedFeedback("@a", $"当前服务器物理内存占用率为§l§6{memoryUsage}");
            }
            else return false;

            return true;
        }

        public static bool cmdTask(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 3) return false;
            var taskName = args[2];
            if (args[1] == "add")
            {
                Api.runcmd($"scoreboard players set {taskName} Tasks 1");
                StandardizedFeedback("@a", $"已向待办事项板添加§l{taskName}");
            }
            else if (args[1] == "remove")
            {
                Api.runcmd($"scoreboard players reset {taskName} Tasks");
                StandardizedFeedback("@a", $"已将§l{taskName}§r从待办事项板上移除");
            }
            else return false;

            return true;
        }

        public static bool cmdTeleport(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 2) return false;
            var victim = e.playername;
            var destination = args[1];
            var destinationList = new List<string>();

            playerDatas.Where(p=>p.Value.IsOnline&&p.Value.Name.ToLower().StartsWith(destination.ToLower()))
                .ToList()
                .ForEach(p=>destinationList.Add(p.Value.Name));

            if (destinationList.Count==1)
            {
                api.runcmd($"tp {victim} {destinationList[0]}");
                StandardizedFeedback("@a",$"已将{victim}传送至{destinationList[0]}.");
            }
            else
            {
                StandardizedFeedback("@a",$"匹配{destination}的玩家共有{destinationList.Count}名,无法进行精准传送,请修改传送目标.");
            }

            return true;
        }

        public static bool cmdTick(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 2) return false;
            if (args[1] == "status")
            {
                Api.runcmd("gamerule randomtickspeed");
                Task.Run(async delegate
                {
                    await Task.Delay(1000);
                    StandardizedFeedback("@a", $"现在的游戏随机刻为{GameDatas.TickStatus}");
                });
            }
            else if (int.TryParse(args[1], out int tickSpeed))
            {
                Api.runcmd($"gamerule randomtickspeed {tickSpeed}");
                StandardizedFeedback("@a", $"已将游戏内随机刻修改为{tickSpeed}。");
            }
            else return false;

            return true;
        }

        public static bool cmdWhiteList(string[] args, InputTextEvent e, MCCSAPI api)
        {
            if (args.Length != 3) return false;
            var name = e.playername;
            var pendingName = args[2];
            if (args[1] == "add")
            {
                if (Configs.WhiteList.PlayerList.Any(player => player.Name == pendingName))
                {
                    StandardizedFeedback(name, $"白名单中已存在玩家{pendingName}");
                    return true;
                }

                if (playerDatas.ContainsKey(pendingName))
                {
                    Configs.WhiteList.PlayerList.Add(new Player()
                        { Name = pendingName, Xuid = playerDatas[pendingName].Xuid });
                    string newConfig = new Serializer().Serialize(Configs);
                    File.WriteAllText(PluginPath.ConfigPath, newConfig);
                    StandardizedFeedback("@a", $"{name}已将{pendingName}加入白名单。");
                }
                else
                {
                    StandardizedFeedback(name, $"{pendingName}未曾尝试加入过服务器，请让其尝试加入服务器后再添加白名单");
                }
            }
            else if (args[1] == "remove")
            {
                if (Configs.WhiteList.PlayerList.Any(player => player.Name == pendingName))
                {
                    if (playerDatas.ContainsKey(pendingName))
                    {
                        Configs.WhiteList.PlayerList.Remove(new Player()
                            { Name = pendingName, Xuid = playerDatas[pendingName].Xuid });
                        Api.runcmd($"kick {pendingName} 您已被{name}永久封禁。");
                        string newConfig = new Serializer().Serialize(Configs);
                        File.WriteAllText(PluginPath.ConfigPath, newConfig);
                        StandardizedFeedback("@a", $"{pendingName}已被{name}永久封禁。");
                    }
                    else
                    {
                        StandardizedFeedback(name, $"{pendingName}未曾加入过服务器，请让其加入服务器后再移除白名单");
                    }

                    return true;
                }

                StandardizedFeedback(name, $"白名单中不存在玩家{pendingName}");
            }
            else return false;

            return true;
        }
    }
}