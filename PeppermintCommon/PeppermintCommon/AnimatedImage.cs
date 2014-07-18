using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace PeppermintCommon
{
    public class AnimatedImage : IEnumerable<Task<AnimatedImage.AnimatedFrame>>
    {
        public class AnimatedFrame
        {
            public readonly TimeSpan Delay;
            public readonly ImageSource Data;

            public AnimatedFrame(TimeSpan delay, ImageSource data)
            {
                Delay = delay;
                Data = data;
            }
        }

        private class Pixels
        {
            public byte[] Bytes;
        }
        private class FrameInfo
        {
            public TimeSpan Delay { get; set; }
            public uint Width { get; set; }
            public uint Height { get; set; }
            public uint Left { get; set; }
            public uint Top { get; set; }
        }
        
        public int Width { get { return _decoder != null ? (int)_decoder.OrientedPixelWidth : 0; } }
        public int Height { get { return _decoder != null ? (int)_decoder.OrientedPixelHeight : 0; }}
        public uint FrameCount {  get { return _decoder != null ? _decoder.FrameCount : 0; } }
        private BitmapDecoder _decoder;
        private IRandomAccessStream _source;
        private AnimatedImage() { }



        private async Task<bool> _Create(IStorageFile image)
        {
            _source = new InMemoryRandomAccessStream();
            using (var reader = await image.OpenStreamForReadAsync())
            {
                await reader.CopyToAsync(_source.AsStreamForWrite());
            }
            _decoder = await BitmapDecoder.CreateAsync(_source);
            return true;
        }

        public static async Task<AnimatedImage> Create(IStorageFile image)
        {
            var ret = new AnimatedImage();
            await ret._Create(image);
            return ret;
        }


        private async Task<AnimatedFrame> GetFrame(uint frame, Pixels previousFrame)
        {
            var frameData = await _decoder.GetFrameAsync(frame);
            var info = await GetFrameInfo(_decoder, frameData);
            if (previousFrame.Bytes == null)
            {
                _source.Seek(0);
                var img = new WriteableBitmap(Width,Height);
                await img.SetSourceAsync(_source);
                if (FrameCount > 1)
                {
                    previousFrame.Bytes = img.PixelBuffer.ToArray();
                }
                return new AnimatedFrame(info.Delay, img);
            }
            else
            {
                var bitmapTransform = new BitmapTransform { InterpolationMode = BitmapInterpolationMode.Cubic };
                var pixelProvider = await frameData.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                    bitmapTransform, ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.ColorManageToSRgb);
                var pixels = pixelProvider.DetachPixelData();
                pixels = MergeFrame(previousFrame.Bytes, pixels, info);
                previousFrame.Bytes = pixels;
                var img = new WriteableBitmap(Width, Height);
                img.PixelBuffer.AsStream().Write(pixels, 0, pixels.Length);
                return new AnimatedFrame(info.Delay, img);
            }
        }
        private byte[] MergeFrame(byte[] under, byte[] over, FrameInfo overInfo)
        {
            //TODO: There's got to be a better way to do this.
            // I've looked and nothing seems to be available for "metro" apps.
            var ret = new byte[under.Length];
            Array.Copy(under, ret, under.Length);
            Parallel.For(0, overInfo.Height - 1, h =>
            {
                var row = h + overInfo.Top;
                Parallel.For(0, overInfo.Width - 1, w =>
                {
                    var col = w + overInfo.Left;
                    var overindex = (h * overInfo.Width + w) * 4;
                    var underindex = (row * Width + col) * 4;

                    if (over[overindex + 3] != 255) return;
                    ret[underindex] = over[overindex];
                    ret[underindex + 1] = over[overindex + 1];
                    ret[underindex + 2] = over[overindex + 2];
                });
            });
            return ret;
        }
        public IEnumerator<Task<AnimatedFrame>> GetEnumerator()
        {
            var previous = new Pixels();
            for (var i = 0u; i < FrameCount; i += 1)
            {
                yield return GetFrame(i, previous);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region GetInfo
        private async static Task<FrameInfo> GetFrameInfo(BitmapDecoder decoder, IBitmapFrame frame)
        {
            var frameInfo = new FrameInfo
            {
                Delay = TimeSpan.FromMilliseconds(100),
                Width = frame.PixelWidth,
                Height = frame.PixelHeight,
                Left = 0,
                Top = 0
            };
            try
            {
                const string delayQuery = "/grctlext/Delay";
                const string widthQuery = "/imgdesc/Width";
                const string heightQuery = "/imgdesc/Height";
                const string leftQuery = "/imgdesc/Left";
                const string topQuery = "/imgdesc/Top";
                var delay = await GetQueryOrNull<ushort>(frame, delayQuery) ?? 10u;
                frameInfo.Delay = TimeSpan.FromMilliseconds(10 * ((delay >= 5) ? delay : 5));
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
        #endregion GetInfo

    }
}
