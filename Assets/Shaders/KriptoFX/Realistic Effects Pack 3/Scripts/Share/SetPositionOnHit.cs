﻿using UnityEngine;

public class SetPositionOnHit : MonoBehaviour
{

    public float OffsetPosition;

    private EffectSettings effectSettings;
    private Transform tRoot;
    private bool isInitialized;

    private void GetEffectSettingsComponent(Transform tr)
    {
        var parent = tr.parent;
        if (parent != null)
        {
            effectSettings = parent.GetComponentInChildren<EffectSettings>();
            if (effectSettings == null)
                GetEffectSettingsComponent(parent.transform);
        }
    }

    private void Start()
    {
        GetEffectSettingsComponent(transform);
        if (effectSettings == null)
            Debug.Log("Prefab root or children have not script \"PrefabSettings\"");
        tRoot = effectSettings.transform;
    }

    private void effectSettings_CollisionEnter(object sender, CollisionInfo e)
    {
        var direction = (tRoot.position + Vector3.Normalize(e.Hit.point - tRoot.position) * (effectSettings.MoveDistance + 1)).normalized;
        transform.position = e.Hit.point - direction * OffsetPosition;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            effectSettings.CollisionEnter += effectSettings_CollisionEnter;
        }
    }

    // Update is called once per frame
    private void OnDisable()
    {
        transform.position = Vector3.zero;
    }
}
