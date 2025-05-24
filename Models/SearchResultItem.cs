using CommunityToolkit.Mvvm.ComponentModel;
using SkinHunterWPF.Services;
using System.Windows.Media.Imaging;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
// No se necesita 'using System.Windows;' si System.Windows.Application se califica completamente

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

        private readonly string? _imagePath;

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

        private bool _isImageLoadingOrLoaded = false;

        public async Task LoadImageAsync()
        {
            if (_isImageLoadingOrLoaded || string.IsNullOrEmpty(_imagePath))
            {
                return;
            }

            _isImageLoadingOrLoaded = true;

            BitmapImage? loadedBitmap = null;
            Uri? imageUri = null;

            try
            {
                string fullUrl = CdragonDataService.GetAssetUrl(_imagePath);
                if (Uri.TryCreate(fullUrl, UriKind.Absolute, out imageUri))
                {

                    if (System.Windows.Application.Current != null) // Calificado aquí
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => // Calificado aquí
                        {
                            BitmapImage bitmap = new();
                            bitmap.BeginInit();
                            bitmap.UriSource = imageUri;
                            bitmap.DecodePixelWidth = 64;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            try
                            {
                                bitmap.EndInit();
                                if (bitmap.CanFreeze)
                                {
                                    bitmap.Freeze();
                                }
                                loadedBitmap = bitmap; // Asignar a variable local primero
                            }
                            catch (Exception exEndInit)
                            {
                                Debug.WriteLine($"Error en EndInit para imagen {imageUri}: {exEndInit.Message}");
                                loadedBitmap = null;
                            }
                        });
                    }
                }
                else
                {
                    Debug.WriteLine($"[SearchResultItem] URL de imagen inválida generada para: {_imagePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creando Uri o despachando carga de imagen {_imagePath}: {ex.Message}");
                loadedBitmap = null;
            }

            ImageSource = loadedBitmap; // Asignar fuera del Dispatcher.InvokeAsync si es necesario,
                                        // o si loadedBitmap se establece correctamente dentro.
                                        // Dado que ImageSource es [ObservableProperty], la asignación aquí notificará el cambio.
                                        // Si la asignación se hace dentro del Dispatcher, también es válido.
        }
    }
}