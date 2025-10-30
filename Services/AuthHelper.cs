using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MiniPM.Models;

namespace MiniPM.Services
{
    public class AuthHelper
    {
        // PBKDF2 helpers
        public string HashPassword(string password)
        {
            // Create salt + hash
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            using var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var hash = derive.GetBytes(32);

            var combined = new byte[1 + salt.Length + hash.Length];
            combined[0] = 0; // version
            Array.Copy(salt, 0, combined, 1, salt.Length);
            Array.Copy(hash, 0, combined, 1 + salt.Length, hash.Length);
            return Convert.ToBase64String(combined);
        }

        public bool VerifyPassword(string password, string stored)
        {
            try
            {
                var data = Convert.FromBase64String(stored);
                var salt = data.Skip(1).Take(16).ToArray();
                var hash = data.Skip(1 + 16).Take(32).ToArray();
                using var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
                var testHash = derive.GetBytes(32);
                return testHash.SequenceEqual(hash);
            }
            catch
            {
                return false;
            }
        }

        public string GenerateJwtToken(User user, string jwtKey, string issuer)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };
            var token = new JwtSecurityToken(issuer: issuer, claims: claims, expires: DateTime.UtcNow.AddDays(7), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
