using System;
using hitomiDownloader.Models;
using MahApps.Metro.Controls;

namespace hitomiDownloader
{
    /// <inheritdoc />
    /// <summary>
    /// GalleryViewer.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GalleryViewer : MetroWindow
    {
        public GalleryViewer(ImageManager manager)
        {
            DataContext = manager;
            InitializeComponent();
        }
        protected override void OnClosed(EventArgs e)
        {
            var im = DataContext as ImageManager;
            im.Release();
            base.OnClosed(e);
        }
    }
}
