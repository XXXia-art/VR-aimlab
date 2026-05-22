using UnityEngine;

namespace VRAimLab
{
    public class GunSelector : MonoBehaviour
    {
        public Transform gunParent;
        public GameObject currentGun;

        void Start()
        {
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

            // 清除现有的枪
            foreach (Transform child in gunParent)
            {
                if (child.name.Contains("Gun") || child.name.Contains("AK47"))
                    Destroy(child.gameObject);
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
        }

        void BuildPistol()
        {
            // 手枪模型（沿用 GameBootstrap 的逻辑）
            GameObject gunRoot = new GameObject("GunRoot");
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
            if (gun != null) gun.muzzleTransform = gunBarrel.transform;

            currentGun = gunRoot;
        }

        void BuildAK47()
        {
            GameObject akRoot = new GameObject("AK47");
            akRoot.transform.SetParent(gunParent);
            akRoot.transform.localPosition = new Vector3(0, -0.05f, 0.05f);
            akRoot.transform.localRotation = Quaternion.identity;

            AK47Builder builder = akRoot.AddComponent<AK47Builder>();
            GameObject built = builder.Build(akRoot.transform);

            // AK47 枪口位置
            Transform muzzle = built.transform.Find("Muzzle");
            if (muzzle == null) muzzle = built.transform.Find("Barrel");

            VRGun gun = gunParent.GetComponentInParent<VRGun>();
            if (gun != null && muzzle != null) gun.muzzleTransform = muzzle;

            currentGun = built;
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
