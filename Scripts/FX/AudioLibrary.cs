using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Audio;

namespace Kuantech.Core.FX
{
   
    public class AudioLibrary : MonoBehaviour
    {
        public struct SoundPlayInfo
        {
            public float LastPlayedTime;
            public int ComboCount;
        }
        [Header("Audio Mix")]
        [SerializeField] private AudioMixer MasterMixer;
        public List<Sound> Clips;
        public Dictionary<int, Sound> _audios;
        public Dictionary<string, SoundPlayInfo> _lastPlayedTimes;
        
        public SoundQueue SoundQueue;

        [Header("Music")] 
        public List<Music> Musics;
        public AudioSource MusicPlayer;
        
        [Header("Music Crossfade")]
        [SerializeField] private float MusicChangeDuration = 1.0f; // default crossfade seconds
        [SerializeField] private bool UseUnscaledTime = true;       // ignore timescale during fades

        private AudioSource _musicA;
        private AudioSource _musicB;
        private bool _usingA = true;           // which one is the "current" player
        private Coroutine _musicFadeCo;
        
        private bool _initialized = false;
        public void Initialize()
        {
            //Load music values
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultValue:1f);
            float sfxVolume = PlayerPrefs.GetFloat("SfxVolume", defaultValue: 1f);
            int toggleMusic = PlayerPrefs.GetInt("ToggleMusic", defaultValue:1);
            int toggleSfx = PlayerPrefs.GetInt("ToggleSfx", defaultValue: 1);
            SetMusicVolume(toggleMusic == 1 ? musicVolume : 0.0001f);
            SetSfxVolume(toggleSfx == 1 ? sfxVolume : 0.0001f);

            _audios  = new Dictionary<int, Sound>();
            if(Clips != null)
            {
                foreach (var clip in Clips)
                {
                    _audios[clip.AudioTag] = clip;
                    clip.PlayWithAudioLibrary = false; //Prevent endless cycle
                }
            }
            SoundQueue = new SoundQueue(this);
            _initialized = true;
        
            EnsureMusicSources();
        }
        
        private void EnsureMusicSources()
        {
            // Reuse the existing MusicPlayer as A, create B lazily
            if (_musicA == null)
            {
                _musicA = MusicPlayer != null ? MusicPlayer : gameObject.AddComponent<AudioSource>();
                _musicA.playOnAwake = false;
                _musicA.loop = true;
            }
            if (_musicB == null)
            {
                _musicB = gameObject.AddComponent<AudioSource>();
                _musicB.playOnAwake = false;
                _musicB.loop = true;

                // Match routing/settings
                _musicB.outputAudioMixerGroup = _musicA.outputAudioMixerGroup;
                _musicB.spatialBlend = _musicA.spatialBlend;
            }
        }
        
        private void Update()
        {
            if(SoundQueue == null || !_initialized) return;
            SoundQueue.HandleQueue();
        }

        /// <summary>
        /// Checks if audio with given tag is available
        /// </summary>
        /// <param name="audioTag"></param>
        /// <returns></returns>
        public bool IsSoundAvailable(int audioTag)
        {
            if (_audios == null || !_audios.ContainsKey(audioTag)) return false;
            if (_audios[audioTag] == null) return false;
            return true;
        }
        
        /// <summary>
        /// A static method to play sounds by tag
        /// </summary>
        /// <param name="tag"></param>
        public static void PlaySoundByTag(int tag)
        {
            var audioLib = EffectsLibrary.GetAudioLibrary();
            if (audioLib == null) return;
            audioLib.PlaySound(tag);
        }
        
        /// <summary>
        /// A static method to play sounds by tag
        /// </summary>
        /// <param name="tag"></param>
        public static void PlayComboSoundByTag(int tag, int comboIndex)
        {
            var audioLib = EffectsLibrary.GetAudioLibrary();
            if (audioLib == null) return;
            Sound sound = audioLib.GetSountByTag(tag);
            audioLib.PlayComboSound(sound, comboIndex);
        }
        
        /// <summary>
        /// Plays a sound that is on the 
        /// </summary>
        /// <param name="audioType"></param>
        public void PlaySound(int audioType)
        {
            Sound sound = GetSountByTag(audioType);
            if (sound == null) return;
            PlaySound(_audios[audioType]);
        }

        public Sound GetSountByTag(int audioType)
        {
            if (_audios == null || !_audios.ContainsKey(audioType)) return null;
            if (_audios[audioType] == null) return null;
            return _audios[audioType];
        }
        
        public void StopSound(int audioType, float fadeOutDuration=0)
        {
            if (_audios == null || !_audios.ContainsKey(audioType)) return;
            if (_audios[audioType] == null) return;
            _audios[audioType].Stop(fadeOutDuration);
        }
        
        public void PlaySound(Sound sound)
        {
            if(sound.Cooldown == 0)
            {
                sound.Play();
                return;
            }
            if(_lastPlayedTimes == null)
            {
                _lastPlayedTimes = new Dictionary<string, SoundPlayInfo>();
            }
            string clipName = sound.AudioId;
          
            float elapsedTime = GetElapsedTime(clipName);
            if(elapsedTime >= sound.Cooldown)
            {
                sound.Play();
                SetLastPlayedTime(sound);
            }
            else if(sound.QueueSound)
            {
                SoundQueue.QueueSound(sound);
            }
        }

        public void PlayComboSound(Sound sound, int comboIndex)
        {
            sound.PlayComboSfx(comboIndex);
        }
        
