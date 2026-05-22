using System.Runtime.Serialization;

namespace BreastCancer.Community.Exceptions;

[Serializable]
public sealed class PostAccessForbiddenException : Exception
{
    public PostAccessForbiddenException(string message) : base(message)
    {
    }

    private PostAccessForbiddenException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
