using UnityEngine;
using UnityEngine.XR;
using System.Collections;

namespace VRAimLab
{
    public class VRGun : MonoBehaviour
    {
        [Header("Ray Settings")]
        public Transform muzzleTransform;
        public float maxRayDistance = 100f;
        public LayerMask targetLayer;

        [Header("Visual")]
        public LineRenderer laserLine;
        public Color aimColor = Color.green;
        public Color targetLockedColor = Color.red;
        public float laserWidth = 0.04f;
        public Material laserMaterial;

        [Header("Muzzle Flash")]
        public ParticleSystem muzzleFlashPrefab;
        public float muzzleFlashDuration = 0.05f;
        public Light muzzleLight;
        public float muzzleLightIntensity = 8f;
        public float muzzleLightDuration = 0.03f;

        [Header("Muzzle Smoke")]
        public ParticleSystem muzzleSmokePrefab;
        public float smokeSpawnInterval = 0.1f;
        private float lastSmokeTime = 0f;

        [Header("Shell Ejection")]
        public Transform shellEjectTransform;
        public Rigidbody shellPrefab;
        public float shellEjectForce = 2f;
        public float shellEjectTorque = 5f;
        public float shellDestroyDelay = 3f;

        [Header("Recoil")]
        public float recoilForce = 0.1f;
        public float recoilDuration = 0.1f;
        public float recoilRotation = 0.03f;
        public float recoilRecoveryDuration = 0.2f;
        private bool isRecoiling = false;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip shootSound;
        public float shootSoundVolume = 0.8f;

        [Header("Input")]
        public XRNode controllerNode = XRNode.RightHand;
        public bool useMouseDebug = true;

        [Header("Fire Mode")]
        [Tooltip("0 = 单发, >0 = 全自动射速（秒/发）")]
        public float fireRate = 0f;

        private bool wasPressed = false;
        private float nextFireTime = 0f;
        private InputDevice device;
        private Camera mainCam;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;

        void Start()
        {
            if (muzzleTransform == null)
                muzzleTransform = transform;

            mainCam = Camera.main;
            SetupLaserLine();
            SetupAudioSource();
            device = InputDevices.GetDeviceAtXRNode(controllerNode);
            
            // 自动检测平台：PICO/VR 设备上自动关闭鼠标调试
            if (!Application.isEditor && Application.platform == RuntimePlatform.Android)
            {
                useMouseDebug = false;
                Debug.Log("[VRGun] 检测到 Android VR 设备，已自动切换到手柄输入模式");
            }
            
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            
            if (muzzleLight == null)
            {
                GameObject lightObj = new GameObject("MuzzleLight");
                lightObj.transform.SetParent(muzzleTransform, false);
                muzzleLight = lightObj.AddComponent<Light>();
                muzzleLight.type = LightType.Point;
                muzzleLight.range = 3f;
                muzzleLight.intensity = 0f;
                muzzleLight.color = new Color(1f, 0.7f, 0.3f);
            }
            if (muzzleLight != null)
                muzzleLight.enabled = false;
        }

        void SetupAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D音效

            // 自动加载 Resources/Audio/ 目录下的音频文件
            if (shootSound == null)
            {
                AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");
                if (clips != null && clips.Length > 0)
                {
                    shootSound = clips[0];
                    Debug.Log($"[VRGun] 自动加载音效: {shootSound.name}");
                }
            }
        }

        void SetupLaserLine()
        {
            if (laserLine == null)
            {
                GameObject lineObj = new GameObject("LaserLine");
                lineObj.transform.SetParent(transform);
                laserLine = lineObj.AddComponent<LineRenderer>();
            }

            laserLine.positionCount = 2;
            laserLine.useWorldSpace = true;
            laserLine.sortingOrder = 100;
            laserLine.numCapVertices = 4;

            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, laserWidth);
            curve.AddKey(1f, laserWidth * 0.3f);
            laserLine.widthCurve = curve;
            laserLine.widthMultiplier = 1f;

            if (laserMaterial == null)
            {
                laserMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            laserMaterial.color = aimColor;
            laserLine.material = laserMaterial;
        }

        void Update()
        {
            device = InputDevices.GetDeviceAtXRNode(controllerNode);
            UpdateLaser();
            HandleShooting();
        }

        Vector3 GetShootOrigin()
        {
            if (useMouseDebug && mainCam != null)
                return mainCam.transform.position;
            return muzzleTransform != null ? muzzleTransform.position : transform.position;
        }

        Vector3 GetShootDirection()
        {
            if (useMouseDebug && mainCam != null)
                return mainCam.transform.forward;

            // 统一使用手柄 forward 作为射击方向，避免模型枪口轴向不一致
            return transform.forward;
        }

