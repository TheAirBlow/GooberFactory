using static GooberFactory.Configuration;
using DSharpPlus.SlashCommands;

namespace GooberFactory.Attributes; 

/// <summary>
/// Checks that the user running this command is an administrator
/// </summary>
public class AdminOnlyAttribute : SlashCheckBaseAttribute {
    /// <summary>
    /// Executes the check
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <returns>Boolean</returns>
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        => Task.FromResult(Config.Discord.AdminRoles.Any(x => 
            ctx.Member.Roles.Any(y => y.Id == x)));
}