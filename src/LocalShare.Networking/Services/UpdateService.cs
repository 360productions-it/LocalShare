using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using LocalShare.Common;
using LocalShare.Core.Interfaces;
using LocalShare.Core.Models;

namespace LocalShare.Networking.Services;

public class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    public const string DefaultUpdateManifestUrl = "https://raw.githubusercontent.com/360productions-it/LocalShare/main/dist/latest_version.json";

    public UpdateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public string CurrentVersion => AppVersionInfo.Version;

    public async Task<Result<UpdateInfo?>> CheckForUpdatesAsync(string? updateManifestUrl = null, CancellationToken cancellationToken = default)
    {
        var targetUrl = string.IsNullOrWhiteSpace(updateManifestUrl) ? DefaultUpdateManifestUrl : updateManifestUrl;

        try
        {
            var response = await _httpClient.GetAsync(targetUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result<UpdateInfo?>.Failure($"Unable to reach update server (HTTP {response.StatusCode}).");
            }

            var updateInfo = await response.Content.ReadFromJsonAsync<UpdateInfo>(cancellationToken: cancellationToken);
            if (updateInfo == null || string.IsNullOrWhiteSpace(updateInfo.Version))
            {
                return Result<UpdateInfo?>.Failure("Invalid update manifest format.");
            }

            if (TryParseVersion(updateInfo.Version, out var remoteVer) && TryParseVersion(CurrentVersion, out var currentVer))
            {
                if (remoteVer > currentVer)
                {
                    return Result<UpdateInfo?>.Success(updateInfo);
                }
            }

            return Result<UpdateInfo?>.Success(null); // Already up to date
        }
        catch (Exception ex)
        {
            return Result<UpdateInfo?>.Failure($"Update check error: {ex.Message}");
        }
    }

    public async Task<Result> DownloadAndApplyUpdateAsync(UpdateInfo updateInfo, Action<double>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
        {
            return Result.Failure("Download URL is missing in update info.");
        }

        try
        {
            var tempInstallerPath = Path.Combine(Path.GetTempPath(), "360LocalShare_Setup_Update.exe");

            using (var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(tempInstallerPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    totalBytesRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        double progressPercentage = (double)totalBytesRead / totalBytes * 100.0;
                        progressCallback?.Invoke(progressPercentage);
                    }
                }
            }

            // Execute the installer silently
            var startInfo = new ProcessStartInfo
            {
                FileName = tempInstallerPath,
                Arguments = "/SILENT /NORESTART",
                UseShellExecute = true
            };

            Process.Start(startInfo);

            // Shutdown the current running application instance cleanly
            Environment.Exit(0);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to download or apply update: {ex.Message}");
        }
    }

    private static bool TryParseVersion(string verString, out Version version)
    {
        var cleanVer = verString.TrimStart('v', 'V');
        return Version.TryParse(cleanVer, out version!);
    }
}
