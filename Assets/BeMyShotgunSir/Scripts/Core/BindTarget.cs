using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core
{
    public interface IBindTarget { }

    public interface IBindTarget<TCommand, TDataView> : IBindTarget
        where TCommand : Command
        where TDataView : IDataView
    {
        void BindCommand(TCommand command);
        void BindDataView(TDataView viewModel);
        void OnBindComplete();
    }

    public abstract class BindTarget<TCommand, TDataView> : MonoBehaviour, IBindTarget<TCommand, TDataView>
        where TCommand : Command
        where TDataView : IDataView
    {
        protected TCommand _command { get; private set; }
        protected TDataView _viewModel { get; private set; }
        public void BindCommand(TCommand command) => _command = command;
        public void BindDataView(TDataView viewModel) => _viewModel = viewModel;
        public abstract void OnBindComplete();
    }
}
