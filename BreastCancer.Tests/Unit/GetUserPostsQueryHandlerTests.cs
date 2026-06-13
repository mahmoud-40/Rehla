using System.Globalization;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Features.Queries.GetUserPosts;
using BreastCancer.Community.Services.Implementation;
using BreastCancer.Context;
using BreastCancer.Enum;
using BreastCancer.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BreastCancer.Tests.Unit;

public sealed class GetUserPostsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var postVisibilityService = new PostVisibilityService(dbContext);
        var handler = new GetUserPostsQueryHandler(dbContext, postVisibilityService);

        var act = async () => await handler.Handle(
            new GetUserPostsQuery("non-existent-user", null, 10, "current-user"),
            CancellationToken.None
        );

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*non-existent-user*");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoPosts_ReturnsEmptyList()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var postVisibilityService = new PostVisibilityService(dbContext);
        var handler = new GetUserPostsQueryHandler(dbContext, postVisibilityService);

        var result = await handler.Handle(
            new GetUserPostsQuery("user-1", null, 10, "viewer-1"),
            CancellationToken.None
        );

        result.Posts.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenViewingOwnPosts_ReturnsAllPostsWithoutVisibilityFilter()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);

        var now = DateTime.UtcNow;
        var posts = new[]
        {
            new Post
            {
                AuthorId = "user-1",
                Content = "Public post",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-30),
                Author = user
            },
            new Post
            {
                AuthorId = "user-1",
                Content = "Doctor only post",
                Visibility = PostVisibility.DoctorOnly,
                Type = PostType.Question,
                CreatedAt = now.AddMinutes(-20),
                Author = user
            },
            new Post
            {
                AuthorId = "user-1",
                Content = "Private post",
                Visibility = PostVisibility.PatientsOnly,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-10),
                Author = user
            }
        };
        dbContext.Posts.AddRange(posts);
        await dbContext.SaveChangesAsync();

        var postVisibilityService = new PostVisibilityService(dbContext);
        var handler = new GetUserPostsQueryHandler(dbContext, postVisibilityService);

        var result = await handler.Handle(
            new GetUserPostsQuery("user-1", null, 10, "user-1"),
            CancellationToken.None
        );

        result.Posts.Should().HaveCount(3);
        result.Total.Should().Be(3);
        result.Posts.Should().ContainSingle(p => p.PostVisibility == PostVisibility.DoctorOnly);
        result.Posts.Should().ContainSingle(p => p.PostVisibility == PostVisibility.PatientsOnly);
    }

    [Fact]
    public async Task Handle_WhenPublicPosts_ReturnsPostsToUnauthenticatedUser()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);

        var now = DateTime.UtcNow;
        var posts = new[]
        {
            new Post
            {
                AuthorId = "user-1",
                Content = "Public post 1",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-30),
                Author = user
            },
            new Post
            {
                AuthorId = "user-1",
                Content = "Private post",
                Visibility = PostVisibility.DoctorOnly,
                Type = PostType.Question,
                CreatedAt = now.AddMinutes(-20),
                Author = user
            },
            new Post
            {
                AuthorId = "user-1",
                Content = "Public post 2",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-10),
                Author = user
            }
        };
        dbContext.Posts.AddRange(posts);
        await dbContext.SaveChangesAsync();

        var postVisibilityService = new PostVisibilityService(dbContext);
        var handler = new GetUserPostsQueryHandler(dbContext, postVisibilityService);

        var result = await handler.Handle(
            new GetUserPostsQuery("user-1", null, 10, CurrentUserId: null),
            CancellationToken.None
        );

        result.Posts.Should().HaveCount(2);
        result.Posts.Should().AllSatisfy(p => p.PostVisibility.Should().Be(PostVisibility.Public));
    }

    [Fact]
    public async Task Handle_WithCursor_ReturnsPaginatedPosts()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);

        var now = DateTime.UtcNow;
        var posts = new[]
        {
            new Post
            {
                AuthorId = "user-1",
                Content = "Post 1",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-30),
                Author = user
            },
            new Post
            {
                AuthorId = "user-1",
                Content = "Post 2",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-20),
                Author = user
            },
            new Post
            {
                AuthorId = "user-1",
                Content = "Post 3",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-10),
                Author = user
            }
        };
        dbContext.Posts.AddRange(posts);
        await dbContext.SaveChangesAsync();

        var postVisibilityService = new PostVisibilityService(dbContext);
        var handler = new GetUserPostsQueryHandler(dbContext, postVisibilityService);

        // First page
        var firstPageResult = await handler.Handle(
            new GetUserPostsQuery("user-1", null, Limit: 2, CurrentUserId: "user-1"),
            CancellationToken.None
        );

        firstPageResult.Posts.Should().HaveCount(2);
        firstPageResult.NextCursor.Should().NotBeNull();
        firstPageResult.Total.Should().Be(3);

        // Second page using cursor
        var secondPageResult = await handler.Handle(
            new GetUserPostsQuery("user-1", firstPageResult.NextCursor, Limit: 2, CurrentUserId: "user-1"),
            CancellationToken.None
        );

        secondPageResult.Posts.Should().HaveCount(1);
        secondPageResult.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ClampsLimitBetween1And50()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);

        var now = DateTime.UtcNow;
        var posts = Enumerable.Range(1, 60)
            .Select(i => new Post
            {
                AuthorId = "user-1",
                Content = $"Post {i}",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-i),
                Author = user
            })
            .ToList();
        dbContext.Posts.AddRange(posts);
        await dbContext.SaveChangesAsync();

        var postVisibilityService = new PostVisibilityService(dbContext);
        var handler = new GetUserPostsQueryHandler(dbContext, postVisibilityService);

        // Test with limit > 50
        var resultLargeLimit = await handler.Handle(
            new GetUserPostsQuery("user-1", null, Limit: 100, CurrentUserId: "user-1"),
            CancellationToken.None
        );

        resultLargeLimit.Posts.Should().HaveCount(50);
        resultLargeLimit.NextCursor.Should().NotBeNull();

        // Test with limit < 1
        var resultSmallLimit = await handler.Handle(
            new GetUserPostsQuery("user-1", null, Limit: -5, CurrentUserId: "user-1"),
            CancellationToken.None
        );

        resultSmallLimit.Posts.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ExcludesDeletedPosts()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);

        var now = DateTime.UtcNow;
        var posts = new[]
        {
            new Post
            {
                AuthorId = "user-1",
                Content = "Active post",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-20),
                IsDeleted = false,
                Author = user
            },
            new Post
            {
                AuthorId = "user-1",
                Content = "Deleted post",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-10),
                IsDeleted = true,
                Author = user
            }
        };
        dbContext.Posts.AddRange(posts);
        await dbContext.SaveChangesAsync();

        var postVisibilityService = new PostVisibilityService(dbContext);
        var handler = new GetUserPostsQueryHandler(dbContext, postVisibilityService);

        var result = await handler.Handle(
            new GetUserPostsQuery("user-1", null, 10, "user-1"),
            CancellationToken.None
        );

        result.Posts.Should().HaveCount(1);
        result.Posts.First().Content.Should().Be("Active post");
    }

    [Fact]
    public async Task Handle_ReturnsPostsOrderedByCreatedAtDescending()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);

        var now = DateTime.UtcNow;
        var posts = new[]
        {
            new Post
            {
                AuthorId = "user-1",
                Content = "Oldest post",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-60),
                Author = user
            },
            new Post
            {
                AuthorId = "user-1",
                Content = "Newest post",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now,
                Author = user
            },
            new Post
            {
                AuthorId = "user-1",
                Content = "Middle post",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-30),
                Author = user
            }
        };
        dbContext.Posts.AddRange(posts);
        await dbContext.SaveChangesAsync();

        var postVisibilityService = new PostVisibilityService(dbContext);
        var handler = new GetUserPostsQueryHandler(dbContext, postVisibilityService);

        var result = await handler.Handle(
            new GetUserPostsQuery("user-1", null, 10, "user-1"),
            CancellationToken.None
        );

        result.Posts.Should().HaveCount(3);
        result.Posts[0].Content.Should().Be("Newest post");
        result.Posts[1].Content.Should().Be("Middle post");
        result.Posts[2].Content.Should().Be("Oldest post");
    }

    [Fact]
    public async Task Handle_PopulatesPostDTOCorrectly()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            ImageUrl = "https://example.com/avatar.jpg"
        };
        var patient = new Patient { UserId = "user-1", User = user };
        user.Patient = patient;

        dbContext.Users.Add(user);
        dbContext.Patients.Add(patient);

        var now = DateTime.UtcNow;
        var post = new Post
        {
            AuthorId = "user-1",
            Content = "Test post content",
            Visibility = PostVisibility.Public,
            Type = PostType.Question,
            MediaUrls = new List<string> { "https://example.com/image1.jpg", "https://example.com/image2.jpg" },
            CreatedAt = now,
            UpdatedAt = now.AddMinutes(5),
            IsEdited = true,
            Author = user
        };

        // Add reactions and comments
        var reaction = new Reaction { UserId = "user-2", Type = ReactionType.Like, Post = post };
        var comment = new Comment { UserId = "user-3", Content = "Great post!", Post = post, IsDeleted = false };

        dbContext.Posts.Add(post);
        dbContext.Reactions.Add(reaction);
        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync();

        var postVisibilityService = new PostVisibilityService(dbContext);
        var handler = new GetUserPostsQueryHandler(dbContext, postVisibilityService);

        var result = await handler.Handle(
            new GetUserPostsQuery("user-1", null, 10, "user-1"),
            CancellationToken.None
        );

        var postDto = result.Posts.First();
        postDto.Id.Should().Be(post.Id);
        postDto.Content.Should().Be("Test post content");
        postDto.PostType.Should().Be(PostType.Question);
        postDto.AuthorId.Should().Be("user-1");
        postDto.AuthorName.Should().Be("John Doe");
        postDto.AuthorAvatarUrl.Should().Be("https://example.com/avatar.jpg");
        postDto.AuthorRole.Should().Be("Patient");
        postDto.ReactionsCount.Should().Be(1);
        postDto.CommentsCount.Should().Be(1);
        postDto.IsLikedByCurrentUser.Should().BeFalse();
        postDto.IsEdited.Should().BeTrue();
        postDto.CreatedAt.Should().Be(now);
        postDto.UpdatedAt.Should().Be(now.AddMinutes(5));
    }

    [Fact]
    public async Task Handle_TracksIsLikedByCurrentUser()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);

        var now = DateTime.UtcNow;
        var post = new Post
        {
            AuthorId = "user-1",
            Content = "Test post",
            Visibility = PostVisibility.Public,
            Type = PostType.Story,
            CreatedAt = now,
            Author = user
        };

        var reaction = new Reaction
        {
            UserId = "viewer-1",
            Type = ReactionType.Like,
            Post = post
        };

        dbContext.Posts.Add(post);
        dbContext.Reactions.Add(reaction);
        await dbContext.SaveChangesAsync();

        var postVisibilityService = new PostVisibilityService(dbContext);
        var handler = new GetUserPostsQueryHandler(dbContext, postVisibilityService);

        // Current user has liked the post
        var resultWithLike = await handler.Handle(
            new GetUserPostsQuery("user-1", null, 10, "viewer-1"),
            CancellationToken.None
        );

        resultWithLike.Posts.First().IsLikedByCurrentUser.Should().BeTrue();

        // Different user hasn't liked the post
        var resultWithoutLike = await handler.Handle(
            new GetUserPostsQuery("user-1", null, 10, "viewer-2"),
            CancellationToken.None
        );

        resultWithoutLike.Posts.First().IsLikedByCurrentUser.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithInvalidCursorFormat_IgnoresCursor()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);

        var now = DateTime.UtcNow;
        var posts = new[]
        {
            new Post
            {
                AuthorId = "user-1",
                Content = "Post 1",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-20),
                Author = user
            },
            new Post
            {
                AuthorId = "user-1",
                Content = "Post 2",
                Visibility = PostVisibility.Public,
                Type = PostType.Story,
                CreatedAt = now.AddMinutes(-10),
                Author = user
            }
        };
        dbContext.Posts.AddRange(posts);
        await dbContext.SaveChangesAsync();

        var postVisibilityService = new PostVisibilityService(dbContext);
        var handler = new GetUserPostsQueryHandler(dbContext, postVisibilityService);

        // Invalid cursor format should be ignored and all posts returned
        var result = await handler.Handle(
            new GetUserPostsQuery("user-1", "invalid-cursor", 10, "user-1"),
            CancellationToken.None
        );

        result.Posts.Should().HaveCount(2);
    }

    private static BreastCancerDB CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase($"get-user-posts-{Guid.NewGuid()}")
            .Options;
        return new BreastCancerDB(options);
    }
}
