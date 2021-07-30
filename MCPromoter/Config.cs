using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace MCPromoter
{
    public class RawConfig
    {
        public static readonly string rawConfig = 
$@"### ===============
### Basic Configure
### ===============

# 存档名称
WorldName: ''
# 服务器开启时间
WorldStartDate: '{DateTime.Now.ToString()}'
# 加载器类型    LiteLoader/DTConsole/BedrockX/BDXCore/ElementZero/Customization
PluginLoader:
  Type: 'Customization'
  CustomizationPath: ''
# 指令前缀
CmdPrefix: '@'
# 自杀信息
SuicideMessages:
  - '{{}}进入了通向二次元的入口'
  - '不要停下来啊，{{}}！'
# 计分板自动切换频率（秒）
StaAutoSwitchesFreq: 60
# 借助WebSocket对接FakePlayer
FakePlayer:
  Address: ''
  Port:  ''
# 每日自动备份时间
AutoBackupTime: '3:00'
# 剑横扫伤害数量上限
MaxDamageSplash: 10

### ===========
### Permissions
### ===========

# 插件内置白名单
WhiteList:
  Enable: true
  PlayerList:
    - Name: 'Steve'
      Xuid: '0000000000000000'
# 插件管理员权限
PluginAdmin:
  Enable: true
  AdminList:
    - Name: 'Steve'
      Xuid: '0000000000000000'
  AdminCmd:
    - 'rc'
    - 'whitelist'
    - 'mcp setting'
# 反作弊
AntiCheat:
  Enable: true
  ForceGamemode: true
  AllowedCmd:
    - '/help'
    - '/tickingarea'
    - '/list'
    - '/me'
    - '/mixer'
    - '/msg'
    - '/tell'
    - '/w'
    - '/tp'

### =======
### Futures
### =======

# 禁用插件功能
PluginDisable:
  Commands:
    - ' '
  Futures:
    Statistics:
      Killed: false
      Placed: false
      Excavation: false
      Death: false
      OnlineMinutes: false
    SuicideMessages: false
    DeathPointReport: false
    AutoBackupServer: false
    SplashDamage: false
    OfflineMessage: false

# 日志记录
Logging:
  Plugin: true
  Chat: true
  Command: true
  PlayerOnlineOffline: true
# 控制台输出
ConsoleOutput:
  Plugin: true
  Chat: true
  Command: true
  PlayerOnlineOffline: true
";
    }
    
    public class Config
    {
        [YamlMember(Alias = "WorldName", ApplyNamingConventions = false)]
        public string WorldName { get; set; }//存档名称
        [YamlMember(Alias = "WorldStartDate", ApplyNamingConventions = false)]
        public string WorldStartDate { get; set; }//服务器开启时间
        [YamlMember(Alias = "PluginLoader", ApplyNamingConventions = false)]
        public PluginLoader PluginLoader { get; set; }//加载器类型
        [YamlMember(Alias = "CmdPrefix", ApplyNamingConventions = false)]
        public string CmdPrefix { get; set; }//插件指令前缀
        [YamlMember(Alias = "SuicideMessages", ApplyNamingConventions = false)]
        public string[] SuicideMessages { get; set; }//自定义自杀信息
        [YamlMember(Alias = "StaAutoSwitchesFreq", ApplyNamingConventions = false)]
        public int StaAutoSwitchesFreq { get; set; }//计分板自动切换频率
        [YamlMember(Alias = "FakePlayer", ApplyNamingConventions = false)]
        public FakePlayer FakePlayer { get; set; }
        [YamlMember(Alias = "AutoBackupTime", ApplyNamingConventions = false)]
        public string AutoBackupTime { get; set; }
        [YamlMember(Alias = "MaxDamageSplash", ApplyNamingConventions = false)]
        public int MaxDamageSplash { get; set; }
        [YamlMember(Alias = "WhiteList", ApplyNamingConventions = false)]
        public WhiteList WhiteList { get; set; }//插件内置白名单
        [YamlMember(Alias = "PluginAdmin", ApplyNamingConventions = false)]
        public PluginAdmin PluginAdmin { get; set; }//插件管理员权限
        [YamlMember(Alias = "AntiCheat", ApplyNamingConventions = false)]
        public AntiCheat AntiCheat { get; set; }//反作弊系统
        [YamlMember(Alias = "PluginDisable", ApplyNamingConventions = false)]
        public PluginDisable PluginDisable { get; set; }//插件禁用
        [YamlMember(Alias = "Logging", ApplyNamingConventions = false)]
        public Logging Logging { get; set; }//日志记录
        [YamlMember(Alias = "ConsoleOutput", ApplyNamingConventions = false)]
        public ConsoleOutput ConsoleOutput { get; set; }//控制台输出
    }
    
    public class PluginLoader
    {
        [YamlMember(Alias = "Type", ApplyNamingConventions = false)]
        public string Type { get; set; }//加载器类型
        [YamlMember(Alias = "CustomizationPath", ApplyNamingConventions = false)]
        public string CustomizationPath { get; set; }//自定义加载器路径
    }

    public class FakePlayer
    {
        [YamlMember(Alias = "Address", ApplyNamingConventions = false)]
        public string Address { get; set; }
        [YamlMember(Alias = "Port", ApplyNamingConventions = false)]
        public string Port { get; set; }
    }

    public class Logging
    {
        [YamlMember(Alias = "Plugin", ApplyNamingConventions = false)]
        public bool Plugin { get; set; }//插件
        [YamlMember(Alias = "Chat", ApplyNamingConventions = false)]
        public bool Chat { get; set; }//玩家对话
        [YamlMember(Alias = "Command", ApplyNamingConventions = false)]
        public bool Command { get; set; }//指令
        [YamlMember(Alias = "PlayerOnlineOffline", ApplyNamingConventions = false)]
        public bool PlayerOnlineOffline { get; set; }//玩家上下线
    }

    public class ConsoleOutput
    {
        [YamlMember(Alias = "Plugin", ApplyNamingConventions = false)]
        public bool Plugin { get; set; }//插件
        [YamlMember(Alias = "Chat", ApplyNamingConventions = false)]
        public bool Chat { get; set; }//玩家对话
        [YamlMember(Alias = "Command", ApplyNamingConventions = false)]
        public bool Command { get; set; }//指令
        [YamlMember(Alias = "PlayerOnlineOffline", ApplyNamingConventions = false)]
        public bool PlayerOnlineOffline { get; set; }//玩家上下线
    }

    public class WhiteList
    {
        [YamlMember(Alias = "Enable", ApplyNamingConventions = false)]
        public bool Enable { get; set; }//启用
        [YamlMember(Alias = "PlayerList", ApplyNamingConventions = false)]
        public List<Player> PlayerList { get; set; }//玩家名单
    }

    public class Player
    {
        [YamlMember(Alias = "Name", ApplyNamingConventions = false)]
        public string Name { get; set; }//玩家名
        [YamlMember(Alias = "Xuid", ApplyNamingConventions = false)]
        public string Xuid { get; set; }//玩家Xuid
    }

    public class PluginAdmin
    {
        [YamlMember(Alias = "Enable", ApplyNamingConventions = false)]
        public bool Enable { get; set; }//启用
        [YamlMember(Alias = "AdminList", ApplyNamingConventions = false)]
        public List<Player> AdminList { get; set; }//管理员名单
        [YamlMember(Alias = "AdminCmd", ApplyNamingConventions = false)]
        public string[] AdminCmd { get; set; }//管理员指令
    }

    public class AntiCheat
    {
        [YamlMember(Alias = "Enable", ApplyNamingConventions = false)]
        public bool Enable { get; set; }//启用
        [YamlMember(Alias = "AllowedCmd", ApplyNamingConventions = false)]
        public string[] AllowedCmd { get; set; }//允许的指令
        [YamlMember(Alias = "ForceGamemode", ApplyNamingConventions = false)]
        public bool ForceGamemode { get; set; }//强制游戏模式
    }

    public class PluginDisable
    {
        [YamlMember(Alias = "Commands", ApplyNamingConventions = false)]
        public string[] Commands { get; set; }//指令
        [YamlMember(Alias = "Futures", ApplyNamingConventions = false)]
        public Futures Futures { get; set; }//功能
    }


    public class Futures
    {
        [YamlMember(Alias = "Statistics", ApplyNamingConventions = false)]
        public Statistics Statistics { get; set; }//计分板
        [YamlMember(Alias = "SuicideMessages", ApplyNamingConventions = false)]
        public bool SuicideMessages { get; set; }//自定义自杀信息
        [YamlMember(Alias = "DeathPointReport", ApplyNamingConventions = false)]
        public bool DeathPointReport { get; set; }//死亡点报告
        [YamlMember(Alias = "AutoBackup", ApplyNamingConventions = false)]
        public bool AutoBackup { get; set; }//自动备份
        [YamlMember(Alias = "SplashDamage", ApplyNamingConventions = false)]
        public bool SplashDamage { get; set; }//伤害溅射
        [YamlMember(Alias = "OfflineMessage", ApplyNamingConventions = false)]
        public bool OfflineMessage { get; set; }//离线消息
        [YamlIgnore]
        public bool QuickBackup { get; set; }//快速备份是否可用
    }

    public class Statistics
    {
        [YamlMember(Alias = "Killed", ApplyNamingConventions = false)]
        public bool Killed { get; set; }//击杀榜
        [YamlMember(Alias = "Placed", ApplyNamingConventions = false)]
        public bool Placed { get; set; }//放置榜
        [YamlMember(Alias = "Excavation", ApplyNamingConventions = false)]
        public bool Excavation { get; set; }//挖掘榜
        [YamlMember(Alias = "Death", ApplyNamingConventions = false)]
        public bool Death { get; set; }//死亡榜
        [YamlMember(Alias = "OnlineMinutes", ApplyNamingConventions = false)]
        public bool OnlineMinutes { get; set; }//在线时间统计
    }
}