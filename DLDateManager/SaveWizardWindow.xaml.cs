using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace DLDateManager
{
    /// <summary>
    /// SaveWizardWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SaveWizardWindow : Window
    {

        public SaveWizardWindow()
        {
            InitializeComponent();

            Uri uri = new Uri("/SaveWizardPage1.xaml", UriKind.Relative);
            frame.Source = uri;
        }
    }
}
