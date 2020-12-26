using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Bot.Handlers;

namespace Discord_Bot.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("Test")]
        public async Task Test() =>
            await ReplyAsync(await GuildConfigHandler.Test(Context));

        [Command("Prefix")]
        public async Task Prefix(string prefix) =>
            await ReplyAsync(embed: await GuildConfigHandler.ChangePrefix(Context, prefix));

        [Command("Whitelist"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task Whitelist(string argument, IChannel channel = null) =>
            await ReplyAsync(embed: await GuildConfigHandler.WhiteList(Context, argument, channel));
    }
}
