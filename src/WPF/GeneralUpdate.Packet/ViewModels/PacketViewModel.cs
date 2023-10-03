using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.AspNetCore.DTO;
using GeneralUpdate.Core.Utils;
using GeneralUpdate.Differential;
using GeneralUpdate.Packet.Domain.Enum;
using GeneralUpdate.Packet.Modules;
using GeneralUpdate.Packet.MVVM;
using GeneralUpdate.Packet.Servieces;
using GeneralUpdate.Zip.Factory;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;

namespace GeneralUpdate.Packet.ViewModels
{
    internal class PacketViewModel : ViewModeBase
    {
        #region Private Members

        private string sourcePath, targetPath, patchPath, infoMessage, url, packetName;
        private List<string> _formats, _encodings, _appTypes;
        private string _currentFormat, _currentEncoding, _currentAppType, _currentVersion, _currentClientAppKey;
        private bool isPublish;
        private AsyncRelayCommand buildCommand;
        private AsyncRelayCommand<string> selectFolderCommand;
        private PacketService _packetService;

        #endregion Private Members

        #region Constructors

        internal PacketViewModel()
        {
            IsPublish = false;
            CurrentEncoding = Encodings.First();
            CurrentFormat = Formats.First();
            CurrentAppType = AppTypes.First();
        }

        #endregion Constructors

        #region Public Properties

        public string SourcePath { get => sourcePath; set => SetProperty(ref sourcePath, value); }
        public string TargetPath { get => targetPath; set => SetProperty(ref targetPath, value); }
        public string PatchPath { get => patchPath; set => SetProperty(ref patchPath, value); }
        public string InfoMessage { get => infoMessage; set => SetProperty(ref infoMessage, value); }
        public bool IsPublish { get => isPublish; set => SetProperty(ref isPublish, value); }
        public string Url { get => url; set => SetProperty(ref url, value); }
        public string PacketName { get => packetName; set => SetProperty(ref packetName, value); }

        public AsyncRelayCommand<string> SelectFolderCommand
        {
            get => selectFolderCommand ?? (selectFolderCommand = new AsyncRelayCommand<string>(SelectFolderAction));
        }

        public AsyncRelayCommand BuildCommand
        {
            get => buildCommand ?? (buildCommand = new AsyncRelayCommand(BuildPacketCallback));
        }

        public List<string> AppTypes
        {
            get
            {
                if (_appTypes == null)
                {
                    _appTypes = new List<string>
                    {
                        "Client",
                        "Upgrade"
                    };
                }
                return _appTypes;
            }
        }

        public List<string> Formats
        {
            get
            {
                if (_formats == null)
                {
                    _formats = new List<string>
                    {
                        ".zip"
                    };
                }
                return _formats;
            }
        }

        public List<string> Encodings
        {
            get
            {
                if (_currentEncoding == null)
                {
                    _encodings = new List<string>
                    {
                        "Default",
                        "UTF8",
                        "UTF7",
                        "Unicode",
                        "UTF32",
                        "BigEndianUnicode",
                        "Latin1",
                        "ASCII"
                    };
                }
                return _encodings;
            }
        }

        public string CurrentFormat
        {
            get => _currentFormat;
            set => SetProperty(ref _currentFormat, value);
        }

        public string CurrentEncoding
        {
            get => _currentEncoding;
            set => SetProperty(ref _currentEncoding, value);
        }

        public string CurrentAppType
        {
            get => _currentAppType;
            set => SetProperty(ref _currentAppType, value);
        }

        public string CurrentVersion { get => _currentVersion; set => SetProperty(ref _currentVersion, value); }

        public string CurrentClientAppKey { get => _currentClientAppKey; set => SetProperty(ref _currentClientAppKey, value); }

        #endregion Public Properties

        #region Private Methods

        /// <summary>
        /// Choose a path
        /// </summary>
        /// <param name="value"></param>
        private async Task SelectFolderAction(string value)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = @"D:\";
            openFileDialog.Filter = "All files (*.*)|*.*";
            if (!openFileDialog.ShowDialog().Value)
            {
                await ShowMessage("Pick options", "No results were selected !");
                return;
            }

