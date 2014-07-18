using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using PeppermintCommon.Annotations;

namespace PeppermintCommon
{
    public sealed partial class Image
    {
        public Image()
        {
            InitializeComponent();
        }

        private AnimatedImage _imageSource;

        public async Task SetImageSource(AnimatedImage value)
        {
                _imageSource = value;
                await BuildStoryBoard();
        }

        public event EventHandler ImageLoaded = (o, e) => { };
        public event EventHandler ImageLoading = (o, e) => { };

        private async Task BuildStoryBoard()
        {
            ImageLoading(this, EventArgs.Empty);
            PlaybackStoryboard.Stop();
            PlaybackStoryboard.Children.Clear();
            var anim = new ObjectAnimationUsingKeyFrames
            {
                BeginTime = TimeSpan.FromSeconds(0)
            };

            var ts = new TimeSpan();

            foreach (var frame in _imageSource)
            {
                var val = await frame;
                var keyFrame = new DiscreteObjectKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(ts),
                    Value = val.Data
                };

                ts = ts.Add(val.Delay);
                anim.KeyFrames.Add(keyFrame);
            }

            Storyboard.SetTarget(anim, TargetImage);
            Storyboard.SetTargetProperty(anim, "Source");

            PlaybackStoryboard.Children.Add(anim);
            PlaybackStoryboard.Begin();
            ImageLoaded(this, EventArgs.Empty);
        }
    }
}
