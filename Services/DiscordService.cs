using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Discord_Bot.Modules;
using Discord_Bot.DataStrucs;
using Discord_Bot.Services;
using Discord_Bot.Handlers;
using Discord.Commands;
using MySql.Data.MySqlClient;

namespace Discord_Bot
{
    public class DiscordService
    {
        private DiscordSocketClient _client;
        private ServiceProvider _services;
        private CommandHandler _commandHandler;
        private MySQL _mySQL;
        private MySqlConnection _mySqlConnection;
        private GuildConfigHandler _guildConfigHandler;
        public DiscordService()
        {
            InitializeServices();

            SubscribeDiscordEvents();
        }

        private void InitializeServices()
        {
            _services = ConfigureServices();
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _commandHandler = _services.GetRequiredService<CommandHandler>();
            _mySQL = _services.GetRequiredService<MySQL>();
            _mySqlConnection = _services.GetRequiredService<MySqlConnection>();
            _guildConfigHandler = _services.GetRequiredService<GuildConfigHandler>();
        }

        private void SubscribeDiscordEvents()
        {
            _client.Log += Client_Log;
            _client.Ready += OnClientReady;
            _client.JoinedGuild += _guildConfigHandler.JoinedGuild;
            _client.LeftGuild += _guildConfigHandler.LeftGuild;
        }

        public async Task InitializeAsync()
        {
            //Initialize Logger
            await LoggingService.InitializeAsync();

            //Initialize Config
            await GlobalData.InitializeAsync();

            //Connect Discord Client
            await ClientConnect();

            //Initialize Command Handler
            await _commandHandler.InitializeAsync();
            
            await Task.Delay(-1);
        }

       
        private async Task ClientConnect()
        {
            await _client.LoginAsync(TokenType.Bot, GlobalData.Config.token);
            await _client.StartAsync();
        }

        private async Task OnClientReady()
        {
            await _client.SetGameAsync(GlobalData.Config.gameStatus);

            //Initialize MySQL 
            _mySQL.Initialize(_services);
            _mySqlConnection = _mySQL.connection;

            //Initialize Guild Config Handler
            _guildConfigHandler.InitializeConnection(_mySqlConnection);

            //Initialize Command Handler
            await _commandHandler.InitializeAsync();

            await Task.CompletedTask;
        }

        private async Task Client_Log(LogMessage arg)
        {
            LoggingService.Log(arg.Source, $"{arg.Message}", arg.Severity);
            await Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<MySQL>()
                .AddSingleton<MySqlConnection>()
                .AddSingleton<GuildConfigHandler>()
                .BuildServiceProvider();
        }
    }
}
