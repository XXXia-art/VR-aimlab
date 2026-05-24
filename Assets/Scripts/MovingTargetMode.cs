using System.Collections;
using UnityEngine;

namespace VRAimLab
{
    public class MovingTargetMode : MonoBehaviour
    {
        [Header("Target")]
        public float targetScale = 0.75f;
        public float moveSpeed = 3f;
        public float moveRange = 2.5f;
        public float moveHeightMin = 1.2f;
        public float moveHeightMax = 2.2f;

        [Header("World Bounds")]
        public float xLimit = 5.5f;      // 房间半宽减去安全边距（roomWidth=12）
        public float yMinLimit = 0.4f;   // 地板以上（考虑球半径0.375）
        public float yMaxLimit = 3.6f;   // 天花板以下（roomHeight=4）
        public float moveDistanceMin = 4f;
        public float moveDistanceMax = 6f;
        public float changeDirectionInterval = 2f;
        public LayerMask targetLayer;

        [Header("Difficulty")]
        public float speedIncreasePerHit = 0.3f;
        public float maxSpeed = 10f;

        private GameObject currentTarget;
        private float currentSpeed;
        private Vector3 moveDirection = Vector3.right;
        private Vector3 startPosition;
        private int hitCount = 0;
        private bool isRunning = false;
        private int moveAxis = 0; // 0=水平(X), 1=垂直(Y)

        public void StartMode()
        {
            isRunning = true;
            currentSpeed = moveSpeed;
            hitCount = 0;
            // 困难模式：随机选择水平或垂直移动
            bool isHard = GameStateManager.Instance != null && GameStateManager.Instance.SelectedDifficulty == Difficulty.Hard;
            moveAxis = isHard ? (Random.value > 0.5f ? 1 : 0) : 0;
            // 根据移动轴设置初始方向
            SetMoveDirectionForAxis();
            SpawnTarget();
            StartCoroutine(ChangeDirectionRoutine());
        }

        public void StopMode()
        {
            isRunning = false;
            StopAllCoroutines();
            if (currentTarget != null)
            {
                Destroy(currentTarget);
                currentTarget = null;
            }
        }

        void SpawnTarget()
        {
            float randomX = Random.Range(-moveRange, moveRange);
            float randomY = Random.Range(moveHeightMin, moveHeightMax);
            float randomZ = Random.Range(moveDistanceMin, moveDistanceMax);
            startPosition = new Vector3(randomX, randomY, randomZ);
            currentTarget = CreateTargetBall();
            currentTarget.transform.position = startPosition;
            currentTarget.name = "MovingTarget";
            currentTarget.SetActive(true);

            // 生成动画：从小变大
            StartCoroutine(SpawnAnim(currentTarget));

            // 确保有 MovingTargetEntity
            MovingTargetEntity entity = currentTarget.GetComponent<MovingTargetEntity>();
            if (entity == null) entity = currentTarget.AddComponent<MovingTargetEntity>();
            entity.Initialize(this);

            Debug.Log($"[MovingTargetMode] 小球已生成，位置={currentTarget.transform.position}, scale={targetScale}");
        }

        GameObject CreateTargetBall()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = Vector3.one * targetScale;

            // 碰撞体（Trigger，用于射线检测）
            SphereCollider col = go.GetComponent<SphereCollider>();
            if (col == null) col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;

            // 材质：青色自发光（和 5x5 模式一致）
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
            float duration = 0.3f;
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

        void Update()
        {
            if (!isRunning || currentTarget == null) return;

            // 移动
            currentTarget.transform.position += moveDirection * currentSpeed * Time.deltaTime;

            // 世界空间边界检测与反弹，防止穿墙
            Vector3 pos = currentTarget.transform.position;

            if (moveAxis == 0) // 水平移动（X轴）
            {
                if (pos.x > xLimit)
                {
                    pos.x = xLimit;
                    moveDirection = Vector3.left * Mathf.Abs(moveDirection.x);
                }
                else if (pos.x < -xLimit)
                {
                    pos.x = -xLimit;
                    moveDirection = Vector3.right * Mathf.Abs(moveDirection.x);
                }
            }
            else // 垂直移动（Y轴）
            {
                if (pos.y > yMaxLimit)
                {
                    pos.y = yMaxLimit;
                    moveDirection = Vector3.down * Mathf.Abs(moveDirection.y);
                }
                else if (pos.y < yMinLimit)
                {
                    pos.y = yMinLimit;
                    moveDirection = Vector3.up * Mathf.Abs(moveDirection.y);
                }
            }

            currentTarget.transform.position = pos;
        }

        IEnumerator ChangeDirectionRoutine()
        {
            while (isRunning)
            {
                yield return new WaitForSeconds(changeDirectionInterval);
                if (Random.value > 0.5f)
                {
                    moveDirection = -moveDirection;
                }
            }
        }

        public void OnTargetHit()
        {
            hitCount++;
            ScoreManager.Instance?.AddHit();
            HitEffectPool.Instance?.Spawn(currentTarget.transform.position);

            // 加速
            currentSpeed = Mathf.Min(currentSpeed + speedIncreasePerHit, maxSpeed);

            // 困难模式：重新随机选择移动轴
            bool isHard = GameStateManager.Instance != null && GameStateManager.Instance.SelectedDifficulty == Difficulty.Hard;
            moveAxis = isHard ? (Random.value > 0.5f ? 1 : 0) : 0;
            SetMoveDirectionForAxis();

            // 销毁旧球，生成新球
            if (currentTarget != null)
            {
                Destroy(currentTarget);
                currentTarget = null;
            }
            SpawnTarget();
        }

        void SetMoveDirectionForAxis()
        {
            if (moveAxis == 0)
                moveDirection = Random.value > 0.5f ? Vector3.right : Vector3.left;
            else
                moveDirection = Random.value > 0.5f ? Vector3.up : Vector3.down;
        }
    }

    public class MovingTargetEntity : MonoBehaviour
    {
        private MovingTargetMode mode;

        public void Initialize(MovingTargetMode m)
        {
            mode = m;
        }

        public void Hit()
        {
            mode?.OnTargetHit();
        }
    }
}
