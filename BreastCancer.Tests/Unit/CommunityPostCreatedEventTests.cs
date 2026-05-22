using AutoMapper;
using BreastCancer.Community;
using BreastCancer.Community.DTO.request;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Events;
using BreastCancer.Community.Features;
using BreastCancer.Enum;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BreastCancer.Tests.Unit;

public sealed class CommunityPostCreatedEventTests
{
    [Fact]
    public async Task PostCreatedEvent_FiresAndHandledBySink()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCommunityModule();

        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<Post>(It.IsAny<CreatePostDTO>()))
            .Returns<CreatePostDTO>(dto => new Post
            {
                Content = dto.Content,
                Type = dto.Type,
                Visibility = dto.Visibility,
                MediaUrls = dto.MediaUrls ?? new List<string>()
            });
        mapper.Setup(m => m.Map<PostDTO>(It.IsAny<Post>()))
            .Returns<Post>(post => new PostDTO
            {
                Id = post.Id,
                AuthorId = post.AuthorId,
                Content = post.Content,
                PostType = post.Type,
                PostVisibility = post.Visibility,
                MediaUrls = post.MediaUrls,
                CreatedAt = post.CreatedAt
            });
        services.AddSingleton(mapper.Object);

        var postRepository = new Mock<IPostRepository>();
        postRepository.Setup(r => r.AddAsync(It.IsAny<Post>()))
            .Callback<Post>(post => post.Id = 42)
            .Returns(Task.CompletedTask);

        var transaction = new Mock<IDbContextTransaction>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(u => u.PostRepository).Returns(postRepository.Object);
        unitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
        unitOfWork.Setup(u => u.SaveAsync()).ReturnsAsync(1);
        services.AddSingleton(unitOfWork.Object);

        var recorder = new InMemoryPostCreatedEventSink();
        services.AddScoped<IPostCreatedEventSink>(_ => recorder);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var createdPost = await mediator.Send(new CreatePostCommand(
            new CreatePostDTO
            {
                Content = "Hello",
                Type = PostType.Story,
                Visibility = PostVisibility.Public
            },
            "author-1",
            new[] { "Doctor" }));

        recorder.Events.Should().ContainSingle();
        recorder.Events[0].PostId.Should().Be(createdPost.Id);
        recorder.Events[0].AuthorId.Should().Be("author-1");
        recorder.Events[0].Visibility.Should().Be(PostVisibility.Public);
    }

    private sealed class InMemoryPostCreatedEventSink : IPostCreatedEventSink
    {
        public List<PostCreatedEvent> Events { get; } = new();

        public Task RecordAsync(PostCreatedEvent domainEvent, CancellationToken cancellationToken)
        {
            Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
