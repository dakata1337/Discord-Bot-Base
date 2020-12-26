using Discord;
using Discord.Commands;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Bot.DataStrucs
{
    public class GuildConfig
    {
        public string prefix { get; set; }
        public string whitelistedChannel { get; set; }
    }
}
