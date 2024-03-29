using System.Text;
using static GooberFactory.Configuration;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using GooberFactory.Attributes;
using Microsoft.Extensions.Logging;
using Serilog;

namespace GooberFactory;

/// <summary>
/// Discord handler
/// </summary>
public static class Discord {
    /// <summary>
    /// Relay discord channel
    /// </summary>
    private static DiscordChannel _channel = null!;
    
    /// <summary>
    /// Discord client instance
    /// </summary>
    private static DiscordClient _client = null!;
    
    /// <summary>
    /// Initializes DSharpPlus
    /// </summary>
    public static async Task Initialize() {
        var factory = new LoggerFactory().AddSerilog();
        _client = new DiscordClient(new DiscordConfiguration {
            Intents = DiscordIntents.AllUnprivileged 
                      | DiscordIntents.MessageContents,
            Token = Config.Discord.BotToken,
            TokenType = TokenType.Bot,
            LoggerFactory = factory
        });
        var slash = _client.UseSlashCommands();
        slash.RegisterCommands<Commands>(Config.Discord.RelayGuild);
        slash.SlashCommandErrored += SlashCommandError;
        await _client.ConnectAsync();
        var guild = await _client.GetGuildAsync(Config.Discord.RelayGuild);
        _channel = guild.GetChannel(Config.Discord.RelayChannel);
        _client.MessageCreated += async (_, e) => {
            if (e.Channel.Id != Config.Discord.RelayChannel) return;
            var member = await e.Guild.GetMemberAsync(e.Author.Id);
            DiscordMember? repliedTo = null;
            if (e.Message.ReferencedMessage != null!) 
                repliedTo = await e.Guild.GetMemberAsync(
                    e.Message.ReferencedMessage.Author.Id);
            await RelayMessage(member, e.Message, repliedTo);
        };
        _client.MessageUpdated += async (_, e) => {
            if (e.Channel.Id != Config.Discord.RelayChannel) return;
            var member = await e.Guild.GetMemberAsync(e.Author.Id);
            DiscordMember? repliedTo = null;
            if (e.Message.ReferencedMessage != null!)
                repliedTo = await e.Guild.GetMemberAsync(
                    e.Message.Reference.Message.Author.Id);
            await RelayMessage(member, e.Message, repliedTo);
        };
    }

    /// <summary>
    /// Handles a slash command error
    /// </summary>
    /// <param name="s">Slash commands extension</param>
    /// <param name="e">Event args</param>
    private static async Task SlashCommandError(SlashCommandsExtension s, SlashCommandErrorEventArgs e) {
        if (e.Exception is not SlashExecutionChecksFailedException ex) return;
        foreach (var check in ex.FailedChecks)
            switch (check) {
                case SyncFinishedAttribute _:
                    await e.Context.CreateResponseAsync(
                        "Slow down, modlist synchronization hasn't finished yet!", true);
                    return;
                case AdminOnlyAttribute _:
                    await e.Context.CreateResponseAsync(
                        "You aren't an administrator!", true);
                    return;
                case MatchStateAttribute at:
                    var builder = new StringBuilder();
                    builder.Append("The server must be ");
                    builder.Append(at.State.ToString().ToLower());
                    builder.Append(", but it's currently ");
                    builder.Append(Program.Socket!.State.ToString().ToLower());
                    builder.Append('!');
                    await e.Context.CreateResponseAsync(builder.ToString(), true);
                    return;
            }
    }

    /// <summary>
    /// Relays a message
    /// </summary>
    /// <param name="author">Author</param>
    /// <param name="message">Message</param>
    /// <param name="repliedTo">Replied To</param>
    private static async Task RelayMessage(DiscordMember author, 
        DiscordMessage message, DiscordMember? repliedTo) {
        if (Program.Socket == null || author.IsBot) return;
        var split = message.Content.Split("\n", StringSplitOptions.RemoveEmptyEntries).ToList();
        split.AddRange(message.Attachments.Select(i => $"<{i.FileName}>"));
        
        if (repliedTo is not null) {
            var builder = new StringBuilder();
            builder.Append($"/gfi-print {Config.Messages.DiscordPrefix} ");
            builder.AppendFormat(Config.Messages.ReplyingTo,
                $"{repliedTo.Color.R:X2}{repliedTo.Color.G:X2}{repliedTo.Color.B:X2}",
                repliedTo.Nickname ?? repliedTo.DisplayName);
            await Program.Socket.SendCommand(builder.ToString());
        }
        
        for (var i = 0; i < split.Count; i++) {
            var builder = new StringBuilder(); var content = new StringBuilder();
            builder.Append($"/gfi-print {Config.Messages.DiscordPrefix} ");
            if (repliedTo is not null || i != 0) content.Append($"{Config.Messages.Newline} ");
            content.Append(split[i]); if (message.IsEdited) content.Append($" {Config.Messages.Edited}");
            builder.AppendFormat(Config.Messages.GameMessage,
                $"{author.Color.R:X2}{author.Color.G:X2}{author.Color.B:X2}",
                author.Nickname ?? author.DisplayName, content);
            await Program.Socket.SendCommand(builder.ToString());
        }
    }

    /// <summary>
    /// Updates discord bot status
    /// </summary>
    public static async Task UpdateStatus() {
        switch (Program.Socket!.State) {
            case PteroSocket.ServerState.Offline:
                await Send(Config.Messages.Stopped);
                await _client.UpdateStatusAsync(new DiscordActivity(
                    "an offline server", ActivityType.Watching));
                break;
            case PteroSocket.ServerState.Starting:
                await Send(Config.Messages.Starting);
                await _client.UpdateStatusAsync(new DiscordActivity(
                    "a starting server", ActivityType.Watching));
                break;
            case PteroSocket.ServerState.Running:
                await Send(Config.Messages.Started);
                await UpdatePlayers();
                break;
        }
    }

    /// <summary>
    /// Updates player amount in bot status
    /// </summary>
    public static async Task UpdatePlayers()
        => await _client.UpdateStatusAsync(new DiscordActivity(
            $"{Program.Socket!.Players.Count} players", ActivityType.Watching));

    /// <summary>
    /// Sends a message in the Relay channel
    /// </summary>
    /// <param name="message">Message</param>
    public static async Task Send(string message)
        => await new DiscordMessageBuilder().WithContent(message).SendAsync(_channel);
}