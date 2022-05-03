using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using DLDateManager.Utilities;
using MSAPI = Microsoft.WindowsAPICodePack;

namespace DLDateManager
{

    internal class SavePage1BindingSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged == null) return;
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        private string? _sourceDirectory;
        public string? SourceDirectory
        {
            get { return _sourceDirectory; }
            set { _sourceDirectory = value; OnPropertyChanged("SourceDirectory"); OnPropertyChanged("IsSourceDirectorySelected"); }
        }

        public bool IsSourceDirectorySelected
        {
            get { return _sourceDirectory != null; }
        }
    }

    /// <summary>
    /// SaveWizardPage1.xaml の相互作用ロジック
    /// </summary>
    public partial class SaveWizardPage1 : Page
    {
        private SavePage1BindingSource _bindingSource = new();

        public string? BeatSaberInstallDirectory { get; set; }

        public SaveWizardPage1()
        {
            InitializeComponent();
            string? sourceDirectory = AppModel.Instance.LastSourceDirectory;
            if (sourceDirectory != null)
            {
                _bindingSource.SourceDirectory = sourceDirectory;
            }
            else
            {
                string? installDir = AppModel.Instance.BeatSaberInstallDirectory;
                if (installDir != null)
                {
                    _bindingSource.SourceDirectory = System.IO.Path.Combine(installDir, @"Beat Saber_Data\CustomLevels");
                }
            }

            DataContext = _bindingSource;
        }
        private void btnFolderOpenSave_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new MSAPI::Dialogs.CommonOpenFileDialog()
            {
                InitialDirectory = _bindingSource.SourceDirectory ?? AppModel.Instance.LastSourceDirectory,
                IsFolderPicker = true,
            };
            if (dlg.ShowDialog() == MSAPI::Dialogs.CommonFileDialogResult.Ok)
            {
                _bindingSource.SourceDirectory = dlg.FileName;
                //AppModel.Instance.SourceDirectory = dlg.FileName;
                // まだ確定していないから反映しない。
            }

        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_bindingSource.SourceDirectory == null)
            {
                MessageBox.Show("フォルダを選択してください。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AppModel.Instance.SourceDirectory = _bindingSource.SourceDirectory;
            AppModel.Instance.LastSourceDirectory = _bindingSource.SourceDirectory;

            var page2 = new SaveWizardPage2();
            NavigationService.Navigate(page2);
        }
    }
}
