using System.Net.Http.Headers;
using System.Security;
using System.Text;
using Microsoft.Extensions.Options;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Infrastructure.Options;

namespace V3RII.Infrastructure.Services;

public sealed class VoiceSynthesisService(IHttpClientFactory httpClientFactory, IOptions<VoiceOptions> voiceOptions) : IVoiceSynthesisService
{
    private readonly VoiceOptions _voiceOptions = voiceOptions.Value;

    public async Task<VoiceSynthesisResultDto> SynthesizeAsync(VoiceSynthesisRequestDto request, CancellationToken cancellationToken = default)
    {
        var voiceName = ResolveVoiceName(request.Language, request.Persona);

        if (!_voiceOptions.Enabled ||
            !_voiceOptions.Provider.Equals("AzureSpeech", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(_voiceOptions.AzureSpeechKey) ||
            string.IsNullOrWhiteSpace(_voiceOptions.AzureSpeechRegion))
        {
            return new VoiceSynthesisResultDto(false, null, _voiceOptions.ContentType, _voiceOptions.Provider, voiceName);
        }

        var language = ResolveLanguage(request.Language);
        var escapedText = SecurityElement.Escape(request.Text.Trim()) ?? string.Empty;
        var ssml = $"""
        <speak version="1.0" xml:lang="{language}" xmlns="http://www.w3.org/2001/10/synthesis">
          <voice name="{voiceName}">{escapedText}</voice>
        </speak>
        """;

        var client = httpClientFactory.CreateClient();
        using var message = new HttpRequestMessage(HttpMethod.Post, $"https://{_voiceOptions.AzureSpeechRegion}.tts.speech.microsoft.com/cognitiveservices/v1");
        message.Headers.Add("Ocp-Apim-Subscription-Key", _voiceOptions.AzureSpeechKey);
        message.Headers.Add("X-Microsoft-OutputFormat", _voiceOptions.OutputFormat);
        message.Headers.UserAgent.Add(new ProductInfoHeaderValue("V3RII", "1.0"));
        message.Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new VoiceSynthesisResultDto(false, null, _voiceOptions.ContentType, _voiceOptions.Provider, voiceName);
        }

        var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return new VoiceSynthesisResultDto(true, Convert.ToBase64String(audioBytes), _voiceOptions.ContentType, _voiceOptions.Provider, voiceName);
    }

    private string ResolveVoiceName(string? language, string? persona)
    {
        var isEnglish = ResolveLanguage(language).StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var isMale = persona?.Equals("male", StringComparison.OrdinalIgnoreCase) == true;

        return (isEnglish, isMale) switch
        {
            (true, true) => _voiceOptions.EnglishMaleVoice,
            (true, false) => _voiceOptions.EnglishFemaleVoice,
            (false, true) => _voiceOptions.TurkishMaleVoice,
            _ => _voiceOptions.TurkishFemaleVoice
        };
    }

    private static string ResolveLanguage(string? language)
    {
        return language?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true ? "en-US" : "tr-TR";
    }
}
