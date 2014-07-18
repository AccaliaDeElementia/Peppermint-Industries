using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using PeppermintCommon;

namespace PeppermintGallery
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
            CoreWindow.GetForCurrentThread().KeyUp += OnKeyUp;
            LoadImage(_animatedGallery.OpenLastFolder);
            Clock();
        }

        private async void LoadImage(Func<Task<AwaitableLazy<AnimatedImage>>> generator)
        {
            lock (_locker)
            {
                if (IsLoading) return;
                IsLoading = true;
                LoadingOverlay.Visibility = Visibility.Visible;
                Error.Visibility = Visibility.Collapsed;
            }
            try
            {
                var img = await generator();
                await LoadAnimatedImage(img);
            }
            catch (Exception e)
            {
#if DEBUG
                if (Debugger.IsAttached) Debugger.Break();
                ErrorText.Text = e.Message;
                Error.Visibility = Visibility.Visible;
#else
                throw;
#endif
            }
            finally
            {
                lock (_locker)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    IsLoading = false;
                }
            }
        }

        private readonly AnimatedGallery _animatedGallery = new AnimatedGallery(5);
        private readonly object _locker = new object();

        private bool IsLoading { get; set; }

        private async void Clock()
        {
            while (true)
            {
                txtTopRight.Text = string.Format("{0:t}", DateTime.Now);
                await Task.Delay(500);
            }
        }

        private async Task LoadAnimatedImage(AwaitableLazy<AnimatedImage> bitmap)
        {
            if (bitmap == null) return;
            var file = await bitmap.Value;
            if (file == null) return;
            await Image.SetImageSource(file);
            DoInfoBars();
        }

        private async void DoInfoBars()
        {
            var page = txtTopMid.Text = _animatedGallery.ImageName;
            txtBottomMid.Text = _animatedGallery.GalleryName;
            txtBottomRight.Text = string.Format(@"{0}/{1}", _animatedGallery.ImageIndex, _animatedGallery.ImageCount);
            TopStrip.Visibility = Visibility.Visible;
            BottomStrip.Visibility = Visibility.Visible;
            await Task.Delay(TimeSpan.FromSeconds(5));
            lock (_locker)
            {
                if (page != _animatedGallery.ImageName) return;
            }
            TopStrip.Visibility = Visibility.Collapsed;
            BottomStrip.Visibility = Visibility.Collapsed;
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            lock (_locker)
            {
                _playing = false;
            }
            LoadImage(_animatedGallery.OpenFolder);
        }


        private void NextImage_OnClick(object sender, RoutedEventArgs e)
        {
            lock (_locker)
            {
                _playing = false;
            }
            LoadImage(_animatedGallery.NextImage);
        }

        private void PrevImage_OnClick(object sender, RoutedEventArgs e)
        {
            lock (_locker)
            {
                _playing = false;
            }
            LoadImage(_animatedGallery.PrevImage);
        }

        private void LastImage_OnClick(object sender, RoutedEventArgs e)
        {
            lock (_locker)
            {
                _playing = false;
            }
            LoadImage(_animatedGallery.LastImage);
        }

        private void FirstImage_OnClick(object sender, RoutedEventArgs e)
        {
            lock (_locker)
            {
                _playing = false;
            }
            LoadImage(_animatedGallery.FirstImage);
        }


        private void ImageGrid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            lock (_locker)
            {
                if (_playing)
                {
                    _playing = false;
                    return;
                }
            }
            var grid = sender as Grid;
            if (grid == null) return;
            var width = grid.ActualWidth;
            var tapx = e.GetPosition(grid).X;
            if (tapx > width / 3d)
            {
                LoadImage(_animatedGallery.NextImage);
            }
            else
            {
                LoadImage(_animatedGallery.PrevImage);
            }
        }
        private void OnKeyUp(CoreWindow sender, KeyEventArgs args)
        {
            lock (_locker)
            {
                if (_playing)
                {
                    _playing = false;
                    return;
                }
            }
            Func<Task<AwaitableLazy<AnimatedImage>>> method = null;
            switch (args.VirtualKey)
            {
                case VirtualKey.Left:
                    method = _animatedGallery.PrevImage;
                    break;
                case VirtualKey.Right:
                case VirtualKey.Space:
                    method = _animatedGallery.NextImage;
                    break;
                case VirtualKey.Home:
                    method = _animatedGallery.FirstImage;
                    break;
                case VirtualKey.End:
                    method = _animatedGallery.LastImage;
                    break;
            }
            if (method == null) return;
            LoadImage(method);
        }

        private async void PlaySlideshow()
        {
            lock (_locker)
            {
                if (_playing) return;
                _playing = true;
            }
            var play = true;
            while (play)
            {
                await Task.Delay(15000);
                var img = await _animatedGallery.NextImage();
                await LoadAnimatedImage(img);
                bool next;
                lock (_locker)
                {
                    next = _playing;
                }
                play = next && await img.Value != null;
            }
        }
        private bool _playing;
        private void Slideshow_Click(object sender, RoutedEventArgs e)
        {
            PlaySlideshow();
        }
    }
}
