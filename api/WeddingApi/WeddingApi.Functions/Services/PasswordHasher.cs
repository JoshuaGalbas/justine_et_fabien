using System.Security.Cryptography;
using System.Text;

namespace WeddingApi.Functions.Services;

public sealed class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 120_000;

    public PasswordHashResult HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return new PasswordHashResult(
            Convert.ToHexString(hash),
            Convert.ToHexString(salt));
    }

    public bool VerifyPassword(string password, PasswordHashResult stored)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentNullException.ThrowIfNull(stored);

        var expectedHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            Convert.FromHexString(stored.Salt),
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return CryptographicOperations.FixedTimeEquals(expectedHash, Convert.FromHexString(stored.Hash));
    }
}

public sealed record PasswordHashResult(string Hash, string Salt);
