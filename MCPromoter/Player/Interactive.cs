﻿using CSR;
using static MCPromoter.Output;

namespace MCPromoter
{
    partial class MCPromoter
    {
        public static bool MobDiePlugin(Events x)
        {
            var e = BaseEvent.getFrom(x) as MobDieEvent;
            if (e == null) return true;

            var attackName = e.srcname;
            var attackType = e.srctype;
            var deadName = e.mobname;
            var deadType = e.mobtype;
            if (!Configs.PluginDisable.Futures.Statistics.Killed)
            {
                if (attackType == "entity.player.name")
                {
                    Api.runcmd($"scoreboard players add @a[name={attackName},tag=!BOT] Killed 1");
                }
            }

            if (deadType == "entity.player.name")
            {
                if (!Configs.PluginDisable.Futures.DeathPointReport)
                {
                    StandardizedFeedback("@a",
                        $"§r§l§f{deadName}§r§o§4 死于 §r§l§f{e.dimension}[{(int)e.XYZ.x},{(int)e.XYZ.y},{(int)e.XYZ.z}]");
                }

                if (deadName.StartsWith("bot_")) return true;

                playerDatas[deadName].DeadPos = e.XYZ;
                playerDatas[deadName].DeadWorld = e.dimension;
                playerDatas[deadName].DeadEnable = true;
                if (playerDatas[deadName].IsSuicide)
                {
                    playerDatas[deadName].IsSuicide = false;
                }
                else
                {
                    if (!Configs.PluginDisable.Futures.Statistics.Death)
                    {
                        Api.runcmd($"scoreboard players add @a[tag=!BOT,name={deadName}] Dead 1");
                    }
                }
            }

            return true;
        }

        public static bool DestroyBlockPlugin(Events x)
        {
            if (Configs.PluginDisable.Futures.Statistics.Excavation) return true;
            var e = BaseEvent.getFrom(x) as DestroyBlockEvent;
            if (e == null) return true;

            var name = e.playername;
            if (!string.IsNullOrEmpty(name))
            {
                Api.runcmd($"scoreboard players add @a[name={name},tag=!BOT] Dig 1");
            }

            return true;
        }

        public static bool PlacedBlockPlugin(Events x)
        {
            if (Configs.PluginDisable.Futures.Statistics.Placed) return true;
            var e = BaseEvent.getFrom(x) as PlacedBlockEvent;
            if (e == null) return true;

            var name = e.playername;
            if (!string.IsNullOrEmpty(name))
            {
                Api.runcmd($"scoreboard players add @a[name={name},tag=!BOT] Placed 1");
            }

            return true;
        }
    }
}