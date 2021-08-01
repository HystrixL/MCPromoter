using System.Threading.Tasks;
using CSR;

namespace MCPromoter
{
    partial class MCPromoter
    {
        public static bool LoadNamePlugin(Events x)
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
                    _mapi.runcmd($"tag {name} add BOT");
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
                    _mapi.runcmd($"kick {name} 您未受邀加入该服务器，详情请咨询Hil。");
                    if (config.ConsoleOutput.Plugin) ConsoleOutputter("MCP", $"{name}未受邀加入该服务器，已自动踢出。");
                    if (config.Logging.Plugin) LogsWriter("MCP", $"{name}未受邀加入该服务器，已自动踢出。");
                });

                return true;
        }
        
        public static bool PlayerLeftPlugin(Events x)
        {
            var e = BaseEvent.getFrom(x) as PlayerLeftEvent;
            if (e == null) return true;

            string name = e.playername;
            if (config.Logging.PlayerOnlineOffline) LogsWriter(name, " 离开了服务器.");
            if (config.ConsoleOutput.PlayerOnlineOffline) ConsoleOutputter(name, " 离开了服务器.");
            if(!name.StartsWith("bot_")) playerDatas[name].IsOnline = false;
            return true;
        }
    }
}