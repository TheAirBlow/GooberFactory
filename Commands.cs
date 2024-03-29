using System.Net.WebSockets;
using static GooberFactory.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GooberFactory.Attributes;
using GooberFactory.Providers;
using GooberFactory.WebAPI;
using Humanizer;
using Serilog;

namespace GooberFactory;

/// <summary>
/// All discord slash commands
/// </summary>
public class Commands : ApplicationCommandModule {
    [MatchState(PteroSocket.ServerState.Running)]
    [SlashCommand("evolution", "Prints current evolution level")]
    public async Task Evolution(InteractionContext ctx) {
        await ctx.DeferAsync();
        var msg = await Program.Socket!.SendCommand("/evolution", true);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(msg));
    }
    
    [AdminOnly] [SlashCommand("reconnect", "Reconnect to Pterodactyl WebSockets (admin only)")]
    public async Task Reconnect(InteractionContext ctx) {
        await ctx.CreateResponseAsync("Reconnection is now in progress...");
        Program.Socket?.Dispose(); await Program.InitSocket();
    }
    
    [AdminOnly] [MatchState(PteroSocket.ServerState.Offline)]
    [SlashCommand("rollback", "Rollbacks to an autosave (admin only)")]
    public async Task Rollback(InteractionContext ctx,
        [Option("filename", "Save filename")] 
        [Autocomplete(typeof(SavesProvider))] string filename) {
        var contents = await PterodactylAPI.GetFolderContents("/saves");
        var file = contents.Files.FirstOrDefault(x => x.Attributes.Name == filename);
        if (file == null) {
            await ctx.CreateResponseAsync($"Save `{filename}` doesn't exist!");
            return;
        }

        try {
            var to = file.Attributes.ModifiedAt.Humanize();
            await ctx.CreateResponseAsync($"Rolling back to {to}...");
            try { await PterodactylAPI.Delete("/gamesave_old.zip"); } catch { /* Ignore */ }
            await PterodactylAPI.Rename("/", "gamesave.zip", "gamesave_old.zip");
            await PterodactylAPI.Rename("/saves", filename, "../gamesave.zip");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Successfully rolled back to {to}!"));
        } catch (Exception e) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Caught an exception, rollback failed!"));
            Log.Error("{0}", e);
        }
    }
    
    [AdminOnly] [MatchState(PteroSocket.ServerState.Running)]
    [SlashCommand("restart", "Restarts the server (admin only)")]
    public async Task Restart(InteractionContext ctx) {
        await Program.Socket!.DoAction(PteroSocket.PowerAction.Restart);
        await ctx.CreateResponseAsync("Sent restart command to the server!");
    }
    
    [AdminOnly] [SlashCommand("start", "Starts the server (admin only)")]
    public async Task Start(InteractionContext ctx) {
        await Program.Socket!.DoAction(PteroSocket.PowerAction.Start);
        await ctx.CreateResponseAsync("Sent start command to the server!");
    }
    
    [AdminOnly] [MatchState(PteroSocket.ServerState.Running)]
    [SlashCommand("stop", "Stops the server (admin only)")]
    public async Task Stop(InteractionContext ctx) {
        await Program.Socket!.DoAction(PteroSocket.PowerAction.Stop);
        await ctx.CreateResponseAsync("Sent stop command to the server!");
    }
    
    [AdminOnly] [MatchState(PteroSocket.ServerState.Running)]
    [SlashCommand("ban", "Permanently ban a player (admin only)")]
    public async Task Ban(InteractionContext ctx,
        [Option("username", "Player's username")] 
        [Autocomplete(typeof(UsernameProvider))] string username,
        [Option("reason", "Reason for the ban")] string reason) {
        await Program.Socket!.SendCommand($"/ban {username} {reason}");
        await ctx.CreateResponseAsync($"{ctx.Member.Mention} banned `{username}` for `{reason}`!");
    }
    
    [AdminOnly] [MatchState(PteroSocket.ServerState.Running)]
    [SlashCommand("unban", "Unbans a banned player (admin only)")]
    public async Task Unban(InteractionContext ctx,
        [Option("username", "Player's username")] 
        [Autocomplete(typeof(UsernameProvider))] string username) {
        await Program.Socket!.SendCommand($"/unban {username}");
        await ctx.CreateResponseAsync($"{ctx.Member.Mention} unbanned `{username}`!");
    }
    
    [AdminOnly] [MatchState(PteroSocket.ServerState.Running)]
    [SlashCommand("kick", "Kicks a player (admin only)")]
    public async Task Kick(InteractionContext ctx,
        [Option("username", "Player's username")]
        [Autocomplete(typeof(UsernameProvider))] string username,
        [Option("reason", "Reason for the ban")] string reason) {
        await Program.Socket!.SendCommand($"/kick {username} {reason}");
        await ctx.CreateResponseAsync($"{ctx.Member.Mention} kicked `{username}` for `{reason}`!");
    }
    
    [MatchState(PteroSocket.ServerState.Running)]
    [SlashCommand("online", "Lists online players")]
    public async Task Online(InteractionContext ctx) {
        if (Program.Socket!.Players.Count == 0) {
            await ctx.CreateResponseAsync("Nobody is online right now!");
            return;
        }
        
        var builder = new StringBuilder();
        builder.AppendLine($"{Program.Socket.Players.Count} player(s) online:");
        foreach (var i in Program.Socket.Players) builder.AppendLine($"- {i}");
        await ctx.CreateResponseAsync(builder.ToString());
    }
    
    [AdminOnly] [SlashCommand("sync", "Force modlist sync (admin only)")]
    public async Task Sync(InteractionContext ctx) {
        await ctx.CreateResponseAsync("Synchronizing cached modlist...");
        try {
            await ModManager.SyncMods();
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Modlist synchronization finished successfully!"));
        } catch (Exception e) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Caught an exception, synchronization failed!"));
            Log.Error("{0}", e);
        }
    }
    
    [SyncFinished] [SlashCommand("modlist", "Lists all mods installed")]
    public async Task ModList(InteractionContext ctx,
        [Option("page", "Page number")] double page = 1) {
        var pages = (int)Math.Ceiling(ModManager.Mods!.Count / 25f);
        if (pages == 0) {
            await ctx.CreateResponseAsync("Sorry, no mods are installed!");
            return;
        }
            
        var embed = new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Orange)
            .WithTitle($"Page {page} out of {pages}");
        var intPage = (int)page;
        if (intPage < 1) {
            await ctx.CreateResponseAsync("A page number cannot be zero or negative!");
            return;
        }
            
        if (intPage > pages) {
            await ctx.CreateResponseAsync($"This page doesn't exist; there are only {pages}");
            return;
        }

        foreach (var info in ModManager.Mods.Skip((intPage - 1) * 25).Take(25)) {
            var emote = info.Enabled ? Config.Messages.Enabled : Config.Messages.Disabled;
            embed.AddField($"{emote} {info.Title} {info.Version}", $"[{info.Name}]({info.Url})", true);
        }
        await ctx.CreateResponseAsync(embed.Build());
    }
    
    [AdminOnly] [MatchState(PteroSocket.ServerState.Offline)]
    [SyncFinished] [SlashCommand("disable", "Disables a mod (admin only)")]
    public async Task Disable(InteractionContext ctx,
        [Option("name", "Mod's name")] string name) {
        var mod = ModManager.Mods!.FirstOrDefault(x => x.Name == name);
        if (mod == null) {
            await ctx.CreateResponseAsync("This mod is not installed!");
            return;
        }
        
        if (!mod.Enabled) {
            await ctx.CreateResponseAsync("This mod is already disabled!");
            return;
        }

        await ctx.DeferAsync();
        mod.Enabled = false;
        await ModManager.UpdateMods();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Successfully disabled `{name}`!"));
    }
    
    [AdminOnly] [MatchState(PteroSocket.ServerState.Offline)]
    [SyncFinished] [SlashCommand("enable", "Enables a mod (admin only)")]
    public async Task Enable(InteractionContext ctx,
        [Option("name", "Mod's name")] string name) {
        var mod = ModManager.Mods!.FirstOrDefault(x => x.Name == name);
        if (mod == null) {
            await ctx.CreateResponseAsync("This mod is not installed!");
            return;
        }
        
        if (mod.Enabled) {
            await ctx.CreateResponseAsync("This mod is already enabled!");
            return;
        }

        await ctx.DeferAsync();
        mod.Enabled = true; await ModManager.UpdateMods();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Successfully enabled `{name}`!"));
    }
    
    [AdminOnly] [MatchState(PteroSocket.ServerState.Offline)]
    [SyncFinished] [SlashCommand("delete", "Deletes a mod (admin only)")]
    public async Task Delete(InteractionContext ctx,
        [Option("name", "Mod's name")] string name) {
        await ctx.DeferAsync();
        var mod = ModManager.Mods!.FirstOrDefault(x => x.Name == name);
        if (mod == null) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("This mod is not installed!"));
            return;
        }
        
        await PterodactylAPI.Delete("/mods", mod.Directory);
        ModManager.Mods!.Remove(mod); await ModManager.UpdateMods();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Successfully deleted `{name}`!"));
    }
    
    [SyncFinished] [AdminOnly] [MatchState(PteroSocket.ServerState.Offline)]
    [SlashCommand("install", "Installs a mod onto the server (admin only)")]
    public async Task Install(InteractionContext ctx,
        [Option("name", "Mod's name")] string name,
        [Option("version", "Mod's version")]
        [Autocomplete(typeof(ModVersionProvider))] string version) {
        try {
            var mod = ModManager.Mods!.FirstOrDefault(x => x.Name == name);
            if (mod == null) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("This mod is not installed!"));
                return;
            }
            
            await ctx.CreateResponseAsync("Preparing for a mod installation...");
            var info = await FactorioAPI.GetModInfo(name);
            var release = info.Releases.FirstOrDefault(x => x.Version == version);
            if (release == null) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("This mod doesn't have the specified version!"));
                return;
            }

            var deps = release.InfoJson.Dependencies
                .Where(x => !x.StartsWith("base")).Select(x => new Dependency(x)).ToList();
            foreach (var i in deps.Where(x => x.Type == Dependency.TypeEnum.Incompatibility))
                if (ModManager.Mods!.Any(x => x.Name == i.ModName)) {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"This mod is incompatible with `{i}`!"));
                    return;
                }

            var hard = deps.Where(x => x.Type is
                Dependency.TypeEnum.Hard or Dependency.TypeEnum.IgnoreLoadOrder 
                && ModManager.Mods!.FirstOrDefault(y => y.Name == x.ModName) == null).ToList();
            
            // ReSharper disable twice VariableHidesOuterVariable
            async Task InstallMod(FactorioAPI.ModInfo info, FactorioAPI.ModInfo.Release release, int index) {
                using var client = new HttpClient();
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"**[{index}/{hard.Count + 1}]** Step 1/3: Uploading `{info.Title}` version `{release.Version}`"));
                var stream = await client.GetStreamAsync($"{Config.ProxyUrl}/{release.DownloadURL}");
                var rng = RandomNumberGenerator.GetInt32(int.MaxValue).ToString();
                await PterodactylAPI.UploadFile(stream, release.FileName, $"/.tmp/{rng}");
                
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"**[{index}/{hard.Count + 1}]** Step 2/3: Extracting `{info.Title}` version `{release.Version}`"));
                await PterodactylAPI.Decompress($"/.tmp/{rng}", release.FileName);
                
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"**[{index}/{hard.Count + 1}]** Step 3/3: Finishing up `{info.Title}` version `{release.Version}`"));
                var folderInfo = await PterodactylAPI.GetFolderContents($"/.tmp/{rng}");
                var folder = folderInfo.Files.First(x => !x.Attributes.IsFile);
                await PterodactylAPI.Rename($"/.tmp/{rng}", folder.Attributes.Name,
                    $"../../mods/{info.Name}_{release.Version}");
                await PterodactylAPI.Delete("/.tmp", rng);
                
                ModManager.Mods!.Add(new ModManager.ModEntry {
                    Url = $"https://mods.factorio.com/mod/{HttpUtility.UrlEncode(info.Name)}",
                    Directory = $"{info.Name}_{release.Version}", Name = info.Name,
                    Title = info.Title, Version = release.Version, Enabled = true
                });
                await ModManager.UpdateMods();
            }

            var failed = new List<string>();
            await InstallMod(info, release, 1);
            for (var i = 0; i < hard.Count; i++) {
                var dependency = hard[i];
                var depInfo = await FactorioAPI.GetModInfo(dependency.ModName);
                var depRelease = depInfo.Releases.LastOrDefault(x => dependency.Matches(x.Version));
                if (depRelease == null) {
                    failed.Add($"`- {depInfo.Name} {dependency.Operator} {dependency.ComparedTo}`");
                    continue;
                }
                await InstallMod(depInfo, depRelease, i + 2);
            }
            
            var builder = new StringBuilder();
            builder.AppendLine($"Successfully installed `{info.Title}` version `{version}`!");
            var soft = deps.Where(x => x.Type is Dependency.TypeEnum.Optional).ToList();
            if (soft.Count != 0) {
                builder.AppendLine($"\n**Note: Found {soft.Count} optional dependencies:**");
                foreach (var i in soft) 
                    builder.AppendLine(i.Operator != null
                        ? $"`- {i.ModName} {i.Operator} {i.ComparedTo}`"
                        : $"`- {i.ModName}`");
            }
            
            if (failed.Count != 0) {
                builder.AppendLine($"\n**Note: Failed to find a matching version of dependency {soft.Count}:**");
                foreach (var i in failed) builder.AppendLine(i);
            }
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(builder.ToString()));
        } catch (Exception e) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Caught an exception, installation failed!"));
            Log.Error("{0}", e);
        }
    }
}