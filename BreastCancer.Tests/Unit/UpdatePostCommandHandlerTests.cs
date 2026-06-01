using AutoMapper;
using BreastCancer.Community.DTO.request;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Features.UpdatePost;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Context;
using BreastCancer.Enum;
using BreastCancer.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BreastCancer.Tests.Unit;

public sealed class UpdatePostCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenNotAuthor_ThrowsForbidden()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Posts.Add(new Post { Id = 1, AuthorId = "author-1", Content = "old" });
        await dbContext.SaveChangesAsync();

        var mapper = CreateMapper();
        var cache = new Mock<ICacheService>();
        var handler = new UpdatePostCommandHandler(dbContext, mapper, cache.Object);

        var command = new UpdatePostCommand(1, new UpdatePostDTO { Content = "new", Visibility = PostVisibility.Public }, "user-2");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<PostAccessForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenPostMissing_ThrowsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var mapper = CreateMapper();
        var cache = new Mock<ICacheService>();
        var handler = new UpdatePostCommandHandler(dbContext, mapper, cache.Object);

        var command = new UpdatePostCommand(99, new UpdatePostDTO { Content = "new", Visibility = PostVisibility.Public }, "user-1");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<PostNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAuthor_UpdatesAndCaches()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Posts.Add(new Post { Id = 1, AuthorId = "user-1", Content = "old", Visibility = PostVisibility.Public });
        await dbContext.SaveChangesAsync();

        var mapper = CreateMapper();
        var cache = new Mock<ICacheService>();
        var handler = new UpdatePostCommandHandler(dbContext, mapper, cache.Object);

        var command = new UpdatePostCommand(1, new UpdatePostDTO { Content = "new", Visibility = PostVisibility.DoctorOnly }, "user-1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Content.Should().Be("new");
        result.PostVisibility.Should().Be(PostVisibility.DoctorOnly);
        result.IsEdited.Should().BeTrue();
        cache.Verify(c => c.SetAsync("post:1", It.IsAny<PostDTO>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static BreastCancerDB CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase($"update-post-{Guid.NewGuid()}")
            .Options;
        return new BreastCancerDB(options);
    }

    private static IMapper CreateMapper()
    {
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<PostDTO>(It.IsAny<Post>()))
            .Returns<Post>(post => new PostDTO
            {
                Id = post.Id,
                AuthorId = post.AuthorId,
                Content = post.Content,
                PostType = post.Type,
                PostVisibility = post.Visibility,
                MediaUrls = post.MediaUrls,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                IsEdited = post.IsEdited
            });
        return mapper.Object;
    }
}
