using BaboonAPI.Hooks.Tracks;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using TootTallyAccounts;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.Helpers;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyGameModifiers;
using TootTallyLeaderboard.Replays;
using TootTallyMultiplayer.APIService;
using TootTallyMultiplayer.MultiplayerCore;
using TootTallyMultiplayer.MultiplayerPanels;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private static MultiplayerLiveScoreController _multiLiveScoreController;

        private MultiplayerPanelBase _currentActivePanel, _lastPanel;
        public bool IsTransitioning;
        private bool _hasSong;
        private UserState _currentUserState;
        private static string _savedDownloadLink;

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
            catch (Exception e)
            {
                Plugin.LogError("Couldn't init lobby panel");
                Plugin.LogError(e.Message);
                Plugin.LogError(e.StackTrace);
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

            if (IsConnected)
            {
                _multiConnection.OnSocketOptionReceived = OnOptionInfoReceived;
                _multiConnection.OnSocketSongInfoReceived = OnSongInfoReceived;
                _multiConnection.OnSocketLobbyInfoReceived = OnLobbyInfoReceived;
                OnLobbyInfoReceived(_currentLobby);
            }

            TootTallyAnimationManager.AddNewScaleAnimation(_multMainPanel.panel, Vector3.one, .8f, GetSecondDegreeAnimation(1.5f), sender =>
            {
                RefreshAllLobbyInfo();
                MultiplayerManager.AllowExit = true;
            });
        }

        public void InitializeLiveScore()
        {
            _multiLiveScoreController = GameObject.Find("GameplayCanvas/UIHolder").AddComponent<MultiplayerLiveScoreController>();
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
                    _multiConnection.OnSocketLobbyInfoReceived = OnLobbyInfoReceived;
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

        public void HidePanel() => _currentActivePanel.panel.SetActive(false);

        public void ShowPanel() => _currentActivePanel.panel.SetActive(true);

        public void RefreshAllLobbyInfo()
        {
            if (IsUpdating || CurrentInstance == null) return;

            _multMainPanel.ClearAllLobby();
            IsUpdating = true;
            Plugin.Instance.StartCoroutine(MultiplayerAPIService.GetLobbyList(lobbyList =>
            {
                _lobbyInfoList = lobbyList;
                UpdateLobbyInfo(true);
                IsUpdating = false;
                _multMainPanel.ShowRefreshLobbyButton();
            }));
        }

        public void RefreshCurrentLobbyInfo()
        {
            if (_currentLobby != null)
                OnLobbyInfoReceived(_currentLobby);
        }

        public void OnLobbyInfoReceived(MultiplayerLobbyInfo lobbyInfo)
        {
            _currentLobby = lobbyInfo;
            if (CurrentInstance != null)
            {
                _currentUserState = (UserState)Enum.Parse(typeof(UserState), _currentLobby.players.Find(x => x.id == TootTallyUser.userInfo.id).state);
                _multLobbyPanel.DisplayAllUserInfo(_currentLobby.players);
                _multLobbyPanel.OnLobbyInfoReceived(lobbyInfo.title, lobbyInfo.players.Count, lobbyInfo.maxPlayerCount);
                OnSongInfoReceived(_currentLobby.songInfo);
            }
        }
        public void OnLobbyInfoReceived(SocketLobbyInfo socketLobbyInfo) => OnLobbyInfoReceived(socketLobbyInfo.lobbyInfo);

        public void TransitionToPanel(MultiplayerPanelBase nextPanel)
        {
            if (_currentActivePanel == nextPanel || IsTransitioning) return;

            IsTransitioning = true;
            _lastPanel = _currentActivePanel;
            var positionOut = -nextPanel.GetPanelPosition;
            TootTallyAnimationManager.AddNewPositionAnimation(_currentActivePanel.panel, positionOut, 0.9f, new SecondDegreeDynamicsAnimation(1.5f, 0.89f, 1.1f), delegate { _lastPanel.panel.SetActive(false); _lastPanel.panel.GetComponent<RectTransform>().anchoredPosition = positionOut; });
            nextPanel.panel.SetActive(true);
            _currentActivePanel = nextPanel;
            TootTallyAnimationManager.AddNewPositionAnimation(nextPanel.panel, Vector2.zero, 0.9f, new SecondDegreeDynamicsAnimation(1.5f, 0.89f, 1.1f), sender => IsTransitioning = false);
        }

        public static SingleTrackData savedTrackData;

        public void OnSongInfoReceived(SocketSongInfo socketSongInfo) => OnSongInfoReceived(socketSongInfo.songInfo);
        public void OnSongInfoReceived(MultiplayerSongInfo songInfo)
        {
            ReplaySystemManager.gameSpeedMultiplier = songInfo.gameSpeed;
            GameModifierManager.LoadModifiersFromString(songInfo.modifiers);

            float diffIndex = (int)((songInfo.gameSpeed - .5f) / .25f);
            float diffMin = diffIndex * .25f + .5f;
            float diffMax = (diffIndex + 1f) * .25f + .5f;

            float by = (songInfo.gameSpeed - diffMin) / (diffMax - diffMin);

            float diff = EasingHelper.Lerp(songInfo.speed_diffs[(int)diffIndex], songInfo.speed_diffs[(int)diffIndex + 1], by);

            UpdateLobbySongInfo(songInfo.songName, songInfo.gameSpeed, songInfo.modifiers, diff);

            var optionalTrack = TrackLookup.tryLookup(songInfo.trackRef);
            _hasSong = OptionModule.IsSome(optionalTrack);

            if (_hasSong)
            {
                _savedDownloadLink = null;
                savedTrackData = TrackLookup.toTrackData(optionalTrack.Value);
                UpdateLobbySongDetails();
                GlobalVariables.levelselect_index = savedTrackData.trackindex;
                GlobalVariables.chosen_track = savedTrackData.trackref;
                GlobalVariables.chosen_track_data = savedTrackData;
                Plugin.LogInfo("Selected: " + savedTrackData.trackref);
                if (_currentUserState == UserState.NoSong)
                    SendUserState(UserState.NotReady);
            }
            else
            {
                _savedDownloadLink = songInfo.download;
                SendUserState(UserState.NoSong);
                _multLobbyPanel.SetNullTrackDataDetails();
            }
        }

        public void DownloadSavedChart()
        {
            if (_savedDownloadLink != null)
            {
                //DownloadChartHere
            }
        }

        public void UpdateLobbySongInfo(string songName, float gamespeed, string modifiers, float difficulty) => _multLobbyPanel?.OnSongInfoChanged(songName, gamespeed, modifiers, difficulty);


        public void UpdateLobbySongDetails()
        {
            if (savedTrackData != null)
                _multLobbyPanel?.SetTrackDataDetails(savedTrackData);
        }

        public void StartLobbyGame()
        {
            _multiConnection.SendOptionInfo(OptionInfoType.StartGame);
            StartGame();
        }

        public void StartGame()
        {
            if (_hasSong)
            {
                MultiplayerManager.UpdateMultiplayerState(MultiplayerState.Playing);
                Plugin.LogInfo("Starting Multiplayer for " + GlobalVariables.chosen_track_data.trackname_short + " - " + GlobalVariables.chosen_track_data.trackref);
                if (CurrentInstance != null)
                {
                    CurrentInstance.fadepanel.gameObject.SetActive(true);
                    LeanTween.alphaCanvas(CurrentInstance.fadepanel, 1f, .65f).setOnComplete(new Action(LoadLoaderScene));
                }
                else
                    LoadLoaderScene();

            }
            else
            {
                TootTallyNotifManager.DisplayNotif("Chart not owned. Cannot start the game.");
            }
        }

        public void KickUserFromLobby(int userID) => _multiConnection.SendOptionInfo(OptionInfoType.KickFromLobby, new dynamic[] { userID });

        public void GiveHostUser(int userID) => _multiConnection.SendOptionInfo(OptionInfoType.GiveHost, new dynamic[] { userID });

        public void LoadLoaderScene()
        {
            CurrentInstance = null;
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
                    if (TootTallyAccounts.TootTallyUser.userInfo.id == optionInfo.values[0])
                        DisconnectFromLobby();
                    break;
                case OptionInfoType.Refresh:
                case OptionInfoType.GiveHost:
                case OptionInfoType.UpdateUserState:
                    RefreshAllLobbyInfo();
                    break;
                case OptionInfoType.UpdateScore:
                    _multiLiveScoreController?.UpdateLiveScore(optionInfo.values[0], optionInfo.values[1], optionInfo.values[2], optionInfo.values[3]);
                    break;
            }
        }

        #region MultiConnectionRequests
        public void SendSongFinishedToLobby() => _multiConnection?.SendOptionInfo(OptionInfoType.SongFinished);
        public void SendSongHashToLobby(string songHash, float gamespeed, string modifiers) => _multiConnection?.SendSongHash(songHash, gamespeed, modifiers);
        public void SendScoreDataToLobby(int score, int combo, int health, int tally) => _multiConnection?.SendUpdateScore(score, combo, health, tally);
        public void SendUserState(UserState state)
        {
            if (_currentUserState != state)
            {
                _currentUserState = state;
                _multLobbyPanel?.OnUserStateChange(state);
                _multiConnection?.SendUserState(state);
            }
        }
        #endregion

        public static SecondDegreeDynamicsAnimation GetSecondDegreeAnimation(float speedMult = 1f) => new SecondDegreeDynamicsAnimation(speedMult, 0.75f, 1.15f);
        public static SecondDegreeDynamicsAnimation GetSecondDegreeAnimationNoBounce(float speedMult = 1f) => new SecondDegreeDynamicsAnimation(speedMult, 1f, 1f);

        public void Dispose()
        {
            CurrentInstance = null;
            _lobbyInfoList?.Clear();
            _multiConnection?.Disconnect();
        }

        public enum MultiplayerState
        {
            None,
            Home,
            CreatingLobby,
            Lobby,
            Hosting,
            SelectSong,
            ExitScene,
            Playing,
            PointScene,
            Quitting
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
