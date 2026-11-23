namespace OrdersApi.Api.Services;

public static class OrderCalculator
{
    public static decimal Total(IEnumerable<(decimal UnitPrice, int Quantity)> lines)
    {
        var total = lines.Sum(line => line.UnitPrice * line.Quantity);
        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }
}
