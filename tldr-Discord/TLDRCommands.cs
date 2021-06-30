using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace tldr_Discord
{
    public class TLDRCommands : SlashCommandModule
    {
        private readonly HttpClient HttpClient;

        public TLDRCommands(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }
        
        [SlashCommand("about", "about the bot")]
        public async Task About(InteractionContext ctx)
        {

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"About")
                .WithAuthor("tldr pages", "https://tldr.sh/", "https://tldr.sh/assets/img/icon.png")
                .WithColor(new DiscordColor("#54B59A"))
                .WithDescription($"**What is tldr-pages?**\n The tldr-pages project is a collection of community-maintained help pages for command-line tools, that aims to be a simpler, more approachable complement to traditional man pages.")
                .AddField("Bot Created By", "<@63306150757543936>")
                .AddField("Git Repo", "https://github.com/Epictek/tldr-Discord")
                .Build();
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("stats", "stats for the bot")]
        public async Task Stats(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithAuthor("tldr pages", "https://tldr.sh/", "https://tldr.sh/assets/img/icon.png")
                .WithColor(new DiscordColor("#54B59A"))
                .AddField("Servers", ctx.Client.Guilds.Count.ToString());
            
            // left here to nose on what other server have this bot in ;)
            // foreach (var guild in ctx.Client.Guilds)
            // {
            //     embed.Description += guild.Value.Name + Environment.NewLine;
            // }
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()));
        }
        
        
        
        
        [SlashCommand("tldr", "retrieve a TLDR for a command")]
        public async Task GetTLDR(InteractionContext ctx,
            [Option("page", "page to find")] string name,
            [Option("platform", "Specifies the platform to be used")]
            Platform platform = Platform.Linux,
            [Option("language", "Specifies the preferred language for the page returned")]
            string? language = null 

        )
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await CheckCache(ctx))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cache update failed"));
            }
            
            
            name = name.ToLower();
            name = name.Replace(' ', '-');


            var index = await GetIndex();
            var page = index.Commands.FirstOrDefault(x => x.Name == name);

            //if page not found, redownload cache and try again
            if (page == null)
            {
                await CheckCache(ctx);
                index = await GetIndex();
                page = index.Commands.FirstOrDefault(x => x.Name == name);

                if (page == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("page not found"));
                    return;
                }
            }

            
            var targets = page.Targets.Where(x => x.Os == platform).ToList();
            
            if  (!targets.Any())
            {
                //fallback to common
                targets = page.Targets.Where(x => x.Os == Platform.Common).ToList();
                
                //fallback to random platform
                if (!targets.Any())
                {
                    targets = page.Targets.ToList();
                }
            }

            if (language != null)
            {
                var langTargets = targets.Where(x => x.Language == language)
                    .ToList();
                if (!langTargets.Any())
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("page not found in chosen language"));
                    return;
                }

                targets = langTargets;

            }
            else
            {
                //default to english
                var langTargets = targets.Where(x => x.Language == "en")
                    .ToList();
                
                if (langTargets.Any())
                {
                    targets = langTargets;
                }
            }

            var finalTarget = targets.FirstOrDefault();

            if (finalTarget == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("page not found"));
                return;
            }
            
            var lines = await GetPageFromTarget(name, finalTarget);


            var builder = new DiscordWebhookBuilder();
            
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"tldr page: {lines.FirstOrDefault()?.Remove(0, 2)} ({finalTarget.Os})")
                .WithAuthor("tldr pages", "https://tldr.sh/", "https://tldr.sh/assets/img/icon.png")
                .WithColor(new DiscordColor("#54B59A"))
                .WithDescription(string.Join("\n", lines.Skip(1)).Replace("\n\n", "\n"))
                .WithFooter("tldr - collaborative cheatsheets for console commands ",
                    "https://github.githubassets.com/images/icons/emoji/unicode/1f4da.png")
                .Build();

            builder.AddEmbed(embed);

            if (finalTarget.Os != Platform.Common && finalTarget.Os != platform)
            {
                var warningEmbed = new DiscordEmbedBuilder()
                    .WithTitle($"Platform: {finalTarget.Os}")
                    .WithColor(DiscordColor.Red)
                    .Build();
                builder.AddEmbed(warningEmbed);
            }

            await ctx.EditResponseAsync(builder);
        }


        private async Task<bool> CheckCache(InteractionContext ctx)
        {
            if (Directory.Exists("tldr-cache") && 
                (Directory.GetLastWriteTimeUtc("tldr-cache") - DateTime.UtcNow) <= TimeSpan.FromDays(30)) return true;
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Updating cache..."));
            try
            {
                await UpdateCache();
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cache updated"));
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }
        
        private async Task<TldrIndex> GetIndex()
        {
            var indexFile = Path.Combine("tldr-cache", "index.json");

            var tldrIndex = TldrIndex.FromJson(await File.ReadAllTextAsync(indexFile));
            return tldrIndex;
        }
        
        
        private Task<string[]> GetPageFromTarget(string name, Target target)
        {
            var dir = "tldr-cache";
            if (target.Language == "en")
            {
                dir = Path.Combine("tldr-cache", "pages");
            }
            else
            {
                dir = Path.Combine("tldr-cache", "pages." + target.Language);
            }

            var pagePath = Path.Combine(Path.Combine(dir, target.Os.ToString().ToLower()), name + ".md");

            return File.ReadAllLinesAsync(pagePath);

        }
        
        
        private async Task UpdateCache()
        {
            var file = await HttpClient.GetStreamAsync("https://tldr.sh/assets/tldr.zip");
            var archive = new ZipArchive(file);
            archive.ExtractToDirectory("tldr-cache", true);
        }
    }
}
