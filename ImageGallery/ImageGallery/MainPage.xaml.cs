using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using PeppermintCommon;

namespace ImageGallery
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
            CoreWindow.GetForCurrentThread().KeyUp += OnKeyUp;
            DoInit();
        }

        
        public async void DoInit()
        {
            var img = await _animatedGallery.OpenLastFolder();
            if (img == null) return;
            LoadAnimatedImage(img);

        }
        private readonly  AnimatedGallery _animatedGallery  = new AnimatedGallery(5);
        private readonly object _locker = new object();

        private bool IsLoading { get; set; }

        private async void LoadAnimatedImage(AwaitableLazy<AnimatedBitmap> bitmap)
        {
            if (bitmap == null) return;
            lock (_locker)
            {
                if (IsLoading) return;
                IsLoading = true;
                LoadingOverlay.Visibility = Visibility.Visible;
            }
            var file = await bitmap.Value;
            if (file == null)
            {
                lock (_locker)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    IsLoading = false;
                } 
                return;
            }
            Image.ImageSource = file;
            var page = txtTopMid.Text = _animatedGallery.ImageName;
            txtBottomMid.Text = _animatedGallery.GalleryName;
            txtBottomRight.Text = string.Format(@"{0}/{1}", _animatedGallery.ImageIndex, _animatedGallery.ImageCount);
            TopStrip.Visibility = Visibility.Visible;
            BottomStrip.Visibility = Visibility.Visible;
            lock (_locker)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                IsLoading = false;
            }
            await Task.Delay(TimeSpan.FromSeconds(5));
            lock (_locker)
            {
                if (page != _animatedGallery.ImageName) return;
            }
            TopStrip.Visibility = Visibility.Collapsed;
            BottomStrip.Visibility = Visibility.Collapsed;

        }

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var img = await _animatedGallery.OpenFolder();
            LoadAnimatedImage(img);
        }


        private void NextImage_OnClick(object sender, RoutedEventArgs e)
        {
            var img = _animatedGallery.NextImage();
            LoadAnimatedImage(img);
        }

        private void PrevImage_OnClick(object sender, RoutedEventArgs e)
        {
            var img = _animatedGallery.PrevImage();
            LoadAnimatedImage(img);
        }

        private void LastImage_OnClick(object sender, RoutedEventArgs e)
        {
            var img = _animatedGallery.LastImage();
            LoadAnimatedImage(img);
        }

        private void FirstImage_OnClick(object sender, RoutedEventArgs e)
        {
            var img = _animatedGallery.FirstImage();
            LoadAnimatedImage(img);
        }


        private void ImageGrid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var grid = sender as Grid;
            if (grid == null) return;
            var width = grid.ActualWidth;
            var tapx = e.GetPosition(grid).X;
            LoadAnimatedImage(tapx > width / 3d ? _animatedGallery.NextImage() : _animatedGallery.PrevImage());
        }
        private void OnKeyUp(CoreWindow sender, KeyEventArgs args)
        {
            lock (_locker)
            {
                if (IsLoading) return;
            }
            Func<AwaitableLazy<AnimatedBitmap>> method = null;
            switch (args.VirtualKey)
            {
                case VirtualKey.Left:
                    method = _animatedGallery.PrevImage;
                    break;
                case VirtualKey.Right:
                    method = _animatedGallery.NextImage;
                    break;
            }
            if (method == null) return;
            LoadAnimatedImage(method());
        }
    }
}
