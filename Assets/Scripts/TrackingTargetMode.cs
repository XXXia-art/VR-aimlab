using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VRAimLab
{
    public class TrackingTargetMode : MonoBehaviour
    {
        [Header("Target")]
        public float targetScale = 0.75f;
        public float moveDistance = 6f;
        public float moveHeightMin = 1.2f;
        public float moveHeightMax = 2.2f;
        public float spawnRangeX = 2.5f;
        public LayerMask targetLayer;

        [Header("Movement")]
        public float circleRadius = 2.0f;
        public float standardSpeed = 1.5f;
        public float hardSpeed = 3.0f;
        public bool randomDirectionChange = false;
        public float directionChangeInterval = 3f;

        [Header("HP & Healing")]
        public int standardHP = 8;
        public int hardHP = 15;
        public float standardHealRate = 1f;
        public float hardHealRate = 2f;
        public float healDelay = 0.5f;

        [Header("Game Settings")]
        public float gameDuration = 30f;

        private GameObject currentTarget;
        private Slider hpBar;
        private int maxHP;
        private int currentHP;
        private float moveSpeed;
        private float healRate;
        private float angle;
        private float centerY;
        private float centerZ;
        private int moveDirection = 1;
        private float lastHitTime = -10f;
        private int totalDestroyed = 0;
        private bool isRunning = false;
        private Coroutine gameTimerCoroutine;
        private Coroutine directionChangeCoroutine;

        public void StartMode()
        {
            isRunning = true;
            totalDestroyed = 0;

            bool isHard = GameStateManager.Instance != null && GameStateManager.Instance.SelectedDifficulty == Difficulty.Hard;
            maxHP = isHard ? hardHP : standardHP;
            moveSpeed = isHard ? hardSpeed : standardSpeed;
            healRate = isHard ? hardHealRate : standardHealRate;
            randomDirectionChange = isHard;

            ScoreManager.Instance?.ResetScore();
            SpawnTarget();
            gameTimerCoroutine = StartCoroutine(GameTimerRoutine());
            if (randomDirectionChange)
                directionChangeCoroutine = StartCoroutine(DirectionChangeRoutine());
        }

        public void StopMode()
        {
            isRunning = false;
            if (gameTimerCoroutine != null) StopCoroutine(gameTimerCoroutine);
            if (directionChangeCoroutine != null) StopCoroutine(directionChangeCoroutine);
            if (currentTarget != null)
            {
                Destroy(currentTarget);
                currentTarget = null;
            }
        }

        void SpawnTarget()
        {
            if (!isRunning) return;

            currentHP = maxHP;
            angle = Random.Range(0f, Mathf.PI * 2f);
            centerY = Random.Range(moveHeightMin, moveHeightMax);
            centerZ = moveDistance;

            currentTarget = CreateTargetBall();
            currentTarget.name = "TrackingTarget";
            currentTarget.SetActive(true);

            // 创建头顶血条
            CreateHPBar();

            TrackingTargetEntity entity = currentTarget.GetComponent<TrackingTargetEntity>();
            if (entity == null) entity = currentTarget.AddComponent<TrackingTargetEntity>();
            entity.Initialize(this);

            StartCoroutine(SpawnAnim(currentTarget));

            Debug.Log($"[TrackingTargetMode] 追踪目标已生成 HP={maxHP} Speed={moveSpeed}");
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
            UpdateMaterialColor(mat);
            rend.material = mat;

            return go;
        }

        void CreateHPBar()
        {
            GameObject canvasObj = new GameObject("HPBarCanvas");
            canvasObj.transform.SetParent(currentTarget.transform, false);
            canvasObj.transform.localPosition = new Vector3(0, targetScale * 0.7f, 0);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            canvas.worldCamera = Camera.main;

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(1.2f, 0.15f);
            canvasRT.localScale = Vector3.one * 0.4f;

            GameObject bgObj = new GameObject("HPBarBg");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            GameObject fillObj = new GameObject("HPBarFill");
            fillObj.transform.SetParent(canvasObj.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = Color.cyan;
            RectTransform fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            hpBar = fillObj.AddComponent<Slider>();
            hpBar.direction = Slider.Direction.LeftToRight;
            hpBar.minValue = 0;
            hpBar.maxValue = maxHP;
            hpBar.value = maxHP;
            hpBar.interactable = false;
            hpBar.fillRect = fillRT;
        }

        void UpdateMaterialColor(Material mat)
        {
            float t = 1f - (currentHP / (float)maxHP);
            Color color = Color.Lerp(new Color(0.35f, 0.85f, 0.95f), new Color(0.9f, 0.2f, 0.2f), t);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Glossiness", 0.85f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 0.4f);
        }

        void UpdateHPBar()
        {
            if (hpBar != null)
                hpBar.value = currentHP;

            if (currentTarget != null)
            {
                Renderer rend = currentTarget.GetComponent<Renderer>();
                if (rend != null)
                    UpdateMaterialColor(rend.material);
            }
        }

        IEnumerator SpawnAnim(GameObject target)
        {
            float duration = 0.2f;
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

        IEnumerator FlashWhite()
        {
            if (currentTarget == null) yield break;
            Renderer rend = currentTarget.GetComponent<Renderer>();
            if (rend == null) yield break;
            Color original = rend.material.GetColor("_Color");
            rend.material.SetColor("_Color", Color.white);
            rend.material.SetColor("_EmissionColor", Color.white);
            yield return new WaitForSeconds(0.05f);
            if (currentTarget != null)
                UpdateMaterialColor(rend.material);
        }

        IEnumerator GameTimerRoutine()
        {
            yield return new WaitForSeconds(gameDuration);
            isRunning = false;
            if (directionChangeCoroutine != null) StopCoroutine(directionChangeCoroutine);
            if (currentTarget != null)
            {
                Destroy(currentTarget);
                currentTarget = null;
            }
            ScoreManager.Instance?.RecordAndShowResult();
            GameStateManager.Instance?.StopGame();
            Debug.Log($"[TrackingTargetMode] 游戏结束！摧毁数={totalDestroyed}");
        }

        IEnumerator DirectionChangeRoutine()
        {
            while (isRunning)
            {
                yield return new WaitForSeconds(directionChangeInterval);
                if (Random.value > 0.5f)
                    moveDirection = -moveDirection;
            }
        }

        void Update()
        {
            if (!isRunning || currentTarget == null) return;

            // 圆周运动
            angle += moveSpeed * Time.deltaTime / circleRadius * moveDirection;
            Vector3 newPos = new Vector3(
                Mathf.Cos(angle) * circleRadius,
                centerY,
                centerZ + Mathf.Sin(angle) * circleRadius
            );
            currentTarget.transform.position = newPos;

            // 脱靶回血
            if (Time.time > lastHitTime + healDelay)
            {
                int heal = Mathf.RoundToInt(healRate * Time.deltaTime);
                if (heal > 0 && currentHP < maxHP)
                {
                    currentHP = Mathf.Min(currentHP + heal, maxHP);
                    UpdateHPBar();
                }
            }
        }

        public void OnTargetHit()
        {
            if (!isRunning || currentTarget == null) return;
            currentHP--;
            lastHitTime = Time.time;
            UpdateHPBar();
            StartCoroutine(FlashWhite());

            if (currentHP <= 0)
            {
                totalDestroyed++;
                ScoreManager.Instance?.AddHit();
                HitEffectPool.Instance?.Spawn(currentTarget.transform.position);

                if (currentTarget != null)
                {
                    Destroy(currentTarget);
                    currentTarget = null;
                }
                SpawnTarget();
            }
        }
    }

    public class TrackingTargetEntity : MonoBehaviour
    {
        private TrackingTargetMode mode;

        public void Initialize(TrackingTargetMode m)
        {
            mode = m;
        }

        public void Hit()
        {
            mode?.OnTargetHit();
        }
    }
}
