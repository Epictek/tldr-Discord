# tldr-Discord
[![Bot Invite Link](https://img.shields.io/badge/Invite%20Bot-badge.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.com/api/oauth2/authorize?client_id=839254228736278579&permissions=0&scope=applications.commands%20bot)
----
A [tldr pages](https://github.com/tldr-pages/tldr) bot for Discord that uses the new slash commands. I tried to adhere to the [v1.5 client spec](https://github.com/tldr-pages/tldr/blob/v1.5/CLIENT-SPECIFICATION.md) but may have gone astray from it due to differences between a Discord client and a command line client.

![screenshot of slash command in use](https://raw.githubusercontent.com/Epictek/tldr-Discord/master/screenshot.png)

# Todo:
Handle caches better, store for 30 days and update if package not found.
Implement language option using the locale of the guild and allow user to override.

## Self Host Guide:
1. Download latest binary from [releases](https://github.com/Epictek/tldr-Discord/releases).
2. Use this guide for setting up a discord bot account. [Creating a bot account](https://dsharpplus.github.io/articles/basics/bot_account.html#creating-a-bot-account)
3. [Get your bot token](https://dsharpplus.github.io/articles/basics/bot_account.html#get-bot-token) and put it inside the [appsettings.json](https://github.com/Epictek/tldr-Discord/blob/master/tldr-Discord/appsetting.json) file, place this file in the same directory as the executable.
4. run tldr-Discord 
