using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
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

        public event EventHandler ImageLoaded = (o, e) => { };

        public uint FrameCount { get; private set; }
        private readonly List<WriteableBitmap> _frames = new List<WriteableBitmap>();

        public async void LoadImage(IRandomAccessStreamReference file)
        {
            await DecodeImage(file);
            BuildStoryBoard();
            PlaybackStoryboard.Begin();

            ImageLoaded(this, EventArgs.Empty);
        }

        private async Task DecodeImage(IRandomAccessStreamReference file)
        {   
            _frames.Clear();
            using (var data = await file.OpenReadAsync())
            {
                var decoder = await BitmapDecoder.CreateAsync(data);

                FrameCount = decoder.FrameCount;
                for (var i = 0u; i < FrameCount; i += 1)
                {
                    var frame = await decoder.GetFrameAsync(i);
                    var target = new WriteableBitmap(
                        (int)decoder.OrientedPixelWidth, 
                        (int)decoder.OrientedPixelHeight);
                    
                    var bitmapTransform = new BitmapTransform {InterpolationMode = BitmapInterpolationMode.Cubic};
                    var pixelProvider = await frame.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, 
                        bitmapTransform, ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.ColorManageToSRgb);
                    var pixels = pixelProvider.DetachPixelData();
                    
                    target.PixelBuffer.AsStream().Write(pixels, 0, pixels.Length);
                    _frames.Add(target);
                }
            }
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
            var speed = TimeSpan.FromMilliseconds(100); // Standard GIF framerate 10 fps?

            foreach (var frame in _frames)
            {
                var keyFrame = new DiscreteObjectKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(ts),
                    Value = frame
                };

                ts = ts.Add(speed);
                anim.KeyFrames.Add(keyFrame);
            }

            Storyboard.SetTarget(anim, TargetImage);
            Storyboard.SetTargetProperty(anim, "Source");

            PlaybackStoryboard.Children.Add(anim);
        }
    }
}
