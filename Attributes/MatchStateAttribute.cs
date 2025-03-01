using DSharpPlus.SlashCommands;

namespace GooberFactory.Attributes; 

/// <summary>
/// Checks that the server's state matches, e.g. is running or offline
/// </summary>
public class MatchStateAttribute : SlashCheckBaseAttribute {
    /// <summary>
    /// Server's state to match for
    /// </summary>
    public readonly PteroSocket.ServerState State;
    
    /// <summary>
    /// True if check failed because socket was null
    /// </summary>
    public bool FailedBecauseNull { get; private set; }
    
    /// <summary>
    /// Creates a new match server state attribute
    /// </summary>
    /// <param name="state">Server State</param>
    public MatchStateAttribute(PteroSocket.ServerState state)
        => State = state;

    /// <summary>
    /// Executes the check
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <returns>Boolean</returns>
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx) {
        if (Program.Socket != null) return Task.FromResult(Program.Socket.State == State);
        FailedBecauseNull = true; return Task.FromResult(false);
    }
}