using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCPromoter
{
    public static class PluginInfo
    {
        public static string Name => "MinecraftPromoter";
        public static string Version => "V1.11.0";
        public static int VersioID
        {
            get
            {
                string[] version = Version.Replace("V", "").Split('.');
                int versionID = int.Parse(version[0]) * 10000 + int.Parse(version[1]) * 100 + int.Parse(version[2]);
                return versionID;
            }
        }
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
}
