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

            string name = e.playername;
            string cmd = e.cmd;
            if (Configs.Logging.Command) LogsWriter(name, cmd);
            if (Configs.ConsoleOutput.Command) ConsoleOutputter(name, cmd);

            if (!Configs.AntiCheat.Enable) return true;
            foreach (var allowedCmd in Configs.AntiCheat.AllowedCmd)
            {
                if (cmd.StartsWith(allowedCmd))
                {
                    return true;
                }
            }

            Api.runcmd($"kick {name} 试图违规使用{cmd}被踢出");
            StandardizedFeedback("@a", $"{name}试图违规使用{cmd}被踢出");
            return false;
        }
    }
}