using BreastCancer.Enum;
using MediatR;

namespace BreastCancer.Community.Features.Commands.RemoveReaction
{
    public sealed record RemoveReactionCommand(int PostId, string UserId) : IRequest<Unit>;
}
