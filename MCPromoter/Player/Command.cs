using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Timers;
using YamlDotNet.Serialization;
using CSR;

namespace MCPromoter
{
    partial class MCPromoter
    {
        public static bool InputTextPlugin(Events x)
        {
            var e = BaseEvent.getFrom(x) as InputTextEvent;
            if (e == null) return true;
            string name = e.playername;
            string xuid = playerDatas[name].Xuid;
            string msg = e.msg;
            CsPlayer csPlayer = new CsPlayer(_mapi, e.playerPtr);

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

                argsList[0] = argsList[0].ToLower();
                try
                {
                    switch (argsList[0])
                    {
                        case "mcp":
                            if (argsList[1] == "status")
                            {
                                foreach (var pluginStatusText in HelpResources.Status)
                                {
                                    StandardizedFeedback(name, pluginStatusText);
                                }
                            }
                            else if (argsList[1] == "help")
                            {
                                foreach (var helpText in HelpResources.Command)
                                {
                                    StandardizedFeedback(name, config.CmdPrefix + helpText);
                                }
                            }
                            else if (argsList[1] == "initialize")
                            {
                                _mapi.runcmd("scoreboard objectives add Killed dummy §l§7击杀榜");
                                _mapi.runcmd("scoreboard objectives add Dig dummy §l§7挖掘榜");
                                _mapi.runcmd("scoreboard objectives add Dead dummy §l§7死亡榜");
                                _mapi.runcmd("scoreboard objectives add Placed dummy §l§7放置榜");
                                // _mapi.runcmd("scoreboard objectives add Attack dummy §l§7伤害榜");
                                // _mapi.runcmd("scoreboard objectives add Hurt dummy §l§7承伤榜");
                                // _mapi.runcmd("scoreboard objectives add Used dummy §l§7使用榜");
                                _mapi.runcmd("scoreboard objectives add Tasks dummy §l§e服务器摸鱼指南");
                                _mapi.runcmd("scoreboard objectives add OnlineMinutes dummy §l§7在线时长榜(分钟)");
                                _mapi.runcmd("scoreboard objectives add _CounterCache dummy");
                                _mapi.runcmd("scoreboard objectives add Counter dummy");
                                _mapi.runcmd("gamerule sendCommandFeedback false");
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
                                    foreach (var pluginSettingHelpText in HelpResources.Setting)
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

                            break;
                        case "here":
                            _mapi.runcmd("playsound random.levelup @a");
                            StandardizedFeedback("@a",
                                $"§e§l{name}§r在{e.dimension}§e§l[{e.XYZ.x},{e.XYZ.y},{e.XYZ.z}]§r向大家打招呼！");
                            break;
                        case "back":
                            if (playerDatas[name].DeadEnable)
                            {
                                if (playerDatas[name].DeadWorld == e.dimension)
                                {
                                    _mapi.runcmd(
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
                                string botName = "bot_" + argsList[2];
                                if (argsList[1] == "add")
                                {
                                    webSocket.Send(
                                        $"{{\"type\": \"add\",\"data\": {{\"name\": \"{botName}\",\"skin\": \"steve\"}}}}");
                                    webSocket.Send($"{{\"type\": \"connect\",\"data\": {{\"name\": \"{botName}\"}}}}");
                                }
                                else if (argsList[1] == "remove")
                                {
                                    webSocket.Send(
                                        $"{{\"type\": \"disconnect\", \"data\": {{ \"name\": \"{botName}\" }}}}");
                                    webSocket.Send(
                                        $"{{\"type\": \"remove\", \"data\": {{ \"name\": \"{botName}\" }}}}");
                                }
                                else if (argsList[1] == "tp")
                                {
                                    _mapi.runcmd($"tp {name} {botName}");
                                }
                                else if (argsList[1] == "call")
                                {
                                    _mapi.runcmd($"tp {botName} {name}");
                                }
                            }

                            break;
                        case "day":
                            if (argsList[1] == "game")
                            {
                                _mapi.runcmd("time query day");
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
                                    ((int)nowDate.Subtract(worldStartDate).TotalDays).ToString();
                                StandardizedFeedback("@a", $"今天是开服的第{serverDay}天.");
                            }

                            break;
                        case "item":
                            if (argsList[1] == "pick")
                            {
                                _mapi.runcmd($"tp @e[type=item] {name}");
                                StandardizedFeedback("@a", $"{name}已拾取所有掉落物");
                            }
                            else if (argsList[1] == "clear")
                            {
                                _mapi.runcmd("kill @e[type=item]");
                                StandardizedFeedback("@a", $"{name}已清除所有掉落物");
                            }
                            else if (argsList[1] == "count")
                            {
                                _mapi.runcmd("scoreboard players set @e[type=item] _CounterCache 1");
                                _mapi.runcmd("scoreboard players set \"itemCounter\" Counter 0");
                                _mapi.runcmd(
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
                                _mapi.runcmd("scoreboard players set @e _CounterCache 1");
                                _mapi.runcmd("scoreboard players set \"entityCounter\" Counter 0");
                                _mapi.runcmd(
                                    "scoreboard players operation \"entityCounter\" Counter+= @e _CounterCache");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    string entityCount = GameDatas.EntityCounter;
                                    StandardizedFeedback("@a", $"当前的实体数为{entityCount}");
                                });
                            }
                            else if (argsList[1] == "list")
                            {
                                _mapi.runcmd("say §r§o§9服务器内的实体列表为§r§l§f @e");
                            }

                            break;
                        case "ki":
                            if (argsList[1] == "status")
                            {
                                _mapi.runcmd("gamerule keepInventory");

                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    StandardizedFeedback("@a", $"当前死亡不掉落{GameDatas.KiStatus}");
                                });
                            }
                            else if (argsList[1] == "true")
                            {
                                _mapi.runcmd("gamerule keepInventory true");
                                StandardizedFeedback("@a", "死亡不掉落已开启");
                            }
                            else if (argsList[1] == "false")
                            {
                                _mapi.runcmd("gamerule keepInventory false");
                                StandardizedFeedback("@a", "死亡不掉落已关闭");
                            }

                            break;
                        case "kill":
                            playerDatas[name].IsSuicide = true;
                            _mapi.runcmd($"kill {name}");
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
                                _mapi.runcmd("gamerule mobGriefing");

                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    StandardizedFeedback("@a", $"当前生物破坏{GameDatas.MgStatus}");
                                });
                            }
                            else if (argsList[1] == "true")
                            {
                                _mapi.runcmd("gamerule mobGriefing true");
                                StandardizedFeedback("@a", "生物破坏已开启");
                            }
                            else if (argsList[1] == "false")
                            {
                                _mapi.runcmd("gamerule mobGriefing false");
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
                            bool cmdResult = _mapi.runcmd(command);
                            StandardizedFeedback("@a", cmdResult ? $"已成功向控制台注入了{command}" : $"{command}运行失败");

                            break;
                        case "size":
                            //string worldSize = ((int) (GetWorldSize($@"worlds\{worldName}") / 1024 / 1024)).ToString();
                            string worldSize = Tools.FormatSize(Tools.GetWorldSize($@"worlds\{config.WorldName}"));
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
                                    { "Dig", "挖掘榜" },
                                    { "Placed", "放置榜" },
                                    { "Killed", "击杀榜" },
                                    { "Tasks", "待办事项榜" },
                                    { "Dead", "死亡榜" },
                                    { "OnlineMinutes", "在线时长榜(分钟)" }
                                };
                                if (statisName != "null")
                                {
                                    _mapi.runcmd($"scoreboard objectives setdisplay sidebar {statisName}");
                                    StandardizedFeedback("@a",
                                        cnStatisName.ContainsKey(statisName)
                                            ? $"已将侧边栏显示修改为{cnStatisName[statisName]}"
                                            : $"已将侧边栏显示修改为{statisName}");
                                }
                                else
                                {
                                    _mapi.runcmd("scoreboard objectives setdisplay sidebar");
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
                                _mapi.runcmd($"scoreboard players set {taskName} Tasks 1");
                                StandardizedFeedback("@a", $"已向待办事项板添加§l{taskName}");
                            }
                            else if (argsList[1] == "remove")
                            {
                                _mapi.runcmd($"scoreboard players reset {taskName} Tasks");
                                StandardizedFeedback("@a", $"已将§l{taskName}§r从待办事项板上移除");
                            }

