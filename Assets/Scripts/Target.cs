using System.Collections;
using UnityEngine;

namespace VRAimLab
{
    public class Target : MonoBehaviour
    {
        private GridManager gridManager;
        private int gridIndex;
        private bool isHit = false;
        private float targetScale;
        private Collider targetCollider;

        public void Initialize(GridManager manager, int index, float scale)
        {
            gridManager = manager;
            gridIndex = index;
            targetScale = scale;
            isHit = false;
            targetCollider = GetComponent<Collider>();
            if (targetCollider != null)
                targetCollider.enabled = true;
        }

        public void PlaySpawnAnimation()
        {
            StopAllCoroutines();
            StartCoroutine(SpawnAnim());
        }

        IEnumerator SpawnAnim()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                transform.localScale = Vector3.one * targetScale * t;
                yield return null;
            }
            transform.localScale = Vector3.one * targetScale;
        }

        public void Hit()
        {
            if (isHit) return;
            isHit = true;
            if (targetCollider != null)
                targetCollider.enabled = false;

            ScoreManager.Instance?.AddHit();
            HitEffectPool.Instance?.Spawn(transform.position);

            StartCoroutine(HitAnim());
        }

        IEnumerator HitAnim()
        {
            Vector3 startScale = transform.localScale;
            float duration = 0.12f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = startScale * (1f - t);
                yield return null;
            }
            gridManager?.ReturnTarget(gameObject, gridIndex);
        }
    }
}
