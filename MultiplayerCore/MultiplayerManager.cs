using HarmonyLib;
using TootTallyAccounts;
using TootTallyCore;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.TootTallyNotifs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TootTallyMultiplayer
{
    public static class MultiplayerManager
    {
        private static PlaytestAnims _currentInstance;
        private static RectTransform _multiButtonOutlineRectTransform;
        private static bool _isSceneActive;
        private static TootTallyAnimation _multiBtnAnimation, _multiTextAnimation;
        private static MultiplayerController.MultiplayerState _state, _previousState;
        private static MultiplayerController _multiController;
        private static bool _multiButtonLoaded;

        [HarmonyPatch(typeof(PlaytestAnims), nameof(PlaytestAnims.Start))]
        [HarmonyPostfix]
        public static void ChangePlayTestToMultiplayerScreen(PlaytestAnims __instance)
        {
            MultiplayerGameObjectFactory.Initialize();
            _currentInstance = __instance;
            _multiController = new MultiplayerController(__instance);

            if (_state == MultiplayerController.MultiplayerState.SelectSong)
                UpdateMultiplayerState(MultiplayerController.MultiplayerState.Hosting);
            else
                _previousState = MultiplayerController.MultiplayerState.None;

            _isSceneActive = true;
            UpdateMultiplayerState(MultiplayerController.MultiplayerState.Enter);
        }

        [HarmonyPatch(typeof(Plugin), nameof(Plugin.Update))]
        [HarmonyPostfix]
        public static void Update()
        {
            if (!_isSceneActive) return;

            if (Input.GetKeyDown(KeyCode.Escape) && _state != MultiplayerController.MultiplayerState.ExitScene)
            {
                if (_state == MultiplayerController.MultiplayerState.CreatingLobby)
                    UpdateMultiplayerState(MultiplayerController.MultiplayerState.Home);
                else if (_state == MultiplayerController.MultiplayerState.Home)
                    UpdateMultiplayerState(MultiplayerController.MultiplayerState.ExitScene);
            }

            if (_multiController != null && _multiController.IsConnectionPending)
                _multiController.UpdateConnection();
        }

        [HarmonyPatch(typeof(PlaytestAnims), nameof(PlaytestAnims.nextScene))]
        [HarmonyPrefix]
        public static bool OverwriteNextScene()
        {
            Plugin.LogInfo("exiting multi");
            _isSceneActive = false;
            SceneManager.LoadScene("saveslot");
            return false;
        }

        [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
        [HarmonyPostfix]
        public static void OnHomeControllerStartPostFixAddMultiplayerButton(HomeController __instance)
        {
            GameObject mainCanvas = GameObject.Find("MainCanvas").gameObject;
            GameObject mainMenu = mainCanvas.transform.Find("MainMenu").gameObject;
            #region MultiplayerButton
            GameObject multiplayerButton = Object.Instantiate(__instance.btncontainers[(int)HomeScreenButtonIndexes.Collect], mainMenu.transform);
            GameObject multiplayerHitbox = Object.Instantiate(mainMenu.transform.Find("Button1Collect").gameObject, mainMenu.transform);
            GameObject multiplayerText = Object.Instantiate(__instance.paneltxts[(int)HomeScreenButtonIndexes.Collect], mainMenu.transform);
            multiplayerButton.name = "MULTIContainer";
            multiplayerHitbox.name = "MULTIButton";
            multiplayerText.name = "MULTIText";
            ThemeManager.OverwriteGameObjectSpriteAndColor(multiplayerButton.transform.Find("FG").gameObject, "MultiplayerButtonV2.png", Color.white);
            ThemeManager.OverwriteGameObjectSpriteAndColor(multiplayerText, "MultiText.png", Color.white);
            multiplayerButton.transform.SetSiblingIndex(0);
            RectTransform multiTextRectTransform = multiplayerText.GetComponent<RectTransform>();
            multiTextRectTransform.anchoredPosition = new Vector2(100, 100);
            multiTextRectTransform.sizeDelta = new Vector2(334, 87);

            _multiButtonOutlineRectTransform = multiplayerButton.transform.Find("outline").GetComponent<RectTransform>();

            multiplayerHitbox.GetComponent<Button>().onClick.AddListener(() =>
            {
                __instance.addWaitForClick();
                __instance.playSfx(3);
                if (TootTallyUser.userInfo == null || TootTallyUser.userInfo.id == 0)
                {
                    TootTallyNotifManager.DisplayNotif("Please login on TootTally to play online.", Theme.colors.notification.errorText);
                    return;
                }

                /*PopUpNotifManager.DisplayNotif("Multiplayer under maintenance...", GameTheme.themeColors.notification.errorText);
                return;*/

                //Yoinked from DNSpy KEKW
                __instance.musobj.Stop();
                __instance.quickFlash(2);
                __instance.fadeAndLoadScene(18);
                //SceneManager.MoveGameObjectToScene(GameObject.Instantiate(multiplayerButton), scene);

                //1 is HomeScreen
                //6 and 7 cards collection
                //9 is LoadController
                //10 is GameController
                //11 is PointSceneController
                //12 is some weird ass fucking notes
                //13 is intro
                //14 is boss fail animation
                //15 is how to play
                //16 is end scene
                //17 is the demo scene
            });

            EventTrigger multiBtnEvents = multiplayerHitbox.GetComponent<EventTrigger>();
            multiBtnEvents.triggers.Clear();

            EventTrigger.Entry pointerEnterEvent = new EventTrigger.Entry();
            pointerEnterEvent.eventID = EventTriggerType.PointerEnter;
            pointerEnterEvent.callback.AddListener((data) =>
            {
                _multiBtnAnimation?.Dispose();
                _multiBtnAnimation = TootTallyAnimationManager.AddNewScaleAnimation(multiplayerButton.transform.Find("outline").gameObject, new Vector2(1.01f, 1.01f), 0.5f, new SecondDegreeDynamicsAnimation(3.75f, 0.80f, 1.05f));
                _multiBtnAnimation.SetStartVector(_multiButtonOutlineRectTransform.localScale);

                _multiTextAnimation?.Dispose();
                _multiTextAnimation = TootTallyAnimationManager.AddNewScaleAnimation(multiplayerText, new Vector2(1f, 1f), 0.5f, new SecondDegreeDynamicsAnimation(3.5f, 0.65f, 1.15f));
                _multiTextAnimation.SetStartVector(multiplayerText.GetComponent<RectTransform>().localScale);

                __instance.playSfx(2); // btn sound effect KEKW
                multiplayerButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(-2, 0);
            });
            multiBtnEvents.triggers.Add(pointerEnterEvent);

            EventTrigger.Entry pointerExitEvent = new EventTrigger.Entry();
            pointerExitEvent.eventID = EventTriggerType.PointerExit;
            pointerExitEvent.callback.AddListener((data) =>
            {
                _multiBtnAnimation?.Dispose();
                _multiBtnAnimation = TootTallyAnimationManager.AddNewScaleAnimation(multiplayerButton.transform.Find("outline").gameObject, new Vector2(.4f, .4f), 0.5f, new SecondDegreeDynamicsAnimation(1.50f, 0.80f, 1.00f));
                _multiBtnAnimation.SetStartVector(_multiButtonOutlineRectTransform.localScale);

                _multiTextAnimation?.Dispose();
                _multiTextAnimation = TootTallyAnimationManager.AddNewScaleAnimation(multiplayerText, new Vector2(.8f, .8f), 0.5f, new SecondDegreeDynamicsAnimation(3.5f, 0.65f, 1.15f));
                _multiTextAnimation.SetStartVector(multiplayerText.GetComponent<RectTransform>().localScale);

                multiplayerButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(2, 0);
            });

            multiBtnEvents.triggers.Add(pointerExitEvent);
            _multiButtonLoaded = true;

            #endregion

            #region graphics

            //Play and collect buttons are programmed differently... for some reasons
            GameObject collectBtnContainer = __instance.btncontainers[(int)HomeScreenButtonIndexes.Collect];
            ThemeManager.OverwriteGameObjectSpriteAndColor(collectBtnContainer.transform.Find("FG").gameObject, "CollectButtonV2.png", Color.white);
            GameObject collectFG = collectBtnContainer.transform.Find("FG").gameObject;
            RectTransform collectFGRectTransform = collectFG.GetComponent<RectTransform>();
            collectBtnContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(900, 475.2f);
            collectBtnContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 190);
            collectFGRectTransform.sizeDelta = new Vector2(320, 190);
            GameObject collectOutline = __instance.allbtnoutlines[(int)HomeScreenButtonIndexes.Collect];
            ThemeManager.OverwriteGameObjectSpriteAndColor(collectOutline, "CollectButtonOutline.png", Color.white);
            RectTransform collectOutlineRectTransform = collectOutline.GetComponent<RectTransform>();
            collectOutlineRectTransform.sizeDelta = new Vector2(351, 217.2f);
            GameObject textCollect = __instance.allpaneltxt.transform.Find("imgCOLLECT").gameObject;
            textCollect.GetComponent<RectTransform>().anchoredPosition = new Vector2(790, 430);
            textCollect.GetComponent<RectTransform>().sizeDelta = new Vector2(285, 48);
            textCollect.GetComponent<RectTransform>().pivot = Vector2.one / 2;

            GameObject improvBtnContainer = __instance.btncontainers[(int)HomeScreenButtonIndexes.Improv];
            GameObject improvFG = improvBtnContainer.transform.Find("FG").gameObject;
            RectTransform improvFGRectTransform = improvFG.GetComponent<RectTransform>();
            improvBtnContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 156);
            improvFGRectTransform.sizeDelta = new Vector2(450, 195);
            GameObject improvOutline = __instance.allbtnoutlines[(int)HomeScreenButtonIndexes.Improv];
            RectTransform improvOutlineRectTransform = improvOutline.GetComponent<RectTransform>();
            improvOutlineRectTransform.sizeDelta = new Vector2(470, 230);
            GameObject textImprov = __instance.allpaneltxt.transform.Find("imgImprov").gameObject;
            textImprov.GetComponent<RectTransform>().anchoredPosition = new Vector2(305, 385);
            textImprov.GetComponent<RectTransform>().sizeDelta = new Vector2(426, 54);
            #endregion

            #region hitboxes
            GameObject buttonCollect = mainMenu.transform.Find("Button1Collect").gameObject;
            RectTransform buttonCollectTransform = buttonCollect.GetComponent<RectTransform>();
            buttonCollectTransform.anchoredPosition = new Vector2(739, 380);
            buttonCollectTransform.sizeDelta = new Vector2(320, 190);
            buttonCollectTransform.Rotate(0, 0, 15f);

            GameObject buttonImprov = mainMenu.transform.Find("Button3").gameObject;
            RectTransform buttonImprovTransform = buttonImprov.GetComponent<RectTransform>();
            buttonImprovTransform.anchoredPosition = new Vector2(310, 383);
            buttonImprovTransform.sizeDelta = new Vector2(450, 195);
            #endregion

        }

        [HarmonyPatch(typeof(HomeController), nameof(HomeController.Update))]
        [HarmonyPostfix]
        public static void AnimateMultiButton(HomeController __instance)
        {
            if (_multiButtonLoaded)
                _multiButtonOutlineRectTransform.transform.parent.transform.Find("FG/texholder").GetComponent<CanvasGroup>().alpha = (_multiButtonOutlineRectTransform.localScale.y - 0.4f) / 1.5f;
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Start))]
        [HarmonyPostfix]
        public static void HideBackButton(LevelSelectController __instance)
        {
            __instance.backbutton.gameObject.SetActive(_multiController == null);
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.clickPlay))]
        [HarmonyPrefix]
        public static bool ClickPlayButtonMultiplayerSelectSong(LevelSelectController __instance)
        {
            if (_multiController == null) return true;

            GlobalVariables.levelselect_index = __instance.songindex;
            __instance.back_clicked = true;
            __instance.bgmus.Stop();
            __instance.doSfx(__instance.sfx_slidedown);
            __instance.fadeOut("zzz_playtest", 0.35f);
            return false;
        }

        private static void ResolveMultiplayerState()
        {
            Plugin.LogInfo($"Multiplayer state changed from {_previousState} to {_state}");
            switch (_state)
            {
                case MultiplayerController.MultiplayerState.Enter:
                    break;
                case MultiplayerController.MultiplayerState.FirstTimePopUp:
                    break;
                case MultiplayerController.MultiplayerState.Home:
                    break;
                case MultiplayerController.MultiplayerState.CreatingLobby:
                    break;
                case MultiplayerController.MultiplayerState.Lobby:
                    break;
                case MultiplayerController.MultiplayerState.Hosting:
                    break;
                case MultiplayerController.MultiplayerState.SelectSong:
                    SceneManager.LoadScene("levelselect");
                    break;
                case MultiplayerController.MultiplayerState.ExitScene:
                    _currentInstance.clickedOK();
                    _multiController = null;
                    break;
            }
        }



        public static void UpdateMultiplayerState(MultiplayerController.MultiplayerState newState)
        {
            _previousState = _state;
            _state = newState;
            ResolveMultiplayerState();
        }

        public enum HomeScreenButtonIndexes
        {
            Play = 0,
            Collect = 1,
            Quit = 2,
            Improv = 3,
            Baboon = 4,
            Credit = 5,
            Settings = 6,
            Advanced = 7
        }

    }
}
