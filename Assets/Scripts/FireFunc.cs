using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VRAimLab;

public class FireFunc : MonoBehaviour
{
	public ParticleSystem muzzleFlashPrefab;
	public float muzzleFlashDuration = 0.05f;
	public float smokeSpawnInterval = 0.1f;
	private float lastSmokeTime = 0f;
	public Transform shellEjectTransform;
	public float shellEjectForce = 2f;
	public float shellEjectTorque = 5f;
	public float shellDestroyDelay = 3f;
	public float maxRayDistance = 100f;
	public LayerMask targetLayer;
	public float recoilForce = 0.1f;
	public float recoilDuration = 0.1f;
	public float recoilRotation = 0.03f;
	public float recoilRecoveryDuration = 0.2f;
	private bool isRecoiling = false;
	private Vector3 originalLocalPosition;
	private Quaternion originalLocalRotation;
	private void Start()
	{
		originalLocalPosition=transform.localPosition;
		originalLocalRotation=transform.localRotation;
	}
	public void Fire()
	{
		Vector3 origin;
		Vector3 direction;

		origin=transform.position;
		direction=transform.forward;

		ScoreManager.Instance?.AddShot();

		//PlayShootSound();
		PlayMuzzleFlash();
		CreateDefaultSmoke();
		EjectShell();
		ApplyRecoil();

		RaycastHit hit;
		if(Physics.Raycast(origin,direction,out hit,maxRayDistance,targetLayer))
		{
			SpawnHitEffect(hit.point,hit.normal);

			Target target = hit.collider.GetComponent<Target>();
			if(target!=null)
			{
				target.Hit();
			} else
			{
				target=hit.collider.GetComponentInParent<Target>();
				if(target!=null)
					target.Hit();
			}
		}
	}
	void PlayMuzzleFlash()
	{
		if(muzzleFlashPrefab!=null)
		{
			GameObject flashObj = Instantiate(muzzleFlashPrefab.gameObject,transform.position,transform.rotation);
			flashObj.transform.SetParent(transform);
			ParticleSystem ps = flashObj.GetComponent<ParticleSystem>();
			if(ps!=null)
			{
				ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
				ps.Play();
			}
			StartCoroutine(DestroyMuzzleFlashAfterDelay(flashObj,muzzleFlashDuration));
		} else
		{
			CreateDefaultMuzzleFlash();
		}
	}
	void CreateDefaultMuzzleFlash()
	{
		GameObject flashObj = new GameObject("MuzzleFlash");
		flashObj.transform.position=transform.position;
		flashObj.transform.rotation=transform.rotation;

		ParticleSystem ps = flashObj.AddComponent<ParticleSystem>();
		ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);

		var main = ps.main;
		main.duration=0.1f;
		main.startLifetime=Random.Range(0.05f,0.08f);
		main.startSize=Random.Range(0.15f,0.25f);
		main.startSpeed=Random.Range(2f,4f);
		main.loop=false;
		main.startColor=new Color(1f,0.6f,0.1f,1f);

