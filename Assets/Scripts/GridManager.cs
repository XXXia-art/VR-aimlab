using System.Collections.Generic;
using UnityEngine;

namespace VRAimLab
{
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int gridSize = 5;
        public float spacing = 1.2f;
        public float spacingX = 1.2f;
        public float spacingY = 1.2f;
        public Vector3 gridOrigin = new Vector3(0, 1.6f, 6f);

        [Header("Target Settings")]
        public GameObject targetPrefab;
        public int maxActiveTargets = 3;
        public float targetScale = 0.35f;
        public LayerMask targetLayer;

        [Header("Gizmos")]
        public bool drawGizmos = true;

        private List<Vector3> gridPositions = new List<Vector3>();
        private List<GameObject> activeTargets = new List<GameObject>();
        private Queue<GameObject> targetPool = new Queue<GameObject>();
        private List<int> occupiedIndices = new List<int>();

        [Header("Game Timer")]
        public float gameDuration = 30f;
        private float gameStartTime;
        private bool isRunning = false;

        void Start()
        {
            if (gridPositions == null || gridPositions.Count == 0)
                GenerateGridPositions();
            InitializePool();
            SpawnInitialTargets();
        }

        public void StartGame()
        {
            isRunning = true;
            gameStartTime = Time.time;
            ScoreManager.Instance?.ResetScore();
        }

        public void StopGame()
        {
            isRunning = false;
            ScoreManager.Instance?.RecordAndShowResult();
            GameStateManager.Instance?.StopGame();
        }

        void Update()
        {
            if (!isRunning) return;
            if (Time.time - gameStartTime >= gameDuration)
            {
                StopGame();
            }
        }

        public void RefreshGrid()
        {
            gridPositions.Clear();
            GenerateGridPositions();
        }

        void GenerateGridPositions()
        {
            gridPositions.Clear();
            float sx = spacingX > 0 ? spacingX : spacing;
            float sy = spacingY > 0 ? spacingY : spacing;
            float offsetX = (gridSize - 1) * sx * 0.5f;
            float offsetY = (gridSize - 1) * sy * 0.5f;
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector3 pos = new Vector3(
                        gridOrigin.x + x * sx - offsetX,
                        gridOrigin.y + y * sy - offsetY,
                        gridOrigin.z
                    );
                    gridPositions.Add(pos);
                }
            }
        }

        void InitializePool()
        {
            int poolSize = maxActiveTargets + 3;
            for (int i = 0; i < poolSize; i++)
            {
                if (targetPrefab == null) break;
                GameObject obj = Instantiate(targetPrefab, transform);
                obj.name = $"Target_Pooled_{i}";
                obj.SetActive(false);
                targetPool.Enqueue(obj);
            }
        }

        void SpawnInitialTargets()
        {
            for (int i = 0; i < maxActiveTargets; i++)
            {
                SpawnTarget();
            }
        }

        public void SpawnTarget()
        {
            if (activeTargets.Count >= maxActiveTargets) return;

            List<int> available = new List<int>();
            for (int i = 0; i < gridPositions.Count; i++)
            {
                if (!occupiedIndices.Contains(i))
                    available.Add(i);
            }

            if (available.Count == 0) return;

            int randIndex = available[Random.Range(0, available.Count)];
            occupiedIndices.Add(randIndex);

            GameObject target = GetFromPool();
            target.transform.position = gridPositions[randIndex];
            target.transform.rotation = Quaternion.identity;
            target.SetActive(true);

            Target t = target.GetComponent<Target>();
            if (t != null)
            {
                t.Initialize(this, randIndex, targetScale);
                t.PlaySpawnAnimation();
            }

            activeTargets.Add(target);
        }

        GameObject GetFromPool()
        {
            if (targetPool.Count > 0)
                return targetPool.Dequeue();

            if (targetPrefab != null)
                return Instantiate(targetPrefab, transform);

            return null;
        }

        public void ReturnTarget(GameObject target, int gridIndex)
        {
            occupiedIndices.Remove(gridIndex);
            activeTargets.Remove(target);
            target.SetActive(false);
            targetPool.Enqueue(target);

            SpawnTarget();
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            float sx = spacingX > 0 ? spacingX : spacing;
            float sy = spacingY > 0 ? spacingY : spacing;
            float offsetX = (gridSize - 1) * sx * 0.5f;
            float offsetY = (gridSize - 1) * sy * 0.5f;
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector3 pos = new Vector3(
                        gridOrigin.x + x * sx - offsetX,
                        gridOrigin.y + y * sy - offsetY,
                        gridOrigin.z
                    );
                    Gizmos.DrawWireSphere(pos, targetScale * 0.5f);
                }
            }
        }
    }
}
