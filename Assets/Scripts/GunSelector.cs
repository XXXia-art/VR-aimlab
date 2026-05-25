using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRAimLab
{
    public class GunSelector : MonoBehaviour
    {
        public Transform gunParent;
        public GameObject currentGun;
        
        [Header("External Model Prefabs")]
        public GameObject pistolModelPrefab;
        public GameObject ak47ModelPrefab;
        public GameObject m4ModelPrefab;

        [Header("Model Scale")]
        [Tooltip("外部模型的统一缩放倍数（1=原始大小）")]
        public float gunModelScale = 0.5f;
        [Tooltip("手枪额外缩放倍数（在统一缩放基础上再乘）")]
        public float pistolScaleMultiplier = 2f;

        void Start()
        {
#if UNITY_EDITOR
            // 自动加载模型（编辑器模式下如果字段为空）
            if (pistolModelPrefab == null)
                pistolModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nokobot/Modern Guns - Handgun/_Prefabs/Handgun Black/M1911 Handgun_Black.prefab");
            if (ak47ModelPrefab == null)
                ak47ModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3D Low Poly Weapon/3D Assets/AK 47.fbx");
            if (m4ModelPrefab == null)
                m4ModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3D Low Poly Weapon/3D Assets/M 16 .fbx");
#endif

            // 运行时备用：从 Resources 加载
            if (pistolModelPrefab == null)
                pistolModelPrefab = Resources.Load<GameObject>("Models/Pistol");
            if (ak47ModelPrefab == null)
                ak47ModelPrefab = Resources.Load<GameObject>("Models/AK47");
            if (m4ModelPrefab == null)
                m4ModelPrefab = Resources.Load<GameObject>("Models/M4");

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGunChanged += OnGunChanged;
                GameStateManager.Instance.OnGameStart += RefreshGun;
            }
            RefreshGun();
        }

        void OnGunChanged(GunType gun)
        {
            RefreshGun();
        }

        public void RefreshGun()
        {
            if (gunParent == null) return;

            // 清除现有的枪（统一前缀 GunModel_）
            foreach (Transform child in gunParent)
            {
                if (child.name.StartsWith("GunModel_"))
                    Destroy(child.gameObject);
            }

            // 清理 VRGun 引用（避免旧枪口 Light 被销毁后仍被引用）
            VRGun gun = gunParent.GetComponentInParent<VRGun>();
            if (gun != null)
            {
                gun.muzzleTransform = null;
                gun.muzzleLight = null;
            }

            GunType selected = GameStateManager.Instance != null 
                ? GameStateManager.Instance.SelectedGun 
                : GunType.Pistol;

            if (selected == GunType.Pistol)
            {
                BuildPistol();
            }
            else if (selected == GunType.AK47)
            {
                BuildAK47();
            }
            else if (selected == GunType.M4)
            {
                BuildM4();
            }
        }

        void BuildPistol()
        {
            if (pistolModelPrefab != null)
            {
                BuildExternalModel(pistolModelPrefab, "GunModel_Pistol", new Vector3(0, -0.05f, 0.1f), Quaternion.identity, pistolScaleMultiplier);
                return;
            }

            // 手枪模型（程序生成回退）
            GameObject gunRoot = new GameObject("GunModel_Pistol");
            gunRoot.transform.SetParent(gunParent);
            gunRoot.transform.localPosition = new Vector3(0, -0.05f, 0.1f);
            gunRoot.transform.localRotation = Quaternion.identity;

            // Gun body
            GameObject gunBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gunBody.name = "GunBody";
            Destroy(gunBody.GetComponent<Collider>());
            gunBody.transform.SetParent(gunRoot.transform);
            gunBody.transform.localPosition = new Vector3(0, 0, 0.05f);
            gunBody.transform.localScale = new Vector3(0.03f, 0.045f, 0.16f);
            Material gunMat = new Material(Shader.Find("Standard"));
            gunMat.SetColor("_Color", new Color(0.12f, 0.12f, 0.14f));
            gunMat.SetFloat("_Metallic", 0.7f);
            gunMat.SetFloat("_Glossiness", 0.5f);
            gunBody.GetComponent<Renderer>().material = gunMat;

            // Grip
            GameObject gunGrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gunGrip.name = "GunGrip";
            Destroy(gunGrip.GetComponent<Collider>());
            gunGrip.transform.SetParent(gunRoot.transform);
            gunGrip.transform.localPosition = new Vector3(0, -0.06f, -0.04f);
            gunGrip.transform.localRotation = Quaternion.Euler(15f, 0, 0);
            gunGrip.transform.localScale = new Vector3(0.03f, 0.08f, 0.05f);
            gunGrip.GetComponent<Renderer>().material = gunMat;

            // Barrel
            GameObject gunBarrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gunBarrel.name = "GunBarrel";
            Destroy(gunBarrel.GetComponent<Collider>());
            gunBarrel.transform.SetParent(gunRoot.transform);
            gunBarrel.transform.localPosition = new Vector3(0, 0.01f, 0.16f);
            gunBarrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
            gunBarrel.transform.localScale = new Vector3(0.012f, 0.04f, 0.012f);
            gunBarrel.GetComponent<Renderer>().material = gunMat;

            // Update VRGun muzzle
            VRGun gun = gunParent.GetComponentInParent<VRGun>();
            if (gun != null)
            {
                gun.muzzleTransform = gunBarrel.transform;
                gun.fireRate = 0f; // 手枪单发
            }

            currentGun = gunRoot;
        }

        void BuildAK47()
        {
            if (ak47ModelPrefab != null)
            {
                BuildExternalModel(ak47ModelPrefab, "GunModel_AK47", new Vector3(0, -0.05f, 0.05f), Quaternion.Euler(0, 180, 0));
                return;
            }

            // 回退到程序生成
            GameObject akRoot = new GameObject("GunModel_AK47");
            akRoot.transform.SetParent(gunParent);
            akRoot.transform.localPosition = new Vector3(0, -0.05f, 0.05f);
            akRoot.transform.localRotation = Quaternion.identity;

            AK47Builder builder = akRoot.AddComponent<AK47Builder>();
            GameObject built = builder.Build(akRoot.transform);

            Transform muzzle = built.transform.Find("Muzzle");
            if (muzzle == null) muzzle = built.transform.Find("Barrel");

            VRGun gun = gunParent.GetComponentInParent<VRGun>();
            if (gun != null)
            {
                if (muzzle != null) gun.muzzleTransform = muzzle;
                gun.fireRate = 0.12f; // AK47 全自动
            }

            currentGun = built;
        }

        void BuildM4()
        {
            if (m4ModelPrefab != null)
            {
                BuildExternalModel(m4ModelPrefab, "GunModel_M4", new Vector3(0, -0.05f, 0.05f), Quaternion.Euler(0, 180, 0));
                return;
            }

            // 没有 M4 模型时回退到 AK47 程序生成
            Debug.LogWarning("[GunSelector] M4 model prefab not assigned, falling back to AK47 procedural model.");
            BuildAK47();
        }

        void BuildExternalModel(GameObject prefab, string name, Vector3 localPos, Quaternion localRot, float extraScale = 1f)
        {
            GameObject model = Instantiate(prefab, gunParent);
            model.name = name;
            model.transform.localPosition = localPos;
            model.transform.localRotation = localRot;
            model.transform.localScale = Vector3.one * gunModelScale * extraScale;

            // 调试日志
            Bounds bounds = CalculateBounds(model);
            Debug.Log($"[GunSelector] 加载模型 {name}, bounds大小={bounds.size}, localScale={model.transform.localScale}");

#if UNITY_EDITOR
            // 尝试加载同目录下的同名材质并应用
            TryApplyMaterial(model, prefab);
#else
            // Build 后材质可能丢失，应用默认材质防止变白
            ApplyDefaultMaterial(model);
#endif

            // 查找枪口位置（按常见命名）
            Transform muzzle = FindMuzzle(model.transform);
            if (muzzle == null)
            {
                // 如果没有明确的枪口，使用模型前方
                GameObject muzzleObj = new GameObject("Muzzle");
                muzzleObj.transform.SetParent(model.transform, false);
                muzzleObj.transform.localPosition = new Vector3(0, 0, 0.5f);
                muzzle = muzzleObj.transform;
            }

            VRGun gun = gunParent.GetComponentInParent<VRGun>();
            if (gun != null)
            {
                gun.muzzleTransform = muzzle;
                if (name.Contains("AK47"))
                    gun.fireRate = 0.12f;
                else if (name.Contains("M4"))
                    gun.fireRate = 0.08f;
                else if (name.Contains("Pistol"))
                    gun.fireRate = 0f;
            }

            currentGun = model;
        }

#if UNITY_EDITOR
        void TryApplyMaterial(GameObject model, GameObject prefab)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(prefabPath)) return;

            string dir = System.IO.Path.GetDirectoryName(prefabPath);
            string baseName = System.IO.Path.GetFileNameWithoutExtension(prefabPath).Trim();
            string matPath = $"{dir}/{baseName}.mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                // 尝试不加空格的其他变体
                matPath = $"{dir}/{baseName.Replace(" ", "")}.mat";
                mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }
            if (mat == null) return;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r is SpriteRenderer || r is ParticleSystemRenderer) continue;
                r.material = mat;
            }
            Debug.Log($"[GunSelector] 已应用材质: {matPath}");
        }
