using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SkinHunterWPF.Models;
using System.Linq;
using System.IO;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace SkinHunterWPF.Services
{
    public static class CdragonDataService
    {
        private static readonly HttpClient _httpClient = CreateHttpClient();
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private const string CdragonBaseUrl = "https://raw.communitydragon.org/latest";
        private const string DataRoot = $"{CdragonBaseUrl}/plugins/rcp-be-lol-game-data/global/default";
        private static string? _cdragonVersion;

        private const string SupabaseUrl = "https://odlqwkgewzxxmbsqutja.supabase.co";
        private const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im9kbHF3a2dld3p4eG1ic3F1dGphIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MzQyMTM2NzcsImV4cCI6MjA0OTc4OTY3N30.qka6a71bavDeUQgy_BKoVavaClRQa_gT36Au7oO9AF0";
        private const string SupabaseStorageBasePath = "/storage/v1/object/public";

        private static HttpClient CreateHttpClient() => new();

        private static async Task<string> GetCdragonVersionAsync()
        {
            if (_cdragonVersion == null)
            {
                try
                {
                    var metaUrl = $"{CdragonBaseUrl}/content-metadata.json";
                    using var request = new HttpRequestMessage(HttpMethod.Get, metaUrl);
                    using var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    using var json = await response.Content.ReadAsStreamAsync();
                    var metadata = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(json, _jsonOptions);
                    if (metadata != null && metadata.TryGetValue("version", out var versionElement))
                    {
                        _cdragonVersion = versionElement.GetString() ?? "latest";
                    }
                    else { _cdragonVersion = "latest"; }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CdragonDataService] Error fetching CDRAGON version: {ex.Message}");
                    _cdragonVersion = "latest";
                }
            }
            return _cdragonVersion;
        }

        private static async Task<T?> FetchDataAsync<T>(string fullUrl, bool isSupabase = false) where T : class
        {
            var httpClientToUse = _httpClient;
            try
            {
                Debug.WriteLine($"[CdragonDataService] Fetching: {fullUrl}");
                using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                if (isSupabase)
                {
                    request.Headers.TryAddWithoutValidation("apikey", SupabaseAnonKey);
                }

                using var response = await httpClientToUse.SendAsync(request);
                response.EnsureSuccessStatusCode();

                byte[] contentBytes = await response.Content.ReadAsByteArrayAsync();

                if (contentBytes == null || contentBytes.Length == 0)
                {
                    Debug.WriteLine($"[CdragonDataService] Warning: Content bytes were null or empty for {Path.GetFileNameWithoutExtension(fullUrl)} despite 200 OK status.");
                    return null;
                }

                using var memoryStream = new MemoryStream(contentBytes);
                var result = await JsonSerializer.DeserializeAsync<T>(memoryStream, _jsonOptions);
                if (result != null)
                {
                    Debug.WriteLine($"[CdragonDataService] Successfully deserialized {Path.GetFileNameWithoutExtension(fullUrl)} into {typeof(T).Name}.");
                }
                else
                {
                    Debug.WriteLine($"[CdragonDataService] Deserialization of {Path.GetFileNameWithoutExtension(fullUrl)} into {typeof(T).Name} resulted in null.");
                }
                return result;
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine($"[CdragonDataService] HTTP Error fetching {Path.GetFileNameWithoutExtension(fullUrl)}: {httpEx.Message} (Status: {httpEx.StatusCode})");
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine($"[CdragonDataService] JSON Error parsing {Path.GetFileNameWithoutExtension(fullUrl)}: {jsonEx.Message}. LineNumber: {jsonEx.LineNumber}, BytePositionInLine: {jsonEx.BytePositionInLine}, Path: {jsonEx.Path}");
                try
                {
                    using var requestRetry = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                    if (isSupabase) { requestRetry.Headers.TryAddWithoutValidation("apikey", SupabaseAnonKey); }
                    using var responseRetry = await httpClientToUse.SendAsync(requestRetry);
                    if (responseRetry.IsSuccessStatusCode)
                    {
                        string text = await responseRetry.Content.ReadAsStringAsync();
                        Debug.WriteLine($"[CdragonDataService] --- Raw JSON Content on Error for {Path.GetFileNameWithoutExtension(fullUrl)} ---\n{text}\n---------------------------------");
                    }
                }
                catch (Exception readEx)
                {
                    Debug.WriteLine($"[CdragonDataService] Could not read response as string on JSON error: {readEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CdragonDataService] Generic Error fetching {Path.GetFileNameWithoutExtension(fullUrl)}: {ex.Message}");
            }
            return null;
        }

        private static async Task<SupabaseChampionData?> FetchChampionDataFromSupabaseAsync(int championId)
        {
            string bucket = "api_json";
            string filePath = $"{championId}.json";
            string supabaseFileUrl = $"{SupabaseUrl}{SupabaseStorageBasePath}/{bucket}/{filePath}";
            return await FetchDataAsync<SupabaseChampionData>(supabaseFileUrl, isSupabase: true);
        }


        public static async Task<List<ChampionSummary>?> GetChampionSummariesAsync()
        {
            _ = await GetCdragonVersionAsync();
            var url = $"{DataRoot}/v1/champion-summary.json";
            var summaries = await FetchDataAsync<List<ChampionSummary>>(url);
            return summaries?.Where(c => c.Id != -1).OrderBy(c => c.Name).ToList();
        }

        public static async Task<Dictionary<string, Skin>?> GetAllSkinsAsync()
        {
            _ = await GetCdragonVersionAsync();
            var url = $"{DataRoot}/v1/skins.json";
            Debug.WriteLine($"[CdragonDataService] Attempting to fetch and parse all skins from: {url}");
            var allSkins = await FetchDataAsync<Dictionary<string, Skin>>(url);
            if (allSkins != null)
            {
                Debug.WriteLine($"[CdragonDataService] Successfully fetched and parsed {allSkins.Count} total skins from skins.json.");
            }
            else
            {
                Debug.WriteLine($"[CdragonDataService] Failed to fetch or parse skins.json. allSkins is null.");
            }
            return allSkins;
        }

        public static async Task<ChampionDetail?> GetChampionDetailsAsync(int championId)
        {
            Debug.WriteLine($"[CdragonDataService] GetChampionDetailsAsync called for champion ID: {championId}");
            _ = await GetCdragonVersionAsync();
            var detailsUrl = $"{DataRoot}/v1/champions/{championId}.json";
            var championDetailWpf = await FetchDataAsync<ChampionDetail>(detailsUrl);

            if (championDetailWpf == null)
            {
                Debug.WriteLine($"[CdragonDataService] Failed to load champion details for {championId} from Cdragon champions/{championId}.json. Returning null.");
                return null;
            }

            var allSkinsFromCdragon = await GetAllSkinsAsync();
            var championDataFromSupabase = await FetchChampionDataFromSupabaseAsync(championId);

            if (championDataFromSupabase == null)
            {
                Debug.WriteLine($"[CdragonDataService] WARNING: Failed to load champion data for {championId} from Supabase. Chroma data might be sourced only from Cdragon/skins.json.");
            }
            else
            {
                Debug.WriteLine($"[CdragonDataService] Successfully loaded champion data for {championId} from Supabase. Contains {championDataFromSupabase.Skins?.Count ?? 0} skin entries.");
            }


            var skinsForThisChampionWpf = new List<Skin>();

            if (allSkinsFromCdragon != null)
            {
                Debug.WriteLine($"[CdragonDataService] Processing skins for champion ID {championId} from {allSkinsFromCdragon.Count} total Cdragon skins.");
                foreach (var cdragonSkinEntry in allSkinsFromCdragon.Where(kvp => kvp.Value.ChampionId == championId))
                {
                    Skin cdragonSkinObject = cdragonSkinEntry.Value;
                    string skinIdentifier = $"Skin ID: {cdragonSkinObject.Id} ('{cdragonSkinObject.Name}') for Champion ID: {championId}";
                    Debug.WriteLine($"[CdragonDataService] Processing {skinIdentifier}");

                    var currentWpfSkin = new Skin
                    {
                        Id = cdragonSkinObject.Id,
                        Name = cdragonSkinObject.Name,
                        TilePath = cdragonSkinObject.TilePath,
                        SplashPath = cdragonSkinObject.SplashPath,
                        RarityGemPath = cdragonSkinObject.RarityGemPath,
                        IsLegacy = cdragonSkinObject.IsLegacy,
                        Description = cdragonSkinObject.Description,
                        Chromas = new List<Chroma>()
                    };

                    var championJsonSkinInfo = championDetailWpf.Skins?.FirstOrDefault(s => s.Id == currentWpfSkin.Id);
                    if (!string.IsNullOrWhiteSpace(championJsonSkinInfo?.Description))
                    {
                        currentWpfSkin.Description = championJsonSkinInfo.Description;
                    }

                    SupabaseSkinData? supabaseSkinData = null;
                    if (championDataFromSupabase?.Skins != null)
                    {
                        supabaseSkinData = championDataFromSupabase.Skins.FirstOrDefault(s => s.Id == currentWpfSkin.Id);
                        if (supabaseSkinData == null)
                        {
                            Debug.WriteLine($"[CdragonDataService] {skinIdentifier}: No matching skin data found in Supabase champion data.");
                        }
                    }
                    else if (championDataFromSupabase != null && championDataFromSupabase.Skins == null)
                    {
                        Debug.WriteLine($"[CdragonDataService] {skinIdentifier}: Supabase champion data loaded, but its 'Skins' list is null.");
                    }


                    bool chromasPopulatedFromSupabase = false;
                    if (supabaseSkinData?.Chromas != null && supabaseSkinData.Chromas.Any())
                    {
                        Debug.WriteLine($"[CdragonDataService] {skinIdentifier}: Populating chromas from Supabase ({supabaseSkinData.Chromas.Count} found).");
                        foreach (var supabaseChroma in supabaseSkinData.Chromas)
                        {
                            if (supabaseChroma != null)
                            {
                                currentWpfSkin.Chromas.Add(new Chroma
                                {
                                    Id = supabaseChroma.Id,
                                    Name = supabaseChroma.Name,
                                    ChromaPath = supabaseChroma.ChromaPath,
                                    Colors = supabaseChroma.Colors != null ? new List<string>(supabaseChroma.Colors) : null
                                });
                                Debug.WriteLine($"[CdragonDataService] {skinIdentifier}: Added Supabase Chroma ID: {supabaseChroma.Id}, Name: '{supabaseChroma.Name}'");
                            }
                        }
                        chromasPopulatedFromSupabase = true;
                    }
                    else if (supabaseSkinData?.Chromas != null && !supabaseSkinData.Chromas.Any())
                    {
                        Debug.WriteLine($"[CdragonDataService] {skinIdentifier}: Supabase skin data found, but its 'Chromas' list is empty.");
                    }
                    else if (supabaseSkinData == null && championDataFromSupabase != null)
                    {
                    }


                    if (!chromasPopulatedFromSupabase && cdragonSkinObject.Chromas != null && cdragonSkinObject.Chromas.Any())
                    {
                        Debug.WriteLine($"[CdragonDataService] {skinIdentifier}: Supabase chromas not used or not found. Falling back to Cdragon/skins.json ({cdragonSkinObject.Chromas.Count} found).");
                        foreach (var cdragonChroma in cdragonSkinObject.Chromas)
                        {
                            if (cdragonChroma != null && !currentWpfSkin.Chromas.Any(ch => ch.Id == cdragonChroma.Id))
                            {
                                currentWpfSkin.Chromas.Add(new Chroma
                                {
                                    Id = cdragonChroma.Id,
                                    Name = cdragonChroma.Name,
                                    ChromaPath = cdragonChroma.ChromaPath,
                                    Colors = cdragonChroma.Colors != null ? new List<string>(cdragonChroma.Colors) : null
                                });
                                Debug.WriteLine($"[CdragonDataService] {skinIdentifier}: Added Cdragon/skins.json Chroma ID: {cdragonChroma.Id}, Name: '{cdragonChroma.Name}'");
                            }
                        }
                    }
                    else if (!chromasPopulatedFromSupabase)
                    {
                        Debug.WriteLine($"[CdragonDataService] {skinIdentifier}: No chromas found from Supabase, and Cdragon/skins.json also has no chromas (or 'Chromas' list is null).");
                    }

                    Debug.WriteLine($"[CdragonDataService] {skinIdentifier}: Final chromas count for skin: {currentWpfSkin.Chromas.Count}");
                    currentWpfSkin.Chromas = currentWpfSkin.Chromas.OrderBy(c => c.Id).ToList();
                    skinsForThisChampionWpf.Add(currentWpfSkin);
                }
            }
            else
            {
                Debug.WriteLine($"[CdragonDataService] WARNING: allSkinsFromCdragon (cdn/v1/skins.json) was null. Cannot process any skins for champion {championId}.");
            }

            championDetailWpf.Skins = skinsForThisChampionWpf.OrderBy(s => s.Name).ToList();
            Debug.WriteLine($"[CdragonDataService] GetChampionDetailsAsync for champion ID {championId} finished. Total skins populated: {championDetailWpf.Skins.Count}");
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
                var pathSegment = relativePath[apiAssetPrefix.Length..].TrimStart('/');
                return $"{DataRoot}/{pathSegment}".ToLowerInvariant();
            }
            return $"{DataRoot}/{relativePath.TrimStart('/')}".ToLowerInvariant();
        }
    }
}