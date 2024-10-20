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
        public AudioSource MainMenuMusic;
        public AudioSource IngameMusic;

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
        /// Plays a sound that is on the 
        /// </summary>
        /// <param name="audioType"></param>
        public void PlaySound(int audioType)
        {
            if (_audios == null || !_audios.ContainsKey(audioType)) return;
            if (_audios[audioType] == null) return;
            PlaySound(_audios[audioType]);
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
        public void PlayMusic(Music music)
        {
            if(music == null) return;
            StopMusic();
            if(MusicPlayer == null) return;
            MusicPlayer.clip = music.Clip;
            MusicPlayer.loop = music.Loop;
            MusicPlayer.volume = music.Volume;
            MusicPlayer.Play();
        }
        public void RestartMusic()
        {
            MusicPlayer.Stop();
            MusicPlayer.Play();
        }
        public void StopMusic()
        {
            if(MusicPlayer != null)
            {
                MusicPlayer.Stop();
            }
        }
        #endregion
    }
}