using static GooberFactory.Configuration;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GooberFactory.WebAPI;
using Serilog;

namespace GooberFactory;

/// <summary>
/// Pterodactyl WebSocket Connection
/// </summary>
public class PteroSocket : IDisposable {
    /// <summary>
    /// Goober's Factory Integration script version required
    /// </summary>
    private const string IntegrationVersion = "2.0";
    
    /// <summary>
    /// List of online players
    /// </summary>
    public List<string> Players = new();
    
    /// <summary>
    /// WebSockets connection
    /// </summary>
    public ClientWebSocket Socket = new();

    /// <summary>
    /// Current server state
    /// </summary>
    public ServerState State;
    
    /// <summary>
    /// Statistics information
    /// </summary>
    public StatsJson? Stats;

    /// <summary>
    /// Was this socket disposed
    /// </summary>
    private bool _disposed = true;

    /// <summary>
    /// Initiates a WebSockets connection
    /// </summary>
    public async Task Connect() {
        var data = await PterodactylAPI.Websocket();
        Socket.Options.SetRequestHeader("Origin", Config.Pterodactyl.PanelUrl);
        await Socket.ConnectAsync(new Uri(data.URL), CancellationToken.None);
        new Thread(async () => await ListenerThread()).Start();
        await SendEvent("auth", data.Token);
    }

    /// <summary>
    /// Incoming listener thread
    /// </summary>
    private async Task ListenerThread() {
        try {
            var buf = new byte[4096];
            while (Socket.State == WebSocketState.Open) {
                var result = await Socket.ReceiveAsync(buf, CancellationToken.None);
                switch (result.MessageType) {
                    case WebSocketMessageType.Text:
                        var msg = Encoding.UTF8.GetString(buf, 0, result.Count);
                        await HandleMessage(msg);
                        break;
                    case WebSocketMessageType.Close:
                        await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        break;
                }
            }
            
            Log.Warning("WebSockets connection was remotely closed!");
        } catch (ObjectDisposedException) {
            Log.Warning("WebSockets connection was disposed!");
        } catch (Exception e) {
            Log.Error("Listener thread crashed: {0}", e);
        }
        
        if (!_disposed) { // don't do anything if disposed manually
            Dispose(); await Program.InitSocket();
        }
    }

    /// <summary>
    /// Handles a message
    /// </summary>
    private async Task HandleMessage(string msg) {
        var ev = JsonSerializer.Deserialize<EventJson>(msg)!;
        switch (ev.Event) {
            case "token expiring":
                var data = await PterodactylAPI.Websocket();
                await SendEvent("auth", data.Token);
                break;
            case "token expired":
                Socket.Dispose(); // tactical suicide
                break;
            case "status":
                await HandleState(ev.Arguments[0]);
                break;
            case "stats":
                Stats = JsonSerializer.Deserialize<StatsJson>(ev.Arguments[0])!;
                await HandleState(Stats.State);
                break;
            case "console output":
                await HandleOutput(ev.Arguments[0]);
                break;
        }
    }

    /// <summary>
    /// Handles state change
    /// </summary>
    /// <param name="state">State</param>
    private async Task HandleState(string state) {
        var old = State;
        State = state switch {
            "starting" => ServerState.Starting,
            "stopping" => ServerState.Stopping,
            "offline" => ServerState.Offline,
            _ => ServerState.Running
        };
        
        if (old == State) return;
        await Discord.UpdateStatus();
        if (State == ServerState.Running) {
            await SendCommand("/gfi-version");
            await SendCommand("/gfi-list");
        }
    }
    
