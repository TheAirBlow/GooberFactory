using static GooberFactory.Configuration;
using Serilog;

namespace GooberFactory;

/// <summary>
/// Entrypoint class
/// </summary>
public static class Program {
    /// <summary>
    /// Pterodactyl socket
    /// </summary>
    public static PteroSocket? Socket;
    
    /// <summary>
    /// Main entrypoint
    /// </summary>
    /// <param name="args">Arguments</param>
    public static async Task Main(string[] args) {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        Log.Information("Welcome to Goober's Factory v2.0!");
        Config.Save();
        await Discord.Initialize();
        await ModManager.SyncMods();
        await InitSocket();
        await Task.Delay(-1);
    }
    
    /// <summary>
    /// Initializes Pterodactyl WebSockets client
    /// </summary>
    public static async Task InitSocket() {
        if (Socket != null) {
            await Discord.Send(Config.Messages.Closed);
            Log.Information("Reconnecting to Pterodactyl WebSocket...");
        } else Log.Information("Connecting to Pterodactyl WebSocket...");
        
        Socket = new PteroSocket();
        try {
            await Socket.Connect();
            await Discord.Send(Config.Messages.Ready);
        } catch (Exception e) {
            Log.Error("Failed to connect to WebSockets: {0}", e);
            await Discord.Send(Config.Messages.Failed);
        }
    }
}