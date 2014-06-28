using System;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        public async void DoInit()
        {
            var img = await _imagegallery.OpenLastFolder();
            if (img == null) return;
            LoadImage(img);

        }
        private readonly Gallery _imagegallery = new Gallery();

        private readonly object _locker = new object();

        private bool IsLoading { get; set; }
        private async void LoadImage(IRandomAccessStreamReference file)
        {
            if (file == null) return;
            lock (_locker)
            {
                if (IsLoading) return;
                IsLoading = true;
                LoadingOverlay.Visibility= Visibility.Visible;
            }
            await Image.LoadImage(file);
            var page = txtTopMid.Text = _imagegallery.ImageName;
            txtBottomMid.Text = _imagegallery.GalleryName;
            txtBottomRight.Text = string.Format(@"{0}/{1}", _imagegallery.ImageIndex, _imagegallery.ImageCount);
            TopStrip.Visibility = Visibility.Visible;
            BottomStrip.Visibility = Visibility.Visible;
            lock (_locker)
            {
                LoadingOverlay.Visibility= Visibility.Collapsed;
                IsLoading = false;
            }
            await Task.Delay(TimeSpan.FromSeconds(5));
            lock (_locker)
            {
                if (page != _imagegallery.ImageName) return;
            }
            TopStrip.Visibility = Visibility.Collapsed;
            BottomStrip.Visibility = Visibility.Collapsed;

        }

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var img = await _imagegallery.OpenFolder();
            LoadImage(img);
        }


        private void NextImage_OnClick(object sender, RoutedEventArgs e)
        {
            var img = _imagegallery.NextImage();
            LoadImage(img);
        }

        private void PrevImage_OnClick(object sender, RoutedEventArgs e)
        {
            var img = _imagegallery.PrevImage();
            LoadImage(img);
        }

        private void LastImage_OnClick(object sender, RoutedEventArgs e)
        {
            var img = _imagegallery.LastImage();
            LoadImage(img);
        }

        private void FirstImage_OnClick(object sender, RoutedEventArgs e)
        {
            var img = _imagegallery.FirstImage();
            LoadImage(img);
        }


        private void ImageGrid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var grid = sender as Grid;
            if (grid == null) return;
            var width = grid.ActualWidth;
            var tapx = e.GetPosition(grid).X;
            LoadImage(tapx > width / 3d ? _imagegallery.NextImage() : _imagegallery.PrevImage());
        }
    }
}
