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
    internal class RestorePage3BindingSource : INotifyPropertyChanged
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

        private bool _isDownloadSongs = false;

        public bool IsDownloadSongs
        {
            get { return _isDownloadSongs; }
            set { _isDownloadSongs = value; OnPropertyChanged("IsDownloadSongs"); }
        }

        private string _titleText = "データを取得しています...";
        public string TitleText
        {
            get { return _titleText; }
            set { _titleText = value; OnPropertyChanged("TitleText"); }
        }

        private List<ErrorItem> _errorList = new();
        public List<ErrorItem> ErrorList
        {
            get { return _errorList; }
            set { _errorList = value; OnPropertyChanged("ErrorList"); }
        }
    }

    /// <summary>
    /// RestoreWizardPage3.xaml の相互作用ロジック
    /// </summary>
    public partial class RestoreWizardPage3 : Page
    {
        private RestorePage3BindingSource _bindingSource = new();

        private List<CustomLevelData>? _customLevelDataList = null;

        public RestoreWizardPage3()
        {
            InitializeComponent();

            Application.Current.Navigated += Application_Navigated;

            _bindingSource.SourceCountText = $"{AppModel.Instance.SongList?.Songs?.Count ?? 0} 件";
            _bindingSource.NotFoundCountText = $"{AppModel.Instance.NotFoundCount ?? 0} 件";
            _bindingSource.TargetDirectory = AppModel.Instance.TargetDirectory;

            DataContext = _bindingSource;
        }

        private void Application_Navigated(object sender, NavigationEventArgs e)
        {
            Application.Current.Navigated -= Application_Navigated;

            _bindingSource.TitleText = "復元処理を行います。";
            this.chkDownloadSongs.IsEnabled = true;
            this.btnCancel.IsEnabled = true;
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            AppModel.Instance.IsDownloadSongs = _bindingSource.IsDownloadSongs;
            var page4 = new RestoreWizardPage4();
            NavigationService.Navigate(page4);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            var parent = Window.GetWindow(this);
            parent.Close();
        }
    }
}
