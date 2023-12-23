using System.Collections.Generic;
using System.Linq;
using TMPro;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.Assets;
using UnityEngine;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;
using static TootTallyMultiplayer.MultiplayerSystem;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public class MultiplayerLobbyPanel : MultiplayerPanelBase
    {
        public GameObject lobbyUserContainer, rightPanelContainer, rightPanelContainerBox;
        public GameObject bottomPanelContainer;

        private List<GameObject> _userRowsList;

        private CustomButton _selectSongButton, _lobbySettingsButton, _startGameButton, _readyUpButton;

        private TMP_Text _titleText, _maxPlayerText, _hostText, _songNameText, _songDescText, _genreText, _bpmText, _gameSpeedText, _yearText, _modifiersText, _ratingText;

        private bool _isHost;

        public MultiplayerLobbyPanel(GameObject canvas, MultiplayerController controller) : base(canvas, controller, "LobbyPanel")
        {
            lobbyUserContainer = panelFG.transform.Find("TopMain/LeftPanel/LeftPanelContainer").gameObject;
            rightPanelContainer = panelFG.transform.Find("TopMain/RightPanel/RightPanelContainer").gameObject;
            rightPanelContainerBox = rightPanelContainer.transform.Find("ContainerBoxVertical").gameObject;
            bottomPanelContainer = panelFG.transform.Find("BottomMain/BottomMainContainer").gameObject;

            _userRowsList = new List<GameObject>();

            GameObjectFactory.CreateCustomButton(bottomPanelContainer.transform, Vector2.zero, new Vector2(150, 75), "Back", "LobbyBackButton", OnBackButtonClick);
            _selectSongButton = GameObjectFactory.CreateCustomButton(bottomPanelContainer.transform, Vector2.zero, new Vector2(150, 75), "SelectSong", "SelectSongButton", OnSelectSongButtonClick);
            _selectSongButton.gameObject.SetActive(false);

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
            _readyUpButton = GameObjectFactory.CreateCustomButton(buttonsHBox.transform, Vector2.zero, new Vector2(35, 35), "Ready Up", "ReadyUpButton");
        }

        public void DisplayAllUserInfo(List<MultiplayerUserInfo> users)
        {
            ClearAllUserRows();
            users.ForEach(DisplayUserInfo);

            var host = users.First();
            _isHost = host.id == TootTallyAccounts.TootTallyUser.userInfo.id;
            if (_isHost)
            {
                _lobbySettingsButton.gameObject.SetActive(true);
                _startGameButton.gameObject.SetActive(true);
                _selectSongButton.gameObject.SetActive(true);
                _readyUpButton.gameObject.SetActive(false);
            }

            _hostText.text = $"Current Host: {host.username}";

        }

        public void DisplayUserInfo(MultiplayerUserInfo user)
        {
            var lobbyInfoContainer = GameObject.Instantiate(AssetBundleManager.GetPrefab("containerboxhorizontal"), lobbyUserContainer.transform);
            _userRowsList.Add(lobbyInfoContainer);
            lobbyInfoContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 75);


            if (_isHost && user.id != TootTallyAccounts.TootTallyUser.userInfo.id)
            {
                GameObjectFactory.CreateCustomButton(lobbyInfoContainer.transform, Vector2.zero, Vector2.one * 32f, AssetManager.GetSprite("Close64.png"), $"Kick{user.username}", delegate { OnKickUserButtonClick(user.id); });
                GameObjectFactory.CreateCustomButton(lobbyInfoContainer.transform, Vector2.zero, Vector2.one * 32f, AssetManager.GetSprite("UserAdd64.png"), $"Promote{user.username}", delegate { OnPromoteButtonClick(user.id); });
            }

            var t1 = GameObjectFactory.CreateSingleText(lobbyInfoContainer.transform, $"Lobby{user.username}Name", $"{user.username}", Color.white);
            t1.alignment = TextAlignmentOptions.Left;

            var t2 = GameObjectFactory.CreateSingleText(lobbyInfoContainer.transform, $"Lobby{user.username}Rank", $"#{user.rank}", Color.white);
            t2.alignment = TextAlignmentOptions.Right;
        }

        public void ClearAllUserRows()
        {
            _userRowsList.ForEach(Object.DestroyImmediate);
            _userRowsList.Clear();
        }

        public void OnBackButtonClick()
        {
            ClearAllUserRows();
            controller.DisconnectFromLobby();
        }

        public void OnSelectSongButtonClick()
        {
            controller.TransitionToSongSelection();
        }

        public void OnStartGameButtonClick()
        {
            controller.StartLobbyGame();
        }

        public void OnKickUserButtonClick(int userID)
        {
            controller.KickUserFromLobby(userID);
        }

        public void OnPromoteButtonClick(int userID)
        {
            DisplayAllUserInfo();
            controller.PromoteUser(userID);
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
    }
}
