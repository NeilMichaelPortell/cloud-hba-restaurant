using StackExchange.Redis;

namespace restaurant.Services;

public class CacheService
{
    private readonly ILogger<CacheService> _logger;
    private IDatabase? _cache;
    private ConnectionMultiplexer? _redis;

    public CacheService(ILogger<CacheService> logger, IConfiguration config)
    {
        _logger = logger;
        string connectionString = config["Redis:ConnectionString"]!;

        try
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 5000;
            options.Ssl = true;
            options.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;

            // Log connection attempt
            _logger.LogInformation("Connecting to Redis: {Host}",
                options.EndPoints.FirstOrDefault()?.ToString());

            _redis = ConnectionMultiplexer.Connect(options);
            _cache = _redis.GetDatabase();

            
            _redis.GetDatabase().Ping();

            _logger.LogInformation("Redis connected and ping successful.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Redis connection failed. Reason: {Message}", ex.Message);
            _cache = null;
        }
    }

    private bool IsAvailable => _cache != null && (_redis?.IsConnected ?? false);

    private string BuildKey(string restaurantId, string menuId, string language, string text)
        => $"translation:{restaurantId}:{menuId}:{language}:{text.ToLower().Trim()}";

    public async Task<string?> GetTranslationAsync(string restaurantId, string menuId,
                                                    string language, string text)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Redis unavailable — skipping cache GET.");
            return null;
        }

        try
        {
            string key = BuildKey(restaurantId, menuId, language, text);
            RedisValue value = await _cache!.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                _logger.LogInformation("Cache MISS: {Key}", key);
                return null;
            }

            _logger.LogInformation("Cache HIT: {Key}", key);
            return value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Cache GET failed: {Message}", ex.Message);
            return null;
        }
    }

    public async Task SetTranslationAsync(string restaurantId, string menuId,
                                          string language, string text, string translated)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Redis unavailable — skipping cache SET.");
            return;
        }

        try
        {
            string key = BuildKey(restaurantId, menuId, language, text);
            await _cache!.StringSetAsync(key, translated, TimeSpan.FromHours(24));
            _logger.LogInformation("Cache SET: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Cache SET failed: {Message}", ex.Message);
        }
    }

    public async Task InvalidateMenuCacheAsync(string restaurantId, string menuId)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Redis unavailable — skipping invalidation.");
            return;
        }

        try
        {
            StackExchange.Redis.IServer server = _redis!.GetServer(
                _redis.GetEndPoints().First());

            string pattern = $"translation:{restaurantId}:{menuId}:*";
            int deleted = 0;

            foreach (RedisKey key in server.Keys(pattern: pattern))
            {
                await _cache!.KeyDeleteAsync(key);
                deleted++;
            }

            _logger.LogInformation("Cache invalidated {Count} keys for menu {MenuId}",
                deleted, menuId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Cache invalidation failed: {Message}", ex.Message);
        }
    }
}