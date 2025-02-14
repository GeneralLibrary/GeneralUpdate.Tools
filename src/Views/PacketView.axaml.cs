﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GeneralUpdate.Tool.Avalonia.ViewModels;

namespace GeneralUpdate.Tool.Avalonia.Views;

public partial class PacketView : UserControl
{
    public PacketView()
    {
        InitializeComponent();
        Storage.Instance.SetStorageProvider(this);
        DataContext = new PacketViewModel();
    }
}