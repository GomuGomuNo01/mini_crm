using MiniCrm.Models;

namespace MiniCrm.Tests.Models;

public class ContractTests
{
    [Fact]
    public void IsExpiringSoon_True_WhenActiveAndEndsWithin30Days()
    {
        var contract = new Contract
        {
            Status = ContractStatus.Active,
            EndDate = DateTime.UtcNow.AddDays(15)
        };

        Assert.True(contract.IsExpiringSoon);
    }

    [Fact]
    public void IsExpiringSoon_True_AtExactly30DaysBoundary()
    {
        var contract = new Contract
        {
            Status = ContractStatus.Active,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        Assert.True(contract.IsExpiringSoon);
    }

    [Fact]
    public void IsExpiringSoon_False_WhenEndDateBeyond30Days()
    {
        var contract = new Contract
        {
            Status = ContractStatus.Active,
            EndDate = DateTime.UtcNow.AddDays(31)
        };

        Assert.False(contract.IsExpiringSoon);
    }

    [Fact]
    public void IsExpiringSoon_False_WhenAlreadyExpired()
    {
        var contract = new Contract
        {
            Status = ContractStatus.Active,
            EndDate = DateTime.UtcNow.AddDays(-1)
        };

        Assert.False(contract.IsExpiringSoon);
    }

    [Theory]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Draft)]
    public void IsExpiringSoon_False_WhenStatusIsNotActive(ContractStatus status)
    {
        var contract = new Contract
        {
            Status = status,
            EndDate = DateTime.UtcNow.AddDays(10)
        };

        Assert.False(contract.IsExpiringSoon);
    }
}
