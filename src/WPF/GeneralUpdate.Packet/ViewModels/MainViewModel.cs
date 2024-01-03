using GeneralUpdate.Packet.Modules;
using GeneralUpdate.Packet.MVVM;
using System.Collections.ObjectModel;
using System.Windows;

namespace GeneralUpdate.Packet.ViewModels
{
    internal class MainViewModel : ViewModeBase
    {
        private ObservableCollection<IModule> _modules;
        private IModule _currentModule;

        public ObservableCollection<IModule> Modules
        { 
            get => _modules ?? (_modules = new ObservableCollection<IModule>());
        }

        public IModule CurrentModule 
        { 
            get => _currentModule; 
            set => SetProperty(ref _currentModule , value); 
        }

        internal MainViewModel() => InitModule();

        private void InitModule() 
        {
            Modules.Add(new PacketModule());
            Modules.Add(new OtherModule());
            Modules.Add(new HelperModule());
            foreach (var module in Modules)
            {
                module.Init();
            }
            CurrentModule = Modules.First();
        }
    }
}
