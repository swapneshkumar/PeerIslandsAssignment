namespace OrderProcessing.Application.Options;

public sealed class OrderProcessingOptions
{
    public const string SectionName = "OrderProcessing";

    public int PendingOrderThresholdMinutes { get; init; } = 15;
    public int PendingOrderBatchSize { get; init; } = 100;
    public int OrderCacheMinutes { get; init; } = 5;
}
