using System.Collections;
using UnityEngine;

namespace VRAimLab
{
    public class MovingTargetMode : MonoBehaviour
    {
        [Header("Target")]
        public GameObject targetPrefab;
        public float targetScale = 0.5f;
        public float moveSpeed = 3f;
        public float moveRange = 4f;
        public float moveHeight = 1.6f;
        public float moveDistance = 8f;
        public float changeDirectionInterval = 2f;
        public LayerMask targetLayer;

        [Header("Difficulty")]
        public float speedIncreasePerHit = 0.2f;
        public float maxSpeed = 8f;

        private GameObject currentTarget;
        private float currentSpeed;
        private Vector3 moveDirection = Vector3.right;
        private Vector3 startPosition;
        private int hitCount = 0;
        private bool isRunning = false;

        public void StartMode()
        {
            isRunning = true;
            currentSpeed = moveSpeed;
            hitCount = 0;
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
            if (targetPrefab == null)
            {
                targetPrefab = CreateDefaultTarget();
            }

            startPosition = new Vector3(0, moveHeight, moveDistance);
            currentTarget = Instantiate(targetPrefab, startPosition, Quaternion.identity);
            currentTarget.name = "MovingTarget";
            currentTarget.transform.localScale = Vector3.one * targetScale;
            currentTarget.SetActive(true);

            MovingTargetEntity entity = currentTarget.GetComponent<MovingTargetEntity>();
            if (entity == null) entity = currentTarget.AddComponent<MovingTargetEntity>();
            entity.Initialize(this);
        }

        GameObject CreateDefaultTarget()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());

            // 做成圆盘形状（靶子）
            go.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);

            Renderer rend = go.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetColor("_Color", new Color(0.9f, 0.1f, 0.1f));
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Glossiness", 0.6f);
            rend.material = mat;

            // 靶心
            GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            center.name = "Bullseye";
            center.transform.SetParent(go.transform);
            center.transform.localPosition = new Vector3(0, 1f, 0);
            center.transform.localScale = new Vector3(0.3f, 5f, 0.3f);
            Destroy(center.GetComponent<Collider>());

            Material centerMat = new Material(Shader.Find("Standard"));
            centerMat.SetColor("_Color", new Color(1f, 1f, 1f));
            center.GetComponent<Renderer>().material = centerMat;

            return go;
        }

        void Update()
        {
            if (!isRunning || currentTarget == null) return;

            currentTarget.transform.position += moveDirection * currentSpeed * Time.deltaTime;

            // 检查边界
            float dist = Mathf.Abs(currentTarget.transform.position.x - startPosition.x);
            if (dist > moveRange)
            {
                moveDirection = -moveDirection;
                float clampedX = startPosition.x + Mathf.Sign(currentTarget.transform.position.x - startPosition.x) * moveRange;
                currentTarget.transform.position = new Vector3(clampedX, currentTarget.transform.position.y, currentTarget.transform.position.z);
            }

            // 靶子始终面向玩家
            currentTarget.transform.rotation = Quaternion.LookRotation(Vector3.forward);
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

            // 重置靶子位置
            currentTarget.transform.position = startPosition;
            moveDirection = Random.value > 0.5f ? Vector3.right : Vector3.left;

            // 简单的缩放动画
            StartCoroutine(HitScaleAnim());
        }

        IEnumerator HitScaleAnim()
        {
            if (currentTarget == null) yield break;
            Vector3 baseScale = Vector3.one * targetScale;
            currentTarget.transform.localScale = baseScale * 1.3f;
            yield return new WaitForSeconds(0.1f);
            if (currentTarget != null)
                currentTarget.transform.localScale = baseScale;
        }
    }

    public class MovingTargetEntity : MonoBehaviour
    {
        private MovingTargetMode mode;

        public void Initialize(MovingTargetMode m)
        {
            mode = m;
        }

        void OnTriggerEnter(Collider other)
        {
            // 被击中检测
        }
    }
}
