﻿using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;
public static class AddNoteCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory, IModeratorLoggingService moderatorLoggingService, IDatabaseMethodService databaseMethodService, SlashCommandContext ctx, DiscordUser user, string note, DiscordAttachment? image = null)
    {
        await ctx.DeferResponseAsync(true);
        if (ctx.Guild is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This command can only be used in a server!"));
            return;
        }
        if (ctx.Member is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This command can only be used by a member!"));
            return;
        }
        await databaseMethodService.AddInfractionsAsync(new Infraction(ctx.User.Id, user.Id, ctx.Guild.Id, note, false, InfractionType.Note));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{ctx.User.Mention}, a note has been added to {user.Username}({user.Id})"));
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guild = await dbContext.Guilds.FindAsync(ctx.Guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(ctx.Guild.Id));
        DiscordChannel channel = ctx.Guild.GetChannel(Convert.ToUInt64(guild.ModerationLogChannelId));
        moderatorLoggingService.AddToQueue(new ModLogItem(
            channel,
            user,
            "# Note Added\n" +
            $"- **User:** {user.Mention}\n" +
            $"- **Moderator:** {ctx.Member.Mention}\n" +
            $"- **Note:** {note}",
            ModLogType.Info,
            attachment: image));
    }
}