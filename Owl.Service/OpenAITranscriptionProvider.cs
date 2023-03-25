using Newtonsoft.Json;

using System.Net.Http.Headers;

namespace Owl.Service;

public class OpenAiTranscriptionProvider : TranscriptionProvider
{
    private readonly HttpClient _httpClient;

    public OpenAiTranscriptionProvider(IConfiguration configuration, IRecorder recorder)
        : base(recorder)
    {
        var apiKey = configuration["OpenAI:ApiKey"]!;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public override Task InitializeAsync() => Task.CompletedTask;

    public override async Task ProcessAudioAsync(byte[] buffer, int count)
    {

        var audio = ByteString.CopyFrom(buffer, 0, count);
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFilePath, audio.ToByteArray());
        var requestContent = new MultipartFormDataContent
        {
            { new ByteArrayContent(await File.ReadAllBytesAsync(tempFilePath)), "file", "audio.wav" }
        };

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/whisper/asr", requestContent);
        response.EnsureSuccessStatusCode();

        // Delete the temporary file
        File.Delete(tempFilePath);

        // Parse the response and return the transcribed text
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var parsedResponse = JsonConvert.DeserializeObject<OpenAiResponse>(jsonResponse);
        await TranscribeAsync(parsedResponse?.Choices[0].Text, true);
    }
}