using GeneralUpdate.Packet.MVVM;
using System.Diagnostics;
using System.Windows.Controls;

namespace GeneralUpdate.Packet.Views
{
    /// <summary>
    /// Interaction logic for HelperView.xaml
    /// </summary>
    public partial class HelperView : UserControl , IView
    {
        public HelperView()
        {
            InitializeComponent();
        }

        private void LblUrl_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            };
            Process.Start(psi);
            e.Handled = true;
        }
    }
}
