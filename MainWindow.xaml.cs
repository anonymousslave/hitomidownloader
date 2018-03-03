using System.Windows;
using hitomiDownloader.Core;
using MahApps.Metro.Controls.Dialogs;

namespace hitomiDownloader
{
    /// <inheritdoc />
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow
    {
        public Hitomi Hitomi { get; set; }
        public MainWindow()
        {

            Hitomi = new Hitomi();
            DataContext = Hitomi;
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Hitomi.InitializeGallery();
        }
    }
}
