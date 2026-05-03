using AutoMapper;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BreastCancer.Service.Implementation
{
    public class ChatbotService : IChatbotService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatbotService> _logger;
        private readonly string _chatbotApiUrl;
        private readonly int _chatbotTimeoutSeconds;

        public ChatbotService(IUnitOfWork unitOfWork, IMapper mapper, UserManager<ApplicationUser> userManager, HttpClient httpClient, IConfiguration configuration, ILogger<ChatbotService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
            _httpClient = httpClient;
            _logger = logger;
            _chatbotApiUrl = configuration.GetValue<string>("ChatbotSettings:ApiUrl") ?? throw new InvalidOperationException("Chatbot API URL is not configured");
            _chatbotTimeoutSeconds = configuration.GetValue<int>("ChatbotSettings:TimeoutSeconds", 30);
        }

        public async Task<ChatbotResponse> AskQuestion(ChatbotAskDTO askDto)
        {
            try
            {
                var diagnosis = await _unitOfWork.PatientDiagnosisRepository.GetByPatientIdAsync(askDto.PatientId);

                if (diagnosis == null)
                {
                    _logger.LogWarning("Patient diagnosis not found for PatientId: {PatientId}", askDto.PatientId);
                    throw new InvalidOperationException("Patient diagnosis not found");
                }

                var chatbotRequest = _mapper.Map<ChatbotRequestDTO>(askDto);

                var patientContext = _mapper.Map<PatientChatbotContextDTO>(diagnosis);

                chatbotRequest.PatientContext = patientContext;

                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_chatbotTimeoutSeconds));

                var response = await _httpClient.PostAsJsonAsync(_chatbotApiUrl, chatbotRequest, cancellationTokenSource.Token);

                if (response.IsSuccessStatusCode)
                {
                    var chatbotAnswer = await response.Content.ReadFromJsonAsync<ChatbotResponse>();
                    return chatbotAnswer;
                }
                else
                {
                    _logger.LogError("Chatbot API returned error: {StatusCode}", response.StatusCode);
                    throw new InvalidOperationException($"Chatbot API error: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error communicating with chatbot API");
                throw new InvalidOperationException("Failed to communicate with chatbot service", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Chatbot API request timed out");
                throw new InvalidOperationException("Chatbot service request timed out", ex);
            }
        }
    }
}
