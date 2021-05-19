﻿using UnityEngine;
using UnityEngine.EventSystems;

namespace SMG
{
    public class SMGGunsCell : MonoBehaviour, IPointerClickHandler
    {
        private SMGEventReceiver eventReceiver;
        public UnityEngine.UI.Image MImage { get; private set; }
        public int Id { get; private set; }

        public void ChangeItem(int id)
        {
            MImage.sprite = Inventory.InventorySpriteContainer.GetSprite(id);
            MImage.color = MImage.sprite ? Color.white : new Color(1, 1, 1, 0.1f);
            Id = id;
        }


        public void OnInit(SMGEventReceiver ev)
        {
            eventReceiver = ev;
            MImage = GetComponent<UnityEngine.UI.Image>();
        }

        public void OnPointerClick(PointerEventData eventData) => eventReceiver.OnSelectGunsCell(this);
    }
}