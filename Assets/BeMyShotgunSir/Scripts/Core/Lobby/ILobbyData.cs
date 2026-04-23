using System;
using BeMyShotgunSir.Core;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{

    public interface ILobbyNetData : INetData
    {
        string LobbyIP { get; }
        int PlayerCount { get; }
        string[] PlayerNames { get; }
    }

    public interface ILobbyData : IData
    {

    }

    public interface ILobbyDataView : ILobbyData, ILobbyNetData
    {
        event Action OnLobbyIPChanged;
        event Action OnPlayerCountChanged;
        event Action OnPlayerNamesChanged;
    }
}
