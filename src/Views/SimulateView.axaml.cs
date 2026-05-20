using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GeneralUpdate.Tools.Views;

public partial class SimulateView : UserControl
{
    public SimulateView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is ViewModels.SimulateViewModel vm)
        {
            PlatformCombo.SelectionChanged += (_, _) =>
            {
                if (PlatformCombo.SelectedIndex >= 0)
                    vm.Config.Platform = PlatformCombo.SelectedIndex == 1 ? 2 : 1;
            };
            AppTypeCombo.SelectionChanged += (_, _) =>
            {
                if (AppTypeCombo.SelectedIndex >= 0)
                    vm.Config.AppType = AppTypeCombo.SelectedIndex == 1 ? 2 : 1;
            };
        }
    }
}
