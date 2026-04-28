namespace BeMyShotgunSir.Scripts.Core
{
    public abstract class Binder<TCommand, TDataView, TBindTarget>
        where TCommand : Command
        where TDataView : IDataView
        where TBindTarget : IBindTarget<TCommand, TDataView>
    {
        protected readonly TCommand _command;
        protected readonly TDataView _dataView;

        protected Binder(TCommand command, TDataView dataView)
        {
            _command = command;
            _dataView = dataView;
        }

        public void Bind(TBindTarget[] targets)
        {
            BindCommand(targets);
            BindData(targets);
            CompleteBinding(targets);
        }

        protected virtual void BindCommand(TBindTarget[] bindTargets)
        {
            foreach (TBindTarget target in bindTargets)
            {
                target.BindCommand(_command);
            }
        }

        protected virtual void BindData(TBindTarget[] bindTargets)
        {
            foreach (TBindTarget target in bindTargets)
            {
                target.BindDataView(_dataView);
            }
        }

        protected virtual void CompleteBinding(TBindTarget[] bindTargets)
        {
            foreach (TBindTarget target in bindTargets)
            {
                target.OnBindComplete();
            }
        }
    }
}
