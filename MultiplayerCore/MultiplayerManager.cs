using BaboonAPI.Hooks.Tracks;
using HarmonyLib;
using System;
using System.Collections.Generic;
using TootTallyAccounts;
using TootTallyCore;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Helpers;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyGameModifiers;
using TootTallyLeaderboard.Replays;
using TootTallyMultiplayer.APIService;
using TootTallyMultiplayer.MultiplayerCore;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TootTallyMultiplayer
{
    public static class MultiplayerManager
    {
        public static bool AllowExit;

        public const string PLAYTEST_SCENE_NAME = "zzz_playtest";

        private static PlaytestAnims _currentInstance;
        private static RectTransform _multiButtonOutlineRectTransform;
        private static bool _isSceneActive;
        private static TootTallyAnimation _multiBtnAnimation, _multiTextAnimation;
        private static MultiplayerController.MultiplayerState _state, _previousState;
        private static MultiplayerController _multiController;
        private static bool _multiButtonLoaded;
        private static bool _isLevelSelectInit;
        private static bool _isRecursiveRefreshRunning;

        [HarmonyPatch(typeof(PlaytestAnims), nameof(PlaytestAnims.Start))]
        [HarmonyPostfix]
        public static void ChangePlayTestToMultiplayerScreen(PlaytestAnims __instance)
        {
            MultiplayerGameObjectFactory.Initialize();

            MultiAudioController.InitMusic();
            if (!MultiAudioController.IsMusicLoaded)
                MultiAudioController.LoadMusic("MultiplayerMusic.mp3", () => MultiAudioController.PlayMusicSoft());
            else if (MultiAudioController.IsPaused)
                MultiAudioController.ResumeMusicSoft();
            else
                MultiAudioController.PlayMusicSoft();

            _currentInstance = __instance;
            _multiController = new MultiplayerController(__instance);

            _isSceneActive = true;
            AllowExit = false;

            if (_state == MultiplayerController.MultiplayerState.SelectSong || _state == MultiplayerController.MultiplayerState.PointScene || _state == MultiplayerController.MultiplayerState.Quitting)
            {
                _multiController.UpdateLobbySongDetails();
                UpdateMultiplayerState(MultiplayerController.MultiplayerState.Lobby);
            }
            else
            {
                _previousState = MultiplayerController.MultiplayerState.None;
                UpdateMultiplayerState(MultiplayerController.MultiplayerState.Home);
                _isRecursiveRefreshRunning = true;
            }
            StartRecursiveRefresh();
        }

        public static void StartRecursiveRefresh()
        {
            _currentInstance.StartCoroutine(RecursiveLobbyRefresh());
        }

        public static void StopRecursiveRefresh()
        {
            _currentInstance.StopCoroutine(RecursiveLobbyRefresh());
        }

        private static IEnumerator<WaitForSeconds> RecursiveLobbyRefresh()
        {
            WaitForSeconds waitTime = new WaitForSeconds(5f);
            yield return waitTime;
            if (_currentInstance != null && _isRecursiveRefreshRunning)
            {
                _multiController.RefreshAllLobbyInfo();
                _currentInstance.StartCoroutine(RecursiveLobbyRefresh());
            }
        }

        [HarmonyPatch(typeof(Plugin), nameof(Plugin.Update))]
        [HarmonyPostfix]
        public static void Update()
        {
            if (!_isSceneActive) return;

            if (Input.GetKeyDown(KeyCode.Escape) && CanPressEscape())
            {
                if (_state == MultiplayerController.MultiplayerState.Home)
                {
                    if (AllowExit) //Just skip changing state if now allow, but still enter this bracket
                        UpdateMultiplayerState(MultiplayerController.MultiplayerState.ExitScene);
                }
                else if (_state == MultiplayerController.MultiplayerState.Lobby)
                    _multiController.DisconnectFromLobby();
                else
                {
                    _multiController.ReturnToLastPanel();
                    RollbackMultiplayerState();
                }
            }

            _multiController?.Update();
        }

        private static bool CanPressEscape() => !_multiController.IsTransitioning
                && _state != MultiplayerController.MultiplayerState.ExitScene
                && _state != MultiplayerController.MultiplayerState.SelectSong
                && _state != MultiplayerController.MultiplayerState.Playing
                && _state != MultiplayerController.MultiplayerState.PointScene
                && _state != MultiplayerController.MultiplayerState.Quitting;

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
            GameObject multiplayerButton = GameObject.Instantiate(__instance.btncontainers[(int)HomeScreenButtonIndexes.Collect], mainMenu.transform);
            GameObject multiplayerHitbox = GameObject.Instantiate(mainMenu.transform.Find("Button1Collect").gameObject, mainMenu.transform);
            GameObject multiplayerText = GameObject.Instantiate(__instance.paneltxts[(int)HomeScreenButtonIndexes.Collect], mainMenu.transform);
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
                if (__instance.waitforclick != 0) return;
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
            if (_currentInstance != null && _multiController.IsConnected)
            {
                _currentInstance.hidefade();
                _multiController.SendUserState(MultSerializableClasses.UserState.SelectingSong);
            }
        }
        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.clickBack))]
        [HarmonyPrefix]
        public static bool ClickBackButtonMultiplayerSelectSong(LevelSelectController __instance) => ClickPlayButtonMultiplayerSelectSong(__instance);


        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.clickPlay))]
        [HarmonyPrefix]
        public static bool ClickPlayButtonMultiplayerSelectSong(LevelSelectController __instance)
        {
            if (__instance.back_clicked) return false;
            if (_multiController == null || !_multiController.IsConnected) return true;

            var trackData = __instance.alltrackslist[__instance.songindex];
            MultiplayerController.savedTrackData = trackData;
            var trackRef = trackData.trackref;
            var track = TrackLookup.lookup(trackRef);
            var songHash = SongDataHelper.GetSongHash(track);

            GlobalVariables.levelselect_index = trackData.trackindex;
            GlobalVariables.chosen_track = trackData.trackref;
            GlobalVariables.chosen_track_data = trackData;

            _multiController.SendSongHashToLobby(songHash, ReplaySystemManager.gameSpeedMultiplier, GameModifierManager.GetModifiersString());

            __instance.back_clicked = true;
            __instance.bgmus.Stop();
            __instance.doSfx(__instance.sfx_click);

            _multiController.UpdateLobbySongDetails();
            _multiController.SendUserState(MultSerializableClasses.UserState.Ready);
            UpdateMultiplayerState(MultiplayerController.MultiplayerState.Lobby);

            __instance.fader.SetActive(true);
            __instance.fader.transform.localScale = new Vector3(9.9f, 0.001f, 1f);
            LeanTween.cancelAll();
            LeanTween.scaleY(__instance.fader, 9.75f, 0.25f).setEaseInQuart().setOnComplete(new Action(delegate
            {
                _multiController.ShowPanel();
                SceneManager.UnloadSceneAsync("levelselect");
                MultiAudioController.ResumeMusicSoft();
                _currentInstance.startBGAnims();
                _currentInstance.fadepanel.alpha = 1f;
                _currentInstance.fadepanel.gameObject.SetActive(true);
                LeanTween.alphaCanvas(_currentInstance.fadepanel, 0f, 1f).setOnComplete(new Action(_currentInstance.hidefade));
                _currentInstance.factpanel.anchoredPosition3D = new Vector3(0f, -600f, 0f);
            }));
            return false;
        }

        private static PointSceneController _currentPointSceneInstance;

        [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.Start))]
        [HarmonyPostfix]
        private static void OnPointSceneControllerStart(PointSceneController __instance)
        {
            if (_state == MultiplayerController.MultiplayerState.Playing)
            {
                _currentPointSceneInstance = __instance;
                _multiController.SendSongFinishedToLobby();
                _multiController.InitializePointScore();
                UpdateMultiplayerState(MultiplayerController.MultiplayerState.PointScene);
                __instance.btn_retry_obj.SetActive(false);
                __instance.btn_nav_cards.SetActive(false);
                __instance.btn_nav_baboon.SetActive(false);
                __instance.btn_leaderboard.SetActive(false);
            }
        }


        private static readonly string[] LETTER_GRADES = { "F", "D", "C", "B", "A", "S", "SS" };

        public static void OnMultiplayerPointScoreClick(int score, float percent, int maxCombo, int[] noteTally)
        {
            if (_currentPointSceneInstance == null) return;

            _currentPointSceneInstance.txt_score.text = score.ToString("n0");
            _currentPointSceneInstance.txt_perfectos.text = noteTally[4].ToString("n0");
            _currentPointSceneInstance.txt_nices.text = noteTally[3].ToString("n0");
            _currentPointSceneInstance.txt_okays.text = noteTally[2].ToString("n0");
            _currentPointSceneInstance.txt_mehs.text = noteTally[1].ToString("n0");
            _currentPointSceneInstance.txt_nasties.text = noteTally[0].ToString("n0");
            _currentPointSceneInstance.scorepercentage = Mathf.Max(score / GlobalVariables.gameplay_absolute_max_score, .01f);
            _currentPointSceneInstance.scoreindex = (int)Mathf.Clamp((_currentPointSceneInstance.scorepercentage / .2f) - 1, -1, 5);
            _currentPointSceneInstance.letterscore = LETTER_GRADES[_currentPointSceneInstance.scoreindex + 1];
            _currentPointSceneInstance.popScoreAnim(_currentPointSceneInstance.scoreindex + 1);

        }

        [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.clickCont))]
        [HarmonyPostfix]
        private static void OnClickContReturnToMulti(PointSceneController __instance)
        {
            if (_state == MultiplayerController.MultiplayerState.PointScene)
                __instance.scenetarget = PLAYTEST_SCENE_NAME;
        }

        [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.clickRetry))]
        [HarmonyPrefix]
        private static bool OnRetryClickPreventRetry()
        {
            if (_state == MultiplayerController.MultiplayerState.PointScene)
            {
                TootTallyNotifManager.DisplayNotif("Can't retry in multiplayer.");
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(ReplaySystemManager), nameof(ReplaySystemManager.ResolveLoadReplay))]
        [HarmonyPrefix]
        public static bool SkipResolveReplayIfInMulti()
        {
            if (_state != MultiplayerController.MultiplayerState.SelectSong) return true;

            TootTallyNotifManager.DisplayNotif("Cannot watch replays in multiplayer.");
            return false;
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.doScoreText))]
        [HarmonyPostfix]
        private static void OnDoScoreTextSendScoreToLobby(int whichtext, GameController __instance)
        {
            if (IsPlayingMultiplayer)
                _multiController.SendScoreDataToLobby(__instance.totalscore, __instance.highestcombocounter, (int)__instance.currenthealth, whichtext);
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
        [HarmonyPostfix]
        private static void OnMultiplayerGameStart(GameController __instance)
        {
            if (IsPlayingMultiplayer)
                _multiController.InitializeLiveScore();
        }

        private static bool IsPlayingMultiplayer => _multiController != null && _state == MultiplayerController.MultiplayerState.Playing;

        private static void ResolveMultiplayerState()
        {
            Plugin.LogInfo($"Multiplayer state changed from {_previousState} to {_state}");
            switch (_state)
            {
                case MultiplayerController.MultiplayerState.Home:
                    break;
                case MultiplayerController.MultiplayerState.CreatingLobby:
                    break;
                case MultiplayerController.MultiplayerState.Lobby:
                    _multiController.OnLobbyConnectionSuccess();
                    break;
                case MultiplayerController.MultiplayerState.SelectSong:
                    _currentInstance.fadepanel.alpha = 0f;
                    _currentInstance.fadepanel.gameObject.SetActive(true);
                    MultiAudioController.PauseMusicSoft();
                    LeanTween.alphaCanvas(_currentInstance.fadepanel, 1f, .25f)
                    .setOnComplete(() =>
                    {
                        SceneManager.LoadScene("levelselect", LoadSceneMode.Additive);
                        _multiController.HidePanel();
                    });
                    _currentInstance.factpanel.anchoredPosition3D = new Vector3(0f, -600f, 0f);
                    break;
                case MultiplayerController.MultiplayerState.ExitScene:
                    LeanTween.cancel(_currentInstance.fadepanel.gameObject);
                    MultiAudioController.StopMusicSoft();
                    StopRecursiveRefresh();
                    _currentInstance.clickedOK();
                    _multiController.Dispose();
                    break;
                case MultiplayerController.MultiplayerState.Playing:
                    StopRecursiveRefresh();
                    break;
            }
        }



        [HarmonyPatch(typeof(PauseCanvasController), nameof(PauseCanvasController.showPausePanel))]
        [HarmonyPrefix]
        public static bool OnGamePause(PauseCanvasController __instance)
        {
            if (!IsPlayingMultiplayer) return true;

            UpdateMultiplayerState(MultiplayerController.MultiplayerState.Quitting);
            __instance.gc.sfxrefs.backfromfreeplay.Play();
            __instance.gc.pausecanvas.SetActive(false);
            __instance.gc.curtainc.closeCurtain(false);
            LeanTween.alphaCanvas(__instance.curtaincontroller.fullfadeblack, 1f, 0.5f).setDelay(0.6f).setEaseInOutQuint().setOnComplete(() =>
            {
                __instance.curtaincontroller.unloadAssets();
                SceneManager.LoadScene(PLAYTEST_SCENE_NAME);
            });

            return false;
        }

        public static void UpdateMultiplayerState(MultiplayerController.MultiplayerState newState)
        {
            _previousState = _state;
            _state = newState;
            ResolveMultiplayerState();
        }

        public static void RollbackMultiplayerState()
        {
            var lastState = _state;
            _state = _previousState;
            _previousState = lastState;
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
