using BaboonAPI.Hooks.Tracks;
using BepInEx;
using JetBrains.Annotations;
using Microsoft.FSharp.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TootTallyAccounts;
using TootTallyCore.APIServices;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.Helpers;
using TootTallyCore.Utils.TootTallyGlobals;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyGameModifiers;
using TootTallyMultiplayer.APIService;
using TootTallyMultiplayer.MultiplayerCore;
using TootTallyMultiplayer.MultiplayerCore.PointScore;
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
        private static List<string> _newLobbyCodeList;
        private static MultiplayerLobbyInfo _currentLobby;

        private static MultiplayerSystem _multiConnection;
        private static MultiplayerLiveScoreController _multiLiveScoreController;

        private MultiplayerPanelBase _currentActivePanel, _lastPanel;
        public bool IsTransitioning;
        private bool _hasSong;
        private UserState _currentUserState;
        private static string _savedDownloadLink, _savedTrackRef;

        private MultiplayerMainPanel _multMainPanel;
        private MultiplayerLobbyPanel _multLobbyPanel;
        private MultiplayerCreatePanel _multCreatePanel;


        public bool IsUpdating;
        public bool IsConnectionPending;
        public bool IsDownloadPending;
        public bool IsConnected => _multiConnection != null && _multiConnection.IsConnected;
        public bool IsAnybodyLoading => _currentLobby.players.Where(x => x.id != TootTallyUser.userInfo.id).Any(x => x.state == "Loading");

        public bool IsRequestPending => _multCreatePanel.IsRequestPending || IsConnectionPending;

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
            catch (Exception e)
            {
                Plugin.LogError(e.Message);
                Plugin.LogError(e.StackTrace);
            }

            try
            {
                _multLobbyPanel = new MultiplayerLobbyPanel(canvas, this);
            }
            catch (Exception e)
            {
                Plugin.LogError(e.Message);
                Plugin.LogError(e.StackTrace);
            }

            try
            {
                _multCreatePanel = new MultiplayerCreatePanel(canvas, this);
            }
            catch (Exception e)
            {
                Plugin.LogError(e.Message);
                Plugin.LogError(e.StackTrace);
            }
            _lobbyInfoList ??= new List<MultiplayerLobbyInfo>();
            _currentActivePanel = _multMainPanel;

            if (IsConnected)
            {
                UpdateLobbySongDetails();
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

        public void OnGameControllerStartSetup()
        {
            _multiLiveScoreController = GameObject.Find("GameplayCanvas/UIHolder").AddComponent<MultiplayerLiveScoreController>();
            MultiplayerPointScoreController.ClearSavedScores();
        }

        public void OnGameControllerStartSongSendReadyState()
        {
            SendUserState(UserState.Playing);
        }

        public void OnSongQuit()
        {
            _multiConnection.SendUserState(UserState.NotReady);
        }

        public void InitializePointScore()
        {
            GameObject.Find("Canvas/FullPanel").AddComponent<MultiplayerPointScoreController>();
        }

        private IEnumerator<WaitForSeconds> DelayDisplayLobbyInfo(float delay, MultiplayerLobbyInfo lobby, Action<MultiplayerLobbyInfo> callback)
        {
            yield return new WaitForSeconds(delay);
            callback(lobby);
        }

        public void UpdateLobbyInfo()
        {
            for (int i = 0; i < _lobbyInfoList.Count; i++)
            {
                var doAnimation = _newLobbyCodeList.Contains(_lobbyInfoList[i].id);
                if (doAnimation)
                    Plugin.Instance.StartCoroutine(DelayDisplayLobbyInfo(i * .1f, _lobbyInfoList[i], _multMainPanel.DisplayLobby));
                else
                    _multMainPanel.DisplayLobby(_lobbyInfoList[i], false);
            }
            _newLobbyCodeList.Clear();
            _multMainPanel.UpdateScrolling(_lobbyInfoList.Count);
        }

        public void ConnectToLobby(string code)
        {
            RefreshAllLobbyInfo();
            if (_multiConnection != null && _multiConnection.ConnectionPending) return;

            _multiConnection?.Disconnect();
            Plugin.LogInfo("Connecting to " + code);
            IsConnectionPending = true;
            _multiConnection = new MultiplayerSystem(code, false)
            {
                OnWebSocketOpenCallback = delegate
                {
                    _multiConnection.OnWebSocketCloseCallback = null;
                    _multiConnection.OnSocketSongInfoReceived = OnSongInfoReceived;
                    _multiConnection.OnSocketOptionReceived = OnOptionInfoReceived;
                    _multiConnection.OnSocketLobbyInfoReceived = OnLobbyInfoReceived;
                },
                OnWebSocketCloseCallback = DisconnectFromLobby
            };
        }

        public void DisconnectFromLobby()
        {
            if (_multiConnection.IsConnected)
            {
                _multiConnection.Disconnect();
                _multLobbyPanel.ResetData();
                MultiplayerManager.UpdateMultiplayerState(MultiplayerState.Home);
                MoveToMain();
            }
            else
                TootTallyNotifManager.DisplayNotif("Unexpected error occured.");
            _currentLobby = null;
            IsConnectionPending = false;
            RefreshAllLobbyInfo();
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
            MultiplayerLogger.ClearLogs();
            MultiplayerLogger.ServerLog($"Connected to {_multiConnection.GetServerID}");
            MultiplayerManager.UpdateMultiplayerState(MultiplayerState.Lobby);
            OnLobbyConnectionSuccess();
        }

        public void OnLobbyConnectionSuccess()
        {
            MoveToLobby();
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
            IsUpdating = true;
            Plugin.Instance.StartCoroutine(MultiplayerAPIService.GetLobbyList(lobbyList =>
            {
                _multMainPanel.ClearAllLobby();
                var idList = _lobbyInfoList.Select(x => x.id);
                _newLobbyCodeList = lobbyList.Select(x => x.id).Where(x => !idList.Contains(x)).ToList();

                _lobbyInfoList = lobbyList;
                UpdateLobbyInfo();
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

        public void OnUserInfoReceived(MultiplayerUserInfo userInfo)
        {
            if (userInfo == null) return;

            var index = _currentLobby.players.FindIndex(x => x.id == userInfo.id);
            _currentLobby.players[index] = userInfo;

            if (CurrentInstance != null)
                _multLobbyPanel?.UpdateUserInfo(userInfo);
        }

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

        public static MultiplayerSongInfo savedSongInfo;
        public static SingleTrackData savedTrackData;

        public void OnSongInfoReceived(SocketSongInfo socketSongInfo) => OnSongInfoReceived(socketSongInfo.songInfo);
        public void OnSongInfoReceived(MultiplayerSongInfo songInfo)
        {
            if (savedSongInfo != songInfo && songInfo.trackRef != "")
                MultiplayerLogger.ServerLog($"Song \"{songInfo.songName}\" was selected.");

            savedSongInfo = songInfo;
            TootTallyGlobalVariables.gameSpeedMultiplier = songInfo.gameSpeed;
            GameModifierManager.LoadModifiersFromString(songInfo.modifiers);

            float diffIndex = (int)((songInfo.gameSpeed - .5f) / .25f);

            float diff;

            if (diffIndex != 6)
            {
                float diffMin = diffIndex * .25f + .5f;
                float diffMax = (diffIndex + 1f) * .25f + .5f;

                float by = (songInfo.gameSpeed - diffMin) / (diffMax - diffMin);

                diff = EasingHelper.Lerp(songInfo.speed_diffs[(int)diffIndex], songInfo.speed_diffs[(int)diffIndex + 1], by);
            }
            else
                diff = songInfo.speed_diffs[(int)diffIndex];

            UpdateLobbySongInfo(songInfo.songName, songInfo.gameSpeed, songInfo.modifiers, diff);

            var optionalTrack = TrackLookup.tryLookup(songInfo.trackRef);
            _hasSong = songInfo.trackRef != "" && OptionModule.IsSome(optionalTrack);

            if (_hasSong)
            {
                SelectSongFromTrackref(optionalTrack.Value.trackref);
                if (_currentUserState == UserState.NoSong)
                    SendUserState(UserState.NotReady);
            }
            else
            {
                _savedDownloadLink = songInfo.mirror;
                _savedTrackRef = songInfo.trackRef;
                SendUserState(UserState.NoSong);
                _multLobbyPanel.SetNullTrackDataDetails(_savedDownloadLink != null);
            }
        }

        public void DownloadSavedChart(ProgressBar bar)
        {
            if (_savedDownloadLink != null)
            {
                IsDownloadPending = true;
                Plugin.Instance.StartCoroutine(TootTallyAPIService.DownloadZipFromServer(_savedDownloadLink, bar, data =>
                {
                    IsDownloadPending = false;
                    if (data != null)
                    {
                        string downloadDir = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "Downloads/");
                        string fileName = $"{_savedDownloadLink.Split('/').Last()}";
                        if (!Directory.Exists(downloadDir))
                            Directory.CreateDirectory(downloadDir);
                        FileHelper.WriteBytesToFile(downloadDir, fileName, data);

                        string source = Path.Combine(downloadDir, fileName);
                        string destination = Path.Combine(Paths.BepInExRootPath, "CustomSongs/");
                        FileHelper.ExtractZipToDirectory(source, destination);

                        FileHelper.DeleteFile(downloadDir, fileName);
                        TootTallyCore.Plugin.Instance.ReloadTracks();
                        SelectSongFromTrackref(_savedTrackRef);
                        SendUserState(UserState.NotReady);
                    }
                    else
                    {
                        _multLobbyPanel.OnUserStateChange(_currentUserState);
                        TootTallyNotifManager.DisplayNotif("Download failed.");
                    }

                }));
            }
        }

        public void SelectSongFromTrackref(string trackref)
        {
            var track = TrackLookup.tryLookup(trackref);
            savedTrackData = TrackLookup.toTrackData(track.Value);
            UpdateLobbySongDetails();
            GlobalVariables.levelselect_index = savedTrackData.trackindex;
            GlobalVariables.chosen_track = savedTrackData.trackref;
            GlobalVariables.chosen_track_data = savedTrackData;
            _hasSong = true;
            _savedDownloadLink = null;
            _savedTrackRef = null;
            Plugin.LogInfo("Selected: " + savedTrackData.trackref);
        }

        public void UpdateLobbySongInfo(string songName, float gamespeed, string modifiers, float difficulty) =>
                _multLobbyPanel?.OnSongInfoChanged(songName, gamespeed, modifiers, difficulty);


        public void UpdateLobbySongDetails()
        {
            if (CurrentInstance == null) return;

            if (savedTrackData != null)
                _multLobbyPanel?.SetTrackDataDetails(savedTrackData);
            if (savedSongInfo != null)
                _multLobbyPanel?.OnSongInfoChanged(savedSongInfo);
        }

        public void StartLobbyGame()
        {
            _multiConnection.SendOptionInfo(OptionInfoType.StartGame);
        }

        public void StartGame()
        {
            if (_hasSong && CurrentInstance != null && !IsTransitioning)
            {
                MultiplayerManager.UpdateMultiplayerState(MultiplayerState.Playing);
                Plugin.LogInfo("Starting Multiplayer for " + GlobalVariables.chosen_track_data.trackname_short + " - " + GlobalVariables.chosen_track_data.trackref);
                IsTransitioning = true;
                CurrentInstance.sfx_ok.Play();
                CurrentInstance.fadepanel.gameObject.SetActive(true);
                MultiAudioController.PauseMusicSoft();
                LeanTween.alphaCanvas(CurrentInstance.fadepanel, 1f, .65f).setOnComplete(new Action(LoadLoaderScene));
                return;
            }

            TootTallyNotifManager.DisplayNotif($"Cannot start the game. {(!_hasSong ? "Chart not owned." : "")}");
        }

        public void KickUserFromLobby(int userID) => _multiConnection.SendOptionInfo(OptionInfoType.KickFromLobby, new dynamic[] { userID });

        public void GiveHostUser(int userID) => _multiConnection.SendOptionInfo(OptionInfoType.GiveHost, new dynamic[] { userID });

        public void SendQuickChat(QuickChat chat) => _multiConnection.SendOptionInfo(OptionInfoType.QuickChat, new dynamic[] { (int)chat });

        public void LoadLoaderScene()
        {
            CurrentInstance = null;
            _multLobbyPanel = null;
            IsTransitioning = false;
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
                case OptionInfoType.GiveHost:
                case OptionInfoType.UpdateUserState:
                    RefreshAllLobbyInfo();
                    break;
                case OptionInfoType.UpdateUserInfo:
                    OnUserInfoReceived(optionInfo.values[0].ToObject<MultiplayerUserInfo>());
                    break;
                case OptionInfoType.UpdateScore:
                    //id - score - combo - health
                    _multiLiveScoreController?.UpdateLiveScore((int)optionInfo.values[0], (int)optionInfo.values[1], (int)optionInfo.values[2], (int)optionInfo.values[3]);
                    break;
                case OptionInfoType.FinalScore:
                    //id - score - percent - maxcombo - tally
                    MultiplayerPointScoreController.AddScore((int)optionInfo.values[0], (int)optionInfo.values[1], (float)optionInfo.values[2], (int)optionInfo.values[3], optionInfo.values[4].ToObject<int[]>());
                    break;
            }
        }

        public static MultiplayerUserInfo GetUserFromLobby(int id) => _currentLobby?.players.Find(x => x.id == id);

        #region MultiConnectionRequests
        public void SendSongFinishedToLobby() => _multiConnection?.SendOptionInfo(OptionInfoType.SongFinished);
        public void SendSongHashToLobby(string songHash, float gamespeed, string modifiers) => _multiConnection?.SendSongHash(songHash, gamespeed, modifiers);
        public void SendScoreDataToLobby(int score, int combo, int health, int tally)
        {
            _multiConnection?.SendUpdateScore(score, combo, health, tally);
            _multiLiveScoreController?.UpdateLiveScore(TootTallyUser.userInfo.id, score, combo, health);
        }

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
