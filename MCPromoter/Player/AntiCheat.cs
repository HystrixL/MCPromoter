using CSR;

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
            if (config.Logging.Command) LogsWriter(name, cmd);
            if (config.ConsoleOutput.Command) ConsoleOutputter(name, cmd);

            if (!config.AntiCheat.Enable) return true;
            foreach (var allowedCmd in config.AntiCheat.AllowedCmd)
            {
                if (cmd.StartsWith(allowedCmd))
                {
                    return true;
                }
            }

            _mapi.runcmd($"kick {name} 试图违规使用{cmd}被踢出");
            StandardizedFeedback("@a", $"{name}试图违规使用{cmd}被踢出");
            return false;
        }
    }
}