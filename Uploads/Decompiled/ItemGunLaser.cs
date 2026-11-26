using UnityEngine;

public class ItemGunLaser : MonoBehaviour
{
	public SemiLaser semiLaser;

	public Transform muzzleTransform;

	private float shootImpulseTimer;

	private bool shooting;

	public ParticleSystem muzzleFlashParticle;

	private ItemGun itemGun;

	public Sound soundInitialCrack;

	public Sound soundBuildup;

	public Sound soundReload;

	public Sound soundReload2;

	private float buildupImpulseTimer;

	public ParticleSystem particleOverHeat;

	public AnimationCurve buildupImpulseCurve;

	public AnimationCurve heatLatchCurve;

	public AnimationCurve backLatchCurve;

	public Transform transformGunEnergyPlop;

	public Transform transformOverHeatLatch;

	public Transform transformBackPlingPlong;

	private Material materialGunEnergyPlop;

	public ParticleSystem particleBuildUp;

	public ParticleSystem particleInitialCrack;

	private bool soundReload2Played;

	private PhysGrabObject physGrabObject;

	private void Start()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		itemGun = ((Component)this).GetComponent<ItemGun>();
		materialGunEnergyPlop = ((Component)transformGunEnergyPlop).GetComponent<Renderer>().material;
		materialGunEnergyPlop.SetColor("_EmissionColor", Color.black);
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
	}

	private void FixedUpdate()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && itemGun.stateCurrent == ItemGun.State.Shooting)
		{
			Vector3 val = -muzzleTransform.forward * 10f;
			physGrabObject.rb.AddForce(val, (ForceMode)0);
			Vector3 val2 = -muzzleTransform.right * 0.6f;
			physGrabObject.rb.AddTorque(val2, (ForceMode)0);
		}
	}

	private void Update()
	{
		if (shootImpulseTimer > 0f)
		{
			if (!shooting)
			{
				muzzleFlashParticle.Play(true);
				shooting = true;
			}
			shootImpulseTimer -= Time.deltaTime;
		}
		else if (shooting)
		{
			muzzleFlashParticle.Stop(true);
			shooting = false;
		}
		if (buildupImpulseTimer > 0f)
		{
			buildupImpulseTimer -= Time.deltaTime;
		}
		bool playing = buildupImpulseTimer > 0f;
		float pitchMultiplier = Mathf.Lerp(3f, 1f, 1f - itemGun.stateTimer / itemGun.stateTimeMax);
		soundBuildup.PlayLoop(playing, 1f, 1f, pitchMultiplier);
	}

	public void OnStateShootStart()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		soundInitialCrack.Play(((Component)this).transform.position);
		particleInitialCrack.Play(true);
		GameDirector.instance.CameraImpact.ShakeDistance(7f, 6f, 12f, ((Component)this).transform.position, 0.1f);
		if (Object.op_Implicit((Object)(object)physGrabObject) && Object.op_Implicit((Object)(object)physGrabObject.lastPlayerGrabbing))
		{
			if (Object.op_Implicit((Object)(object)semiLaser) && Object.op_Implicit((Object)(object)semiLaser.hurtCollider))
			{
				semiLaser.hurtCollider.playerCausingHurtOverride = physGrabObject.lastPlayerGrabbing;
			}
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				SemiFunc.PlayerShopKeeperShotGunHost(physGrabObject.lastPlayerGrabbing);
			}
		}
	}

	public void OnStateShootUpdate()
	{
		LaserShooting();
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)physGrabObject) && Object.op_Implicit((Object)(object)physGrabObject.lastPlayerGrabbing))
		{
			SemiFunc.PlayerShopKeeperLaserTickHost(physGrabObject.lastPlayerGrabbing);
		}
	}

	public void OnStateReloadStart()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		soundReload.Play(((Component)this).transform.position);
		particleBuildUp.Stop(true);
		particleOverHeat.Play(true);
		soundReload.Play(((Component)this).transform.position);
		soundReload2Played = false;
		transformGunEnergyPlop.localPosition = new Vector3(0f, 0f, -0.6f);
	}

	public void OnStateReloadUpdate()
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		float num = -0.6f;
		float num2 = itemGun.stateTimer / (itemGun.stateTimeMax / 2f);
		float num3 = buildupImpulseCurve.Evaluate(num2);
		if (num3 > 1f)
		{
			num3 = 1f;
		}
		transformGunEnergyPlop.localPosition = new Vector3(0f, 0f, Mathf.Lerp(num, 0f, num3));
		Color val = Color.Lerp(Color.yellow, Color.black, num3);
		materialGunEnergyPlop.SetColor("_EmissionColor", val);
		num2 = itemGun.stateTimer / itemGun.stateTimeMax;
		num3 = backLatchCurve.Evaluate(num2);
		float num4 = 130f * num3;
		transformBackPlingPlong.localRotation = Quaternion.Euler(0f - num4, 0f, 0f);
		num2 = itemGun.stateTimer / itemGun.stateTimeMax;
		num3 = heatLatchCurve.Evaluate(num2);
		float num5 = 84f * num3;
		transformOverHeatLatch.localRotation = Quaternion.Euler(0f - num5, 0f, 0f);
		if (!soundReload2Played && itemGun.stateTimer > itemGun.stateTimeMax * 0.8f)
		{
			soundReload2.Play(((Component)this).transform.position);
			soundReload2Played = true;
		}
	}

	public void OnStateIdleStart()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		particleBuildUp.Stop(true);
		transformGunEnergyPlop.localPosition = new Vector3(0f, 0f, 0f);
		materialGunEnergyPlop.SetColor("_EmissionColor", Color.black);
		transformOverHeatLatch.localRotation = Quaternion.Euler(0f, 0f, 0f);
		transformBackPlingPlong.localRotation = Quaternion.Euler(0f, 0f, 0f);
	}

	public void OnStateBuildStart()
	{
		particleBuildUp.Play(true);
	}

	public void OnStateBuildUpdate()
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		buildupImpulseTimer = 0.1f;
		float num = -0.6f;
		float num2 = itemGun.stateTimer / itemGun.stateTimeMax;
		float num3 = buildupImpulseCurve.Evaluate(num2);
		if (num3 > 1f)
		{
			num3 = 1f;
		}
		transformGunEnergyPlop.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0f, num, num3));
		Color val = Color.Lerp(Color.black, Color.yellow, num3);
		materialGunEnergyPlop.SetColor("_EmissionColor", val);
	}

	private void LaserShooting()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		float gunRange = itemGun.gunRange;
		Vector3 endPosition = muzzleTransform.position + muzzleTransform.forward * gunRange;
		bool isHitting = false;
		RaycastHit val = default;
		if (Physics.Raycast(muzzleTransform.position, muzzleTransform.forward, ref val, gunRange, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct())))
		{
			endPosition = val.point;
			isHitting = true;
		}
		semiLaser.LaserActive(muzzleTransform.position, endPosition, isHitting, 3f, 6f, 2f);
		shootImpulseTimer = 0.1f;
	}
}
