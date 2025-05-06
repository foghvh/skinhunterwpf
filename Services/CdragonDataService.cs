using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SkinHunterWPF.Models;
using System.Linq;
using System.IO;

namespace SkinHunterWPF.Services
{
    public class SupabaseChampionData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("skins")]
        public List<SupabaseSkinData>? Skins { get; set; }
    }

    public class SupabaseSkinData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("chromas")]
        public List<SupabaseChromaData>? Chromas { get; set; }
    }

    public class SupabaseChromaData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("chromaPath")]
        public string ChromaPath { get; set; } = string.Empty;

        [JsonPropertyName("colors")]
        public List<string>? Colors { get; set; }
    }


    public static class CdragonDataService
    {
        private static readonly HttpClient _httpClient = new();
        private const string CdragonBaseUrl = "https://raw.communitydragon.org/latest";
        private const string DataRoot = $"{CdragonBaseUrl}/plugins/rcp-be-lol-game-data/global/default";
        private static string? _cdragonVersion;

        private const string SupabaseUrl = "https://odlqwkgewzxxmbsqutja.supabase.co";
        private const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im9kbHF3a2dld3p4eG1ic3F1dGphIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MzQyMTM2NzcsImV4cCI6MjA0OTc4OTY3N30.qka6a71bavDeUQgy_BKoVavaClRQa_gT36Au7oO9AF0";
        private const string SupabaseStorageBasePath = "/storage/v1/object/public";


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

        private static async Task<T?> FetchDataAsync<T>(string fullUrl, HttpClient? client = null) where T : class
        {
            var httpClientToUse = client ?? _httpClient;
            try
            {
                Console.WriteLine($"Fetching: {fullUrl}");
                var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                if (client != null && client != _httpClient)
                {
                    request.Headers.TryAddWithoutValidation("apikey", SupabaseAnonKey);
                }

                var response = await httpClientToUse.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var jsonStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<T>(jsonStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Error fetching {Path.GetFileName(fullUrl)}: {httpEx.Message} (Status: {httpEx.StatusCode})");
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON Error parsing {Path.GetFileName(fullUrl)}: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Generic Error fetching {Path.GetFileName(fullUrl)}: {ex.Message}");
            }
            return null;
        }

        private static async Task<SupabaseChampionData?> FetchChampionDataFromSupabaseAsync(int championId)
        {
            string bucket = "api_json";
            string filePath = $"{championId}.json";
            string supabaseFileUrl = $"{SupabaseUrl}{SupabaseStorageBasePath}/{bucket}/{filePath}";

            Console.WriteLine($"Attempting to fetch champion JSON from Supabase: {supabaseFileUrl}");
            return await FetchDataAsync<SupabaseChampionData>(supabaseFileUrl);
        }


        public static async Task<List<ChampionSummary>?> GetChampionSummariesAsync()
        {
            _ = await GetCdragonVersionAsync();
            var url = $"{DataRoot}/v1/champion-summary.json";
            var summaries = await FetchDataAsync<List<ChampionSummary>>(url);
            return summaries?
                .Where(c => c.Id != -1)
                .OrderBy(c => c.Name)
                .ToList();
        }

        public static async Task<Dictionary<string, Skin>?> GetAllSkinsAsync()
        {
            _ = await GetCdragonVersionAsync();
            var url = $"{DataRoot}/v1/skins.json";
            return await FetchDataAsync<Dictionary<string, Skin>>(url);
        }

        public static async Task<ChampionDetail?> GetChampionDetailsAsync(int championId)
        {
            _ = await GetCdragonVersionAsync();
            var detailsUrl = $"{DataRoot}/v1/champions/{championId}.json";
            var championDetailWpf = await FetchDataAsync<ChampionDetail>(detailsUrl);

            if (championDetailWpf == null) return null;

            var allSkinsFromCdragon = await GetAllSkinsAsync();
            var championDataFromSupabase = await FetchChampionDataFromSupabaseAsync(championId);

            var skinsForThisChampionWpf = new List<Skin>(); // Esta será la lista final para championDetailWpf.Skins

            if (allSkinsFromCdragon != null)
            {
                // Iterar sobre las skins obtenidas del skins.json de CDRagon
                foreach (var cdragonSkinObject in allSkinsFromCdragon.Values)
                {
                    if (cdragonSkinObject.ChampionId == championId)
                    {
                        // 1. Crear una NUEVA instancia de nuestro modelo Skin (WPF) para esta skin
                        var currentWpfSkin = new Skin
                        {
                            Id = cdragonSkinObject.Id,
                            Name = cdragonSkinObject.Name,
                            TilePath = cdragonSkinObject.TilePath,
                            SplashPath = cdragonSkinObject.SplashPath,
                            RarityGemPath = cdragonSkinObject.RarityGemPath,
                            IsLegacy = cdragonSkinObject.IsLegacy,
                            Description = cdragonSkinObject.Description
                            // La lista de Chromas se inicializará si se encuentran datos en Supabase
                        };

                        // 2. Buscar esta skin específica en los datos de Supabase por su ID
                        if (championDataFromSupabase?.Skins != null)
                        {
                            var supabaseSkinData = championDataFromSupabase.Skins.FirstOrDefault(s => s.Id == currentWpfSkin.Id);

                            // 3. Si la skin se encontró en Supabase Y tiene una lista de chromas
                            if (supabaseSkinData?.Chromas != null && supabaseSkinData.Chromas.Any())
                            {
                                // Inicializar la lista de Chromas para currentWpfSkin
                                currentWpfSkin.Chromas = new List<Chroma>();

                                // Iterar sobre TODOS los chromas de esta skin específica de Supabase
                                foreach (var supabaseChroma in supabaseSkinData.Chromas)
                                {
                                    if (supabaseChroma != null) // Asegurarse de que el objeto chroma no sea nulo
                                    {
                                        // Añadir cada chroma a la lista de currentWpfSkin
                                        currentWpfSkin.Chromas.Add(new Chroma
                                        {
                                            Id = supabaseChroma.Id,
                                            Name = supabaseChroma.Name,
                                            ChromaPath = supabaseChroma.ChromaPath,
                                            Colors = supabaseChroma.Colors
                                        });
                                    }
                                    // Si supabaseChroma es null, se ignora (ya no hay un Console.WriteLine aquí para evitar ruido)
                                }
                                // En este punto, currentWpfSkin.Chromas debería tener todos los chromas no nulos de supabaseSkinData.Chromas
                                // Console.WriteLine($"DEBUG: Skin '{currentWpfSkin.Name}' (ID: {currentWpfSkin.Id}) - ADDED {currentWpfSkin.Chromas.Count} chromas.");
                            }
                            // else: Si no hay chromas en Supabase, currentWpfSkin.Chromas permanecerá null (o como se inicialice en el modelo Skin)
                        }
                        // else: Si no hay datos de skins en Supabase, o la skin específica no se encontró,
                        // currentWpfSkin.Chromas permanecerá null.

                        // 4. Añadir la skin procesada (con o sin chromas) a la lista del campeón
                        skinsForThisChampionWpf.Add(currentWpfSkin);
                    }
                }
            }

            // 5. Asignar la lista construida de skins al objeto ChampionDetail principal
            championDetailWpf.Skins = skinsForThisChampionWpf.OrderBy(s => s.Name).ToList();
            return championDetailWpf;
        }


        public static string GetAssetUrl(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return "pack://application:,,,/Assets/placeholder.png";
            }

            if (Uri.TryCreate(relativePath, UriKind.Absolute, out Uri? uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                return relativePath;
            }

            const string apiAssetPrefix = "/lol-game-data/assets";
            if (relativePath.StartsWith(apiAssetPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var pathSegment = relativePath.Substring(apiAssetPrefix.Length).TrimStart('/');
                return $"{DataRoot}/{pathSegment}".ToLowerInvariant();
            }
            return $"{DataRoot}/{relativePath.TrimStart('/')}".ToLowerInvariant();
        }
    }
}