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

            // Back Wall (behind targets, pushed back for clearance)
            float wallZ = gridDistance + 2.5f;
            GameObject backWall = FindOrCreatePrimitive("BackWall", PrimitiveType.Cube, runtime);
            backWall.transform.position = new Vector3(0, roomHeight * 0.5f, wallZ);
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

            // ── Cover blocks — two rows in front of back wall ──
            float wallFaceZ = wallZ - 0.1f;
            float blockDepth = 1f;
            float totalW = roomWidth - 0.8f;
            int cols = 6;
            float blockW = totalW / cols;
            float startX = -(totalW * 0.5f) + blockW * 0.5f;

            // Reuse back wall material for cover blocks

            float rowH = 1f;

            for (int row = 0; row < 2; row++)
            {
                float yBase = row * rowH;

                for (int col = 0; col < cols; col++)
                {
                    string name = "CoverBlock_" + row + "_" + col;
                    GameObject block = FindOrCreatePrimitive(name, PrimitiveType.Cube, runtime);
                    block.transform.position = new Vector3(startX + col * blockW, yBase + rowH * 0.5f, wallFaceZ);
                    block.transform.localScale = new Vector3(blockW, rowH, blockDepth);
                    block.GetComponent<Renderer>().material = backWallMat;
                }
            }

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

            // ═══════════════════════════════════════════
            //  REALISTIC PISTOL GEOMETRY
            // ═══════════════════════════════════════════

            // ── Slide (main body) ──
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

            // ── Underslide rail ──
            GameObject underRail = FindOrCreatePrimitive("UnderRail", PrimitiveType.Cube, runtime);
            underRail.transform.SetParent(gunRoot.transform);
            underRail.transform.localPosition = new Vector3(0, -0.022f, 0.06f);
            underRail.transform.localScale = new Vector3(0.024f, 0.006f, 0.09f);

            // ── Grip ──
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

            // ── Beavertail ──
            GameObject beavertail = FindOrCreatePrimitive("Beavertail", PrimitiveType.Cube, runtime);
            beavertail.transform.SetParent(gunRoot.transform);
            beavertail.transform.localPosition = new Vector3(0, -0.025f, -0.095f);
            beavertail.transform.localRotation = Quaternion.Euler(-15f, 0, 0);
            beavertail.transform.localScale = new Vector3(0.03f, 0.005f, 0.014f);

            // ── Barrel: chamber block + muzzle ──
            GameObject barrelChamber = FindOrCreatePrimitive("BarrelChamber", PrimitiveType.Cube, runtime);
            barrelChamber.transform.SetParent(gunRoot.transform);
            barrelChamber.transform.localPosition = new Vector3(0, 0.01f, 0.14f);
            barrelChamber.transform.localScale = new Vector3(0.018f, 0.022f, 0.02f);

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

            // ── Ejection port ──
            GameObject ejectionPort = FindOrCreatePrimitive("EjectionPort", PrimitiveType.Cube, runtime);
            ejectionPort.transform.SetParent(gunRoot.transform);
            ejectionPort.transform.localPosition = new Vector3(0.012f, 0.024f, 0.04f);
            ejectionPort.transform.localScale = new Vector3(0.012f, 0.003f, 0.016f);

            // ── Slide serrations — rear, both sides ──
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    string serName = "Serration_" + (side > 0 ? "R_" : "L_") + i;
                    GameObject ser = FindOrCreatePrimitive(serName, PrimitiveType.Cube, runtime);
                    ser.transform.SetParent(gunRoot.transform);
                    ser.transform.localPosition = new Vector3(side * 0.016f, 0.008f, -0.05f + i * 0.007f);
                    ser.transform.localScale = new Vector3(0.0015f, 0.032f, 0.003f);
                }
            }

            // ── Trigger guard ──
            GameObject triggerGuard = GameObject.Find("TriggerGuard");
            if (triggerGuard == null)
            {
                triggerGuard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                triggerGuard.name = "TriggerGuard";
                DestroyCollider(triggerGuard, runtime);
            }
            triggerGuard.transform.SetParent(gunRoot.transform);
            triggerGuard.transform.localPosition = new Vector3(0, -0.048f, -0.04f);
            triggerGuard.transform.localScale = new Vector3(0.014f, 0.018f, 0.054f);

            // ── Trigger ──
            GameObject trigger = FindOrCreatePrimitive("Trigger", PrimitiveType.Cylinder, runtime);
            trigger.transform.SetParent(gunRoot.transform);
            trigger.transform.localPosition = new Vector3(0, -0.05f, -0.04f);
            trigger.transform.localRotation = Quaternion.Euler(0, 0, 90);
            trigger.transform.localScale = new Vector3(0.003f, 0.012f, 0.003f);

            // ── Slide stop lever ──
            GameObject slideStop = FindOrCreatePrimitive("SlideStop", PrimitiveType.Cube, runtime);
            slideStop.transform.SetParent(gunRoot.transform);
            slideStop.transform.localPosition = new Vector3(-0.016f, -0.035f, -0.02f);
            slideStop.transform.localScale = new Vector3(0.004f, 0.005f, 0.016f);

            // ── Magazine release ──
            GameObject magRelease = FindOrCreatePrimitive("MagRelease", PrimitiveType.Cube, runtime);
            magRelease.transform.SetParent(gunRoot.transform);
            magRelease.transform.localPosition = new Vector3(-0.017f, -0.06f, -0.045f);
            magRelease.transform.localScale = new Vector3(0.005f, 0.01f, 0.008f);

            // ── Magazine base plate ──
            GameObject magPlate = GameObject.Find("MagPlate");
            if (magPlate == null)
            {
                magPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                magPlate.name = "MagPlate";
                DestroyCollider(magPlate, runtime);
            }
            magPlate.transform.SetParent(gunRoot.transform);
            magPlate.transform.localPosition = new Vector3(0, -0.10f, -0.04f);
            magPlate.transform.localScale = new Vector3(0.028f, 0.006f, 0.048f);

            // ── Front sight post ──
            GameObject frontSight = GameObject.Find("FrontSight");
            if (frontSight == null)
            {
                frontSight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frontSight.name = "FrontSight";
                DestroyCollider(frontSight, runtime);
            }
            frontSight.transform.SetParent(gunRoot.transform);
            frontSight.transform.localPosition = new Vector3(0, 0.036f, 0.19f);
            frontSight.transform.localScale = new Vector3(0.004f, 0.007f, 0.004f);

            // ── Rear sight notch ──
            GameObject rearSight = GameObject.Find("RearSight");
            if (rearSight == null)
            {
                rearSight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rearSight.name = "RearSight";
                DestroyCollider(rearSight, runtime);
            }
            rearSight.transform.SetParent(gunRoot.transform);
            rearSight.transform.localPosition = new Vector3(0, 0.036f, 0.08f);
            rearSight.transform.localScale = new Vector3(0.016f, 0.006f, 0.004f);

            // ═══════════════════════════════════════════
            //  PBR MATERIAL ASSIGNMENT
            // ═══════════════════════════════════════════

            // Slide body: Matte engineering polymer
            Material bodyMat = new Material(Shader.Find("Standard"));
            bodyMat.SetColor("_Color",          new Color(0.10f, 0.10f, 0.11f));
            bodyMat.SetFloat("_Metallic",       0.05f);
            bodyMat.SetFloat("_Glossiness",     0.18f);
            gunBody.GetComponent<Renderer>().material = bodyMat;
            underRail.GetComponent<Renderer>().material = bodyMat;

            // Grip + Beavertail: Textured rubber with normal map
            Material gripMat = new Material(Shader.Find("Standard"));
            gripMat.SetColor("_Color",          new Color(0.07f, 0.07f, 0.08f));
            gripMat.SetFloat("_Metallic",       0f);
            gripMat.SetFloat("_Glossiness",     0.10f);
            Texture2D gripNormal = CreateGripNormalMap(256, 8, 0.55f);
            gripMat.SetTexture("_BumpMap",      gripNormal);
            gripMat.EnableKeyword("_NORMALMAP");
            gripMat.SetFloat("_BumpScale",      1f);
            gunGrip.GetComponent<Renderer>().material = gripMat;
            beavertail.GetComponent<Renderer>().material = gripMat;

            // Barrel + chamber + metal parts: Blued steel — soft metallic
            Material barrelMat = new Material(Shader.Find("Standard"));
            barrelMat.SetColor("_Color",        new Color(0.06f, 0.06f, 0.07f));
            barrelMat.SetFloat("_Metallic",     1f);
            barrelMat.SetFloat("_Glossiness",   0.38f);
            gunBarrel.GetComponent<Renderer>().material = barrelMat;
            barrelChamber.GetComponent<Renderer>().material = barrelMat;
            triggerGuard.GetComponent<Renderer>().material = barrelMat;
            trigger.GetComponent<Renderer>().material = barrelMat;
            slideStop.GetComponent<Renderer>().material = barrelMat;
            magRelease.GetComponent<Renderer>().material = barrelMat;
            magPlate.GetComponent<Renderer>().material = barrelMat;
            frontSight.GetComponent<Renderer>().material = barrelMat;
            rearSight.GetComponent<Renderer>().material = barrelMat;

            // Serrations: Same polymer as body, slightly darker for depth read
            Material serrationMat = new Material(Shader.Find("Standard"));
            serrationMat.SetColor("_Color",     new Color(0.07f, 0.07f, 0.08f));
            serrationMat.SetFloat("_Metallic",  0.05f);
            serrationMat.SetFloat("_Glossiness", 0.12f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    var ser = GameObject.Find("Serration_" + (side > 0 ? "R_" : "L_") + i);
                    if (ser != null) ser.GetComponent<Renderer>().material = serrationMat;
                }
            }

            // Ejection port: Dark void, slight metallic
            Material portMat = new Material(Shader.Find("Standard"));
            portMat.SetColor("_Color",          new Color(0.02f, 0.02f, 0.03f));
            portMat.SetFloat("_Metallic",       0.3f);
            portMat.SetFloat("_Glossiness",     0.25f);
            ejectionPort.GetComponent<Renderer>().material = portMat;

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
            canvasRT.sizeDelta = new Vector2(900f, 300f);
            canvasObj.transform.localScale = Vector3.one * 0.0028f;

            UIFaceCamera uiFace = canvasObj.GetComponent<UIFaceCamera>();
            if (uiFace == null) uiFace = canvasObj.AddComponent<UIFaceCamera>();
            uiFace.cameraTransform = Camera.main.transform;
            uiFace.distance = uiDistance;
            uiFace.height = uiHeight;
            uiFace.lockYRotation = true;

            // Background — near-invisible dark mask
            GameObject panel = GameObject.Find("Panel");
            if (panel == null)
            {
                panel = new GameObject("Panel");
                panel.transform.SetParent(canvasObj.transform, false);
                RectTransform panelRT = panel.AddComponent<RectTransform>();
                panelRT.anchorMin = new Vector2(0, 0.05f);
                panelRT.anchorMax = new Vector2(1, 0.95f);
                panelRT.offsetMin = new Vector2(20, 0);
                panelRT.offsetMax = new Vector2(-20, 0);

                UnityEngine.UI.Image panelImg = panel.AddComponent<UnityEngine.UI.Image>();
                panelImg.color = new Color(0.02f, 0.02f, 0.03f, 0.35f);
            }

            // ── SCORE — large, prominent ──
            CreateTextElement("ScoreText", canvasObj.transform,
                "SCORE\n<size=140%>0</size>",
                new Vector2(0, 0.52f), new Vector2(1, 0.95f), 22, Color.white);

            // ── Stats row: HIT | SHOT | ACCURACY | TIME ──
            float colW = 0.25f;
            CreateTextElement("HitsText", canvasObj.transform,
                "<color=#555555>HIT</color>\n0",
                new Vector2(0f, 0.05f), new Vector2(colW, 0.48f), 17, Color.white);

            CreateTextElement("ShotsText", canvasObj.transform,
                "<color=#555555>SHOT</color>\n0",
                new Vector2(colW, 0.05f), new Vector2(colW * 2f, 0.48f), 17, Color.white);

            CreateTextElement("AccuracyText", canvasObj.transform,
                "<color=#555555>ACC</color>\n0%",
                new Vector2(colW * 2f, 0.05f), new Vector2(colW * 3f, 0.48f), 17, Color.white);

            CreateTextElement("TimeText", canvasObj.transform,
                "<color=#555555>TIME</color>\n00:00",
                new Vector2(colW * 3f, 0.05f), new Vector2(1f, 0.48f), 17, Color.white);

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

        Texture2D CreateGripNormalMap(int size, int cellCount, float depth)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 2;

            float cellSize = (float)size / cellCount;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Procedural stippling: small bumps in a staggered grid
                    float cx = (x % cellSize) / cellSize - 0.5f;
                    float cy = (y % cellSize) / cellSize - 0.5f;
                    float dist = Mathf.Sqrt(cx * cx + cy * cy);
                    float bump = 1f - Mathf.Clamp01(dist * 1.8f);
                    // Add micro-variation
                    bump += (Mathf.PerlinNoise(x * 0.15f, y * 0.15f) - 0.5f) * 0.4f;
                    bump = Mathf.Clamp01(bump);

                    // Encode as tangent-space normal (Z = 1, XY = bump derivatives)
                    float bx = (bump - 0.5f) * depth;
                    float by = (bump - 0.5f) * depth;
                    Vector3 n = new Vector3(bx, by, 1f).normalized;
                    tex.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f));
                }
            }
            tex.Apply();
            return tex;
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
                                     "CoverBlock_0_0", "CoverBlock_0_1", "CoverBlock_0_2", "CoverBlock_0_3",
                                     "CoverBlock_0_4", "CoverBlock_0_5",
                                     "CoverBlock_1_0", "CoverBlock_1_1", "CoverBlock_1_2", "CoverBlock_1_3",
                                     "CoverBlock_1_4", "CoverBlock_1_5",
                                     "RightHand", "GunVisual", "GunRoot",
                                     "GunBody", "UnderRail", "GunGrip", "Beavertail",
                                     "GunBarrel", "BarrelChamber", "EjectionPort",
                                     "Serration_R_0", "Serration_R_1", "Serration_R_2", "Serration_R_3",
                                     "Serration_L_0", "Serration_L_1", "Serration_L_2", "Serration_L_3",
                                     "TriggerGuard", "Trigger", "SlideStop", "MagRelease",
                                     "FrontSight", "RearSight", "MagPlate",
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
