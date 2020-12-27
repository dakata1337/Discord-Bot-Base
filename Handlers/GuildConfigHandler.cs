using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Bot.DataStrucs;
using Discord_Bot.Modules;
using Discord_Bot.Services;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace Discord_Bot.Handlers
{
    public class GuildConfigHandler
    {
        private static MySQL _mySQL;
        private static MySqlConnection _connection;
        public GuildConfigHandler(IServiceProvider serviceProvider)
        {
            _mySQL = serviceProvider.GetRequiredService<MySQL>();
            _connection = _mySQL.connection;
        }

        //On Guild Join Create Config
        public async Task JoinedGuild(SocketGuild guild)
        {
            //If the guild has config, delete it
            if (GuildConfigFunctions.GuildHasConfig(guild, _connection))
            {
                GuildConfigFunctions.RemoveGuildConfig(guild, _connection);
            }

            //Guild Defualt Channel
            var defaultChannel = guild.DefaultChannel as SocketTextChannel;

            List<ulong> whitelistedChannels = new List<ulong>();
            if (whitelistedChannels.Count == 0)
            {
                //Add Default Channel ID
                whitelistedChannels.Add(defaultChannel.Id);

                //Create Guild Config
                GuildConfigFunctions.CreateGuildConfig(guild, _connection);

                //Custom Embed
                var fields = new List<EmbedFieldBuilder>();
                fields.Add(new EmbedFieldBuilder
                {
                    Name = "**Please Note**",
                    Value = $"By default, {defaultChannel.Mention} is the default bot channel.\n" +
                    $"If you want to change it, type {GlobalData.Config.defaultPrefix}whitelist add #YourTextChannel",
                    IsInline = false
                });

                //Send Embed
                await defaultChannel.SendMessageAsync(embed:
                    await EmbedHandler.CreateCustomEmbed(
                        guild: guild,
                        color: Color.DarkTeal,
                        fields: fields,
                        embedTitle: "I have arrived!",
                        footer: $"Thank you for choosing {guild.CurrentUser.Username}"
                ));
            }
            await Task.CompletedTask;
        }

        //On Left Guild Delete Config
        public Task LeftGuild(SocketGuild guild)
        {
            if (GuildConfigFunctions.GuildHasConfig(guild, _connection))
            {
                GuildConfigFunctions.RemoveGuildConfig(guild, _connection);
            }
            return Task.CompletedTask;
        }

        public static async Task<Embed> ChangePrefix(SocketCommandContext context, string newPrefix)
        {
            //If prefix lenght is more than 5 chars long
            if (newPrefix.Length > 5)
            {
                return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"The prefix is to long. Must be 5 characters or less.");
            }

            //Get Guild Config
            var config = GuildConfigFunctions.GetGuildConfig(context.Guild, _connection);

            //Get Guild Prefix from Config
            string oldPrefix = config.prefix;

            //If the Selected Prefix is already the Prefix
            if (oldPrefix == newPrefix)
                return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"\"{newPrefix}\" is already the prefix.");

            //Update Config
            GuildConfigFunctions.UpdateGuildConfig(context.Guild, "prefix", newPrefix, _connection);

            return await EmbedHandler.CreateBasicEmbed("Configuration Changed.", $"The prefix was successfully changed to \"{newPrefix}\".");
        }

        public static async Task<Embed> WhiteList(SocketCommandContext context, string arg, IChannel channel)
        {
            //Check if channel is specified ONLY when arg isnt list
            if (arg != "list")
            {
                //If no channel is specified
                if (channel is null)
                    return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"No channel specified.");

                //Checks if the selected channel is text channel
                if (context.Guild.GetChannel(channel.Id) != context.Guild.GetTextChannel(channel.Id))
                    return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"{context.Guild.GetChannel(channel.Id)} is not a text channel.");
            }

            //Get Guild Config
            var guildConfig = GuildConfigFunctions.GetGuildConfig(context.Guild, _connection);

            //Get Whitelisted Channel
            List<ulong> whitelistedChannels = Array.ConvertAll(guildConfig.whitelistedChannel.Split(';'), ulong.Parse).ToList();

            switch (arg)
            {
                #region Add Channel to Whitelist
                case "add":
                    //Limits the whitelisted channels to 5
                    int limit = 5;
                    if (whitelistedChannels.Count > limit) 
                        return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"You have reached the maximum of {limit} whitelisted channels.");

                    //Check If the channel is already whitelisted
                    foreach (ulong item in whitelistedChannels)
                        if (item == channel.Id)
                            return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"{context.Guild.GetChannel(item)} is already whitelisted!");

                    //Add Channel to Whitelist
                    whitelistedChannels.Add(channel.Id);

                    //Update Config
                    GuildConfigFunctions.UpdateGuildConfig(context.Guild, "whitelistedChannel", $"{string.Join(';', whitelistedChannels)}", _connection);

                    return await EmbedHandler.CreateBasicEmbed("Configuration Changed.", $"{context.Guild.GetChannel(channel.Id)} was whitelisted.");
                #endregion

                #region Remove Channel from Whitelist
                case "remove":
                    if (whitelistedChannels.Count == 1)
                        return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"You can't have less than 1 whitelisted channel.");

                    bool notFound = true;
                    foreach (ulong item in whitelistedChannels)
                    {
                        if (item == channel.Id)
                        {
                            notFound = false;
                            break;
                        }
                    }

                    //If the Channel is not Whitelisted
                    if (notFound)
                        return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"{context.Guild.GetChannel(channel.Id)} is not whitelisted.");

                    //Remove Channel from Whitelist
                    whitelistedChannels.Remove(channel.Id);

                    //Update Config
                    GuildConfigFunctions.UpdateGuildConfig(context.Guild, "whitelistedChannel", $"{string.Join(';', whitelistedChannels)}", _connection);

                    return await EmbedHandler.CreateBasicEmbed("Configuration Changed.", $"{context.Guild.GetChannel(channel.Id)} was removed from the whitelist.");
                #endregion

                #region List Whitelisted Channels
                case "list":
                    StringBuilder builder = new StringBuilder();
                    builder.Append("**Whitelisted channels:**\n");
                    for (int i = 0; i < whitelistedChannels.Count; i++)
                    {
                        var whitelistedChannel = context.Guild.GetChannel(whitelistedChannels[i]);
                        builder.Append($"{i + 1}. {whitelistedChannel.Name} (ID: {whitelistedChannel.Id})\n");
                    }
                    return await EmbedHandler.CreateBasicEmbed("Whitelist, List", $"{builder}");
                #endregion

                default:
                    return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"{arg} is not a valid argument.");
            }
        }
    }

    public static class GuildConfigFunctions
    {
        public static void CreateGuildConfig(IGuild guild, MySqlConnection connection)
        {
            var sqlCommands = new string[]
            {
                $"INSERT INTO Guilds VALUES('{guild.Id}')",
                $"INSERT INTO GuildConfigurable VALUES ('{guild.Id}', '{GlobalData.Config.defaultPrefix}', '{guild.DefaultChannelId}')"
            };
            foreach (var sql in sqlCommands)
            {
                using (var cmd = new MySqlCommand(sql, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void RemoveGuildConfig(IGuild guild, MySqlConnection connection)
        {
            var sqlCommands = new string[]
            {
                $"DELETE FROM Guilds WHERE guildID = '{guild.Id}'",
                $"DELETE FROM GuildConfigurable WHERE guildID = '{guild.Id}'"
            };

            foreach (var sql in sqlCommands)
            {
                using (var cmd = new MySqlCommand(sql, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static GuildConfig GetGuildConfig(IGuild guild, MySqlConnection connection)
        {
            var guildConfig = new GuildConfig();
            var command = $"SELECT * FROM GuildConfigurable WHERE guildID = '{guild.Id}'";
            using (var cmd = new MySqlCommand(command, connection))
            {
                using (var data_reader = cmd.ExecuteReader())
                {
                    while (data_reader.Read())
                    {
                        guildConfig.prefix = data_reader.GetValue(1).ToString();
                        guildConfig.whitelistedChannel = data_reader.GetValue(2).ToString();
                    }
                }
            }
            return guildConfig;
        }

        public static void UpdateGuildConfig(IGuild guild, string whatToUpdate, string value, MySqlConnection connection)
        {
            string command = $"UPDATE GuildConfigurable SET {whatToUpdate} = '{value}' WHERE guildID = '{guild.Id}'";
            MySqlCommand cmd = new MySqlCommand(command, connection);
            cmd.ExecuteNonQuery();
        }

        public static bool GuildHasConfig(IGuild guild, MySqlConnection connection)
        {
            var command = $"SELECT EXISTS(SELECT * FROM GuildConfigurable WHERE guildID = '{guild.Id}')";
            using (var cmd = new MySqlCommand(command, connection))
            {
                using (var data_reader = cmd.ExecuteReader())
                {
                    while (data_reader.Read())
                    {
                        if (data_reader.GetValue(0).ToString() == "1")
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
