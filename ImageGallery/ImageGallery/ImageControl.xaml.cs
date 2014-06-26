using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

namespace ImageGallery
{
    public sealed partial class ImageControl
    {

        public ImageControl()
        {
            InitializeComponent();
        }

        public uint FrameCount { get; private set; }
        private readonly List<WriteableBitmap> _frames = new List<WriteableBitmap>();

        public async void LoadImage(StorageFile file)
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
                    var bitmapTransform = new BitmapTransform();
                    var pixelProvider = await frame.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8, decoder.BitmapAlphaMode, 
                        bitmapTransform, ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.DoNotColorManage);
                    var pixels = pixelProvider.DetachPixelData();
                    target.PixelBuffer.AsStream().Write(pixels, 0, pixels.Length);
                    _frames.Add(target);
                }
            }
            BuildStoryBoard();
            PlaybackStoryboard.Begin();
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

            Storyboard.SetTarget(anim, Image);
            Storyboard.SetTargetProperty(anim, "Source");

            PlaybackStoryboard.Children.Add(anim);
        }
    }
}
