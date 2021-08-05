using System.Collections.Generic;
using System.Timers;
using CSR;
using WebSocketSharp;

namespace MCPromoter
{
    partial class MCPromoter
    {
        public static MCCSAPI Api;
        public static Config Configs;
        public static Dictionary<string, Command> Commands = new Dictionary<string, Command>();

        public static Dictionary<string, PlayerDatas> playerDatas = new Dictionary<string, PlayerDatas>();
        public static WebSocket webSocket = null;
        private static Timer onlineMinutesAccTimer = null;
        private static Timer forceGamemodeTimer = null;
        private static Timer autoBackupTimer = null;
    }
}