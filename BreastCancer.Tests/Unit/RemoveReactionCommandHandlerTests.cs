using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Features.Commands.RemoveReaction;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Enum;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BreastCancer.Tests.Unit;

public sealed class RemoveReactionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenReactionDoesNotExist_ThrowsReactionNotFoundException()
    {
        // Arrange
        var sut = CreateSut();
        sut.ReactionRepository
            .Setup(r => r.GetReactionByPostIdAndUserIdAsync(1, "user-1"))
            .ReturnsAsync((Reaction?)null);

        var command = new RemoveReactionCommand(1, "user-1");

        // Act
        var act = async () => await sut.Handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ReactionNotFoundException>()
            .WithMessage("You have not reacted to this post.");
    }

    [Fact]
    public async Task Handle_WhenValid_DeletesReactionAndDecrementsCache()
    {
        // Arrange
        var sut = CreateSut();
        var existingReaction = new Reaction 
        { 
            Id = 10, 
            PostId = 1, 
            UserId = "user-1", 
            Type = ReactionType.Like 
        };

        sut.ReactionRepository
            .Setup(r => r.GetReactionByPostIdAndUserIdAsync(1, "user-1"))
            .ReturnsAsync(existingReaction);

        var command = new RemoveReactionCommand(1, "user-1");

        // Act
        var result = await sut.Handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);

        sut.ReactionRepository.Verify(r => r.Delete(It.Is<Reaction>(r => 
            r.Id == 10 && 
            r.PostId == 1 && 
            r.UserId == "user-1")), Times.Once);

        sut.UnitOfWork.Verify(u => u.SaveAsync(), Times.Once);

        sut.CacheService.Verify(c => c.DecrementHashFieldAsync(
            "post:1:reactions",
            ReactionType.Like.ToString(),
            1,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Sut CreateSut()
    {
        var logger = new Mock<ILogger<RemoveReactionCommandHandler>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var cacheService = new Mock<ICacheService>();
        var reactionRepository = new Mock<IReactionRepository>();

        unitOfWork.SetupGet(u => u.ReactionRepository).Returns(reactionRepository.Object);
        unitOfWork.Setup(u => u.SaveAsync()).ReturnsAsync(1);

        var handler = new RemoveReactionCommandHandler(cacheService.Object, logger.Object, unitOfWork.Object);

        return new Sut(handler, unitOfWork, cacheService, reactionRepository);
    }

    private sealed record Sut(
        RemoveReactionCommandHandler Handler,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<ICacheService> CacheService,
        Mock<IReactionRepository> ReactionRepository);
}