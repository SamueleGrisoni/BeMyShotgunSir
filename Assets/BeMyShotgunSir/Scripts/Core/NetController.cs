using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Object;

namespace BeMyShotgunSir.Scripts.Core
{

    /// <summary>
    /// Marker interface: NetController exposed to Command.
    /// </summary>
    public interface INetController_Command { }
    /// <summary>
    /// Marker interface: NetController exposed to Manager.
    /// </summary>
    public interface INetController_Manager { }
    /// <summary>
    /// Marker interface for NetControllers. NetControllers are NetworkBehaviours that are responsible for managing networked state and behavior for a specific aspect of the game (e.g. Lobby, Race).
    /// </summary>
    public interface INetController : INetController_Command, INetController_Manager { }
    /// <summary>
    /// Base class for NetControllers.
    /// </summary>
    public abstract class NetController : NetworkBehaviour, INetController
    {

        [TargetRpc]
        protected void LogMessage_TargetRpc(NetworkConnection connection, string message, int type = 0)
        {
            switch (type)
            {
                case 0:
                    Log.DLazy(() => $"Server message: {message}", this);
                    break;
                case 1:
                    Log.WLazy(() => $"Server warning: {message}", this);
                    break;
                case 2:
                    Log.ELazy(() => $"Server error: {message}", this);
                    break;
                default:
                    Log.DLazy(() => $"Server message: {message}", this);
                    break;
            }
        }
    }
}
