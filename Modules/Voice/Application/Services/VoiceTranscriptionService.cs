using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Infrastructure.Options;

namespace V3RII.Infrastructure.Services;

public sealed class VoiceTranscriptionService(
    IOptions<VoiceOptions> voiceOptions,
    IHostEnvironment hostEnvironment,
    ILogger<VoiceTranscriptionService> logger) : IVoiceTranscriptionService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/aac",
        "audio/mp4",
        "audio/mpeg",
        "audio/ogg",
        "audio/wav",
        "audio/webm",
        "video/mp4",
        "video/webm"
    };

    private readonly VoiceOptions _voiceOptions = voiceOptions.Value;

    public async Task<VoiceTranscriptionResultDto> TranscribeAsync(IFormFile audio, string? language, CancellationToken cancellationToken = default)
    {
        if (!_voiceOptions.TranscriptionEnabled ||
            !_voiceOptions.TranscriptionProvider.Equals("LocalProcess", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(_voiceOptions.TranscriptionExecutablePath))
        {
            return new VoiceTranscriptionResultDto(
                false,
                false,
                null,
                _voiceOptions.TranscriptionProvider,
                "Ses alındı fakat ücretsiz local transkripsiyon servisi API tarafında aktif değil.");
        }

        if (audio.Length <= 0)
        {
            return new VoiceTranscriptionResultDto(true, false, null, _voiceOptions.TranscriptionProvider, "Ses kaydı boş geldi.");
        }

        if (audio.Length > _voiceOptions.TranscriptionMaxFileBytes)
        {
            return new VoiceTranscriptionResultDto(true, false, null, _voiceOptions.TranscriptionProvider, "Ses kaydı çok uzun veya büyük.");
        }

        if (!AllowedContentTypes.Contains(audio.ContentType))
        {
            return new VoiceTranscriptionResultDto(true, false, null, _voiceOptions.TranscriptionProvider, "Ses formatı desteklenmiyor.");
        }

        var extension = ResolveExtension(audio.ContentType);
        var inputPath = Path.Combine(Path.GetTempPath(), $"v3rii-voice-{Guid.NewGuid():N}{extension}");

        try
        {
            await using (var stream = File.Create(inputPath))
            {
                await audio.CopyToAsync(stream, cancellationToken);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _voiceOptions.TranscriptionTimeoutSeconds)));

            var arguments = BuildArguments(inputPath, language);
            var modelCachePath = Path.Combine(hostEnvironment.ContentRootPath, ".whisper-cache");
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _voiceOptions.TranscriptionExecutablePath,
                    Arguments = arguments,
                    WorkingDirectory = hostEnvironment.ContentRootPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.Environment["HF_HOME"] = modelCachePath;
            process.StartInfo.Environment["HF_HUB_DISABLE_SYMLINKS_WARNING"] = "1";
            process.StartInfo.Environment["PYTHONIOENCODING"] = "utf-8";
            process.StartInfo.Environment["PYTHONUTF8"] = "1";

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);

            var output = (await outputTask).Trim();
            var error = (await errorTask).Trim();

            if (process.ExitCode != 0)
            {
                logger.LogWarning("Voice transcription process failed with exit code {ExitCode}. {Error}", process.ExitCode, error);
                return new VoiceTranscriptionResultDto(true, false, null, _voiceOptions.TranscriptionProvider, ResolveProcessErrorMessage(process.ExitCode, error));
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return new VoiceTranscriptionResultDto(true, false, null, _voiceOptions.TranscriptionProvider, "Konuşma algılanamadı.");
            }

            return new VoiceTranscriptionResultDto(true, true, output, _voiceOptions.TranscriptionProvider, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new VoiceTranscriptionResultDto(true, false, null, _voiceOptions.TranscriptionProvider, "Ses çözümleme zaman aşımına uğradı.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Voice transcription failed.");
            return new VoiceTranscriptionResultDto(true, false, null, _voiceOptions.TranscriptionProvider, "Ses motoru çalıştırılamadı. Sunucuda Python/faster-whisper kurulumunu kontrol edin.");
        }
        finally
        {
            TryDelete(inputPath);
        }
    }

    private string BuildArguments(string inputPath, string? language)
    {
        var normalizedLanguage = language?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true ? "en" : "tr";
        return _voiceOptions.TranscriptionArgumentsTemplate
            .Replace("{input}", inputPath, StringComparison.OrdinalIgnoreCase)
            .Replace("{language}", normalizedLanguage, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveProcessErrorMessage(int exitCode, string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            var compactError = error.Replace(Environment.NewLine, " ").Trim();
            if (compactError.Length > 260)
            {
                compactError = compactError[..260] + "...";
            }

            if (compactError.Contains("Access is denied", StringComparison.OrdinalIgnoreCase) ||
                compactError.Contains("Permission denied", StringComparison.OrdinalIgnoreCase))
            {
                return $"Ses motoru dosya iznine takıldı: {compactError}";
            }

            if (compactError.Contains("Unable to open file", StringComparison.OrdinalIgnoreCase) ||
                compactError.Contains("No such file", StringComparison.OrdinalIgnoreCase))
            {
                return $"Ses dosyası okunamadı: {compactError}";
            }

            if (compactError.Contains("model", StringComparison.OrdinalIgnoreCase) ||
                compactError.Contains("huggingface", StringComparison.OrdinalIgnoreCase) ||
                compactError.Contains("download", StringComparison.OrdinalIgnoreCase))
            {
                return $"Whisper model hatası: {compactError}";
            }

            return $"Ses motoru hata verdi: {compactError}";
        }

        if (exitCode == 12 || error.Contains("faster-whisper is not installed", StringComparison.OrdinalIgnoreCase))
        {
            return "Ses motoru kurulu değil. API sunucusunda Scripts/Voice/install-whisper.ps1 çalıştırılmalı.";
        }

        if (error.Contains("No module named", StringComparison.OrdinalIgnoreCase))
        {
            return "Ses motoru bağımlılıkları eksik. API sunucusunda faster-whisper kurulumu gerekli.";
        }

        return $"Ses motoru hata verdi. ExitCode={exitCode}";
    }

    private static string ResolveExtension(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "audio/mp4" or "video/mp4" => ".mp4",
            "audio/mpeg" => ".mp3",
            "audio/ogg" => ".ogg",
            "audio/wav" => ".wav",
            "audio/webm" or "video/webm" => ".webm",
            "audio/aac" => ".aac",
            _ => ".audio"
        };

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Temp cleanup failure should not affect the response.
        }
    }
}
