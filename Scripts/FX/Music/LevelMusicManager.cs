using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public class LevelMusicManager : LevelModule
    {
        [Serializable]
        public struct PhaseMusicData
        {
            public string PhaseKey;
            public string MusicId;
        }

        public List<PhaseMusicData> PhaseMusics;

        public override void OnLevelPhaseChange(LevelPhase oldPhase, LevelPhase newPhase)
        {
            AudioLibrary audioLibrary = EffectsLibrary.GetAudioLibrary();
            if (audioLibrary == null) return;
            foreach (var data in PhaseMusics)
            {
                if (string.Equals(data.PhaseKey, newPhase.Key))
                {
                    audioLibrary.PlayMusicById(data.MusicId);
                    return;
                }
            }
        }
    }
}