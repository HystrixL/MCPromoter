using System.Collections.Generic;
using System.Timers;
using CSR;
using WebSocketSharp;

namespace MCPromoter
{
    partial class MCPromoter
    {
        private static MCCSAPI _mapi;

        private static Config config;
        private static Dictionary<string, PlayerDatas> playerDatas = new Dictionary<string, PlayerDatas>();
        private static WebSocket webSocket = null;
        private static Timer onlineMinutesAccTimer = null;
        private static Timer forceGamemodeTimer = null;
        private static Timer autoBackupTimer = null;
        private static Timer staAutoSwitchesTimer = null;
        private static List<string> onlinePlayer = new List<string>();
        private static List<string> acceptPlayer = new List<string>();
        private static bool isQuickSleep = false;
        private static string quickSleepInitiator = "";
    }
}