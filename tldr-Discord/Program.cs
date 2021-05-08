using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace tldr_Discord
{
    internal class Program
    {
        private static DiscordClient _discord;

        private static async Task Main(string[] args)
        {
            var discordConfig = Config.AppSetting.GetSection("DiscordSettings");

            _discord = new DiscordClient(new DiscordConfiguration
            {
                Token = discordConfig["token"],
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Debug
            });


            var services = new ServiceCollection()
                .AddHttpClient()
                .BuildServiceProvider();


            var slash = _discord.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = services
            });


            slash.RegisterCommands<TLDRCommands>();

            slash.SlashCommandErrored += async (sender, eventArgs) =>
                _discord.Logger.LogError(eventArgs.Exception.ToString());

            _discord.Ready += (sender, eventArgs) =>
            {
                _ = Task.Run(UpdateBotStatus);
                return Task.CompletedTask;
            };

            
            await _discord.ConnectAsync();

            await Task.Delay(-1);
        }
        private static Timer StatusTimer;

        private static async Task UpdateBotStatus()
        {
            StatusTimer = new Timer(async _ =>
                {
                    var guilds = _discord.Guilds;
                    
                    using var proc = Process.GetCurrentProcess();
                    await _discord.UpdateStatusAsync(new DiscordActivity(
                        $"Servers: {_discord.Guilds.Count}"));
                },
                null,
                TimeSpan.FromSeconds(1), //time to wait before executing the timer for the first time (set first status)
                TimeSpan.FromMinutes(3)
            );
        }
        
    }
}