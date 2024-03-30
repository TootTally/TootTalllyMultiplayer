using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TootTallyAccounts;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyLeaderboard;
using TootTallyMultiplayer.MultiplayerCore;
using TootTallyMultiplayer.MultiplayerCore.InputPrompts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public class MultiplayerLobbyPanel : MultiplayerPanelBase
    {
        public GameObject lobbyUserContainer, rightPanel, rightPanelContainer, middlePanel;
        public GameObject titleContainer, songDescContainer, buttonContainer;
        public GameObject songInfoContainer, songInfoTop, songInfoBottom;

        //private GameObject _quickChatContainer;

        private Dictionary<int, MultiplayerCard> _userCardsDict;

        private CustomButton _selectSongButton, _startGameButton, _readyUpButton;
        private CustomButton _profileButton, _giveHostButton, _kickButton, _reportButton;

        private GameObject _lobbySettingButton;

        private TMP_Text _titleText, _maxPlayerText, _songNameText, _songArtistText, _timeText,
            _songDescText, _bpmText, _gameSpeedText, _modifiersText, _ratingText;

        private TMP_Text _lobbyLogText;

        private ProgressBar _downloadProgressBar;

        private Slider _hiddenUserCardSlider, _scrollSpeedSlider;
        private ScrollableSliderHandler _scrollingHandler;

        private GameObject _dropdownMenu, _dropdownMenuContainer;
        private MultiplayerUserInfo _dropdownUserInfo;

        private TootTallyAnimation _dropdownAnimation;

        private LobbySettingsInputPrompt _lobbySettingsInputPrompt;
        private TootTallyAnimation _lobbySettingsAnimation;

        public bool IsHost;

        private int _maxPlayerCount;
        private int _readyCount;
        private float _previousUserCount;

        private float _savedGameSpeed;

        private float _lastLobbyContainerPosY;
        private Vector3 _lobbyContainerScrollingDistance;

        private bool _canPressButton;

        private UserState _userState;
        //private int _quickChatPageIndex;

        public MultiplayerLobbyPanel(GameObject canvas, MultiplayerController controller) : base(canvas, controller, "LobbyLayout")
        {
            panel.transform.localPosition = new Vector2(2000, 0);
            lobbyUserContainer = center.transform.Find("Left/UserContainer").gameObject;

            rightPanel = center.transform.Find("Right").gameObject;
            rightPanelContainer = rightPanel.transform.Find("InfoContainer").gameObject;

            middlePanel = center.transform.Find("Middle").gameObject;

            titleContainer = rightPanelContainer.transform.GetChild(0).gameObject;

            songInfoContainer = rightPanelContainer.transform.GetChild(1).gameObject;
            songInfoTop = songInfoContainer.transform.GetChild(0).gameObject;
            songInfoBottom = songInfoContainer.transform.GetChild(1).gameObject;

            songDescContainer = rightPanelContainer.transform.GetChild(2).gameObject;
            buttonContainer = rightPanelContainer.transform.GetChild(3).gameObject;

            //_quickChatContainer = MultiplayerGameObjectFactory.GetHorizontalBox(new Vector2(64, 64), middlePanel.transform);
            //_quickChatContainer.name = "QuickChat";

            //GameObjectFactory.CreateCustomButton(_quickChatContainer.transform, Vector2.zero, new Vector2(64, 64), AssetManager.GetSprite("Bubble.png"), "QuickChatButton", OnQuickChatOpenButtonClick);

            _hiddenUserCardSlider = new GameObject("ContainerSlider", typeof(Slider)).GetComponent<Slider>();
            _hiddenUserCardSlider.gameObject.SetActive(true);
            _hiddenUserCardSlider.onValueChanged.AddListener(value => OnSliderValueChangeScrollContainer(lobbyUserContainer, value));
            _scrollingHandler = _hiddenUserCardSlider.gameObject.AddComponent<ScrollableSliderHandler>();
            _scrollingHandler.enabled = false;

            _userCardsDict = new Dictionary<int, MultiplayerCard>();

            _lobbySettingsInputPrompt = MultiplayerGameObjectFactory.CreateLobbySettingsInputPrompt(canvas.transform, OnSettingsPromptConfirm);
            _lobbySettingsInputPrompt.gameObject.SetActive(false);
            _lobbySettingButton = GameObjectFactory.CreateClickableImageHolder(headerRight.transform, Vector2.zero, new Vector2(72, 72), AssetManager.GetSprite("motherfuckinglobbysettingsicon256.png"), "LobbySettingButton", _lobbySettingsInputPrompt.Show).gameObject;
            _lobbySettingButton.gameObject.SetActive(false);
            

            GameObjectFactory.CreateClickableImageHolder(headerLeft.transform, Vector2.zero, new Vector2(72, 72), AssetManager.GetSprite("gtfo.png"), "LobbyBackButton", OnBackButtonClick);

            //Menu when clicking on user pfp
            _dropdownMenu = MultiplayerGameObjectFactory.GetBorderedVerticalBox(new Vector2(300, 180), 5, panel.transform);
            _dropdownMenu.GetComponent<Image>().enabled = false;
            _lobbyContainerScrollingDistance = Vector3.zero;

            var trigger = _dropdownMenu.AddComponent<EventTrigger>();
            EventTrigger.Entry pointerExitEvent = new EventTrigger.Entry();
            pointerExitEvent.eventID = EventTriggerType.PointerExit;
            pointerExitEvent.callback.AddListener(data => HideDropdown());
            trigger.triggers.Add(pointerExitEvent);

            _dropdownMenu.AddComponent<LayoutElement>().ignoreLayout = true;
            var rect = _dropdownMenu.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0, 1);
            _dropdownMenuContainer = _dropdownMenu.transform.GetChild(0).gameObject;
            _profileButton = GameObjectFactory.CreateCustomButton(_dropdownMenuContainer.transform, Vector2.zero, new Vector2(295, 60), "Profile", "DropdownProfile", OnProfileButtonClick);
            _giveHostButton = GameObjectFactory.CreateCustomButton(_dropdownMenuContainer.transform, Vector2.zero, new Vector2(295, 60), "Give Host", "DropdownGiveHost", OnGiveHostButtonClick);
            _kickButton = GameObjectFactory.CreateCustomButton(_dropdownMenuContainer.transform, Vector2.zero, new Vector2(295, 60), "Kick", "DropdownKick", OnKickUserButtonClick);
            _reportButton = GameObjectFactory.CreateCustomButton(_dropdownMenuContainer.transform, Vector2.zero, new Vector2(295, 60), "Report", "DropdownReport", OnReportButtonClick);
            _dropdownMenu.SetActive(false);

            //TITLE
            _titleText = GameObjectFactory.CreateSingleText(headerCenter.transform, "TitleText", "-");
            _titleText.enableAutoSizing = true;
            _titleText.alignment = TextAlignmentOptions.Left;
            _titleText.fontStyle = TMPro.FontStyles.Bold;
            _titleText.fontSizeMax = 60;

            _maxPlayerText = GameObjectFactory.CreateSingleText(headerCenter.transform, "MaxPlayer", "-/-");
            _maxPlayerText.alignment = TextAlignmentOptions.Right;
            _titleText.fontStyle = TMPro.FontStyles.Bold;
            _maxPlayerText.fontSize = 32;
            _maxPlayerText.enableWordWrapping = false;

            //SONG INFO
            titleContainer.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(15, 15, 15, 15);
            songInfoContainer.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(15, 15, 15, 15);
            _songNameText = GameObjectFactory.CreateSingleText(titleContainer.transform, "SongNameText", "-");
            _songNameText.rectTransform.sizeDelta = new Vector2(0, 60);
            _songNameText.enableAutoSizing = true;
            _songNameText.fontSizeMax = 60;
            _songNameText.fontSizeMin = 48;
            _songNameText.overflowMode = TextOverflowModes.Ellipsis;
            _songNameText.fontStyle = TMPro.FontStyles.Bold;
            var titleTextTrigger = _songNameText.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry pointerClickEvent = new EventTrigger.Entry();
            pointerClickEvent.eventID = EventTriggerType.PointerClick;
            pointerClickEvent.callback.AddListener(data => { OnTitleTextClickOpenSongLink(); });
            titleTextTrigger.triggers.Add(pointerClickEvent);

            _songArtistText = GameObjectFactory.CreateSingleText(titleContainer.transform, "SongArtistText", "-");
            _songArtistText.rectTransform.sizeDelta = new Vector2(0, 75);
            _songArtistText.fontSizeMax = 30;

            _songDescText = GameObjectFactory.CreateSingleText(songDescContainer.transform, "SongDescText", "-");
            _songDescText.alignment = TextAlignmentOptions.TopLeft;
            _songDescText.fontSizeMax = 22;

            _songArtistText.overflowMode = _songDescText.overflowMode = TextOverflowModes.Overflow;
            _songNameText.alignment = _songArtistText.alignment = TextAlignmentOptions.Left;

            //DETAILS
            //Top
            float iconSize = 32f;
            songDescContainer.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(15, 15, 15, 15);
            GameObjectFactory.CreateImageHolder(songInfoTop.transform, Vector2.zero, Vector2.one * iconSize, AssetManager.GetSprite("gamespeed64.png"), "GameSpeedIcon");
            _gameSpeedText = GameObjectFactory.CreateSingleText(songInfoTop.transform, "GameSpeedText", " -");
            GameObjectFactory.CreateImageHolder(songInfoTop.transform, Vector2.zero, Vector2.one * iconSize, AssetManager.GetSprite("stardiff64.png"), "RatingIcon");
            _ratingText = GameObjectFactory.CreateSingleText(songInfoTop.transform, "RatingText", " -");
            _modifiersText = GameObjectFactory.CreateSingleText(songInfoTop.transform, "ModsText", "M ");

            //Bottom
            GameObjectFactory.CreateImageHolder(songInfoBottom.transform, Vector2.zero, Vector2.one * iconSize, AssetManager.GetSprite("time64.png"), "TimeIcon");
            _timeText = GameObjectFactory.CreateSingleText(songInfoBottom.transform, "TimeText", " -");
            GameObjectFactory.CreateImageHolder(songInfoBottom.transform, Vector2.zero, Vector2.one * iconSize, AssetManager.GetSprite("bpm64.png"), "BPMIcon");
            _bpmText = GameObjectFactory.CreateSingleText(songInfoBottom.transform, "BPMText", " -");

            _timeText.rectTransform.sizeDelta = _gameSpeedText.rectTransform.sizeDelta = _modifiersText.rectTransform.sizeDelta = _bpmText.rectTransform.sizeDelta = new Vector2(200, 0);
            _ratingText.rectTransform.sizeDelta = new Vector2(170, 0);

            BetterScrollSpeedSliderPatcher.SetSliderOption();

            _scrollSpeedSlider = GameObjectFactory.CreateSliderFromPrefab(songInfoBottom.transform, "ScrollSpeedSlider");
            _scrollSpeedSlider.minValue = BetterScrollSpeedSliderPatcher.options.Min.Value / 100f;
            _scrollSpeedSlider.maxValue = BetterScrollSpeedSliderPatcher.options.Max.Value / 100f;
            _scrollSpeedSlider.value = BetterScrollSpeedSliderPatcher.options.LastValue.Value / 100f;

            _scrollSpeedSlider.gameObject.SetActive(true);
            _scrollSpeedSlider.transform.localScale = Vector3.one * 1.75f;
            var sliderText = GameObjectFactory.CreateSingleText(_scrollSpeedSlider.handleRect, "ScrollSpeedSliderText", BetterScrollSpeedSliderPatcher.SliderValueToText(_scrollSpeedSlider.value));
            sliderText.enableWordWrapping = false;
            sliderText.fontSize = 12;
            _scrollSpeedSlider.onValueChanged.AddListener((float value) =>
            {
                BetterScrollSpeedSliderPatcher.options.LastValue.Value = value * 100f;
                sliderText.text = BetterScrollSpeedSliderPatcher.SliderValueToText(value);
                GlobalVariables.gamescrollspeed = value;
            });

            SetTextsParameters(_timeText, _bpmText, _gameSpeedText, _modifiersText, _ratingText);

            //BUTTONS
            _selectSongButton = GameObjectFactory.CreateCustomButton(buttonContainer.transform, Vector2.zero, new Vector2(170, 65), "SelectSong", "SelectSongButton", OnSelectSongButtonClick);
            _selectSongButton.gameObject.SetActive(false);
            _startGameButton = GameObjectFactory.CreateCustomButton(buttonContainer.transform, Vector2.zero, new Vector2(170, 65), "Start Game", "StartGameButton", OnStartGameButtonClick);
            _startGameButton.gameObject.SetActive(false);
            _readyUpButton = GameObjectFactory.CreateCustomButton(buttonContainer.transform, Vector2.zero, new Vector2(220, 100), "Ready Up", "ReadyUpButton", OnReadyButtonClick);
            _downloadProgressBar = GameObjectFactory.CreateProgressBar(buttonContainer.transform, Vector2.zero, new Vector2(550, 35), false, "DownloadProgressBar");
            //_quickChatPageIndex = 1;
            ResetData();

            footer.AddComponent<Mask>();
            _lobbyLogText = GameObjectFactory.CreateSingleText(footer.transform, "LogText", MultiplayerLogger.GetFormattedLogs());
            _lobbyLogText.alignment = TextAlignmentOptions.BottomLeft;
            _lobbyLogText.overflowMode = TextOverflowModes.Masking;
            _lobbyLogText.lineSpacing = 22;
            _lobbyLogText.margin = Vector4.one * 2f;
            MultiplayerLogger.OnLogReceivedUpdate = UpdateLogText;
        }

        public void ResetData()
        {
            _userState = UserState.None;
            DisableButton(.8f);
            _lobbySettingsInputPrompt.Hide(false);
        }

        private void SetTextsParameters(params TMP_Text[] texts)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                var t = texts[i];
                t.fontSize = 22;
                t.alignment = TextAlignmentOptions.Left;
                t.enableWordWrapping = false;
            }
        }

        private MultiplayerUserInfo _hostInfo;
        private List<MultiplayerUserInfo> _lastUsers;
        public void DisplayAllUserInfo(List<MultiplayerUserInfo> users)
        {
            if (_lastUsers != null)
            {
                users.FindAll(x => !_lastUsers.Any(l => x.id == l.id)).ToList().ForEach(u =>
                {
                    MultiplayerLogger.ServerLog($"{u.username} joined the lobby.");
                });
                _lastUsers.FindAll(l => !users.Any(x => l.id == x.id)).ToList().ForEach(u =>
                {
                    MultiplayerLogger.ServerLog($"{u.username} left the lobby.");
                });
            }

            ClearAllUserRows();

            _hostInfo = users.First();
            IsHost = _hostInfo.id == TootTallyUser.userInfo.id;
            _lobbySettingButton.SetActive(IsHost);
            _startGameButton.gameObject.SetActive(IsHost);
            _selectSongButton.gameObject.SetActive(IsHost);

            _readyUpButton.gameObject.SetActive(!IsHost);

            _maxPlayerText.text = $"{users.Count}/{_maxPlayerCount}";

            users.ForEach(DisplayUserInfo);
            UpdateScrolling(_userCardsDict.Count);
            _lastUsers = users;
        }

        public void UpdateScrolling(int userCount)
        {
            var enableScrolling = userCount > 6;
            if (!enableScrolling && _scrollingHandler.enabled)
            {
                _scrollingHandler.ResetAcceleration();
                _hiddenUserCardSlider.value = 0;
            }
            _scrollingHandler.enabled = enableScrolling;
            _scrollingHandler.accelerationMult = enableScrolling ? 20f / userCount : 1f;
            center.transform.Find("Left").GetComponent<HorizontalLayoutGroup>().enabled = !enableScrolling; //only need this to initialize, else it causes scrolling bugs

            if (_previousUserCount != 0 && _hiddenUserCardSlider.value != 0 && enableScrolling)
                _hiddenUserCardSlider.value *= _previousUserCount / userCount;

            _previousUserCount = userCount;
        }

        public void DisplayUserInfoDebug()
        {
            DisplayUserInfo(new MultiplayerUserInfo() { id = TootTallyUser.userInfo.id + _userCardsDict.Count, rank = 69, username = "TestUser", state = "Ready" });
            UpdateScrolling(_userCardsDict.Count);
        }

        public void DisplayUserInfo(MultiplayerUserInfo user)
        {
            //Should probably turn this into a prefab.
            var parsedState = (UserState)Enum.Parse(typeof(UserState), user.state);
            var userState = user.id == _hostInfo.id ? UserState.Host : parsedState;

            if (_userState == UserState.None && IsSelf(user.id))
                OnUserStateChange(parsedState);

            var userCard = MultiplayerGameObjectFactory.CreateUserCard(lobbyUserContainer.transform);
            _userCardsDict.Add(user.id, userCard);

            var imageHolder = GameObjectFactory.CreateClickableImageHolder(userCard.container, Vector2.zero, new Vector2(100, 64), AssetManager.GetSprite("icon.png"), $"PFP", () => OnUserPFPClick(user));
            imageHolder.transform.localPosition = new Vector3(-305, 0, 0);
            imageHolder.transform.localScale = Vector2.one * .9f;
            imageHolder.transform.SetAsFirstSibling();
            AssetManager.GetProfilePictureByID(user.id, sprite =>
            {
                imageHolder.GetComponent<Image>().sprite = sprite;
            });

            UpdateUserInfo(user);
        }

        public void UpdateUserInfo(MultiplayerUserInfo user)
        {
            if (!_userCardsDict.ContainsKey(user.id)) return;

            var userCard = _userCardsDict[user.id];

            var parsedPreviousState = userCard.user != null ? (UserState)Enum.Parse(typeof(UserState), userCard.user.state) : UserState.None;

            var parsedState = (UserState)Enum.Parse(typeof(UserState), user.state);
            var userState = user.id == _hostInfo.id ? UserState.Host : parsedState;
            var displayedState = userState == UserState.Host && (user.state == "Ready" || user.state == "NotReady") ? "Host" : user.state;

            if (_userState == UserState.None && IsSelf(user.id))
                OnUserStateChange(parsedState);

            userCard.UpdateUserInfo(user, displayedState);

            _readyCount = _userCardsDict.Values.Where(x => x.user.state == "Ready" && !IsSelf(x.user.id)).Count() + 1;

            var color = UserStateToColor(userState);
            GameObjectFactory.TintImage(userCard.container.GetComponent<Image>(), color, .2f);
            userCard.image.color = color;

            if (IsHost && !controller.IsTimerStarted)
                SetHostButtonText();
            UpdateScrolling(_userCardsDict.Count);
        }

        private float _posYJumpValue = 83f;
        private float _posYOffset = -340f;

        public void OnSliderValueChangeScrollContainer(GameObject container, float value)
        {
            var gridPanelRect = container.GetComponent<RectTransform>();
            gridPanelRect.anchoredPosition = new Vector2(gridPanelRect.anchoredPosition.x, value * (_userCardsDict.Count - 7f) * _posYJumpValue + _posYOffset);
            _lobbyContainerScrollingDistance.y = _lastLobbyContainerPosY - gridPanelRect.anchoredPosition.y;
            _dropdownMenu.transform.localPosition -= _lobbyContainerScrollingDistance;
            _lastLobbyContainerPosY = gridPanelRect.anchoredPosition.y;
        }

        public void RemoveUserCard(int id)
        {
            _userCardsDict.Remove(id);
        }

        private Color UserStateToColor(UserState userState) =>
            userState switch
            {
                UserState.NoSong => new Color(.95f, .2f, .2f, 1),
                UserState.NotReady => new Color(.95f, .95f, .2f, 1),
                UserState.Ready => new Color(.2f, .95f, .2f, 1),
                UserState.Host => new Color(.95f, .2f, .95f, 1),
                _ => new Color(.95f, .95f, .95f, 1),
            };

        private void OnUserPFPClick(MultiplayerUserInfo user)
        {
            _dropdownUserInfo = user;
            UpdateDropdown(user.id);
            var v3 = Input.mousePosition;
            v3.z = 0;
            _dropdownMenu.transform.position = v3 - (new Vector3(1, 1, 0) * 4f);
            _dropdownMenu.transform.localScale = Vector2.zero;
            _dropdownMenu.SetActive(true);
            _dropdownAnimation?.Dispose();
            _dropdownAnimation = TootTallyAnimationManager.AddNewScaleAnimation(_dropdownMenu, Vector2.one, .6f, MultiplayerController.GetSecondDegreeAnimation(2.8f));
        }

        private void HideDropdown()
        {
            _dropdownAnimation?.Dispose();
            _dropdownAnimation = TootTallyAnimationManager.AddNewScaleAnimation(_dropdownMenu, Vector2.zero, .4f, MultiplayerController.GetSecondDegreeAnimationNoBounce(5f));
        }

        private void UpdateDropdown(int userID)
        {
            var isSelf = IsSelf(userID);
            var showAllOptions = IsHost && !isSelf;
            _kickButton.gameObject.SetActive(showAllOptions);
            _giveHostButton.gameObject.SetActive(showAllOptions);
            _reportButton.gameObject.SetActive(showAllOptions);
            _dropdownMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(300, showAllOptions ? 240 : 60);
        }

        private void OnQuickChatOpenButtonClick()
        {

        }

        private bool IsSelf(int userID) => TootTallyUser.userInfo.id == userID;

        public void ClearAllUserRows()
        {
            _userCardsDict.Values.Do(x => GameObject.DestroyImmediate(x.gameObject));
            _userCardsDict.Clear();
        }

        public void OnReadyButtonClick()
        {
            if (!_canPressButton) return;

            switch (_userState)
            {
                case UserState.NoSong:
                    _readyUpButton.gameObject.SetActive(false);
                    controller.DownloadSavedChart(_downloadProgressBar);
                    break;
                case UserState.NotReady:
                    controller.SendUserState(UserState.Ready);
                    DisableButton(.8f);
                    break;
                case UserState.Ready:
                    controller.SendUserState(UserState.NotReady);
                    DisableButton(.8f);
                    break;
            }
        }

        public void SetHostButtonText()
        {
            if (IsHost)
                if (_readyCount == _userCardsDict.Count)
                    _startGameButton.textHolder.text = "Start Game";
                else
                    _startGameButton.textHolder.text = $"{_readyCount}/{_userCardsDict.Count} Force Start";
        }

        public void OnSettingsPromptConfirm(string name, string desc, string password, string maxPlayer)
        {
            if (!MultiplayerCreatePanel.ValidateInput(name, desc, password, maxPlayer)) return;

            controller.SendSetLobbySettings(name, desc, password, int.Parse(maxPlayer));
            TootTallyNotifManager.DisplayNotif("Sending new lobby info...");
            _lobbySettingsInputPrompt.Hide();
        }

        public void OnBackButtonClick()
        {
            if (controller.IsTransitioning) return;

            ClearAllUserRows();
            controller.DisconnectFromLobby();
            _userState = UserState.None;
        }

        public void OnSelectSongButtonClick()
        {
            if (!_canPressButton) return;

            controller.TransitionToSongSelection();
        }

        public void OnStartGameButtonClick()
        {
            DisableButton(.5f);
            controller.StartLobbyGame();
        }

        public void OnKickUserButtonClick()
        {
            HideDropdown();
            controller.KickUserFromLobby(_dropdownUserInfo.id);
            controller.RefreshCurrentLobbyInfo();
        }

        public void OnTitleTextClickOpenSongLink()
        {
            controller.OpenSongLink();
        }

        public void OnReportButtonClick()
        {
            TootTallyNotifManager.DisplayNotif("Report not implemented yet.");
        }

        public void OnProfileButtonClick()
        {
            HideDropdown();
            Application.OpenURL($"https://toottally.com/profile/{_dropdownUserInfo.id}");
        }

        public void OnTimerStart()
        {
            DisableButton();
            if (IsHost) _startGameButton.textHolder.text = "Abort Game";
        }

        public void OnTimerAbort()
        {
            EnableButton();
            if (IsHost) SetHostButtonText();
        }

        public void OnGiveHostButtonClick()
        {
            HideDropdown();

            if (!_canPressButton) return;

            _userState = UserState.None;
            controller.GiveHostUser(_dropdownUserInfo.id);
            controller.RefreshCurrentLobbyInfo();
        }

        public void OnLobbyInfoReceived(string title, int playerCount, int maxPlayer)
        {
            _titleText.text = title;
            _maxPlayerCount = maxPlayer;
            _maxPlayerText.text = $"{playerCount}/{maxPlayer}";
        }

        public void OnSongInfoChanged(string songName, float gamespeed, string modifiers, float difficulty)
        {
            _savedGameSpeed = gamespeed;

            _songNameText.text = songName;
            _gameSpeedText.text = $" <b>{gamespeed:0.00}x</b>";
            _modifiersText.text = $"M <b>{modifiers}</b>";
            _ratingText.text = $" <b>{difficulty:0.00}</b>";
            _startGameButton.gameObject.SetActive(IsHost && !controller.IsDownloadPending);
        }

        public void OnSongInfoChanged(MultiplayerSongInfo songInfo) => OnSongInfoChanged(songInfo.songName, songInfo.gameSpeed, songInfo.modifiers, songInfo.difficulty);

        public void SetTrackDataDetails(SingleTrackData trackData)
        {
            _songArtistText.text = $"{trackData.artist}";
            _songDescText.text = $"{trackData.desc}";
            _bpmText.text = $" <b>{trackData.tempo * _savedGameSpeed}</b>";

            if (_savedGameSpeed == 0)
                _savedGameSpeed = 1;

            //What the fuck am I doing??
            var time = TimeSpan.FromSeconds(trackData.length / _savedGameSpeed);
            var stringTime = $"{(time.Hours != 0 ? (time.Hours + ":") : "")}{(time.Minutes != 0 ? time.Minutes : "0")}:{(time.Seconds != 0 ? time.Seconds : "00"):00}";

            _timeText.text = $" <b>{stringTime}</b>";
        }

        private bool _isDownloadable;

        public void SetNullTrackDataDetails(bool isDownloadable)
        {
            _isDownloadable = isDownloadable;
            _readyUpButton.gameObject.SetActive(isDownloadable);
            _startGameButton.gameObject.SetActive(false);
            _songArtistText.text = $"-";
            _songDescText.text = $"-";
            _bpmText.text = $" -";
            _timeText.text = $" -";
        }

        public void OnUserStateChange(UserState state)
        {
            _userState = state;
            switch (state)
            {
                case UserState.NoSong:
                    _readyUpButton.gameObject.SetActive(!controller.IsDownloadPending && _isDownloadable);
                    _readyUpButton.textHolder.text = "Download Song";
                    break;
                case UserState.NotReady:
                    _readyUpButton.gameObject.SetActive(!IsHost);
                    _readyUpButton.textHolder.text = "Ready Up";
                    break;
                case UserState.Ready:
                    _readyUpButton.gameObject.SetActive(!IsHost);
                    _readyUpButton.textHolder.text = "Not Ready";
                    break;
            }
        }

        private void DisableButton(float delay)
        {
            _canPressButton = false;
            Plugin.Instance.StartCoroutine(DelayAllowButtonClick(delay));
        }

        public void DisableButton() { _canPressButton = false; }
        public void EnableButton() { _canPressButton = true; }

        private IEnumerator<WaitForSeconds> DelayAllowButtonClick(float delay)
        {
            yield return new WaitForSeconds(delay);
            AllowButtonClick();
        }

        private static string ColorText(string text, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";

        private void UpdateLogText(string text) => _lobbyLogText.text = text;

        private void AllowButtonClick() => _canPressButton = true;
    }
}
