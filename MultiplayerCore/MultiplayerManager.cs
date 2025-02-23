using BaboonAPI.Hooks.Tracks;
using HarmonyLib;
using System;
using System.Collections.Generic;
using TootTallyAccounts;
using TootTallyCore;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Helpers;
using TootTallyCore.Utils.TootTallyGlobals;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyGameModifiers;
using TootTallyLeaderboard;
using TootTallyLeaderboard.Replays;
using TootTallyMultiplayer.APIService;
using TootTallyMultiplayer.MultiplayerCore;
using TootTallySpectator;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TootTallyMultiplayer
{
    public static class MultiplayerManager
    {
        public static readonly WaitForSeconds WAIT_TIME = new WaitForSeconds(5f);

        public static bool AllowExit;

        public const string PLAYTEST_SCENE_NAME = "zzz_playtest";
        public const string LEVELSELECT_SCENE_NAME = "levelselect";

        private static readonly string[] LETTER_GRADES = { "F", "D", "C", "B", "A", "S", "SS" };

        private static PlaytestAnims _currentInstance;
        private static PointSceneController _currentPointSceneInstance;

        private static RectTransform _multiButtonOutlineRectTransform;
        private static TootTallyAnimation _multiBtnAnimation, _multiTextAnimation;
        private static MultiplayerController.MultiplayerState _state, _previousState;
        private static MultiplayerController _multiController;
        public static MultiplayerController GetMultiplayerController => _multiController;

        private static bool _isSceneActive;
        private static bool _multiButtonLoaded;
        private static bool _isLevelSelectInit, _attemptedInitLevelSelect;

        private static bool _isRecursiveRefreshRunning;
        public static bool IsConnectedToMultiplayer => _multiController != null && _multiController.IsConnected;
        public static bool IsPlayingMultiplayer => _multiController != null && _state == MultiplayerController.MultiplayerState.Playing;

        #region Playtest Patches
        [HarmonyPatch(typeof(PlaytestAnims), nameof(PlaytestAnims.Start))]
        [HarmonyPrefix]
        public static bool OnStartPrefixLoadLevelSelectIfNotInit(PlaytestAnims __instance)
        {
            GlobalVariables.scene_destination = LEVELSELECT_SCENE_NAME;
            if (SpectatingManager.IsSpectating) SpectatingManager.StopAllSpectator();

            _currentInstance = __instance;
            if (!_isLevelSelectInit)
            {
                _attemptedInitLevelSelect = true;
                __instance.fadepanel.alpha = 1f;
                __instance.fadepanel.gameObject.SetActive(true);
                SceneManager.LoadScene(LEVELSELECT_SCENE_NAME, LoadSceneMode.Additive);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PlaytestAnims), nameof(PlaytestAnims.Start))]
        [HarmonyPostfix]
        public static void ChangePlayTestToMultiplayerScreen(PlaytestAnims __instance)
        {
            if (!_isLevelSelectInit) return;

            Camera.main.transform.localPosition = Vector3.zero;

            __instance.logo_trect.gameObject.SetActive(false);
            __instance.logo_crect.gameObject.SetActive(false);

            GameModifierManager.ClearAllModifiers();
            MultiplayerGameObjectFactory.Initialize();
            MultiAudioController.InitMusic();

            if (!MultiAudioController.IsDefaultMusicLoaded)
                MultiAudioController.LoadMusic("MultiplayerMusic.mp3", () => MultiAudioController.PlayMusicSoft());
            else if (MultiAudioController.IsPaused)
                MultiAudioController.ResumeMusicSoft();
            else
                MultiAudioController.PlayMusicSoft();

            _multiController = new MultiplayerController(__instance);

            _isSceneActive = true;
            AllowExit = false;

            if (_multiController.IsConnected && (_state == MultiplayerController.MultiplayerState.SelectSong ||
                                                 _state == MultiplayerController.MultiplayerState.PointScene ||
                                                 _state == MultiplayerController.MultiplayerState.Quitting))
            {
                UpdateMultiplayerState(MultiplayerController.MultiplayerState.Lobby);
                // Force reset to NotReady since it could be stuck in Viewing Score
                // TODO: Fix bug here where if the user doesn't have the song, it's still
                //       set as not ready. -gristCollector
                _multiController.SendUserState(MultSerializableClasses.UserState.NotReady);
                _multiController.UpdateLobbySongDetails();
            }
            else
            {
                _previousState = MultiplayerController.MultiplayerState.None;
                UpdateMultiplayerState(MultiplayerController.MultiplayerState.Home);
                StartRecursiveRefresh();
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
                    ExitMultiplayer();
                else if (_state == MultiplayerController.MultiplayerState.Lobby)
                    _multiController.DisconnectFromLobby();
                else
                {
                    _multiController.ReturnToLastPanel();
                    RollbackMultiplayerState();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Minus) && _state != MultiplayerController.MultiplayerState.Playing)
                MultiAudioController.ChangeVolume(-.01f);
            else if (Input.GetKeyDown(KeyCode.Equals) && _state != MultiplayerController.MultiplayerState.Playing)
                MultiAudioController.ChangeVolume(.01f);

            _multiController?.Update();
        }

        private static bool CanPressEscape() => !_multiController.IsTransitioning
                && !_multiController.IsRequestPending
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
        #endregion

        #region Homescreen Patches
        [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
        [HarmonyPostfix]
        public static void OnHomeControllerStartPostFixAddMultiplayerButton(HomeController __instance)
        {
            GameObject mainCanvas = GameObject.Find("MainCanvas").gameObject;
            GameObject mainMenu = mainCanvas.transform.Find("MainMenu").gameObject;
            MultiplayerGameObjectFactory.SetTogglePrefab(__instance);

            #region MultiplayerButton
            GameObject multiplayerButton = GameObject.Instantiate(__instance.btncontainers[(int)HomeScreenButtonIndexes.Collect], mainMenu.transform);
            GameObject multiplayerHitbox = GameObject.Instantiate(mainMenu.transform.Find("Button1Collect").gameObject, mainMenu.transform);
            GameObject multiplayerText = GameObject.Instantiate(__instance.paneltxts[(int)HomeScreenButtonIndexes.Collect], mainMenu.transform);
            multiplayerButton.name = "MULTIContainer";
            multiplayerHitbox.name = "MULTIButton";
            multiplayerText.name = "MULTIText";
            GameObject.DestroyImmediate(multiplayerText.transform.GetChild(1).GetComponent<LocalizeStringEvent>());
            multiplayerText.transform.GetChild(1).GetComponent<Text>().text = multiplayerText.transform.GetChild(1).GetChild(0).GetComponent<Text>().text = "<i>MULTI</i>";
            ThemeManager.OverwriteGameObjectSpriteAndColor(multiplayerButton.transform.Find("FG").gameObject, "MultiplayerButtonV2.png", Color.white);
            multiplayerButton.transform.SetSiblingIndex(0);
            multiplayerText.transform.SetSiblingIndex(21);
            multiplayerHitbox.transform.SetSiblingIndex(22);
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
                    TootTallyNotifManager.DisplayError("Please login on TootTally to play online.");
                    return;
                }

                /*PopUpNotifManager.DisplayNotif("Multiplayer under maintenance...", GameTheme.themeColors.notification.errorText);
                return;*/

                //Yoinked from DNSpy KEKW
                Plugin.LogInfo("Entering Multiplayer...");
                __instance.musobj.Stop();
                __instance.quickFlash(2);
                LeanTween.scale(__instance.fullcanvas, new Vector3(0.001f, 0.001f, 1f), 0.5f).setEaseInQuart();
                __instance.screenfade.alpha = 0f;
                LeanTween.alphaCanvas(__instance.screenfade, 1f, 0.45f).setDelay(0.25f).setOnComplete(new Action(LoadPlayTestScene));
                //SceneManager.MoveGameObjectToScene(GameObject.Instantiate(multiplayerButton), scene);
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
            GameObject textCollect = __instance.paneltxts[(int)HomeScreenButtonIndexes.Collect];
            textCollect.transform.GetChild(1).localScale = Vector3.one * .6f;
            textCollect.GetComponent<RectTransform>().anchoredPosition = new Vector2(790, 430);
            textCollect.GetComponent<RectTransform>().sizeDelta = new Vector2(285, 48);
            textCollect.GetComponent<RectTransform>().pivot = Vector2.one / 2;

            GameObject improvBtnContainer = __instance.btncontainers[(int)HomeScreenButtonIndexes.Improv];
            GameObject improvFG = improvBtnContainer.transform.Find("FG").gameObject;
            ThemeManager.OverwriteGameObjectSpriteAndColor(improvFG, "ImprovButtonV2.png", Color.white);
            RectTransform improvFGRectTransform = improvFG.GetComponent<RectTransform>();
            improvBtnContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 156);
            improvFGRectTransform.sizeDelta = new Vector2(450, 190);
            GameObject improvOutline = __instance.allbtnoutlines[(int)HomeScreenButtonIndexes.Improv];
            ThemeManager.OverwriteGameObjectSpriteAndColor(improvOutline, "ImprovButtonOutline.png", Color.white);
            RectTransform improvOutlineRectTransform = improvOutline.GetComponent<RectTransform>();
            improvOutlineRectTransform.sizeDelta = new Vector2(480, 220);
            GameObject txtContainer = __instance.paneltxts[(int)HomeScreenButtonIndexes.Improv];
            var improvIcon = txtContainer.transform.GetChild(0);
            var improvTxt = txtContainer.transform.GetChild(1);
            improvIcon.localScale = improvTxt.localScale = Vector3.one * .6f;
            improvIcon.localPosition = new Vector2(-196f, 0);
            improvTxt.localPosition = new Vector2(42, 25f);
            txtContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(305, 385);
            txtContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(426, 54);
            #endregion

            #region hitboxes
            GameObject buttonCollect = mainMenu.transform.Find("Button1Collect").gameObject;
            RectTransform buttonCollectTransform = buttonCollect.GetComponent<RectTransform>();
            buttonCollectTransform.anchoredPosition = new Vector2(739, 380);
            buttonCollectTransform.sizeDelta = new Vector2(320, 190);
            buttonCollectTransform.Rotate(0, 0, 15f);

            GameObject buttonImprov = mainMenu.transform.Find("Button3FreeImprov").gameObject;
            RectTransform buttonImprovTransform = buttonImprov.GetComponent<RectTransform>();
            buttonImprovTransform.anchoredPosition = new Vector2(310, 383);
            buttonImprovTransform.sizeDelta = new Vector2(450, 195);
            #endregion

        }

        private static void LoadPlayTestScene() => SceneManager.LoadScene(PLAYTEST_SCENE_NAME);

        [HarmonyPatch(typeof(HomeController), nameof(HomeController.Update))]
        [HarmonyPostfix]
        public static void AnimateMultiButton(HomeController __instance)
        {
            if (_multiButtonLoaded)
                _multiButtonOutlineRectTransform.transform.parent.transform.Find("FG/texholder").GetComponent<CanvasGroup>().alpha = (_multiButtonOutlineRectTransform.localScale.y - 0.4f) / 1.5f;
        }
        #endregion

        #region Compatibility Patches
        [HarmonyPatch(typeof(GlobalLeaderboardManager), nameof(GlobalLeaderboardManager.OnLevelSelectControllerStartPostfix))]
        [HarmonyPrefix]
        public static bool PreventGlobalLeaderboardFromLoadingWhenInitMulti()
        {
            if (_attemptedInitLevelSelect)
            {
                _attemptedInitLevelSelect = false;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(GameObjectFactory), nameof(GameObjectFactory.OnLevelSelectControllerInitialize))]
        [HarmonyPostfix]
        public static void ArtificiallyInitLevelSelectOnFirstMultiplayerEnter(LevelSelectController levelSelectController)
        {
            if (!_isLevelSelectInit)
            {
                Plugin.LogInfo("levelselect init success");
                _isLevelSelectInit = true;

                if (_currentInstance != null)
                {
                    LeanTween.cancelAll();
                    SceneManager.UnloadSceneAsync(LEVELSELECT_SCENE_NAME);
                    _currentInstance.Start();
                }
            }
        }

        [HarmonyPatch(typeof(ReplaySystemManager), nameof(ReplaySystemManager.ResolveLoadReplay))]
        [HarmonyPrefix]
        public static bool SkipResolveReplayIfInMulti()
        {
            if (_state != MultiplayerController.MultiplayerState.SelectSong) return true;

            TootTallyNotifManager.DisplayNotif("Cannot watch replays in multiplayer.");
            return false;
        }
        #endregion

        #region LevelSelect Patches
        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Start))]
        [HarmonyPostfix]
        public static void HideBackButton(LevelSelectController __instance)
        {
            if (IsConnectedToMultiplayer)
            {
                _currentInstance.hidefade();
                _multiController.SendUserState(MultSerializableClasses.UserState.SelectingSong);
            }
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.clickBack))]
        [HarmonyPrefix]
        public static bool ClickBackButtonMultiplayerSelectSong(LevelSelectController __instance)
        {
            if (!IsConnectedToMultiplayer) return true;

            OnMultiplayerSelectSongExit(__instance);
            return false;
        }


        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.clickPlay))]
        [HarmonyPrefix]
        public static bool ClickPlayButtonMultiplayerSelectSong(LevelSelectController __instance)
        {
            if (__instance.back_clicked) return false;
            if (!IsConnectedToMultiplayer) return true;

            var trackData = __instance.alltrackslist[__instance.songindex];
            MultiplayerController.savedTrackData = trackData;
            var trackRef = trackData.trackref;
            var track = TrackLookup.lookup(trackRef);
            var songHash = SongDataHelper.GetSongHash(track);

            GlobalVariables.levelselect_index = trackData.trackindex;
            GlobalVariables.chosen_track = trackData.trackref;
            GlobalVariables.chosen_track_data = trackData;

            _multiController.SendSongHashToLobby(songHash, TootTallyGlobalVariables.gameSpeedMultiplier, GameModifierManager.GetModifiersString());

            OnMultiplayerSelectSongExit(__instance);
            return false;
        }


        private static void OnMultiplayerSelectSongExit(LevelSelectController __instance)
        {
            _multiController.SendUserState(MultSerializableClasses.UserState.Ready);
            UpdateMultiplayerState(MultiplayerController.MultiplayerState.Lobby);
            __instance.back_clicked = true;
            __instance.bgmus.Stop();
            __instance.doSfx(__instance.sfx_click);
            __instance.fader.SetActive(true);
            __instance.fader.transform.localScale = new Vector3(9.9f, 0.001f, 1f);
            LeanTween.cancelAll();
            LeanTween.scaleY(__instance.fader, 9.75f, 0.25f).setEaseInQuart().setOnComplete(new Action(delegate
            {
                _multiController.ShowPanel();
                _multiController.ShowMute();
                SceneManager.UnloadSceneAsync(LEVELSELECT_SCENE_NAME);
                MultiAudioController.ResumeMusicSoft();
                _currentInstance.startBGAnims();
                _currentInstance.fadepanel.alpha = 1f;
                _currentInstance.fadepanel.gameObject.SetActive(true);
                LeanTween.alphaCanvas(_currentInstance.fadepanel, 0f, 1f).setOnComplete(new Action(_currentInstance.hidefade));
                _currentInstance.factpanel.anchoredPosition3D = new Vector3(0f, -600f, 0f);
            }));
        }

        //This is so dumb lmfao
        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.hoverPlay))]
        [HarmonyPrefix]
        public static bool PreventHoverPlayLeanTweenWhenTransitioning(LevelSelectController __instance) => IsConnectedToMultiplayer && !__instance.back_clicked;

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.unHoverPlay))]
        [HarmonyPrefix]
        public static bool PreventUnhoverPlayLeanTweenWhenTransitioning(LevelSelectController __instance) => IsConnectedToMultiplayer && !__instance.back_clicked;
        #endregion

        [HarmonyPatch(typeof(LoadController), nameof(LoadController.Start))]
        [HarmonyPostfix]
        public static void StopMusicForWhateverFuckingReasons() 
        {
            if (IsConnectedToMultiplayer)
                MultiAudioController.StopMusicHard();
        }

        #region PointScene Patches
        [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.Start))]
        [HarmonyPostfix]
        private static void OnPointSceneControllerStart(PointSceneController __instance)
        {
            if (_state == MultiplayerController.MultiplayerState.Playing)
            {
                _currentPointSceneInstance = __instance;
                _multiController.SendSongFinishedToLobby();
                _multiController.InitializePointScore();
                _multiController.SendUserState(MultSerializableClasses.UserState.ViewingScore);
                UpdateMultiplayerState(MultiplayerController.MultiplayerState.PointScene);
                __instance.btn_retry_obj.SetActive(false);
                __instance.btn_nav_cards.SetActive(false);
                __instance.btn_nav_baboon.SetActive(false);
                __instance.btn_leaderboard.SetActive(false);
            }
        }

        public static void OnMultiplayerPointScoreClick(int score, float percent, int maxCombo, int[] noteTally)
        {
            if (_currentPointSceneInstance == null) return;

            _currentPointSceneInstance.txt_score.text = $"{score}:n0 {percent:0.00}%";
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

        [HarmonyPatch(typeof(GameController), nameof(GameController.pauseRetryLevel))]
        [HarmonyPrefix]
        private static bool PreventRetryInMultiplayer()
        {
            if (_multiController != null && _state == MultiplayerController.MultiplayerState.Quitting)
            {
                TootTallyNotifManager.DisplayNotif("Can't quick retry in multiplayer.");
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.pauseQuitLevel))]
        [HarmonyPrefix]
        private static bool PreventQuickQuittingInMultiplayer()
        {
            if (_multiController != null && _state == MultiplayerController.MultiplayerState.Quitting)
            {
                TootTallyNotifManager.DisplayNotif("Can't fast quit in multiplayer.");
                return false;
            }
            return true;
        }
        #endregion

        #region GameController Patches
        [HarmonyPatch(typeof(GameController), nameof(GameController.doScoreText))]
        [HarmonyPostfix]
        private static void OnDoScoreTextSendScoreToLobby(int whichtext, GameController __instance)
        {
            if (!IsPlayingMultiplayer || !IsConnectedToMultiplayer || _wasAutotootUsed) return;

            if (!_wasAutotootUsed && __instance.controllermode)
            {
                _wasAutotootUsed = true;
                _multiController.SendQuitFlag();
            }
            else
                _multiController.SendScoreDataToLobby(__instance.totalscore, __instance.highestcombocounter, (int)__instance.currenthealth, whichtext);
        }

        private static bool _wasAutotootUsed;

        [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
        [HarmonyPostfix]
        private static void OnMultiplayerGameStart(GameController __instance)
        {
            _wasAutotootUsed = false;
            if (IsPlayingMultiplayer)
                _multiController.OnGameControllerStartSetup();
        }

        private static bool _isSyncing = true;
        private static float _syncTimeoutTimer;

        [HarmonyPatch(typeof(GameController), nameof(GameController.Update))]
        [HarmonyPostfix]
        private static void OnMultiplayerUpdateWaitForSync(GameController __instance)
        {
            if (_isSyncing)
            {
                if (IsPlayingMultiplayer && (!_multiController.IsAnybodyLoading || _syncTimeoutTimer > 10f))
                    __instance.startSong(true);
                else
                    _syncTimeoutTimer += Time.deltaTime;
            }

            if (IsPlayingMultiplayer)
                __instance.restarttimer = 0.01f;
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.startSong))]
        [HarmonyPrefix]
        private static bool OnMultiplayerWaitForSync()
        {
            if (IsPlayingMultiplayer && _multiController.IsAnybodyLoading && _syncTimeoutTimer < 10f)
            {
                _isSyncing = true;
                _syncTimeoutTimer = 0;
                _multiController.OnGameControllerStartSongSendReadyState();
                TootTallyNotifManager.DisplayNotif("Waiting for all players to load...");
                return false;
            }
            _syncTimeoutTimer = 0;
            _isSyncing = false;
            return true;
        }


        [HarmonyPatch(typeof(PauseCanvasController), nameof(PauseCanvasController.showPausePanel))]
        [HarmonyPrefix]
        public static bool OnGamePause(PauseCanvasController __instance)
        {
            if (!IsPlayingMultiplayer) return true;
            UpdateMultiplayerState(MultiplayerController.MultiplayerState.Quitting);
            var gameController = __instance.gc;
            gameController.paused = true;
            gameController.quitting = true;
            gameController.sfxrefs.backfromfreeplay.Play();
            gameController.pausecanvas.SetActive(false);
            gameController.curtainc.closeCurtain(false);
            LeanTween.alphaCanvas(__instance.curtaincontroller.fullfadeblack, 1f, 0.5f).setDelay(0.6f).setEaseInOutQuint().setOnComplete(() =>
            {
                __instance.curtaincontroller.unloadAssets();
                SceneManager.LoadScene(PLAYTEST_SCENE_NAME);
            });

            return false;
        }

        #endregion

        #region SpectatorPatches
        [HarmonyPatch(typeof(SpectatingManager), nameof(SpectatingManager.OnSpectateButtonPress))]
        [HarmonyPrefix]
        public static bool PreventSpectatingWhileInMultiplayer()
        {
            if (IsPlayingMultiplayer || IsConnectedToMultiplayer || _isSceneActive)
            {
                TootTallyNotifManager.DisplayNotif("Cannot spectate someone while in multiplayer.");
                return false;
            }
            return true;
        }
        #endregion

        #region GameModifiers Patches
        [HarmonyPatch(typeof(GameModifierManager), nameof(GameModifierManager.Toggle))]
        [HarmonyPrefix]
        public static bool PreventToggles(GameModifiers.ModifierType modifierType)
        {
            if (!IsPlayingMultiplayer && !IsConnectedToMultiplayer && !_isSceneActive) return true;
            if (modifierType == GameModifiers.ModifierType.Hidden && _multiController.IsFreemod)
            {
                TootTallyNotifManager.DisplayNotif("Cannot enable Hidden with freemod.");
                return false;
            }
            if (modifierType == GameModifiers.ModifierType.Flashlight && _multiController.IsFreemod)
            {
                TootTallyNotifManager.DisplayNotif("Cannot enable Flashlight with freemod.");
                return false;
            }

            if (modifierType == GameModifiers.ModifierType.Brutal)
            {
                TootTallyNotifManager.DisplayNotif("Cannot enable Brutal Mode in multiplayer.");
                return false;
            }
            else if (modifierType == GameModifiers.ModifierType.InstaFail)
            {
                TootTallyNotifManager.DisplayNotif("Cannot enable InstaFail in multiplayer.");
                return false;
            }

            return true;
        }
        #endregion

        #region MultiplayerState
        public static void ExitMultiplayer()
        {
            if (!AllowExit) return;
            UpdateMultiplayerState(MultiplayerController.MultiplayerState.ExitScene);
        }

        public static void UpdateMultiplayerStateIfChanged(MultiplayerController.MultiplayerState newState)
        {
            if (_state == newState) return;

            _previousState = _state;
            _state = newState;
            ResolveMultiplayerState();
        }

        public static void UpdateMultiplayerState(MultiplayerController.MultiplayerState newState)
        {
            _previousState = _state;
            _state = newState;
            ResolveMultiplayerState();
        }

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
                        SceneManager.LoadScene(LEVELSELECT_SCENE_NAME, LoadSceneMode.Additive);
                        _multiController.HidePanel();
                    });
                    _currentInstance.factpanel.anchoredPosition3D = new Vector3(0f, -600f, 0f);
                    break;
                case MultiplayerController.MultiplayerState.ExitScene:
                    LeanTween.cancel(_currentInstance.fadepanel.gameObject);
                    MultiAudioController.StopMusicSoft();
                    StopRecursiveRefresh();
                    _multiController.Dispose();
                    LeaveScene();
                    break;
                case MultiplayerController.MultiplayerState.Playing:
                    StopRecursiveRefresh();
                    break;
                case MultiplayerController.MultiplayerState.Quitting:
                    if (!_wasAutotootUsed)
                        _multiController.SendQuitFlag();
                    _multiController.OnSongQuit();
                    break;
                case MultiplayerController.MultiplayerState.PointScene:
                    break;
            }
        }

        private static void LeaveScene()
        {
            if (TootTallyUser.userInfo.id == 8) //If emmett
            {
                _currentInstance.nextScene();
                return;
            }

            _currentInstance.sfx_ok.Play();
            _currentInstance.fadepanel.gameObject.SetActive(true);
            LeanTween.alphaCanvas(_currentInstance.fadepanel, 1f, 0.4f).setOnComplete(new Action(_currentInstance.nextScene));
        }
        #endregion

        #region Utils
        public static void RollbackMultiplayerState()
        {
            var lastState = _state;
            _state = _previousState;
            _previousState = lastState;
            ResolveMultiplayerState();
        }

        public static void StartRecursiveRefresh()
        {
            if (_isRecursiveRefreshRunning) return;
            _isRecursiveRefreshRunning = true;
            _currentInstance.StartCoroutine(RecursiveLobbyRefresh());
        }

        public static void StopRecursiveRefresh()
        {
            if (!_isRecursiveRefreshRunning) return;
            _isRecursiveRefreshRunning = false;
            _currentInstance.StopCoroutine(RecursiveLobbyRefresh());
        }

        private static IEnumerator<WaitForSeconds> RecursiveLobbyRefresh()
        {
            yield return WAIT_TIME;
            if (_currentInstance != null && _isRecursiveRefreshRunning)
            {
                _multiController.RefreshAllLobbyInfo();
                _currentInstance.StartCoroutine(RecursiveLobbyRefresh());
            }
        }
        #endregion

        #region DEBUG
        public static void DebugFakeLobby() => _multiController?.DebugFakeLobby();
        public static void DebugFakeUser() => _multiController?.DebugFakeUser();
        #endregion
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
