using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using static MCPromoter.Output;

namespace MCPromoter
{
    partial class MCPromoter
    {
        public static void ForceGamemode(Object source, ElapsedEventArgs e)
        {
            Api.runcmd("gamemode default @a");
        }

        public static void OnlineMinutesAcc(Object source, ElapsedEventArgs e)
        {
            Api.runcmd($"scoreboard players add @a[tag=!BOT] OnlineMinutes 1");
        }

        static string[] StaAutoSwitchesList =
        {
            "Dig", "Placed", "Killed", "Tasks", "Dead", "OnlineMinutes"
        };

        static int StaAutoSwitchesTimer = 0;

        public static void StaAutoSwitches(Object source, ElapsedEventArgs e)
        {
            Api.runcmd($"scoreboard objectives setdisplay sidebar {StaAutoSwitchesList[StaAutoSwitchesTimer]}");
            if (StaAutoSwitchesTimer >= StaAutoSwitchesList.Length - 1)
            {
                StaAutoSwitchesTimer = 0;
            }
            else
            {
                StaAutoSwitchesTimer++;
            }
        }

        public static void AutoBackup(Object source, ElapsedEventArgs e)
        {
            if (DateTime.Now.ToString("t") == Configs.AutoBackupTime)
            {
                StandardizedFeedback("@a", $"服务器将在§l5秒§r后重启进行每日自动备份，预计需要一分钟");
                Task.Run(async delegate
                {
                    await Task.Delay(5000);
                    Process.Start(PluginPath.QbHelperPath,
                        $"MAKE {Configs.WorldName} AUTO {DateTime.Now:MMdd}EverydayAutoBackup|每日自动备份 {Configs.PluginLoader.CustomizationPath}");
                    Api.runcmd("stop");
                });
            }
        }
    }
}