using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TootTallyMultiplayer.APIService;
using UnityEngine;

namespace TootTallyMultiplayer.MultiplayerCore
{
    public static class MultiAudioController
    {
        private static float GetMaxVolume => .2f * GlobalVariables.localsettings.maxvolume_music;
        private static AudioSource _audioSource;
        private static bool _isInitialized;
        private static float _volume;

        public static bool IsPlaying => _audioSource.isPlaying;
        public static bool IsPaused;
        public static bool IsMusicLoaded;


        public static void InitMusic()
        {
            if (_isInitialized) return;

            _audioSource = Plugin.Instance.gameObject.AddComponent<AudioSource>();
            _audioSource.loop = true;
            _volume = GetMaxVolume;

            _isInitialized = true;
            IsPaused = false;
            IsMusicLoaded = false;
        }

        public static void LoadMusic(string fileName, Action OnLoadedCallback = null)
        {
            Plugin.Instance.StartCoroutine(MultiplayerAPIService.TryLoadingAudioClipLocal(fileName, clip =>
            {
                IsMusicLoaded = true;
                _audioSource.clip = clip;
                _audioSource.volume = 0;
                OnLoadedCallback?.Invoke();
            }));
        }

        public static void PlayMusicSoft(float time = .3f)
        {
            if (!GlobalVariables.menu_music) return;

            _audioSource.Play();
            LeanTween.value(0, _volume, time).setOnUpdate(v => _audioSource.volume = v);
        }

        public static void ResumeMusicSoft(float time = .3f)
        {
            if (!GlobalVariables.menu_music) return;

            IsPaused = false;
            _audioSource.UnPause();
            LeanTween.value(0, _volume, time).setOnUpdate(v => _audioSource.volume = v);
        }

        public static void StopMusicSoft(float time = .3f)
        {
            if (!GlobalVariables.menu_music) return;

            var currentVolume = _audioSource.volume;
            LeanTween.value(currentVolume, 0, time).setOnComplete(_audioSource.Stop).setOnUpdate(v => _audioSource.volume = v);
        }

        public static void PauseMusicSoft(float time = .3f)
        {
            if (!GlobalVariables.menu_music) return;

            IsPaused = true;
            var currentVolume = _audioSource.volume;
            LeanTween.value(currentVolume, 0, time).setOnComplete(_audioSource.Pause).setOnUpdate(v => _audioSource.volume = v);
        }
        
        public static void ChangeVolume(float increment)
        {
            _volume = Mathf.Clamp(_volume + increment, 0, 1);
            _audioSource.volume = _volume;
        }


    }
}
