﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.EventHandlers;

public class WhiteListButton(IDbContextFactory<LiveBotDbContext> dbContextFactory)
{
    public async Task OnButtonClick(DiscordClient client, ComponentInteractionCreateEventArgs e)
    {
        if (e.Guild is null ||e.Interaction.Type != InteractionType.Component || e.Interaction.Data.CustomId != "Activate") return;
        DiscordInteractionResponseBuilder responseBuilder = new()
        {
            IsEphemeral = true
        };
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        var settingsList = await liveBotDbContext.WhiteListSettings.Where(x => x.GuildId == e.Guild.Id).ToListAsync();
        if (settingsList.Count==0)
        {
            responseBuilder.WithContent("Whitelist feature not set up properly. Contact a moderator.");
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, responseBuilder);
            return;
        }
        var member = (DiscordMember)e.User;
        var entries = await liveBotDbContext.WhiteLists.Where(x => 
                x.UbisoftName == member.Username ||
                x.UbisoftName == member.Nickname ||
                x.UbisoftName ==  member.DisplayName ||
                x.UbisoftName == member.GlobalName
                )
            .ToListAsync();
        WhiteList? entry = entries.FirstOrDefault(whiteList => settingsList.Any(wls => wls.Id == whiteList.WhiteListSettingsId));

        if (entry is null)
        {
            responseBuilder.WithContent("Your username/Nickname has not been found in the database, please make sure you have set it exactly as on Ubisoft Connect!");
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, responseBuilder);
            return;
        }

        if (entry.DiscordId !=null)
        {
            responseBuilder.WithContent("You have already been verified once, if you think this is a mistake please contact a moderator");
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, responseBuilder);
            return;
        }

        DiscordRole role = e.Guild.GetRole(entry.Settings.RoleId);
        await member.GrantRoleAsync(role);
        entry.DiscordId = member.Id;
        liveBotDbContext.WhiteLists.Update(entry);
        await liveBotDbContext.SaveChangesAsync();
        
        responseBuilder.WithContent("You have verified successfully!");
        await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, responseBuilder);
    }
}