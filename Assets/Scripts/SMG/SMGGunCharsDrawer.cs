﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SMG
{
    public class SMGGunCharsDrawer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI maxFlyDistText;
        [SerializeField] private TextMeshProUGUI optFlyDistText;
        [SerializeField] private TextMeshProUGUI caliberText;
        [SerializeField] private TextMeshProUGUI dispVolText;
        public void OnChangeSelectedGun(int id)
        {
            var chars = GunCharacteristics.GetGunCharacteristics(id);
            damageText.text = $"Урон: {chars.damage}";
            maxFlyDistText.text = $"Максимальная дистанция поражения: {chars.maxFlyD}";
            optFlyDistText.text = $"Оптимальная дистанция поражения: {chars.OptFlyD}";
            caliberText.text = $"Калибр: {chars.Caliber}";
            dispVolText.text = $"Объём магазина: {chars.DispenserV}";
        }
    }
}