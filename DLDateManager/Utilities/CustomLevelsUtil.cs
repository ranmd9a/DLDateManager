using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DLDateManager.Utilities
{
    internal class ProgressData
    {
        public int Percent { get; set; } = 0;

        public string? DownloadedSongName { get; set; }
    }


    internal static class CustomLevelsUtil
    {
        private static (string, SongInfo?, string?) GetCustomLevelHash(string customLevelPath)
        {
            List<byte> combinedBytes = new List<byte>();
            string infoDatPath = Path.Combine(customLevelPath, "info.dat");
            if (!File.Exists(infoDatPath))
            {
                Debug.WriteLine($"File not found: {infoDatPath}");
                return ("", null, "info.dat がありません。");
            }

            byte[] bytes = File.ReadAllBytes(infoDatPath);

            combinedBytes.AddRange(bytes);

            string infoDatText = Encoding.UTF8.GetString(bytes);
            SongInfo? infoData = System.Text.Json.JsonSerializer.Deserialize<SongInfo>(infoDatText);
            if (infoData != null)
            {
                for (int i = 0; i < infoData.DifficultyBeatmapSets.Length; i++)
                {
                    for (int i2 = 0; i2 < infoData.DifficultyBeatmapSets[i].DifficultyBeatmaps.Length; i2++)
                    {
                        string? beatmapFilename = infoData.DifficultyBeatmapSets[i].DifficultyBeatmaps[i2].BeatmapFilename;
                        if (beatmapFilename != null)
                        {
                            var beatmapPath = Path.Combine(customLevelPath, beatmapFilename);
                            Debug.WriteLine(beatmapPath);
                            if (File.Exists(beatmapPath))
                            {
                                combinedBytes.AddRange(File.ReadAllBytes(beatmapPath));
                            }
                        }
                    }
                }

            }

            string hash = CreateSha1FromBytes(combinedBytes.ToArray());
            return (hash, infoData, null);
        }

        public static string CreateSha1FromBytes(byte[] input)
        {
            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(input);

            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        public static (List<CustomLevelData>, List<ErrorItem>, bool) GetDirectories(string baseDir, CancellationToken token, bool nameOnly, IProgress<int>? progress = null)
        {
            List<ErrorItem> errorList = new();

            DirectoryInfo topDir = new DirectoryInfo(baseDir);

            DirectoryInfo[] dirInfos = topDir.GetDirectories();

            var result = new List<CustomLevelData>();
            int cnt = 0;
            int updateInterval = 0;
            int prePercent = 0;
            int totalCount = dirInfos.Length;
            bool canceled = false;
            foreach (var dirInfo in dirInfos)
            {
                Debug.WriteLine(dirInfo.FullName);
                if (token.IsCancellationRequested)
                {
                    Debug.WriteLine("★Aborted");
                    canceled = true;
                    return (result, errorList, canceled);
                    //break;
                }

                cnt++;
                updateInterval++;
                string hash = "";
                SongInfo? infoData = null;
                if (!nameOnly)
                {
                    (hash, infoData, var errorText) = GetCustomLevelHash(dirInfo.FullName);
                    if (hash == "" || infoData == null)
                    {
                        Debug.WriteLine($"hash or dirInfo is empty: {dirInfo.Name}");
                        errorList.Add(new ErrorItem(
                            dirInfo.FullName,
                            errorText
                        ));
                        continue;
                    }
                }
                if (progress != null)
                {
                    int percent = cnt * 100 / totalCount;
                    if (updateInterval > 100)
                    {
                        // 100件処理したら進捗通知
                        if (percent > prePercent)
                        {
                            // 進捗が増加している場合だけ通知
                            progress.Report(cnt * 100 / totalCount);
                            prePercent = percent;
                        }
                        updateInterval = 0;
                    }
                }

                result.Add(new CustomLevelData()
                {
                    DirectoryName = dirInfo.Name,
                    FullName = dirInfo.FullName,
                    InfoFileInfo = new FileInfo(Path.Combine(dirInfo.FullName, "info.dat")),
                    SongInfo = infoData,
                    Hash = hash,
                });
            }
            progress?.Report(100);
            return (result, errorList, canceled);
        }

        public static async Task SaveSongList(string outputFileName, string title, List<CustomLevelData> customLevelDataList)
        {
            var list = new SongList()
            {
                PlaylistTitle = title,
                Songs = new List<Song>(),
            };

            foreach (var customLevel in customLevelDataList)
            {
                list.Songs.Add(new Song()
                {
                    DirectoryName = customLevel.DirectoryName,
                    SongName = customLevel.SongInfo.SongName,
                    Hash = customLevel.Hash,
                    CreationTime = customLevel.InfoFileInfo.CreationTimeUtc.ToString("u"),
                });
            }
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            // なぜかシングルクォートが \u0027 に変換されて保存される。読み込んだ時にはちゃんと復元されるっぽいからまあよし。
            using (var stream = File.Create(outputFileName))
            {
                await JsonSerializer.SerializeAsync<SongList>(stream, list, options);
            }
            JsonSerializer.Serialize(list);
            return;
        }

        public static bool SetCreationTime(Song song, string levelDir)
        {
            if (DateTimeOffset.TryParse(song.CreationTime, out DateTimeOffset creationTime))
            {
                var dirInfo = new DirectoryInfo(levelDir);
                dirInfo.CreationTime = creationTime.ToLocalTime().DateTime;

                foreach (var file in dirInfo.GetFiles())
                {
                    file.CreationTime = creationTime.ToLocalTime().DateTime;
                }
                return true;
            }
            return false;
        }

        public static (int, List<Song>) RestoreDownloadDate(List<Song> songs, string targetDir, IProgress<ProgressData>? progress = null)
        {
            var notFoundList = new List<Song>();
            if (songs == null)
            {
                return (0, notFoundList);
            }

            int processedCount = 0;
            int totalCount = songs.Count;

            foreach (var song in songs)
            {
                if (song.DirectoryName == null || song.CreationTime == null)
                {
                    // 不正なデータ
                    continue;
                }
                string levelDir = Path.Combine(targetDir, song.DirectoryName);
                if (!Directory.Exists(levelDir))
                {
                    notFoundList.Add(song);
                    continue;
                }
                if (DateTimeOffset.TryParse(song.CreationTime, out DateTimeOffset creationTime))
                {
                    var dirInfo = new DirectoryInfo(levelDir);
                    dirInfo.CreationTime = creationTime.ToLocalTime().DateTime;

                    foreach (var file in dirInfo.GetFiles())
                    {
                        file.CreationTime = creationTime.ToLocalTime().DateTime;
                    }
                    processedCount++;
                    progress?.Report(
                        new ProgressData()
                        {
                            Percent = processedCount * 100 / totalCount
                        });
                }
            }
            return (processedCount, notFoundList);
        }
    }
}
