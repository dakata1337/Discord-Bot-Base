using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Bot.DataStrucs;
using Discord_Bot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Discord_Bot.Handlers
{
    class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        public CommandHandler(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            HookEvents();
        }

        public void HookEvents()
        {
            _commands.Log += LogAsync;
            _client.MessageReceived += HandleCommandAsync;
            _commands.CommandExecuted += CommandExecutedAsync;
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            //If Execution was successfull - return
            if (result.IsSuccess)
                return;

            //If an Error Occurs send to Discord
            if (!command.IsSpecified || !result.IsSuccess)
            {
                await context.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Error.", result.ErrorReason));
            }
        }

        private Task LogAsync(LogMessage arg)
        {
            switch (arg.Severity)
            {
                case LogSeverity.Info:
                    LoggingService.Log(arg.Source, arg.Message);
                    break;
                default:
                    LoggingService.Log(arg.Source, arg.Message, ConsoleColor.Red);
                    break;
            }
            return Task.CompletedTask;
        }

        private async Task<Task> HandleCommandAsync(SocketMessage socketMessage)
        {
            //If the message lenght is 0 - return
            if (socketMessage.Content.Length == 0)
                return Task.CompletedTask;

            var argPos = 0;
            var context = new SocketCommandContext(_client, socketMessage as SocketUserMessage);

            //If the message is from Bot/Webhook - return
            if (!(socketMessage is SocketUserMessage message) || message.Author.IsBot || message.Author.IsWebhook)
                return Task.CompletedTask;

            //Config Object
            var jObj = GuildConfigFunctions.GetGuildConfig(context.Guild);

            //Get prefix from Config
            string serverPrefix = jObj["prefix"].ToString();

            //If the message doesnt have prefix - return
            if (!message.HasStringPrefix(serverPrefix, ref argPos))
                return Task.CompletedTask;

            //If the message is only the prefix - return
            if (message.Content == serverPrefix)
                return Task.CompletedTask;


            ulong[] channels = jObj["whitelistedChannels"].ToObject<ulong[]>();
            var whitelistedChannelCheck = from a in channels
                                          where a == context.Channel.Id
                                          select a;

            //Get whitelisted channel
            ulong whitelistedChannel = whitelistedChannelCheck.FirstOrDefault();

            //If Context Channel is Whitelisted - Execute
            if (whitelistedChannel == context.Channel.Id)
                return _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);

            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(
                assembly: Assembly.GetEntryAssembly(),
                services: _services);
        }
    }
}
