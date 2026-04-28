namespace BeMyShotgunSir.Scripts.Core
{
    /// <summary>
    /// Base interfaces for static data within a view model.
    /// </summary>
    public interface IData { }

    /// <summary>
    /// Base interface for network data within a view model. This data should map directly to the data shared over the network by a specific NetController, and should be designed to be easily serializable for network transmission.
    /// </summary>
    public interface INetData { }

    /// <summary>
    /// Base interface for a view model's data view, which combines both static and network data. This is the interface that views will interact with to get the data they need to display.
    /// </summary>
    public interface IDataView : IData, INetData { }
    /// <summary>
    /// Base class for view models in the application. A view model is responsible for holding and managing the data that a view displays.
    /// </summary>
    public abstract class ViewModel<TData, TNetData> : IDataView where TData : IData where TNetData : INetData
    {
        /// <summary>
        /// Initialize data and net data to default values. This is called when the view model is first created, and can be used to set up any necessary state or default values. If no parameter is passed, the view model should initialize to a default state.
        /// </summary>
        /// <param name="data"></param>
        public abstract void InitData(TData data = default);
        /// <summary>
        /// Initialize data from net data. This is called when the view model receives new data from the network, and updates the view model's state accordingly.
        /// </summary>
        /// <param name="data"></param>
        public abstract void InitNetData(TNetData data);

    }

}
