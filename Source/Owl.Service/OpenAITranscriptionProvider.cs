using Newtonsoft.Json;

using System.Net.Http.Headers;

namespace Owl.Service;

public class OpenAiTranscriptionProvider : TranscriptionProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiTranscriptionProvider> _logger;

    public OpenAiTranscriptionProvider(IConfiguration configuration, IRecorder recorder, ILoggerFactory loggerFactory)
        : base(recorder)
    {
        _logger = loggerFactory.CreateLogger<OpenAiTranscriptionProvider>();
        var apiKey = configuration["OpenAI:ApiKey"]!;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public override Task InitializeAsync() => Task.CompletedTask;

    public override async Task ProcessAudioAsync(byte[] buffer, int count)
    {

        try
        {
            var audio = ByteString.CopyFrom(buffer, 0, count);
            var requestContent = new MultipartFormDataContent {
                {
                    new ByteArrayContent(audio.ToByteArray()) {
                        Headers = {
                            ContentType = new MediaTypeHeaderValue("audio/wav")
                        }
                    },
                    "file",
                    "audio.wav"
                }
            };
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/engines/whisper/transcriptions", requestContent);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var parsedResponse = JsonConvert.DeserializeObject<OpenAiResponse>(jsonResponse);
            await TranscribeAsync(parsedResponse?.Choices[0].Text, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio.");
        }
    }
}