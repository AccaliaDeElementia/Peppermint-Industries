using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace PeppermintCommon
{
    public class AnimatedGallery
    {
        private StorageFolder _folder;
        private List<StorageFile> _files;
        private int _current;
        private readonly int _queueLength;
        private readonly List<AwaitableLazy<AnimatedBitmap>> _nextList = new List<AwaitableLazy<AnimatedBitmap>>();

        public AnimatedGallery(int queueLength = 5)
        {
            _queueLength = queueLength;
        }

        public string ImageName { get { return _files[_current].Name; } }
        public string GalleryName { get { return _folder.Name; } }
        public int ImageIndex { get { return _current + 1; } }
        public int ImageCount { get { return _files.Count; } }

        public async Task<AwaitableLazy<AnimatedBitmap>> OpenFolder()
        {
            var picker = new FolderPicker
            {
                CommitButtonText = "Open Folder",
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.Downloads
            };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");
            _folder = await picker.PickSingleFolderAsync();
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryFolder", _folder);
            await LoadDirectory(_folder);
            FillQueue();
            return _nextList.FirstOrDefault();
        }

        public async Task<AwaitableLazy<AnimatedBitmap>> OpenLastFolder()
        {
            try
            {
                _folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("GalleryFolder");
                StorageFile tgt = null;
                if (StorageApplicationPermissions.FutureAccessList.ContainsItem("GalleryImage"))
                {
                    tgt = await StorageApplicationPermissions.FutureAccessList.GetFileAsync("GalleryImage");
                }
                await LoadDirectory(_folder, tgt);
                FillQueue();
                return _nextList.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private void FillQueue()
        {
            _nextList.Clear();
            foreach (var tgt in
                from itarget in _files.Skip(_current).Take(_queueLength)
                select new AwaitableLazy<AnimatedBitmap>(() => AnimatedBitmap.Create(itarget)) into tgt
                let ignore = tgt.Value
                select tgt)
            {
                _nextList.Add(tgt);
            }
        }

        private async Task LoadDirectory(IStorageFolder sf, IStorageItemProperties target = null)
        {
            var types = new[] { ".jpg", ".jpeg", ".png", ".gif", ".tif" };
            _files = (from file in await sf.GetFilesAsync()
                      where types.Contains(file.FileType)
                      orderby MungeName(file.Name)
                      select file).ToList();
            _current = 0;
            if (target != null)
            {
                _current = _files.FindIndex(a => a.DisplayName == target.DisplayName);
            }
        }

        public AwaitableLazy<AnimatedBitmap> NextImage()
        {
            var nextIdx = _current + 1;
            if (_files == null || !_files.Any() || nextIdx >= _files.Count) return null;
            _nextList.RemoveAt(0);
            if (nextIdx + _queueLength < _files.Count)
            {
                var tgtfile = _files[nextIdx + _queueLength];
                var tgt = new AwaitableLazy<AnimatedBitmap>(() => AnimatedBitmap.Create(tgtfile));
                var ignore = tgt.Value;
                _nextList.Add(tgt);
            }
            _current = nextIdx;
            
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryImage", _files[_current]);
            return _nextList[0];
        }

        public AwaitableLazy<AnimatedBitmap> PrevImage()
        {
            var nextIdx = _current - 1;
            if (_files == null || !_files.Any() || nextIdx < 0) return null;

            var tgtfile = _files[nextIdx];
            var tgt = new AwaitableLazy<AnimatedBitmap>(() => AnimatedBitmap.Create(tgtfile));
            var ignore = tgt.Value;
            _nextList.Insert(0, tgt);
            while (_nextList.Count > _queueLength)
            {
                _nextList.RemoveAt(_nextList.Count - 1);
            }
            _current = nextIdx;
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryImage", _files[_current]);
            return _nextList[0];
        }

        private static readonly Regex MungePattern = new Regex(@"(\d+)|(\D+)");

        private static string MungeName(string name)
        {
            return string.Join("",
                from Match part in MungePattern.Matches(name)
                select part.Groups[1].Length > 0 ? part.Value.PadLeft(20, '0') : part.Value);
        }

        public AwaitableLazy<AnimatedBitmap> LastImage()
        {
            var tgt = _nextList.Last();
            if (_current + _queueLength < _files.Count)
            {
                var tgtfile = _files.Last();
                tgt = new AwaitableLazy<AnimatedBitmap>(() => AnimatedBitmap.Create(tgtfile));
                var ignore = tgt.Value;
            }
            _nextList.Clear();
            _nextList.Insert(0, tgt);

            _current = _files.Count - 1;
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryImage", _files[_current]);
            return _nextList[0];
        }

        public AwaitableLazy<AnimatedBitmap> FirstImage()
        {
            _current = 0;
            FillQueue();
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryImage", _files[_current]);
            return _nextList[0];
        }
    }
}
