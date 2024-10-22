﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using WebSocketSharp;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static MCPromoter.Output;

namespace MCPromoter
{
    partial class MCPromoter
    {
        public static void InitializePlugin()
        {
            var pluginRootDirectory = new DirectoryInfo(PluginPath.RootPath);
            if (!pluginRootDirectory.Exists)
            {
                pluginRootDirectory.Create();
            }

            var logsRootDirectory = new DirectoryInfo(PluginPath.LogsRootPath);
            if (!logsRootDirectory.Exists)
            {
                logsRootDirectory.Create();
            }

            var qbRootPath = new DirectoryInfo(PluginPath.QbRootPath);
            if (!qbRootPath.Exists)
            {
                qbRootPath.Create();
                File.Create(PluginPath.QbInfoPath);
                File.Create(PluginPath.QbLogPath);
                var qbIniFile = new IniFile(PluginPath.QbInfoPath);
                for (var i = 0; i < 6; i++)
                {
                    string slot;
                    if (i == 0)
                    {
                        slot = "AUTO";
                    }
                    else
                    {
                        slot = i.ToString();
                    }

                    qbIniFile.IniWriteValue(slot, "WorldName", "null");
                    qbIniFile.IniWriteValue(slot, "BackupTime", "null");
                    qbIniFile.IniWriteValue(slot, "Comment", "null");
                    qbIniFile.IniWriteValue(slot, "Size", "0");
                }

                ConsoleOutputter("MCP", $@"请将QuickBackup.exe放入{PluginPath.QbHelperPath}以启用QuickBackup");
                LogsWriter("MCP", $@"请将QuickBackup.exe放入{PluginPath.QbHelperPath}以启用QuickBackup");
            }

            File.WriteAllText(PluginPath.PlayerDatasPath, javaScriptSerializer.Serialize(playerDatas));
            File.WriteAllText(PluginPath.ConfigPath, RawConfig.rawConfig);

            ConsoleOutputter("MCP", $@"已完成插件配置文件的初始化.配置文件位于{PluginPath.ConfigPath} .请完成配置文件后重启服务器.");
            LogsWriter("MCP", $@"已完成插件配置文件的初始化.配置文件位于{PluginPath.ConfigPath} .请完成配置文件后重启服务器.");
        }

        public static void LoadPlugin(bool isFirstLoad = false)
        {
            if (!File.Exists(PluginPath.ConfigPath)) InitializePlugin();
            string configText = File.ReadAllText(PluginPath.ConfigPath);
            Configs = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build()
                .Deserialize<Config>(configText);

            if (isFirstLoad)
            {
                string savedPlayerDatas = File.ReadAllText(PluginPath.PlayerDatasPath);
                if (!string.IsNullOrWhiteSpace(savedPlayerDatas))
                    playerDatas = javaScriptSerializer.Deserialize<Dictionary<string, PlayerDatas>>(savedPlayerDatas);
            }

            if (!Directory.Exists(PluginPath.QbRootPath) || !File.Exists(PluginPath.QbHelperPath))
            {
                ConsoleOutputter("MCP", "快速备份QuickBackup核心组件丢失，@qb无法使用");
                LogsWriter("MCP", "快速备份QuickBackup核心组件丢失，@qb无法使用");
                ConsoleOutputter("MCP", $@"请将QuickBackup.exe放入{PluginPath.QbRootPath}以启用QuickBackup");
                LogsWriter("MCP", $@"请将QuickBackup.exe放入{PluginPath.QbRootPath}以启用QuickBackup");
                Configs.PluginDisable.Futures.QuickBackup = true;
            }

            if (Configs.WorldName.Contains(" "))
            {
                ConsoleOutputter("MCP", "存档名包含空格,QuickBackup无法工作!请进行修改.");
                LogsWriter("MCP", "存档名包含空格,QuickBackup无法工作!请进行修改.");
                Configs.PluginDisable.Futures.QuickBackup = true;
            }

            if (!Directory.Exists($@"worlds\{Configs.WorldName}"))
            {
                ConsoleOutputter("MCP", "找不到指定存档,QuickBackup无法工作!请检查配置文件.");
                LogsWriter("MCP", "找不到指定存档,QuickBackup无法工作!请检查配置文件.");
                Configs.PluginDisable.Futures.QuickBackup = true;
            }

            if (Configs.PluginDisable.Futures.Statistics.OnlineMinutes)
            {
                if (onlineMinutesAccTimer != null)
                {
                    onlineMinutesAccTimer.Dispose();
                }
            }
            else
            {
                onlineMinutesAccTimer = new Timer(60000);
                onlineMinutesAccTimer.Elapsed += OnlineMinutesAcc;
                onlineMinutesAccTimer.AutoReset = true;
                onlineMinutesAccTimer.Start();
            }

            if (!Configs.AntiCheat.Enable || !Configs.AntiCheat.ForceGamemode)
            {
                if (forceGamemodeTimer != null)
                {
                    forceGamemodeTimer.Dispose();
                }
            }
            else
            {
                forceGamemodeTimer = new Timer(2000);
                forceGamemodeTimer.Elapsed += ForceGamemode;
                forceGamemodeTimer.AutoReset = true;
                forceGamemodeTimer.Start();
            }

            if (Configs.PluginDisable.Futures.AutoBackupServer || Configs.PluginDisable.Futures.QuickBackup)
            {
                if (autoBackupTimer != null)
                {
                    autoBackupTimer.Dispose();
                }
            }
            else
            {
                autoBackupTimer = new Timer(60000);
                autoBackupTimer.Elapsed += AutoBackup;
                autoBackupTimer.AutoReset = true;
                autoBackupTimer.Start();
            }

            switch (Configs.PluginLoader.Type)
            {
                case "DTConsole":
                    Configs.PluginLoader.CustomizationPath = @"..\MCModDllExe\debug.bat";
                    break;
                case "LiteLoader":
                case "BedrockX":
                case "BDXCore":
                    Configs.PluginLoader.CustomizationPath = @"bedrock_server.exe";
                    break;
                case "ElementZero":
                    Configs.PluginLoader.CustomizationPath = @"bedrock_server_mod.exe";
                    break;
                default:
                    Configs.PluginLoader.CustomizationPath = Configs.PluginLoader.CustomizationPath;
                    break;
            }

            if (!File.Exists(Configs.PluginLoader.CustomizationPath))
            {
                ConsoleOutputter("MCP", "找不到指定的插件加载器,QuickBackup无法重启服务器!请检查配置文件.");
                LogsWriter("MCP", "找不到指定的插件加载器,QuickBackup无法重启服务器!请检查配置文件.");
            }

            // bool webSocketStatus;
            if (!Configs.PluginDisable.Futures.FakePlayer)
            {
                try
                {
                    webSocket = new WebSocket($@"ws://{Configs.FakePlayer.Address}:{Configs.FakePlayer.Port}");
                    webSocket.OnMessage += BotListener;
                    webSocket.Connect();
                }
                catch
                {
                    ConsoleOutputter("MCP", "无法连接至FakePlayer的WebSocket服务器,请检查设置.");
                    LogsWriter("MCP", "无法连接至FakePlayer的WebSocket服务器,请检查设置.");
                    Configs.PluginDisable.Futures.FakePlayer = true;
                }
            }

            ConsoleOutputter("MCP", "已完成初始化。");
            LogsWriter("MCP", "已完成初始化。");
        }
    }
}