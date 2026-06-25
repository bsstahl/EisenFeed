using EisenFeed.Core.Contracts;

namespace EisenFeed.Ingestion.Transform.Rules;

public sealed class FeedTransformStrategySelector
{
    private readonly ITransformFeedItems _defaultTransformer = new FeedItemTransformer();

    public ITransformFeedItems Select(string profile)
    {
        if (string.Equals(profile, "default", StringComparison.OrdinalIgnoreCase))
        {
            return _defaultTransformer;
        }

        throw new NotSupportedException($"Unsupported transform profile '{profile}'.");
    }
}