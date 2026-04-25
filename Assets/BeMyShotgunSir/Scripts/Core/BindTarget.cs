using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public interface IBindTarget<TCommand, TDataView>
    {
        void BindCommand(TCommand command);
        void BindDataView(TDataView dataView);
        void OnBindComplete();
    }

    public abstract class BindTarget<TCommand, TDataView> : MonoBehaviour, IBindTarget<TCommand, TDataView>
    {
        protected TCommand _command { get; private set; }
        protected TDataView _dataView { get; private set; }
        public void BindCommand(TCommand command) => _command = command;
        public void BindDataView(TDataView dataView) => _dataView = dataView;
        public abstract void OnBindComplete();
    }
}
