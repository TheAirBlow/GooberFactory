using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using static GooberFactory.Configuration;

namespace GooberFactory.WebAPI;

/// <summary>
/// Pterodactyl API
/// </summary>
public static class PterodactylAPI {
    /// <summary>
    /// Pterodactyl HTTP client
    /// </summary>
    private static readonly HttpClient _client = new();

    /// <summary>
    /// Configures the HTTP client
    /// </summary>
    static PterodactylAPI() {
        _client.BaseAddress = new Uri(Config.Pterodactyl.PanelUrl);
        _client.DefaultRequestHeaders.Add("Authorization",
            $"Bearer {Config.Pterodactyl.ApiToken}");
        _client.Timeout = TimeSpan.FromSeconds(10);
    }
    
    /// <summary>
    /// Fetch the websockets URL and Token
    /// </summary>
    /// <returns>URL and Token</returns>
    public static async Task<WebsocketJson.DataObject> Websocket() {
        var resp = await _client.GetAsync($"/api/client/servers/{Config.Pterodactyl.ServerId}/websocket");
        var json = JsonSerializer.Deserialize<WebsocketJson>(await resp.Content.ReadAsStringAsync());
        return json!.Data;
    }
    
    /// <summary>
    /// Reads file's contents as string
    /// </summary>
    /// <param name="path">Path</param>
    /// <returns>File's contents</returns>
    public static async Task<string> ReadFile(string path) {
        var resp = await _client.GetAsync(
            $"/api/client/servers/{Config.Pterodactyl.ServerId}" +
            $"/files/contents?file={HttpUtility.UrlEncode(path)}");
        
        return await resp.Content.ReadAsStringAsync();
    }
    
    /// <summary>
    /// Writes a string into a file
    /// </summary>
    /// <param name="path">File Path</param>
    /// <param name="content">New Content</param>
    public static async Task WriteFile(string path, string content) {
        var resp = await _client.PostAsync(
            $"/api/client/servers/{Config.Pterodactyl.ServerId}/files/write?file={HttpUtility.UrlEncode(path)}",
            new StringContent(content));
        resp.EnsureSuccessStatusCode();
    }
    
    /// <summary>
    /// Uploads a file in the specified directory
    /// </summary>
    /// <param name="stream">File stream</param>
    /// <param name="filename">Filename</param>
    /// <param name="directory">Directory</param>
    public static async Task UploadFile(Stream stream, string filename, string directory) {
        var resp = await _client.GetAsync($"/api/client/servers/{Config.Pterodactyl.ServerId}/files/upload");
        var json = JsonSerializer.Deserialize<AttributesJson>(await resp.Content.ReadAsStringAsync());
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.Add("Content-Disposition", 
            $"form-data; name=\"files\"; filename=\"{filename}\"");
        streamContent.Headers.Add("Content-Type", "application/zip");
        content.Add(streamContent);
        resp = await _client.SendAsync(
            new HttpRequestMessage {
                RequestUri = new Uri($"{json!.Attributes.URL}&directory={directory}"),
                Content = content, Method = HttpMethod.Post
            });
        resp.EnsureSuccessStatusCode();
    }
    
    /// <summary>
    /// Compresses files
    /// </summary>
    /// <param name="root">Root Directory</param>
    /// <param name="filenames">Filenames</param>
    /// <returns>Archive filename</returns>
    public static async Task<string> Compress(string root, params string[] filenames) {
        var resp = await _client.PostAsync(
            $"/api/client/servers/{Config.Pterodactyl.ServerId}/files/compress",
            JsonContent.Create(new BulkFileJson {
                Root = root, Files = filenames
            }));
        resp.EnsureSuccessStatusCode();
        var json = JsonSerializer.Deserialize<AttributesJson>(
            await resp.Content.ReadAsStringAsync());
        return json!.Attributes.Name;
    }
    
    /// <summary>
    /// Extracts an archive
    /// </summary>
    /// <param name="directory">Directory</param>
    /// <param name="filename">Filename</param>
    public static async Task Decompress(string directory, string filename) {
        var res = await _client.PostAsync(
            $"/api/client/servers/{Config.Pterodactyl.ServerId}/files/decompress",
            JsonContent.Create(new DecompressJson {
                Root = directory, File = filename
            }));
        res.EnsureSuccessStatusCode();
    }
    
