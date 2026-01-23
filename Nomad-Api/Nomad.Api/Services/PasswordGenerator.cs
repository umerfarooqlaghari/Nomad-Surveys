using System.Security.Cryptography;
using System.Text;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public class PasswordGenerator : IPasswordGenerator
{
    private readonly string _secret;
    private const string CharSet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789"; // No ambiguous chars

    public PasswordGenerator(IConfiguration config)
    {
        _secret = config["PasswordSecret"] ?? "Nomad-Default-Secret-2026-Secure-Salt";
    }

    public string Generate(string email)
    {
        if (string.IsNullOrEmpty(email)) return "Nomad@2026!";

        var emailLower = email.ToLower().Trim();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(emailLower));

        // Create a 10-character password from the hash
        var password = new StringBuilder();
        for (int i = 0; i < 10; i++)
        {
            // Use 2 bytes per character for better distribution
            var index = BitConverter.ToUInt16(hash, i * 2) % CharSet.Length;
            password.Append(CharSet[index]);

            // Add some "complexity" requirements
            if (i == 3) password.Append('!');
            if (i == 7) password.Append('@');
        }

        return password.ToString();
    }

    public bool IsGeneratedPassword(string email, string hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword)) return false;
        
        var generated = Generate(email);
        return BCrypt.Net.BCrypt.Verify(generated, hashedPassword);
    }
}
