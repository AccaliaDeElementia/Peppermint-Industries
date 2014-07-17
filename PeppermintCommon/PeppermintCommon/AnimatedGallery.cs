using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace PeppermintCommon
{
    public class AnimatedGallery
    {
        private StorageFolder _folder;
        private List<StorageFile> _files;
        private int _current;
        private readonly int _queueLength;
        private  int _cachelength = 1;
        private readonly SortedDictionary<string, AwaitableLazy<IRandomAccessStream>> _cache =
            new SortedDictionary<string, AwaitableLazy<IRandomAccessStream>>();

        public AnimatedGallery(int queueLength = 5)
        {
            _queueLength = queueLength;
        }

        public string ImageName { get { return _files[_current].Name; } }
        public string GalleryName { get { return _folder.Name; } }
        public int ImageIndex { get { return _current + 1; } }
        public int ImageCount { get { return _files.Count; } }

        private async static Task<AwaitableLazy<AnimatedBitmap>> MakeBitmap(AwaitableLazy<IRandomAccessStream> stream )
        {
            var strm = await stream.Value;
            return new AwaitableLazy<AnimatedBitmap>(() => AnimatedBitmap.Create(strm));
        } 

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
            if (_folder == null) return null;
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryFolder", _folder);
            await LoadDirectory(_folder);
            UpdateCache();
            // don't enable cache until first image loaded.
            await _cache.First().Value.Value;
            _cachelength = _queueLength;
            UpdateCache();
            return await MakeBitmap(_cache.First().Value);
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
                UpdateCache();
                // don't enable cache until first image loaded.
                await _cache.First().Value.Value;
                _cachelength = _queueLength;
                UpdateCache();
                return await MakeBitmap(_cache.First().Value);
            }
            catch
            {
                return null;
            }
        }

        private async static Task<IRandomAccessStream> ReadFile(IStorageFile file)
        {
            var strm = new InMemoryRandomAccessStream();
            using (var reader = await file.OpenStreamForReadAsync())
            {
                await reader.CopyToAsync(strm.AsStreamForWrite());
            }
            return strm;
        }

        private void UpdateCache()
        {
            var self = MungeName(_files[_current].Name);
            //Ignoring this warning because otherwise comparison will be inconsistent with ordering
            // ReSharper disable once StringCompareIsCultureSpecific.1
            foreach (var kvp in _cache.Where(o => String.Compare(o.Key, self) < 0).ToList())
            {
                _cache.Remove(kvp.Key);
            }
            foreach (var file in _files.Skip(_current).Take(_queueLength).Where(o => !_cache.ContainsKey(o.Name)))
            {
                var tgtfile = file;
                var tgt = new AwaitableLazy<IRandomAccessStream>(()=>ReadFile(tgtfile));
                // Start the Lazy<> container finding a value, we don't want to wait on it though.
                // ReSharper disable once UnusedVariable
                var ignore = tgt.Value;
                _cache[MungeName(file.Name)] = tgt;
            }
            foreach (var kvp in _cache.Skip(_cachelength).ToList())
            {
                _cache.Remove(kvp.Key);
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

        #region Navigation
        // ReSharper disable once CSharpWarnings::CS1998
        public async Task<AwaitableLazy<AnimatedBitmap>> NextImage()
        {
            var nextIdx = _current + 1;
            if (_files == null || !_files.Any() || nextIdx >= _files.Count) return null;
            _current = nextIdx;
            UpdateCache();
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryImage", _files[_current]);
            return await MakeBitmap(_cache.First().Value);
        }

        // ReSharper disable once CSharpWarnings::CS1998
        public async Task<AwaitableLazy<AnimatedBitmap>> PrevImage()
        {
            var nextIdx = _current - 1;
            if (_files == null || !_files.Any() || nextIdx < 0) return null;

            _current = nextIdx;
            UpdateCache();
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryImage", _files[_current]);
            return await MakeBitmap(_cache.First().Value);
        }

        // ReSharper disable once CSharpWarnings::CS1998
        public async Task<AwaitableLazy<AnimatedBitmap>> LastImage()
        {
            _current = _files.Count - 1;
            UpdateCache();
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryImage", _files[_current]);
            return await MakeBitmap(_cache.First().Value);
        }

        // ReSharper disable once CSharpWarnings::CS1998
        public async Task<AwaitableLazy<AnimatedBitmap>> FirstImage()
        {
            _current = 0;
            UpdateCache();
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryImage", _files[_current]);
            return await MakeBitmap(_cache.First().Value);
        }
        #endregion Navigation

        #region Munger
        private static readonly Regex MungePattern = new Regex(@"(\d+)|(\D+)");

        private static string MungeName(string name)
        {
            return string.Join("",
                from Match part in MungePattern.Matches(name)
                select part.Groups[1].Length > 0 ? part.Value.PadLeft(20, '0') : part.Value);
        }
        #endregion Munger
    }
}
