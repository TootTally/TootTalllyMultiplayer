using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using UnityEngine;
using UnityEngine.UI;
using static TootTallyCore.APIServices.SerializableClass;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public class MultiplayerLobbyPanel : MultiplayerPanelBase
    {
        public GameObject lobbyUserContainer, rightPanelContainer, rightPanelContainerBox;
        public GameObject bottomPanelContainer;

        private List<GameObject> _userRowsList;

        private CustomButton _selectSongButton, _lobbySettingsButton, _startGameButton, _readyUpButton;
        private CustomButton _profileButton, _giveHostButton, _kickButton;

        private TMP_Text _titleText, _maxPlayerText, _hostText, _songNameText, _songDescText, _genreText, _bpmText, _gameSpeedText, _yearText, _modifiersText, _ratingText;

        private GameObject _dropdownMenu, _dropdownMenuContainer;
        private MultiplayerUserInfo _dropdownUserInfo;

        private TootTallyAnimation _dropdownAnimation;

        private bool _isHost;

        private UserState _userState;

        public MultiplayerLobbyPanel(GameObject canvas, MultiplayerController controller) : base(canvas, controller, "LobbyPanel")
        {
            lobbyUserContainer = panelFG.transform.Find("TopMain/LeftPanel/LeftPanelContainer").gameObject;

            rightPanelContainer = panelFG.transform.Find("TopMain/RightPanel/RightPanelContainer").gameObject;
            rightPanelContainerBox = rightPanelContainer.transform.Find("ContainerBoxVertical").gameObject;
            bottomPanelContainer = panelFG.transform.Find("BottomMain/BottomMainContainer").gameObject;

            lobbyUserContainer.transform.parent.GetComponent<Image>().color = Color.black;
            lobbyUserContainer.GetComponent<VerticalLayoutGroup>().spacing = 8;

            _userRowsList = new List<GameObject>();

            GameObjectFactory.CreateCustomButton(bottomPanelContainer.transform, Vector2.zero, new Vector2(150, 75), "Back", "LobbyBackButton", OnBackButtonClick);
            _selectSongButton = GameObjectFactory.CreateCustomButton(bottomPanelContainer.transform, Vector2.zero, new Vector2(150, 75), "SelectSong", "SelectSongButton", OnSelectSongButtonClick);
            _selectSongButton.gameObject.SetActive(false);

            //Menu when clicking on user pfp
            _dropdownMenu = MultiplayerGameObjectFactory.AddVerticalBox(panelFG.transform);
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
            var titleVBox = MultiplayerGameObjectFactory.AddVerticalBox(rightPanelContainerBox.transform);
            var topHBox = MultiplayerGameObjectFactory.AddHorizontalBox(titleVBox.transform);
            _titleText = GameObjectFactory.CreateSingleText(topHBox.transform, "TitleText", "Birthday Party", Color.white);
            _titleText.alignment = TextAlignmentOptions.TopLeft;
            _maxPlayerText = GameObjectFactory.CreateSingleText(topHBox.transform, "MaxPlayer", "2/24", Color.white);
            _maxPlayerText.alignment = TextAlignmentOptions.TopRight;
            _hostText = GameObjectFactory.CreateSingleText(titleVBox.transform, "HostText", "Current Host: Electrostats", Color.white);

            var songVBox = MultiplayerGameObjectFactory.AddVerticalBox(rightPanelContainerBox.transform);
            var t1 = GameObjectFactory.CreateSingleText(songVBox.transform, "NextSongText", "Next Song:", Color.white);
            _songNameText = GameObjectFactory.CreateSingleText(songVBox.transform, "SongNameText", "-", Color.white);
            _songDescText = GameObjectFactory.CreateSingleText(songVBox.transform, "SongDescText", "-", Color.white);
            t1.alignment = _songNameText.alignment = _songDescText.alignment = TextAlignmentOptions.Left;

            var detailVBox = MultiplayerGameObjectFactory.AddVerticalBox(rightPanelContainerBox.transform);
            var HBox1 = MultiplayerGameObjectFactory.AddHorizontalBox(detailVBox.transform);
            _genreText = GameObjectFactory.CreateSingleText(HBox1.transform, "GenreText", "Genre: -", Color.white);
            _genreText.alignment = TextAlignmentOptions.Left;
            _gameSpeedText = GameObjectFactory.CreateSingleText(HBox1.transform, "GameSpeedText", "Game Speed: -", Color.white);
            _gameSpeedText.alignment = TextAlignmentOptions.Right;

            var HBox2 = MultiplayerGameObjectFactory.AddHorizontalBox(detailVBox.transform);
            _yearText = GameObjectFactory.CreateSingleText(HBox2.transform, "YearText", "Year: -", Color.white);
            _yearText.alignment = TextAlignmentOptions.Left;
            _modifiersText = GameObjectFactory.CreateSingleText(HBox2.transform, "ModsText", "-", Color.white);
            _modifiersText.alignment = TextAlignmentOptions.Right;

            _bpmText = GameObjectFactory.CreateSingleText(detailVBox.transform, "BPMText", "BPM: -", Color.white);
            _ratingText = GameObjectFactory.CreateSingleText(detailVBox.transform, "RatingText", "Diff: -", Color.white);
            _bpmText.alignment = _ratingText.alignment = TextAlignmentOptions.Left;

            var pingHBox = MultiplayerGameObjectFactory.AddHorizontalBox(rightPanelContainerBox.transform);

            var buttonsHBox = MultiplayerGameObjectFactory.AddHorizontalBox(rightPanelContainerBox.transform);
            _lobbySettingsButton = GameObjectFactory.CreateCustomButton(buttonsHBox.transform, Vector2.zero, new Vector2(35, 35), "Lobby Settings", "LobbySettingsButton");
            _lobbySettingsButton.gameObject.SetActive(false);
            _startGameButton = GameObjectFactory.CreateCustomButton(buttonsHBox.transform, Vector2.zero, new Vector2(35, 35), "Start Game", "StartGameButton", OnStartGameButtonClick);
            _startGameButton.gameObject.SetActive(false);
            _readyUpButton = GameObjectFactory.CreateCustomButton(buttonsHBox.transform, Vector2.zero, new Vector2(35, 35), "Ready Up", "ReadyUpButton", OnReadyButtonClick);
            _userState = UserState.NotReady;
        }

        private MultiplayerUserInfo _hostInfo;

        public void DisplayAllUserInfo(List<MultiplayerUserInfo> users)
        {
            ClearAllUserRows();

            _hostInfo = users.First();
            _isHost = _hostInfo.id == TootTallyAccounts.TootTallyUser.userInfo.id;
            _lobbySettingsButton.gameObject.SetActive(_isHost);
            _startGameButton.gameObject.SetActive(_isHost);
            _selectSongButton.gameObject.SetActive(_isHost);
            _kickButton.gameObject.SetActive(_isHost);
            _giveHostButton.gameObject.SetActive(_isHost);

            _readyUpButton.gameObject.SetActive(!_isHost);

            _dropdownMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 60);

            _hostText.text = $"Current Host: {_hostInfo.username}";

            users.ForEach(DisplayUserInfo);
        }

        public void DisplayUserInfo(MultiplayerUserInfo user)
        {
            var lobbyInfoContainer = GameObject.Instantiate(AssetBundleManager.GetPrefab("containerboxhorizontal"), lobbyUserContainer.transform);
            var horizontalLayout = lobbyInfoContainer.GetComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childControlHeight = horizontalLayout.childControlWidth = false;
            horizontalLayout.childForceExpandHeight = horizontalLayout.childForceExpandWidth = false;

            _userRowsList.Add(lobbyInfoContainer);
            lobbyInfoContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(705, 75);

            var image = GameObjectFactory.CreateClickableImageHolder(lobbyInfoContainer.transform, Vector2.zero, new Vector2(90, 64), AssetManager.GetSprite("icon.png"), $"{user.username}PFP", delegate { OnUserPFPClick(user); });
            image.transform.localPosition = new Vector3(-305, 0, 0);
            AssetManager.GetProfilePictureByID(user.id, sprite => image.GetComponent<Image>().sprite = sprite);

            var t1 = GameObjectFactory.CreateSingleText(lobbyInfoContainer.transform, $"Lobby{user.username}Name", $"{user.username}", Color.white);
            t1.rectTransform.sizeDelta = new Vector2(275, 75);
            t1.alignment = TextAlignmentOptions.Left;

            var t2 = GameObjectFactory.CreateSingleText(lobbyInfoContainer.transform, $"Lobby{user.username}Rank", $"#{user.rank}", Color.white);
            t2.rectTransform.sizeDelta = new Vector2(275, 75);
            t2.alignment = TextAlignmentOptions.Right;

            var outline = lobbyInfoContainer.AddComponent<Outline>();
            outline.effectDistance = Vector2.one * 3f;

            if (user.id == _hostInfo.id)
            {
                GameObjectFactory.TintImage(lobbyInfoContainer.GetComponent<Image>(), new Color(.95f, .2f, .95f, 1), .2f);
                outline.effectColor = new Color(.95f, .2f, .95f, 1);
            }
            else
                switch (user.state != null ? Enum.Parse(typeof(UserState), user.state) : default)
                {
                    case UserState.NoSong:
                        GameObjectFactory.TintImage(lobbyInfoContainer.GetComponent<Image>(), new Color(.95f, .2f, .2f), .2f);
                        outline.effectColor = new Color(.95f, .2f, .2f, 1);
                        break;
                    case UserState.NotReady:
                        GameObjectFactory.TintImage(lobbyInfoContainer.GetComponent<Image>(), new Color(.95f, .95f, .2f, 1), .2f);
                        outline.effectColor = new Color(.95f, .95f, .2f, 1);
                        break;
                    case UserState.Ready:
                        GameObjectFactory.TintImage(lobbyInfoContainer.GetComponent<Image>(), new Color(.2f, .95f, .2f), .2f);
                        outline.effectColor = new Color(.2f, .95f, .2f, 1);
                        break;
                    default:
                        outline.effectColor = new Color(1, 1, 1, 1);
                        break;
                }
        }

        private void OnUserPFPClick(MultiplayerUserInfo user)
        {
            _dropdownUserInfo = user;
            var v3 = Input.mousePosition;
            v3.z = 0;
            _dropdownMenu.transform.position = v3;
            _dropdownMenu.transform.localScale = Vector2.zero;
            _dropdownMenu.SetActive(true);
            _dropdownAnimation?.Dispose();
            TootTallyAnimationManager.AddNewScaleAnimation(_dropdownMenu, Vector2.one, .8f, MultiplayerController.GetSecondDegreeAnimation(2.2f));
        }

        public void ClearAllUserRows()
        {
            _userRowsList.ForEach(GameObject.DestroyImmediate);
            _userRowsList.Clear();
        }

        public void OnReadyButtonClick()
        {
            switch (_userState)
            {
                case UserState.NoSong:
                    controller.DownloadSavedChart();
                    break;
                case UserState.NotReady:
                    controller.SendUserState(UserState.Ready);
                    break;
                case UserState.Ready:
                    controller.SendUserState(UserState.NotReady);
                    break;
            }
        }

        public void OnBackButtonClick()
        {
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
            controller.KickUserFromLobby(_dropdownUserInfo.id);
            controller.RefreshCurrentLobbyInfo();
        }

        public void OnProfileButtonClick()
        {
            Application.OpenURL("");
        }

        public void OnGiveHostButtonClick()
        {
            controller.GiveHostUser(_dropdownUserInfo.id);
            controller.SendUserState(UserState.NotReady);
            controller.RefreshCurrentLobbyInfo();
        }

        public void OnSongInfoChanged(string songName, float gamespeed, string modifiers)
        {
            _songNameText.text = songName;
            _gameSpeedText.text = $"Game Speed: {gamespeed:0.00}x";
            _modifiersText.text = modifiers;
        }

        public void OnSongInfoChanged(MultiplayerSongInfo songInfo) => OnSongInfoChanged(songInfo.songName, songInfo.gameSpeed, songInfo.modifiers);

        public void SetTrackDataDetails(SingleTrackData trackData)
        {
            _songDescText.text = $"{trackData.desc}";
            _genreText.text = $"Genre: {trackData.genre}";
            _yearText.text = $"Year: {trackData.year}";
            _bpmText.text = $"BPM: {trackData.tempo}";
        }

        public void SetNullTrackDataDetails()
        {
            _songDescText.text = $"-";
            _genreText.text = $"Genre: -";
            _yearText.text = $"Year: -";
            _bpmText.text = $"BPM: -";
        }

        public void OnUserStateChange(UserState state)
        {
            _userState = state;
            switch (state)
            {
                case UserState.NoSong:
                    _readyUpButton.textHolder.text = "Download Song";
                    break;
                case UserState.NotReady:
                    _readyUpButton.textHolder.text = "Ready Up";
                    break;
                case UserState.Ready:
                    _readyUpButton.textHolder.text = "Not Ready";
                    break;
            }
        }

    }
}
