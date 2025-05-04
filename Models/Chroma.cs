using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Windows.Media;

namespace SkinHunterWPF.Models
{
    public class Chroma
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("chromaPath")]
        public string ChromaPath { get; set; } = string.Empty;

        [JsonPropertyName("colors")]
        public List<string>? Colors { get; set; }

        public string ImageUrl => Services.CdragonDataService.GetAssetUrl(ChromaPath);

        public Brush? ColorBrush
        {
            get
            {
                if (Colors == null || Colors.Count == 0) return Brushes.Gray;
                if (Colors.Count == 1)
                {
                    try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(Colors[0])); }
                    catch { return Brushes.Gray; }
                }
                try
                {
                    var gradient = new LinearGradientBrush
                    {
                        StartPoint = new System.Windows.Point(0, 0.5), // Left Middle
                        EndPoint = new System.Windows.Point(1, 0.5)   // Right Middle
                    };
                    // Handle cases with more than 2 colors if needed, adjusting stops
                    gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(Colors[0]), 0.0));
                    gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(Colors[1]), 1.0));
                    return gradient;
                }
                catch { return Brushes.Gray; }
            }
        }
    }
}