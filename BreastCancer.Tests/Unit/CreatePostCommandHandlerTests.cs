using BreastCancer.Community.DTO.request;
using BreastCancer.Community.Exceptions;
using BreastCancer.Enum;
using AutoMapper;
using BreastCancer.Community.DTO.response;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;
using BreastCancer.Community.Events.Models;
using BreastCancer.Community.Features.CreatePost;

namespace BreastCancer.Tests.Unit;

public sealed class CreatePostCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenNonDoctorCreatesDoctorUpdate_ThrowsForbidden()
    {
        var sut = CreateSut();
        var command = new CreatePostCommand(
            new CreatePostDTO
            {
                Content = "Doctor update",
                Type = PostType.DoctorUpdate,
                Visibility = PostVisibility.Public
            },
            "author-1",
            new[] { "Patient" });

        var act = async () => await sut.Handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<PostAccessForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenDoctorCreatesDoctorUpdate_SavesAndPublishes()
    {
        var sut = CreateSut();
        var command = new CreatePostCommand(
            new CreatePostDTO
            {
                Content = "Doctor update",
                Type = PostType.DoctorUpdate,
                Visibility = PostVisibility.DoctorOnly,
                MediaUrls = new List<string> { "https://media" }
            },
            "doctor-1",
            new[] { "Doctor" });

        var result = await sut.Handler.Handle(command, CancellationToken.None);

        result.AuthorId.Should().Be("doctor-1");
        result.PostType.Should().Be(PostType.DoctorUpdate);
        result.PostVisibility.Should().Be(PostVisibility.DoctorOnly);
        result.MediaUrls.Should().ContainSingle("https://media");
        sut.Publisher.Verify(p => p.Publish(It.IsAny<PostCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        sut.UnitOfWork.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNonDoctorCreatesStory_AllowsCreation()
    {
        var sut = CreateSut();
        var command = new CreatePostCommand(
            new CreatePostDTO
            {
                Content = "Story",
                Type = PostType.Story,
                Visibility = PostVisibility.Public
            },
            "patient-1",
            new[] { "Patient" });

        var result = await sut.Handler.Handle(command, CancellationToken.None);

        result.AuthorId.Should().Be("patient-1");
        result.PostType.Should().Be(PostType.Story);
        sut.Publisher.Verify(p => p.Publish(It.IsAny<PostCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Sut CreateSut()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var repository = new Mock<IPostRepository>();
        var publisher = new Mock<IPublisher>();
        var transaction = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();

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
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                IsEdited = post.IsEdited
            });

        unitOfWork.SetupGet(u => u.PostRepository).Returns(repository.Object);
        unitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
        unitOfWork.Setup(u => u.SaveAsync()).ReturnsAsync(1);

        repository.Setup(r => r.AddAsync(It.IsAny<Post>())).Returns(Task.CompletedTask);

        var handler = new CreatePostCommandHandler(mapper.Object, publisher.Object, unitOfWork.Object);
        return new Sut(handler, unitOfWork, publisher);
    }

    private sealed record Sut(CreatePostCommandHandler Handler, Mock<IUnitOfWork> UnitOfWork, Mock<IPublisher> Publisher);
}
