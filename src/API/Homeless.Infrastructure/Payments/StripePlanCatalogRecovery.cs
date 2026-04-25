using Stripe;

namespace Homeless.Infrastructure.Payments;

internal static class StripePlanCatalogRecovery
{
    private const string PlanIdMetadataKey = "plan_id";
    private const string MonthlyInterval = "month";

    public static Product? FindManagedPlanProduct(Guid planId, IEnumerable<Product> products)
    {
        var planIdValue = planId.ToString();

        return products.FirstOrDefault(product =>
            product.Active &&
            product.Metadata is not null &&
            product.Metadata.TryGetValue(PlanIdMetadataKey, out var metadataPlanId) &&
            string.Equals(metadataPlanId, planIdValue, StringComparison.OrdinalIgnoreCase));
    }

    public static Price? FindMatchingMonthlyPrice(IEnumerable<Price> prices, string currency, long unitAmount) =>
        prices.FirstOrDefault(price =>
            price.Active &&
            price.UnitAmount == unitAmount &&
            string.Equals(price.Currency, currency, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(price.Recurring?.Interval, MonthlyInterval, StringComparison.OrdinalIgnoreCase) &&
            (price.Recurring?.IntervalCount ?? 1) == 1);
}
