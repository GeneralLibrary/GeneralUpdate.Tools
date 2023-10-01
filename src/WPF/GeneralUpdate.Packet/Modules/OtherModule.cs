using GeneralUpdate.Packet.MVVM;
using GeneralUpdate.Packet.ViewModels;
using GeneralUpdate.Packet.Views;

namespace GeneralUpdate.Packet.Modules
{
    internal class OtherModule : IModule
    {
        private IView _view;

        public IView View => _view;

        public string Name => "Other";

        public string UUID => "11EDEF73-6D73-42F7-82F8-EE39BBF336FD";

        public void Init()
        {
            _view = new OtherView()
            {
                DataContext = new OtherViewModel()
            };
        }
    }
}
