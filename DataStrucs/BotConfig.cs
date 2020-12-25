using System;
using System.Collections.Generic;
using System.Text;

namespace Discord_Bot.DataStrucs
{
    public class BotConfig
    {
        public string token { get; set; }
        public string gameStatus { get; set; }
        public string defaultPrefix { get; set; }
        public string guildConfigLocation { get; set; }
    }
}
