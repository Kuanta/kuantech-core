using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Kuantech.Core.FX
{
   
    public class AudioLibrary : MonoBehaviour
    {
        [Header("Audio Mix")]
        [SerializeField] private AudioMixer MasterMixer;
        public List<Sound> Clips;
        public Dictionary<int, Sound> _audios;
        public Dictionary<string, float> _lastPlayedTimes;
        
        [Header("Music")] 
        public AudioSource MainMenuMusic;
        public AudioSource IngameMusic;

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
            _audios[audioType].Play();
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
                _lastPlayedTimes = new Dictionary<string, float>();
            }
            string clipName = sound.AudioSource.clip.name;
            float lastPlayedTime;
            if(!_lastPlayedTimes.ContainsKey(clipName))
            {
                lastPlayedTime = 0;
            }else{
                lastPlayedTime = _lastPlayedTimes[sound.AudioSource.clip.name];

            }
            float elapsedTime = Time.time - lastPlayedTime;
            if(elapsedTime >= sound.Cooldown)
            {
                sound.Play();
            }
            _lastPlayedTimes[clipName] = Time.time;

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
    }
}