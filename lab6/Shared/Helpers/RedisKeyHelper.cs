namespace Shared.Helpers;

public static class RedisKeyHelper
{
    public static string CreateShardKey(string id)
    {
        return  $"SHARD-MAP-{id}";
    }
    public static string CreateRankKey(string id)
    {
        return  $"RANK-{id}";
    }
    public static string CreateSimilarityKey(string id)
    {
        return  $"SIMILARITY-{id}";
    }
    public static string CreateTextKey(string id)
    {
        return  $"TEXT-{id}";
    }
}