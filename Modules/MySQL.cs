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
        public void Initialize(ServiceProvider _services)
        {
            _commandHandler = _services.GetRequiredService<CommandHandler>();
            _client = _services.GetRequiredService<DiscordSocketClient>();

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

                Thread guildPrefixUpdate = new Thread(new ThreadStart(UpdateGuildConfigs));
                guildPrefixUpdate.Start();


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
                        MySqlCommand cmd = new MySqlCommand($"SELECT * FROM GuildConfigurable WHERE guildID = '{guild.Id}'", connection);

                        using (var data_reader = cmd.ExecuteReader())
                        {
                            if (!data_reader.HasRows)
                                continue;

                            var count = data_reader.FieldCount;
                            while (data_reader.Read())
                            {
                                var guildConfig = new GuildConfig();

                                guildConfig.prefix = data_reader.GetValue(1).ToString();
                                guildConfig.whitelistedChannel = data_reader.GetValue(2).ToString();

                                Configs.Add(guild.Id, guildConfig);
                            }
                        }
                        
                    }
                    catch (Exception e)
                    {
                        LoggingService.Log("UGC", e.Message, ConsoleColor.Red);
                    }
                }

                //LoggingService.Log("UGC", $"Updated all guild configs ({Configs.Count})");
                _commandHandler.GuildConfigs = Configs;

                Thread.Sleep(GlobalData.Config.DB_Updatetime);
            }
        }
    }
}
