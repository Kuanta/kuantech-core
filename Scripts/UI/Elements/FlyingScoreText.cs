using System;
using Kuantech.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Kuantech.Puzzle;

namespace Kuantech.JigsawTen.UI
{
    public class FlyingScoreText : FlyingUIElement
    {
        [SerializeField] private TMP_Text ScoreText;
        [NonSerialized] public int ScoreContribution;
        [SerializeField] private Image BackgroundImage;
        [SerializeField] private Renderer Renderer;
        private PuzzleLevel Level;
        public string ScoreId;
        public void SetScoreContribution(PuzzleLevel level, int scoreContrib, Color color)
        {
            ScoreContribution = scoreContrib;
            Level = level;
            if(BackgroundImage != null) BackgroundImage.color = color;
            if(Renderer != null) Renderer.material.SetColor("_BaseColor", color);
        }

        protected override void OnTargetReached(UnityAction OnTargetReachedHandler)
        {
            base.OnTargetReached(OnTargetReachedHandler);
            if(Level == null) return;
            Level.EarnScore(ScoreId, ScoreContribution);
        }
        public void SetText(string text)
        {
            ScoreText.text = text;
        }
    }
}