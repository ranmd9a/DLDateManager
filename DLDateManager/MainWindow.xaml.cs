using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            DataContext = AppModel.Instance;
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var win = new SaveWizardWindow();
            win.Owner = GetWindow(this);
            win.ShowDialog();
        }

        private async void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new MSAPI::Dialogs.CommonOpenFileDialog();
            if (dlg.ShowDialog() != MSAPI::Dialogs.CommonFileDialogResult.Ok)
            {
                return;
            }
            string sourceFile = dlg.FileName;

            try
            {
                SongList? list;
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                using (var stream = File.OpenRead(sourceFile))
                {
                    list = await JsonSerializer.DeserializeAsync<SongList>(stream, options);
                }
                if (list == null || list.Songs == null || list.Songs.Count == 0)
                {
                    MessageBox.Show("ファイルに曲情報が含まれていません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                AppModel.Instance.SongList = list;
                AppModel.Instance.SourceFile = sourceFile;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var win = new RestoreWizardWindow();
            win.Owner = GetWindow(this);
            win.ShowDialog();
        }
    }
}
