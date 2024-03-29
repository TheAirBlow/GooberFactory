using System.Text.Encodings.Web;
using System.Text.Json;
using Serilog;

namespace GooberFactory; 

/// <summary>
/// Global configuration file
/// </summary>
public class Configuration {
    /// <summary>
    /// JSON serializer options
    /// </summary>
    private static JsonSerializerOptions _options = new() {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true, IncludeFields = true
    };
    
    /// <summary>
    /// Static object instance
    /// </summary>
    public static Configuration Config;

    /// <summary>
    /// Loads the configuration file
    /// </summary>
    static Configuration() {
        Log.Information("Parsing configuration file");
        if (File.Exists("config.json")) {
            var content = File.ReadAllText("config.json");
            try {
                Config = JsonSerializer.Deserialize<Configuration>(content, _options)!;
            } catch (Exception e) {
                Log.Fatal("Failed to parse configuration file!");
                Log.Fatal("{0}", e);
                Environment.Exit(-1);
            }
            return;
        }

        Config = new Configuration(); Config.Save();
        Log.Fatal("No configuration file was found, created an blank one!");
        Log.Fatal("Populate it with all information required.");
        Environment.Exit(-1);
    }

    /// <summary>
    /// Messages information
    /// </summary>
    public class MessagesInfo {
        // Research
        public string ResearchChanged = "<:kinggrr:1138432214120550470> **Changed research from {0} to {1}!**";
        public string ResearchStarted = "<:kingthumbsup:1138432233091383326> **Started research for {0}!**";
        public string ResearchFinished = "<:kinghehe:1138432223821975645> **Finished research for {0}!**";
        
        // Server and bot status
        public string Closed = "<:FakeNitroEmoji:1207738977826644021> **WebSockets connection was closed!**";
        public string Starting = "<:awooga:1138432126820286496> **Server is starting, give me a moment...**";
        public string Started = "<:superiorjoe:1013644883791188029> **Server successfully started!**";
        public string Failed = "<:wtf:1138432329782661141> **Failed to connect to WebSockets!**";
        public string Stopped = "<:over:1138432586566340639> **Server successfully stopped!**";
        public string Ready = "<:obama:1138432261411324025> **Discord Relay is ready!**";
        
        // Punishments
        public string Kicked = "<:trollface:1033549664831672382> **{0} got kicked by `{1}` for `{2}`!**";
        public string Unbanned = "<:boykisser:1138432181925052599>  **`{0}` got unbanned by `{1}`!**";
        public string Banned = "<:ballr:1033563203717840906> **{0} got banned by `{1}` for `{2}`!**";
        
        // Player actions
        public string Disconnected = "<:pointandlaugh:1138432278003974185> **{0} disconnected {1}.**";
        public string Left = "<:pointandlaugh:1138432278003974185> **{0} left the game.**";
        public string Joined = "<:awooga:1138432126820286496> **{0} joined the game.**";
        
        // Game events
        public string Rocket = "<:letsgoo:1138432150304210996> **{0} launched a rocket!**";
        public string Respawned = "<:hushbingus:1138432595294691338> **{0} respawned!**";
        public string Saved = "<:mengoke:1138432246299230249> **World map saved!**";
        public string Died = "<:gunpoint:1138432169337950259> **{0} at {1} {2}!**";
        
        // Chat messages
        public string GameMessage = "[color=#7289DA]<[color=#{0}]{1}[/color]>[/color] {2}";
        public string GameReady = "[color=#3EBF24]Discord Relay is ready![/color]";
        public string DiscordPrefix = "[color=#7289DA][Discord][/color]";
        public string ReplyingTo = "Replying to [color=#{0}]{1}[/color]";
        public string Edited = "[color=#D3D3D3](edited)[/color]";
        public string Newline = "[color=#6CFF3B]⬑[/color]";
        public string DiscordMessage = "*{0}*: {1}";
        
        // Mod state
        public string Enabled = "<:online:1083794732490170540>";
        public string Disabled = "<:offline:1083794760608780359>";
    }

    /// <summary>
    /// Pterodactyl information
    /// </summary>
    public class PterodactylInfo {
        /// <summary>
        /// Pterodactyl panel URL
        /// </summary>
        public string PanelUrl = "https://control.sussy.dev";
    
        /// <summary>
        /// Pterodactyl API token
        /// </summary>
        public string ApiToken = "change_me";
    
        /// <summary>
        /// ID of the pterodactyl server
        /// </summary>
        public string ServerId = "change_me";
    }

    /// <summary>
    /// Discord information
    /// </summary>
    public class DiscordInfo {
        /// <summary>
        /// Discord bot token
        /// </summary>
        public string BotToken = "change_me";

        /// <summary>
        /// List of role IDs to treat as admins
        /// </summary>
        public List<ulong> AdminRoles = new();

        /// <summary>
        /// Relay channel ID
        /// </summary>
        public ulong RelayChannel { get; set; }
    
        /// <summary>
        /// Relay channel's guild ID
        /// </summary>
        public ulong RelayGuild { get; set; }
    }

    /// <summary>
    /// Message placeholders
    /// </summary>
    public MessagesInfo Messages = new();

    /// <summary>
    /// Pterodactyl information
    /// </summary>
    public PterodactylInfo Pterodactyl = new();
    
    /// <summary>
    /// Discord information
    /// </summary>
    public DiscordInfo Discord = new();

    /// <summary>
    /// Cracktorio server URL
    /// </summary>
    public string ProxyUrl = "https://mods.sussy.dev";
    
    /// <summary>
    /// Save configuration changes
    /// </summary>
    public void Save() => File.WriteAllText("config.json",
        JsonSerializer.Serialize(Config, _options));
}
