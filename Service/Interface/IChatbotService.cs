using BreastCancer.DTO.request;
using BreastCancer.DTO.response;

namespace BreastCancer.Service.Interface
{
    public interface IChatbotService
    {
        Task<ChatbotResponse> AskQuestion(ChatbotAskDTO askDto);
    }
}
