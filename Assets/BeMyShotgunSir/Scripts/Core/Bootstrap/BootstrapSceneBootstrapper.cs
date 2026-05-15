using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Bootstrap
{
    public class BootstrapSceneBootstrapper : MonoBehaviour
    {
        private void Start() => GameServices.Instance.Channels.LoadingRequestEvent.RaiseEvent(null, true);
    }
}
