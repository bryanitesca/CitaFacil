using System;
using System.Collections.Generic;
using CitaFacil.Models;

namespace CitaFacil.ViewModels.Admin
{
    public class DatabaseBackupPageViewModel
    {
        public DatabaseBackupConfigViewModel Config { get; set; } = new();

        public IReadOnlyList<DatabaseBackupHistory> History { get; set; } = Array.Empty<DatabaseBackupHistory>();

        public string? LastError { get; set; }

        // === PROPIEDADES DE PAGINACIÓN ===
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}