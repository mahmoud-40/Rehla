using BreastCancer.Context;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Threading.Channels;

namespace BreastCancer.Community.Events;

public sealed class FanoutWorker : BackgroundService
{
    private const string FeedKeyPrefix = "rehla:community:feed:";

    private readonly Channel<FanoutJob> _fanoutChannel;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FanoutWorker> _logger;

    public FanoutWorker(
        Channel<FanoutJob> fanoutChannel,
        IConnectionMultiplexer connectionMultiplexer,
        IServiceScopeFactory scopeFactory,
        ILogger<FanoutWorker> logger)
    {
        _fanoutChannel = fanoutChannel ?? throw new ArgumentNullException(nameof(fanoutChannel));
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FanoutWorker started.");

        try
        {
            await foreach (var job in _fanoutChannel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in FanoutWorker loop. Worker will continue processing next jobs.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown.
        }

        _logger.LogInformation("FanoutWorker stopped.");
    }

    private async Task ProcessJobAsync(FanoutJob job, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BreastCancerDB>();

            var followerIds = await dbContext.Follows
                .AsNoTracking()
                .Where(f => f.FollowingId == job.AuthorId)
                .Select(f => f.FollowerId)
                .ToListAsync(cancellationToken);

            if (followerIds.Count == 0)
            {
                _logger.LogInformation("No followers found for author {AuthorId} when processing fanout job for post {PostId}.", job.AuthorId, job.PostId);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var redisDb = _connectionMultiplexer.GetDatabase();
            var score = job.Timestamp.ToUnixTimeSeconds();
            var postIdMember = job.PostId.ToString();

            var batch = redisDb.CreateBatch();
            var tasks = new List<Task>(followerIds.Count * 2);

            foreach (var followerId in followerIds)
            {
                var key = BuildFeedKey(followerId);
                tasks.Add(batch.SortedSetAddAsync(key, postIdMember, score));
                tasks.Add(batch.SortedSetRemoveRangeByRankAsync(key, 0, -501));
            }

            batch.Execute();
            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Fanout completed for post {PostId}. Author {AuthorId}, followers {FollowerCount}.",
                job.PostId,
                job.AuthorId,
                followerIds.Count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis failure while fanning out post {PostId}.", job.PostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while processing fanout job for post {PostId}.", job.PostId);
        }
    }

    private static string BuildFeedKey(string followerId) => $"{FeedKeyPrefix}{followerId}";
}
