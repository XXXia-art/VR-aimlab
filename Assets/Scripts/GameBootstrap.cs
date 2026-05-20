using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace VRAimLab
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("XR Rig")]
        public bool setupXR = true;

        [Header("Room")]
        public float roomWidth = 12f;
        public float roomDepth = 12f;
        public float roomHeight = 4f;
        public float gridDistance = 6f;

        [Header("Visual")]
        public Color wallColor = new Color(0.35f, 0.35f, 0.37f);
        public Color floorColor1 = new Color(0.55f, 0.55f, 0.55f);
        public Color floorColor2 = new Color(0.75f, 0.75f, 0.75f);
        public Color targetColor = new Color(0.35f, 0.85f, 0.95f);
        public Color laserAimColor = Color.green;
        public Color laserLockColor = Color.red;

        [Header("Target")]
        public float targetScale = 0.35f;
        public int maxActiveTargets = 3;
        public int targetLayerIndex = 0;

        [Header("Grid")]
        public int gridSize = 5;
        public float gridSpacing = 0.7f;
        public float gridHeight = 2.2f;

        [Header("UI")]
        public float uiDistance = 2.5f;
        public float uiHeight = 1.8f;

        private LayerMask targetLayerMask;

        [ContextMenu("VRAimLab/Build Full Scene")]
        void BuildSceneMenu()
        {
            BuildFullScene(false);
        }

        [ContextMenu("VRAimLab/Cleanup Runtime Objects")]
        void CleanupMenu()
        {
            CleanupRuntimeObjects();
        }

        void Awake()
        {
            targetLayerMask = 1 << targetLayerIndex;
            BuildFullScene(true);
        }

        void BuildFullScene(bool runtime)
        {
            SetupCamera(runtime);
            BuildRoom(runtime);
            SetupGun(runtime);
            SetupGameplay(runtime);
            SetupUI(runtime);
            SetupHitEffects(runtime);

#if UNITY_EDITOR
            if (!runtime)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
                Debug.Log("[VRAimLab] Scene built successfully! Save the scene to persist changes.");
            }
