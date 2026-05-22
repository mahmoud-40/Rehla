using System.Runtime.Serialization;

namespace BreastCancer.Community.Exceptions
{
    [Serializable]
    public sealed class AlreadyFollowingException : Exception
    {
        public AlreadyFollowingException(string message) : base(message)
        {
        }

        private AlreadyFollowingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
