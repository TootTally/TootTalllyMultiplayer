using BaboonAPI.Hooks.Tracks;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyGameModifiers;
using TootTallyLeaderboard.Replays;
using TootTallyMultiplayer.APIService;
using TootTallyMultiplayer.MultiplayerPanels;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using static Rewired.UI.ControlMapper.ControlMapper;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;
using static TootTallyMultiplayer.MultiplayerSystem;

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
        private bool _hasSong;

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

            TootTallyAnimationManager.AddNewScaleAnimation(_multMainPanel.panel, Vector3.one, 1f, GetSecondDegreeAnimation(1.5f), sender => RefreshAllLobbyInfo());
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
            _multiConnection = new MultiplayerSystem(code, false)
            {
                OnWebSocketOpenCallback = delegate
                {
                    IsConnected = true;
                    _multiConnection.OnSocketSongInfoReceived = OnSongInfoReceived;
                    _multiConnection.OnSocketOptionReceived = OnOptionInfoReceived;
                }
            };
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

        public void SendSongHashToLobby(string songHash, float gamespeed, string modifiers) => _multiConnection?.SendSongHash(songHash, gamespeed, modifiers);

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

        public void OnSongInfoReceived(SocketSongInfo socketSongInfo)
        {
            var songInfo = socketSongInfo.songInfo;
            ReplaySystemManager.gameSpeedMultiplier = socketSongInfo.songInfo.gameSpeed;
            GameModifierManager.LoadModifiersFromString(songInfo.modifiers);
            UpdateLobbySongInfo(songInfo.songName, songInfo.gameSpeed, songInfo.modifiers);

            var optionalTrack = TrackLookup.tryLookup(songInfo.trackRef);
            _hasSong = OptionModule.IsSome(optionalTrack);

            if (_hasSong)
            {
                var trackData = TrackLookup.toTrackData(optionalTrack.Value);
                UpdateLobbySongDetails(trackData);
                GlobalVariables.levelselect_index = trackData.trackindex;
                GlobalVariables.chosen_track = trackData.trackref;
                GlobalVariables.chosen_track_data = trackData;
                Plugin.LogInfo("Selected: " + trackData.trackref);
            }
            else
            {
                //TODO: Offer option to download song
            }
        }

        public void UpdateLobbySongInfo(string songName, float gamespeed, string modifiers) => _multLobbyPanel.OnSongInfoChanged(songName, gamespeed, modifiers);

        public void UpdateLobbySongDetails(SingleTrackData trackData) => _multLobbyPanel.SetTrackDataDetails(trackData);

        public void StartLobbyGame()
        {
            _multiConnection.SendOptionInfo(OptionInfoType.StartGame);
            StartGame();
        }

        public void StartGame()
        {
            if (_hasSong)
            {
                Plugin.LogInfo("Starting Multiplayer for " + GlobalVariables.chosen_track_data.trackname_short + " - " + GlobalVariables.chosen_track_data.trackref);
                CurrentInstance.fadepanel.gameObject.SetActive(true);
                LeanTween.alphaCanvas(CurrentInstance.fadepanel, 1f, .65f).setOnComplete(new Action(LoadLoaderScene));
            }
            else
            {
                TootTallyNotifManager.DisplayNotif("Chart not owned. Cannot start the game.");
            }
        }

        public void KickUserFromLobby(int userID) => _multiConnection.SendOptionInfo(OptionInfoType.KickFromLobby, new dynamic[] {userID});

        public void PromoteUser(int userID) => _multiConnection.SendOptionInfo(OptionInfoType.GiveHost, new dynamic[] { userID });

        public void LoadLoaderScene()
        {
            SceneManager.LoadScene("loader");
        }

        public void TransitionToSongSelection()
        {
            MultiplayerManager.UpdateMultiplayerState(MultiplayerState.SelectSong);
        }

        public void OnOptionInfoReceived(SocketOptionInfo optionInfo)
        {
            switch (Enum.Parse(typeof(OptionInfoType), optionInfo.optionType))
            {
                case OptionInfoType.StartGame:
                    StartGame(); break;
                case OptionInfoType.KickFromLobby:
                    if (TootTallyAccounts.TootTallyUser.userInfo.id == (int)optionInfo.values[0])
                        DisconnectFromLobby();
                    break;
                case OptionInfoType.Refresh:
                    RefreshAllLobbyInfo();
                    break;
            }
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
