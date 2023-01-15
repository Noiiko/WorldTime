﻿using Discord.Interactions;
using System.Text;

namespace WorldTime.Commands;
public class ApplicationCommands : CommandsBase {
    const string EmbedHelpField1 = $"`/help` - {HelpHelp}\n"
        + $"`/list` - {HelpList}\n"
        + $"`/set` - {HelpSet}\n"
        + $"`/remove` - {HelpRemove}";
    const string EmbedHelpField2 = $"`/set-for` - {HelpSetFor}\n`/remove-for` - {HelpRemoveFor}";

    #region Help strings
    const string HelpHelp = "Displays a list of available bot commands.";
    const string HelpList = "Shows the current time for all recently active known users.";
    const string HelpSet = "Adds or updates your time zone to the bot.";
    const string HelpSetFor = "Sets/updates time zone for a given user.";
    const string HelpRemove = "Removes your time zone information from this bot.";
    const string HelpRemoveFor = "Removes time zone for a given user.";
    #endregion

    [SlashCommand("help", HelpHelp)]
    public async Task CmdHelp() {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!.ToString(3);
        var guildct = ShardedClient.Guilds.Count;
        using var db = DbContext;
        var uniquetz = db.GetDistinctZoneCount();
        await RespondAsync(embed: new EmbedBuilder() {
            Title = "Help & About",
            Description = $"World Time v{version} - Serving {guildct} communities across {uniquetz} time zones.\n\n"
                + "This bot is provided for free, without any paywalled 'premium' features. "
                + "If you've found this bot useful, please consider contributing via the "
                + "bot author's page on Ko-fi: https://ko-fi.com/noithecat.",
            Footer = new EmbedFooterBuilder() {
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Text = "World Time"
            }
        }.AddField(inline: false, name: "Commands", value: EmbedHelpField1
        ).AddField(inline: false, name: "Admin commands", value: EmbedHelpField2
        ).AddField(inline: false, name: "Zones", value:
            "This bot accepts zone names from the IANA Time Zone Database (a.k.a. Olson Database). " +
            "A useful tool to determine yours can be found at: https://kevinnovak.github.io/Time-Zone-Picker/"
        ).Build());
    }

    [RequireGuildContext]
    [SlashCommand("list", HelpList)]
    public async Task CmdList([Summary(description: "A specific user whose time to look up.")]SocketGuildUser? user = null) {
        if (!await AreUsersDownloadedAsync(Context.Guild)) {
            await RespondAsync(ErrNoUserCache, ephemeral: true);
            return;
        }

        if (user == null) {
            // No parameter - full listing
            await CmdListWithoutParamAsync();
        } else {
            // Has parameter - do single user listing
            await CmdListWithUserParamAsync(user);
        }
    }

    private async Task CmdListWithoutParamAsync() {
        // Called by CmdList
        using var db = DbContext;
        var userlist = db.GetGuildZones(Context.Guild.Id);
        if (userlist.Count == 0) {
            await RespondAsync(":x: Nothing to show. Register your time zones with the bot using the `/set` command.");
            return;
        }
        // Order times by popularity to limit how many are shown, group by printed name
        var sortedlist = new SortedDictionary<string, List<ulong>>();
        foreach ((string area, List<ulong> users) in userlist.OrderByDescending(o => o.Value.Count).Take(20)) {
            // Filter further to top 20 distinct timezones, even if they are not displayed in the final result
            var areaprint = TzPrint(area);
            if (!sortedlist.ContainsKey(areaprint)) sortedlist.Add(areaprint, new List<ulong>());
            sortedlist[areaprint].AddRange(users);
        }

        const int MaxSingleLineLength = 750;
        const int MaxSingleOutputLength = 900;

        // Build zone listings with users
        var outputlines = new List<string>();
        foreach ((string area, List<ulong> users) in sortedlist) {
            var buffer = new StringBuilder();
            buffer.Append(area[4..] + ": ");
            bool empty = true;
            foreach (var userid in users) {
                var userinstance = Context.Guild.GetUser(userid);
                if (userinstance == null) continue;
                if (empty) empty = !empty;
                else buffer.Append(", ");
                var useradd = FormatName(userinstance);
                if (buffer.Length + useradd.Length > MaxSingleLineLength) {
                    buffer.Append("others...");
                    break;
                } else buffer.Append(useradd);
            }
            if (!empty) outputlines.Add(buffer.ToString());
        }

        // Prepare for output - send buffers out if they become too large
        outputlines.Sort();
        bool hasOutputOneLine = false;
        // First output is shown as an interaction response, followed then as regular channel messages
        async Task doOutput(Embed msg) {
            if (!hasOutputOneLine) {
                await RespondAsync(embed: msg);
                hasOutputOneLine = true;
            } else {
                await ReplyAsync(embed: msg);
            }
        }

        var resultout = new StringBuilder();
        foreach (var line in outputlines) {
            if (resultout.Length + line.Length > MaxSingleOutputLength) {
                await doOutput(new EmbedBuilder().WithDescription(resultout.ToString()).Build());
                resultout.Clear();
            }
            if (resultout.Length > 0) resultout.AppendLine(); // avoids trailing newline by adding to the previous line
            resultout.Append(line);
        }
        if (resultout.Length > 0) {
            await doOutput(new EmbedBuilder().WithDescription(resultout.ToString()).Build());
        }
    }

