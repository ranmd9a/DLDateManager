using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using DLDateManager.Utilities;

namespace DLDateManager
{
    internal sealed class AppModel : INotifyPropertyChanged
    {
        private static AppModel _instance = new();

        public static AppModel Instance { get { return _instance; } }

        private AppModel()
        {
            try
            {
                string? installDir = RegistoryUtil.GetInstallDir();
                if (installDir != null)
                {
                    _beatsaberInstallDirectory = installDir;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged == null) return;
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        private string? _beatsaberInstallDirectory;
        public string? BeatSaberInstallDirectory
        {
            get { return _beatsaberInstallDirectory; }
            set { _beatsaberInstallDirectory = value; OnPropertyChanged("BeatSaberInstallDirectory"); }
        }

        private string? _sourceDirectory;
        public string? SourceDirectory
        {
            get { return _sourceDirectory; }
            set { _sourceDirectory = value; OnPropertyChanged("SourceDirectory"); }
        }

        private string? _lastSourceDirectory;
        public string? LastSourceDirectory
        {
            get { return _lastSourceDirectory; }
            set { _lastSourceDirectory = value; OnPropertyChanged("LastSourceDirectory"); }
        }

        private string? _sourceFile;
        public string? SourceFile
        {
            get { return _sourceFile; }
            set { _sourceFile = value; OnPropertyChanged("SourceFile"); }
        }

        private SongList _songList;

        public SongList SongList
        {
            get { return _songList; }
            set { _songList = value; OnPropertyChanged("SongList"); }
        }

        private string? _targetDirectory;
        public string? TargetDirectory
        {
            get { return _targetDirectory; }
            set { _targetDirectory = value; OnPropertyChanged("TargetDirectory"); }
        }

        private string? _lastTargetDirectory;
        public string? LastTargetDirectory
        {
            get { return _lastTargetDirectory; }
            set { _lastTargetDirectory = value; OnPropertyChanged("LastTargetDirectory"); }
        }

        private int? _notFoundCount;
        public int? NotFoundCount
        {
            get { return _notFoundCount; }
            set { _notFoundCount = value; OnPropertyChanged("NotFoundCount"); }
        }

        private bool _isDownloadSongs = false;

        public bool IsDownloadSongs
        {
            get { return _isDownloadSongs; }
            set { _isDownloadSongs = value; OnPropertyChanged("IsDownloadSongs"); }
        }
    }

    internal class SongInfo
    {
        [JsonPropertyName("_songName")]
        public string SongName { get; set; }

        [JsonPropertyName("_difficultyBeatmapSets")]
        public DifficultyBeatmapSet[] DifficultyBeatmapSets { get; set; }

    }

    internal class DifficultyBeatmapSet
    {
        [JsonPropertyName("_difficultyBeatmaps")]
        public DifficultyBeatmap[] DifficultyBeatmaps { get; set; }
    }

    internal class DifficultyBeatmap
    {
        [JsonPropertyName("_difficulty")]
        public string? Difficulty { get; set; }

        [JsonPropertyName("_beatmapFilename")]
        public string? BeatmapFilename { get; set; }
    }

    internal class CustomLevelData
    {
        public string DirectoryName { get; init; }

        public string FullName { get; init; }

        public FileInfo InfoFileInfo { get; init; }

        public SongInfo SongInfo { get; init; }

        public string Hash { get; init; }
    }

    internal class ErrorItem
    {
        public string FullName { get; init; }

        private string _directoryName;
        public string DirectoryName
        {
            get { return _directoryName; }
        }

        public string ErrorText { get; init; }

        public ErrorItem(string fullName, string errorText)
        {
            FullName = fullName;
            var dirInfo = new DirectoryInfo(FullName);
            _directoryName = dirInfo.Name;
            ErrorText = errorText;
        }
    }

    internal class DupItem
    {
        public string Hash { get; init; }

        private string _directoryName;
        public string DirectoryName
        {
            get { return _directoryName; }
        }

        public string FullName { get; init; }

        public DupItem(string hash, string fullName)
        {
            Hash = hash;
            FullName = fullName;
            var dirInfo = new DirectoryInfo(FullName);
            _directoryName = dirInfo.Name;
        }
    }

    internal class Song
    {
        public string? DirectoryName { get; set; }

        public string? SongName { get; set; }

        public string? Hash { get; set; }

        public string? CreationTime { get; set; }
    }

    internal class SongList
    {
        public string? PlaylistTitle { get; set; }

        public List<Song>? Songs { get; set; }
    }
}
