using BreastCancer.Community.DTO.request;
using BreastCancer.Enum;
using MediatR;

namespace BreastCancer.Community.Features.Commands.AddReaction
{
    public sealed record AddReactionCommand(int PostId, ReactionType Type, string UserId, IReadOnlyCollection<string> Roles) : IRequest<Unit>;
}