    private async Task CmdListWithUserParamAsync(SocketGuildUser parameter) {
        // Called by CmdList
        using var db = DbContext;
        var result = db.GetUserZone(parameter);
        if (result == null) {
            bool isself = Context.User.Id == parameter.Id;
            if (isself) await RespondAsync(":x: You do not have a time zone. Set it with `tz.set`.", ephemeral: true);
            else await RespondAsync(":x: The given user does not have a time zone set.", ephemeral: true);
            return;
        }

        var resulttext = TzPrint(result)[4..] + ": " + FormatName(parameter);
        await RespondAsync(embed: new EmbedBuilder().WithDescription(resulttext).Build());
    }

    [SlashCommand("set", HelpSet)]
    public async Task CmdSet([Summary(description: "The new time zone to set.")]string zone) {
        var parsedzone = ParseTimeZone(zone);
        if (parsedzone == null) {
            await RespondAsync(ErrInvalidZone, ephemeral: true);
            return;
        }
        using var db = DbContext;
        db.UpdateUser((SocketGuildUser)Context.User, parsedzone);
        await RespondAsync($":white_check_mark: Your time zone has been set to **{parsedzone}**.");
    }

    [RequireGuildContext]
    [SlashCommand("set-for", HelpSetFor)]
    public async Task CmdSetFor([Summary(description: "The user whose time zone to modify.")] SocketGuildUser user,
                                 [Summary(description: "The new time zone to set.")] string zone) {
        if (!IsUserAdmin((SocketGuildUser)Context.User)) {
            await RespondAsync(ErrNotAllowed, ephemeral: true).ConfigureAwait(false);
            return;
        }

        // Extract parameters
        var newtz = ParseTimeZone(zone);
        if (newtz == null) {
            await RespondAsync(ErrInvalidZone);
            return;
        }

        using var db = DbContext;
        db.UpdateUser(user, newtz);
        await RespondAsync($":white_check_mark: Time zone for **{user}** set to **{newtz}**.");
    }

    [RequireGuildContext]
    [SlashCommand("remove", HelpRemove)]
    public async Task CmdRemove() {
        using var db = DbContext;
        var success = db.DeleteUser((SocketGuildUser)Context.User);
        if (success) await RespondAsync(":white_check_mark: Your zone has been removed.");
        else await RespondAsync(":x: You don't have a time zone set.");
    }

    [RequireGuildContext]
    [SlashCommand("remove-for", HelpRemoveFor)]
    public async Task CmdRemoveFor([Summary(description: "The user whose time zone to remove.")] SocketGuildUser user) {
        if (!IsUserAdmin((SocketGuildUser)Context.User)) {
            await RespondAsync(ErrNotAllowed, ephemeral: true).ConfigureAwait(false);
            return;
        }

        using var db = DbContext;
        if (db.DeleteUser(user))
            await RespondAsync($":white_check_mark: Removed zone information for {user}.");
        else
            await RespondAsync($":white_check_mark: No time zone is set for {user}.");
    }
}