using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CitaFacil.Services.Storage
{
    public class ImageStorageService : IImageStorageService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };
        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png" };
        private const int MaxFileSizeBytes = 4 * 1024 * 1024; // 4 MB
        private const string BaseFolder = "uploads";
        private const string AvatarFolder = "avatars";

        private readonly IWebHostEnvironment _environment;

        public ImageStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<ImageStorageResult> SaveUserAvatarAsync(IFormFile? archivo, string? rutaActual, CancellationToken cancellationToken = default)
        {
            if (archivo is null || archivo.Length == 0)
            {
                return ImageStorageResult.Ok(rutaActual);
            }

            if (archivo.Length > MaxFileSizeBytes)
            {
                return ImageStorageResult.Failure("La fotografía debe pesar máximo 4 MB.");
            }

            var extension = Path.GetExtension(archivo.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                return ImageStorageResult.Failure("Formato de archivo no permitido. Usa imágenes jpg o png.");
            }

            var contentType = archivo.ContentType ?? string.Empty;
            if (!AllowedMimeTypes.Contains(contentType))
            {
                return ImageStorageResult.Failure("El tipo de contenido no es válido.");
            }

            var uploadsRoot = Path.Combine(_environment.WebRootPath, BaseFolder, AvatarFolder);
            Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await archivo.CopyToAsync(stream, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(rutaActual))
            {
                var sanitized = rutaActual.TrimStart('~').TrimStart('/', '\\');
                var existing = Path.Combine(_environment.WebRootPath, sanitized);
                try
                {
                    if (File.Exists(existing))
                    {
                        File.Delete(existing);
                    }
                }
                catch
                {
                    // Ignoramos errores al eliminar fotos anteriores.
                }
            }

            var relativePath = $"/{BaseFolder}/{AvatarFolder}/{fileName}".Replace("\\", "/");
            return ImageStorageResult.Ok(relativePath);
        }

        public Task DeleteAsync(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return Task.CompletedTask;
            }

            var sanitized = ruta.TrimStart('~').TrimStart('/', '\\');
            var fullPath = Path.Combine(_environment.WebRootPath, sanitized);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }
    }
}

