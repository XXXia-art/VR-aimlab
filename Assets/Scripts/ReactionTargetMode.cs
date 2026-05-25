using System.Collections;
using UnityEngine;

namespace VRAimLab
{
    public class ReactionTargetMode : MonoBehaviour
    {
        [Header("Target")]
        public float targetScale = 0.75f;
        public float moveDistanceMin = 4f;
        public float moveDistanceMax = 6f;
        public float moveHeightMin = 1.2f;
        public float moveHeightMax = 2.2f;
        public float spawnRangeX = 2.5f;
        public LayerMask targetLayer;

        [Header("Game Settings")]
        public float gameDuration = 30f;
        public float standardLifeTime = 1f;
        public float hardLifeTime = 0.5f;

        private GameObject currentTarget;
        private float currentLifeTime;
        private Coroutine lifeTimerCoroutine;
        private Coroutine gameTimerCoroutine;
        private int hitCount = 0;
        private int spawnedCount = 0;
        private bool isRunning = false;

        public void StartMode()
        {
            isRunning = true;
            hitCount = 0;
            spawnedCount = 0;

            bool isHard = GameStateManager.Instance != null && GameStateManager.Instance.SelectedDifficulty == Difficulty.Hard;
            currentLifeTime = isHard ? hardLifeTime : standardLifeTime;

            ScoreManager.Instance?.ResetScore();
            SpawnTarget();
            gameTimerCoroutine = StartCoroutine(GameTimerRoutine());
        }

        public void StopMode()
        {
            isRunning = false;
            if (lifeTimerCoroutine != null) StopCoroutine(lifeTimerCoroutine);
            if (gameTimerCoroutine != null) StopCoroutine(gameTimerCoroutine);
            if (currentTarget != null)
            {
                Destroy(currentTarget);
                currentTarget = null;
            }
        }

        void SpawnTarget()
        {
            if (!isRunning) return;

            float randomX = Random.Range(-spawnRangeX, spawnRangeX);
            float randomY = Random.Range(moveHeightMin, moveHeightMax);
            float randomZ = Random.Range(moveDistanceMin, moveDistanceMax);
            Vector3 spawnPos = new Vector3(randomX, randomY, randomZ);

            currentTarget = CreateTargetBall();
            currentTarget.transform.position = spawnPos;
            currentTarget.name = "ReactionTarget";
            currentTarget.SetActive(true);

            StartCoroutine(SpawnAnim(currentTarget));

            ReactionTargetEntity entity = currentTarget.GetComponent<ReactionTargetEntity>();
            if (entity == null) entity = currentTarget.AddComponent<ReactionTargetEntity>();
            entity.Initialize(this);

            spawnedCount++;
            lifeTimerCoroutine = StartCoroutine(TargetLifeRoutine());

            Debug.Log($"[ReactionTargetMode] 小球已生成 #{spawnedCount}, 生存时间={currentLifeTime}s");
        }

        GameObject CreateTargetBall()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = Vector3.one * targetScale;

            SphereCollider col = go.GetComponent<SphereCollider>();
            if (col == null) col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;

            Renderer rend = go.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetColor("_Color", new Color(0.35f, 0.85f, 0.95f));
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Glossiness", 0.85f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.35f, 0.85f, 0.95f) * 0.4f);
            rend.material = mat;

            return go;
        }

        IEnumerator SpawnAnim(GameObject target)
        {
            float duration = 0.15f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                target.transform.localScale = Vector3.one * targetScale * t;
                yield return null;
            }
            if (target != null)
                target.transform.localScale = Vector3.one * targetScale;
        }

        IEnumerator TargetLifeRoutine()
        {
            yield return new WaitForSeconds(currentLifeTime);
            // 时间到，未命中，销毁并生成下一个
            if (currentTarget != null)
            {
                Destroy(currentTarget);
                currentTarget = null;
            }
            SpawnTarget();
        }

        IEnumerator GameTimerRoutine()
        {
            yield return new WaitForSeconds(gameDuration);
            // 游戏结束
            isRunning = false;
            if (lifeTimerCoroutine != null) StopCoroutine(lifeTimerCoroutine);
            if (currentTarget != null)
            {
                Destroy(currentTarget);
                currentTarget = null;
            }
            ScoreManager.Instance?.RecordAndShowResult();
            GameStateManager.Instance?.StopGame();
            Debug.Log($"[ReactionTargetMode] 游戏结束！命中={hitCount}, 总生成={spawnedCount}");
        }

        public void OnTargetHit()
        {
            if (!isRunning) return;
            hitCount++;
            ScoreManager.Instance?.AddHit();
            HitEffectPool.Instance?.Spawn(currentTarget.transform.position);

            // 停止生存计时
            if (lifeTimerCoroutine != null) StopCoroutine(lifeTimerCoroutine);

            // 销毁并生成下一个
            if (currentTarget != null)
            {
                Destroy(currentTarget);
                currentTarget = null;
            }
            SpawnTarget();
        }
    }

    public class ReactionTargetEntity : MonoBehaviour
    {
        private ReactionTargetMode mode;

        public void Initialize(ReactionTargetMode m)
        {
            mode = m;
        }

        public void Hit()
        {
            mode?.OnTargetHit();
        }
    }
}
