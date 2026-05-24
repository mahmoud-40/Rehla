using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Features;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace BreastCancer.Tests.Unit;

public sealed class UnfollowUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenFollowMissing_ThrowsNotFound()
    {
        var sut = CreateSut(new List<Follow>());
        var command = new UnfollowUserCommand("user-1", "user-2");

        var act = async () => await sut.Handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenFollowExists_Deletes()
    {
        var existing = new Follow { FollowerId = "user-1", FollowingId = "user-2" };
        var sut = CreateSut(new List<Follow> { existing });
        var command = new UnfollowUserCommand("user-1", "user-2");

        var result = await sut.Handler.Handle(command, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        sut.Repository.Verify(r => r.Delete(existing), Times.Once);
        sut.UnitOfWork.Verify(u => u.SaveAsync(), Times.Once);
    }

    private static Sut CreateSut(List<Follow> existing)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var repository = new Mock<IFollowRepository>();

        repository.Setup(r => r.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Follow, bool>>>(), It.IsAny<Func<System.Linq.IQueryable<Follow>, System.Linq.IOrderedQueryable<Follow>>>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(existing);

        unitOfWork.SetupGet(u => u.FollowRepository).Returns(repository.Object);
        unitOfWork.Setup(u => u.SaveAsync()).ReturnsAsync(1);
        var transaction = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
        unitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transaction.Object);

        var handler = new UnfollowUserCommandHandler(unitOfWork.Object);
        return new Sut(handler, unitOfWork, repository);
    }

    private sealed record Sut(UnfollowUserCommandHandler Handler, Mock<IUnitOfWork> UnitOfWork, Mock<IFollowRepository> Repository);
}
