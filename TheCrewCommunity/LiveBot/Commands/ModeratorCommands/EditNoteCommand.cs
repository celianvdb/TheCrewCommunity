﻿using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;
public static class EditNoteCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory, IModeratorLoggingService moderatorLoggingService,CommandContext ctx, DiscordUser user, long noteId)
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Infraction? infraction = await dbContext.Infractions.FindAsync(noteId);
        if (infraction is null || infraction.UserId != user.Id || infraction.AdminDiscordId != ctx.User.Id || infraction.InfractionType != InfractionType.Note)
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Could not find a note with that ID"));
            return;
        }

        string oldNote = infraction.Reason;
        var customId = $"EditNote-{ctx.User.Id}";
        DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder().WithTitle("Edit users note").WithCustomId(customId)
            .AddComponents(new TextInputComponent("Content", "Content", null, infraction.Reason, true, TextInputStyle.Paragraph));
        await ctx.RespondAsync(modal);

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var response = await interactivity.WaitForModalAsync(customId, ctx.User);
        if (response.TimedOut) return;
        infraction.Reason = response.Result.Values["Content"];
        dbContext.Infractions.Update(infraction);
        await dbContext.SaveChangesAsync();
        await response.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent($"Note `#{infraction.Id}` content changed\nFrom:`{oldNote}`\nTo:`{infraction.Reason}`").AsEphemeral());
        Guild? guild = await dbContext.Guilds.FindAsync(ctx.Guild.Id);
        if (guild is null) return;
        DiscordChannel channel = ctx.Guild.GetChannel(Convert.ToUInt64(guild.ModerationLogChannelId));
        moderatorLoggingService.AddToQueue(new ModLogItem(
            channel,
            user,
            "# Note Edited\n" +
            $"- **User:** {user.Mention}\n" +
            $"- **Moderator:** {ctx.Member.Mention}\n" +
            $"- **Old Note:** {oldNote}\n" +
            $"- **New Note:** {infraction.Reason}",
            ModLogType.Info));
    }
}