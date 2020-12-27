﻿using Discord;
using Discord.WebSocket;
using Discord_Bot.DataStrucs;
using Discord_Bot.Handlers;
using Discord_Bot.Services;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discord_Bot.Modules
{
    public class MySQL
    {
        public MySqlConnection connection;
        private CommandHandler _commandHandler;
        private DiscordSocketClient _client;

        public MySQL(IServiceProvider _services)
        {
            _commandHandler = _services.GetRequiredService<CommandHandler>();
            _client = _services.GetRequiredService<DiscordSocketClient>();

            Initialize();
        }

        public void Initialize()
        {
            var config = GlobalData.Config;     
            try
            {
                var connStr = new MySqlConnectionStringBuilder()
                {
                    Server = config.DB_Server,
                    Port = uint.Parse(config.DB_Port),
                    UserID = config.DB_User,
                    Password = config.DB_Password,
                    Database = config.DB_Database
                };

                connection = new MySqlConnection(connStr.GetConnectionString(true));
                connection.Open();

                Thread guildConfigUpdate = new Thread(new ThreadStart(UpdateGuildConfigs));
                guildConfigUpdate.Start();


                LoggingService.Log("MySQL", $"Succsessfully connected to {config.DB_Server}:{config.DB_Port} - Database: {config.DB_Database}");
            }
            catch (Exception ex)
            {
                LoggingService.Log("MySQL", "An exception was caught! Error message:\n" + ex.Message, ConsoleColor.Red);
                Thread.Sleep(-1);
            }
        }

        private void UpdateGuildConfigs()
        {
            while (true)
            {
                Dictionary<ulong, GuildConfig> Configs = new Dictionary<ulong, GuildConfig>();
                foreach (var guild in _client.Guilds)
                {
                    try
                    {
                        if (GuildConfigFunctions.GuildHasConfig(guild, connection))
                        {
                            Configs.Add(guild.Id, GuildConfigFunctions.GetGuildConfig(guild, connection));
                        }
                        else
                        {
                            GuildConfigFunctions.CreateGuildConfig(guild, connection);

                            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
                            fields.Add(new EmbedFieldBuilder
                            {
                                Name = "**I'm sorry for being late**",
                                Value = $"We had some technical difficulties.\n" +
                                $"Everything should be normal by now.",
                                IsInline = false
                            });

                            fields.Add(new EmbedFieldBuilder
                            {
                                Name = "**Please Note**",
                                Value = $"By default, {guild.DefaultChannel.Mention} is the default bot channel.\n" +
                                $"If you want to change it, type {GlobalData.Config.defaultPrefix}whitelist add #YourTextChannel",
                                IsInline = false
                            });

                            Task.Run(async () =>
                            {
                                await guild.DefaultChannel.SendMessageAsync(embed: await EmbedHandler.CreateCustomEmbed(
                                    guild: guild,
                                    embedTitle: "Oh oh..",
                                    fields: fields,
                                    color: Color.DarkTeal,
                                    footer: $"Thank you for choosing {guild.CurrentUser.Username}"
                                ));
                            });
                            
                        }
                    }
                    catch (Exception e)
                    {
                        LoggingService.Log("UGC", e.Message, ConsoleColor.Red);
                    }
                }

                //LoggingService.Log("UGC", $"Updated all guild configs ({Configs.Count})");
                _commandHandler.GuildConfigs = Configs;

                Thread.Sleep(GlobalData.Config.cacheUpdateTime);
            }
        }
    }
}
