﻿using UnityEngine;
namespace Shoots
{/// <summary>
/// патрон для оружия
/// </summary>
    public class Bullet : MonoBehaviour
    {
        private Vector3 target;//точка назначения
        private EnemyCollision enemy;// возможный враг
        private bool isFinished;// долетел ли снаряд
        private bool haveTarget;// имеет ли патрон цель(возможен выстрел в воздух)
        private GameObject impactEffect;// эффекта столкновения

        [SerializeField] private float mass = 0;
        [SerializeField] private float area = 0.649f;
        [SerializeField] private float kf = 1;

        BulletValues mBv;

        public Inventory.ItemStates.ItemsID Id;

        public void Init(BulletValues bv, RaycastHit t, GameObject impact, EnemyCollision e)
        {
            mBv = bv;
            target = t.point;
            enemy = e;
            haveTarget = true;
            impactEffect = impact;

            var parent = new GameObject("parentForImpact").transform;
            parent.SetParent(t.transform);
            impactEffect = Instantiate(impactEffect, parent);
            impactEffect.transform.forward = t.normal;
            impactEffect.SetActive(false);
        }
        public void Init(BulletValues bv, Vector3 t)
        {
            mBv = bv;
            target = t;
        }

        private void Update()
        {
            if (this.isFinished)
                return;

            if ((transform.position = Vector3.MoveTowards(transform.position, target, mBv.Speed * Time.deltaTime)) == target)
                Boom();
        }
        /// <summary>
        /// "взрыв" снаряда
        /// </summary>
        private void Boom()
        {
            if (haveTarget)
            {
                mBv.SetSpeed();
                if (enemy)
                {
                    enemy.InjureEnemy(Gun.GetOptimalDamage(mass, mBv.Speed, area, kf, mBv.CoveredDistance, mBv.MaxDistance));
                }
                else if (BulletValues.CanReflect(BulletValues.Energy(mass * kf, mBv.Speed), BulletValues.Energy(mass * kf, mBv.StartSpeed), mBv.Speed, mBv.Angle)
                    && Physics.Raycast(target, mBv.PossibleReflectionPoint, out RaycastHit hit, mBv.MaxDistance, mBv.Layers, QueryTriggerInteraction.Ignore))
                {
                    hit.transform.TryGetComponent(out enemy);

                    mBv.SetValues(hit.distance + mBv.CoveredDistance, Vector3.Reflect(transform.forward, hit.normal), Mathf.Abs(90 - Vector3.Angle(transform.forward, hit.normal)));
                    target = hit.point;
                    impactEffect.transform.forward = hit.normal;

                    var gg = new GameObject("Source");
                    gg.AddComponent<AudioSource>().PlayOneShot(Resources.Load<AudioClip>("Guns\\BulletReflect"));
                    gg.transform.position = hit.point;
                    return;
                }
                
                impactEffect.transform.SetPositionAndRotation(target, Quaternion.identity);
                impactEffect.SetActive(true);
            }

            isFinished = true;

            Destroy(gameObject, 0.1f);
        }
    }
}