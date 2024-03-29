using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GooberFactory.WebAPI;

namespace GooberFactory.Providers;

/// <summary>
/// Provides list of mod versions
/// </summary>
public class ModVersionProvider : IAutocompleteProvider {
    /// <summary>
    /// Runs the provider
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <returns>List of choices</returns>
    public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx) {
        try {
            var info = await FactorioAPI.GetModInfo((string)ctx.Options[0].Value);
            var list = info.Releases.Select(x => new DiscordAutoCompleteChoice(x.Version, x.Version));
            if (list.Count() > 10) list = list.Skip(list.Count() - 10);
            return list;
        } catch {
            return new List<DiscordAutoCompleteChoice>();
        }
    }
}