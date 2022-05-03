using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    internal class RestorePage2BindingSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged == null) return;
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        private string _sourceCountText = "- 件";
        public string SourceCountText
        {
            get { return _sourceCountText; }
            set { _sourceCountText = value; OnPropertyChanged("SourceCountText"); }
        }

        private string _notFoundCountText = "- 件";
        public string NotFoundCountText
        {
            get { return _notFoundCountText; }
            set { _notFoundCountText = value; OnPropertyChanged("NotFoundCountText"); }
        }

        private string _targetDirectory = "";
        public string TargetDirectory
        {
            get { return _targetDirectory; }
            set { _targetDirectory = value; OnPropertyChanged("TargetDirectory"); }
        }

        private List<ErrorItem> _errorList = new();
        public List<ErrorItem> ErrorList
        {
            get { return _errorList; }
            set { _errorList = value; OnPropertyChanged("ErrorList"); }
        }
    }

    /// <summary>
    /// RestoreWizardPage2.xaml の相互作用ロジック
    /// </summary>
    public partial class RestoreWizardPage2 : Page
    {
        private RestorePage2BindingSource _bindingSource = new();

        private List<CustomLevelData>? _customLevelDataList = null;

        public RestoreWizardPage2()
        {
            InitializeComponent();

            Application.Current.Navigated += Application_Navigated;

            _bindingSource.TargetDirectory = AppModel.Instance.TargetDirectory;
        }

        private async void Application_Navigated(object sender, NavigationEventArgs e)
        {
            Application.Current.Navigated -= Application_Navigated;

            if (_bindingSource.TargetDirectory != null)
            {
                var progress = new Progress<int>(ShowProgress);
                Cursor = Cursors.Wait;
                try
                {
                    CancellationTokenSource cancelSource = new(); // 使ってない
                    // hash 計算省略 (フォルダ名のみ取得)
                    (_customLevelDataList, var errorList, var canceled)
                        = await Task.Run(() => CustomLevelsUtil.GetDirectories(_bindingSource.TargetDirectory, cancelSource.Token, true, progress));

                    if (errorList != null)
                    {
                        _bindingSource.ErrorList = errorList;
                    }

                    // restore 時は _customLevelDataList が 0 でもエラーではない。
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

                var directoryNameMap = ((IEnumerable<CustomLevelData>)_customLevelDataList)
                    .ToDictionary(k => k.DirectoryName.ToLower(), v => v);

                var notFoundList = AppModel.Instance.SongList?.Songs?
                    .Where((song) => {
                        if (song.DirectoryName == null)
                        {
                            // DirectoryName がないものは除外
                            return false;
                        }
                        if (directoryNameMap.ContainsKey(song.DirectoryName.ToLower()))
                        {
                            // CustomLevels 以下に同名フォルダが存在する
                            return false;
                        }
                        if (song.Hash == null || song.Hash == "")
                        {
                            // Hash がないものはダウンロードできないので対象にする。
                            return true;
                        }
                        //return !hashSet.Contains(song.Hash.ToUpper());
                        return true;
                    });
                AppModel.Instance.NotFoundCount = notFoundList.Count();

                var page3 = new RestoreWizardPage3();
                NavigationService.Navigate(page3);
            }
        }

        private void ShowProgress(int percent)
        {
            progressBar.Value = percent;
        }
    }

}
