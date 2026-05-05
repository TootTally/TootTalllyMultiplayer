using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static AudioClip _joinSound, _leaveSound;
        private static bool _isInitialized;
        private static float _volume;

        public static bool IsPlaying => _audioSource.isPlaying;
        public static bool IsPaused;
        public static bool IsMusicLoaded;
        public static bool IsPlayingDefault => IsMusicLoaded && _audioSource.isPlaying && _audioSource.clip == _defaultAudio;
        public static bool IsTransitioning;
        public static bool IsMuted => Plugin.Instance.MuteMusic.Value;
        private static string[] _musicFileNames =
        {
            "MultiplayerMusic.mp3",
            "MultiplayerMusic2.mp3"
        };
        private static int _currentMusicIndex;

        public static void InitMusic()
        {
            if (_isInitialized) return;

            _audioSource = Plugin.Instance.gameObject.AddComponent<AudioSource>();
            _audioSource.loop = true;
            _volume = GetMaxVolume;
            _currentMusicIndex = Mathf.Clamp((int)Plugin.Instance.SavedMusicStyle.Value, 0, _musicFileNames.Length - 1);
            _isInitialized = true;
            IsPaused = false;
            IsMusicLoaded = false;
            IsTransitioning = false;
            LoadSound("MultiplayerJoin.wav", c => _joinSound = c);
            LoadSound("MultiplayerLeave.wav", c => _leaveSound = c);
        }

        public static void NextSong(Action OnLoadedCallback = null)
        {
            if (IsMuted || IsTransitioning) return;
            IsTransitioning = true;
            _currentMusicIndex = ++_currentMusicIndex % Enum.GetNames(typeof(MusicStyle)).Length;
            if (IsPlayingDefault)
                StopMusicSoft(.3f, () => LoadMusic(_currentMusicIndex, OnLoadedCallback));
            else
                LoadMusic(_currentMusicIndex, OnLoadedCallback);
        }

        public static void PreviousSong(Action OnLoadedCallback = null)
        {
            if (IsMuted || IsTransitioning) return;
            IsTransitioning = true;
            _currentMusicIndex = --_currentMusicIndex % Enum.GetNames(typeof(MusicStyle)).Length;
            if (IsPlayingDefault)
                StopMusicSoft(.3f, () => LoadMusic(_currentMusicIndex, OnLoadedCallback));
            else
                LoadMusic(_currentMusicIndex, OnLoadedCallback);
        }

        public static void LoadCurrentIndexMusic(Action onLoadedCallback = null) => LoadMusic(_currentMusicIndex, onLoadedCallback);

        public static void LoadMusic(int musicIndex, Action OnLoadedCallback = null)
        {
            if (musicIndex < 0 || musicIndex > _musicFileNames.Length - 1)
            {
                Plugin.LogError($"Music Index inexpected value was {musicIndex}");
                return;
            }

            IsMusicLoaded = false;
            Plugin.Instance.StartCoroutine(MultiplayerAPIService.TryLoadingAudioClipLocal(_musicFileNames[musicIndex], clip =>
            {
                IsTransitioning = false;
                if (clip == null)
                {
                    IsMusicLoaded = false;
                    Plugin.LogError($"Music {_musicFileNames[musicIndex]} couldn't be loaded.");
                    return;
                }
                Plugin.Instance.SavedMusicStyle.Value = (MusicStyle)musicIndex;
                IsMusicLoaded = true;
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
                if (clip == null)
                {
                    Plugin.LogError($"Clip {path} couldn't be loaded.");
                    return;
                }
                _audioSource.clip = clip;
                _audioSource.volume = 0;
                OnLoadedCallback?.Invoke();
            }));
        }

        public static void LoadSound(string path, Action<AudioClip> OnLoadedCallback)
        {
            Plugin.Instance.StartCoroutine(MultiplayerAPIService.TryLoadingSound(path, clip =>
            {
                if (clip == null)
                {
                    Plugin.LogError($"Sound {path} couldn't be loaded.");
                    return;
                }
                OnLoadedCallback?.Invoke(clip);
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

        public static void PlayJoinSound()
        {
            if (_joinSound == null || _audioSource == null) return;
            _audioSource.PlayOneShot(_joinSound);
        }

        public static void PlayLeaveSound()
        {
            if (_leaveSound == null || _audioSource == null) return;
            _audioSource.PlayOneShot(_leaveSound);
        }

        public enum MusicStyle
        {
            Default,
            Alternative,
        }
    }
}
