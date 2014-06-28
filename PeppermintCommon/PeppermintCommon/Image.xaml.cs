using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        private readonly List<FrameInfo> _frames = new List<FrameInfo>();

        public async Task LoadImage(IRandomAccessStreamReference file)
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
                byte[] first = null, prev=null;
                FrameInfo firstInfo = null;
                for (var i = 0u; i < FrameCount; i += 1)
                {
                    var frame = await decoder.GetFrameAsync(i);
                    var target = new WriteableBitmap(
                        (int)decoder.OrientedPixelWidth,
                        (int)decoder.OrientedPixelHeight);
                    var info = await GetFrameInfo(frame);
                    var bitmapTransform = new BitmapTransform { InterpolationMode = BitmapInterpolationMode.Cubic };
                    var pixelProvider = await frame.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                        bitmapTransform, ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.ColorManageToSRgb);
                    var pixels = pixelProvider.DetachPixelData();
                    if (first == null)
                    {
                        first = pixels;
                        firstInfo = info;
                        prev = pixels;
                    }
                    var cleanframe = MakeFrame(first, firstInfo, pixels, info, prev, firstInfo);
                    target.PixelBuffer.AsStream().Write(cleanframe, 0, cleanframe.Length);
                    prev = cleanframe;

                    info.Data = target;
                    _frames.Add(info);

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

            foreach (var frame in _frames)
            {
                var keyFrame = new DiscreteObjectKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(ts),
                    Value = frame.Data
                };

                ts = ts.Add(frame.Delay);
                anim.KeyFrames.Add(keyFrame);
            }

            Storyboard.SetTarget(anim, TargetImage);
            Storyboard.SetTargetProperty(anim, "Source");

            PlaybackStoryboard.Children.Add(anim);
        }

        private static byte[] MakeFrame(
            byte[] fullImage, FrameInfo fullInfo,
            byte[] rawFrame, FrameInfo frameInfo,
            byte[] previousFrame, FrameInfo previousFrameInfo)
        {
            if (previousFrameInfo != null && previousFrame != null &&
                    previousFrameInfo.DisposalMethod == FrameDisposalMethod.Combine)
            {
                return MergeFrame(previousFrame, previousFrameInfo, rawFrame, frameInfo);
            }
            return MergeFrame(fullImage, fullInfo, rawFrame, frameInfo);
        }

        private static byte[] MergeFrame(byte[] under, FrameInfo underInfo, byte[] over, FrameInfo overInfo)
        {
            var ret = new byte[under.Length];
            Array.Copy(under, ret, under.Length);
            for (var h = 0; h < overInfo.Height; h += 1)
            {
                var row = h + overInfo.Top;
                for (var w = 0; w < overInfo.Width; w += 1)
                {
                    var col = w + overInfo.Left;
                    var overindex = (h * overInfo.Width + w) * 4;
                    var underindex = (row * underInfo.Width + col) * 4;

                    if (over[overindex + 3] != 255) continue;
                    ret[underindex] = over[overindex];
                    ret[underindex + 1] = over[overindex + 1];
                    ret[underindex + 2] = over[overindex + 2];
                }
            }
            return ret;
        }

        private class FrameInfo
        {
            public TimeSpan Delay { get; set; }
            public FrameDisposalMethod DisposalMethod { get; set; }
            public uint Width { get; set; }
            public uint Height { get; set; }
            public uint Left { get; set; }
            public uint Top { get; set; }
            public WriteableBitmap Data { get; set; }
        }

        private enum FrameDisposalMethod
        {
            Replace = 0,
            Combine = 1,
            // ReSharper disable once UnusedMember.Local
            RestoreBackground = 2, //Unhandled
            // ReSharper disable once UnusedMember.Local
            RestorePrevious = 3 //Unhandled
        }


        private async static Task<FrameInfo> GetFrameInfo(BitmapFrame frame)
        {
            var frameInfo = new FrameInfo
            {
                Delay = TimeSpan.FromMilliseconds(100),
                DisposalMethod = FrameDisposalMethod.Replace,
                Width = frame.PixelWidth,
                Height = frame.PixelHeight,
                Left = 0,
                Top = 0
            };


            try
            {
                const string delayQuery = "/grctlext/Delay";
                const string disposalQuery = "/grctlext/Disposal";
                const string widthQuery = "/imgdesc/Width";
                const string heightQuery = "/imgdesc/Height";
                const string leftQuery = "/imgdesc/Left";
                const string topQuery = "/imgdesc/Top";

                var delay = await GetQueryOrNull<ushort>(frame, delayQuery);
                if (delay.HasValue)
                    frameInfo.Delay = TimeSpan.FromMilliseconds(10 * delay.Value);

                var disposal = await GetQueryOrNull<byte>(frame, disposalQuery);
                if (disposal.HasValue)
                    frameInfo.DisposalMethod = (FrameDisposalMethod)disposal.Value;

                uint? width = await GetQueryOrNull<ushort>(frame, widthQuery);
                if (width.HasValue)
                    frameInfo.Width = width.Value;

                var height = await GetQueryOrNull<ushort>(frame, heightQuery);
                if (height.HasValue)
                    frameInfo.Height = height.Value;

                var left = await GetQueryOrNull<ushort>(frame, leftQuery);
                if (left.HasValue)
                    frameInfo.Left = left.Value;

                var top = await GetQueryOrNull<ushort>(frame, topQuery);
                if (top.HasValue)
                    frameInfo.Top = top.Value;
            }
            catch (NotSupportedException)
            {
            }

            return frameInfo;
        }

        private async static Task<T?> GetQueryOrNull<T>(BitmapFrame frame, string query)
            where T : struct
        {
            var data = await frame.BitmapProperties.GetPropertiesAsync(new[] { query });

            if (!data.ContainsKey(query)) return null;
            var value = data[query].Value;
            if (value != null)
                return (T)value;
            return null;
        }

    }
}
