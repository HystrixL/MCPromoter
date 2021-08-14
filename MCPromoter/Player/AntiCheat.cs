using System.Linq;
using CSR;
using static MCPromoter.Output;

namespace MCPromoter
{
    partial class MCPromoter
    {
        public static bool InputCommandPlugin(Events x)
        {
            var e = BaseEvent.getFrom(x) as InputCommandEvent;
            if (e == null) return true;

            var name = e.playername;
            var cmd = e.cmd;
            if (Configs.Logging.Command) LogsWriter(name, cmd);
            if (Configs.ConsoleOutput.Command) ConsoleOutputter(name, cmd);

            if (!Configs.AntiCheat.Enable) return true;
            if (Configs.AntiCheat.AllowedCmd.Any(allowedCmd => cmd.StartsWith(allowedCmd))) return true;

            Api.runcmd($"kick {name} 试图违规使用{cmd}被踢出");
            StandardizedFeedback("@a", $"{name}试图违规使用{cmd}被踢出");
            return false;
        }
    }
}