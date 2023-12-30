using System;
using System.Collections.Generic;
using TMPro;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public class MultiplayerMainPanel : MultiplayerPanelBase
    {
        public GameObject topPanelContainer;
        public GameObject lobbyListContainer, lobbyInfoContainer, lobbyConnectContainer;
        private List<GameObject> _lobbyInfoRowsList;

        private Slider _slider;
        private ScrollableSliderHandler _scrollingHandler;

        private static EventTrigger.Entry _pointerExitLobbyContainerEvent;

        private TMP_Text _lobbyPlayerListText;

        private static CustomButton _connectButton, _createLobbyButton, _refreshLobbyButton;
        private static TootTallyAnimation _connectButtonScaleAnimation;

        private static MultiplayerLobbyInfo _selectedLobby;
        private static GameObject _hoveredLobbyContainer;
        private static GameObject _selectedLobbyContainer;

        public MultiplayerMainPanel(GameObject canvas, MultiplayerController controller) : base(canvas, controller, "MainPanel")
        {
            topPanelContainer = panelFG.transform.Find("TopMain/TopMainContainer").gameObject;
            lobbyListContainer = panelFG.transform.Find("BottomMain/LeftPanel/LeftPanelContainer").gameObject;
            lobbyInfoContainer = panelFG.transform.Find("BottomMain/RightPanel/TopContainer").gameObject;
            lobbyConnectContainer = panelFG.transform.Find("BottomMain/RightPanel/BottomContainer").gameObject;

            panel.transform.localScale = Vector2.zero;

            _lobbyInfoRowsList = new List<GameObject>();

            var connectLayout = lobbyConnectContainer.GetComponent<VerticalLayoutGroup>();
            connectLayout.childControlHeight = connectLayout.childControlWidth = false;
            connectLayout.childAlignment = TextAnchor.MiddleCenter;

            var titleText = GameObjectFactory.CreateSingleText(topPanelContainer.transform, "TitleText", "TootTally Multiplayer", Color.white);
            titleText.enableAutoSizing = true;
            titleText.alignment = TextAlignmentOptions.Left;
            var serverText = GameObjectFactory.CreateSingleText(topPanelContainer.transform, "ServerText", "Server: Toronto", Color.white);
            serverText.enableAutoSizing = true;
            serverText.alignment = TextAlignmentOptions.Right;

            _slider = new GameObject("ContainerSlider", typeof(Slider)).GetComponent<Slider>();
            _slider.gameObject.SetActive(true);
            _slider.onValueChanged.AddListener((value) => controller.OnSliderValueChangeScrollContainer(lobbyListContainer, value));
            _scrollingHandler = _slider.gameObject.AddComponent<ScrollableSliderHandler>();
            _scrollingHandler.enabled = false;

            _lobbyPlayerListText = GameObjectFactory.CreateSingleText(lobbyInfoContainer.transform, "LobbyDetailInfoText", "PlaceHolder", Color.white);
            _lobbyPlayerListText.enableAutoSizing = true;
            _lobbyPlayerListText.fontSizeMax = 42;
            _lobbyPlayerListText.alignment = TextAlignmentOptions.TopLeft;

            _pointerExitLobbyContainerEvent = new EventTrigger.Entry();
            _pointerExitLobbyContainerEvent.eventID = EventTriggerType.PointerExit;
            _pointerExitLobbyContainerEvent.callback.AddListener((data) => OnMouseExitClearLobbyDetails());

            _createLobbyButton = GameObjectFactory.CreateCustomButton(lobbyConnectContainer.transform, Vector2.zero, new Vector2(150, 75), "Create", "LobbyCreateButton", OnCreateLobbyButtonClick);
            _refreshLobbyButton = GameObjectFactory.CreateCustomButton(lobbyConnectContainer.transform, Vector2.zero, new Vector2(150, 75), "Refresh", "RefreshLobbyButton", OnRefreshLobbyButtonClick);

            _connectButton = GameObjectFactory.CreateCustomButton(lobbyConnectContainer.transform, Vector2.zero, new Vector2(150, 75), "Connect", "LobbyConnectButton", OnConnectButtonClick);
            _connectButton.gameObject.SetActive(false);
        }

        public void DisplayLobby(MultiplayerLobbyInfo lobbyInfo)
        {

            var lobbyContainer = MultiplayerGameObjectFactory.AddHorizontalBox(lobbyListContainer.transform);
            _lobbyInfoRowsList.Add(lobbyContainer);
            var button = lobbyContainer.AddComponent<EventTrigger>();

            EventTrigger.Entry pointerEnterEvent = new EventTrigger.Entry();
            pointerEnterEvent.eventID = EventTriggerType.PointerEnter;
            pointerEnterEvent.callback.AddListener((data) => OnMouseEnterDisplayLobbyDetails(lobbyInfo, lobbyContainer));
            button.triggers.Add(pointerEnterEvent);

            EventTrigger.Entry pointerClickEvent = new EventTrigger.Entry();
            pointerClickEvent.eventID = EventTriggerType.PointerClick;
            pointerClickEvent.callback.AddListener((data) => OnMouseClickSelectLobby(lobbyInfo, lobbyContainer));
            button.triggers.Add(pointerClickEvent);

            button.triggers.Add(_pointerExitLobbyContainerEvent);
            var test = MultiplayerGameObjectFactory.AddVerticalBox(lobbyContainer.transform);
            var t1 = GameObjectFactory.CreateSingleText(test.transform, "LobbyName", lobbyInfo.songInfo.songShortName, Color.white);
            var t2 = GameObjectFactory.CreateSingleText(test.transform, "LobbyState", lobbyInfo.code, Color.white);
            t1.alignment = t2.alignment = TextAlignmentOptions.Left;
            var t5 = GameObjectFactory.CreateSingleText(lobbyContainer.transform, "LobbyTitle", $"{lobbyInfo.title}", Color.white);
            t5.alignment = TextAlignmentOptions.Right;

            /*if (lobbyInfo.password != null)
                GameObjectFactory.CreateImageHolder(lobbyContainer.transform, Vector2.zero, Vector2.one * 64f, AssetManager.GetSprite("lock.png"), "LockedLobbyIcon");*/

            var test2 = MultiplayerGameObjectFactory.AddVerticalBox(lobbyContainer.transform);
            var t3 = GameObjectFactory.CreateSingleText(test2.transform, "LobbyCount", $"{lobbyInfo.players.Count}/{lobbyInfo.maxPlayerCount}", Color.white);
            var t4 = GameObjectFactory.CreateSingleText(test2.transform, "LobbyPing", $"-ms", Color.white);
            t3.alignment = t4.alignment = TextAlignmentOptions.Right;
            lobbyContainer.transform.eulerAngles = new Vector3(270, 25, 0);
            TootTallyAnimationManager.AddNewEulerAngleAnimation(lobbyContainer, new Vector3(25, 25, 0), 2f, new SecondDegreeDynamicsAnimation(1.25f, 1f, 1f));

            Plugin.Instance.StartCoroutine(SendPing("68.183.206.69", ping => t4.text = $"{ping}ms"));
        }

        private static IEnumerator<WaitForSeconds> SendPing(string address, Action<int> callback)
        {
            WaitForSeconds waitTime = new WaitForSeconds(.01f);
            Ping pingSender = new Ping(address);
            while (!pingSender.isDone)
            {
                yield return waitTime;
            }
            callback(pingSender.time);
        }

        public void UpdateScrolling(int lobbyCount)
        {
            _scrollingHandler.enabled = lobbyCount > 7;
            _slider.value = 0;
        }

        public void OnMouseEnterDisplayLobbyDetails(MultiplayerLobbyInfo lobbyInfo, GameObject lobbyContainer)
        {
            _lobbyPlayerListText.text = "<u>Player List</u>\n";
            lobbyInfo.players.ForEach(u => _lobbyPlayerListText.text += $"{u.username}\n");

            if (_selectedLobbyContainer == null || _hoveredLobbyContainer != lobbyContainer && lobbyContainer != _selectedLobbyContainer)
            {
                controller.GetInstance.sfx_hover.Play();
                _hoveredLobbyContainer = lobbyContainer;
                var outline = _hoveredLobbyContainer.AddComponent<Outline>();
                outline.effectColor = new Color(.8f, .8f, .95f);
                outline.effectDistance = Vector2.one * 5f;
            }

        }

        public void OnMouseExitClearLobbyDetails()
        {
            if (_selectedLobby != null)
                OnMouseEnterDisplayLobbyDetails(_selectedLobby, _selectedLobbyContainer);
            else
                _lobbyPlayerListText.text = "";

            if (_hoveredLobbyContainer != null)
                GameObject.DestroyImmediate(_hoveredLobbyContainer.GetComponent<Outline>());
            _hoveredLobbyContainer = null;
        }

        public void OnMouseClickSelectLobby(MultiplayerLobbyInfo lobbyInfo, GameObject lobbyContainer)
        {
            if (_selectedLobby == lobbyInfo) return;

            if (_selectedLobbyContainer != null)
                GameObject.DestroyImmediate(_selectedLobbyContainer.GetComponent<Outline>());

            _selectedLobby = lobbyInfo;
            _selectedLobbyContainer = _hoveredLobbyContainer;

            var outline = _hoveredLobbyContainer.GetComponent<Outline>();
            outline.effectColor = new Color(1, 0, 0);
            outline.effectDistance = Vector2.one * 5f;
            _hoveredLobbyContainer = null;

            _connectButtonScaleAnimation?.Dispose();

            if (lobbyInfo.players.Count < lobbyInfo.maxPlayerCount)
            {
                _connectButton.gameObject.SetActive(true);
                _connectButton.transform.localScale = Vector2.zero;
                _connectButton.gameObject.GetComponent<RectTransform>().pivot = Vector2.one / 2f;
                _connectButtonScaleAnimation = TootTallyAnimationManager.AddNewScaleAnimation(_connectButton.gameObject, Vector3.one, 1f, new SecondDegreeDynamicsAnimation(2.5f, 0.98f, 1.1f));
                controller.GetInstance.sfx_hover.Play();
            }

        }

        public void ClearAllLobby()
        {
            _selectedLobby = null; _selectedLobbyContainer = null; _hoveredLobbyContainer = null;
            _connectButton.gameObject.SetActive(false);
            _lobbyInfoRowsList.ForEach(GameObject.DestroyImmediate);
            _lobbyInfoRowsList.Clear();
        }

        public void OnCreateLobbyButtonClick()
        {
            _scrollingHandler.enabled = false;
            MultiplayerManager.UpdateMultiplayerState(MultiplayerController.MultiplayerState.CreatingLobby);
            controller.MoveToCreate();
        }

        public void OnConnectButtonClick()
        {
            if (_selectedLobby == null) return;

            controller.ConnectToLobby(_selectedLobby.code);
        }

        public void OnRefreshLobbyButtonClick()
        {
            _refreshLobbyButton.gameObject.SetActive(false);
            controller.RefreshAllLobbyInfo();
        }

        public void ShowRefreshLobbyButton() => _refreshLobbyButton.gameObject.SetActive(true);
    }
}
