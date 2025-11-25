namespace CitaFacil.Services.Storage
{
    public record ImageStorageResult(bool Success, string? FilePath, string? ErrorMessage)
    {
        public static ImageStorageResult Ok(string? filePath) => new(true, filePath, null);

        public static ImageStorageResult Failure(string message) => new(false, null, message);
    }
}

