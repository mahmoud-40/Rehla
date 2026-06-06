using System.Runtime.Serialization;

namespace BreastCancer.Community.Exceptions
{
    [Serializable]
    public class DuplicateReactionException : Exception
    {
        public DuplicateReactionException( string message) : base(message) { }

        private DuplicateReactionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
