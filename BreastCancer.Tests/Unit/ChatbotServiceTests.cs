using System.Net;
using System.Net.Http.Json;
using AutoMapper;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Exceptions;
using BreastCancer.Service.Implementation;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BreastCancer.Tests.Unit;

public class ChatbotServiceTests
{
    [Fact]
    public async Task AskQuestion_WhenDiagnosisExists_ReturnsChatbotResponse()
    {
        var sut = CreateSut();
        var diagnosis = NewDiagnosis();

        sut.PatientDiagnosisRepository
            .Setup(x => x.GetByPatientIdAsync("patient-1"))
            .ReturnsAsync(diagnosis);

        sut.Handler.Response = new ChatbotResponse { Answer = "Use a balanced diet." };

        var result = await sut.Service.AskQuestion(new ChatbotAskDTO
        {
            PatientId = "patient-1",
            Question = "What should I eat?"
        });

        result.Should().NotBeNull();
        result.Answer.Should().Be("Use a balanced diet.");
        sut.Handler.LastRequest.Should().NotBeNull();
        sut.Handler.LastRequest!.Question.Should().Be("What should I eat?");
        sut.Handler.LastRequest.PatientContext.CancerType.Should().Be("Invasive Ductal Carcinoma");
        sut.Handler.LastRequest.PatientContext.AgeAtDiagnosis.Should().Be(42);
    }

    [Fact]
    public async Task AskQuestion_WhenDiagnosisMissing_ThrowsDiagnosisNotFoundException()
    {
        var sut = CreateSut();

        sut.PatientDiagnosisRepository
            .Setup(x => x.GetByPatientIdAsync("missing"))
            .ReturnsAsync((PatientDiagnosis?)null);

        var act = async () => await sut.Service.AskQuestion(new ChatbotAskDTO
        {
            PatientId = "missing",
            Question = "Any advice?"
        });

        await act.Should().ThrowAsync<ChatbotDiagnosisNotFoundException>()
            .WithMessage("Patient diagnosis not found");
    }

    [Fact]
    public async Task AskQuestion_WhenApiReturnsError_ThrowsExternalServiceException()
    {
        var sut = CreateSut();
        sut.PatientDiagnosisRepository
            .Setup(x => x.GetByPatientIdAsync("patient-1"))
            .ReturnsAsync(NewDiagnosis());

        sut.Handler.HttpStatus = HttpStatusCode.BadRequest;

        var act = async () => await sut.Service.AskQuestion(new ChatbotAskDTO
        {
            PatientId = "patient-1",
            Question = "Any advice?"
        });

        var exception = await act.Should().ThrowAsync<ChatbotExternalServiceException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AskQuestion_WhenHttpRequestFails_ThrowsExternalServiceException()
    {
        var sut = CreateSut();
        sut.PatientDiagnosisRepository
            .Setup(x => x.GetByPatientIdAsync("patient-1"))
            .ReturnsAsync(NewDiagnosis());

        sut.Handler.FailWithHttpRequestException = true;

        var act = async () => await sut.Service.AskQuestion(new ChatbotAskDTO
        {
            PatientId = "patient-1",
            Question = "Any advice?"
        });

        var exception = await act.Should().ThrowAsync<ChatbotExternalServiceException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task AskQuestion_WhenRequestTimesOut_ThrowsTimeoutException()
    {
        var sut = CreateSut();
        sut.PatientDiagnosisRepository
            .Setup(x => x.GetByPatientIdAsync("patient-1"))
            .ReturnsAsync(NewDiagnosis());

        sut.Handler.FailWithTimeout = true;

        var act = async () => await sut.Service.AskQuestion(new ChatbotAskDTO
        {
            PatientId = "patient-1",
            Question = "Any advice?"
        });

        await act.Should().ThrowAsync<ChatbotExternalServiceTimeoutException>()
            .WithMessage("Chatbot service request timed out");
    }

    [Fact]
    public void Constructor_WhenApiUrlMissing_ThrowsInvalidOperationException()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var mapper = BuildMapper();
        var httpClient = new HttpClient(new ChatbotHttpMessageHandler());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ChatbotSettings:TimeoutSeconds"] = "5"
            })
            .Build();