        public float GetElapsedTime(string soundId)
        {
            if(_lastPlayedTimes == null || !_lastPlayedTimes.ContainsKey(soundId))
            {
                return float.MaxValue;
            }
            return Time.time - _lastPlayedTimes[soundId].LastPlayedTime;
        }

        public void SetLastPlayedTime(Sound sound)
        {
            if(sound.AudioId.IsNullOrEmpty()) return;
            if(!_lastPlayedTimes.ContainsKey(sound.AudioId))
            {
                _lastPlayedTimes[sound.AudioId] = new SoundPlayInfo{
                    LastPlayedTime = 0,
                    ComboCount = 0,
                };
            }
            SoundPlayInfo info  = _lastPlayedTimes[sound.AudioId];
            //Did combo count increased?
            float elapsedTime = Time.time - info.LastPlayedTime;
            if(elapsedTime < sound.ComboCooldown && sound.ComboCooldown > 0)
            {
                info.ComboCount++;
            }else{
                info.ComboCount = 0;
            }
            info.LastPlayedTime = Time.time;
            _lastPlayedTimes[sound.AudioId] = info;
        }

        public static int GetComboCount(Sound sound)
        {
            AudioLibrary audioLibrary = EffectsLibrary.GetAudioLibrary();
            if(audioLibrary == null) return 0;
            if (sound.AudioId.IsNullOrEmpty()) return 0;
            if (audioLibrary._lastPlayedTimes == null)
            {
                audioLibrary._lastPlayedTimes = new Dictionary<string, SoundPlayInfo>();
            }
            if(!audioLibrary._lastPlayedTimes.ContainsKey(sound.AudioId)) return 0;
            float elapsedTime = audioLibrary.GetElapsedTime(sound.AudioId);
            if(elapsedTime < sound.ComboCooldown && sound.ComboCooldown > 0)
            {
                return audioLibrary._lastPlayedTimes[sound.AudioId].ComboCount;
            }
            return 0;
        }
  
        /// <summary> 
        /// Sets the music volume
        /// </summary>
        /// <param name="value">Normalized volume value</param>
        public void SetMusicVolume(float value)
        {
            value = Mathf.Max(0.0001f, value);
            if (MasterMixer == null) return;
            MasterMixer.SetFloat("musicVolume", Mathf.Log10(value * 0.5f) * 20);
        }

        public void SetSfxVolume(float value)
        {
            value = Mathf.Max(0.0001f, value);
            if (MasterMixer == null) return;
            MasterMixer.SetFloat("sfxVolume", Mathf.Log10(value * 0.5f) * 20);
        }

        #region Music

        public void PlayMusicById(string musicId, float? durationOverride = null)
        {
            if (musicId.IsNullOrEmpty()) return;
            var music = GetMusicById(musicId);
            PlayMusic(music, durationOverride);
        }

        public Music GetMusicById(string musicId)
        {
            foreach (var music in Musics)
            {
                if (string.Equals(music.Id, musicId))
                {
                    return music;
                }
            }

            return null;
        }
        public void PlayMusic(Music music, float? durationOverride = null)
        {
            if (music == null) return;
            EnsureMusicSources();
            float dur = durationOverride ?? MusicChangeDuration;
            // if nothing is playing, dur can be used as fade-in; if <= 0 => hard switch
            if (_musicFadeCo != null) StopCoroutine(_musicFadeCo);
            _musicFadeCo = StartCoroutine(CrossfadeTo(music, dur));
        }
        
        private System.Collections.IEnumerator CrossfadeTo(Music next, float duration)
        {
            // Pick source roles
            AudioSource from = _usingA ? _musicA : _musicB;
            AudioSource to   = _usingA ? _musicB : _musicA;

            // Prepare "to" source
            to.clip   = next.Clip;
            to.loop   = next.Loop;
            float targetVol = Mathf.Clamp01(next.Volume);
            to.volume = 0f;
            to.Play();

            // If nothing is currently playing, treat as simple fade-in
            bool fromActive = (from.isPlaying && from.clip != null);

            if (duration <= 0f)
            {
                if (fromActive) from.Stop();
                to.volume = targetVol;
                _usingA = !_usingA; // swap active
                yield break;
            }

            float t = 0f;
            float fromStart = fromActive ? from.volume : 0f;
            float toStart   = 0f;

            // Crossfade
            while (t < duration)
            {
                float dt = UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                t += dt;
                float k = Mathf.Clamp01(t / duration);

                if (fromActive) from.volume = Mathf.Lerp(fromStart, 0f, k);
                to.volume = Mathf.Lerp(toStart, targetVol, k);

                yield return null;
            }

            // Finalize
            if (fromActive)
            {
                from.Stop();
                from.volume = 0f;
            }
            to.volume = targetVol;

            _usingA = !_usingA; // 'to' becomes current
            _musicFadeCo = null;
        }
        
        public void StopMusic()
        {
            EnsureMusicSources();
            if (_musicFadeCo != null) { StopCoroutine(_musicFadeCo); _musicFadeCo = null; }
            if (_musicA != null) { _musicA.Stop(); _musicA.volume = 0f; }
            if (_musicB != null) { _musicB.Stop(); _musicB.volume = 0f; }
        }

        public void RestartMusic()
        {
            EnsureMusicSources();
            var cur = _usingA ? _musicA : _musicB;
            if (cur != null && cur.clip != null)
            {
                cur.Stop();
                cur.Play();
            }
        }
        #endregion
    }
}