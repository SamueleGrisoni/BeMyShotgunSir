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
    public abstract class NetController : NetworkBehaviour, INetController { }
}
