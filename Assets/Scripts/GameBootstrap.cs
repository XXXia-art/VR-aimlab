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
        public float roomDepth = 18f;
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
        public float gridSpacing = 0.5f;
        public float gridHeight = 1.7f;

        [Header("UI")]
        public float uiDistance = 2.5f;
        public float uiHeight = 1.8f;

        private LayerMask targetLayerMask;
        private GridManager gridManager;
        private MovingTargetMode movingTargetMode;

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
            SetupScoreUI(runtime);
            SetupMenu(runtime);
            SetupHitEffects(runtime);
            SetupGameState(runtime);

#if UNITY_EDITOR
            if (!runtime)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
                Debug.Log("[VRAimLab] Scene built successfully! Save the scene to persist changes.");
            }
#endif
        }

        void SetupGameState(bool runtime)
        {
            GameObject go = FindOrCreate("GameStateManager", runtime);
            GameStateManager sm = go.GetComponent<GameStateManager>();
            if (sm == null) sm = go.AddComponent<GameStateManager>();

            sm.OnGameStart += OnGameStart;
            sm.OnGameStop += OnGameStop;
        }

        void OnGameStart()
        {
            CleanupGameplay();

            if (GameStateManager.Instance.SelectedGameMode == GameModeType.Grid5x5)
                StartGridMode();
            else
                StartMovingTargetMode();

            GunSelector selector = FindObjectOfType<GunSelector>();
            selector?.RefreshGun();
        }

        void OnGameStop()
        {
            CleanupGameplay();
        }

        void StartGridMode()
        {
            GameObject gridObj = FindOrCreate("GridManager", true);
            gridManager = gridObj.GetComponent<GridManager>();
            if (gridManager == null) gridManager = gridObj.AddComponent<GridManager>();
            gridManager.gridSize = gridSize;
            gridManager.spacing = gridSpacing;
            gridManager.gridOrigin = new Vector3(0, gridHeight, gridDistance);
            gridManager.maxActiveTargets = maxActiveTargets;
            gridManager.targetScale = targetScale;
            gridManager.targetLayer = targetLayerMask;
            gridManager.RefreshGrid();

            // Create target template
            GameObject targetTemplate = FindOrCreate("TargetTemplate", true);
            SetupTargetTemplate(targetTemplate);
            gridManager.targetPrefab = targetTemplate;
            targetTemplate.SetActive(false);

            // ScoreManager
            GameObject scoreObj = FindOrCreate("ScoreManager", true);
            ScoreManager sm = scoreObj.GetComponent<ScoreManager>();
            if (sm == null) sm = scoreObj.AddComponent<ScoreManager>();
            LinkScoreUI(sm);
        }

        void StartMovingTargetMode()
        {
            GameObject modeObj = FindOrCreate("MovingTargetMode", true);
            movingTargetMode = modeObj.GetComponent<MovingTargetMode>();
            if (movingTargetMode == null) movingTargetMode = modeObj.AddComponent<MovingTargetMode>();
            movingTargetMode.targetLayer = targetLayerMask;
            movingTargetMode.StartMode();

            // ScoreManager
            GameObject scoreObj = FindOrCreate("ScoreManager", true);
            ScoreManager sm = scoreObj.GetComponent<ScoreManager>();
            if (sm == null) sm = scoreObj.AddComponent<ScoreManager>();
            LinkScoreUI(sm);
        }

        void CleanupGameplay()
        {
            GameObject gridObj = GameObject.Find("GridManager");
            if (gridObj != null) Destroy(gridObj);

            GameObject targetTemplate = GameObject.Find("TargetTemplate");
            if (targetTemplate != null) Destroy(targetTemplate);

            GameObject modeObj = GameObject.Find("MovingTargetMode");
            if (modeObj != null)
            {
                var mode = modeObj.GetComponent<MovingTargetMode>();
                mode?.StopMode();
                Destroy(modeObj);
            }

            GameObject movingTarget = GameObject.Find("MovingTarget");
            if (movingTarget != null) Destroy(movingTarget);
        }

        void SetupTargetTemplate(GameObject targetTemplate)
        {
            targetTemplate.transform.localScale = Vector3.one * targetScale;
            targetTemplate.layer = targetLayerIndex;

            Renderer targetRend = targetTemplate.GetComponent<Renderer>();
            if (targetRend == null) targetRend = targetTemplate.AddComponent<MeshRenderer>();
            MeshFilter targetFilter = targetTemplate.GetComponent<MeshFilter>();
            if (targetFilter == null)
            {
                targetFilter = targetTemplate.AddComponent<MeshFilter>();
                targetFilter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            }

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
        }

        void SetupCamera(bool runtime)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "Main Camera";
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

            var camMouseAim = cam.GetComponent<MouseAim>();
            if (camMouseAim == null) camMouseAim = cam.gameObject.AddComponent<MouseAim>();
            camMouseAim.sensitivity = 1.5f;
            camMouseAim.minPitch = -40f;
            camMouseAim.maxPitch = 40f;
        }

        void BuildRoom(bool runtime)
        {
            Texture2D floorTex = CreateCheckerboardTexture(1024, 64, floorColor1, floorColor2);
            Texture2D wallTex = CreateCheckerboardTexture(1024, 64, wallColor, wallColor * 1.15f);

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

            GameObject backWall = FindOrCreatePrimitive("BackWall", PrimitiveType.Cube, runtime);
            backWall.transform.position = new Vector3(0, roomHeight * 0.5f, gridDistance + 0.5f);
            backWall.transform.localScale = new Vector3(roomWidth, roomHeight, 0.2f);
            Material backWallMat = new Material(Shader.Find("Standard"));
            backWallMat.mainTexture = wallTex;
            backWallMat.SetFloat("_Glossiness", 0.05f);
            backWallMat.SetFloat("_Metallic", 0f);
            backWall.GetComponent<Renderer>().material = backWallMat;

            GameObject leftWall = FindOrCreatePrimitive("LeftWall", PrimitiveType.Cube, runtime);
            leftWall.transform.position = new Vector3(-roomWidth * 0.5f - 0.1f, roomHeight * 0.5f, gridDistance * 0.5f);
            leftWall.transform.localScale = new Vector3(0.2f, roomHeight, roomDepth);
            Material leftWallMat = new Material(Shader.Find("Standard"));
            leftWallMat.mainTexture = wallTex;
            leftWallMat.SetFloat("_Glossiness", 0.05f);
            leftWallMat.SetFloat("_Metallic", 0f);
            leftWall.GetComponent<Renderer>().material = leftWallMat;

            GameObject rightWall = FindOrCreatePrimitive("RightWall", PrimitiveType.Cube, runtime);
            rightWall.transform.position = new Vector3(roomWidth * 0.5f + 0.1f, roomHeight * 0.5f, gridDistance * 0.5f);
            rightWall.transform.localScale = new Vector3(0.2f, roomHeight, roomDepth);
            Material rightWallMat = new Material(Shader.Find("Standard"));
            rightWallMat.mainTexture = wallTex;
            rightWallMat.SetFloat("_Glossiness", 0.05f);
            rightWallMat.SetFloat("_Metallic", 0f);
            rightWall.GetComponent<Renderer>().material = rightWallMat;

            GameObject ceiling = FindOrCreatePrimitive("Ceiling", PrimitiveType.Cube, runtime);
            ceiling.transform.position = new Vector3(0, roomHeight, gridDistance * 0.5f);
            ceiling.transform.localScale = new Vector3(roomWidth, 0.1f, roomDepth);
            Material ceilMat = new Material(Shader.Find("Standard"));
            ceilMat.mainTexture = wallTex;
            ceilMat.SetFloat("_Glossiness", 0.05f);
            ceilMat.SetFloat("_Metallic", 0f);
            ceiling.GetComponent<Renderer>().material = ceilMat;

            Light dirLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
            if (dirLight != null)
            {
                dirLight.intensity = 1.2f;
                dirLight.shadows = LightShadows.Soft;
                dirLight.shadowStrength = 0.6f;
                dirLight.color = new Color(1f, 0.98f, 0.95f);
            }

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

            // GunSelector 负责根据选择创建对应枪械
            GunSelector gunSelector = rightHand.GetComponent<GunSelector>();
            if (gunSelector == null)
            {
                gunSelector = rightHand.AddComponent<GunSelector>();
                gunSelector.gunParent = rightHand.transform;
            }
            // 菜单状态下也显示默认枪械
            gunSelector.RefreshGun();

            // VRGun 脚本
            VRGun gun = rightHand.GetComponent<VRGun>();
            if (gun == null) gun = rightHand.AddComponent<VRGun>();
            gun.aimColor = laserAimColor;
            gun.targetLockedColor = laserLockColor;
            gun.controllerNode = XRNode.RightHand;
            gun.useMouseDebug = true;
            gun.targetLayer = targetLayerMask;

            // GunFollowCamera for FPS style
            var gunFollow = rightHand.GetComponent<GunFollowCamera>();
            if (gunFollow == null) gunFollow = rightHand.AddComponent<GunFollowCamera>();
            gunFollow.targetCamera = Camera.main;

            // Screen crosshair
            GameObject crosshairObj = GameObject.Find("ScreenCrosshair");
            if (crosshairObj == null)
            {
                crosshairObj = new GameObject("ScreenCrosshair");
            }
            var crosshair = crosshairObj.GetComponent<ScreenCrosshair>();
            if (crosshair == null) crosshairObj.AddComponent<ScreenCrosshair>();
        }

        void SetupScoreUI(bool runtime)
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
            canvasRT.sizeDelta = new Vector2(800f, 450f);
            canvasObj.transform.localScale = Vector3.one * 0.003f;

            UIFaceCamera uiFace = canvasObj.GetComponent<UIFaceCamera>();
            if (uiFace == null) uiFace = canvasObj.AddComponent<UIFaceCamera>();
            uiFace.cameraTransform = Camera.main.transform;
            uiFace.distance = uiDistance;
            uiFace.height = uiHeight;
            uiFace.lockYRotation = true;

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

            CreateTextElement("Title", canvasObj.transform, "VR AIM LAB", new Vector2(0, 0.75f), new Vector2(1, 1f), 36, new Color(0f, 0.85f, 0.95f), true);
            CreateTextElement("ScoreText", canvasObj.transform, "Score: 0", new Vector2(0, 0.45f), new Vector2(1, 0.72f), 22, Color.white);
            CreateTextElement("HitsText", canvasObj.transform, "Hits: 0", new Vector2(0, 0.2f), new Vector2(0.33f, 0.45f), 18, Color.white);
            CreateTextElement("ShotsText", canvasObj.transform, "Shots: 0", new Vector2(0.33f, 0.2f), new Vector2(0.66f, 0.45f), 18, Color.white);
            CreateTextElement("AccuracyText", canvasObj.transform, "Accuracy: 0.0%", new Vector2(0.66f, 0.2f), new Vector2(1, 0.45f), 18, Color.white);
            CreateTextElement("TimeText", canvasObj.transform, "Time: 00:00", new Vector2(0, -0.05f), new Vector2(1, 0.2f), 20, new Color(0.7f, 0.7f, 0.7f));
        }

        void SetupMenu(bool runtime)
        {
            // 左墙菜单面板
            GameObject menuCanvasObj = GameObject.Find("MenuCanvas");
            if (menuCanvasObj == null)
            {
                menuCanvasObj = new GameObject("MenuCanvas");
            }

            Canvas menuCanvas = menuCanvasObj.GetComponent<Canvas>();
            if (menuCanvas == null)
            {
                menuCanvas = menuCanvasObj.AddComponent<Canvas>();
                menuCanvas.renderMode = RenderMode.WorldSpace;
                menuCanvas.worldCamera = Camera.main;
            }
            menuCanvas.sortingOrder = 100;

            CanvasScaler menuScaler = menuCanvasObj.GetComponent<CanvasScaler>();
            if (menuScaler == null)
            {
                menuScaler = menuCanvasObj.AddComponent<CanvasScaler>();
                menuScaler.dynamicPixelsPerUnit = 1f;
                menuScaler.referencePixelsPerUnit = 100f;
            }

            RectTransform menuRT = menuCanvasObj.GetComponent<RectTransform>();
            if (menuRT == null) menuRT = menuCanvasObj.AddComponent<RectTransform>();
            menuRT.sizeDelta = new Vector2(700f, 500f);
            menuCanvasObj.transform.localScale = Vector3.one * 0.003f;

            // 放在左墙上，面向房间中心
            Vector3 menuPos = new Vector3(-roomWidth * 0.5f + 0.25f, 1.8f, gridDistance * 0.5f);
            menuCanvasObj.transform.position = menuPos;
            menuCanvasObj.transform.LookAt(new Vector3(0, 1.6f, gridDistance * 0.3f));
            menuCanvasObj.transform.Rotate(0, 180, 0); // Canvas 背面是正面，需要翻转

            // 背景
            GameObject menuPanel = GameObject.Find("MenuPanel");
            if (menuPanel == null)
            {
                menuPanel = new GameObject("MenuPanel");
                menuPanel.transform.SetParent(menuCanvasObj.transform, false);
                RectTransform rt = menuPanel.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                UnityEngine.UI.Image img = menuPanel.AddComponent<UnityEngine.UI.Image>();
                img.color = new Color(0.05f, 0.05f, 0.07f, 0.92f);
            }

            // 标题
            CreateTextElement("MenuTitle", menuCanvasObj.transform, "设 置", new Vector2(0, 0.82f), new Vector2(1, 1f), 40, new Color(0f, 0.85f, 0.95f), true);

            // 游戏模式选择
            CreateTextElement("ModeLabel", menuCanvasObj.transform, "游戏模式", new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.78f), 22, new Color(0.8f, 0.8f, 0.8f));
            CreateButton("ModeLeftBtn", menuCanvasObj.transform, "<", new Vector2(0.1f, 0.48f), new Vector2(0.2f, 0.6f), () => { });
            CreateTextElement("ModeValue", menuCanvasObj.transform, "5x5 网格射击", new Vector2(0.22f, 0.48f), new Vector2(0.78f, 0.6f), 24, Color.white);
            CreateButton("ModeRightBtn", menuCanvasObj.transform, ">", new Vector2(0.8f, 0.48f), new Vector2(0.9f, 0.6f), () => { });

            // 枪械选择
            CreateTextElement("GunLabel", menuCanvasObj.transform, "枪械选择", new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.46f), 22, new Color(0.8f, 0.8f, 0.8f));
            CreateButton("GunLeftBtn", menuCanvasObj.transform, "<", new Vector2(0.1f, 0.18f), new Vector2(0.2f, 0.3f), () => { });
            CreateTextElement("GunValue", menuCanvasObj.transform, "手枪", new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.3f), 24, Color.white);
            CreateButton("GunRightBtn", menuCanvasObj.transform, ">", new Vector2(0.8f, 0.18f), new Vector2(0.9f, 0.3f), () => { });

            // 开始/停止按钮
            CreateButton("StartBtn", menuCanvasObj.transform, "开始游戏", new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.14f), () => { }, new Color(0.1f, 0.6f, 0.3f));
            CreateButton("StopBtn", menuCanvasObj.transform, "停止游戏", new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.14f), () => { }, new Color(0.6f, 0.15f, 0.15f));

            // MenuPanel 控制器
            MenuPanel panel = menuCanvasObj.GetComponent<MenuPanel>();
            if (panel == null) panel = menuCanvasObj.AddComponent<MenuPanel>();
            panel.menuCanvas = menuCanvas;
            panel.titleText = GetText("MenuTitle");
            panel.modeText = GetText("ModeValue");
            panel.gunText = GetText("GunValue");
            panel.startButton = GetButton("StartBtn");
            panel.stopButton = GetButton("StopBtn");
            panel.modeLeftButton = GetButton("ModeLeftBtn");
            panel.modeRightButton = GetButton("ModeRightBtn");
            panel.gunLeftButton = GetButton("GunLeftBtn");
            panel.gunRightButton = GetButton("GunRightBtn");
        }

        void SetupHitEffects(bool runtime)
        {
            GameObject poolObj = GameObject.Find("HitEffectPool");
            if (poolObj == null) poolObj = new GameObject("HitEffectPool");
            HitEffectPool pool = poolObj.GetComponent<HitEffectPool>();
            if (pool == null) pool = poolObj.AddComponent<HitEffectPool>();
        }

        void LinkScoreUI(ScoreManager sm)
        {
            sm.scoreText = GetText("ScoreText");
            sm.hitsText = GetText("HitsText");
            sm.shotsText = GetText("ShotsText");
            sm.accuracyText = GetText("AccuracyText");
            sm.timeText = GetText("TimeText");
        }

        // ============ Helpers ============

        GameObject FindOrCreate(string name, bool runtime)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            return go;
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
                if (runtime) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        Texture2D CreateCheckerboardTexture(int size, int checkSize, Color c1, Color c2)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 4;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool even = ((x / checkSize) + (y / checkSize)) % 2 == 0;
                    tex.SetPixel(x, y, even ? c1 : c2);
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
            else if (go.transform.parent != parent) go.transform.SetParent(parent, false);

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
            tmp.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            tmp.autoSizeTextContainer = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TMPro.TextOverflowModes.Overflow;
        }

        void CreateButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick, Color? bgColor = null)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
            }
            else if (go.transform.parent != parent) go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            UnityEngine.UI.Image img = go.GetComponent<UnityEngine.UI.Image>();
            if (img == null) img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = bgColor ?? new Color(0.2f, 0.2f, 0.25f, 0.9f);

            UnityEngine.UI.Button btn = go.GetComponent<UnityEngine.UI.Button>();
            if (btn == null) btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.onClick.RemoveAllListeners();
            if (onClick != null) btn.onClick.AddListener(onClick);

            // Label child
            Transform labelTr = go.transform.Find("Label");
            GameObject labelObj = labelTr != null ? labelTr.gameObject : null;
            if (labelObj == null)
            {
                labelObj = new GameObject("Label");
                labelObj.transform.SetParent(go.transform, false);
            }
            RectTransform labelRT = labelObj.GetComponent<RectTransform>();
            if (labelRT == null) labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI labelTMP = labelObj.GetComponent<TMPro.TextMeshProUGUI>();
            if (labelTMP == null) labelTMP = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 20;
            labelTMP.color = Color.white;
            labelTMP.alignment = TMPro.TextAlignmentOptions.Center;
            labelTMP.autoSizeTextContainer = false;
            labelTMP.enableWordWrapping = false;
        }

        TMPro.TextMeshProUGUI GetText(string name)
        {
            GameObject go = GameObject.Find(name);
            return go?.GetComponent<TMPro.TextMeshProUGUI>();
        }

        UnityEngine.UI.Button GetButton(string name)
        {
            GameObject go = GameObject.Find(name);
            return go?.GetComponent<UnityEngine.UI.Button>();
        }

        void CleanupRuntimeObjects()
        {
            string[] names = new[] {
                "Floor", "BackWall", "LeftWall", "RightWall", "Ceiling",
                "RightHand", "GunVisual", "GunRoot", "GunBody", "GunGrip", "GunBarrel", "AK47",
                "TargetTemplate", "GridManager", "ScoreManager", "ScoreCanvas", "Panel", "Title",
                "ScoreText", "HitsText", "ShotsText", "AccuracyText", "TimeText",
                "HitEffectPool", "FillLight", "ScreenCrosshair",
                "CrosshairTop", "CrosshairBottom", "CrosshairLeft", "CrosshairRight",
                "MenuCanvas", "MenuPanel", "MenuTitle", "ModeLabel", "ModeValue", "GunLabel", "GunValue",
                "ModeLeftBtn", "ModeRightBtn", "GunLeftBtn", "GunRightBtn", "StartBtn", "StopBtn",
                "GameStateManager", "MovingTargetMode", "MovingTarget"
            };
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