		var emission = ps.emission;
		emission.rateOverTime=0;
		emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f,Random.Range(15,25)) });

		var shape = ps.shape;
		shape.shapeType=ParticleSystemShapeType.Cone;
		shape.angle=15f;
		shape.radius=0.03f;

		var velocityOverLifetime = ps.velocityOverLifetime;
		velocityOverLifetime.z=new ParticleSystem.MinMaxCurve(0.5f,1.5f);

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
		colorOverLifetime.color=gradient;

		var sizeOverLifetime = ps.sizeOverLifetime;
		sizeOverLifetime.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(
			new Keyframe(0f,1f),
			new Keyframe(1f,1.5f)
		));

		var renderer = ps.GetComponent<ParticleSystemRenderer>();
		Shader additiveShader = Shader.Find("Particles/Additive");
		if(additiveShader==null)
			additiveShader=Shader.Find("Sprites/Default");
		renderer.material=new Material(additiveShader);
		renderer.material.color=new Color(1f,0.8f,0.5f);

		ps.Play();
		Destroy(flashObj,0.2f);
	}
	void CreateDefaultSmoke()
	{
		if(Time.time-lastSmokeTime>=smokeSpawnInterval)
		{
			GameObject smokeObj = new GameObject("MuzzleSmoke");
			smokeObj.transform.position=transform.position;
			smokeObj.transform.rotation=transform.rotation;

			ParticleSystem ps = smokeObj.AddComponent<ParticleSystem>();
			ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);

			var main = ps.main;
			main.duration=1.5f;
			main.startLifetime=Random.Range(0.8f,1.2f);
			main.startSize=Random.Range(0.05f,0.08f);
			main.startSpeed=Random.Range(0.3f,0.5f);
			main.loop=false;
			main.startColor=new Color(0.3f,0.3f,0.3f,0.8f);
			main.gravityModifier=-0.1f;

			var emission = ps.emission;
			emission.rateOverTime=0;
			emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f,Random.Range(5,10)) });

			var shape = ps.shape;
			shape.shapeType=ParticleSystemShapeType.Cone;
			shape.angle=30f;
			shape.radius=0.02f;

			var velocityOverLifetime = ps.velocityOverLifetime;
			velocityOverLifetime.x=new ParticleSystem.MinMaxCurve(-0.1f,0.1f);
			velocityOverLifetime.y=new ParticleSystem.MinMaxCurve(0.1f,0.3f);

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
			colorOverLifetime.color=gradient;

			var renderer = ps.GetComponent<ParticleSystemRenderer>();
			Shader smokeShader = Shader.Find("Sprites/Default");
			if(smokeShader==null)
				smokeShader=Shader.Find("Standard");
			renderer.material=new Material(smokeShader);

			ps.Play();
			Destroy(smokeObj,2f);
			lastSmokeTime=Time.time;
		}
	}
	void EjectShell()
	{
		if(shellEjectTransform==null)
		{
			shellEjectTransform=new GameObject("ShellEjectPoint").transform;
			shellEjectTransform.SetParent(transform.parent);
			shellEjectTransform.localPosition=new Vector3(0.05f,0.02f,0f);
		}
		CreateDefaultShell();
	}

	void CreateDefaultShell()
	{
		GameObject shellObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		shellObj.name="Shell";
		shellObj.transform.position=shellEjectTransform.position;
		shellObj.transform.rotation=shellEjectTransform.rotation;
		shellObj.transform.localScale=new Vector3(0.015f,0.03f,0.015f);

		Rigidbody rb = shellObj.AddComponent<Rigidbody>();
		rb.mass=0.01f;
		rb.drag=0.5f;
		rb.angularDrag=0.5f;

		MeshRenderer renderer = shellObj.GetComponent<MeshRenderer>();
		Shader standardShader = Shader.Find("Standard");
		if(standardShader==null)
			standardShader=Shader.Find("Sprites/Default");
		renderer.material=new Material(standardShader);
		renderer.material.color=new Color(0.7f,0.7f,0.7f);

		Vector3 ejectDirection = shellEjectTransform.right+Vector3.up*0.5f;
		ejectDirection+=Random.insideUnitSphere*0.2f;

		rb.velocity=ejectDirection*shellEjectForce;
		rb.angularVelocity=Random.insideUnitSphere*shellEjectTorque;

		Destroy(shellObj,shellDestroyDelay);
	}

	void SpawnHitEffect(Vector3 position,Vector3 normal)
	{
		if(HitEffectPool.Instance!=null)
		{
			HitEffectPool.Instance.Spawn(position);
		} else
		{
			CreateHitEffect(position,normal);
		}
	}

	void CreateHitEffect(Vector3 position,Vector3 normal)
	{
		GameObject effectObj = new GameObject("HitEffect");
		effectObj.transform.position=position;
		effectObj.transform.rotation=Quaternion.LookRotation(normal);

		ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
		ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);

		var main = ps.main;
		main.duration=0.2f;
		main.startLifetime=Random.Range(0.15f,0.25f);
		main.startSize=Random.Range(0.05f,0.1f);
		main.startSpeed=Random.Range(1.5f,2.5f);
		main.loop=false;
		main.startColor=new Color(1f,0.7f,0f,1f);

		var emission = ps.emission;
		emission.rateOverTime=0;
		emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f,Random.Range(10,15)) });

		var shape = ps.shape;
		shape.shapeType=ParticleSystemShapeType.Hemisphere;
		shape.radius=0.02f;

		var velocityOverLifetime = ps.velocityOverLifetime;
		velocityOverLifetime.z=new ParticleSystem.MinMaxCurve(-0.5f,-1f);

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
		colorOverLifetime.color=gradient;

		var renderer = ps.GetComponent<ParticleSystemRenderer>();
		Shader hitShader = Shader.Find("Sprites/Default");
		if(hitShader==null)
			hitShader=Shader.Find("Standard");
		renderer.material=new Material(hitShader);

		ps.Play();
		Destroy(effectObj,0.5f);
	}
	void ApplyRecoil()
	{
		if(isRecoiling)
			return;
		StartCoroutine(RecoilAnimation());
	}

	IEnumerator RecoilAnimation()
	{
		isRecoiling=true;

		Vector3 targetRecoilPos = new Vector3(0,0,-recoilForce);
		Vector3 targetRecoilRot = new Vector3(-recoilRotation,0,0);

		float elapsed = 0f;
		while(elapsed<recoilDuration)
		{
			elapsed+=Time.deltaTime;
			float t = Mathf.SmoothStep(0,1,elapsed/recoilDuration);

			transform.localPosition=Vector3.Lerp(originalLocalPosition,originalLocalPosition-transform.forward*recoilForce,t);
			transform.localRotation=originalLocalRotation*Quaternion.Euler(Vector3.Lerp(Vector3.zero,targetRecoilRot,t));
			yield return null;
		}

		elapsed=0f;
		while(elapsed<recoilRecoveryDuration)
		{
			elapsed+=Time.deltaTime;
			float t = Mathf.SmoothStep(0,1,elapsed/recoilRecoveryDuration);

			transform.localPosition=Vector3.Lerp(originalLocalPosition-transform.forward*recoilForce,originalLocalPosition,t);
			transform.localRotation=originalLocalRotation*Quaternion.Euler(Vector3.Lerp(targetRecoilRot,Vector3.zero,t));
			yield return null;
		}
		originalLocalRotation = transform.localRotation;
		originalLocalPosition = transform.localPosition;
		isRecoiling=false;
	}
	IEnumerator DestroyMuzzleFlashAfterDelay(GameObject flashObj,float delay)
	{
		yield return new WaitForSeconds(delay);
		Destroy(flashObj);
	}
}