    /// <summary>
    /// Handles console output
    /// </summary>
    /// <param name="log">Log Line</param>
    private async Task HandleOutput(string log) {
        if (log.StartsWith("Unknown command \"gfi-version\".")) {
            await Discord.Send(
                "**Hey, you forgot to add the Goober's Factory Integration script into your save file!**\n" +
                "It is necessary for this bot to work properly. Now bye, I'm commiting tactical suicide!");
            Dispose(); return;
        }
        
        var match = Regex.Match(log, "[0-9]*\\.[0-9]* Info AppManagerStates.cpp:[0-9]*: Saving finished");
        if (match.Success) {
            await Discord.Send(Config.Messages.Saved);
            return;
        }
        
        match = Regex.Match(log, "\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2} \\[(.*)\\] (.*)");
        if (match.Success) {
            var message = match.Groups[2].Value;
            switch (match.Groups[1].Value) {
                case "CHAT": {
                    var index = message.IndexOf(": ");
                    var author = message[..index];
                    var content = message[(index + 2)..];
                    if (author == "<server>" && content.StartsWith(Config.Messages.DiscordPrefix))
                        break;
                    await Discord.Send(string.Format(Config.Messages.DiscordMessage, author, content));
                    break;
                }
                case "JOIN": {
                    var index = message.LastIndexOf(" joined the game");
                    var username = message[..index];
                    Players.Add(username);
                    await Discord.UpdatePlayers();
                    await Discord.Send(string.Format(
                        Config.Messages.Joined, username));
                    break;
                }
                case "LEAVE": {
                    var index = message.LastIndexOf(" left the game");
                    var username = message[..index];
                    Players.Remove(username);
                    await Discord.UpdatePlayers();
                    await Discord.Send(string.Format(
                        Config.Messages.Left, username));
                    break;
                }
                case "KICK": {
                    var kick = Regex.Match(message, "(.*) was kicked by (.*). Reason: (.*).");
                    if (!kick.Success) break;
                    await Discord.Send(string.Format(Config.Messages.Kicked, 
                        kick.Groups[1].Value, kick.Groups[2].Value, kick.Groups[3].Value));
                    break;
                }
                case "BAN": {
                    var kick = Regex.Match(message, "(.*) was banned by (.*). Reason: (.*).");
                    if (!kick.Success) break;
                    await Discord.Send(string.Format(Config.Messages.Banned, 
                        kick.Groups[1].Value, kick.Groups[2].Value, kick.Groups[3].Value));
                    break;
                }
                case "UNBANNED": {
                    var kick = Regex.Match(message, "(.*) was unbanned by (.*).");
                    if (!kick.Success) break;
                    await Discord.Send(string.Format(Config.Messages.Unbanned, 
                        kick.Groups[1].Value, kick.Groups[2].Value));
                    break;
                }
                case "ROCKET": {
                    var kick = Regex.Match(message, "(.*) launched a rocket!");
                    if (!kick.Success) break;
                    await Discord.Send(string.Format(
                        Config.Messages.Rocket, kick.Groups[1].Value));
                    break;
                }
                case "DIED": {
                    var kick = Regex.Match(message, "(.*) at ([-0-9]*) ([-0-9]*)!");
                    if (!kick.Success) break;
                    await Discord.Send(string.Format(Config.Messages.Died,
                        kick.Groups[1].Value, kick.Groups[2].Value, kick.Groups[3].Value));
                    break;
                }
                case "DISCONNECT": {
                    var kick = Regex.Match(message, "(.*) disconnected (.*)");
                    if (!kick.Success) break;
                    Players.Remove(kick.Groups[1].Value);
                    await Discord.UpdatePlayers();
                    await Discord.Send(string.Format(Config.Messages.Disconnected,
                        kick.Groups[1].Value, kick.Groups[2].Value));
                    break;
                }
                case "RESPAWN": {
                    var kick = Regex.Match(message, "(.*) respawned!");
                    if (!kick.Success) break;
                    await Discord.Send(string.Format(
                        Config.Messages.Respawned, kick.Groups[1].Value));
                    break;
                }
                case "RESEARCH": {
                    if (message.StartsWith("Started research")) {
                        var kick = Regex.Match(message, "Started research for (.*)!");
                        if (!kick.Success) break;
                        await Discord.Send(string.Format(
                            Config.Messages.ResearchStarted, kick.Groups[1].Value));
                        return;
                    }
                    
                    if (message.StartsWith("Research changed")) {
                        var kick = Regex.Match(message, "Research changed from (.*) to (.*)!");
                        if (!kick.Success) break;
                        await Discord.Send(string.Format(Config.Messages.ResearchChanged,
                            kick.Groups[1].Value, kick.Groups[2].Value));
                        return;
                    }
                    
                    var kick1 = Regex.Match(message, "Finished (.*)!");
                    if (!kick1.Success) break;
                    await Discord.Send(string.Format(
                        Config.Messages.ResearchFinished,kick1.Groups[1].Value));
                    break;
                }
                case "LIST": {
                    Players = message[16..].Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList();
                    await Discord.UpdatePlayers();
                    break;
                }
                case "GFI": {
                    var index = message.IndexOf("version ");
                    var version = message[(index + 8)..];
                    if (version != IntegrationVersion) {
                        await Discord.Send(
                            "**Hey, the Goober's Factory Integration script is too old or too new for me!**\n" +
                            $"I expected {IntegrationVersion}, but got {version} instead. Now bye, I'm commiting tactical suicide!");
                        Dispose();
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Sends an event message
    /// </summary>
    /// <param name="event">Event</param>
    /// <param name="args">Arguments</param>
    private async Task SendEvent(string @event, params string[] args)
        => await Socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new EventJson(@event, args)),
            WebSocketMessageType.Text, true, CancellationToken.None);
    
    /// <summary>
    /// Sends a command to the server
    /// </summary>
    /// <param name="command">Command</param>
    public async Task SendCommand(string command)
        => await SendEvent("send command", command);
    
    /// <summary>
    /// Performs a power action
    /// </summary>
    /// <param name="action">Power Action</param>
    public async Task DoAction(PowerAction action)
        => await SendEvent("set state", action switch {
            PowerAction.Restart => "restart", PowerAction.Start => "start", PowerAction.Stop => "stop",
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        });

    /// <summary>
    /// Disposes current socket connection
    /// </summary>
    public void Dispose() {
        _disposed = true;
        Socket.Dispose();
    }

    /// <summary>
    /// Server state
    /// </summary>
    public enum ServerState {
        Offline, Starting, Running, Stopping
    }
    
    /// <summary>
    /// Power action
    /// </summary>
    public enum PowerAction {
        Start, Stop, Restart
    }

    /// <summary>
    /// Statistics JSON
    /// </summary>
    public class StatsJson {
        /// <summary>
        /// Network usage info
        /// </summary>
        public class NetworkInfo {
            /// <summary>
            /// Received (downloaded) in bytes
            /// </summary>
            [JsonPropertyName("rx_bytes")]
            public long Received;

            /// <summary>
            /// Transmitted (uploaded) in bytes
            /// </summary>
            [JsonPropertyName("tx_bytes")]
            public long Transmitted;
        }
        
        /// <summary>
        /// Used amount of memory in bytes
        /// </summary>
        [JsonPropertyName("memory_bytes")]
        public long MemoryUsage { get; set; }
        
        /// <summary>
        /// Total amount of memory in bytes
        /// </summary>
        [JsonPropertyName("memory_limit_bytes")]
        public long TotalMemory { get; set; }
        
        /// <summary>
        /// Absolute CPU usage
        /// </summary>
        [JsonPropertyName("cpu_absolute")]
        public float CpuUsage { get; set; }
        
        /// <summary>
        /// Used amount of disk memory in bytes
        /// </summary>
        [JsonPropertyName("disk_bytes")]
        public long DiskUsage { get; set; }
        
        /// <summary>
        /// Current state (do not use)
        /// </summary>
        [JsonPropertyName("state")]
        public string State { get; set; }
        
        /// <summary>
        /// Network information
        /// </summary>
        [JsonPropertyName("network")]
        public NetworkInfo Network { get; set; }
    }
    
    /// <summary>
    /// An event JSON
    /// </summary>
    private class EventJson {
        /// <summary>
        /// Event name
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        /// <summary>
        /// Event arguments
        /// </summary>
        [JsonPropertyName("args")]
        public string[] Arguments { get; set; }
        
        /// <summary>
        /// Empty constructor
        /// </summary>
        public EventJson() { }

        /// <summary>
        /// Creates a new event JSON
        /// </summary>
        /// <param name="event">Event</param>
        /// <param name="args">Arguments</param>
        public EventJson(string @event, params string[] args) {
            Event = @event; Arguments = args;
        }
    }
}