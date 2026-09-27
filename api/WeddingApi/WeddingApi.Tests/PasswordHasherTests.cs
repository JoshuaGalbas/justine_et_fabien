using WeddingApi.Functions.Services;
using Xunit;

namespace WeddingApi.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_AndVerifyPassword_ShouldMatch()
    {
        var hasher = new PasswordHasher();

        var result = hasher.HashPassword("SecurePass!123");

        Assert.NotNull(result);
        Assert.NotEmpty(result.Salt);
        Assert.NotEmpty(result.Hash);
        Assert.True(hasher.VerifyPassword("SecurePass!123", result));
        Assert.False(hasher.VerifyPassword("WrongPassword!123", result));
    }
}
