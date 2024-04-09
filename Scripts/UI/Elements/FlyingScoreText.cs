using System;
using Kuantech.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.JigsawTen.UI
{
    public class FlyingScoreText : FlyingUIElement
    {
        [SerializeField] private TMP_Text ScoreText;
        [NonSerialized] public JigsawTenLevel Level;
        [NonSerialized] public int ScoreContribution;
        [SerializeField] private Image BackgroundImage;
        [SerializeField] private Renderer Renderer;
        public void SetScoreContribution(JigsawTenLevel level, int scoreContrib, Color color)
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
            Level.EarnScore(ScoreContribution);
        }
        public void SetText(string text)
        {
            ScoreText.text = text;
        }
    }
}