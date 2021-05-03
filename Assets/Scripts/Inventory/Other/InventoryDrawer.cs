﻿using UnityEngine;

public sealed class InventoryDrawer : Singleton<InventoryDrawer>
{
    [SerializeField] private GameObject mainField;

    [Space(15)]
    [SerializeField] private Transform mainContainer;
    [SerializeField] private Transform supportContainer;

    private static bool MainFieldEnabled { get; set; } = false;
    public Transform GetMainContainer() => mainContainer;

    public Transform GetSupportContainer() => supportContainer;


    private void Start()
    {
        EffectsManager.Instance.Init();
        mainField.SetActive(MainFieldEnabled);
    }

    /// <summary>
    /// смена активности инвентаря
    /// </summary>
    public bool ChangeActiveMainField(bool value)
    {
        MainFieldEnabled = !Shoots.GunAnimator.Instance.IsAiming && value;
        mainField.SetActive(MainFieldEnabled);
        SetActiveDOF(MainFieldEnabled);
        return MainFieldEnabled;
    }
    private void SetActiveDOF(bool active)
    {
        EffectsManager.Instance.SetEnableDOF(active);
    }
}
