using CommunityToolkit.Mvvm.ComponentModel;
using SkinHunterWPF.Services;
using System.Windows.Media.Imaging;
using System;
using System.Threading.Tasks;

namespace SkinHunterWPF.Models
{
    public enum SearchResultType
    {
        Champion,
        Skin
    }

    public partial class SearchResultItem : ObservableObject
    {
        public int Id { get; }
        public string Name { get; }
        public SearchResultType Type { get; }
        public string DisplayType { get; }

        [ObservableProperty]
        private BitmapImage? _imageSource;

        private readonly string? _imagePath; // Marcado como readonly

        public int ChampionId { get; }
        public Skin? OriginalSkinObject { get; }
        public ChampionSummary? OriginalChampionObject { get; }


        public SearchResultItem(ChampionSummary champion)
        {
            Id = champion.Id;
            Name = champion.Name;
            Type = SearchResultType.Champion;
            DisplayType = "Champion";
            _imagePath = champion.SquarePortraitPath;
            ChampionId = champion.Id;
            OriginalChampionObject = champion;
        }

        public SearchResultItem(Skin skin, ChampionSummary? parentChampion)
        {
            Id = skin.Id;
            Name = skin.Name;
            Type = SearchResultType.Skin;
            DisplayType = "Champion Skin";
            _imagePath = skin.TilePath;
            ChampionId = skin.ChampionId;
            OriginalSkinObject = skin;
            OriginalChampionObject = parentChampion;
        }

        public Task LoadImageAsync()
        {
            if (ImageSource == null && !string.IsNullOrEmpty(_imagePath)) // Usar propiedad generada ImageSource
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(CdragonDataService.GetAssetUrl(_imagePath), UriKind.Absolute);
                    bitmap.DecodePixelWidth = 64;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ImageSource = bitmap; // Usar propiedad generada
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading search result image {_imagePath}: {ex.Message}");
                }
            }
            return Task.CompletedTask; // Para satisfacer async y quitar advertencia CS1998
        }
    }
}