using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DLDateManager.Utilities;

namespace DLDateManager
{
    internal class RestoreResult
    {
        public bool Canceled { get; set; } = false;

        public int Processed { get; set; } = 0;

        public int Downloaded { get; set; } = 0;

        public int Skipped { get; set; } = 0;

        public int Failed { get; set; } = 0;

        public override string ToString()
        {
            return $"復元: {Processed}件  ダウンロード: {Downloaded}件  スキップ: {Skipped}件  失敗: {Failed}件";
        }
    }

    internal class RestorePage4BindingSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged == null) return;
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        private string _titleText = "情報を復元しています...";
        public string TitleText
        {
            get { return _titleText; }
            set { _titleText = value; OnPropertyChanged("TitleText"); }
        }

        private string _resultText = "復元: - 件  ダウンロード: - 件  スキップ: - 件  失敗: - 件";
        public string ResultText
        {
            get { return _resultText; }
            set { _resultText = value; OnPropertyChanged("ResultText"); }
        }

        private bool _aborted = false;
        public bool Aborted
        {
            get { return _aborted; }
            set { _aborted = value; OnPropertyChanged("Aborted"); }
        }

        private List<ErrorItem> _errorList = new();
        public List<ErrorItem> ErrorList
        {
            get { return _errorList; }
            set { _errorList = value; OnPropertyChanged("ErrorList"); }
        }
    }

    /// <summary>
    /// RestoreWizardPage4.xaml の相互作用ロジック
    /// </summary>
    public partial class RestoreWizardPage4 : Page
    {
        private RestorePage4BindingSource _bindingSource = new();

        private HttpClient _httpClient = new();

        private CancellationTokenSource? cancelSource;

        public RestoreWizardPage4()
        {
            InitializeComponent();

            Application.Current.Navigated += Application_Navigated;

            DataContext = _bindingSource;
        }

        private async void Application_Navigated(object sender, NavigationEventArgs e)
        {
            Debug.WriteLine("Application_Navigated");
            Debug.WriteLine(e.ExtraData);
            Application.Current.Navigated -= Application_Navigated;

            //btnClose.IsEnabled = false;
            _bindingSource.ErrorList.Clear();

            string? targetDirectory = AppModel.Instance.TargetDirectory;
            if (targetDirectory != null)
            {
                var progress = new Progress<ProgressData>(ShowProgress);
                Cursor = Cursors.Wait;
                try
                {
                    SongList songList = AppModel.Instance.SongList;
                    if (songList != null)
                    {
                        cancelSource = new();
                        btnAbort.IsEnabled = true;

                        var result = await Task.Run(() => Restore(songList, targetDirectory, cancelSource.Token, progress));

                        btnAbort.IsEnabled = false;

                        if (result.Canceled)
                        {
                            _bindingSource.TitleText = $"中断されました。[閉じる]ボタンを押して画面を閉じてください。";
                        }
                        else
                        {
                            _bindingSource.TitleText = $"復元しました。[閉じる]ボタンを押して画面を閉じてください。";
                            progressBar.Value = 100;
                        }
                        _bindingSource.ResultText = result.ToString();
                        btnClose.IsEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }

        private async Task<RestoreResult> Restore(SongList songList, string targetDirectory, CancellationToken token, IProgress<ProgressData>? progress)
        {
            RestoreResult result = new();

            if (songList != null)
            {
                int totalCount = songList.Songs.Count; // null にはならないはず

                (int processedCount, List<Song> notFoundList)
                    = await Task.Run(() => CustomLevelsUtil.RestoreDownloadDate(songList.Songs, targetDirectory, progress));
                result.Processed = processedCount;

                if (notFoundList.Count() > 0 && AppModel.Instance.IsDownloadSongs)
                {
                    (result.Downloaded, var canceled) = await DownloadAll(targetDirectory, totalCount, processedCount, notFoundList, token, progress);

                    if (canceled)
                    {
                        result.Canceled = true;
                    }
                }
                else
                {
                    result.Skipped = notFoundList.Count();
                }

                result.Failed = totalCount - result.Processed - result.Downloaded - result.Skipped;
            }

            return result;

        }

        private async Task<(int, bool)> DownloadAll(string targetDirectory, int totalCount, int processedCount, List<Song> notFoundList, CancellationToken token, IProgress<ProgressData>? progress = null)
        {
            int count = 0;
            bool canceled = false;
            foreach (Song song in notFoundList)
            {
                if (token.IsCancellationRequested)
                {
                    Debug.WriteLine("★Aborted");
                    canceled = true;
                    break;
                }

                var response = await BeatSaverUtil.Download(_httpClient, targetDirectory, song);
                if (response.IsSucceeded)
                {
                    // 成功時 response.DownloadedDirectory は非 null
                    CustomLevelsUtil.SetCreationTime(song, response.DownloadedDirectory);
                    count++;
                    progress?.Report(new ProgressData()
                    {
                        Percent = (processedCount + count) * 100 / totalCount,
                        DownloadedSongName = song.SongName,
                    });
                }
                else
                {
                    Debug.WriteLine("★失敗");
                }
            }
            return (count, canceled);
        }

        private void ShowProgress(ProgressData progressData)
        {
            progressBar.Value = progressData.Percent;
            _bindingSource.TitleText = $"情報を復元しています...{progressData.Percent}%";
            if (progressData.DownloadedSongName != null)
            {
                _bindingSource.ResultText = $"ダウンロード中...{progressData.DownloadedSongName}";
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            var parent = Window.GetWindow(this);
            parent.Close();
        }

        private void btnAbort_Click(object sender, RoutedEventArgs e)
        {
            //_bindingSource.Aborted = true;
            cancelSource?.Cancel();
            btnAbort.IsEnabled = false;
        }
    }
}
