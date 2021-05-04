using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace tldr_Discord
{
    class Program
    {            
        static DiscordClient _discord;

        static async Task Main(string[] args)
        {

            var discordConfig = Config.AppSetting.GetSection("DiscordSettings");

            _discord = new DiscordClient(new DiscordConfiguration
            {
                Token = discordConfig["token"],
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Debug,
                
            });
            

            var services = new ServiceCollection()
                .AddHttpClient()
                .BuildServiceProvider();
            
            
            var slash = _discord.UseSlashCommands(new SlashCommandsConfiguration()
            {
                Services = services
            });

            
            slash.RegisterCommands<TLDRCommands>();

            slash.SlashCommandErrored += async (sender, eventArgs) =>
                _discord.Logger.LogError(eventArgs.Exception.ToString());
            
            await _discord.ConnectAsync();

            await Task.Delay(-1);
        }
    }
}