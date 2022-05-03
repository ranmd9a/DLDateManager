using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
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
using MSAPI = Microsoft.WindowsAPICodePack;

namespace DLDateManager
{
    internal class RestorePage1BindingSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged == null) return;
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        private string? _targetDirectory;
        public string? TargetDirectory
        {
            get { return _targetDirectory; }
            set { _targetDirectory = value; OnPropertyChanged("TargetDirectory"); OnPropertyChanged("IsTargetDirectorySelected"); }
        }

        public bool IsTargetDirectorySelected
        {
            get { return _targetDirectory != null; }
        }
    }

    /// <summary>
    /// RestoreWizardPage1.xaml の相互作用ロジック
    /// </summary>
    public partial class RestoreWizardPage1 : Page
    {
        private RestorePage1BindingSource _bindingSource = new();

        public RestoreWizardPage1()
        {
            InitializeComponent();

            string? installDir = AppModel.Instance.BeatSaberInstallDirectory;
            if (installDir != null)
            {
                _bindingSource.TargetDirectory = System.IO.Path.Combine(installDir, @"Beat Saber_Data\CustomLevels");
            }

            DataContext = _bindingSource;
        }

        private void btnFolderOpenSave_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new MSAPI::Dialogs.CommonOpenFileDialog()
            {
                InitialDirectory = _bindingSource.TargetDirectory ?? AppModel.Instance.LastTargetDirectory,
                AllowNonFileSystemItems = false,
                EnsurePathExists = true,
                IsFolderPicker = true,
            };
            if (dlg.ShowDialog() == MSAPI::Dialogs.CommonFileDialogResult.Ok)
            {
                (bool isValid, string? errorMessage) = ValidateFolder(dlg.FileName);
                if (!isValid)
                {
                    MessageBox.Show(errorMessage, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _bindingSource.TargetDirectory = dlg.FileName;
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            (bool isValid, string? errorMessage) = ValidateFolder(_bindingSource.TargetDirectory);
            if (!isValid)
            {
                MessageBox.Show(errorMessage, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SongList? songList = AppModel.Instance.SongList;
            if (songList == null || songList.Songs == null || songList.Songs.Count == 0)
            {
                // 基本的にありえないはず
                MessageBox.Show("参照元のデータがありません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AppModel.Instance.TargetDirectory = _bindingSource.TargetDirectory;
            AppModel.Instance.LastTargetDirectory = _bindingSource.TargetDirectory;

            var page2 = new RestoreWizardPage2();
            NavigationService.Navigate(page2);
        }

        private (bool, string?) ValidateFolder(string? folderName)
        {
            if (folderName == null)
            {
                return (false, "フォルダを選択してください。");
            }

            DirectoryInfo dirInfo = new(folderName);
            if (!dirInfo.Exists)
            {
                return (false, "フォルダが存在しません。");
            }

            if (dirInfo.Name != "CustomLevels")
            {
                return (false, "フォルダ名が CustomLevels ではありません。");
            }

            if (String.Compare(dirInfo.Root.FullName, dirInfo.FullName, true) == 0) {
                return (false, "ルートフォルダは選択できません。");
            }

            return (true, null);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            var parent = Window.GetWindow(this);
            parent.Close();
        }
    }

}
