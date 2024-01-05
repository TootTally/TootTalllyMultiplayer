using System;
using System.Collections.Generic;
using TMPro;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements.UIR;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public class MultiplayerMainPanel : MultiplayerPanelBase
    {
        public GameObject topPanelContainer;
        public GameObject lobbyListContainer, lobbyInfoContainer, lobbyConnectContainer;
        private List<GameObject> _lobbyInfoRowsList;
        private Dictionary<string, int> _savedCodeToPing;
        private string _lastSelectedLobby;

        private Slider _slider;
        private ScrollableSliderHandler _scrollingHandler;

        private static EventTrigger.Entry _pointerExitLobbyContainerEvent;

        private TMP_Text _lobbyPlayerListText;

        private static CustomButton _connectButton, _createLobbyButton, _refreshLobbyButton;
        private static TootTallyAnimation _connectButtonScaleAnimation;

        private static MultiplayerLobbyInfo _selectedLobby;
        private static GameObject _hoveredLobbyContainer;
        private static GameObject _selectedLobbyContainer;
        private static int _previousLobbyCount;

        public MultiplayerMainPanel(GameObject canvas, MultiplayerController controller) : base(canvas, controller, "MainPanel")
        {
            topPanelContainer = panelFG.transform.Find("TopMain/TopMainContainer").gameObject;
            lobbyListContainer = panelFG.transform.Find("BottomMain/LeftPanel/LeftPanelContainer").gameObject;
            lobbyInfoContainer = panelFG.transform.Find("BottomMain/RightPanel/TopContainer").gameObject;
            lobbyConnectContainer = panelFG.transform.Find("BottomMain/RightPanel/BottomContainer").gameObject;

            panel.transform.localScale = Vector2.zero;

            _lobbyInfoRowsList = new List<GameObject>();
            _savedCodeToPing = new Dictionary<string, int>();

            panelFG.transform.Find("BottomMain/LeftPanel").GetComponent<Image>().color = new Color(0,0,0, .01f);

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
            _slider.onValueChanged.AddListener(OnSliderValueChangeScrollContainer);
            _scrollingHandler = _slider.gameObject.AddComponent<ScrollableSliderHandler>();
            _scrollingHandler.enabled = false;

            _lobbyPlayerListText = GameObjectFactory.CreateSingleText(lobbyInfoContainer.transform, "LobbyDetailInfoText", "", Color.white);
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

        public void DisplayLobbyDebug()
        {
            MultiplayerManager.StopRecursiveRefresh();
            DisplayLobby(new MultiplayerLobbyInfo() { id = "AAAAA", maxPlayerCount = 16, players = new List<MultiplayerUserInfo>(), songInfo = new MultiplayerSongInfo(), state = "SelectingSong", title = "TEST LOBBY" }, true);
            UpdateScrolling(_lobbyInfoRowsList.Count);
        }

        public void DisplayLobby(MultiplayerLobbyInfo lobbyinfo) => DisplayLobby(lobbyinfo, true);

        public void DisplayLobby(MultiplayerLobbyInfo lobbyInfo, bool shouldAnimate)
        {
            var lobbyContainer = MultiplayerGameObjectFactory.AddHorizontalBox(lobbyListContainer.transform);
            lobbyContainer.GetComponent<Image>().color = new Color(0, 1, 0, 1);
            lobbyContainer.GetComponent<HorizontalLayoutGroup>().spacing = -10; //removes the gap between the two other containers
            _lobbyInfoRowsList.Add(lobbyContainer);
            var button = lobbyContainer.AddComponent<EventTrigger>();

            EventTrigger.Entry pointerEnterEvent = new EventTrigger.Entry();
            pointerEnterEvent.eventID = EventTriggerType.PointerEnter;
            pointerEnterEvent.callback.AddListener((data) => OnMouseEnterDisplayLobbyDetails(lobbyInfo, lobbyContainer));
            button.triggers.Add(pointerEnterEvent);

            EventTrigger.Entry pointerClickEvent = new EventTrigger.Entry();
            pointerClickEvent.eventID = EventTriggerType.PointerClick;
            pointerClickEvent.callback.AddListener((data) => OnMouseClickSelectLobby(lobbyInfo, lobbyContainer, true));
            button.triggers.Add(pointerClickEvent);

            button.triggers.Add(_pointerExitLobbyContainerEvent);
            var test = MultiplayerGameObjectFactory.AddVerticalBox(lobbyContainer.transform);

            var t1 = GameObjectFactory.CreateSingleText(test.transform, "LobbyName", lobbyInfo.title, Color.white);
            t1.fontStyle = FontStyles.Bold;
            t1.fontSizeMax = 64; t1.fontSizeMin = 32;

            var t2 = GameObjectFactory.CreateSingleText(test.transform, "LobbyState", lobbyInfo.state, Color.white);
            if (lobbyInfo.state == "Playing")
                t2.text += $": {lobbyInfo.songInfo.songShortName}";
            t2.fontSizeMax = 36; t2.fontSizeMin = 18;

            t1.alignment = t2.alignment = TextAlignmentOptions.Left;
            t1.enableAutoSizing = t2.enableAutoSizing = true;

            /*if (lobbyInfo.password != null)
                GameObjectFactory.CreateImageHolder(lobbyContainer.transform, Vector2.zero, Vector2.one * 64f, AssetManager.GetSprite("lock.png"), "LockedLobbyIcon");*/

            var test2 = MultiplayerGameObjectFactory.AddVerticalBox(lobbyContainer.transform);

            var t3 = GameObjectFactory.CreateSingleText(test2.transform, "LobbyCount", $"{lobbyInfo.players.Count}/{lobbyInfo.maxPlayerCount}", Color.white);
            t3.fontSize = 32;
            t3.fontStyle = FontStyles.Bold;

            var t4 = GameObjectFactory.CreateSingleText(test2.transform, "LobbyPing", $"-ms", Color.white);
            t3.alignment = t4.alignment = TextAlignmentOptions.Right;

            if (shouldAnimate)
            {
                lobbyContainer.transform.eulerAngles = new Vector3(270, 25, 0);
                TootTallyAnimationManager.AddNewEulerAngleAnimation(lobbyContainer, new Vector3(25, 25, 0), 2f, new SecondDegreeDynamicsAnimation(1.25f, 1f, 1f));
            }
            else
                lobbyContainer.transform.eulerAngles = new Vector3(25, 25, 0);

            if (_lastSelectedLobby == lobbyInfo.id)
                OnMouseClickSelectLobby(lobbyInfo, lobbyContainer, false);

            if (_savedCodeToPing.ContainsKey(lobbyInfo.id))
                t4.text = $"{_savedCodeToPing[lobbyInfo.id]}ms";
            else
                Plugin.Instance.StartCoroutine(SendPing("68.183.206.69", ping =>
                {
                    _savedCodeToPing.Add(lobbyInfo.id, ping);
                    t4.text = $"{ping}ms";
                }));
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

        public void OnSliderValueChangeScrollContainer(float value)
        {
            var gridPanelRect = lobbyListContainer.GetComponent<RectTransform>();
            gridPanelRect.anchoredPosition = new Vector2(gridPanelRect.anchoredPosition.x, value * (_lobbyInfoRowsList.Count - 8f) * 52.5f - 440f);
        }

        public void UpdateScrolling(int lobbyCount)
        {
            var enableScrolling = lobbyCount > 7;
            if (!enableScrolling && _scrollingHandler.enabled)
            {
                _scrollingHandler.ResetAcceleration();
                _slider.value = 0;
            }
            _scrollingHandler.enabled = enableScrolling;
            _scrollingHandler.accelerationMult = enableScrolling ? 16f / lobbyCount : 1f;

            if (_previousLobbyCount != 0 && _slider.value != 0 && enableScrolling)
                _slider.value *= _previousLobbyCount / lobbyCount;

            _previousLobbyCount = lobbyCount;
        }

        public void OnMouseEnterDisplayLobbyDetails(MultiplayerLobbyInfo lobbyInfo, GameObject lobbyContainer)
        {
            _lobbyPlayerListText.text = "<u>Player List</u>\n";
            lobbyInfo.players.ForEach(u => _lobbyPlayerListText.text += $"{u.username}\n");

            if (_selectedLobbyContainer == null || _hoveredLobbyContainer != lobbyContainer && lobbyContainer != _selectedLobbyContainer)
            {
                controller.GetInstance.sfx_hover.Play();
                _hoveredLobbyContainer = lobbyContainer;
                _hoveredLobbyContainer.GetComponent<Image>().color = new Color(.8f, .8f, .95f);
            }

        }

        public void OnMouseExitClearLobbyDetails()
        {
            if (_selectedLobby != null)
                OnMouseEnterDisplayLobbyDetails(_selectedLobby, _selectedLobbyContainer);
            else
                _lobbyPlayerListText.text = "";

            if (_hoveredLobbyContainer != null)
                _hoveredLobbyContainer.GetComponent<Image>().color = new Color(0, 1f, 0f);
            _hoveredLobbyContainer = null;
        }

        public void OnMouseClickSelectLobby(MultiplayerLobbyInfo lobbyInfo, GameObject lobbyContainer, bool animateConnect)
        {
            if (_selectedLobby == lobbyInfo) return;

            if (_selectedLobbyContainer != null)
                _selectedLobbyContainer.GetComponent<Image>().color = new Color(0, 1f, 0f);

            _selectedLobby = lobbyInfo;
            _lastSelectedLobby = lobbyInfo.id;
            _selectedLobbyContainer = lobbyContainer;

            _selectedLobbyContainer.GetComponent<Image>().color = new Color(1f, 0f, 0f);
            _hoveredLobbyContainer = null;

            _connectButtonScaleAnimation?.Dispose();

            if (lobbyInfo.players.Count < lobbyInfo.maxPlayerCount)
            {
                _connectButton.gameObject.SetActive(true);
                _connectButton.gameObject.GetComponent<RectTransform>().pivot = Vector2.one / 2f;
                if (animateConnect)
                {
                    _connectButton.transform.localScale = Vector2.zero;
                    _connectButtonScaleAnimation = TootTallyAnimationManager.AddNewScaleAnimation(_connectButton.gameObject, Vector3.one, 1f, new SecondDegreeDynamicsAnimation(2.5f, 0.98f, 1.1f));
                    controller.GetInstance.sfx_hover.Play();
                }
                else
                    _connectButton.transform.localScale = Vector3.one;

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
            _lastSelectedLobby = null; _selectedLobby = null;
        }

        public void OnRefreshLobbyButtonClick()
        {
            _refreshLobbyButton.gameObject.SetActive(false);
            controller.RefreshAllLobbyInfo();
        }

        public void ShowRefreshLobbyButton() => _refreshLobbyButton.gameObject.SetActive(true);
    }
}
