using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace PeppermintCommon
{
    public class AnimatedBitmap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public List<AnimatedBitmapFrame> Frames { get; private set; }
        private AnimatedBitmap()
        {

        }

        public static async Task<AnimatedBitmap> Create(IRandomAccessStreamReference file)
        {
            var value = new AnimatedBitmap();
            value.Frames = await value.DecodeImage(file);
            return value;
        }

        private class FrameInfo
        {
            public TimeSpan Delay { get; set; }
            public FrameDisposalMethod DisposalMethod { get; set; }
            public uint Width { get; set; }
            public uint Height { get; set; }
            public uint Left { get; set; }
            public uint Top { get; set; }
        }

        #region Animation Decoding
        private async Task<List<AnimatedBitmapFrame>> DecodeImage(IRandomAccessStreamReference file)
        {
            var results = new List<AnimatedBitmapFrame>();
            using (var data = await file.OpenReadAsync())
            {
                var decoder = await BitmapDecoder.CreateAsync(data);
                Width = (int)decoder.OrientedPixelWidth;
                Height = (int)decoder.OrientedPixelHeight;
                byte[] first = null, prev = null;
                FrameInfo firstInfo = null;
                for (var i = 0u; i < decoder.FrameCount; i += 1)
                {
                    var frame = await decoder.GetFrameAsync(i);
                    var info = await GetFrameInfo(decoder, frame);
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
                    prev = cleanframe;

                    results.Add(new AnimatedBitmapFrame
                    {
                        Delay = info.Delay,
                        Frame = cleanframe
                    });
                }
                return results;
            }
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
            //TODO: There's got to be a better way to do this.
            // I've looked and nothing seems to be available for "metro" apps.
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
        private enum FrameDisposalMethod
        {
            Replace = 0,
            Combine = 1,
            // ReSharper disable once UnusedMember.Local
            RestoreBackground = 2, //Unhandled
            // ReSharper disable once UnusedMember.Local
            RestorePrevious = 3 //Unhandled
        }

        private async static Task<FrameInfo> GetFrameInfo(BitmapDecoder decoder, IBitmapFrame frame)
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
                var delay= await GetQueryOrNull<ushort>(frame, delayQuery) ?? 10u;
                frameInfo.Delay = TimeSpan.FromMilliseconds(10 * ((delay!=0)?delay:10));
                frameInfo.DisposalMethod = (FrameDisposalMethod)(await GetQueryOrNull<byte>(frame, disposalQuery) ?? 0);
                frameInfo.Width = await GetQueryOrNull<ushort>(frame, widthQuery) ?? frameInfo.Width;
                frameInfo.Height = await GetQueryOrNull<ushort>(frame, heightQuery) ?? frameInfo.Height;
                frameInfo.Left = await GetQueryOrNull<ushort>(frame, leftQuery) ?? frameInfo.Left;
                frameInfo.Top = await GetQueryOrNull<ushort>(frame, topQuery) ?? frameInfo.Top;
                if (frameInfo.Height + frameInfo.Top > decoder.OrientedPixelHeight)
                {
                    frameInfo.Top = decoder.OrientedPixelHeight - frameInfo.Height;
                }
                if (frameInfo.Left + frameInfo.Width > decoder.OrientedPixelWidth)
                {
                    frameInfo.Left = decoder.OrientedPixelWidth - frameInfo.Width;
                }
            }
            catch (NotSupportedException)
            {
            }
            return frameInfo;
        }

        private async static Task<T?> GetQueryOrNull<T>(IBitmapFrame frame, string query)
            where T : struct
        {
            var data = await frame.BitmapProperties.GetPropertiesAsync(new[] { query });
            if (!data.ContainsKey(query)) return null;
            var value = data[query].Value;
            if (value != null)
                return (T)value;
            return null;
        }
        #endregion Animation Decoding
    }

    public class AnimatedBitmapFrame
    {
        public TimeSpan Delay { get; internal set; }
        public byte[] Frame { get; internal set; }

    }


}
