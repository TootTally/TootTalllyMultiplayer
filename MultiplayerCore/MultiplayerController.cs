using System;
using System.Collections.Generic;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyMultiplayer.APIService;
using TootTallyMultiplayer.MultiplayerPanels;
using UnityEngine;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer
{
    public class MultiplayerController
    {
        public PlaytestAnims GetInstance => CurrentInstance;
        public static PlaytestAnims CurrentInstance { get; private set; }

        private static List<MultiplayerLobbyInfo> _lobbyInfoList;
        private static MultiplayerLobbyInfo _currentLobby;

        private static MultiplayerSystem _multiConnection;

        private MultiplayerPanelBase _currentActivePanel, _lastPanel;
        private bool _isTransitioning;

        private MultiplayerMainPanel _multMainPanel;
        private MultiplayerLobbyPanel _multLobbyPanel;
        private MultiplayerCreatePanel _multCreatePanel;


        public bool IsUpdating;
        public bool IsConnectionPending, IsConnected;

        public MultiplayerController(PlaytestAnims __instance)
        {
            CurrentInstance = __instance;
            CurrentInstance.factpanel.gameObject.SetActive(false);

            GameObject canvasWindow = GameObject.Find("Canvas-Window").gameObject;
            Transform panelTransform = canvasWindow.transform.Find("Panel");

            var canvas = GameObject.Instantiate(AssetBundleManager.GetPrefab("multiplayercanvas"));

            try
            {
                _multMainPanel = new MultiplayerMainPanel(canvas, this);
            }
            catch (Exception)
            {
                Plugin.LogError("Couldn't init main panel");
            }
            try
            {
                _multLobbyPanel = new MultiplayerLobbyPanel(canvas, this);
            }
            catch (Exception)
            {
                Plugin.LogError("Couldn't init lobby panel");
            }
            try
            {
                _multCreatePanel = new MultiplayerCreatePanel(canvas, this);
            }
            catch (Exception)
            {
                Plugin.LogError("Couldn't init create panel");
            }

            _lobbyInfoList ??= new List<MultiplayerLobbyInfo>();
            _currentActivePanel = _multMainPanel;

            IsConnected = _multiConnection != null && _multiConnection.IsConnected;

            TootTallyAnimationManager.AddNewScaleAnimation(_multMainPanel.panel, Vector3.one, 1f, GetSecondDegreeAnimation(1.5f), sender => UpdateLobbyInfo(true));
        }

        public void OnSliderValueChangeScrollContainer(GameObject container, float value)
        {
            var gridPanelRect = container.GetComponent<RectTransform>();
            gridPanelRect.anchoredPosition = new Vector2(gridPanelRect.anchoredPosition.x, Mathf.Min(value * (_lobbyInfoList.Count - 7f) * 105f - 440f, (_lobbyInfoList.Count - 8f) * 105f + 74f - 440f)); //This is so scuffed I fucking love it
        }

        private IEnumerator<WaitForSeconds> DelayDisplayLobbyInfo(float delay, MultiplayerLobbyInfo lobby, Action<MultiplayerLobbyInfo> callback)
        {
            yield return new WaitForSeconds(delay);
            callback(lobby);
        }

        public void UpdateLobbyInfo(bool delay)
        {
            for (int i = 0; i < _lobbyInfoList.Count; i++)
            {
                if (delay)
                    Plugin.Instance.StartCoroutine(DelayDisplayLobbyInfo(i * .1f, _lobbyInfoList[i], _multMainPanel.DisplayLobby));
                else
                    _multMainPanel.DisplayLobby(_lobbyInfoList[i]);
            }
            _multMainPanel.UpdateScrolling(_lobbyInfoList.Count);
        }

        public void ConnectToLobby(string code)
        {
            if (_multiConnection != null && _multiConnection.ConnectionPending) return;

            _multiConnection?.Disconnect();
            Plugin.LogInfo("Connecting to " + code);
            IsConnectionPending = true;
            _multiConnection = new MultiplayerSystem(code, false) { OnWebSocketOpenCallback = delegate { IsConnected = true; } };
        }

        public void DisconnectFromLobby()
        {
            if (_multiConnection.IsConnected)
                _multiConnection.Disconnect();
            _currentLobby = null;
            IsConnected = IsConnectionPending = false;
            MultiplayerManager.UpdateMultiplayerState(MultiplayerState.Home);
            MoveToMain();
        }

        public void SendSongHashToLobby(string songHash) => _multiConnection?.SendSongHash(songHash);

        public void Update()
        {
            if (IsConnected && _multiConnection != null)
            {
                if (IsConnectionPending)
                    UpdateConnection();
                else
                    _multiConnection.UpdateStacks();
            }
        }

        public void UpdateConnection()
        {
            IsConnectionPending = false;
            TootTallyNotifManager.DisplayNotif("Connected to " + _multiConnection.GetServerID);
            MultiplayerManager.UpdateMultiplayerState(MultiplayerState.Lobby);
            OnLobbyConnectionSuccess();
        }

        public void OnLobbyConnectionSuccess()
        {
            MoveToLobby();
            RefreshAllLobbyInfo();
        }

        public void MoveToCreate()
        {
            TransitionToPanel(_multCreatePanel);
        }

        public void MoveToLobby()
        {
            TransitionToPanel(_multLobbyPanel);
        }

        public void MoveToMain()
        {
            TransitionToPanel(_multMainPanel);
        }

        public void ReturnToLastPanel()
        {
            TransitionToPanel(_lastPanel);
        }

        public void RefreshAllLobbyInfo()
        {
            if (IsUpdating) return;

            _multMainPanel.ClearAllLobby();
            IsUpdating = true;
            Plugin.Instance.StartCoroutine(MultiplayerAPIService.GetLobbyList(lobbyList =>
            {
                _lobbyInfoList = lobbyList;
                UpdateLobbyInfo(true);
                IsUpdating = false;
                if (IsConnected && _multiConnection != null)
                {
                    _currentLobby = _lobbyInfoList.Find(l => l.id == _multiConnection.GetServerID);
                    _multLobbyPanel.DisplayAllUserInfo(_currentLobby.players);
                }
                _multMainPanel.ShowRefreshLobbyButton();
            }));
        }

        public void CreateNewLobby(MultiplayerLobbyInfo lobbyInfo)
        {
            if (lobbyInfo == null) return;

            _lobbyInfoList.Add(lobbyInfo);
            _multiConnection = new MultiplayerSystem(lobbyInfo.id, true);
        }

        public void TransitionToPanel(MultiplayerPanelBase nextPanel)
        {
            if (_currentActivePanel == nextPanel || _isTransitioning) return;

            _isTransitioning = true;
            _lastPanel = _currentActivePanel;
            var positionOut = -nextPanel.GetPanelPosition;
            TootTallyAnimationManager.AddNewPositionAnimation(_currentActivePanel.panel, positionOut, 0.9f, new SecondDegreeDynamicsAnimation(1.5f, 0.89f, 1.1f), delegate { _lastPanel.panel.SetActive(false); _lastPanel.panel.GetComponent<RectTransform>().anchoredPosition = positionOut; });
            nextPanel.panel.SetActive(true);
            _currentActivePanel = nextPanel;
            TootTallyAnimationManager.AddNewPositionAnimation(nextPanel.panel, Vector2.zero, 0.9f, new SecondDegreeDynamicsAnimation(1.5f, 0.89f, 1.1f), sender => _isTransitioning = false);
        }

        public void TransitionToSongSelection()
        {
            MultiplayerManager.UpdateMultiplayerState(MultiplayerState.SelectSong);
        }

        public static SecondDegreeDynamicsAnimation GetSecondDegreeAnimation(float speedMult = 1f) => new SecondDegreeDynamicsAnimation(speedMult, 0.75f, 1.15f);

        public void Dispose()
        {
            CurrentInstance = null;
            _lobbyInfoList?.Clear();
            _multiConnection?.Disconnect();
        }

        public enum MultiplayerState
        {
            None,
            Enter,
            FirstTimePopUp,
            LoadPanels,
            Home,
            CreatingLobby,
            Lobby,
            Hosting,
            SelectSong,
            ExitScene,
        }

        public enum MultiplayerUserState
        {
            Spectating = -1,
            NotReady,
            Ready,
            Loading,
            Playing,
        }

    }
}
