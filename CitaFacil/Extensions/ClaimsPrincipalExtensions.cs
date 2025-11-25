using System.Security.Claims;

namespace CitaFacil.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static long? GetUserId(this ClaimsPrincipal user)
        {
            var identifier = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(identifier, out var id) ? id : (long?)null;
        }

        public static string? GetRole(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Role);
        }

        public static string GetUserName(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Generates a placeholder image URL using initials from first and last name
        /// </summary>
        /// <param name="firstName">First name</param>
        /// <param name="lastName">Last name</param>
        /// <param name="size">Image size (default 200px)</param>
        /// <param name="backgroundColor">Background color hex without # (default 0d6efd)</param>
        /// <param name="textColor">Text color hex without # (default fff)</param>
        /// <returns>URL for placeholder image</returns>
        public static string GeneratePlaceholderImage(string firstName, string lastName, int size = 200, string backgroundColor = "0d6efd", string textColor = "fff")
        {
            var initials = "";
            if (!string.IsNullOrEmpty(firstName))
                initials += firstName[0];
            if (!string.IsNullOrEmpty(lastName))
                initials += lastName[0];
            
            if (string.IsNullOrEmpty(initials))
                initials = "??";
                
            return $"https://ui-avatars.com/api/?name={initials}&background={backgroundColor}&color={textColor}&size={size}&bold=true";
        }
    }
}

