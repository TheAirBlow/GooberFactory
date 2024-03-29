using DSharpPlus.SlashCommands;

namespace GooberFactory.Attributes; 

/// <summary>
/// Makes sure that modlist synchronization has finished
/// </summary>
public class SyncFinishedAttribute : SlashCheckBaseAttribute {
    /// <summary>
    /// Executes the check
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <returns>Boolean</returns>
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        => Task.FromResult(ModManager.Mods != null);
}