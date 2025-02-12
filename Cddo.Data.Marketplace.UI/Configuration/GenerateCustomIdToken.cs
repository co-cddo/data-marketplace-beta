using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Cddo.Data.Marketplace.UI.Configuration
{
    public class AGMTokenService
    {
        private readonly IConfiguration _configuration;

        public AGMTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateCustomIdToken(string userEmail, string userName)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // Retrieve the secret key from the configuration
            var secretKey = _configuration["AGMJwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT Secret Key is not configured.");
            }

            var key = Encoding.ASCII.GetBytes(secretKey);

            // Set the token expiration to midnight of the current day
            var expirationTime = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1);  // Midnight

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userEmail), // Subject claim (user's email)
                new Claim(JwtRegisteredClaimNames.Email, userEmail), // Email claim
                new Claim(JwtRegisteredClaimNames.Name, userName), // Name claim
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
                new Claim(JwtRegisteredClaimNames.Iss, _configuration["BaseUrl"]), // Issuer
                new Claim(JwtRegisteredClaimNames.Aud, $"{_configuration["BaseUrl"]}api"), // Audience
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64) // Correct Issued at claim (Unix timestamp)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expirationTime,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token); // Return the signed JWT token
        }
    }
}
