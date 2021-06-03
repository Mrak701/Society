﻿using UnityEngine;

namespace CarouselAnomaly
{
    class TriggerReceiver : MonoBehaviour
    {
        [SerializeField] private CarouselManager manager;
        private void Start() => manager.InitCollider(GetComponent<Collider>());

        private void OnTriggerEnter(Collider other) => manager.AddInList(other.attachedRigidbody);

        private void OnTriggerExit(Collider other) => manager.RemoveFromList(other.attachedRigidbody);
    }
}