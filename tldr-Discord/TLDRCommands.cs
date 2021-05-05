using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace tldr_Discord
{
    public class TLDRCommands : SlashCommandModule
    {
        private readonly HttpClient HttpClient;

        public TLDRCommands(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        [SlashCommand("tldr", "retrieve a TLDR for a command")]
        public async Task GetTLDR(InteractionContext ctx,
            [Option("page", "page to find")] string page,
            [Choice("linux", "linux")]
            [Choice("osx", "osx")]
            [Choice("windows", "windows")]
            [Choice("android", "android")]
            [Choice("sunos", "sunos")]
            [Choice("common", "common")]
            [Option("platform", "Specifies the platform to be used")]
            string platform = "linux",

            // [Option("language", "Specifies the preferred language for the page returned")]
            // string? language = null,
            [Option("update", "Updates the offline cache of pages")]
            bool update = false
        )
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (update || !Directory.Exists("tldr-cache"))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Updating cache..."));
                try
                {
                    await UpdateCache();
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cache updated"));
                }
                catch (Exception e)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cache update failed"));
                    return;
                }
            }

            //language ??= ctx.Guild.PreferredLocale;

            page = page.ToLower();
            page = page.Replace(' ', '-');
            var path = FindPage(page, platform);
            if (path == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("page not found"));
                return;
            }

            var lines = await File.ReadAllLinesAsync(path);

            var builder = new DiscordWebhookBuilder();

            var resultPlatform = path.Split('/')[2];

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"tldr page: {lines.FirstOrDefault()?.Remove(0, 2)} ({resultPlatform})")
                .WithAuthor("tldr pages", "https://tldr.sh/", "https://tldr.sh/assets/img/icon.png")
                .WithColor(new DiscordColor(0x478061))
                .WithDescription(string.Join("\n", lines.Skip(1)).Replace("\n\n", "\n"))
                .WithFooter("tldr - collaborative cheatsheets for console commands ",
                    "https://github.githubassets.com/images/icons/emoji/unicode/1f4da.png")
                .Build();

            builder.AddEmbed(embed);

            if (resultPlatform != "common" && resultPlatform != platform)
            {
                var warningEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Platform: " + resultPlatform)
                    .WithColor(DiscordColor.Red)
                    .Build();
                builder.AddEmbed(warningEmbed);
            }

            await ctx.EditResponseAsync(builder);
        }

        private string? FindPage(string page, string platformPref)
        {
            var rootDir = Path.Combine("tldr-cache", "pages");

            var preferedPage = GetPageInPath(page, platformPref);
            if (preferedPage != null) return preferedPage;

            if (platformPref != "common")
            {
                var commonPage = GetPageInPath(page, "common");
                if (commonPage != null) return commonPage;
            }

            var platfromPaths = Directory.GetDirectories(rootDir).Select(x => x.Split('/').LastOrDefault())
                .Where(x => x != "common" && x != platformPref);

            foreach (var platfromPath in platfromPaths)
            {
                var ppage = GetPageInPath(page, platfromPath);

                if (File.Exists(ppage)) return ppage;
            }

            return null;
        }

        private string? GetPageInPath(string page, string platform)
        {
            var rootDir = Path.Combine("tldr-cache", "pages");

            var preferedPage = Path.Combine(Path.Combine(rootDir, platform), page + ".md");

            return File.Exists(preferedPage) ? preferedPage : null;
        }

        private async Task UpdateCache()
        {
            var file = await HttpClient.GetStreamAsync("https://tldr.sh/assets/tldr.zip");
            var archive = new ZipArchive(file);
            archive.ExtractToDirectory("tldr-cache", true);
        }
    }
}
