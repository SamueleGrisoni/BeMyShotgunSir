using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core
{
    /// <summary>
    /// Marker interface: Manager exposed to Bootstrapper.
    /// </summary>
    public interface IManager_Bootstrapper { }
    /// <summary>
    /// Marker interface: Manager exposed to NetController.
    /// </summary>
    public interface IManager_NetController { }
    /// <summary>
    /// Marker interface: Manager exposed to ConnectionManager.
    /// </summary>
    public interface IManager_ConnectionManager { }
    /// <summary>
    /// Marker interface for Managers.
    /// </summary>
    public interface IManager : IManager_Bootstrapper, IManager_NetController, IManager_ConnectionManager { }
    /// <summary>
    /// Marker Base class for Managers.
    /// </summary>
    public class Manager : MonoBehaviour, IManager { }
}
