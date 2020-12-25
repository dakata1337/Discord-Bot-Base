using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Bot.Handlers
{
    public static class EmbedHandler
    {
        /// <summary>
        /// Creates Basic Discord Embed. 
        /// </summary>
        public static async Task<Embed> CreateBasicEmbed(string title, string description, string? imageUrl = null, Color? color = null)
        {
            //Here you can specifie your default embed color
            Color defaultColor = Color.DarkTeal;

            var embed = new EmbedBuilder()
            {
                Title = title,
                Description = description,
                Color = color == null ? defaultColor : (Color)color,
                Timestamp = DateTime.Now,
                ImageUrl = imageUrl
            };
            return embed.Build();
        }

        /// <summary>
        /// Creates Error Discord Embed. 
        /// </summary>
        public static async Task<Embed> CreateErrorEmbed(string title, string description, string? imageUrl = null)
        {
            //Here you can specifie your Default Error Embed color
            Color defaultColor = Color.Red;

            var embed = new EmbedBuilder()
            {
                Title = title,
                Description = description,
                Color = defaultColor,
                Timestamp = DateTime.Now,
                ImageUrl = imageUrl
            };
            return embed.Build();
        }

        /// <summary>
        /// Creates Advanced Discord Embed. 
        /// </summary>
        public static async Task<Embed> CreateCustomEmbed(SocketGuild guild, Color color, List<EmbedFieldBuilder> fields, string embedTitle)
        {
            var embed = await Task.Run(() => new EmbedBuilder
            {
                Title = embedTitle,
                Timestamp = DateTime.UtcNow,
                Color = color,
                Fields = fields
            });

            return embed.Build();
        }

        /// <summary>
        /// Creates Advanced Discord Embed. 
        /// </summary>
        public static async Task<Embed> CreateCustomEmbed(SocketGuild guild, Color color, List<EmbedFieldBuilder> fields, string embedTitle, string footer)
        {
            var embed = await Task.Run(() => new EmbedBuilder
            {
                Title = embedTitle,
                Timestamp = DateTime.UtcNow,
                Color = color,
                Fields = fields
            });

            if (!(footer is null))
                embed.Footer = new EmbedFooterBuilder { Text = $"{footer}", IconUrl = guild.CurrentUser.GetAvatarUrl() };

            return embed.Build();
        }
    }
}
