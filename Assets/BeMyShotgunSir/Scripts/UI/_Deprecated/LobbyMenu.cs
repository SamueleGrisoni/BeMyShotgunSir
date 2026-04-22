using BeMyShotgunSir.Scripts.Core;
using BeMyShotgunSir.Scripts.Core.Lobby;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BeMyShotgunSir.Scripts.UI
{
    public class LobbyMenu : MonoBehaviour
    {
        private const int _MAX_BIND_ATTEMPTS = 120;
        [SerializeField] private Button _quitButton;
        private Coroutine _bindRoutine;

        private void Awake()
        {
            _quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        private void OnEnable()
        {
            LobbyManager.OnLobbySpawned += OnLobbySpawned;

            if (TryBindLobbyMenu())
                return;

            _bindRoutine = StartCoroutine(BindWhenReady());
        }

        private void OnDisable()
        {
            LobbyManager.OnLobbySpawned -= OnLobbySpawned;

            if (_bindRoutine != null)
            {
                StopCoroutine(_bindRoutine);
                _bindRoutine = null;
            }
        }

        private void OnDestroy()
        {
            _quitButton.onClick.RemoveListener(OnQuitButtonClicked);
        }

        private void OnLobbySpawned(LobbyManager lobby)
        {
            if (lobby == null)
                return;

            lobby.UiMenu = this;

            if (_bindRoutine != null)
            {
                StopCoroutine(_bindRoutine);
                _bindRoutine = null;
            }
        }

        private IEnumerator BindWhenReady()
        {
            for (int attempt = 0; attempt < _MAX_BIND_ATTEMPTS && enabled; attempt++)
            {
                if (TryBindLobbyMenu())
                {
                    _bindRoutine = null;
                    yield break;
                }

                yield return null;
            }

            _bindRoutine = null;
            Debug.LogWarning("LobbyMenu: Unable to bind LobbyManager after retry attempts.", this);
        }

        private bool TryBindLobbyMenu()
        {
            if (GameServices.Instance == null || GameServices.Instance.LobbyManager == null)
                return false;

            GameServices.Instance.LobbyManager.UiMenu = this;
            return true;
        }

        private void OnQuitButtonClicked() =>
         GameServices.Instance.ConnectionManager.QuitLobby();
    }
}
