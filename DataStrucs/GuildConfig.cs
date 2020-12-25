using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Discord_Bot.DataStrucs
{
    public class GuildConfig
    {
        public string prefix { get; set; }
        public List<ulong> whitelistedChannels { get; set; }
    }

    public static class GuildConfigFunctions
    {
        public static JObject GetGuildConfig(IGuild guild)
        {
            return (JObject)JsonConvert.DeserializeObject(File.ReadAllText(GetConfigLocation(guild)));
        }

        public static string GetConfigLocation(IGuild guild)
        {
            return $@"{GlobalData.Config.guildConfigLocation}\{guild.Id}.json";
        }

        public static void SaveGuildConfig(IGuild guild, JObject config)
        {
            File.WriteAllText(GetConfigLocation(guild), JsonConvert.SerializeObject(config, Formatting.Indented), new UTF8Encoding(true));
        }
    }
}
