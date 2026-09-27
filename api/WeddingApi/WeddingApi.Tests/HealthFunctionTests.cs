using Xunit;

namespace WeddingApi.Tests;

public class HealthFunctionTests
{
    [Fact]
    public void SmokeTest_AlwaysPasses()
    {
        Assert.True(true);
    }

    [Fact]
    public void AdminRoutes_ShouldNotUseReservedAdminPrefix()
    {
        Assert.StartsWith("organizer/", WeddingApi.Shared.Constants.ApiRoutes.AdminDashboard);
        Assert.StartsWith("organizer/", WeddingApi.Shared.Constants.ApiRoutes.AdminHouseholds);
        Assert.StartsWith("organizer/", WeddingApi.Shared.Constants.ApiRoutes.AuditLog);
        Assert.StartsWith("organizer/", WeddingApi.Shared.Constants.ApiRoutes.GalleryUpload);
    }
}