            string selectedFilePath = openFileDialog.FileName;
            switch (value)
            {
                case "Source":
                    SourcePath = selectedFilePath;
                    break;

                case "Target":
                    TargetPath = selectedFilePath;
                    break;

                case "Patch":
                    PatchPath = selectedFilePath;
                    break;
            }
        }

        /// <summary>
        ///  Build patch package
        /// </summary>
        private async Task BuildPacketCallback()
        {
            if (ValidationParameters())
            {
                await ShowMessage("Build options", "Required field not filled !");
                return;
            }

            if (ValidationFolder())
            {
                await ShowMessage("Build options", "Folder does not exist !");
                return;
            }

            try
            {
                await DifferentialCore.Instance.Clean(SourcePath, TargetPath, PatchPath, (sender, args) => { },
                    String2OperationType(CurrentFormat), String2Encoding(CurrentEncoding), PacketName);
                if (IsPublish)
                {
                    var packetPath = Path.Combine(TargetPath, $"{PacketName}{CurrentFormat}");
                    if (!File.Exists(packetPath))
                    {
                        await ShowMessage("Build options", $"The package was not found in the following path {packetPath} !");
                        return;
                    }
                    var md5 = FileUtil.GetFileMD5(packetPath);
                    await _packetService.PostUpgradePacket<UploadReapDTO>(Url, packetPath, String2AppType(CurrentAppType), CurrentVersion, CurrentClientAppKey, md5, async (resp) =>
                    {
                        if (resp == null)
                        {
                            await ShowMessage("Build options", "Upload failed !");
                            return;
                        }

                        if (resp.Code == HttpStatus.OK)
                        {
                            await ShowMessage("Build options", resp.Message);
                        }
                        else
                        {
                            await ShowMessage("Build options", resp.Body);
                        }
                    });
                }
                else
                {
                    await ShowMessage("Build options", "Build complete.");
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("Build options", $"Operation failed : {TargetPath} , Error : {ex.Message}  !");
            }
        }

        private bool ValidationParameters() => (string.IsNullOrEmpty(SourcePath) || string.IsNullOrEmpty(TargetPath) || string.IsNullOrEmpty(PatchPath) ||
            string.IsNullOrEmpty(PacketName) || string.IsNullOrEmpty(CurrentFormat) || string.IsNullOrEmpty(CurrentEncoding));

        private bool ValidationFolder() => (!Directory.Exists(SourcePath) || !Directory.Exists(TargetPath) || !Directory.Exists(PatchPath));

        private Encoding String2Encoding(string encoding)
        {
            Encoding result = null;
            switch (encoding)
            {
                case "Default":
                    result = Encoding.Default;
                    break;

                case "UTF8":
                    result = Encoding.UTF8;
                    break;

                case "UTF7":
                    result = Encoding.UTF7;
                    break;

                case "Unicode":
                    result = Encoding.Unicode;
                    break;

                case "UTF32":
                    result = Encoding.UTF32;
                    break;

                case "BigEndianUnicode":
                    result = Encoding.BigEndianUnicode;
                    break;

                case "Latin1":
                    result = Encoding.Latin1;
                    break;

                case "ASCII":
                    result = Encoding.ASCII;
                    break;
            }
            return result;
        }

        private OperationType String2OperationType(string type)
        {
            var result = Zip.Factory.OperationType.GZip;
            switch (type)
            {
                case "ZIP":
                    result = Zip.Factory.OperationType.GZip;
                    break;

                case "7Z":
                    result = Zip.Factory.OperationType.G7z;
                    break;
            }
            return result;
        }

        private int String2AppType(string appType)
        {
            int result = 0;
            switch (appType)
            {
                case "Client":
                    result = 1;
                    break;

                case "Upgrade":
                    result = 2;
                    break;
            }
            return result;
        }

        private async Task ShowMessage(string title,string message) 
        {
            await  Application.Current.Dispatcher.BeginInvoke(new Action(() => 
            { 
                 MessageBox.Show(message, title, MessageBoxButton.OK);
            }));
        }

        #endregion Private Methods
    }
}