                            break;
                        case "tick":
                            if (argsList[1] == "status")
                            {
                                _mapi.runcmd("gamerule randomtickspeed");
                                Task.Run(async delegate
                                {
                                    await Task.Delay(1000);
                                    StandardizedFeedback("@a", $"现在的游戏随机刻为{GameDatas.TickStatus}");
                                });
                            }
                            else if (int.TryParse(argsList[1], out int tickSpeed))
                            {
                                _mapi.runcmd($"gamerule randomtickspeed {tickSpeed}");
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
                                        { Name = pendingName, Xuid = playerDatas[pendingName].Xuid });
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
                                                { Name = pendingName, Xuid = playerDatas[pendingName].Xuid });
                                            _mapi.runcmd($"kick {pendingName} 您已被{name}永久封禁。");
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
                                    StandardizedFeedback("@a", $"现在游戏内已存在一个由{quickSleepInitiator}发起的快速跳过夜晚投票");
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
                                    StandardizedFeedback(name, "孤单的您，害怕一人在野外入睡，您更渴望温暖的被窝。");
                                    return true;
                                }

                                _mapi.runcmd("time query daytime");
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
                                            $"§6§l{quickSleepInitiator}发起快速跳过夜晚投票，请使用{config.CmdPrefix}qs accept投出支持票，使用{config.CmdPrefix}qs refuse投出反对票。");
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
                                                _mapi.runcmd("time set sunrise");
                                                _mapi.runcmd("effect @a instant_damage 1 1 true");
                                                _mapi.runcmd("effect @a blindness 8 1 true");
                                                _mapi.runcmd("effect @a nausea 8 1 true");
                                                _mapi.runcmd("effect @a hunger 8 5 true");
                                                _mapi.runcmd("effect @a slowness 8 5 true");
                                            }
                                            else
                                            {
                                                StandardizedFeedback("@a", $"§4§l{quickSleepInitiator}忽然之间厌倦了夜晚。");
                                                _mapi.runcmd($"effect {quickSleepInitiator} instant_damage 1 1 true");
                                                _mapi.runcmd($"effect {quickSleepInitiator} blindness 8 1 true");
                                                _mapi.runcmd($"effect {quickSleepInitiator} nausea 8 1 true");
                                                _mapi.runcmd($"effect {quickSleepInitiator} hunger 8 5 true");
                                                _mapi.runcmd($"effect {quickSleepInitiator} slowness 8 5 true");
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
                            else if (argsList[1] == "accept")
                            {
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
                                    StandardizedFeedback(name, $"当前暂无快速跳过夜晚的投票，您可通过{config.CmdPrefix}qs进行发起");
                                }
                            }
                            else if (argsList[1] == "refuse")
                            {
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
                                    StandardizedFeedback(name, $"当前暂无快速跳过夜晚的投票，您可通过{config.CmdPrefix}qs进行发起");
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
                                    _mapi.runcmd("stop");
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
                                    _mapi.runcmd("stop");
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
                                    _mapi.runcmd("stop");
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
                                        $"[槽位{slot}]备份存档:§6{qbWorldName}§r  备份时间:§l{qbTime}§r  注释:{qbComment}  大小:{Tools.FormatSize(long.Parse(qbSize))}");
                                }
                            }

                            break;
                        default:
                            StandardizedFeedback(name, $"无效的MCP指令，请输入{config.CmdPrefix}mcp help获取帮助");
                            break;
                    }
                }
                catch(IndexOutOfRangeException)
                {
                    StandardizedFeedback(name,$"无效的MCP指令，请输入{config.CmdPrefix}mcp help获取帮助");
                }
            }
            else
            {
                if (config.Logging.Chat) LogsWriter(name, msg);
                if (config.ConsoleOutput.Chat) ConsoleOutputter(name, msg);
            }

            return true;
        }
    }
}