        void UpdateLaser()
        {
            Vector3 origin = GetShootOrigin();
            Vector3 direction = GetShootDirection();

            RaycastHit hit;
            bool hitTarget = Physics.Raycast(origin, direction, out hit, maxRayDistance, targetLayer, QueryTriggerInteraction.Collide);

            Vector3 endPoint = hitTarget ? hit.point : origin + direction * maxRayDistance;

            laserLine.SetPosition(0, origin);
            laserLine.SetPosition(1, endPoint);

            Color currentColor = hitTarget ? targetLockedColor : aimColor;
            laserMaterial.color = currentColor;
        }

        void HandleShooting()
        {
            bool isPressed = false;

            if (useMouseDebug)
            {
                isPressed = Input.GetMouseButton(0);
            }
            else if (device.isValid)
            {
                device.TryGetFeatureValue(CommonUsages.triggerButton, out isPressed);
            }

            if (fireRate > 0f)
            {
                // 全自动模式：按住连发
                if (isPressed && Time.time >= nextFireTime)
                {
                    Shoot();
                    nextFireTime = Time.time + fireRate;
                }
            }
            else
            {
                // 单发模式：按下瞬间发射一次
                if (isPressed && !wasPressed)
                {
                    Shoot();
                }
            }
            wasPressed = isPressed;
        }

        void Shoot()
        {
            Vector3 origin = GetShootOrigin();
            Vector3 direction = GetShootDirection();

            PlayShootSound();
            PlayMuzzleFlash();
            PlayMuzzleSmoke();
            EjectShell();
            ApplyRecoil();

            RaycastHit hit;
            bool hitTarget = Physics.Raycast(origin, direction, out hit, maxRayDistance, targetLayer, QueryTriggerInteraction.Collide);

            if (hitTarget)
            {
                SpawnHitEffect(hit.point, hit.normal);
                
                Target target = hit.collider.GetComponent<Target>();
                if (target != null)
                {
                    target.Hit();
                }
                else
                {
                    target = hit.collider.GetComponentInParent<Target>();
                    if (target != null)
                    {
                        target.Hit();
                    }
                    else
                    {
                        MovingTargetEntity movingTarget = hit.collider.GetComponent<MovingTargetEntity>();
                        if (movingTarget != null)
                            movingTarget.Hit();
                        else
                        {
                            movingTarget = hit.collider.GetComponentInParent<MovingTargetEntity>();
                            if (movingTarget != null)
                                movingTarget.Hit();
                        }
                    }
                }

                // 追踪模式目标
                ReactionTargetEntity reactionTarget = hit.collider.GetComponent<ReactionTargetEntity>();
                if (reactionTarget != null)
                    reactionTarget.Hit();
                else
                {
                    reactionTarget = hit.collider.GetComponentInParent<ReactionTargetEntity>();
                    if (reactionTarget != null)
                        reactionTarget.Hit();
                }

                TrackingTargetEntity trackingTarget = hit.collider.GetComponent<TrackingTargetEntity>();
                if (trackingTarget != null)
                    trackingTarget.Hit();
                else
                {
                    trackingTarget = hit.collider.GetComponentInParent<TrackingTargetEntity>();
                    if (trackingTarget != null)
                        trackingTarget.Hit();
                }

                ScoreManager.Instance?.AddShot(true);
            }
            else
            {
                ScoreManager.Instance?.AddShot(false);
            }
        }

        void ApplyRecoil()
        {
            if (isRecoiling) return;
            StartCoroutine(RecoilAnimation());
        }

        IEnumerator RecoilAnimation()
        {
            isRecoiling = true;

            // 在枪模型上执行后坐力，避免和 XRControllerTracker 冲突
            GunSelector selector = GetComponent<GunSelector>();
            Transform gunModel = (selector != null && selector.currentGun != null)
                ? selector.currentGun.transform
                : transform;

            // 局部空间后坐力：-Z 是枪管后方（不受世界旋转影响）
            Vector3 recoilOffset = new Vector3(0, 0, -recoilForce);
            Vector3 targetRecoilRot = new Vector3(-recoilRotation, 0, 0);

            Vector3 startPos = gunModel.localPosition;
            Quaternion startRot = gunModel.localRotation;

            float elapsed = 0f;
            while (elapsed < recoilDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / recoilDuration);

                gunModel.localPosition = Vector3.Lerp(startPos, startPos + recoilOffset, t);
                gunModel.localRotation = startRot * Quaternion.Euler(Vector3.Lerp(Vector3.zero, targetRecoilRot, t));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < recoilRecoveryDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / recoilRecoveryDuration);

