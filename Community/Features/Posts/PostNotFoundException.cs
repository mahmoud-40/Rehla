using System.Runtime.Serialization;

namespace BreastCancer.Community.Features.Posts;

[Serializable]
public sealed class PostNotFoundException : Exception
{
    public PostNotFoundException(string message) : base(message)
    {
    }

    private PostNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
