using CSR;

namespace MCPromoter
{
    partial class MCPromoter
    {
        public static bool MobDiePlugin(Events x)
        {
            var e = BaseEvent.getFrom(x) as MobDieEvent;
                if (e == null) return true;

                string attackName = e.srcname;
                string attackType = e.srctype;
                string deadName = e.mobname;
                string deadType = e.mobtype;
                if (!config.PluginDisable.Futures.Statistics.Killed)
                {
                    if (attackType == "entity.player.name")
                    {
                        _mapi.runcmd($"scoreboard players add @a[name={attackName},tag=!BOT] Killed 1");
                    }
                }

                if (deadType == "entity.player.name")
                {
                    var deadPosition = (x: ((int) e.XYZ.x).ToString(), y: ((int) e.XYZ.y).ToString(),
                        z: ((int) e.XYZ.z).ToString(), world: e.dimension);
                    if (!config.PluginDisable.Futures.DeathPointReport)
                    {
                        StandardizedFeedback("@a",
                            $"§r§l§f{deadName}§r§o§4 死于 §r§l§f{deadPosition.world}[{deadPosition.x},{deadPosition.y},{deadPosition.z}]");
                    }

                    if (deadName.StartsWith("bot_")) return true;

                    playerDatas[deadName].DeadX = deadPosition.x;
                    playerDatas[deadName].DeadY = deadPosition.y;
                    playerDatas[deadName].DeadZ = deadPosition.z;
                    playerDatas[deadName].DeadWorld = deadPosition.world;
                    playerDatas[deadName].DeadEnable = true;
                    if (playerDatas[deadName].IsSuicide)
                    {
                        playerDatas[deadName].IsSuicide = false;
                    }
                    else
                    {
                        if (!config.PluginDisable.Futures.Statistics.Death)
                        {
                            _mapi.runcmd($"scoreboard players add @a[tag=!BOT,name={deadName}] Dead 1");
                        }
                    }
                }
                return true;
        }
        
        public static bool DestroyBlockPlugin(Events x)
        {
            if (config.PluginDisable.Futures.Statistics.Excavation) return true;
            var e = BaseEvent.getFrom(x) as DestroyBlockEvent;
            if (e == null) return true;

            string name = e.playername;
            if (!string.IsNullOrEmpty(name))
            {
                _mapi.runcmd($"scoreboard players add @a[name={name},tag=!BOT] Dig 1");
            }

            return true;
        }

        public static bool PlacedBlockPlugin(Events x)
        {
            if (config.PluginDisable.Futures.Statistics.Placed) return true;
                            var e = BaseEvent.getFrom(x) as PlacedBlockEvent;
                            if (e == null) return true;
                            
                            string name = e.playername;
                            if (!string.IsNullOrEmpty(name))
                            {
                                _mapi.runcmd($"scoreboard players add @a[name={name},tag=!BOT] Placed 1");
                            }
            
                            return true;
        }
    }
}