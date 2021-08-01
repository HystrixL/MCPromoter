namespace MCPromoter
{
    public static class HelpResources
    {
        public static readonly string[] Command =
        {
            "§2========================",
            "calc <表达式>    计算表达式/物品数量",
            "back      返回死亡地点",
            "bot [add|remove|tp|call] <BOT名>   召唤/杀死/传送到/唤来bot",
            "bot list  列出服务器内存在的bot",
            "day [game|server]     查询游戏内/开服天数",
            "entity [count|list]   统计/列出服务器内实体",
            "here      全服报点",
            "item      [clear|count|pick]      清除/统计/拾取服务器内掉落物",
            "ki [true|false|status]   开启/关闭/查询死亡不掉落",
            "kill      自杀(不计入死亡榜)",
            "mg [true|false]   开启/关闭生物破坏",
            "network [ip|port|ping]     查询玩家ip/端口/延迟",
            "om <玩家名> <消息>      向某位离线玩家发送离线消息",
            "qs     发起快速跳过夜晚投票",
            "qs [accept|refuse]     同意/拒绝快速跳过夜晚",
            "qb make <槽位> <注释>      快速备份存档(需指定槽位及注释)(槽位范围1~5)",
            "qb back <槽位>       快速回档服务器(需指定槽位)(槽位范围1~5)",
            "qb restart    快速重启服务器",
            "qb list   查询QuickBackup各槽位信息(存档名、备份时间、注释)",
            "sh <指令>   向控制台注入指令(需特殊授权)",
            "size      获取存档大小",
            "sta <计分板名>    将侧边栏显示调整为特定计分板",
            "sta null      关闭侧边栏显示",
            "sta auto [true|false]      开启/关闭计分板自动切换",
            "system [cpu|memory]    查询服务器CPU/内存占用率",
            "task [add|remove] <任务名>   添加/移除指定任务",
            "tick [倍数|status]      设置/查询随机刻倍数",
            "whitelist [add|remove] <玩家名>     将玩家加入/移出白名单",
            "§2========================"
        };

        public static readonly string[] Setting =
        {
            "§2========================",
            "anticheat [true|false]      开启/关闭反作弊",
            "prefix <newPrefix>     修改插件前缀",
            "staautoswitchesfreq <newFreq>      修改计分板自动切换频率",
            "whitelist [true|false]      开启/关闭插件内置计分板",
            "pluginadmin [true|false]       开启/关闭插件管理员",
            "damagesplash [true|false|int]      开启/关闭/修改剑横扫伤害数量上限",
            "offlinemessage [true|false]    开启/关闭离线消息",
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