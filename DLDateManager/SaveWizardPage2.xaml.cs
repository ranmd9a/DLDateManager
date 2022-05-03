using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using MSAPI = Microsoft.WindowsAPICodePack;

namespace DLDateManager
{
    internal class SavePage2BindingSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged == null) return;
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        private string _titleText = "データを取得しています...";
        public string TitleText
        {
            get { return _titleText; }
            set { _titleText = value; OnPropertyChanged("TitleText"); }
        }

        private List<ErrorItem> _errorList = new();
        public List<ErrorItem> ErrorList {
            get { return _errorList; }
            set { _errorList = value; OnPropertyChanged("ErrorList"); }
        }

        private List<DupItem> _dupList = new();
        public List<DupItem> DupList
        {
            get { return _dupList; }
            set { _dupList = value; OnPropertyChanged("DupList"); }
        }
    }

    /// <summary>
    /// SaveWizardPage2.xaml の相互作用ロジック
    /// </summary>
    public partial class SaveWizardPage2 : Page
    {
        private SavePage2BindingSource _bindingSource = new();

        private List<CustomLevelData>? _customLevelDataList = null;

        private CancellationTokenSource? cancelSource;

        public SaveWizardPage2()
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

            btnSave.IsEnabled = false;
            _customLevelDataList = null;
            _bindingSource.ErrorList.Clear();

            string? sourceDirectory = AppModel.Instance.SourceDirectory;
            if (sourceDirectory != null)
            {
                var progress = new Progress<int>(ShowProgress);
                Cursor = Cursors.Wait;
                try
                {
                    cancelSource = new();
                    btnAbort.IsEnabled = true;
                    (_customLevelDataList, var errorList, var canceled)
                        = await Task.Run(() => CustomLevelsUtil.GetDirectories(sourceDirectory, cancelSource.Token, false, progress));

                    if (errorList != null)
                    {
                        _bindingSource.ErrorList = errorList;
                    }

                    if (canceled)
                    {
                        MessageBox.Show("処理を中断しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        var parent = Window.GetWindow(this);
                        parent.Close();
                        return;
                    }

                    if (_customLevelDataList == null || _customLevelDataList.Count == 0)
                    {
                        MessageBox.Show("曲が見つかりませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    List<DupItem> dupList = CreateDupListFromCustomLevelDataList();
                    _bindingSource.DupList = dupList;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                    btnAbort.IsEnabled = false;
                }

                btnSave.IsEnabled = true;
                btnDupCopy.IsEnabled = true;
                btnErrorCopy.IsEnabled = true;
                _bindingSource.TitleText = $"{_customLevelDataList.Count} 件取得しました。[保存して閉じる]ボタンを押して保存してください。";
            }
        }

        private List<DupItem> CreateDupListFromCustomLevelDataList()
        {
            var dupList = new List<DupItem>();

            var hashMap = new Dictionary<string, List<string>>();
            if (_customLevelDataList == null) return dupList;

            foreach (var item in _customLevelDataList)
            {
                if (item.Hash == null)
                {
                    continue;
                }
                var hash = item.Hash.ToUpper();
                List<string> list;
                if (hashMap.ContainsKey(hash))
                {
                    list = hashMap[hash];
                }
                else
                {
                    list = new List<string>();
                    hashMap.Add(hash, list);
                }
                list.Add(item.FullName);
            }

            foreach (var item in hashMap)
            {
                if (item.Value.Count > 1)
                {
                    // 2件以上
                    foreach (var fullName in item.Value)
                    {
                        dupList.Add(new DupItem(item.Key, fullName));
                    }
                }
            }

            return dupList;
        }

        private void ShowProgress(int percent)
        {
            progressBar.Value = percent;
            _bindingSource.TitleText = $"データを取得しています...{percent}%";
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_customLevelDataList == null)
            {
                return;
            }

            string nowString = DateTime.Now.ToString("yyyyMMdd-HHmm");
            string outputFileName = $"backup_{nowString}.clist";

            var saveDialog = new MSAPI::Dialogs.CommonSaveFileDialog();

            MSAPI.Dialogs.CommonFileDialogFilter filter1 = new MSAPI.Dialogs.CommonFileDialogFilter()
            {
                DisplayName = "CustomLevels情報ファイル",
            };
            filter1.Extensions.Add("clist");
            saveDialog.Filters.Add(filter1);
            saveDialog.DefaultFileName = outputFileName;

            if (saveDialog.ShowDialog() == MSAPI::Dialogs.CommonFileDialogResult.Ok)
            {
                string selectedFileName = saveDialog.FileName;
                string title = $"songlist {nowString}";

                Cursor = Cursors.Wait;
                try
                {
                    await CustomLevelsUtil.SaveSongList(selectedFileName, title, _customLevelDataList);

                    outputFileName = selectedFileName;
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

                MessageBox.Show($"保存しました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);

                // これだとアプリケーションが終了する。
                //var main = Application.Current.MainWindow;
                //main.Close();
                var parent = Window.GetWindow(this);
                parent.Close();
            }

            //AppModel.Instance.SourceDirectory = _bindingSource.SourceDirectory;
        }

        private void btnAbort_Click(object sender, RoutedEventArgs e)
        {
            cancelSource?.Cancel();
            btnAbort.IsEnabled = false;
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender == null) return;
                var item = sender as ListViewItem;
                if (item?.Content == null) return;

                if (item.Content is DupItem)
                {
                    var dupItem = item.Content as DupItem;
                    if (dupItem == null) return;

                    List<string> args = new List<string>();
                    args.Add(dupItem.FullName);
                    // エクスプローラで開く
                    Process.Start("EXPLORER.EXE", args);
                    return;
                }

                if (item.Content is ErrorItem)
                {
                    var errorItem = item.Content as ErrorItem;
                    if (errorItem == null) return;

                    List<string> args = new List<string>();
                    args.Add(@errorItem.FullName);
                    // エクスプローラで開く
                    Process.Start("EXPLORER.EXE", args);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnErrorCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new();
                foreach (var item in _bindingSource.ErrorList)
                {
                    string line = $"{item.FullName}\t{item.ErrorText}";
                    sb.AppendLine(line);
                }
                Clipboard.SetText(sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDupCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new();
                foreach (var item in _bindingSource.DupList)
                {
                    string line = $"{item.Hash}\t{item.FullName}";
                    sb.AppendLine(line);
                }
                Clipboard.SetText(sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
