using System.Collections.Generic;
using TMPro;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.Assets;
using UnityEngine;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public class MultiplayerLobbyPanel : MultiplayerPanelBase
    {
        public GameObject lobbyUserContainer, rightPanelContainer, rightPanelContainerBox;
        public GameObject bottomPanelContainer;

        private List<GameObject> _userRowsList;

        private CustomButton _backButton, _selectSongButton;
        public MultiplayerLobbyPanel(GameObject canvas, MultiplayerController controller) : base(canvas, controller, "LobbyPanel")
        {
            lobbyUserContainer = panelFG.transform.Find("TopMain/LeftPanel/LeftPanelContainer").gameObject;
            rightPanelContainer = panelFG.transform.Find("TopMain/RightPanel/RightPanelContainer").gameObject;
            rightPanelContainerBox = rightPanelContainer.transform.Find("ContainerBoxVertical").gameObject;
            bottomPanelContainer = panelFG.transform.Find("BottomMain/BottomMainContainer").gameObject;

            _userRowsList = new List<GameObject>();

            _backButton = GameObjectFactory.CreateCustomButton(bottomPanelContainer.transform, Vector2.zero, new Vector2(150, 75), "Back", "LobbyBackButton", OnBackButtonClick);
            _selectSongButton = GameObjectFactory.CreateCustomButton(bottomPanelContainer.transform, Vector2.zero, new Vector2(150, 75), "SelectSong", "SelectSongButton", OnSelectSongButtonClick);

            //Variable names my beloved
            var titleVBox = MultiplayerGameObjectFactory.AddVerticalBox(rightPanelContainerBox.transform);
            var topHBox = MultiplayerGameObjectFactory.AddHorizontalBox(titleVBox.transform);
            var t1 = GameObjectFactory.CreateSingleText(topHBox.transform, "TitleText", "Birthday Party", Color.white);
            t1.alignment = TextAlignmentOptions.TopLeft;
            var t2 = GameObjectFactory.CreateSingleText(topHBox.transform, "MaxPlayer", "2/24", Color.white);
            t2.alignment = TextAlignmentOptions.TopRight;
            var t3 = GameObjectFactory.CreateSingleText(titleVBox.transform, "HostText", "Current Host: Electrostats", Color.white);

            var songVBox = MultiplayerGameObjectFactory.AddVerticalBox(rightPanelContainerBox.transform);
            var t4 = GameObjectFactory.CreateSingleText(songVBox.transform, "NextSongText", "Next Song:", Color.white);
            var t5 = GameObjectFactory.CreateSingleText(songVBox.transform, "SongNameText", "Daily Diary", Color.white);
            var t6 = GameObjectFactory.CreateSingleText(songVBox.transform, "SongDescText", "song japanese chars", Color.white);
            t4.alignment = t5.alignment = t6.alignment = TextAlignmentOptions.Left;

            var detailVBox = MultiplayerGameObjectFactory.AddVerticalBox(rightPanelContainerBox.transform);
            var HBox1 = MultiplayerGameObjectFactory.AddHorizontalBox(detailVBox.transform);
            var t7 = GameObjectFactory.CreateSingleText(HBox1.transform, "GenreText", "Genre: J-POP", Color.white);
            t7.alignment = TextAlignmentOptions.Left;
            var t8 = GameObjectFactory.CreateSingleText(HBox1.transform, "GameSpeedText", "Game Speed: 1x", Color.white);
            t8.alignment = TextAlignmentOptions.Right;

            var HBox2 = MultiplayerGameObjectFactory.AddHorizontalBox(detailVBox.transform);
            var t9 = GameObjectFactory.CreateSingleText(HBox2.transform, "YearText", "Genre: J-POP", Color.white);
            t9.alignment = TextAlignmentOptions.Left;
            var t10 = GameObjectFactory.CreateSingleText(HBox2.transform, "ModsText", "HD,FL", Color.white);
            t10.alignment = TextAlignmentOptions.Right;

            var t11 = GameObjectFactory.CreateSingleText(detailVBox.transform, "BPMText", "BPM: 172", Color.white);
            var t12 = GameObjectFactory.CreateSingleText(detailVBox.transform, "RatingText", "Diff: 4.2", Color.white);
            t11.alignment = t12.alignment = TextAlignmentOptions.Left;

            var pingHBox = MultiplayerGameObjectFactory.AddHorizontalBox(rightPanelContainerBox.transform);
            var t13 = GameObjectFactory.CreateSingleText(pingHBox.transform, "PingText", "35ms", Color.white);
            t13.alignment = TextAlignmentOptions.BottomRight;

            var buttonsHBox = MultiplayerGameObjectFactory.AddHorizontalBox(rightPanelContainerBox.transform);
            GameObjectFactory.CreateCustomButton(buttonsHBox.transform, Vector2.zero, new Vector2(35, 35), "Lobby Settings", "LobbySettingsButton");
            GameObjectFactory.CreateCustomButton(buttonsHBox.transform, Vector2.zero, new Vector2(35, 35), "Start Game", "StartGameButton");
        }

        public void DisplayAllUserInfo(List<MultiplayerUserInfo> users) => users.ForEach(DisplayUserInfo);

        public void DisplayUserInfo(MultiplayerUserInfo user)
        {
            var lobbyInfoContainer = GameObject.Instantiate(AssetBundleManager.GetPrefab("containerboxhorizontal"), lobbyUserContainer.transform);
            _userRowsList.Add(lobbyInfoContainer);
            lobbyInfoContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 75);

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
    }
}
