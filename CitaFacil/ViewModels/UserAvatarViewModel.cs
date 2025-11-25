namespace CitaFacil.ViewModels
{
    public class UserAvatarViewModel
    {
        public string Name { get; set; } = "";
        public string? PhotoUrl { get; set; }
        public string Size { get; set; } = "md"; // sm, md, lg, xl
        public string CssClass { get; set; } = "";

        public string Initials => GenerateInitials(Name);

        private string GenerateInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "UN";

            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 2)
            {
                return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper();
            }
            
            if (parts.Length == 1 && parts[0].Length >= 2)
            {
                return parts[0].Substring(0, 2).ToUpper();
            }
            
            return parts.Length > 0 ? parts[0][0].ToString().ToUpper() + "N" : "UN";
        }
    }
}