    /// <summary>
    /// Deletes a file or a directory
    /// </summary>
    /// <param name="root">Root directory</param>
    /// <param name="filenames">Filenames</param>
    public static async Task Delete(string root, params string[] filenames) {
        var resp = await _client.PostAsync(
            $"/api/client/servers/{Config.Pterodactyl.ServerId}/files/delete",
            JsonContent.Create(new BulkFileJson {
                Root = root, Files = filenames
            }));
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Renames a file or directory
    /// </summary>
    /// <param name="root">Root directory</param>
    /// <param name="from">Original name</param>
    /// <param name="to">New name</param>
    public static async Task Rename(string root, string from, string to) {
        var resp = await _client.PostAsync(
            $"/api/client/servers/{Config.Pterodactyl.ServerId}/files/rename",
            JsonContent.Create(new RenameJson {
                Root = root, Files = [new RenameJson.Entry { From = from, To = to }]
            }));
        resp.EnsureSuccessStatusCode();
    }
    
    /// <summary>
    /// Fetches download link for file
    /// </summary>
    /// <param name="path">File Path</param>
    /// <returns>Download Link</returns>
    public static async Task<string> Download(string path) {
        var res = await _client.GetAsync(
            $"/api/client/servers/{Config.Pterodactyl.ServerId}" +
            $"/files/download?file={HttpUtility.UrlEncode(path)}");
        res.EnsureSuccessStatusCode();
        var json = JsonSerializer.Deserialize<AttributesJson>(await res.Content.ReadAsStringAsync());
        return json!.Attributes.URL;
    }

    /// <summary>
    /// Returns a folder's contents
    /// </summary>
    /// <param name="directory">Directory Path</param>
    /// <returns>Folder Info JSON</returns>
    public static async Task<FolderInfo> GetFolderContents(string directory) {
        var res = await _client.GetAsync(
            $"/api/client/servers/{Config.Pterodactyl.ServerId}" +
            $"/files/list?directory={HttpUtility.UrlEncode(directory)}");
        res.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<FolderInfo>(await res.Content.ReadAsStringAsync())!;
    }
    
    /// <summary>
    /// Websocket response JSON
    /// </summary>
    public class WebsocketJson {
        public class DataObject {
            [JsonPropertyName("socket")]
            public string URL { get; set; }
            
            [JsonPropertyName("token")]
            public string Token { get; set; }
        }

        [JsonPropertyName("data")]
        public DataObject Data { get; set; }
    }
    
    /// <summary>
    /// Generic object attributes JSON
    /// </summary>
    private class AttributesJson {
        public class Attribute {
            [JsonPropertyName("url")]
            public string URL { get; set; }
            
            [JsonPropertyName("name")]
            public string Name { get; set; }
        }
        
        [JsonPropertyName("attributes")]
        public Attribute Attributes { get; set; }
    }
    
    /// <summary>
    /// A folder's content listing
    /// </summary>
    public class FolderInfo {
        /// <summary>
        /// File object attributes
        /// </summary>
        public class Attributes {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            
            [JsonPropertyName("is_file")]
            public bool IsFile { get; set; }
        }
        
        /// <summary>
        /// A file object
        /// </summary>
        public class FileObject {
            [JsonPropertyName("attributes")]
            public Attributes Attributes { get; set; }
        }
        
        [JsonPropertyName("data")]
        public List<FileObject> Files { get; set; }
    }
    
    /// <summary>
    /// Generic bulk file operation JSON
    /// </summary>
    private class BulkFileJson {
        [JsonPropertyName("root")]
        public string Root { get; set; }
        
        [JsonPropertyName("files")]
        public string[] Files { get; set; }
    }
    
    /// <summary>
    /// Rename request JSON
    /// </summary>
    private class RenameJson {
        /// <summary>
        /// A rename entry
        /// </summary>
        public class Entry {
            [JsonPropertyName("from")]
            public string From { get; set; }
            
            [JsonPropertyName("to")]
            public string To { get; set; }
        }
        
        [JsonPropertyName("root")]
        public string Root { get; set; }
        
        [JsonPropertyName("files")]
        public List<Entry> Files { get; set; }
    }
    
    /// <summary>
    /// Decompression request JSON
    /// </summary>
    private class DecompressJson {
        [JsonPropertyName("root")]
        public string Root { get; set; }
        
        [JsonPropertyName("file")]
        public string File { get; set; }
    }
}