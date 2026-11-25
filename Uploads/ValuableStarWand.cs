using Photon.Pun;
using UnityEngine;

public class ValuableStarWand : MonoBehaviour
{
	public ParticleSystem particle;

	private PropLight propLight;

	private float initialLightIntensity = 1f;

	private float targetLightIntensity;

	private Rigidbody rb;

	private PhysGrabObject physGrabObject;

	private ValuableObject valuableObject;

	public Sound sparkles;

	public Sound buildupSound;

	private int maxShotsBeforeBreak = 15;

	private bool soundPlayed;

	public PrefabRef bulletPrefab;

	public Transform bulletSpawnPoint;

	private Vector3 bulletDirection;

	private bool grabbedPrev;

	private float shootTimer;

	private float shootTimerMax = 1f;

	private bool canShoot;

	private bool readyToShoot;

	private bool shootImpulse;

	private float swingThresholdMin = 8f;

	private float swingThresholdMax = 15f;

	private float rotateOnPeakTimer;

	private bool rotateOnPeakImpulse;

	private bool rotateBackActive;

	private bool buildupSoundImpulse = true;

	private bool debug;

	private void Awake()
	{
		propLight = ((Component)this).GetComponentInChildren<PropLight>();
		rb = ((Component)this).GetComponent<Rigidbody>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		valuableObject = ((Component)this).GetComponent<ValuableObject>();
		rotateOnPeakImpulse = true;
		shootImpulse = true;
	}

	private void Update()
	{
		DebugRuben();
		HandleShooting();
		HandleBuildupSound();
		RotateOnGrab();
		RotateOnPeak();
		HandleShootCooldown();
		SparklesVFX();
	}

	private void DebugRuben()
	{
		if (SemiFunc.KeyDownRuben((KeyCode)117))
		{
			debug = !debug;
		}
		if (SemiFunc.KeyDownRuben((KeyCode)327))
		{
			CastSpell(launch: true);
			shootTimer = 0f;
			readyToShoot = false;
			canShoot = false;
		}
		if (debug)
		{
			SemiLogger.LogRuben($"Can Shoot:{canShoot}");
			SemiLogger.LogRuben($"Ready To Shoot:{readyToShoot}");
		}
	}

	private void HandleShootCooldown()
	{
		if (!(shootTimer >= shootTimerMax))
		{
			shootTimer += Time.deltaTime;
			if (shootTimer >= shootTimerMax)
			{
				buildupSoundImpulse = true;
				canShoot = true;
			}
		}
	}

