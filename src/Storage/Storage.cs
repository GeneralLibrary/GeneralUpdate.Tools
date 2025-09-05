using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace GeneralUpdate.Tool.Avalonia;

public class Storage
{
    private IStorageProvider? _storageProvider;
    private static Storage _instance;
    private static readonly object _lock = new();

    public static Storage Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new Storage();
                    }
                }
            }
            
            return _instance;
        }
    }

    private Storage()
    {
    }

    /// <summary>
    /// 打开文件
    /// </summary>
    /// <returns></returns>
    public async Task<IReadOnlyList<IStorageFile>> OpenFileDialog() => await _storageProvider!.OpenFilePickerAsync(new FilePickerOpenOptions()
    {
        Title = "Open File",
        FileTypeFilter = GetFileTypes(),
        AllowMultiple = true
    });

    /// <summary>
    /// 选择文件目录
    /// </summary>
    /// <returns></returns>
    public async Task<IReadOnlyList<IStorageFolder>> SelectFolderDialog() => await _storageProvider!.OpenFolderPickerAsync(new FolderPickerOpenOptions()
    {
        Title = "Select Folder",
        AllowMultiple = true,
    });

    /// <summary>
    /// 选择文件保存
    /// </summary>
    /// <returns></returns>
    public async Task<IStorageFile?> SaveFilePickerAsync(string suggestedFileName="") => await _storageProvider!.SaveFilePickerAsync(new FilePickerSaveOptions()
    {
        Title = "Save option",
        SuggestedFileName = suggestedFileName,
    });

    /// <summary>
    /// 初始化提供器
    /// </summary>
    /// <param name="visual"></param>
    public void SetStorageProvider(Visual visual)
    {
        var topLevel = TopLevel.GetTopLevel(visual);
        _storageProvider = topLevel?.StorageProvider;
    }

    private List<FilePickerFileType>? GetFileTypes()
    {
        return
        [
            FilePickerFileTypes.All,
            FilePickerFileTypes.TextPlain
        ];
    }
}