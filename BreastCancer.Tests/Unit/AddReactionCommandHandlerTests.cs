using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Features.Commands.AddReaction;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Enum;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace BreastCancer.Tests.Unit;

public sealed class AddReactionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenPostDoesNotExist_ThrowsPostNotFoundException()
    {
        var sut = CreateSut();
        sut.PostRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Post?)null);

        var command = new AddReactionCommand(1, ReactionType.Like, "user-1", new[] { "Patient" });

        var act = async () => await sut.Handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<PostNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenPostIsDeleted_ThrowsPostNotFoundException()
    {
        var sut = CreateSut();
        sut.PostRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Post { Id = 1, IsDeleted = true });

        var command = new AddReactionCommand(1, ReactionType.Like, "user-1", new[] { "Patient" });

        var act = async () => await sut.Handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<PostNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserCannotSeePost_ThrowsPostAccessForbiddenException()
    {
        var sut = CreateSut();
        // Post is DoctorOnly, but the user is a Patient
        sut.PostRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Post { Id = 1, Visibility = PostVisibility.DoctorOnly, IsDeleted = false });

        var command = new AddReactionCommand(1, ReactionType.Like, "user-1", new[] { "Patient" });

        var act = async () => await sut.Handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<PostAccessForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyReacted_ThrowsDuplicateReactionException()
    {
        var sut = CreateSut();
        sut.PostRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Post { Id = 1, Visibility = PostVisibility.Public, IsDeleted = false });

        sut.ReactionRepository.Setup(r => r.FilterAsync(
            It.IsAny<Expression<Func<Reaction, bool>>>(),
            It.IsAny<Func<IQueryable<Reaction>, IOrderedQueryable<Reaction>>?>(), 
            It.IsAny<int?>()))
            .ReturnsAsync(new List<Reaction> { new Reaction { Id = 10 } });

        var command = new AddReactionCommand(1, ReactionType.Like, "user-1", new[] { "Patient" });

        var act = async () => await sut.Handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateReactionException>();
    }

    [Fact]
    public async Task Handle_WhenValid_SavesReactionAndIncrementsCache()
    {
        var sut = CreateSut();
        sut.PostRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Post { Id = 1, Visibility = PostVisibility.Public, IsDeleted = false });

        sut.ReactionRepository.Setup(r => r.FilterAsync(
            It.IsAny<Expression<Func<Reaction, bool>>>(),
            It.IsAny<Func<IQueryable<Reaction>, IOrderedQueryable<Reaction>>?>(), 
            It.IsAny<int?>()))
            .ReturnsAsync(new List<Reaction>());

        var command = new AddReactionCommand(1, ReactionType.Like, "user-1", new[] { "Patient" });

        var result = await sut.Handler.Handle(command, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);

        sut.ReactionRepository.Verify(r => r.AddAsync(It.Is<Reaction>(reaction =>
            reaction.PostId == 1 &&
            reaction.UserId == "user-1" &&
            reaction.Type == ReactionType.Like)), Times.Once);

        sut.UnitOfWork.Verify(u => u.SaveAsync(), Times.Once);

        sut.CacheService.Verify(c => c.IncrementHashFieldAsync(
            "post:1:reactions",
            ReactionType.Like.ToString(),
            1,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Sut CreateSut()
    {
        var logger = new Mock<ILogger<AddReactionCommandHandler>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var cacheService = new Mock<ICacheService>();

        var postRepository = new Mock<IPostRepository>();
        var reactionRepository = new Mock<IReactionRepository>();

        unitOfWork.SetupGet(u => u.PostRepository).Returns(postRepository.Object);
        unitOfWork.SetupGet(u => u.ReactionRepository).Returns(reactionRepository.Object);
        unitOfWork.Setup(u => u.SaveAsync()).ReturnsAsync(1);

        reactionRepository.Setup(r => r.AddAsync(It.IsAny<Reaction>())).Returns(Task.CompletedTask);

        var handler = new AddReactionCommandHandler(logger.Object, unitOfWork.Object, cacheService.Object);

        return new Sut(handler, unitOfWork, cacheService, postRepository, reactionRepository);
    }

    private sealed record Sut(
        AddReactionCommandHandler Handler,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<ICacheService> CacheService,
        Mock<IPostRepository> PostRepository,
        Mock<IReactionRepository> ReactionRepository);
}