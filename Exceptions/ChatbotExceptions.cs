using System.Net;

namespace BreastCancer.Service.Exceptions
{
    public class ChatbotDiagnosisNotFoundException : Exception
    {
        public ChatbotDiagnosisNotFoundException(string message)
            : base(message)
        {
        }
    }

    public class ChatbotExternalServiceException : Exception
    {
        public HttpStatusCode? StatusCode { get; }

        public ChatbotExternalServiceException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }

    public sealed class ChatbotExternalServiceTimeoutException : ChatbotExternalServiceException
    {
        public ChatbotExternalServiceTimeoutException(string message, Exception? innerException = null)
            : base(message, HttpStatusCode.GatewayTimeout, innerException)
        {
        }
    }
}
