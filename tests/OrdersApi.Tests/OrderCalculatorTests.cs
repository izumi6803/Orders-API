using OrdersApi.Api.Services;

namespace OrdersApi.Tests;

public class OrderCalculatorTests
{
    [Fact]
    public void Empty_order_totals_zero()
    {
        Assert.Equal(0m, OrderCalculator.Total([]));
    }

    [Fact]
    public void Sums_price_times_quantity()
    {
        var total = OrderCalculator.Total([(250_000m, 2), (450_000m, 1)]);
        Assert.Equal(950_000m, total);
    }

    [Fact]
    public void Rounds_to_two_decimals_away_from_zero()
    {
        var total = OrderCalculator.Total([(10.005m, 1)]);
        Assert.Equal(10.01m, total);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(100, 3, 300)]
    [InlineData(0.1, 3, 0.3)]
    public void Handles_zero_and_fractional_lines(decimal price, int quantity, decimal expected)
    {
        Assert.Equal(expected, OrderCalculator.Total([(price, quantity)]));
    }
}
