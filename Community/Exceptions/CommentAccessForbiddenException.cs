namespace BreastCancer.Community.Exceptions;

public class CommentAccessForbiddenException : Exception
{
    public CommentAccessForbiddenException(string message) : base(message) { }
    public CommentAccessForbiddenException(string message, Exception innerException) 
        : base(message, innerException) { }
}