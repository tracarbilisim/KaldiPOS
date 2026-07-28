using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace KaldiPOS.Utils
{
    public static class ProductImageHelper
    {
        public static string? SelectImage(Window owner)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Ürün Resmi Seç",
                Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.webp;*.bmp|Tüm Dosyalar|*.*",
                Multiselect = false,
                CheckFileExists = true
            };

            return dialog.ShowDialog(owner) == true
                ? dialog.FileName
                : null;
        }

        public static string SaveProductImage(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                throw new FileNotFoundException("Seçilen resim dosyası bulunamadı.", sourcePath);

            string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            string[] allowed = [".jpg", ".jpeg", ".png", ".webp", ".bmp"];

            if (Array.IndexOf(allowed, extension) < 0)
                throw new InvalidOperationException("Yalnızca JPG, JPEG, PNG, WEBP veya BMP dosyası seçebilirsiniz.");

            string relativeDirectory = Path.Combine("Assets", "Products", "Custom");
            string targetDirectory = Path.Combine(AppContext.BaseDirectory, relativeDirectory);
            Directory.CreateDirectory(targetDirectory);

            string fileName = $"product_{Guid.NewGuid():N}{extension}";
            string targetPath = Path.Combine(targetDirectory, fileName);
            File.Copy(sourcePath, targetPath, false);

            return Path.Combine(relativeDirectory, fileName)
                .Replace(Path.DirectorySeparatorChar, '/');
        }

        public static string ToAbsolutePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;

            string normalized = relativePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            return Path.Combine(AppContext.BaseDirectory, normalized);
        }

        public static BitmapImage? LoadPreview(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
