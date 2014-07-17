using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

namespace PeppermintCommon
{
    public sealed partial class Image
    {
        public Image()
        {
            InitializeComponent();
        }

        private AnimatedBitmap _imageSource;

        public AnimatedBitmap ImageSource
        {
            get { return _imageSource; }
            set
            {
                _imageSource = value;
                DoLoadImage();
            }
        }

        
        public event EventHandler ImageLoaded = (o, e) => { };

        public async Task LoadImage(IRandomAccessStream file)
        {
            ImageSource = await AnimatedBitmap.Create(file);
        }

        private void DoLoadImage()
        {
            BuildStoryBoard();
            PlaybackStoryboard.Begin();
            ImageLoaded(this, EventArgs.Empty);
        }

        private void BuildStoryBoard()
        {
            PlaybackStoryboard.Stop();
            PlaybackStoryboard.Children.Clear();
            var anim = new ObjectAnimationUsingKeyFrames
            {
                BeginTime = TimeSpan.FromSeconds(0)
            };

            var ts = new TimeSpan();

            foreach (var frame in ImageSource.Frames)
            {
                var bitmap = new WriteableBitmap(ImageSource.Width, ImageSource.Height);
                bitmap.PixelBuffer.AsStream().Write(frame.Frame, 0, frame.Frame.Length);
                var keyFrame = new DiscreteObjectKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(ts),
                    Value = bitmap
                };

                ts = ts.Add(frame.Delay);
                anim.KeyFrames.Add(keyFrame);
            }

            Storyboard.SetTarget(anim, TargetImage);
            Storyboard.SetTargetProperty(anim, "Source");

            PlaybackStoryboard.Children.Add(anim);
        }
    }
}
