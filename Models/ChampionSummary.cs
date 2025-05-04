using System.Text.Json.Serialization;

namespace SkinHunterWPF.Models
{
    public class ChampionSummary
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonPropertyName("squarePortraitPath")]
        public string SquarePortraitPath { get; set; } = string.Empty;

        public string ImageUrl => Services.CdragonDataService.GetAssetUrl(SquarePortraitPath);
        public string Key => Alias?.ToLowerInvariant() ?? string.Empty;
    }
}