using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Administration.Helpers
{
    public static class AuthHelper
    {
        private const string UserSessionKey = "LoggedUser";

        public static void SetLoggedUser(this ISession session, Models.Utilisateur user)
        {
            var userData = new
            {
                user.Id,
                user.NomUtilisateur,
                user.Email
                // Role et IsActive sont supprimés (déterminés dynamiquement)
            };
            var userJson = JsonSerializer.Serialize(userData);
            session.SetString(UserSessionKey, userJson);
        }

        public static (int Id, string Username, string Email)? GetLoggedUser(this ISession session)
        {
            var userJson = session.GetString(UserSessionKey);
            if (string.IsNullOrEmpty(userJson)) return null;

            using var doc = JsonDocument.Parse(userJson);
            var root = doc.RootElement;
            return (
                Id: root.GetProperty("Id").GetInt32(),
                Username: root.GetProperty("NomUtilisateur").GetString()!,
                Email: root.GetProperty("Email").GetString()!
            );
        }

        public static void RemoveLoggedUser(this ISession session)
        {
            session.Remove(UserSessionKey);
        }
    }
}