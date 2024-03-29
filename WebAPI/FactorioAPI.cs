using System.Text.Json;
using System.Text.Json.Serialization;
using static GooberFactory.Configuration;

namespace GooberFactory.WebAPI;

/// <summary>
/// Factorio API
/// </summary>
public static class FactorioAPI {
    /// <summary>
    /// Factorio HTTP client
    /// </summary>
    private static readonly HttpClient _client = new();

    /// <summary>
    /// Configures the HTTP client
    /// </summary>
    static FactorioAPI() {
        _client.BaseAddress = new Uri(Config.ProxyUrl);
        _client.Timeout = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Fetches information about a mod by it's name
    /// </summary>
    /// <param name="name">Mod's name</param>
    /// <returns>Mod info JSON</returns>
    public static async Task<ModInfo> GetModInfo(string name) {
        var res = await _client.SendAsync(
            new HttpRequestMessage {
                RequestUri = new Uri($"https://mods.factorio.com/api/mods/{name}/full"),
                Method = HttpMethod.Get
            });
        res.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<ModInfo>(await res.Content.ReadAsStringAsync())!;
    }
    
    /// <summary>
    /// The mod list
    /// </summary>
    public class ModList {
        public class Mod {
            [JsonPropertyName("name")]
            public string Name { get; set; }
        
            [JsonPropertyName("enabled")]
            public bool Enabled { get; set; }
        }

        [JsonPropertyName("mods")]
        public List<Mod> Mods { get; set; }
    }
    
    /// <summary>
    /// A mod's info.json
    /// </summary>
    public class ModJson {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("version")]
        public string Version { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("dependencies")]
        public List<string> Dependencies { get; set; }
    }
    
    /// <summary>
    /// Information about a mod
    /// </summary>
    public class ModInfo {
        /// <summary>
        /// A mod's release
        /// </summary>
        public class Release {
            [JsonPropertyName("download_url")]
            public string DownloadURL { get; set; }
            
            [JsonPropertyName("file_name")]
            public string FileName { get; set; }
            
            [JsonPropertyName("version")]
            public string Version { get; set; }
            
            [JsonPropertyName("info_json")]
            public ModJson InfoJson { get; set; }
        }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("summary")]
        public string Summary { get; set; }
        
        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }
        
        [JsonPropertyName("releases")]
        public List<Release> Releases { get; set; }
    }
}