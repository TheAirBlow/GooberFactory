using System.Text.Json;
using System.Web;
using GooberFactory.WebAPI;
using Serilog;

namespace GooberFactory;

/// <summary>
/// Server mod manager
/// </summary>
public static class ModManager {
    /// <summary>
    /// List of mods installed
    /// </summary>
    public static List<ModEntry>? Mods;
    
    /// <summary>
    /// Syncs cached modlist with server's modlist
    /// </summary>
    public static async Task SyncMods() {
        Log.Information("Syncing cached modlist...");
        var tmp = new List<ModEntry>();
        var str = await PterodactylAPI.ReadFile("/mods/mod-list.json");
        var list = JsonSerializer.Deserialize<FactorioAPI.ModList>(str)!;
        var contents = await PterodactylAPI.GetFolderContents("/mods");
        var attrs = contents.Files.Select(x => x.Attributes).Where(x => !x.IsFile);
        foreach (var attr in attrs) {
            str = await PterodactylAPI.ReadFile($"/mods/{attr.Name}/info.json");
            var info = JsonSerializer.Deserialize<FactorioAPI.ModJson>(str)!;
            tmp.Add(new ModEntry {
                Url = $"https://mods.factorio.com/mod/{HttpUtility.UrlEncode(info.Name)}",
                Title = info.Title, Version = info.Version, Name = info.Name,
                Enabled = list.Mods.First(x => x.Name == info.Name).Enabled,
                Directory = attr.Name
            });
        }

        Mods = tmp;
    }

    /// <summary>
    /// Update the mod-list.json file
    /// </summary>
    public static async Task UpdateMods() {
        var list = new FactorioAPI.ModList {
            Mods = Mods!.Select(x => new FactorioAPI.ModList.Mod {
                Name = x.Name, Enabled = x.Enabled
            }).ToList()
        };
        var str = JsonSerializer.Serialize(list);
        await PterodactylAPI.WriteFile("/mods/mod-list.json", str);
    }
    
    /// <summary>
    /// Simplified mod JSON
    /// </summary>
    public class ModEntry {
        /// <summary>
        /// Mod's display title
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Mod's version
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// Mod's identifier
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Mod portal URL
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Mod directory name
        /// </summary>
        public string Directory { get; set; }
        
        /// <summary>
        /// Is mod enabled
        /// </summary>
        public bool Enabled { get; set; }
    }
}