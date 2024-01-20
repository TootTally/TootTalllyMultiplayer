using System;
using System.Collections.Generic;
using TMPro;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements.UIR;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public class MultiplayerMainPanel : MultiplayerPanelBase
    {
        public GameObject lobbyListContainer, lobbyInfoContainer;
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
        private static float _previousLobbyCount;

        public MultiplayerMainPanel(GameObject canvas, MultiplayerController controller) : base(canvas, controller, "MainLayout")
        {
            lobbyListContainer = center.transform.Find("Left/LobbyContainer").gameObject;
            lobbyInfoContainer = center.transform.Find("Right/InfoContainer").gameObject;

            _lobbyInfoRowsList = new List<GameObject>();
            _savedCodeToPing = new Dictionary<string, int>();

            GameObjectFactory.CreateClickableImageHolder(headerLeft.transform, Vector2.zero, new Vector2(72, 72), AssetManager.GetSprite("gtfo.png"), "LobbyBackButton", MultiplayerManager.ExitMultiplayer);

            var titleText = GameObjectFactory.CreateSingleText(headerCenter.transform, "TitleText", "TootTally Multiplayer");
            titleText.enableAutoSizing = true;
            var serverText = GameObjectFactory.CreateSingleText(headerRight.transform, "ServerText", "Server: Toronto");
            serverText.fontSize = 40;

            _slider = new GameObject("ContainerSlider", typeof(Slider)).GetComponent<Slider>();
            _slider.gameObject.SetActive(true);
            _slider.onValueChanged.AddListener(OnSliderValueChangeScrollContainer);
            _scrollingHandler = _slider.gameObject.AddComponent<ScrollableSliderHandler>();
            _scrollingHandler.enabled = false;

            _lobbyPlayerListText = GameObjectFactory.CreateSingleText(lobbyInfoContainer.transform, "LobbyDetailInfoText", "");
            _lobbyPlayerListText.rectTransform.sizeDelta = new Vector2(0, 680);
            _lobbyPlayerListText.enableAutoSizing = true;
            _lobbyPlayerListText.fontSizeMax = 32;
            _lobbyPlayerListText.alignment = TextAlignmentOptions.TopLeft;

            _pointerExitLobbyContainerEvent = new EventTrigger.Entry();
            _pointerExitLobbyContainerEvent.eventID = EventTriggerType.PointerExit;
            _pointerExitLobbyContainerEvent.callback.AddListener((data) => OnMouseExitClearLobbyDetails());

            _createLobbyButton = GameObjectFactory.CreateCustomButton(footer.transform, Vector2.zero, new Vector2(150, 75), "Create", "LobbyCreateButton", OnCreateLobbyButtonClick);
            _refreshLobbyButton = GameObjectFactory.CreateCustomButton(footer.transform, Vector2.zero, new Vector2(150, 75), "Refresh", "RefreshLobbyButton", OnRefreshLobbyButtonClick);

            _connectButton = GameObjectFactory.CreateCustomButton(footer.transform, Vector2.zero, new Vector2(150, 75), "Connect", "LobbyConnectButton", OnConnectButtonClick);
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
            var lobbyContainer = MultiplayerGameObjectFactory.GetHorizontalBox(new Vector2(0,120), lobbyListContainer.transform);
            lobbyContainer.GetComponent<Image>().enabled = true;
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
            var test = MultiplayerGameObjectFactory.GetVerticalBox(new Vector2(590, 0), lobbyContainer.transform);
            var tLayout = test.GetComponent<VerticalLayoutGroup>();
            tLayout.childForceExpandHeight = tLayout.childControlHeight = true;

            var t1 = GameObjectFactory.CreateSingleText(test.transform, "LobbyName", lobbyInfo.title);
            t1.fontStyle = FontStyles.Bold;
            t1.fontSize = 28;

            var t2 = GameObjectFactory.CreateSingleText(test.transform, "LobbyState", lobbyInfo.state);
            if (lobbyInfo.state == "Playing")
                t2.text += $": {lobbyInfo.songInfo.songShortName}";
            t2.fontSizeMax = 36; t2.fontSizeMin = 18;

            t1.alignment = t2.alignment = TextAlignmentOptions.Left;

            /*if (lobbyInfo.password != null)
                GameObjectFactory.CreateImageHolder(lobbyContainer.transform, Vector2.zero, Vector2.one * 64f, AssetManager.GetSprite("lock.png"), "LockedLobbyIcon");*/

            var test2 = MultiplayerGameObjectFactory.GetVerticalBox(new Vector2(590, 0), lobbyContainer.transform);
            var t2Layout = test2.GetComponent<VerticalLayoutGroup>();
            t2Layout.childForceExpandHeight = t2Layout.childControlHeight = true;

            var t3 = GameObjectFactory.CreateSingleText(test2.transform, "LobbyCount", $"{lobbyInfo.players.Count}/{lobbyInfo.maxPlayerCount}");
            t3.fontSize = 32;
            t3.fontStyle = FontStyles.Bold;

            var t4 = GameObjectFactory.CreateSingleText(test2.transform, "LobbyPing", $"-ms");
            t3.alignment = t4.alignment = TextAlignmentOptions.Right;

            if (shouldAnimate)
            {
                lobbyContainer.transform.eulerAngles = new Vector3(270, 0, 0);
                TootTallyAnimationManager.AddNewEulerAngleAnimation(lobbyContainer, Vector3.zero, 2f, new SecondDegreeDynamicsAnimation(1.25f, 1f, 1f));
            }

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

        private float _posYJumpValue = 125f;
        private float _posYOffset = 0f;

        public void OnSliderValueChangeScrollContainer(float value)
        {
            var gridPanelRect = lobbyListContainer.GetComponent<RectTransform>();
            gridPanelRect.anchoredPosition = new Vector2(gridPanelRect.anchoredPosition.x, value * (_lobbyInfoRowsList.Count - 5f) * _posYJumpValue + _posYOffset);
        }

        public void UpdateScrolling(int lobbyCount)
        {
            var enableScrolling = lobbyCount > 4;
            if (!enableScrolling && _scrollingHandler.enabled)
            {
                _scrollingHandler.ResetAcceleration();
                _slider.value = 0;
            }
            _scrollingHandler.enabled = enableScrolling;
            center.transform.Find("Left").GetComponent<HorizontalLayoutGroup>().enabled = enableScrolling; //only need this to initialize, else it causes scrolling bugs
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
                _hoveredLobbyContainer.GetComponent<Image>().color = new Color(.15f, .15f, .15f, .58f);
            }

        }

        public void OnMouseExitClearLobbyDetails()
        {
            if (_selectedLobby != null)
                OnMouseEnterDisplayLobbyDetails(_selectedLobby, _selectedLobbyContainer);
            else
                _lobbyPlayerListText.text = "";

            if (_hoveredLobbyContainer != null)
                _hoveredLobbyContainer.GetComponent<Image>().color = new Color(0, 0, 0, .58f);
            _hoveredLobbyContainer = null;
        }

        public void OnMouseClickSelectLobby(MultiplayerLobbyInfo lobbyInfo, GameObject lobbyContainer, bool animateConnect)
        {
            if (_selectedLobby == lobbyInfo) return;

            if (_selectedLobbyContainer != null)
                _selectedLobbyContainer.GetComponent<Image>().color = new Color(0, 0, 0, .58f);

            _selectedLobby = lobbyInfo;
            _lastSelectedLobby = lobbyInfo.id;
            _selectedLobbyContainer = lobbyContainer;

            _selectedLobbyContainer.GetComponent<Image>().color = new Color(0, .35f, 0, .58f);
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
