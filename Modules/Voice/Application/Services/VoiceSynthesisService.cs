using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Infrastructure.Options;

namespace V3RII.Infrastructure.Services;

public sealed class VoiceSynthesisService(IHttpClientFactory httpClientFactory, IOptions<VoiceOptions> voiceOptions) : IVoiceSynthesisService
{
    private readonly VoiceOptions _voiceOptions = voiceOptions.Value;
    private const string RealtimeEndpoint = "https://api.openai.com/v1/realtime/calls";

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

    public async Task<VoiceRealtimeSessionResultDto> CreateRealtimeSessionAsync(VoiceRealtimeSessionRequestDto request, CancellationToken cancellationToken = default)
    {
        var voice = ResolveRealtimeVoice(request.Persona);
        if (!_voiceOptions.RealtimeEnabled || string.IsNullOrWhiteSpace(_voiceOptions.OpenAiApiKey))
        {
            return new VoiceRealtimeSessionResultDto(false, null, _voiceOptions.RealtimeModel, voice, RealtimeEndpoint, null);
        }

        var instructions = BuildRealtimeInstructions(request.Language);
        var payload = new
        {
            session = new
            {
                type = "realtime",
                model = _voiceOptions.RealtimeModel,
                instructions,
                audio = new
                {
                    input = new
                    {
                        turn_detection = new
                        {
                            type = "server_vad",
                            interrupt_response = true
                        }
                    },
                    output = new
                    {
                        voice
                    }
                }
            },
            expires_after = new
            {
                anchor = "created_at",
                seconds = Math.Clamp(_voiceOptions.RealtimeClientSecretTtlSeconds, 60, 600)
            }
        };

        var client = httpClientFactory.CreateClient();
        using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/realtime/client_secrets");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _voiceOptions.OpenAiApiKey);
        message.Headers.UserAgent.Add(new ProductInfoHeaderValue("V3RII", "1.0"));
        message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new VoiceRealtimeSessionResultDto(false, null, _voiceOptions.RealtimeModel, voice, RealtimeEndpoint, null);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var clientSecret = TryReadString(root, "value")
            ?? TryReadString(root, "client_secret", "value")
            ?? TryReadString(root, "client_secret");
        var expiresAt = TryReadUnixTime(root, "expires_at") ?? TryReadUnixTime(root, "client_secret", "expires_at");

        return new VoiceRealtimeSessionResultDto(
            !string.IsNullOrWhiteSpace(clientSecret),
            clientSecret,
            _voiceOptions.RealtimeModel,
            voice,
            RealtimeEndpoint,
            expiresAt);
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

    private string ResolveRealtimeVoice(string? persona)
    {
        return persona?.Equals("male", StringComparison.OrdinalIgnoreCase) == true
            ? _voiceOptions.RealtimeMaleVoice
            : _voiceOptions.RealtimeFemaleVoice;
    }

    private static string BuildRealtimeInstructions(string? language)
    {
        var isEnglish = language?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
        return isEnglish
            ? """
              You are the V3RII website voice assistant. Speak clearly and concisely. Help visitors understand V3RII CRM, B2B, WMS, UTS and AQUA products, integrations such as Netsis/ERP, reporting, parametric setup, support and demo requests. If the visitor needs a formal support record, ask them to use the written support form in the widget. Do not invent prices or private customer data.
              """
            : """
              Sen V3RII ana web sitesi sesli destek asistanısın. Türkçe, net ve kısa konuş. Ziyaretçilere V3RII CRM, B2B, WMS, UTS ve AQUA ürünlerini; Netsis/ERP entegrasyonlarını, raporlama, parametrik yapı, destek ve demo taleplerini anlat. Resmi destek kaydı gerekirse kullanıcıyı widget içindeki yazılı destek formuna yönlendir. Fiyat veya özel müşteri bilgisi uydurma.
              """;
    }

    private static string? TryReadString(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static DateTimeOffset? TryReadUnixTime(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.TryGetInt64(out var unixTime) ? DateTimeOffset.FromUnixTimeSeconds(unixTime) : null;
    }
}
