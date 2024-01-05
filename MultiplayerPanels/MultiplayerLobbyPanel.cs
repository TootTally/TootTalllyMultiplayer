using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TootTallyAccounts;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public class MultiplayerLobbyPanel : MultiplayerPanelBase
    {
        public GameObject lobbyUserContainer, rightPanelContainer, rightPanelContainerBox;
        public GameObject bottomPanelContainer;

        private Dictionary<int, MultiplayerCard> _userCardsDict;

        private CustomButton _selectSongButton, _lobbySettingsButton, _startGameButton, _readyUpButton;
        private CustomButton _profileButton, _giveHostButton, _kickButton;

        private TMP_Text _titleText, _maxPlayerText, _hostText, _songNameText, _songArtistText, _timeText,
            _songDescText, _genreText, _bpmText, _gameSpeedText, _yearText, _modifiersText, _ratingText;

        private ProgressBar _downloadProgressBar;

        private Slider _slider;
        private ScrollableSliderHandler _scrollingHandler;

        private GameObject _dropdownMenu, _dropdownMenuContainer;
        private MultiplayerUserInfo _dropdownUserInfo;

        private TootTallyAnimation _dropdownAnimation;

        private bool _isHost;
        private int _maxPlayerCount;
        private float _savedGameSpeed;
        private int _readyCount;
        private float _previousUserCount;

        private bool _canPressButton;

        private UserState _userState;

        public MultiplayerLobbyPanel(GameObject canvas, MultiplayerController controller) : base(canvas, controller, "LobbyPanel")
        {
            lobbyUserContainer = panelFG.transform.Find("TopMain/LeftPanel/LeftPanelContainer").gameObject;

            rightPanelContainer = panelFG.transform.Find("TopMain/RightPanel/RightPanelContainer").gameObject;
            rightPanelContainerBox = rightPanelContainer.transform.Find("ContainerBoxVertical").gameObject;
            bottomPanelContainer = panelFG.transform.Find("BottomMain/BottomMainContainer").gameObject;

            lobbyUserContainer.transform.parent.GetComponent<Image>().color = Color.black;
            lobbyUserContainer.GetComponent<VerticalLayoutGroup>().spacing = 8;

            _slider = new GameObject("ContainerSlider", typeof(Slider)).GetComponent<Slider>();
            _slider.gameObject.SetActive(true);
            _slider.onValueChanged.AddListener(value => OnSliderValueChangeScrollContainer(lobbyUserContainer, value));
            _scrollingHandler = _slider.gameObject.AddComponent<ScrollableSliderHandler>();
            _scrollingHandler.enabled = false;

            rightPanelContainer.transform.parent.GetComponent<Image>().color = Color.black;
            rightPanelContainerBox.GetComponent<Image>().color = new Color(0, 1, 0);

            _userCardsDict = new Dictionary<int, MultiplayerCard>();

            GameObjectFactory.CreateCustomButton(bottomPanelContainer.transform, Vector2.zero, new Vector2(150, 75), "Back", "LobbyBackButton", OnBackButtonClick);
            _selectSongButton = GameObjectFactory.CreateCustomButton(bottomPanelContainer.transform, Vector2.zero, new Vector2(150, 75), "SelectSong", "SelectSongButton", OnSelectSongButtonClick);
            _selectSongButton.gameObject.SetActive(false);

            //Menu when clicking on user pfp
            _dropdownMenu = MultiplayerGameObjectFactory.AddVerticalBox(panelFG.transform);

            var trigger = _dropdownMenu.AddComponent<EventTrigger>();
            EventTrigger.Entry pointerExitEvent = new EventTrigger.Entry();
            pointerExitEvent.eventID = EventTriggerType.PointerExit;
            pointerExitEvent.callback.AddListener(data => HideDropdown());
            trigger.triggers.Add(pointerExitEvent);

            _dropdownMenu.AddComponent<LayoutElement>().ignoreLayout = true;
            _dropdownMenu.GetComponent<Image>().color = Color.white;
            var rect = _dropdownMenu.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(300, 180);
            _dropdownMenuContainer = MultiplayerGameObjectFactory.AddVerticalBox(_dropdownMenu.transform);
            _profileButton = GameObjectFactory.CreateCustomButton(_dropdownMenuContainer.transform, Vector2.zero, new Vector2(295, 60), "Profile", "DropdownProfile", OnProfileButtonClick);
            _giveHostButton = GameObjectFactory.CreateCustomButton(_dropdownMenuContainer.transform, Vector2.zero, new Vector2(295, 60), "Give Host", "DropdownGiveHost", OnGiveHostButtonClick);
            _kickButton = GameObjectFactory.CreateCustomButton(_dropdownMenuContainer.transform, Vector2.zero, new Vector2(295, 60), "Kick", "DropdownKick", OnKickUserButtonClick);
            _dropdownMenu.SetActive(false);

            //Variable names my beloved
            var verticalLayout = rightPanelContainerBox.GetComponent<VerticalLayoutGroup>();
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandHeight = false;

            //TITLE
            var titleVBox = MultiplayerGameObjectFactory.AddVerticalBox(rightPanelContainerBox.transform);
            titleVBox.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = titleVBox.GetComponent<VerticalLayoutGroup>().childControlHeight = false;
            titleVBox.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 120);
            var topHBox = MultiplayerGameObjectFactory.AddHorizontalBox(titleVBox.transform);
            topHBox.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 75);

            _titleText = GameObjectFactory.CreateSingleText(topHBox.transform, "TitleText", "-", Color.white);
            _titleText.enableAutoSizing = true;
            _titleText.alignment = TextAlignmentOptions.TopLeft;
            _titleText.fontStyle = TMPro.FontStyles.Bold;
            _titleText.fontSizeMax = 60;

            _maxPlayerText = GameObjectFactory.CreateSingleText(topHBox.transform, "MaxPlayer", "-/-", Color.white);
            _maxPlayerText.alignment = TextAlignmentOptions.TopRight;
            _maxPlayerText.fontSize = 32;
            _maxPlayerText.enableWordWrapping = false;

            _hostText = GameObjectFactory.CreateSingleText(titleVBox.transform, "HostText", "Current Host: -", Color.white);
            _hostText.alignment = TextAlignmentOptions.Left;
            _hostText.enableAutoSizing = true;
            _hostText.rectTransform.sizeDelta = new Vector2(0, 30);

            //SONG INFO
            var songVBox = MultiplayerGameObjectFactory.AddVerticalBox(rightPanelContainerBox.transform);
            songVBox.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = songVBox.GetComponent<VerticalLayoutGroup>().childControlHeight = false;
            songVBox.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 360);
            _songNameText = GameObjectFactory.CreateSingleText(songVBox.transform, "SongNameText", "-", Color.white);
            _songNameText.rectTransform.sizeDelta = new Vector2(0, 60);
            _songNameText.enableAutoSizing = true;
            _songNameText.fontSizeMax = 60;
            _songNameText.fontSizeMin = 48;
            _songNameText.overflowMode = TextOverflowModes.Ellipsis;
            _songNameText.fontStyle = TMPro.FontStyles.Bold;

            _songArtistText = GameObjectFactory.CreateSingleText(songVBox.transform, "SongArtistText", "-", Color.white);
            _songArtistText.rectTransform.sizeDelta = new Vector2(0, 75);
            _songArtistText.fontSizeMax = 30;

            _songDescText = GameObjectFactory.CreateSingleText(songVBox.transform, "SongDescText", "-", Color.white);
            _songDescText.rectTransform.sizeDelta = new Vector2(0, 150);
            _songDescText.fontSizeMax = 22;

            _songArtistText.overflowMode = _songDescText.overflowMode = TextOverflowModes.Ellipsis;
            _songNameText.alignment = _songDescText.alignment = _songArtistText.alignment = TextAlignmentOptions.Left;

            //DETAILS
            var detailHBox = MultiplayerGameObjectFactory.AddHorizontalBox(rightPanelContainerBox.transform);
            var hLayout = detailHBox.GetComponent<HorizontalLayoutGroup>();
            hLayout.childForceExpandHeight = hLayout.childControlHeight = false;
            hLayout.childAlignment = TextAnchor.UpperLeft;
            detailHBox.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 200);

            //Left side
            var VBox1 = MultiplayerGameObjectFactory.AddVerticalBox(detailHBox.transform);
            VBox1.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 180);
            _genreText = GameObjectFactory.CreateSingleText(VBox1.transform, "GenreText", "Genre: -", Color.white);
            _yearText = GameObjectFactory.CreateSingleText(VBox1.transform, "YearText", "Year: -", Color.white);
            _bpmText = GameObjectFactory.CreateSingleText(VBox1.transform, "BPMText", "BPM: -", Color.white);
            _timeText = GameObjectFactory.CreateSingleText(VBox1.transform, "TimeText", "Time: -", Color.white);

            //Right side
            var VBox2 = MultiplayerGameObjectFactory.AddVerticalBox(detailHBox.transform);
            VBox2.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 145);
            VBox2.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(50, 5, 5, 5);
            _gameSpeedText = GameObjectFactory.CreateSingleText(VBox2.transform, "GameSpeedText", "Game Speed: -", Color.white);
            _modifiersText = GameObjectFactory.CreateSingleText(VBox2.transform, "ModsText", "-", Color.white);
            _ratingText = GameObjectFactory.CreateSingleText(VBox2.transform, "RatingText", "Diff: -", Color.white);

            SetTextsParameters(_timeText, _bpmText, _yearText, _genreText, _gameSpeedText, _modifiersText, _ratingText);

            //BUTTONS
            var buttonsHBox = MultiplayerGameObjectFactory.AddHorizontalBox(rightPanelContainerBox.transform);
            buttonsHBox.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 60);
            _lobbySettingsButton = GameObjectFactory.CreateCustomButton(buttonsHBox.transform, Vector2.zero, new Vector2(35, 35), "Lobby Settings", "LobbySettingsButton");
            _lobbySettingsButton.gameObject.SetActive(false);
            _startGameButton = GameObjectFactory.CreateCustomButton(buttonsHBox.transform, Vector2.zero, new Vector2(35, 35), "Start Game", "StartGameButton", OnStartGameButtonClick);
            _startGameButton.gameObject.SetActive(false);
            _readyUpButton = GameObjectFactory.CreateCustomButton(buttonsHBox.transform, Vector2.zero, new Vector2(35, 35), "Ready Up", "ReadyUpButton", OnReadyButtonClick);
            _downloadProgressBar = GameObjectFactory.CreateProgressBar(buttonsHBox.transform, Vector2.zero, new Vector2(35, 35), false, "DownloadProgressBar");
            ResetData();
        }

        public void ResetData()
        {
            _userState = UserState.None;
            DisableButton(.8f);
        }

        private void SetTextsParameters(params TMP_Text[] texts)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                var t = texts[i];
                t.fontSize = t.fontSizeMax = 32;
                t.alignment = TextAlignmentOptions.Left;
                t.enableAutoSizing = true;
                t.enableWordWrapping = false;
            }
        }

        private MultiplayerUserInfo _hostInfo;

        public void DisplayAllUserInfo(List<MultiplayerUserInfo> users)
        {
            ClearAllUserRows();

            _hostInfo = users.First();
            _isHost = _hostInfo.id == TootTallyUser.userInfo.id;
            _lobbySettingsButton.gameObject.SetActive(_isHost);
            _startGameButton.gameObject.SetActive(_isHost);
            _selectSongButton.gameObject.SetActive(_isHost);

            _readyUpButton.gameObject.SetActive(!_isHost);

            _hostText.text = $"Current Host: {_hostInfo.username}";
            _maxPlayerText.text = $"{users.Count}/{_maxPlayerCount}";
            UpdateScrolling(_userCardsDict.Count);

            users.ForEach(DisplayUserInfo);
        }

        public void UpdateScrolling(int userCount)
        {
            var enableScrolling = userCount > 9;
            if (!enableScrolling && _scrollingHandler.enabled)
            {
                _scrollingHandler.ResetAcceleration();
                _slider.value = 0;
            }
            _scrollingHandler.enabled = enableScrolling;
            _scrollingHandler.accelerationMult = enableScrolling ? 20f / userCount : 1f;

            if (_previousUserCount != 0 && _slider.value != 0 && enableScrolling)
                _slider.value *= _previousUserCount / userCount;

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

            var imageHolder = GameObjectFactory.CreateClickableImageHolder(userCard.transform, Vector2.zero, new Vector2(100, 64), AssetManager.GetSprite("icon.png"), $"PFP", () => OnUserPFPClick(user));
            imageHolder.transform.localPosition = new Vector3(-305, 0, 0);
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

            var outline = userCard.GetComponent<Outline>();

            _readyCount = _userCardsDict.Values.Where(x => x.user.state == "Ready" && !IsSelf(x.user.id)).Count() + 1;

            var color = UserStateToColor(userState);
            GameObjectFactory.TintImage(userCard.GetComponent<Image>(), color, .2f);
            outline.effectColor = color;

            if (_isHost)
                if (_readyCount == _userCardsDict.Count)
                    _startGameButton.textHolder.text = "Start Game";
                else
                    _startGameButton.textHolder.text = $"{_readyCount}/{_userCardsDict.Count} Force Start";
        }

        private float _posYJumpValue = 83f;
        private float _posYOffset = -387.5f;

        public void OnSliderValueChangeScrollContainer(GameObject container, float value)
        {
            var gridPanelRect = container.GetComponent<RectTransform>();
            gridPanelRect.anchoredPosition = new Vector2(gridPanelRect.anchoredPosition.x, value * (_userCardsDict.Count - 9f) * _posYJumpValue + _posYOffset);
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
            _dropdownMenu.transform.position = v3;
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
            var showAllOptions = _isHost && !isSelf;
            _kickButton.gameObject.SetActive(showAllOptions);
            _giveHostButton.gameObject.SetActive(showAllOptions);
            _dropdownMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(300, showAllOptions ? 180 : 60);
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

        public void OnBackButtonClick()
        {
            if (controller.IsTransitioning) return;

            ClearAllUserRows();
            controller.DisconnectFromLobby();
            _userState = UserState.NotReady;
        }

        public void OnSelectSongButtonClick()
        {
            controller.TransitionToSongSelection();
        }

        public void OnStartGameButtonClick()
        {
            controller.StartLobbyGame();
        }

        public void OnKickUserButtonClick()
        {
            HideDropdown();
            controller.KickUserFromLobby(_dropdownUserInfo.id);
            controller.RefreshCurrentLobbyInfo();
        }

        public void OnProfileButtonClick()
        {
            HideDropdown();
            Application.OpenURL($"https://toottally.com/profile/{_dropdownUserInfo.id}");
        }

        public void OnGiveHostButtonClick()
        {
            HideDropdown();
            controller.GiveHostUser(_dropdownUserInfo.id);
            controller.SendUserState(UserState.NotReady);
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
            _gameSpeedText.text = $"Game Speed: <b>{gamespeed:0.00}x</b>";
            _modifiersText.text = $"Mods: <b>{modifiers}</b>";
            _ratingText.text = $"diff: <b>{difficulty:0.00}</b>";
            _startGameButton.gameObject.SetActive(_isHost);
        }

        public void OnSongInfoChanged(MultiplayerSongInfo songInfo) => OnSongInfoChanged(songInfo.songName, songInfo.gameSpeed, songInfo.modifiers, songInfo.difficulty);

        public void SetTrackDataDetails(SingleTrackData trackData)
        {
            _songArtistText.text = $"{trackData.artist}";
            _songDescText.text = $"{trackData.desc}";
            _genreText.text = $"Genre: <b>{trackData.genre}</b>";
            _yearText.text = $"Year: <b>{trackData.year}</b>";
            _bpmText.text = $"BPM: <b>{trackData.tempo * _savedGameSpeed}</b>";

            if (_savedGameSpeed == 0)
                _savedGameSpeed = 1;

            //What the fuck am I doing??
            var time = TimeSpan.FromSeconds(trackData.length / _savedGameSpeed);
            var stringTime = $"{(time.Hours != 0 ? (time.Hours + ":") : "")}{(time.Minutes != 0 ? time.Minutes : "0")}:{(time.Seconds != 0 ? time.Seconds : "00"):00}";

            _timeText.text = $"Time: <b>{stringTime}</b>";
        }

        private bool _isDownloadable;

        public void SetNullTrackDataDetails(bool isDownloadable)
        {
            _isDownloadable = isDownloadable;
            _readyUpButton.gameObject.SetActive(isDownloadable);
            _startGameButton.gameObject.SetActive(false);
            _songArtistText.text = $"-";
            _songDescText.text = $"-";
            _genreText.text = $"Genre: -";
            _yearText.text = $"Year: -";
            _bpmText.text = $"BPM: -";
            _timeText.text = $"Time: -";
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
                    _readyUpButton.gameObject.SetActive(!_isHost);
                    _readyUpButton.textHolder.text = "Ready Up";
                    break;
                case UserState.Ready:
                    _readyUpButton.gameObject.SetActive(!_isHost);
                    _readyUpButton.textHolder.text = "Not Ready";
                    break;
            }
        }

        private void DisableButton(float delay)
        {
            _canPressButton = false;
            Plugin.Instance.StartCoroutine(DelayAllowButtonClick(delay));
        }

        private IEnumerator<WaitForSeconds> DelayAllowButtonClick(float delay)
        {
            yield return new WaitForSeconds(delay);
            AllowButtonClick();
        }

        private void AllowButtonClick() => _canPressButton = true;
    }
}
