using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SkinHunterWPF.Models;
using System.Linq; // Required for Linq methods

namespace SkinHunterWPF.Services
{
    public static class CdragonDataService
    {
        private static readonly HttpClient _httpClient = new(); // Use primary constructor if available
        private const string CdragonBaseUrl = "https://raw.communitydragon.org/latest";
        private const string DataRoot = $"{CdragonBaseUrl}/plugins/rcp-be-lol-game-data/global/default";
        private static string? _cdragonVersion;

        private static async Task<string> GetCdragonVersionAsync()
        {
            if (_cdragonVersion == null)
            {
                try
                {
                    var metaUrl = $"{CdragonBaseUrl}/content-metadata.json";
                    var response = await _httpClient.GetAsync(metaUrl);
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStreamAsync();
                    var metadata = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(json);
                    if (metadata != null && metadata.TryGetValue("version", out var versionElement))
                    {
                        _cdragonVersion = versionElement.GetString() ?? "latest";
                    }
                    else
                    {
                        _cdragonVersion = "latest";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching CDRAGON version: {ex.Message}");
                    _cdragonVersion = "latest";
                }
            }
            return _cdragonVersion;
        }

        private static async Task<T?> FetchDataAsync<T>(string relativePath) where T : class
        {
            try
            {
                _ = await GetCdragonVersionAsync(); // Ensure version is fetched, don't need the value here
                var url = $"{DataRoot}/{relativePath}";
                Console.WriteLine($"Fetching: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<T>(jsonStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Error fetching {relativePath}: {httpEx.Message} (Status: {httpEx.StatusCode})");
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON Error parsing {relativePath}: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Generic Error fetching {relativePath}: {ex.Message}");
            }
            return null;
        }

        public static async Task<List<ChampionSummary>?> GetChampionSummariesAsync()
        {
            var summaries = await FetchDataAsync<List<ChampionSummary>>("v1/champion-summary.json");
            return summaries?
                .Where(c => c.Id != -1)
                .OrderBy(c => c.Name)
                .ToList();
        }

        public static async Task<Dictionary<string, Skin>?> GetAllSkinsAsync()
        {
            return await FetchDataAsync<Dictionary<string, Skin>>("v1/skins.json");
        }

        public static async Task<ChampionDetail?> GetChampionDetailsAsync(int championId)
        {
            var details = await FetchDataAsync<ChampionDetail>($"v1/champions/{championId}.json");

            if (details != null)
            {
                var allSkins = await GetAllSkinsAsync();
                if (allSkins != null)
                {
                    details.Skins = allSkins.Values
                                        .Where(s => s.ChampionId == championId)
                                        .OrderBy(s => s.Name)
                                        .ToList();

                    // Placeholder for actual chroma merging logic
                    await MergeChromaDataAsync(details.Skins);
                }
            }
            return details;
        }

        // Replace this with actual logic based on where chroma data comes from
        private static async Task MergeChromaDataAsync(List<Skin> skins)
        {
            // Example: Fetch chroma data from another source if needed
            // var championChromaSource = await FetchChampionChromaData(skins.FirstOrDefault()?.ChampionId);

            foreach (var skin in skins)
            {
                // Find and assign chromas to skin.Chromas
                // Simulating finding chromas for some skins
                if (skin.Id % 7 == 0 && skin.Name != "Original" && !skin.Name.Contains("Base "))
                {
                    skin.Chromas = new List<Chroma>
                     {
                         new Chroma { Id = skin.Id * 10 + 1, Name = "Ruby", Colors = new List<string> { "#E53935", "#B71C1C" }, ChromaPath = "/lol-game-data/assets/v1/champion-chroma-images/dummy/chroma1.png" },
                         new Chroma { Id = skin.Id * 10 + 2, Name = "Emerald", Colors = new List<string> { "#43A047" }, ChromaPath = "/lol-game-data/assets/v1/champion-chroma-images/dummy/chroma2.png" },
                         new Chroma { Id = skin.Id * 10 + 3, Name = "Sapphire", Colors = new List<string> { "#1E88E5" }, ChromaPath = "/lol-game-data/assets/v1/champion-chroma-images/dummy/chroma3.png" },
                         new Chroma { Id = skin.Id * 10 + 4, Name = "Obsidian", Colors = new List<string> { "#424242", "#212121" }, ChromaPath = "/lol-game-data/assets/v1/champion-chroma-images/dummy/chroma4.png" },
                         new Chroma { Id = skin.Id * 10 + 5, Name = "Pearl", Colors = new List<string> { "#EEEEEE" }, ChromaPath = "/lol-game-data/assets/v1/champion-chroma-images/dummy/chroma5.png" }
                     };
                }
                else
                {
                    skin.Chromas = null; // Ensure it's null if no chromas found
                }
            }
            await Task.CompletedTask; // Remove if actual async work is done
        }


        public static string GetAssetUrl(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return "pack://application:,,,/Assets/placeholder.png";
            }
            const string apiAssetPrefix = "/lol-game-data/assets";
            if (relativePath.StartsWith(apiAssetPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var pathSegment = relativePath.Substring(apiAssetPrefix.Length).TrimStart('/');
                return $"{DataRoot}/{pathSegment}".ToLowerInvariant();
            }
            // Console.WriteLine($"Warning: Asset path '{relativePath}' did not match expected prefix '{apiAssetPrefix}'. Assuming relative to DataRoot.");
            return $"{DataRoot}/{relativePath.TrimStart('/')}".ToLowerInvariant();
        }
    }
}