#endif

        void ApplyDefaultMaterial(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            foreach (var r in renderers)
            {
                if (r is SpriteRenderer || r is ParticleSystemRenderer) continue;

                Material mat = r.sharedMaterial;
                bool needsMaterial = mat == null || mat.shader == null ||
                    string.IsNullOrEmpty(mat.shader.name) ||
                    mat.shader.name == "Hidden/InternalErrorShader";

                if (!needsMaterial)
                    continue; // 材质有效，保留原纹理

                // 材质丢失：先尝试从 Resources 加载同名材质
                Material loadedMat = null;
                if (mat != null && !string.IsNullOrEmpty(mat.name))
                {
                    loadedMat = Resources.Load<Material>($"Models/{mat.name}");
                    if (loadedMat == null)
                        loadedMat = Resources.Load<Material>($"Models/Materials/{mat.name}");
                }

                if (loadedMat != null)
                {
                    r.material = loadedMat;
                    Debug.Log($"[GunSelector] 从 Resources 加载材质: {mat.name}");
                }
                else
                {
                    Material defaultMat = new Material(Shader.Find("Standard"));
                    defaultMat.SetColor("_Color", new Color(0.12f, 0.12f, 0.14f));
                    defaultMat.SetFloat("_Metallic", 0.7f);
                    defaultMat.SetFloat("_Glossiness", 0.5f);
                    r.material = defaultMat;
                }
            }
        }

        Transform FindMuzzle(Transform parent)
        {
            // 常见枪口命名
            string[] names = { "Muzzle", "muzzle", "Barrel_Location", "Barrel", "barrel", "BarrelEnd", "Tip", "FirePoint" };
            foreach (var n in names)
            {
                Transform t = parent.Find(n);
                if (t != null) return t;
            }
            // 深度搜索
            foreach (Transform child in parent.GetComponentsInChildren<Transform>())
            {
                foreach (var n in names)
                {
                    if (child.name.Contains(n))
                        return child;
                }
            }
            return null;
        }

        Bounds CalculateBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one * 0.1f);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGunChanged -= OnGunChanged;
                GameStateManager.Instance.OnGameStart -= RefreshGun;
            }
        }
    }
}
