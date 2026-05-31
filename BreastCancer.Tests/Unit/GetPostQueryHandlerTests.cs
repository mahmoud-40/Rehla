using AutoMapper;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Features.Posts;
using BreastCancer.Context;
using BreastCancer.Enum;
using BreastCancer.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BreastCancer.Tests.Unit;

public sealed class GetPostQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenPostNotFound_ThrowsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var mapper = CreateMapper();
        var handler = new GetPostQueryHandler(dbContext, mapper);

        var act = async () => await handler.Handle(new GetPostQuery(1, "user-1", new[] { "Patient" }), CancellationToken.None);

        await act.Should().ThrowAsync<PostNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenVisibilityForbidden_ThrowsForbidden()
    {
        await using var dbContext = CreateDbContext();
        var post = new Post
        {
            Id = 1,
            AuthorId = "author-1",
            Content = "secret",
            Visibility = PostVisibility.DoctorOnly
        };
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mapper = CreateMapper();
        var handler = new GetPostQueryHandler(dbContext, mapper);

        var act = async () => await handler.Handle(new GetPostQuery(1, "user-1", new[] { "Patient" }), CancellationToken.None);

        await act.Should().ThrowAsync<PostAccessForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenVisible_ReturnsPost()
    {
        await using var dbContext = CreateDbContext();
        var post = new Post
        {
            Id = 1,
            AuthorId = "author-1",
            Content = "hello",
            Visibility = PostVisibility.Public
        };
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mapper = CreateMapper();
        var handler = new GetPostQueryHandler(dbContext, mapper);

        var result = await handler.Handle(new GetPostQuery(1, "user-1", new[] { "Patient" }), CancellationToken.None);

        result.Id.Should().Be(1);
        result.Content.Should().Be("hello");
    }

    private static BreastCancerDB CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase($"get-post-{Guid.NewGuid()}")
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
