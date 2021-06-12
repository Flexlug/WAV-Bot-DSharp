﻿using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace WAV_Bot_DSharp.Commands
{
    /// <summary>
    /// Class with demonstration of possibilities.
    /// Disclaimer: The code shouldn't be used exactly this way as it is, it's just there to give you some ideas.
    /// </summary>
    [Hidden, RequireGuild]
    public sealed class DemonstrationCommands : SkBaseCommandModule
    {
        private ILogger<DemonstrationCommands> logger;

        public DemonstrationCommands(ILogger<DemonstrationCommands> logger)
        {
            this.logger = logger;

            logger.LogInformation("DemostrationCommands loaded");
        }

        [Command("button-example"), Hidden, RequireOwner]
        public async Task ButtonDemo(CommandContext ctx)
        {
            await new DiscordMessageBuilder()
                .AddComponents(new DiscordButtonComponent(ButtonStyle.Danger, "asd", "ДУ НОТ ПРЕС МИ"),
                               new DiscordButtonComponent(ButtonStyle.Primary, "asd", "ПРЕС МИ"),
                               new DiscordButtonComponent(ButtonStyle.Secondary, "ASD", "Ю КЭН ПРЕС МИ"),
                               new DiscordButtonComponent(ButtonStyle.Success, "ASD", "Ю ПРЕСД МИ!!!"))
                .WithContent("типа контент")
                .SendAsync(ctx.Channel);
        }

        /// <summary>
        /// With this command you can send a message to any discord server (Guild) which the bot is a part of,
        /// as long the Bot is on the server and got enough permissions to send a message to the targeted channel.
        /// <see cref="https://support.discordapp.com/hc/en-us/articles/206346498-Where-can-I-find-my-User-Server-Message-ID-"/>
        /// </summary>
        /// <param name="commandContext"></param>
        /// <param name="guildId"></param>
        /// <param name="channelId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [Command("sendtoguildchannel"), Aliases("stgc"), Description("Send a message to a specified channel in a special guild"), RequireOwner]
        public async Task SendToChannelAsync(CommandContext commandContext,
            [Description("Id of the target guild")] ulong guildId,
            [Description("Id of the target channel")] ulong channelId,
            [Description("Message to send"), RemainingText] string message)
        {
            var guild = await commandContext.Client.GetGuildAsync(guildId);
            var channel = guild.GetChannel(channelId);
            await channel.SendMessageAsync(message);
        }
    }
}
