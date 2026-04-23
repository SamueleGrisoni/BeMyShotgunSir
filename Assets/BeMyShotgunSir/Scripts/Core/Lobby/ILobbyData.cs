using System;
using BeMyShotgunSir.Core;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{

    public interface ILobbyNetworkData : IData
    {
        string LobbyIP { get; }
        int PlayerCount { get; }
        string[] PlayerNames { get; }
    }
    public interface ILobbyData : ILobbyNetworkData
    {

    }

    public interface ILobbyDataView : ILobbyData
    {
        event Action OnLobbyIPChanged;
        event Action OnPlayerCountChanged;
        event Action OnPlayerNamesChanged;
    }
}
