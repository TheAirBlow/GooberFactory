using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace GooberFactory.Providers;

/// <summary>
/// Provides a list of online players
/// </summary>
public class UsernameProvider : IAutocompleteProvider {
    /// <summary>
    /// Runs the provider
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <returns>List of choices</returns>
    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        => Task.FromResult(Program.Socket!.Players.Select(i => new DiscordAutoCompleteChoice(i, i)));
}