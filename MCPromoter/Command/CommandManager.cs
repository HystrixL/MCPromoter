using CSR;

namespace MCPromoter
{
    public delegate bool Command(string[] args,InputTextEvent e,MCCSAPI api);
    
    public static class CommandManager
    {
        public static void addCommand(string keyWord,Command command)
        {
            MCPromoter.Commands.Add(keyWord,command);
        }
        
        public static void addCommandHelp(string keyWord,string helpContent)
        {
            HelpResources.Command.Add(keyWord,helpContent);
        }
    }
}