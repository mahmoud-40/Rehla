using BreastCancer.Community.DTO.request;
using BreastCancer.Community.Features;
using BreastCancer.Enum;
using FluentAssertions;
using Xunit;
namespace BreastCancer.Tests.Unit;

public sealed class CreatePostCommandValidatorTests
{
    private readonly CreatePostCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenContentTooLong_ReturnsError()
    {
        var content = new string('a', 2001);
        var command = CreateCommand(new CreatePostDTO
        {
            Content = content,
            Type = PostType.Story,
            Visibility = PostVisibility.Public
        });

        var result = _validator.Validate(command);

        result.Errors.Should().Contain(error => error.PropertyName == "Post.Content");
    }

    [Fact]
    public void Validate_WhenTooManyMediaUrls_ReturnsError()
    {
        var command = CreateCommand(new CreatePostDTO
        {
            Content = "Hello",
            Type = PostType.Story,
            Visibility = PostVisibility.Public,
            MediaUrls = new List<string> { "https://one", "https://two", "https://three", "https://four", "https://five" }
        });

        var result = _validator.Validate(command);

        result.Errors.Should().Contain(error => error.PropertyName == "Post.MediaUrls");
    }

    [Fact]
    public void Validate_WhenMediaUrlInvalid_ReturnsError()
    {
        var command = CreateCommand(new CreatePostDTO
        {
            Content = "Hello",
            Type = PostType.Story,
            Visibility = PostVisibility.Public,
            MediaUrls = new List<string> { "not-a-url" }
        });

        var result = _validator.Validate(command);

        result.Errors.Should().Contain(error => error.PropertyName == "Post.MediaUrls[0]");
    }

    [Fact]
    public void Validate_WhenVisibilityNotAllowed_ReturnsError()
    {
        var command = CreateCommand(new CreatePostDTO
        {
            Content = "Hello",
            Type = PostType.Story,
            Visibility = PostVisibility.FollowersOnly
        });

        var result = _validator.Validate(command);

        result.Errors.Should().Contain(error => error.PropertyName == "Post.Visibility");
    }

    private static CreatePostCommand CreateCommand(CreatePostDTO dto)
        => new(dto, "author-1", new[] { "Doctor" });
}
