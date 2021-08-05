using CSR;

namespace MCPromoter
{
    public delegate void Command(string[] args,InputTextEvent e,MCCSAPI api);
    
    public static class CommandManager
    {
        public static void addCommand(string keyWord,Command command)
        {
            MCPromoter.Commands.Add(keyWord,command);
        }
        
        public static void addCommandHelp(string keyWord,string[] helpContent)
        {
            MCPromoter.CommandHelps.Add(keyWord,helpContent);
        }
    }
}