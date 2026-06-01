using AutoMapper;
using BreastCancer.Community.DTO.request;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Features.CreatePost;
using BreastCancer.Community.Features.UpdatePost;
using BreastCancer.Community.Services.Implementation;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Community.Options;
using BreastCancer.Context;
using BreastCancer.Enum;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using FluentAssertions;
using MediatR;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;
using BreastCancer.Community.Events.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace BreastCancer.Tests.Integration;

[Collection("Redis")]
public sealed class PostCacheIntegrationTests : IAsyncLifetime
{
    private RedisContainer? _redisContainer;
    private IConnectionMultiplexer? _connectionMultiplexer;
    private IServiceProvider? _serviceProvider;

    public async Task InitializeAsync()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();

        var connectionString = _redisContainer.GetConnectionString();
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;
        _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(options);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_connectionMultiplexer);
        services.AddSingleton<IOptions<RedisSettings>>(new OptionsWrapper<RedisSettings>(new RedisSettings
        {
            ConnectionString = connectionString,
            DefaultTTLSeconds = 3600
        }));
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddDbContext<BreastCancerDB>(optionsBuilder =>
            optionsBuilder.UseInMemoryDatabase($"post-cache-{Guid.NewGuid()}"));

        services.AddScoped<IUnitOfWork, FakeUnitOfWork>();
        services.AddAutoMapper(cfg =>
        {
            cfg.CreateMap<CreatePostDTO, Post>();
            cfg.CreateMap<Post, PostDTO>()
                .ForMember(dest => dest.PostVisibility, opt => opt.MapFrom(src => src.Visibility))
                .ForMember(dest => dest.PostType, opt => opt.MapFrom(src => src.Type));
        });

        services.AddScoped<IPublisher, NoOpPublisher>();
        services.AddScoped<CreatePostCommandHandler>();
        services.AddScoped<UpdatePostCommandHandler>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_redisContainer != null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }

        _connectionMultiplexer?.Dispose();
    }

    [Fact]
    public async Task CreateUpdatePost_UpdatesCachedDto()
    {
        await using var scope = _serviceProvider!.CreateAsyncScope();
        var createHandler = scope.ServiceProvider.GetRequiredService<CreatePostCommandHandler>();
        var updateHandler = scope.ServiceProvider.GetRequiredService<UpdatePostCommandHandler>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var created = await createHandler.Handle(new CreatePostCommand(
            new CreatePostDTO
            {
                Content = "Initial",
                Type = PostType.Story,
                Visibility = PostVisibility.Public
            },
            "author-1",
            new[] { "Patient" }),
            CancellationToken.None);

        var cachedAfterCreate = await cacheService.GetAsync<PostDTO>($"post:{created.Id}");
        cachedAfterCreate.Should().NotBeNull();
        cachedAfterCreate!.Content.Should().Be("Initial");

        var updated = await updateHandler.Handle(new UpdatePostCommand(
            created.Id,
            new UpdatePostDTO { Content = "Updated", Visibility = PostVisibility.Public },
            "author-1"),
            CancellationToken.None);

        var cachedAfterUpdate = await cacheService.GetAsync<PostDTO>($"post:{created.Id}");
        cachedAfterUpdate.Should().NotBeNull();
        cachedAfterUpdate!.Content.Should().Be("Updated");
        cachedAfterUpdate.IsEdited.Should().BeTrue();
        updated.Content.Should().Be("Updated");
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly BreastCancerDB _dbContext;
        private readonly IDbContextTransaction _transaction;
        public FakeUnitOfWork(BreastCancerDB dbContext)
        {
            _dbContext = dbContext;
            _transaction = new FakeDbContextTransaction();
            PostRepository = new FakePostRepository(dbContext);
        }

        public IDoctorRepository DoctorsRepository => throw new NotImplementedException();
        public IPatientRepository PatientsRepository => throw new NotImplementedException();
        public ICaregiverRepository CaregiversRepository => throw new NotImplementedException();
        public ITreatmentPlanRepository TreatmentPlansRepository => throw new NotImplementedException();
        public IRefreshTokenRepository RefreshTokenRepository => throw new NotImplementedException();
        public IPatientDiagnosisRepository PatientDiagnosisRepository => throw new NotImplementedException();
        public IPostRepository PostRepository { get; }
        public IFollowRepository FollowRepository => throw new NotImplementedException();
        public INotificationRepository NotificationRepository => throw new NotImplementedException();
        public IHighFollowerPostRepository HighFollowerPostRepository => throw new NotImplementedException();

        public Task<IDbContextTransaction> BeginTransactionAsync() => Task.FromResult(_transaction);

        public Task<int> SaveAsync() => _dbContext.SaveChangesAsync();
        public void Save() => _dbContext.SaveChanges();
    }

    private sealed class FakeDbContextTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public void Commit() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Rollback() { }
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void CreateSavepoint(string name) { }
        public Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void ReleaseSavepoint(string name) { }
        public Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void RollbackToSavepoint(string name) { }
        public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakePostRepository : IPostRepository
    {
        private readonly BreastCancerDB _dbContext;
        public FakePostRepository(BreastCancerDB dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Post entity)
        {
            _dbContext.Posts.Add(entity);
            await _dbContext.SaveChangesAsync();
        }

        public void Add(Post entity) => _dbContext.Posts.Add(entity);
        public void Update(Post entity) => _dbContext.Posts.Update(entity);
        public void Delete(Post entity) => _dbContext.Posts.Remove(entity);
        public Task<IEnumerable<Post>> GetAllAsync() => _dbContext.Posts.ToListAsync().ContinueWith(t => t.Result.AsEnumerable());
        public Task<Post?> GetByIdAsync(string id) => _dbContext.Posts.FindAsync(id).AsTask();
        public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
        public Task<IEnumerable<Post>> FilterAsync(
            Expression<Func<Post, bool>> predicate,
            Func<IQueryable<Post>, IOrderedQueryable<Post>>? orderBy = null,
            int? pageNumber = null,
            int? pageSize = null)
        {
            IQueryable<Post> query = _dbContext.Posts.Where(predicate);
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                var skip = (pageNumber.Value - 1) * pageSize.Value;
                query = query.Skip(skip).Take(pageSize.Value);
            }

            return query.ToListAsync().ContinueWith(t => t.Result.AsEnumerable());
        }

    }

    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
            => Task.CompletedTask;

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
