using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace PeppermintCommon
{
    public class Gallery
    {
        private List<StorageFile> _files;
        private StorageFile _current;
        private StorageFolder _folder;

        public string GalleryName { get { return _folder != null ? _folder.Name : @"[No Gallery]"; } }
        public string ImageName { get { return _current != null ? _current.Name : @"[No Image]"; } }
        public int ImageCount { get { return _files != null ? _files.Count() : 0; } }
        public int ImageIndex { get { return _files != null ? _files.Select(o=>o.Name).ToList().IndexOf(_current.Name) + 1 : 0; } }
        public async Task<StorageFile> OpenFolder()
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
            return await LoadDirectory(_folder);

        }
        
        public async Task<StorageFile> OpenLastFolder()
        {
            try
            {
                _folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("GalleryFolder");
                var first = await LoadDirectory(_folder);
                if (!StorageApplicationPermissions.FutureAccessList.ContainsItem("GalleryImage")) return first;
                var img = await StorageApplicationPermissions.FutureAccessList.GetFileAsync("GalleryImage");
                var ret = _files.Any(o => o.Name == img.Name) ? img : first;
                return _current = ret;
            }
            catch
            {
                return null;
            }
        }

        private async Task<StorageFile> LoadDirectory(IStorageFolder sf, IStorageItemProperties target=null)
        {
            var types = new[] { ".jpg", ".jpeg", ".png", ".gif", ".tif" };
            _files = (from file in await sf.GetFilesAsync()
                    where types.Contains(file.FileType)
                    orderby MungeName(file.Name)
                    select file).ToList();
            return _current = _files.FirstOrDefault(o=> target == null || o.FolderRelativeId == target.FolderRelativeId);
        }
        public StorageFile NextImage()
        {
            if (_files == null || !_files.Any() || _current == null) return null;
            var mname = MungeName(_current.Name);
            // ReSharper disable once StringCompareIsCultureSpecific.1
            var ret = _files.FirstOrDefault(file => string.Compare(MungeName(file.Name), mname) > 0);
            if (ret == null)  return null;
            _current = ret;
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryImage", _current);
            return _current;
        }
        public StorageFile PrevImage()
        {
            if (_files == null || !_files.Any() || _current == null) return null;
            var mname = MungeName(_current.Name);
            // ReSharper disable once StringCompareIsCultureSpecific.1
            var ret = _files.LastOrDefault(file => string.Compare(MungeName(file.Name), mname) < 0);
            if (ret == null) return null;
            _current = ret; 
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryImage", _current);
            return _current;
        }

        public StorageFile FirstImage()
        {
            if (_files == null || !_files.Any() || _current == null) return null;
            var ret = _files.FirstOrDefault();
            if (ret == null) return null;
            _current = ret; 
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryImage", _current);
            return _current;
        }

        public StorageFile LastImage()
        {
            if (_files == null || !_files.Any() || _current == null) return null;
            var ret = _files.FirstOrDefault();
            if (ret == null) return null;
            _current = ret; 
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryImage", _current);
            return _current;
        }

        private static readonly Regex MungePattern = new Regex(@"(\d+)|(\D+)");

        private static string MungeName(string name)
        {
            return string.Join("",
                from Match part in MungePattern.Matches(name)
                select part.Groups[1].Length > 0 ? part.Value.PadLeft(20, '0') : part.Value);
        }
    }
}
