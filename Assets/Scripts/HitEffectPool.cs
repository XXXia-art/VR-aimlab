using System.Collections.Generic;
using UnityEngine;

namespace VRAimLab
{
    public class HitEffectPool : MonoBehaviour
    {
        public static HitEffectPool Instance;

        [Header("Effect Settings")]
        public GameObject hitEffectPrefab;
        public int poolSize = 10;
        public float effectDuration = 0.5f;

        private Queue<GameObject> pool = new Queue<GameObject>();

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            InitializePool();
        }

        void InitializePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = CreateEffect();
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        GameObject CreateEffect()
        {
            if (hitEffectPrefab != null)
                return Instantiate(hitEffectPrefab, transform);

            // Fallback: create a simple burst effect
            GameObject go = new GameObject("HitEffect");
            go.transform.SetParent(transform);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.1f;
            main.startLifetime = 0.3f;
            main.startSize = 0.1f;
            main.startSpeed = 2f;
            main.loop = false;
            main.startColor = new Color(0f, 0.9f, 1f, 1f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return go;
        }

        public void Spawn(Vector3 position)
        {
            GameObject effect = GetFromPool();
            effect.transform.position = position;
            effect.SetActive(true);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }

            StartCoroutine(ReturnToPoolAfterDelay(effect, effectDuration));
        }

        GameObject GetFromPool()
        {
            if (pool.Count > 0)
                return pool.Dequeue();

            return CreateEffect();
        }

        System.Collections.IEnumerator ReturnToPoolAfterDelay(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            effect.SetActive(false);
            pool.Enqueue(effect);
        }
    }
}
