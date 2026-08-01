using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace ExpenseManager.App.Services;

[ComImport, Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDataTransferManagerInterop
{
    // Returns IntPtr (the raw ABI pointer), not DataTransferManager directly. Declaring the WinRT
    // type here compiles, but at runtime .NET's plain COM interop wraps the result as a generic
    // System.__ComObject instead of going through CsWinRT's projection, and the implicit cast to
    // DataTransferManager throws InvalidCastException. The pointer has to be marshaled explicitly
    // via WinRT.MarshalInterface<T>.FromAbi — see GetForWindow below.
    IntPtr GetForWindow([In] IntPtr appWindow, [In] ref Guid riid);
    void ShowShareUIForWindow(IntPtr appWindow);
}

/// <summary>Wraps the Windows Share contract (DataTransferManager) for a desktop app. Unpackaged
/// desktop apps can't call the UWP-era DataTransferManager.GetForCurrentView() — a
/// DataTransferManager has to be bound to a specific HWND via IDataTransferManagerInterop
/// instead. Same GUIDs as Microsoft's own WPF ShareSource sample
/// (Windows-classic-samples/Samples/ShareSource/wpf/DataTransferManagerHelper.cs), but that
/// sample predates CsWinRT and relies on .NET Framework's built-in WinRT interop to
/// auto-marshal the interop call's return value — that doesn't happen on .NET 5+, so this
/// version returns IntPtr and marshals explicitly via WinRT.MarshalInterface&lt;T&gt;.FromAbi.</summary>
public class ShareService
{
    private static readonly Guid DataTransferManagerIid = new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

    private string _title = "";
    private string _description = "";
    private string? _text;
    private StorageFile? _file;

    public void ShareText(IntPtr hwnd, string title, string description, string text)
    {
        _title = title;
        _description = description;
        _text = text;
        _file = null;
        ShowShareUI(hwnd);
    }

    public async Task ShareFileAsync(IntPtr hwnd, string filePath, string title, string description)
    {
        _title = title;
        _description = description;
        _text = null;
        _file = await StorageFile.GetFileFromPathAsync(filePath);
        ShowShareUI(hwnd);
    }

    private void ShowShareUI(IntPtr hwnd)
    {
        var dataTransferManager = GetForWindow(hwnd);
        dataTransferManager.DataRequested += OnDataRequested;
        ShowShareUIForWindow(hwnd);
    }

    private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
    {
        sender.DataRequested -= OnDataRequested;

        var data = args.Request.Data;
        data.Properties.Title = _title;
        data.Properties.Description = _description;

        if (_file is not null)
            data.SetStorageItems([_file]);
        else if (_text is not null)
            data.SetText(_text);
    }

    private static DataTransferManager GetForWindow(IntPtr hwnd)
    {
        var iid = DataTransferManagerIid;
        var interop = DataTransferManager.As<IDataTransferManagerInterop>();
        var ptr = interop.GetForWindow(hwnd, ref iid);
        return WinRT.MarshalInterface<DataTransferManager>.FromAbi(ptr);
    }

    private static void ShowShareUIForWindow(IntPtr hwnd) =>
        DataTransferManager.As<IDataTransferManagerInterop>().ShowShareUIForWindow(hwnd);

    public static void CopyTextToClipboard(string text)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }

    public static async Task CopyFileToClipboardAsync(string filePath)
    {
        var file = await StorageFile.GetFileFromPathAsync(filePath);
        var package = new DataPackage();
        package.SetStorageItems([file]);
        Clipboard.SetContent(package);
    }
}
