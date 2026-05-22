using UnityEngine;

namespace VRAimLab
{
    public class AK47Builder : MonoBehaviour
    {
        [Header("Colors")]
        public Color bodyColor = new Color(0.15f, 0.12f, 0.08f);
        public Color metalColor = new Color(0.2f, 0.2f, 0.22f);
        public Color woodColor = new Color(0.4f, 0.2f, 0.08f);
        public Color magColor = new Color(0.1f, 0.1f, 0.1f);

        public GameObject Build(Transform parent)
        {
            GameObject akRoot = new GameObject("AK47");
            akRoot.transform.SetParent(parent);
            akRoot.transform.localPosition = Vector3.zero;
            akRoot.transform.localRotation = Quaternion.identity;

            Material bodyMat = CreateMat(bodyColor, 0.3f, 0.1f);
            Material metalMat = CreateMat(metalColor, 0.8f, 0.5f);
            Material woodMat = CreateMat(woodColor, 0.1f, 0.05f);
            Material magMat = CreateMat(magColor, 0.4f, 0.2f);

            // 1. 枪身主体 (Receiver)
            GameObject receiver = CreateCube(akRoot.transform, "Receiver", new Vector3(0, 0, 0.12f), new Vector3(0.04f, 0.055f, 0.22f), bodyMat);

            // 2. 枪管
            GameObject barrel = CreateCylinder(akRoot.transform, "Barrel", new Vector3(0, 0.01f, 0.32f), new Vector3(0.012f, 0.18f, 0.012f), metalMat);

            // 3. 护木 (Handguard)
            GameObject handguard = CreateCube(akRoot.transform, "Handguard", new Vector3(0, -0.01f, 0.2f), new Vector3(0.045f, 0.04f, 0.14f), woodMat);

            // 4. 弹匣 (Magazine) - 弧形
            GameObject mag = CreateCube(akRoot.transform, "Magazine", new Vector3(0, -0.08f, 0.08f), new Vector3(0.03f, 0.1f, 0.06f), magMat);
            mag.transform.localRotation = Quaternion.Euler(8f, 0, 0);

            // 5. 枪托 (Stock) - 木质
            GameObject stock = CreateCube(akRoot.transform, "Stock", new Vector3(0, 0.005f, -0.18f), new Vector3(0.035f, 0.05f, 0.16f), woodMat);

            // 6.  pistol grip (握把)
            GameObject grip = CreateCube(akRoot.transform, "Grip", new Vector3(0, -0.06f, -0.02f), new Vector3(0.03f, 0.07f, 0.05f), woodMat);
            grip.transform.localRotation = Quaternion.Euler(15f, 0, 0);

            // 7. 准星 (Front Sight)
            GameObject frontSight = CreateCube(akRoot.transform, "FrontSight", new Vector3(0, 0.035f, 0.42f), new Vector3(0.005f, 0.015f, 0.005f), metalMat);

            // 8. 照门 (Rear Sight)
            GameObject rearSight = CreateCube(akRoot.transform, "RearSight", new Vector3(0, 0.04f, 0.05f), new Vector3(0.02f, 0.01f, 0.02f), metalMat);

            // 9. 扳机护圈
            GameObject triggerGuard = CreateCube(akRoot.transform, "TriggerGuard", new Vector3(0, -0.03f, 0.0f), new Vector3(0.025f, 0.015f, 0.04f), metalMat);

            // 10. 枪机拉柄
            GameObject bolt = CreateCube(akRoot.transform, "Bolt", new Vector3(0.025f, 0.02f, 0.05f), new Vector3(0.015f, 0.01f, 0.03f), metalMat);

            // 11. 枪口制退器
            GameObject muzzle = CreateCylinder(akRoot.transform, "Muzzle", new Vector3(0, 0.01f, 0.43f), new Vector3(0.018f, 0.02f, 0.018f), metalMat);

            return akRoot;
        }

        GameObject CreateCube(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().material = mat;
            return go;
        }

        GameObject CreateCylinder(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            go.transform.localScale = scale;
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().material = mat;
            return go;
        }

        Material CreateMat(Color color, float metallic, float gloss)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetColor("_Color", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", gloss);
            return mat;
        }
    }
}
