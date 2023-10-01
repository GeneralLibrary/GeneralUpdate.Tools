using GeneralUpdate.Packet.MVVM;
using GeneralUpdate.Packet.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralUpdate.Packet.Modules
{
    internal class HelperModule : IModule
    {
        private IView _view;

        public IView View => _view;

        public string Name => "Helper";

        public string UUID => "2336B35E-EF78-4538-9415-3556DCE78AC8";

        public void Init()
        {
            _view = new HelperView();
        }
    }
}
