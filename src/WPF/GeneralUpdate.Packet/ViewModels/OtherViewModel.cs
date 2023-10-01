using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Packet.Models;
using GeneralUpdate.Packet.Modules;
using GeneralUpdate.Packet.MVVM;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;
using System.Windows;

namespace GeneralUpdate.Packet.ViewModels
{
    internal class OtherViewModel : ViewModeBase
    {
        private const string _jsonTemplateFileName = "version.json";
        private string _fileMD5;

        private AsyncRelayCommand getFileMD5Command;
        private AsyncRelayCommand buildJsonCommand;
        
        public AsyncRelayCommand BuildJsonCommand
        {
            get => buildJsonCommand ?? (buildJsonCommand = new AsyncRelayCommand(BuildJsonCallback));
        }

        public AsyncRelayCommand GetFileMD5Command 
        { 
            get => getFileMD5Command ?? (getFileMD5Command = new AsyncRelayCommand(GetFileMD5Callback)); 
        }

        public string FileMD5 
        { 
            get => _fileMD5;
            set => SetProperty(ref _fileMD5, value);
        }

        private async Task GetFileMD5Callback()
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
            FileMD5 = GetFileMD5(selectedFilePath);
        }

        private string GetFileMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }

        /// <summary>
        /// Build version template file (json).
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private async Task BuildJsonCallback()
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
            string path = Path.Combine(selectedFilePath, _jsonTemplateFileName);
            if (File.Exists(path)) await ShowMessage("Build options", "File already exists !");
            var jsonObj = new List<VersionTemplateModel>
            {
                new VersionTemplateModel(),
                new VersionTemplateModel(),
                new VersionTemplateModel()
            };
            await BuildVersionTemplate(path, jsonObj);
        }

        /// <summary>
        /// Generate a version information file template.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path">Generate file path.</param>
        /// <param name="content">Generate file content.</param>
        /// <returns></returns>
        private async Task BuildVersionTemplate<T>(string path, T content) where T : class
        {
            string json = JsonConvert.SerializeObject(content);
            await File.WriteAllTextAsync(path, json, System.Text.Encoding.UTF8);
            if (File.Exists(path)) await ShowMessage("Build options", "Generated successfully !");
        }

        private async Task ShowMessage(string title, string message)
        {
            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK);
            }));
        }
    }
}
