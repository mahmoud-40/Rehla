using FluentValidation;
using BreastCancer.Community.DTO.request;

namespace Rehla.Community.Features.Commands.CreateComment
{
    public sealed class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
    {
        private const int MaxContentLength = 1000;
        private const int MinContentLength = 1;
        private const int MaxImageUrlLength = 500;

        public CreateCommentCommandValidator()
        {
            RuleFor(x => x.postId)
                .GreaterThan(0)
                .WithMessage("Post ID must be a valid positive number.");

            RuleFor(x => x.authorId)
                .NotNull()
                .WithMessage("Author ID must be not null.");

            RuleFor(x => x.comment)
                .NotNull()
                .WithMessage("Comment data is required.");

            When(x => x.comment != null, () =>
            {
                RuleFor(x => x.comment.Content)
                    .NotNull()
                    .WithMessage("Comment content cannot be null.")
                    .NotEmpty()
                    .WithMessage("Comment content cannot be empty.")
                    .MinimumLength(MinContentLength)
                    .WithMessage($"Comment content must be at least {MinContentLength} character long.")
                    .MaximumLength(MaxContentLength)
                    .WithMessage($"Comment content cannot exceed {MaxContentLength} characters.")
                    .Must(content => !string.IsNullOrWhiteSpace(content))
                    .WithMessage("Comment content cannot be only whitespace.");

                RuleFor(x => x.comment.ImageUrl)
                    .MaximumLength(MaxImageUrlLength)
                    .WithMessage($"Image URL cannot exceed {MaxImageUrlLength} characters.")
                    .Must(url => string.IsNullOrEmpty(url) || IsValidUrl(url))
                    .WithMessage("Image URL must be a valid URL format.")
                    .When(x => !string.IsNullOrEmpty(x.comment?.ImageUrl));
                
                RuleFor(x => x.comment)
                    .Must(HaveValidContentOrImage)
                    .WithMessage("Comment must have either content or an image.");
            });

            // 7. Security: Check for malicious content
            RuleFor(x => x.comment.Content)
                .Must(content => !ContainsMaliciousContent(content))
                .WithMessage("Content contains potentially harmful characters or scripts.")
                .When(x => x.comment?.Content != null);
        }
        private static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true;

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private static bool HaveValidContentOrImage(CreateCommentDTO comment)
        {
            if (comment == null)
                return false;

            return !string.IsNullOrWhiteSpace(comment.Content) || 
                   !string.IsNullOrWhiteSpace(comment.ImageUrl);
        }
    private static bool ContainsMaliciousContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        // Check for common XSS patterns
        var maliciousPatterns = new[]
        {
            "<script",
            "javascript:",
            "onerror=",
            "onload=",
            "onclick=",
            "onmouseover=",
            "alert(",
            "eval(",
            "document.cookie",
            "localStorage",
            "sessionStorage",
            "window.location",
            "document.write",
            "innerHTML",
            "outerHTML"
        };

        var lowerContent = content.ToLowerInvariant();
        foreach (var pattern in maliciousPatterns)
        {
            if (lowerContent.Contains(pattern.ToLowerInvariant()))
            {
                return true;
            }
        }

        return false;
    }
    }

}