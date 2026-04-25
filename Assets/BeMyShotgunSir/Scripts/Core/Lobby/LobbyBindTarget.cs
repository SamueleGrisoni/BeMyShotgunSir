using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public interface ILobbyBindTarget : IBindTarget
    {
        void BindLobbyCommand(LobbyCommand lobbyCommand);
        void BindLobbyDataView(ILobbyDataView lobbyData);
        void OnBindComplete();
    }

    public abstract class LobbyBindTarget : MonoBehaviour, ILobbyBindTarget
    {
        public abstract void BindLobbyCommand(LobbyCommand lobbyCommand);
        public abstract void BindLobbyDataView(ILobbyDataView lobbyData);
        public abstract void OnBindComplete();
    }
}
