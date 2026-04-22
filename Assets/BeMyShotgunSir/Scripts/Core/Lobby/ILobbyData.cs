using System;
using BeMyShotgunSir.Core;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public interface ILobbyData : IData
    {
        string LobbyIP { get; }
        int PlayerCount { get; }
        string[] PlayerNames { get; }
    }

    public interface ILobbyDataView : ILobbyData
    {
        Action OnLobbyIPChanged { get; set; }
        Action OnPlayerCountChanged { get; set; }
        Action OnPlayerNamesChanged { get; set; }
    }
}
