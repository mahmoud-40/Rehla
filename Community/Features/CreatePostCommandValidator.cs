using BreastCancer.Enum;
using FluentValidation;

namespace BreastCancer.Community.Features;

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(command => command.AuthorId)
            .NotEmpty();

        RuleFor(command => command.Post.Content)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(command => command.Post.MediaUrls)
            .Must(media => media == null || media.Count <= 4)
            .WithMessage("A post can have a maximum of 4 media items");

        RuleForEach(command => command.Post.MediaUrls)
            .NotEmpty()
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Invalid media URL format");

        RuleFor(command => command.Post.Type)
            .Must(type => type is PostType.Story or PostType.Question or PostType.MilestoneShare or PostType.DoctorUpdate)
            .WithMessage("Invalid Post Type");

        RuleFor(command => command.Post.Visibility)
            .Must(visibility => visibility is PostVisibility.Public or PostVisibility.PatientsOnly or PostVisibility.CaregiverOnly or PostVisibility.DoctorOnly)
            .WithMessage("Invalid Post Visibility");
    }
}
