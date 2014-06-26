using System;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using PeppermintCommon;

namespace ImageGallery
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
        }
        private readonly Gallery _imagegallery = new Gallery();

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var img = await _imagegallery.OpenFolder();
            if (img == null) return;
            Image.LoadImage(img);
        }

        private void NextImage_OnClick(object sender, RoutedEventArgs e)
        {
            var img = _imagegallery.NextImage();
            if (img == null) return;
            Image.LoadImage(img);
        }
    }
}
