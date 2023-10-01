using GeneralUpdate.Packet.MVVM;

namespace GeneralUpdate.Packet.Modules
{
    internal interface IModule
    {
        public IView View { get; }

        public string Name { get; }

        public string UUID { get; }

        public void Init();
    }
}
