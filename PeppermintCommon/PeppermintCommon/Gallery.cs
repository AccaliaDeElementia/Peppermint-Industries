using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace PeppermintCommon
{
    public class Gallery
    {
        private IEnumerable<StorageFile> Files;
        private StorageFile Current;
        public async Task<StorageFile> OpenFolder()
        {
            var picker = new FolderPicker();
            picker.CommitButtonText = "Open Folder";
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");
            var folder = await picker.PickSingleFolderAsync();
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("GalleryFolder", folder);
            var types = new[] { ".jpg", ".jpeg", ".png", ".gif", ".tif" };
            Files = from file in await folder.GetFilesAsync()
                        where types.Contains(file.FileType)
                        orderby MungeName(file.Name)
                        select file;
            return Current = Files.FirstOrDefault();
        }

        public StorageFile NextImage()
        {
            if (Files == null || !Files.Any() || Current == null) return null;
            var mname = MungeName(Current.Name);
            // ReSharper disable once StringCompareIsCultureSpecific.1
            return Current = Files.FirstOrDefault(file => string.Compare(MungeName(file.Name), mname) > 0);
        }

        private static readonly Regex MungePattern = new Regex(@"(\d+)|(\D+)");

        private static string MungeName(string name)
        {
            return string.Join("",
                from Match part in MungePattern.Matches(name)
                select part.Groups[1].Length>0?part.Value.PadLeft(20, '0'):part.Value);
        }
    }
}
