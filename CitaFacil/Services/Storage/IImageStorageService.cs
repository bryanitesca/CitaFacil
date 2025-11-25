using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CitaFacil.Services.Storage
{
    public interface IImageStorageService
    {
        Task<ImageStorageResult> SaveUserAvatarAsync(IFormFile? archivo, string? rutaActual, CancellationToken cancellationToken = default);
        Task DeleteAsync(string? ruta);
    }
}

