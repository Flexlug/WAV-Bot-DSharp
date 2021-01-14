﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using NLog;

namespace WAV_Bot_DSharp.Commands
{
    /// <summary>
    /// Commands that can be used by @everyone. 
    /// </summary>
    [Group("Utils")]
    public sealed class UserCommands : BaseCommandModule
    {
        public UserCommands() {  }

        /// <summary>
        /// Prints out the latency between the bot and discord api servers.
        /// </summary>
        /// <param name="commandContext">CommandContext from the message that has executed this command.</param>
        /// <returns></returns>
        [Command("ping"), Description("Shows bot ping to discord api server")]
        public async Task PingAsync(CommandContext commandContext)
        {
            await commandContext.RespondAsync($"Bot latency to the discord api server: {commandContext.Client.Ping}");
        }

        /// <summary>
        /// Try to recognize timespan
        /// </summary>
        /// <param name="commandContext">CommandContext from the message that has executed this command.</param>
        /// <param name="timeSpan">Timespan to recognize</param>
        /// <returns></returns>
        [Command("timespan"), Description("Try to recognize timespan")]
        public async Task GetTimespan(CommandContext commandContext,
            [Description("Timespan to recognize")] TimeSpan timeSpan)
        {
            await commandContext.RespondAsync(timeSpan.ToString());
        }

        /// <summary>
        /// Try to recognize datetime. DateTime inputs in AMERICAN STYLE!!!
        /// </summary>
        /// <param name="commandContext">CommandContext from the message that has executed this command.</param>
        /// <param name="datetime">Datetime to recognize</param>
        /// <returns></returns>
        [Command("datetime"), Description("Try to recognize datetime (american style)")]
        public async Task GetDatetime(CommandContext commandContext, 
            [Description("Datetime to recognize")] DateTime datetime)
        {
            await commandContext.RespondAsync($"{datetime.ToShortDateString()} {datetime.ToLongTimeString()}");
        }
    }
}