using System.Runtime.Serialization;

namespace BreastCancer.Community.Exceptions
{
    [Serializable]
    public sealed class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message) : base(message)
        {
        }
        
        private UserNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
