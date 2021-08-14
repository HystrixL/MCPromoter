using System.Linq;
using System.Threading.Tasks;
using CSR;
using static MCPromoter.Output;

namespace MCPromoter
{
    partial class MCPromoter
    {
        public static bool LoadNamePlugin(Events x)
        {
            var e = BaseEvent.getFrom(x) as LoadNameEvent;
            if (e == null) return true;

            var name = e.playername;
            var uuid = e.uuid;
            var xuid = e.xuid;
            var isAllowLogin = false;

            if (name.StartsWith("bot_"))
            {
                isAllowLogin = true;
                Api.runcmd($"tag {name} add BOT");
            }
            else
            {
                if (playerDatas.ContainsKey(name))
                {
                    playerDatas[name].Name = name;
                    playerDatas[name].IsOnline = true;
                    playerDatas[name].Uuid = uuid;
                    playerDatas[name].Xuid = xuid;
                }
                else
                {
                    playerDatas.Add(name, new PlayerDatas { Name = name, Uuid = uuid, Xuid = xuid, IsOnline = true });
                    if (Configs.Logging.Plugin) LogsWriter("MCP", $"新实例化用于存储{name}信息的PlayerDatas类");
                    if (Configs.ConsoleOutput.Plugin) ConsoleOutputter("MCP", $"新实例化用于存储{name}信息的PlayerDatas类");
                }
            }

            if (Configs.WhiteList.PlayerList.Any(player => player.Name == name && player.Xuid == xuid))
            {
                isAllowLogin = true;
            }

            if (!Configs.WhiteList.Enable) isAllowLogin = true;

            if (isAllowLogin)
            {
                Api.runcmd("playsound random.orb @a");
                if (!Configs.PluginDisable.Futures.OfflineMessage)
                {
                    Task.Run(async delegate
                    {
                        await Task.Delay(30000);
                        StandardizedFeedback(name, $"您有 §l{playerDatas[name].OfflineMessage.Count} §r条未读离线消息.");
                        foreach (var offlineMessage in playerDatas[name].OfflineMessage)
                            StandardizedFeedback(name, offlineMessage);

                        playerDatas[name].OfflineMessage.Clear();
                    });
                }
            }
            else
            {
                Task.Run(async delegate
                {
                    await Task.Delay(1000);
                    if (Configs.Logging.PlayerOnlineOffline) LogsWriter(name, " 尝试加入服务器.");
                    if (Configs.ConsoleOutput.PlayerOnlineOffline) ConsoleOutputter(name, " 尝试加入服务器.");
                    Api.disconnectClient(uuid, "您未受邀加入该服务器。");
                    if (Configs.ConsoleOutput.Plugin) ConsoleOutputter("MCP", $"{name}未受邀加入该服务器，已自动踢出。");
                    if (Configs.Logging.Plugin) LogsWriter("MCP", $"{name}未受邀加入该服务器，已自动踢出。");
                });
            }

            return true;
        }

        public static bool PlayerLeftPlugin(Events x)
        {
            var e = BaseEvent.getFrom(x) as PlayerLeftEvent;
            if (e == null) return true;

            string name = e.playername;
            if (Configs.Logging.PlayerOnlineOffline) LogsWriter(name, " 离开了服务器.");
            if (Configs.ConsoleOutput.PlayerOnlineOffline) ConsoleOutputter(name, " 离开了服务器.");
            if (!name.StartsWith("bot_")) playerDatas[name].IsOnline = false;
            return true;
        }
    }
}