                gunModel.localPosition = Vector3.Lerp(startPos + recoilOffset, startPos, t);
                gunModel.localRotation = startRot * Quaternion.Euler(Vector3.Lerp(targetRecoilRot, Vector3.zero, t));
                yield return null;
            }

            gunModel.localPosition = startPos;
            gunModel.localRotation = startRot;
            isRecoiling = false;
        }

        void PlayMuzzleFlash()
        {
            if (muzzleFlashPrefab != null)
            {
                GameObject flashObj = Instantiate(muzzleFlashPrefab.gameObject, muzzleTransform.position, muzzleTransform.rotation);
                flashObj.transform.SetParent(muzzleTransform);
                ParticleSystem ps = flashObj.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play();
                }
                StartCoroutine(DestroyMuzzleFlashAfterDelay(flashObj, muzzleFlashDuration));
            }
            else
            {
                CreateDefaultMuzzleFlash();
            }

            if (muzzleLight != null)
            {
                StartCoroutine(MuzzleLightFlash());
            }
        }

        void CreateDefaultMuzzleFlash()
        {
            GameObject flashObj = new GameObject("MuzzleFlash");
            flashObj.transform.position = muzzleTransform.position;
            flashObj.transform.rotation = muzzleTransform.rotation;

            ParticleSystem ps = flashObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.1f;
            main.startLifetime = Random.Range(0.05f, 0.08f);
            main.startSize = Random.Range(0.15f, 0.25f);
            main.startSpeed = Random.Range(2f, 4f);
            main.loop = false;
            main.startColor = new Color(1f, 0.6f, 0.1f, 1f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, Random.Range(15, 25)) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.03f;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);

            var colorOverLifetime = ps.colorOverLifetime;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.9f, 0.7f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0f, 1f), 0.3f),
                    new GradientColorKey(new Color(1f, 0.2f, 0f, 0f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 1.5f)
            ));

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            Shader additiveShader = Shader.Find("Particles/Additive");
            if (additiveShader == null)
                additiveShader = Shader.Find("Sprites/Default");
            renderer.material = new Material(additiveShader);
            renderer.material.color = new Color(1f, 0.8f, 0.5f);

            ps.Play();
            Destroy(flashObj, 0.2f);
        }

        IEnumerator MuzzleLightFlash()
        {
            muzzleLight.enabled = true;
            muzzleLight.intensity = muzzleLightIntensity;
            muzzleLight.color = new Color(1f, 0.7f, 0.3f);

            float elapsed = 0f;
            while (elapsed < muzzleLightDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / muzzleLightDuration;
                muzzleLight.intensity = muzzleLightIntensity * (1 - t);
                muzzleLight.color = Color.Lerp(new Color(1f, 0.7f, 0.3f), Color.black, t);
                yield return null;
            }

            muzzleLight.enabled = false;
        }

        void PlayMuzzleSmoke()
        {
            if (muzzleSmokePrefab == null)
            {
                CreateDefaultSmoke();
                return;
            }

            if (Time.time - lastSmokeTime >= smokeSpawnInterval)
            {
                GameObject smokeObj = Instantiate(muzzleSmokePrefab.gameObject, muzzleTransform.position, muzzleTransform.rotation);
                ParticleSystem ps = smokeObj.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play();
                }
                Destroy(smokeObj, 2f);
                lastSmokeTime = Time.time;
            }
        }

        void CreateDefaultSmoke()
        {
            if (Time.time - lastSmokeTime >= smokeSpawnInterval)
            {
                GameObject smokeObj = new GameObject("MuzzleSmoke");
                smokeObj.transform.position = muzzleTransform.position;
                smokeObj.transform.rotation = muzzleTransform.rotation;

                ParticleSystem ps = smokeObj.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = ps.main;
                main.duration = 1.5f;
                main.startLifetime = Random.Range(0.8f, 1.2f);
                main.startSize = Random.Range(0.05f, 0.08f);
                main.startSpeed = Random.Range(0.3f, 0.5f);
                main.loop = false;
                main.startColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                main.gravityModifier = -0.1f;

                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, Random.Range(5, 10)) });

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 30f;
                shape.radius = 0.02f;

                var velocityOverLifetime = ps.velocityOverLifetime;
                velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
                velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);

                var colorOverLifetime = ps.colorOverLifetime;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.3f, 0.3f, 0.3f, 0.8f), 0f),
                        new GradientColorKey(new Color(0.2f, 0.2f, 0.2f, 0f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0.8f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                colorOverLifetime.color = gradient;

                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                Shader smokeShader = Shader.Find("Sprites/Default");
                if (smokeShader == null)
                    smokeShader = Shader.Find("Standard");
                renderer.material = new Material(smokeShader);

                ps.Play();
                Destroy(smokeObj, 2f);
                lastSmokeTime = Time.time;
            }
        }

        void EjectShell()
        {
            if (shellEjectTransform == null)
            {
                shellEjectTransform = new GameObject("ShellEjectPoint").transform;
                shellEjectTransform.SetParent(muzzleTransform.parent);
                shellEjectTransform.localPosition = new Vector3(0.05f, 0.02f, 0f);
            }

            if (shellPrefab != null)
            {
                Rigidbody shell = Instantiate(shellPrefab, shellEjectTransform.position, shellEjectTransform.rotation);

                Vector3 ejectDirection = shellEjectTransform.right + Vector3.up * 0.5f;
                ejectDirection += Random.insideUnitSphere * 0.2f;

                shell.velocity = ejectDirection * shellEjectForce;
                shell.angularVelocity = Random.insideUnitSphere * shellEjectTorque;

                Destroy(shell.gameObject, shellDestroyDelay);
            }
            else
            {
                CreateDefaultShell();
            }
        }

        void CreateDefaultShell()
        {
            GameObject shellObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shellObj.name = "Shell";
            shellObj.transform.position = shellEjectTransform.position;
            shellObj.transform.rotation = shellEjectTransform.rotation;
            shellObj.transform.localScale = new Vector3(0.015f, 0.03f, 0.015f);

            Rigidbody rb = shellObj.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
            rb.drag = 0.5f;
            rb.angularDrag = 0.5f;

            MeshRenderer renderer = shellObj.GetComponent<MeshRenderer>();
            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null)
                standardShader = Shader.Find("Sprites/Default");
            renderer.material = new Material(standardShader);
            renderer.material.color = new Color(0.7f, 0.7f, 0.7f);

            Vector3 ejectDirection = shellEjectTransform.right + Vector3.up * 0.5f;
            ejectDirection += Random.insideUnitSphere * 0.2f;

            rb.velocity = ejectDirection * shellEjectForce;
            rb.angularVelocity = Random.insideUnitSphere * shellEjectTorque;

            Destroy(shellObj, shellDestroyDelay);
        }

        void SpawnHitEffect(Vector3 position, Vector3 normal)
        {
            if (HitEffectPool.Instance != null)
            {
                HitEffectPool.Instance.Spawn(position);
            }
            else
            {
                CreateHitEffect(position, normal);
            }
        }

        void CreateHitEffect(Vector3 position, Vector3 normal)
        {
            GameObject effectObj = new GameObject("HitEffect");
            effectObj.transform.position = position;
            effectObj.transform.rotation = Quaternion.LookRotation(normal);

            ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.2f;
            main.startLifetime = Random.Range(0.15f, 0.25f);
            main.startSize = Random.Range(0.05f, 0.1f);
            main.startSpeed = Random.Range(1.5f, 2.5f);
            main.loop = false;
            main.startColor = new Color(1f, 0.7f, 0f, 1f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, Random.Range(10, 15)) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.02f;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.5f, -1f);

            var colorOverLifetime = ps.colorOverLifetime;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.7f, 0f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.3f, 0f, 0f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            Shader hitShader = Shader.Find("Sprites/Default");
            if (hitShader == null)
                hitShader = Shader.Find("Standard");
            renderer.material = new Material(hitShader);

            ps.Play();
            Destroy(effectObj, 0.5f);
        }

        void PlayShootSound()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            if (shootSound != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(shootSound, shootSoundVolume);
            }
            else
            {
                PlayDefaultShootSound();
            }
        }

        void PlayDefaultShootSound()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            audioSource.pitch = Random.Range(0.9f, 1.1f);

            int sampleCount = 44100 / 4;
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Pow(1 - t, 1.5f) * Mathf.Exp(-t * 6);

                float highTone = Mathf.Sin(t * 800 * Mathf.PI * 2) * envelope * 0.3f;
                float midTone = Mathf.Sin(t * 400 * Mathf.PI * 2) * envelope * 0.25f;

                float noise = (Mathf.PerlinNoise(t * 30, 0) * 2 - 1) * envelope * 0.4f;
                float snap = Mathf.Pow(1 - t, 3) * (Random.value * 2 - 1) * 0.15f;

                samples[i] = highTone + midTone + noise + snap;
            }

            AudioClip clip = AudioClip.Create("ShootSound", sampleCount, 1, 44100, false);
            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip, shootSoundVolume);
            Destroy(clip, 1f);
        }

        IEnumerator DestroyMuzzleFlashAfterDelay(GameObject flashObj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(flashObj);
        }

        void OnDestroy()
        {
            if (laserMaterial != null)
                Destroy(laserMaterial);
        }
    }
}