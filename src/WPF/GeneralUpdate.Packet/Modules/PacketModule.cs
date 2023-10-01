using GeneralUpdate.Packet.MVVM;
using GeneralUpdate.Packet.ViewModels;
using GeneralUpdate.Packet.Views;

namespace GeneralUpdate.Packet.Modules
{
    internal class PacketModule : IModule
    {
        private IView _view;

        public IView View => _view;

        public string Name => "Packet";

        public string UUID => "7C75A518-30D4-4AF8-B4E4-6B03DA40290E";

        public void Init()
        {
            _view = new PacketView()
            {
                DataContext = new PacketViewModel()
            };
        }
    }
}
