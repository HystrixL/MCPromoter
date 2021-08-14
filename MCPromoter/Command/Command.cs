using System.Linq;
using CSR;
using static MCPromoter.Output;

namespace MCPromoter
{
    partial class MCPromoter
    {
        public static void CommandInitialization()
        {
            CommandManager.addCommand("back", CommandLibrary.cmdBack);
            CommandManager.addCommandHelp("back", "返回死亡地点");
            CommandManager.addCommand("bot", CommandLibrary.cmdBot);
            CommandManager.addCommandHelp("bot [add|remove|tp|call] <BOT名>", "召唤/杀死/传送到/唤来bot");
            CommandManager.addCommandHelp("bot list", "列出服务器内存在的bot");
            CommandManager.addCommand("calc", CommandLibrary.cmdCalc);
            CommandManager.addCommandHelp("calc", "计算表达式/物品数量");
            CommandManager.addCommand("day", CommandLibrary.cmdDay);
            CommandManager.addCommandHelp("day [game|server]", "查询游戏内/开服天数");
            CommandManager.addCommand("entity", CommandLibrary.cmdEntity);
            CommandManager.addCommandHelp("entity [count|list]", "统计/列出服务器内实体");
            CommandManager.addCommand("here", CommandLibrary.cmdHere);
            CommandManager.addCommandHelp("here", "全服报点");
            CommandManager.addCommand("item", CommandLibrary.cmdItem);
            CommandManager.addCommandHelp("item [clear|count|pick]", "清除/统计/拾取服务器内掉落物");
            CommandManager.addCommand("ki", CommandLibrary.cmdKi);
            CommandManager.addCommandHelp("ki [true|false|status]", "开启/关闭/查询死亡不掉落");
            CommandManager.addCommand("kill", CommandLibrary.cmdKill);
            CommandManager.addCommandHelp("kill", "自杀(不计入死亡榜)");
            CommandManager.addCommand("mcp", CommandLibrary.cmdMCP);
            // CommandManager.addCommandHelp("",);
            CommandManager.addCommand("mg", CommandLibrary.cmdMg);
            CommandManager.addCommandHelp("mg [true|false|status]", "开启/关闭/查询生物破坏");
            CommandManager.addCommand("network", CommandLibrary.cmdNetwork);
            CommandManager.addCommandHelp("network [ip|port|ping]", "查询玩家ip/端口/延迟");
            CommandManager.addCommand("om", CommandLibrary.cmdOm);
            CommandManager.addCommandHelp("om <玩家名> <消息>", "向某位离线玩家发送离线消息");
            CommandManager.addCommand("qb", CommandLibrary.cmdQb);
            CommandManager.addCommandHelp("qb make <槽位> <注释>", "快速备份存档(需指定槽位及注释)(槽位范围1~5)");
            CommandManager.addCommandHelp("qb back <槽位>", "快速回档服务器(需指定槽位)(槽位范围AUTO、1~5)");
            CommandManager.addCommandHelp("qb restart", "快速重启服务器");
            CommandManager.addCommandHelp("qb list", "查询QuickBackup各槽位信息(存档名、备份时间、注释、大小)");
            CommandManager.addCommand("qs", CommandLibrary.cmdQs);
            CommandManager.addCommandHelp("qs", "发起快速跳过夜晚投票");
            CommandManager.addCommandHelp("qs [accept|refuse]", "同意/拒绝快速跳过夜晚");
            CommandManager.addCommand("rc", CommandLibrary.cmdRc);
            CommandManager.addCommandHelp("rc <指令>", " 向控制台注入指令");
            CommandManager.addCommand("size", CommandLibrary.cmdSize);
            CommandManager.addCommandHelp("size", "获取存档大小");
            CommandManager.addCommand("sta", CommandLibrary.cmdSta);
            CommandManager.addCommandHelp("sta <计分板名>", "将侧边栏显示调整为特定计分板(null为关闭)");
            CommandManager.addCommandHelp("sta auto [true|false]", "开启/关闭计分板自动切换");
            CommandManager.addCommand("system", CommandLibrary.cmdSystem);
            CommandManager.addCommandHelp("system [cpu|memory]", "查询服务器CPU/内存占用率");
            CommandManager.addCommand("task", CommandLibrary.cmdTask);
            CommandManager.addCommandHelp("task [add|remove] <任务名>", "添加/移除待办");
            CommandManager.addCommand("tick", CommandLibrary.cmdTick);
            CommandManager.addCommandHelp("tick [倍数|status]", "设置/查询随机刻倍数");
            CommandManager.addCommand("tp", CommandLibrary.cmdTeleport);
            CommandManager.addCommandHelp("tp <玩家名>", "将自己传送至目标玩家,目标名可只打开头,会自动匹配(忽略大小写).");
            CommandManager.addCommand("whitelist", CommandLibrary.cmdWhiteList);
            CommandManager.addCommandHelp("whitelist [add|remove] <玩家名>", "将玩家加入/移出白名单");
        }

        public static bool InputTextPlugin(Events x)
        {
            var e = BaseEvent.getFrom(x) as InputTextEvent;
            if (e == null) return true;
            var name = e.playername;
            var xuid = playerDatas[name].Xuid;
            var msg = e.msg;


            if (msg.StartsWith(Configs.CmdPrefix))
            {
                var argsList = msg.Split(' ');
                argsList[0] = argsList[0].Replace(Configs.CmdPrefix, "");
                argsList[0] = argsList[0].ToLower();

                if (!Commands.ContainsKey(argsList[0]))
                {
                    StandardizedFeedback(name, "您使用的指令不存在,请查询@mcp help.");
                    return true;
                }

                if (Configs.PluginDisable.Commands.Any(disableCommand =>
                    msg.Replace(Configs.CmdPrefix, "").StartsWith(disableCommand)))
                {
                    StandardizedFeedback("@a", $"该指令已被通过配置文件禁用,当前无法使用.详情请咨询Hil.");
                    return true;
                }

                if (Configs.PluginAdmin.Enable)
                {
                    var isAdminCmd = false;
                    var isAdminPlayer = false;
                    if (Configs.PluginAdmin.AdminCmd.Any(adminCmd =>
                        msg.Replace(Configs.CmdPrefix, "").StartsWith(adminCmd)))
                    {
                        isAdminCmd = true;
                        if (Configs.PluginAdmin.AdminList.Any(player => player.Name == name && player.Xuid == xuid))
                        {
                            isAdminPlayer = true;
                        }
                    }

                    if (isAdminCmd && !isAdminPlayer)
                    {
                        StandardizedFeedback(name, $"{msg}需要admin权限才可使用，您当前无权使用该指令。");
                        return true;
                    }
                }

                if (Configs.Logging.Plugin) LogsWriter(name, msg);
                if (Configs.ConsoleOutput.Plugin) ConsoleOutputter(name, msg);

                var commandResult = Commands[argsList[0]](argsList, e, Api);
                if (!commandResult) StandardizedFeedback(name, "指令用法错误,请查询@mcp help.");
            }
            else
            {
                if (Configs.Logging.Chat) LogsWriter(name, msg);
                if (Configs.ConsoleOutput.Chat) ConsoleOutputter(name, msg);
            }

            return true;
        }
    }
}