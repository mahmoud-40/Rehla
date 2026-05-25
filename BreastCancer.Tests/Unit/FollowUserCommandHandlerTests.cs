using BreastCancer.Community.Events.Models;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Features.FollowUser;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace BreastCancer.Tests.Unit;

public sealed class FollowUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenAlreadyFollowing_ThrowsAlreadyFollowing()
    {
        var sut = CreateSut(existingFollow: true);
        var command = new FollowUserCommand("user-1", "user-2");

        var act = async () => await sut.Handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<AlreadyFollowingException>();
    }

    [Fact]
    public async Task Handle_WhenNewFollow_SavesAndPublishes()
    {
        var sut = CreateSut(existingFollow: false);
        var command = new FollowUserCommand("user-1", "user-2");

        var result = await sut.Handler.Handle(command, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        sut.UnitOfWork.Verify(u => u.SaveAsync(), Times.Once);
        sut.Publisher.Verify(p => p.Publish(It.IsAny<FollowCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Sut CreateSut(bool existingFollow)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var repository = new Mock<IFollowRepository>();
        var publisher = new Mock<IPublisher>();
        var transaction = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();

        var existing = existingFollow
            ? new List<Follow> { new() { FollowerId = "user-1", FollowingId = "user-2" } }
            : new List<Follow>();

        repository.Setup(r => r.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Follow, bool>>>(), It.IsAny<Func<System.Linq.IQueryable<Follow>, System.Linq.IOrderedQueryable<Follow>>>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(existing);
        repository.Setup(r => r.AddAsync(It.IsAny<Follow>())).Returns(Task.CompletedTask);

        unitOfWork.SetupGet(u => u.FollowRepository).Returns(repository.Object);
        unitOfWork.Setup(u => u.SaveAsync()).ReturnsAsync(1);
        unitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transaction.Object);

        var handler = new FollowUserCommandHandler(publisher.Object, unitOfWork.Object);
        return new Sut(handler, unitOfWork, publisher);
    }

    private sealed record Sut(FollowUserCommandHandler Handler, Mock<IUnitOfWork> UnitOfWork, Mock<IPublisher> Publisher);
}
