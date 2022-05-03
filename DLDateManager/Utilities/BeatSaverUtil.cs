using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DLDateManager.Utilities
{
    internal class BeatsaverBeatmap
    {
        public string id { get; set; }

        public string name { get; set; }

        public BeatsaverBeatmapVersion[]? versions { get; set; }
    }

    internal class BeatsaverBeatmapVersion
    {
        public string hash { get; set; }

        public string downloadURL { get; set; }

    }

    internal enum DownloadResponseErrorType
    {
        InvalidRequest,
        NotFound,
        TooManyRequests,
        ServerError,
        UnknownError,
    }

    internal class DownloadResponse
    {
        private DownloadResponseErrorType? _errorType = null;
        private string? _errorMessage;

        public DownloadResponse(DownloadResponseErrorType? errorType, string? errorMessage = null)
        {
            _errorType = errorType;
            _errorMessage = errorMessage;
        }

        public bool IsSucceeded
        {
            get { return _errorType == null; }
        }

        public DownloadResponseErrorType? ErrorType
        {
            get { return _errorType; }
        }

        public string? ErrorMessage { get { return _errorMessage; } }

        public string? DownloadedDirectory { get; set; }
    }


    internal static class BeatSaverUtil
    {
        private static AssemblyName asmName = Assembly.GetExecutingAssembly().GetName();

        public static async Task<(System.Net.HttpStatusCode, BeatsaverBeatmap?)> GetBeatmapByHash(HttpClient httpClient, string hash)
        {
            string requestUrl = $"https://api.beatsaver.com/maps/hash/{hash}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            Debug.WriteLine($"AssemblyName.Name: ${asmName.Name}");
            Debug.WriteLine($"AssemblyName.Version: ${asmName.Version}");
            request.Headers.Add("UserAgent", $"{asmName.Name}:{asmName.Version}");

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine("★failed.");
                return (response.StatusCode, null);
            }

            BeatsaverBeatmap? beatmap;
            using (var content = response.Content)
            using (var readStream = await content.ReadAsStreamAsync())
            {
                beatmap = await JsonSerializer.DeserializeAsync<BeatsaverBeatmap>(readStream);
            }
            return (System.Net.HttpStatusCode.OK, beatmap);
        }

        public static async Task<DownloadResponse> Download(HttpClient httpClient, string targetDir, Song song)
        {
            if (song.DirectoryName == null || song.Hash == null)
            {
                return new DownloadResponse(DownloadResponseErrorType.InvalidRequest);
            }

            try
            {
                (var statusCode, var beatmap) = await GetBeatmapByHash(httpClient, song.Hash);

                DownloadResponseErrorType? errorType;

                if (beatmap?.versions == null || beatmap.versions.Count() == 0)
                {
                    errorType = GetDownloadErrorType(statusCode);
                    return new DownloadResponse(errorType, statusCode.ToString());
                }

                string downloadUrl = beatmap.versions[0].downloadURL;

                string tempFile = Path.GetTempFileName();
                Debug.WriteLine(tempFile);

                var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);

                Debug.WriteLine($"AssemblyName.Name: ${asmName.Name}");
                Debug.WriteLine($"AssemblyName.Version: ${asmName.Version}");
                request.Headers.Add("UserAgent", $"{asmName.Name}:{asmName.Version}");

                // Content をメモリ上に保持してもたいしたサイズではないが一応 ResponseHeadersRead を指定
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("★failed.");
                    errorType = GetDownloadErrorType(statusCode);
                    return new DownloadResponse(errorType, statusCode.ToString());
                }

                using (var content = response.Content)
                using (var stream = await content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.CopyTo(fileStream);
                    fileStream.Flush();
                }

                string levelDir = Path.Combine(targetDir, song.DirectoryName);
                try
                {

                    // Zip 展開
                    ZipFile.ExtractToDirectory(tempFile, levelDir);
                }
                finally
                {
                    File.Delete(tempFile);
                }
                return new DownloadResponse(null)
                {
                    DownloadedDirectory = levelDir,
                };
            }
            catch (Exception ex)
            {
                return new DownloadResponse(DownloadResponseErrorType.UnknownError, ex.Message);
            }
        }

        private static DownloadResponseErrorType GetDownloadErrorType(System.Net.HttpStatusCode statusCode)
        {
            DownloadResponseErrorType errorType;
            switch (statusCode)
            {
                case System.Net.HttpStatusCode.BadRequest:
                case System.Net.HttpStatusCode.Unauthorized:
                case System.Net.HttpStatusCode.Forbidden:
                case System.Net.HttpStatusCode.MethodNotAllowed:
                    errorType = DownloadResponseErrorType.InvalidRequest;
                    break;
                case System.Net.HttpStatusCode.NotFound:
                    errorType = DownloadResponseErrorType.NotFound;
                    break;
                case System.Net.HttpStatusCode.TooManyRequests:
                    errorType = DownloadResponseErrorType.TooManyRequests;
                    break;
                case System.Net.HttpStatusCode.InternalServerError:
                case System.Net.HttpStatusCode.ServiceUnavailable:
                    errorType = DownloadResponseErrorType.ServerError;
                    break;
                default:
                    errorType = DownloadResponseErrorType.UnknownError;
                    break;
            }

            return errorType;
        }
    }
}
