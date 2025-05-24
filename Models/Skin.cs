using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace SkinHunterWPF.Models
{
    public class Skin
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("tilePath")]
        public string TilePath { get; set; } = string.Empty;

        [JsonPropertyName("splashPath")]
        public string SplashPath { get; set; } = string.Empty;

        [JsonPropertyName("rarityGemPath")]
        public string? RarityGemPath { get; set; }

        [JsonPropertyName("isLegacy")]
        public bool IsLegacy { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("chromas")]
        public List<Chroma>? Chromas { get; set; }

        [JsonIgnore]
        public string TileImageUrl => Services.CdragonDataService.GetAssetUrl(TilePath);
        [JsonIgnore]
        public string SplashImageUrl => Services.CdragonDataService.GetAssetUrl(SplashPath);
        [JsonIgnore]
        public string? RarityImageUrl => RarityGemPath != null ? Services.CdragonDataService.GetAssetUrl(RarityGemPath) : null;
        [JsonIgnore]
        public string RarityName => GetRarityNameFromPath(RarityGemPath);

        [JsonIgnore]
        public int ChampionId => Id / 1000; // Propiedad calculada, solo get

        [JsonIgnore]
        public bool HasChromas => Chromas?.Any() ?? false;

        // Constructor sin parámetros necesario para la deserialización si no todos los campos son públicos con set.
        // Aunque con System.Text.Json y propiedades públicas con set, no siempre es estrictamente necesario
        // si el JSON mapea directamente.
        public Skin() { }


        private static string GetRarityNameFromPath(string? path)
        {
            if (string.IsNullOrEmpty(path)) return "Standard";
            if (path.Contains("ultimate")) return "Ultimate";
            if (path.Contains("mythic")) return "Mythic";
            if (path.Contains("legendary")) return "Legendary";
            if (path.Contains("epic")) return "Epic";
            if (path.Contains("transcendent")) return "Transcendent";
            if (path.Contains("exalted")) return "Exalted";
            return "Unknown";
        }
    }
}