using CommunityToolkit.Mvvm.ComponentModel;
using SkinHunterWPF.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SkinHunterWPF.Models
{
    public partial class ChampionSummary : ObservableObject
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonPropertyName("squarePortraitPath")]
        public string SquarePortraitPath { get; set; } = string.Empty;

        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }

        private BitmapImage? _championImageSourceField;

        [JsonIgnore]
        public BitmapImage? ChampionImageSource
        {
            get
            {
                if (_championImageSourceField == null || _championImageSourceField == _placeholderImage)
                {
                    if (!_isImageLoading && !string.IsNullOrEmpty(OriginalImageUrl) && !OriginalImageUrl.StartsWith("pack:"))
                    {
                        _ = LoadImageAsync();
                    }
                    return _placeholderImage;
                }
                return _championImageSourceField;
            }
            private set // Cambiado a private para control interno
            {
                SetProperty(ref _championImageSourceField, value);
            }
        }

        public void ReleaseImage()
        {
            Debug.WriteLine($"[ChampionSummary] Liberando imagen para {Name}");
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_championImageSourceField != _placeholderImage)
                    {
                        CancelCurrentLoad(); // Cancelar si hay una carga en progreso
                        ChampionImageSource = _placeholderImage; // Volver al placeholder
                        // _championImageSourceField = _placeholderImage; // Ya lo hace el setter de ChampionImageSource
                        Debug.WriteLine($"[ChampionSummary] Imagen para {Name} establecida a placeholder.");
                    }
                });
            }
            else // Si no hay Dispatcher (ej. pruebas unitarias o cierre muy temprano)
            {
                if (_championImageSourceField != _placeholderImage)
                {
                    CancelCurrentLoad();
                    _championImageSourceField = _placeholderImage; // Asignación directa
                    OnPropertyChanged(nameof(ChampionImageSource)); // Notificar cambio manualmente
                    Debug.WriteLine($"[ChampionSummary] Imagen para {Name} establecida a placeholder (sin Dispatcher).");
                }
            }
        }


        [JsonIgnore]
        private volatile bool _isImageLoading = false;
        [JsonIgnore]
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        [JsonIgnore]
        private static readonly Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);
        [JsonIgnore]
        private static readonly BitmapImage? _placeholderImage = LoadPlaceholderImage();

        private CancellationTokenSource? _cancellationTokenSource;

        private async Task LoadImageAsync()
        {
            if (_isImageLoading) return;
            _isImageLoading = true;

            _cancellationTokenSource?.Cancel(); // Cancelar carga anterior
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            string imageUrl = OriginalImageUrl;
            BitmapImage? finalImage = null;

            if (string.IsNullOrEmpty(imageUrl) || imageUrl.StartsWith("pack:"))
            {
                finalImage = _placeholderImage;
            }
            else
            {
                lock (_imageCache) // Sincronizar acceso al caché
                {
                    if (_imageCache.TryGetValue(imageUrl, out var cachedImageFromLock))
                    {
                        finalImage = cachedImageFromLock;
                        Debug.WriteLine($"[ImageLoad] Cache HIT: {imageUrl} for {Name}");
                    }
                }

                if (finalImage == null) // Si no está en caché, cargarla
                {
                    try
                    {
                        Debug.WriteLine($"[ImageLoad] Loading: {imageUrl} for {Name}");
                        using var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
                        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                        response.EnsureSuccessStatusCode();
                        byte[] imageData = await response.Content.ReadAsByteArrayAsync(token);

                        if (token.IsCancellationRequested)
                        {
                            Debug.WriteLine($"[ImageLoad] Cancelled during download: {imageUrl} for {Name}");
                            _isImageLoading = false;
                            // Asegurar que se usa placeholder si se cancela antes de asignar
                            if (System.Windows.Application.Current != null)
                                System.Windows.Application.Current.Dispatcher.Invoke(() => ChampionImageSource = _placeholderImage);
                            else
                                ChampionImageSource = _placeholderImage;
                            return;
                        }

                        var bitmap = new BitmapImage();
                        using (var stream = new MemoryStream(imageData))
                        {
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.DecodePixelWidth = 80;
                            bitmap.StreamSource = stream;
                            bitmap.EndInit();
                        }
                        bitmap.Freeze(); // Importante para uso en otros hilos y rendimiento
                        finalImage = bitmap;

                        lock (_imageCache) // Sincronizar acceso al caché para añadir
                        {
                            _imageCache.TryAdd(imageUrl, finalImage);
                        }
                        Debug.WriteLine($"[ImageLoad] Loaded and Cached: {imageUrl} for {Name}");
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine($"[ImageLoad] Cancelled (OperationCanceledException): {imageUrl} for {Name}");
                        finalImage = _placeholderImage; // Usar placeholder si se cancela
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ImageLoad] Error loading image {imageUrl} for {Name}: {ex.Message}");
                        finalImage = _placeholderImage; // Usar placeholder en caso de error
                    }
                }
            }

            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!token.IsCancellationRequested) // Doble chequeo por si se canceló mientras estaba en cola del Dispatcher
                    {
                        ChampionImageSource = finalImage ?? _placeholderImage;
                    }
                    else
                    {
                        ChampionImageSource = _placeholderImage; // Asegurar placeholder si se canceló
                        Debug.WriteLine($"[ImageLoad] Carga para {Name} fue cancelada antes de asignación en Dispatcher.");
                    }
                });
            }
            else // Para entornos sin Dispatcher (ej. pruebas)
            {
                ChampionImageSource = finalImage ?? _placeholderImage;
            }
            _isImageLoading = false;
        }

        public void CancelCurrentLoad()
        {
            if (_isImageLoading)
            {
                Debug.WriteLine($"[ChampionSummary] Cancelando carga de imagen para {Name}.");
                _cancellationTokenSource?.Cancel();
                _isImageLoading = false; // Marcar como no cargando inmediatamente
            }
        }

        [JsonIgnore]
        public string OriginalImageUrl => CdragonDataService.GetAssetUrl(SquarePortraitPath);

        [JsonIgnore]
        public string Key => Alias?.ToLowerInvariant() ?? string.Empty;

        private static BitmapImage? LoadPlaceholderImage()
        {
            try
            {
                var placeholder = new BitmapImage();
                placeholder.BeginInit();
                placeholder.UriSource = new Uri("pack://application:,,,/Assets/placeholder.png", UriKind.Absolute);
                placeholder.DecodePixelWidth = 80;
                placeholder.CacheOption = BitmapCacheOption.OnLoad;
                placeholder.EndInit();
                placeholder.Freeze();
                return placeholder;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageLoad] Failed to load placeholder image: {ex.Message}");
                return null;
            }
        }
    }
}