#endif
        }

        void SetupCamera(bool runtime)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
            }

            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.09f);
            cam.allowHDR = false;
            cam.allowMSAA = true;

            if (setupXR)
            {
                var tracker = cam.GetComponent<XRCameraTracker>();
                if (tracker == null) tracker = cam.gameObject.AddComponent<XRCameraTracker>();
            }

            // Editor FPS-style mouse look on camera
            var camMouseAim = cam.GetComponent<MouseAim>();
            if (camMouseAim == null) camMouseAim = cam.gameObject.AddComponent<MouseAim>();
            camMouseAim.sensitivity = 1.5f;
            camMouseAim.minPitch = -40f;
            camMouseAim.maxPitch = 40f;
        }

        void BuildRoom(bool runtime)
        {
            // High-res checkerboard textures
            Texture2D floorTex = CreateCheckerboardTexture(1024, 64, floorColor1, floorColor2);
            Texture2D wallTex = CreateCheckerboardTexture(1024, 64, wallColor, wallColor * 1.15f);

            // Floor
            GameObject floor = GameObject.Find("Floor");
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Floor";
                DestroyCollider(floor, runtime);
            }
            floor.transform.position = new Vector3(0, 0, gridDistance * 0.5f);
            floor.transform.localScale = new Vector3(roomWidth / 10f, 1, roomDepth / 10f);

            Material floorMat = new Material(Shader.Find("Standard"));
            floorMat.mainTexture = floorTex;
            floorMat.SetFloat("_Glossiness", 0.1f);
            floorMat.SetFloat("_Metallic", 0f);
            floor.GetComponent<Renderer>().material = floorMat;

            // Back Wall (behind targets) - checkerboard like floor
            GameObject backWall = FindOrCreatePrimitive("BackWall", PrimitiveType.Cube, runtime);
            backWall.transform.position = new Vector3(0, roomHeight * 0.5f, gridDistance + 0.5f);
            backWall.transform.localScale = new Vector3(roomWidth, roomHeight, 0.2f);
            Material backWallMat = new Material(Shader.Find("Standard"));
            backWallMat.mainTexture = wallTex;
            backWallMat.SetFloat("_Glossiness", 0.05f);
            backWallMat.SetFloat("_Metallic", 0f);
            backWall.GetComponent<Renderer>().material = backWallMat;

            // Left Wall
            GameObject leftWall = FindOrCreatePrimitive("LeftWall", PrimitiveType.Cube, runtime);
            leftWall.transform.position = new Vector3(-roomWidth * 0.5f - 0.1f, roomHeight * 0.5f, gridDistance * 0.5f);
            leftWall.transform.localScale = new Vector3(0.2f, roomHeight, roomDepth);
            Material leftWallMat = new Material(Shader.Find("Standard"));
            leftWallMat.mainTexture = wallTex;
            leftWallMat.SetFloat("_Glossiness", 0.05f);
            leftWallMat.SetFloat("_Metallic", 0f);
            leftWall.GetComponent<Renderer>().material = leftWallMat;

            // Right Wall
            GameObject rightWall = FindOrCreatePrimitive("RightWall", PrimitiveType.Cube, runtime);
            rightWall.transform.position = new Vector3(roomWidth * 0.5f + 0.1f, roomHeight * 0.5f, gridDistance * 0.5f);
            rightWall.transform.localScale = new Vector3(0.2f, roomHeight, roomDepth);
            Material rightWallMat = new Material(Shader.Find("Standard"));
            rightWallMat.mainTexture = wallTex;
            rightWallMat.SetFloat("_Glossiness", 0.05f);
            rightWallMat.SetFloat("_Metallic", 0f);
            rightWall.GetComponent<Renderer>().material = rightWallMat;

            // Ceiling
            GameObject ceiling = FindOrCreatePrimitive("Ceiling", PrimitiveType.Cube, runtime);
            ceiling.transform.position = new Vector3(0, roomHeight, gridDistance * 0.5f);
            ceiling.transform.localScale = new Vector3(roomWidth, 0.1f, roomDepth);
            Material ceilMat = new Material(Shader.Find("Standard"));
            ceilMat.mainTexture = wallTex;
            ceilMat.SetFloat("_Glossiness", 0.05f);
            ceilMat.SetFloat("_Metallic", 0f);
            ceiling.GetComponent<Renderer>().material = ceilMat;

            // Improve lighting - setup directional light with shadows
            Light dirLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
            if (dirLight != null)
            {
                dirLight.intensity = 1.2f;
                dirLight.shadows = LightShadows.Soft;
                dirLight.shadowStrength = 0.6f;
                dirLight.color = new Color(1f, 0.98f, 0.95f);
            }

            // Add ambient fill light
            GameObject fillLight = GameObject.Find("FillLight");
            if (fillLight == null)
            {
                fillLight = new GameObject("FillLight");
                fillLight.transform.position = new Vector3(0, roomHeight - 0.5f, gridDistance * 0.3f);
            }
            Light fill = fillLight.GetComponent<Light>();
            if (fill == null)
            {
                fill = fillLight.AddComponent<Light>();
                fill.type = LightType.Point;
                fill.intensity = 0.4f;
                fill.range = 15f;
                fill.color = new Color(0.9f, 0.95f, 1f);
            }
        }

        void SetupGun(bool runtime)
        {
            GameObject rightHand = GameObject.Find("RightHand");
            if (rightHand == null)
            {
                rightHand = new GameObject("RightHand");
                rightHand.transform.position = new Vector3(0.2f, 1.3f, 0.2f);
            }

            if (setupXR)
            {
                var tracker = rightHand.GetComponent<XRControllerTracker>();
                if (tracker == null) tracker = rightHand.AddComponent<XRControllerTracker>();
                tracker.node = XRNode.RightHand;
            }

            // Gun model - build a more realistic pistol shape from primitives
            GameObject gunRoot = GameObject.Find("GunRoot");
            if (gunRoot == null)
            {
                gunRoot = new GameObject("GunRoot");
            }
            gunRoot.transform.SetParent(rightHand.transform);
            gunRoot.transform.localPosition = new Vector3(0, -0.05f, 0.1f);
            gunRoot.transform.localRotation = Quaternion.Euler(0, 0, 0);

            // In editor, gun follows camera for FPS-style aiming
            var gunFollow = gunRoot.GetComponent<GunFollowCamera>();
            if (gunFollow == null) gunFollow = gunRoot.AddComponent<GunFollowCamera>();
            gunFollow.targetCamera = Camera.main;

            // Gun body (slide)
            GameObject gunBody = GameObject.Find("GunBody");
            if (gunBody == null)
            {
                gunBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gunBody.name = "GunBody";
                DestroyCollider(gunBody, runtime);
            }
            gunBody.transform.SetParent(gunRoot.transform);
            gunBody.transform.localPosition = new Vector3(0, 0, 0.05f);
            gunBody.transform.localScale = new Vector3(0.03f, 0.045f, 0.16f);

            // Gun grip (handle)
            GameObject gunGrip = GameObject.Find("GunGrip");
            if (gunGrip == null)
            {
                gunGrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gunGrip.name = "GunGrip";
                DestroyCollider(gunGrip, runtime);
            }
            gunGrip.transform.SetParent(gunRoot.transform);
            gunGrip.transform.localPosition = new Vector3(0, -0.06f, -0.04f);
            gunGrip.transform.localRotation = Quaternion.Euler(15f, 0, 0);
            gunGrip.transform.localScale = new Vector3(0.03f, 0.08f, 0.05f);

            // Gun barrel
            GameObject gunBarrel = GameObject.Find("GunBarrel");
            if (gunBarrel == null)
            {
                gunBarrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                gunBarrel.name = "GunBarrel";
                DestroyCollider(gunBarrel, runtime);
            }
            gunBarrel.transform.SetParent(gunRoot.transform);
            gunBarrel.transform.localPosition = new Vector3(0, 0.01f, 0.16f);
            gunBarrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
            gunBarrel.transform.localScale = new Vector3(0.012f, 0.04f, 0.012f);

            Material gunMat = new Material(Shader.Find("Standard"));
            gunMat.SetColor("_Color", new Color(0.12f, 0.12f, 0.14f));
            gunMat.SetFloat("_Metallic", 0.7f);
            gunMat.SetFloat("_Glossiness", 0.5f);
            gunBody.GetComponent<Renderer>().material = gunMat;
            gunGrip.GetComponent<Renderer>().material = gunMat;
            gunBarrel.GetComponent<Renderer>().material = gunMat;

            // VRGun script
            VRGun gun = rightHand.GetComponent<VRGun>();
            if (gun == null) gun = rightHand.AddComponent<VRGun>();
            gun.muzzleTransform = gunBarrel.transform;
            gun.aimColor = laserAimColor;
            gun.targetLockedColor = laserLockColor;
            gun.controllerNode = XRNode.RightHand;
            gun.useMouseDebug = true;
            gun.targetLayer = targetLayerMask;

            // Screen crosshair for editor debugging
            GameObject crosshairObj = GameObject.Find("ScreenCrosshair");
            if (crosshairObj == null)
            {
                crosshairObj = new GameObject("ScreenCrosshair");
            }
            var crosshair = crosshairObj.GetComponent<ScreenCrosshair>();
            if (crosshair == null) crosshairObj.AddComponent<ScreenCrosshair>();
        }

        void SetupGameplay(bool runtime)
        {
            // Create Target Template
            GameObject targetTemplate = GameObject.Find("TargetTemplate");
            if (targetTemplate == null)
            {
                targetTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                targetTemplate.name = "TargetTemplate";
                DestroyCollider(targetTemplate, runtime);
            }
            targetTemplate.transform.localScale = Vector3.one * targetScale;
            targetTemplate.layer = targetLayerIndex;

            Renderer targetRend = targetTemplate.GetComponent<Renderer>();
            Material targetMat = new Material(Shader.Find("Standard"));
            targetMat.SetColor("_Color", targetColor);
            targetMat.SetFloat("_Metallic", 0.1f);
            targetMat.SetFloat("_Glossiness", 0.85f);
            targetMat.EnableKeyword("_EMISSION");
            targetMat.SetColor("_EmissionColor", targetColor * 0.4f);
            targetRend.material = targetMat;
            targetRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            targetRend.receiveShadows = true;

            SphereCollider col = targetTemplate.GetComponent<SphereCollider>();
            if (col == null) col = targetTemplate.AddComponent<SphereCollider>();
            col.isTrigger = true;

            Target targetScript = targetTemplate.GetComponent<Target>();
            if (targetScript == null) targetTemplate.AddComponent<Target>();

            targetTemplate.SetActive(false);

            // Grid Manager
            GameObject gridObj = GameObject.Find("GridManager");
            if (gridObj == null) gridObj = new GameObject("GridManager");
            GridManager gm = gridObj.GetComponent<GridManager>();
            if (gm == null) gm = gridObj.AddComponent<GridManager>();
            gm.gridSize = gridSize;
            gm.spacing = gridSpacing;
            gm.gridOrigin = new Vector3(0, gridHeight, gridDistance);
            gm.targetPrefab = targetTemplate;
            gm.maxActiveTargets = maxActiveTargets;
            gm.targetScale = targetScale;
            gm.targetLayer = targetLayerMask;
            gm.RefreshGrid();

            // Score Manager
            GameObject scoreObj = GameObject.Find("ScoreManager");
            if (scoreObj == null) scoreObj = new GameObject("ScoreManager");
            ScoreManager sm = scoreObj.GetComponent<ScoreManager>();
            if (sm == null) sm = scoreObj.AddComponent<ScoreManager>();
        }

        void SetupUI(bool runtime)
        {
            GameObject canvasObj = GameObject.Find("ScoreCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("ScoreCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 1f;
                scaler.referencePixelsPerUnit = 100f;

                GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            }

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            // Use pixel-based size then scale down to world space
            canvasRT.sizeDelta = new Vector2(800f, 450f);
            canvasObj.transform.localScale = Vector3.one * 0.003f;

            UIFaceCamera uiFace = canvasObj.GetComponent<UIFaceCamera>();
            if (uiFace == null) uiFace = canvasObj.AddComponent<UIFaceCamera>();
            uiFace.cameraTransform = Camera.main.transform;
            uiFace.distance = uiDistance;
            uiFace.height = uiHeight;
            uiFace.lockYRotation = true;

            // Background panel
            GameObject panel = GameObject.Find("Panel");
            if (panel == null)
            {
                panel = new GameObject("Panel");
                panel.transform.SetParent(canvasObj.transform, false);
                RectTransform panelRT = panel.AddComponent<RectTransform>();
                panelRT.anchorMin = Vector2.zero;
                panelRT.anchorMax = Vector2.one;
                panelRT.offsetMin = Vector2.zero;
                panelRT.offsetMax = Vector2.zero;

                UnityEngine.UI.Image panelImg = panel.AddComponent<UnityEngine.UI.Image>();
                panelImg.color = new Color(0.05f, 0.05f, 0.06f, 0.85f);
            }

            // Title
            CreateTextElement("Title", canvasObj.transform, "VR AIM LAB", new Vector2(0, 0.75f), new Vector2(1, 1f), 36, new Color(0f, 0.85f, 0.95f), true);

            // Score
            CreateTextElement("ScoreText", canvasObj.transform, "Score: 0", new Vector2(0, 0.45f), new Vector2(1, 0.72f), 22, Color.white);

            // Stats row
            CreateTextElement("HitsText", canvasObj.transform, "Hits: 0", new Vector2(0, 0.2f), new Vector2(0.33f, 0.45f), 18, Color.white);
            CreateTextElement("ShotsText", canvasObj.transform, "Shots: 0", new Vector2(0.33f, 0.2f), new Vector2(0.66f, 0.45f), 18, Color.white);
            CreateTextElement("AccuracyText", canvasObj.transform, "Accuracy: 0.0%", new Vector2(0.66f, 0.2f), new Vector2(1, 0.45f), 18, Color.white);

            // Time
            CreateTextElement("TimeText", canvasObj.transform, "Time: 00:00", new Vector2(0, -0.05f), new Vector2(1, 0.2f), 20, new Color(0.7f, 0.7f, 0.7f));

            // Link references to ScoreManager
            ScoreManager sm = FindFirstObjectByType<ScoreManager>();
            if (sm != null)
            {
                sm.scoreText = GetText("ScoreText");
                sm.hitsText = GetText("HitsText");
                sm.shotsText = GetText("ShotsText");
                sm.accuracyText = GetText("AccuracyText");
                sm.timeText = GetText("TimeText");
            }
        }

        void SetupHitEffects(bool runtime)
        {
            GameObject poolObj = GameObject.Find("HitEffectPool");
            if (poolObj == null) poolObj = new GameObject("HitEffectPool");
            HitEffectPool pool = poolObj.GetComponent<HitEffectPool>();
            if (pool == null) pool = poolObj.AddComponent<HitEffectPool>();
        }

        GameObject FindOrCreatePrimitive(string name, PrimitiveType type, bool runtime)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                go = GameObject.CreatePrimitive(type);
                go.name = name;
                DestroyCollider(go, runtime);
            }
            return go;
        }

        void DestroyCollider(GameObject go, bool runtime)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (runtime)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }
        }

        void ApplyMaterial(GameObject go, Color color)
        {
            MeshRenderer rend = go.GetComponent<MeshRenderer>();
            if (rend == null) return;

            Material mat = new Material(Shader.Find("Standard"));
            mat.SetColor("_Color", color);
            mat.SetFloat("_Glossiness", 0.05f);
            mat.SetFloat("_Metallic", 0f);
            rend.material = mat;
        }

        Texture2D CreateCheckerboardTexture(int size, int checkSize, Color c1, Color c2)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 4;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool even = ((x / checkSize) + (y / checkSize)) % 2 == 0;
                    tex.SetPixel(x, y, even ? c1 : c2);
                }
            }
            tex.Apply();
            return tex;
        }

        void CreateTextElement(string name, Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, bool bold = false)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
            }
            else if (go.transform.parent != parent)
            {
                go.transform.SetParent(parent, false);
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            TMPro.TextMeshProUGUI tmp = go.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.autoSizeTextContainer = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TMPro.TextOverflowModes.Overflow;
            tmp.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
        }

        TMPro.TextMeshProUGUI GetText(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
                return go.GetComponent<TMPro.TextMeshProUGUI>();
            return null;
        }

        void CleanupRuntimeObjects()
        {
            string[] names = new[] { "Floor", "BackWall", "LeftWall", "RightWall", "Ceiling", 
                                     "RightHand", "GunVisual", "GunRoot", "GunBody", "GunGrip", "GunBarrel",
                                     "TargetTemplate", "GridManager", "ScoreManager", "ScoreCanvas", "Panel", "Title", 
                                     "ScoreText", "HitsText", "ShotsText", "AccuracyText", 
                                     "TimeText", "HitEffectPool", "FillLight", "ScreenCrosshair",
                                     "CrosshairTop", "CrosshairBottom", "CrosshairLeft", "CrosshairRight" };
            foreach (var n in names)
            {
                GameObject go = GameObject.Find(n);
                if (go != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(go);
#else
                    Destroy(go);
#endif
                }
            }
        }
    }
}