        var logger = new Mock<ILogger<ChatbotService>>();

        var act = () => new ChatbotService(
            unitOfWork.Object,
            mapper.Object,
            httpClient,
            configuration,
            logger.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Chatbot API URL is not configured");
    }

    private static PatientDiagnosis NewDiagnosis() => new()
    {
        UserId = "patient-1",
        AgeAtDiagnosis = 42,
        CancerType = "Invasive Ductal Carcinoma",
        CancerTypeDetailed = "Stage II",
        TumorStage = "T2",
        NeoplasmHistologicGrade = "G2",
        ErStatus = "Positive",
        PrStatus = "Positive",
        Her2Status = "Negative",
        Chemotherapy = true,
        HormoneTherapy = true,
        RadioTherapy = false
    };

    private static SutContext CreateSut()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var diagnosisRepository = new Mock<IPatientDiagnosisRepository>();
        unitOfWork.SetupGet(x => x.PatientDiagnosisRepository).Returns(diagnosisRepository.Object);

        var mapper = BuildMapper();

        var handler = new ChatbotHttpMessageHandler();
        var httpClient = new HttpClient(handler);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ChatbotSettings:ApiUrl"] = "https://example.com/chatbot",
                ["ChatbotSettings:TimeoutSeconds"] = "5"
            })
            .Build();

        var logger = new Mock<ILogger<ChatbotService>>();

        var service = new ChatbotService(
            unitOfWork.Object,
            mapper.Object,
            httpClient,
            configuration,
            logger.Object);

        return new SutContext(service, diagnosisRepository, handler);
    }

    private static Mock<IMapper> BuildMapper()
    {
        var mapper = new Mock<IMapper>();

        mapper.Setup(x => x.Map<ChatbotRequestDTO>(It.IsAny<ChatbotAskDTO>()))
            .Returns((ChatbotAskDTO source) => new ChatbotRequestDTO
            {
                Question = source.Question,
                PatientContext = null!
            });

        mapper.Setup(x => x.Map<PatientChatbotContextDTO>(It.IsAny<PatientDiagnosis>()))
            .Returns((PatientDiagnosis source) => new PatientChatbotContextDTO
            {
                AgeAtDiagnosis = source.AgeAtDiagnosis,
                CancerType = source.CancerType ?? string.Empty,
                CancerTypeDetailed = source.CancerTypeDetailed ?? string.Empty,
                TumorStage = source.TumorStage ?? string.Empty,
                NeoplasmHistologicGrade = source.NeoplasmHistologicGrade ?? string.Empty,
                ErStatus = source.ErStatus ?? string.Empty,
                PrStatus = source.PrStatus ?? string.Empty,
                Her2Status = source.Her2Status ?? string.Empty,
                Chemotherapy = source.Chemotherapy,
                HormoneTherapy = source.HormoneTherapy,
                RadioTherapy = source.RadioTherapy
            });

        return mapper;
    }

    private sealed record SutContext(
        ChatbotService Service,
        Mock<IPatientDiagnosisRepository> PatientDiagnosisRepository,
        ChatbotHttpMessageHandler Handler);

    private sealed class ChatbotHttpMessageHandler : HttpMessageHandler
    {
        public ChatbotRequestDTO? LastRequest { get; private set; }
        public ChatbotResponse Response { get; set; } = new() { Answer = "Default" };
        public HttpStatusCode HttpStatus { get; set; } = HttpStatusCode.OK;
        public bool FailWithHttpRequestException { get; set; }
        public bool FailWithTimeout { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (FailWithHttpRequestException)
            {
                throw new HttpRequestException("Network issue");
            }

            if (FailWithTimeout)
            {
                throw new TaskCanceledException("Timed out");
            }

            if (request.Content != null)
            {
                LastRequest = await request.Content.ReadFromJsonAsync<ChatbotRequestDTO>(cancellationToken: cancellationToken);
            }

            if (HttpStatus != HttpStatusCode.OK)
            {
                return new HttpResponseMessage(HttpStatus);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Response)
            };
        }
    }
}
