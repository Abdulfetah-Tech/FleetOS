using EthioFleet.Application.Common;

namespace EthioFleet.Tests.UnitTests;

public sealed class PageWindowTests
{
    [Theory]
    [InlineData(-5, 0, 1, 1, 0)]
    [InlineData(3, 250, 3, 100, 200)]
    [InlineData(int.MaxValue, 100, int.MaxValue, 100, int.MaxValue)]
    public void CreateNormalizesBoundsWithoutOverflow(
        int page, int pageSize, int expectedPage, int expectedPageSize, int expectedOffset)
    {
        var window = PageWindow.Create(page, pageSize);

        Assert.Equal(expectedPage, window.Page);
        Assert.Equal(expectedPageSize, window.PageSize);
        Assert.Equal(expectedOffset, window.Offset);
    }
}
