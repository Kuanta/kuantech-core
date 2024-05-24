using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    [Serializable]
    public class SoundQueue
    {
        public Dictionary<string, Queue<Sound>> _soundQueues;
        [NonSerialized] public AudioLibrary AudioLibrary;
        public int QueueSize = 3;

        public SoundQueue(AudioLibrary audioLibrary)
        {
            AudioLibrary = audioLibrary;
            _soundQueues = new Dictionary<string, Queue<Sound>>();
        }

        public void QueueSound(Sound sound)
        {
            if (_soundQueues == null)
            {
                _soundQueues = new Dictionary<string, Queue<Sound>>();
            }
            if (sound.AudioId.IsNullOrEmpty()) return;
            if (!_soundQueues.ContainsKey(sound.AudioId) || _soundQueues[sound.AudioId] == null)
            {
                _soundQueues[sound.AudioId] = new Queue<Sound>();
            }

            if (_soundQueues[sound.AudioId].Count > QueueSize) return; //Don't queue too much sound
            _soundQueues[sound.AudioId].Enqueue(sound);
            sound.Enqueued = true;
        }

        public void HandleQueue()
        {
            if (_soundQueues == null) return;
            foreach (var soundId in _soundQueues.Keys)
            {
                if(_soundQueues[soundId].IsNullOrEmpty()) continue;
                Sound sound = _soundQueues[soundId].Peek();
                if(SoundCanBePlayed(sound))
                {
                    //Dequeue sound
                    sound = _soundQueues[soundId].Dequeue();
                    AudioLibrary.SetLastPlayedTime(sound);
                    sound.Play();
                    sound.Enqueued = false;
                    sound.Deqeued();
                }
            }
        }

        public bool SoundCanBePlayed(Sound sound)
        {
            return AudioLibrary.GetElapsedTime(sound.AudioId) > sound.Cooldown;
        }
    }
}