using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Bot.DataStrucs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Discord_Bot.Handlers
{
    public class GuildConfigHandler
    {
        //On Guild Join Create Config
        public async Task JoinedGuild(SocketGuild guild)
        {
            string configLocation = $@"{GlobalData.Config.guildConfigLocation}\{guild.Id}.json";

            //If Config Location doesn't exist, create it
            if (!Directory.Exists(GlobalData.Config.guildConfigLocation))
                Directory.CreateDirectory(GlobalData.Config.guildConfigLocation);

            //If the guild has config, delete it
            if (File.Exists(configLocation))
                File.Delete(configLocation);

            string json = JsonConvert.SerializeObject(GenerateNewConfig(GlobalData.Config.defaultPrefix), Formatting.Indented);
            var jObj = JsonConvert.DeserializeObject<JObject>(json);

            //Guild Defualt Channel
            var defaultChannel = guild.DefaultChannel as SocketTextChannel;

            //If there are 0 Whitelisted Channels add the Default Channel to Whitelist
            if (jObj["whitelistedChannels"].Value<JArray>().Count == 0)
            {
                ulong[] ts = { defaultChannel.Id };
                jObj["whitelistedChannels"] = JToken.FromObject(ts);
            }

            //Save Guild Config
            GuildConfigFunctions.SaveGuildConfig(guild, jObj);

            //Custome Embed
            var fields = new List<EmbedFieldBuilder>();
            fields.Add(new EmbedFieldBuilder
            {
                Name = "**NOTE**",
                Value = $"By default, {defaultChannel.Mention} is the default bot channel.\n" +
                $"If you want to change it go to the channel and type {jObj["prefix"]}prefix YourPrefixHere",
                IsInline = false
            });

            //Send Embed
            await defaultChannel.SendMessageAsync(embed:
                await EmbedHandler.CreateCustomEmbed(
                    guild: guild,
                    color: Color.Blue,
                    fields: fields,
                    embedTitle: "I have arrived!", 
                    footer: $"Thank you for choosing {guild.CurrentUser.Username}"
                )); 

            await Task.CompletedTask;
        }

        //Generate Config
        public GuildConfig GenerateNewConfig(string prefix) => new GuildConfig
        {
            prefix = prefix,
            whitelistedChannels = new List<ulong>()
        };

        //On Left Guild Delete Config
        public Task LeftGuild(SocketGuild guild)
        {
            string configFile = GuildConfigFunctions.GetConfigLocation(guild);
            if (File.Exists(configFile))
                File.Delete(configFile);

            return Task.CompletedTask;
        }

        public static async Task<Embed> ChangePrefix(SocketCommandContext context, string prefix)
        {
            //If prefix lenght is more than 5 chars long
            if (prefix.Length > 5)
            {
                return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"The prefix is to long. Must be 15 characters or less.");
            }

            //Get Guild Config
            var jObj = GuildConfigFunctions.GetGuildConfig(context.Guild);

            //If the Selected Prefix is already the Prefix
            if ((string)jObj["prefix"] == prefix)
                return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"\"{prefix}\" is already the prefix.");

            //Update Config
            jObj["prefix"] = prefix;

            //Save Config
            GuildConfigFunctions.SaveGuildConfig(context.Guild, jObj);
            return await EmbedHandler.CreateBasicEmbed("Configuration Changed.", $"The prefix was successfully changed to \"{prefix}\".");
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
            var jObj = GuildConfigFunctions.GetGuildConfig(context.Guild);

            //Get Whitelisted Channel
            ulong[] whitelistedChannels = jObj["whitelistedChannels"].ToObject<ulong[]>();
            List<ulong> newList = new List<ulong>(whitelistedChannels);

            switch (arg)
            {
                #region Add Channel to Whitelist
                case "add":
                    //Limits the whitelisted channels to 100
                    int limit = 100;
                    if (whitelistedChannels.Length > limit) 
                        return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"You have reached the maximum of {limit} whitelisted channels.");

                    //Check If the channel is already whitelisted
                    foreach (ulong item in whitelistedChannels)
                        if (item == channel.Id)
                            return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"{context.Guild.GetChannel(item)} is already whitelisted!");

                    //Add the Channel Id to the List
                    newList.Add(channel.Id);

                    //Overwrite the jObj file with the updated List
                    jObj["whitelistedChannels"] = JToken.FromObject(newList.ToArray());

                    //Saving to file
                    GuildConfigFunctions.SaveGuildConfig(context.Guild, jObj);

                    return await EmbedHandler.CreateBasicEmbed("Configuration Changed.", $"{context.Guild.GetChannel(channel.Id)} was whitelisted.");
                #endregion

                #region Remove Channel from Whitelist
                case "remove":
                    if (newList.Count == 1)
                        return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"You can't have less than 1 whitelisted channel.");

                    bool found = false;
                    foreach (ulong item in whitelistedChannels)
                    {
                        if (item == channel.Id)
                        {
                            found = true;
                            break;
                        }
                    }

                    //If the channel is not whitelisted
                    if (!found)
                        return await EmbedHandler.CreateErrorEmbed("Configuration Error.", $"{context.Guild.GetChannel(channel.Id)} is not whitelisted.");

                    //Remove Channel Id from the List
                    newList.Remove(channel.Id);

                    //Overwrite the jObj file with the updated List
                    jObj["whitelistedChannels"] = JToken.FromObject(newList.ToArray());

                    //Saving to file
                    GuildConfigFunctions.SaveGuildConfig(context.Guild, jObj);

                    return await EmbedHandler.CreateBasicEmbed("Configuration Changed.", $"{context.Guild.GetChannel(channel.Id)} was removed from the whitelist.");
                #endregion

                #region List Whitelisted Channels
                case "list":
                    StringBuilder builder = new StringBuilder();
                    builder.Append("**Whitelisted channels:**\n");
                    for (int i = 0; i < whitelistedChannels.Length; i++)
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
}
