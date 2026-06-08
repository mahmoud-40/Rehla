using System.Runtime.Serialization;

namespace BreastCancer.Community.Exceptions
{
    [Serializable]
    public sealed class ReactionNotFoundException : Exception
    {
        public ReactionNotFoundException(string message) : base(message) { }
        private ReactionNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