	private bool IsAngularVelocityAboveThreshold()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Vector3 angularVelocity = rb.angularVelocity;
		return angularVelocity.magnitude > swingThresholdMax;
	}

	private void RotateOnGrab()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if (physGrabObject.grabbed)
		{
			if (!grabbedPrev)
			{
				shootTimer = 0f;
				canShoot = false;
				grabbedPrev = true;
				if (SemiFunc.IsMasterClientOrSingleplayer())
				{
					physGrabObject.TurnXYZ(Quaternion.Euler(-75f, 0f, 0f), Quaternion.identity, Quaternion.identity);
				}
			}
		}
		else
		{
			grabbedPrev = false;
		}
	}

	private void RotateOnPeak()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		if (IsAngularVelocityAboveThreshold() && canShoot)
		{
			if (rotateOnPeakImpulse)
			{
				rotateOnPeakImpulse = false;
				if (SemiFunc.IsMasterClientOrSingleplayer())
				{
					physGrabObject.TurnXYZ(Quaternion.Euler(-15f, 0f, 0f), Quaternion.identity, Quaternion.identity);
					rotateBackActive = true;
					rotateOnPeakTimer = 0f;
				}
			}
		}
		else
		{
			rotateOnPeakImpulse = true;
		}
		if (!rotateBackActive)
		{
			return;
		}
		rotateOnPeakTimer += Time.deltaTime;
		if (rotateOnPeakTimer >= 0.5f)
		{
			shootTimer = 0.5f;
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				physGrabObject.TurnXYZ(Quaternion.Euler(-55f, -15f, 0f), Quaternion.identity, Quaternion.identity);
			}
			rotateBackActive = false;
			rotateOnPeakTimer = 0f;
		}
	}

	private void HandleShooting()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (!canShoot)
		{
			return;
		}
		if (IsAngularVelocityAboveThreshold())
		{
			if (shootImpulse)
			{
				shootImpulse = false;
				readyToShoot = true;
			}
		}
		else
		{
			shootImpulse = true;
		}
		if (readyToShoot)
		{
			Vector3 angularVelocity = rb.angularVelocity;
			if (angularVelocity.magnitude < swingThresholdMin)
			{
				CastSpell(launch: true);
				shootTimer = 0f;
				readyToShoot = false;
				canShoot = false;
			}
		}
	}

	private void CastSpell(bool launch)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		if (physGrabObject.impactDetector.inCart || physGrabObject.impactDetector.isIndestructible)
		{
			return;
		}
		SemiFunc.CameraShakeDistance(((Component)this).transform.position, 4f, 0.1f, 3f, 8f);
		SemiFunc.CameraShakeImpactDistance(((Component)this).transform.position, 4f, 0.1f, 3f, 8f);
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		LoseValue();
		physGrabObject.rb.AddForceAtPosition(-bulletSpawnPoint.forward * 5f, bulletSpawnPoint.position, (ForceMode)1);
		Vector3 zero = Vector3.zero;
		zero = ((physGrabObject.playerGrabbing.Count <= 0) ? bulletSpawnPoint.forward : physGrabObject.playerGrabbing[0].playerAvatar.localCamera.GetOverrideTransform().forward);
		bulletDirection = Vector3.Lerp(bulletSpawnPoint.forward, zero, 0.6f);
		GameObject val = ((!SemiFunc.IsMultiplayer()) ? Object.Instantiate<GameObject>(bulletPrefab.Prefab, bulletSpawnPoint.position, Quaternion.LookRotation(bulletDirection)) : PhotonNetwork.Instantiate(bulletPrefab.ResourcePath, bulletSpawnPoint.position, Quaternion.LookRotation(bulletDirection), (byte)0, (object[])null));
		SlowProjectile slowProjectile = ((val != null) ? val.GetComponent<SlowProjectile>() : null);
		if (!Object.op_Implicit((Object)(object)slowProjectile))
		{
			return;
		}
		slowProjectile.SetSpawner(((Component)this).gameObject);
		if (Object.op_Implicit((Object)(object)slowProjectile.hurtCollider))
		{
			HurtCollider component = slowProjectile.hurtCollider.GetComponent<HurtCollider>();
			if (Object.op_Implicit((Object)(object)component))
			{
				component.ignoreObjects.Add(physGrabObject);
			}
		}
		if (launch)
		{
			slowProjectile.Launch(bulletDirection);
		}
	}

	private void LoseValue()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		float valueLost = Mathf.Ceil(valuableObject.dollarValueOriginal / (float)maxShotsBeforeBreak);
		physGrabObject.impactDetector.Break(valueLost, ((Component)this).transform.position, physGrabObject.impactDetector.breakLevelLight, _forceBreak: true);
	}

	private void SparklesVFX()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		Vector3 linearVelocity = rb.linearVelocity;
		if (linearVelocity.magnitude > 0.45f)
		{
			if (physGrabObject.impactDetector.inCart && physGrabObject.playerGrabbing.Count <= 0)
			{
				return;
			}
			if (particle.isStopped)
			{
				particle.Play(true);
				if (!soundPlayed)
				{
					sparkles.Play(((Component)this).transform.position);
					soundPlayed = true;
				}
			}
			targetLightIntensity = initialLightIntensity;
		}
		else
		{
			if (particle.isPlaying)
			{
				particle.Stop(true);
				soundPlayed = false;
			}
			targetLightIntensity = 0f;
		}
		if (propLight.lightComponent.intensity != targetLightIntensity)
		{
			propLight.lightComponent.intensity = Mathf.Lerp(propLight.lightComponent.intensity, targetLightIntensity, Time.deltaTime * 5f);
		}
	}

	private void HandleBuildupSound()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if (IsAngularVelocityAboveThreshold() && readyToShoot && buildupSoundImpulse && !physGrabObject.impactDetector.inCart && !physGrabObject.impactDetector.isIndestructible)
		{
			buildupSoundImpulse = false;
			((Component)buildupSound.Play(bulletSpawnPoint.position)).transform.SetParent(((Component)this).transform);
		}
	}
}
