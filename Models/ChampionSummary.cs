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
// No se necesita 'using System.Windows;' si se califica Application.Current

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
            private set
            {
                SetProperty(ref _championImageSourceField, value);
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

            _cancellationTokenSource?.Cancel();
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
                lock (_imageCache)
                {
                    if (_imageCache.TryGetValue(imageUrl, out var cachedImageFromLock))
                    {
                        finalImage = cachedImageFromLock;
                    }
                }

                if (finalImage == null)
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
                            _isImageLoading = false;
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
                        bitmap.Freeze();
                        finalImage = bitmap;

                        lock (_imageCache)
                        {
                            _imageCache.TryAdd(imageUrl, finalImage);
                        }
                        Debug.WriteLine($"[ImageLoad] Loaded and Cached: {imageUrl} for {Name}");
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine($"[ImageLoad] Cancelled: {imageUrl} for {Name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ImageLoad] Error loading image {imageUrl} for {Name}: {ex.Message}");
                        finalImage = _placeholderImage;
                    }
                }
            }

            if (!token.IsCancellationRequested)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => // Calificado aquí
                {
                    if (!token.IsCancellationRequested)
                    {
                        ChampionImageSource = finalImage;
                    }
                });
            }
            _isImageLoading = false;
        }

        public void CancelCurrentLoad()
        {
            _cancellationTokenSource?.Cancel();
            _isImageLoading = false;
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