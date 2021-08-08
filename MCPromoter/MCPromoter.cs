using System;
using CSR;
using MCPromoter;

namespace MCPromoter
{
    public partial class MCPromoter
    {
        public static void Init(MCCSAPI api)
        {
            Api = api;
            LoadPlugin(true);
            CommandInitialization();

            api.addAfterActListener(EventKey.onInputText, InputTextPlugin);
            api.addAfterActListener(EventKey.onAttack, AttackPlugin);
            api.addAfterActListener(EventKey.onMobDie, MobDiePlugin);
            api.addAfterActListener(EventKey.onDestroyBlock, DestroyBlockPlugin);
            api.addAfterActListener(EventKey.onPlacedBlock, PlacedBlockPlugin);
            api.addBeforeActListener(EventKey.onServerCmdOutput, ServerCmdOutputPlugin);
            api.addBeforeActListener(EventKey.onInputCommand, InputCommandPlugin);
            api.addBeforeActListener(EventKey.onServerCmd, ServerCmdPlugin);
            api.addAfterActListener(EventKey.onLoadName, LoadNamePlugin);
            api.addAfterActListener(EventKey.onPlayerLeft, PlayerLeftPlugin);
        }
    }
}

namespace CSR
{
    partial class Plugin
    {
        public static void onStart(MCCSAPI api)
        {
            MCPromoter.MCPromoter.Init(api);
            Console.WriteLine($"[{PluginInfo.Name} - {PluginInfo.Version}]Loaded.");
        }
    }
}