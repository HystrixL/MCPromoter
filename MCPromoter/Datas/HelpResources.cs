using System.Collections.Generic;

namespace MCPromoter
{
    public static class HelpResources
    {
        public static Dictionary<string, string> Command = new Dictionary<string, string>();

        public static readonly string[] Setting =
        {
            "§2========================",
            "AntiCheat [true|false]      开启/关闭反作弊",
            "prefix <newPrefix>     修改插件前缀",
            "StaAutoSwitchesFreq <newFreq>      修改计分板自动切换频率",
            "Whitelist [true|false]      开启/关闭插件内置计分板",
            "PluginAdmin [true|false]       开启/关闭插件管理员",
            "DamageSplash [true|false|int]      开启/关闭/修改剑横扫伤害数量上限",
            "OfflineMessage [true|false]    开启/关闭离线消息",
            "§2========================"
        };

        public static readonly string[] Status =
        {
            "§2========================",
            $"§c§l{PluginInfo.Name} - {PluginInfo.Version}",
            $"§o作者：{PluginInfo.Author}",
            "§2========================",
            "mcp help      获取MCP模块帮助",
            "mcp initialize        初始化MCP模块",
            "mcp setting [option] [value]       修改MCP模块设置",
            "mcp setting reload      重载MCP配置文件",
            "§2========================"
        };
    }
}