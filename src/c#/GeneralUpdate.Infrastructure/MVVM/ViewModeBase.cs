using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Infrastructure.MVVM
{
    public abstract class ViewModeBase : ObservableObject, IViewModel
    {
        public void Enter()
        {
            throw new NotImplementedException();
        }

        public void Exit()
        {
            throw new NotImplementedException();
        }
    }
}