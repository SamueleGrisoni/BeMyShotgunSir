namespace BeMyShotgunSir.Scripts.Core
{
    /// <summary>
    /// Base marker class for Commands. Commands are used by the UI to send user requests to the Network. <br/>
    /// </summary>
    public abstract class Command { }

    /// <summary>
    /// Base class for Commands. Commands are used by the UI to send user requests to the Network. <br/>
    /// </summary>
    ///  <typeparam name="T">
    /// The type of NetController this Command will interact with.
    /// </typeparam>
    public abstract class Command<T> : Command
        where T : NetController
    {
        protected readonly T _netController;
        protected Command(T netController)
        {
            _netController = netController;
        }
    }
}
