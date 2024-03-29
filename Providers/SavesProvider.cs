using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GooberFactory.WebAPI;
using Humanizer;

namespace GooberFactory.Providers;

/// <summary>
/// Provides a list of saves
/// </summary>
public class SavesProvider : IAutocompleteProvider {
    /// <summary>
    /// Runs the provider
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <returns>List of choices</returns>
    public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx) {
        var attrs = await PterodactylAPI.GetFolderContents("/saves");
        var files = attrs.Files.OrderBy(x => x.Attributes.ModifiedAt).Take(10);
        return files.Select(x => new DiscordAutoCompleteChoice(
            $"{x.Attributes.Name} ({x.Attributes.ModifiedAt.Humanize()})", x.Attributes.Name));
    }
}