using System;
using System.IO;
using TootTallyMultiplayer.APIService;
using UnityEngine;
using UnityEngine.Analytics;

namespace TootTallyMultiplayer.MultiplayerCore
{
    public static class MultiAudioController
    {
        private static float GetMaxVolume => .2f * GlobalVariables.localsettings.maxvolume_music;
        private static AudioSource _audioSource;
        private static AudioClip _defaultAudio;
        private static bool _isInitialized;
        private static float _volume;

        public static bool IsPlaying => _audioSource.isPlaying;
        public static bool IsPaused;
        public static bool IsDefaultMusicLoaded;
        public static bool IsPlayingDefault => IsDefaultMusicLoaded && _audioSource.isPlaying && _audioSource.clip == _defaultAudio;
        public static bool IsMuted => Plugin.Instance.MuteMusic.Value;

        public static void InitMusic()
        {
            if (_isInitialized) return;

            _audioSource = Plugin.Instance.gameObject.AddComponent<AudioSource>();
            _audioSource.loop = true;
            _volume = GetMaxVolume;

            _isInitialized = true;
            IsPaused = false;
            IsDefaultMusicLoaded = false;
        }

        public static void LoadMusic(string fileName, Action OnLoadedCallback = null)
        {
            IsDefaultMusicLoaded = false;

            Plugin.Instance.StartCoroutine(MultiplayerAPIService.TryLoadingAudioClipLocal(fileName, clip =>
            {
                if (clip == null)
                {
                    IsDefaultMusicLoaded = false;
                    Plugin.LogError($"Music {fileName} couldn't be loaded.");
                    return;
                }
                IsDefaultMusicLoaded = true;
                _audioSource.clip = clip;
                _defaultAudio = clip;
                _audioSource.volume = 0;
                OnLoadedCallback?.Invoke();
            }));
        }

        public static void LoadClip(string path, Action OnLoadedCallback = null)
        {
            Plugin.Instance.StartCoroutine(MultiplayerAPIService.TryLoadingSongAudio(path, clip =>
            {
                _audioSource.clip = clip;
                _audioSource.volume = 0;
                OnLoadedCallback?.Invoke();
            }));
        }

        public static void PlayMusicSoft(float time = .3f)
        {
            if (IsMuted || _audioSource.clip == null) return;

            _audioSource.Play();
            LeanTween.value(0, _volume, time).setOnUpdate(v => _audioSource.volume = v);
        }

        public static void ResumeMusicSoft(float time = .3f)
        {
            if (IsMuted || _audioSource.clip == null) return;

            IsPaused = false;
            _audioSource.UnPause();
            LeanTween.value(0, _volume, time).setOnUpdate(v => _audioSource.volume = v);
        }

        public static void StopMusicHard() => _audioSource?.Stop();
        public static void PauseMusicHard() => _audioSource?.Pause();
        public static void PlayMusicHard()
        {
            if (IsMuted || _audioSource.clip == null) return;
            _audioSource.volume = _volume;
            _audioSource?.Play();
        }

        public static void StopMusicSoft(float time = .3f, Action OnComplete = null)
        {
            var currentVolume = _audioSource.volume;
            LeanTween.value(currentVolume, 0, time).setOnComplete(() =>
            {
                _audioSource.Stop();
                OnComplete?.Invoke();
            }).setOnUpdate(v => _audioSource.volume = v);
        }

        public static void PauseMusicSoft(float time = .3f)
        {
            IsPaused = true;
            var currentVolume = _audioSource.volume;
            LeanTween.value(currentVolume, 0, time).setOnComplete(_audioSource.Pause).setOnUpdate(v => _audioSource.volume = v);
        }

        public static void PauseMusicSoft(float time, Action OnComplete)
        {
            IsPaused = true;
            var currentVolume = _audioSource.volume;
            LeanTween.value(currentVolume, 0, time).setOnComplete(() =>
            {
                _audioSource.Pause();
                OnComplete?.Invoke();
            }).setOnUpdate(v => _audioSource.volume = v);
        }

        public static void ChangeVolume(float increment)
        {
            _volume = Mathf.Clamp(_volume + increment, 0, 1);
            _audioSource.volume = _volume;
        }

        public static void SetSongToDefault()
        {
            if (_audioSource.clip != _defaultAudio && _defaultAudio != null)
                _audioSource.clip = _defaultAudio;
        }

    }
}
