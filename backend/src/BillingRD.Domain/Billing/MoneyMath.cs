namespace BillingRD.Domain.Billing;

public static class MoneyMath
{
    public static decimal RoundCurrency(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
