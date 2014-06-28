using System;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using PeppermintCommon;

namespace ImageGallery
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
            DoInit();
        }
        public async void DoInit(){
            var img = await _imagegallery.OpenLastFolder();
            if (img == null) return;
            Image.LoadImage(img);

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

        private void PrevImage_OnClick(object sender, RoutedEventArgs e)
        {
            var img = _imagegallery.PrevImage();
            if (img == null) return;
            Image.LoadImage(img);
        }

        private void LastImage_OnClick(object sender, RoutedEventArgs e)
        {
            var img = _imagegallery.LastImage();
            if (img == null) return;
            Image.LoadImage(img);
        }

        private void FirstImage_OnClick(object sender, RoutedEventArgs e)
        {
            var img = _imagegallery.FirstImage();
            if (img == null) return;
            Image.LoadImage(img);
        }

        private void Image_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var img = _imagegallery.NextImage();
            if (img == null) return;
            Image.LoadImage(img);
        }
    }
}
