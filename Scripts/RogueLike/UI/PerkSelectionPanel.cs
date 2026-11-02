using System.Collections.Generic;
using Kuantech.Core.UI;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.RogueLike
{
    public class PerkSelectionPanel : KtUIPanel
    {
        [SerializeField] private PerkSelectionCard PerkCardPrefab;

        [SerializeField] private RectTransform PerkCardsParent;

        private UnityAction<PerkSelectionData> _onSelectRelay = null;

        public void SetCards(List<PerkSelectionData> selectionDatas, UnityAction<PerkSelectionData> onSelect)
        {
            //Clear existing ones
            PerkCardsParent.DestroyAllChildren();

            foreach(var perkSelectionData in selectionDatas)
            {
                PerkSelectionCard card = CreatePerkCard(perkSelectionData);
            }

            _onSelectRelay = onSelect;
        }

        private PerkSelectionCard CreatePerkCard(PerkSelectionData perkSelectionData)
        {
            PerkSelectionCard card = Instantiate(PerkCardPrefab, PerkCardsParent, false);
            card.SetPerk(perkSelectionData);
            card.OnSelect += OnCardSelect;
            return card;
        }

        private void OnCardSelect(PerkSelectionData perkSelectionData)
        {
            if(_onSelectRelay != null)
            {
                _onSelectRelay(perkSelectionData);
            }
            Close();
        }
    }
}