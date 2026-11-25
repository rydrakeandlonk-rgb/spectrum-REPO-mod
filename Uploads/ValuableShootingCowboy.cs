using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ValuableShootingCowboy : MonoBehaviour
{
	public Transform arm;

	public AnimationCurve animCurve;

	[Header("Bullet")]
	public Transform gunTip;

	public HurtCollider hurtCollider;

	public LineRenderer lineRenderer;

	public AnimationCurve lineRendererWidthCurve;

	public List<ParticleSystem> shootEffects = new List<ParticleSystem>();

	public ParticleSystem breakEffect;

	public Sound soundShoot;

	public float recoilForce = 5f;

	[Header("Screenshake")]
	public float shakeStrength = 3f;

	public float shakeDuration = 0.1f;

	public float shakeDistanceMin = 3f;

	public float shakeDistanceMax = 10f;

	public Transform armShakeTransform;

	public Transform cowboyNeck;

	public Transform scarf1;

	public Transform scarf2;

	public Sound armRotationLoop;

	public Sound mechanicWhirringLoop;

	public Sound reload;

	public Sound shoot;

	public Sound sparks;

	private float bulletRange = 50f;

	private float bulletSphereRadius = 0.1f;

	private float lineRendererLerp;

	private bool lineRendererActive;

	private float hurtColliderTimer;

	private PhotonView photonView;

	private float armRotationTime = 5f;

	private float armRotationTimer;

	private float reloadTime = 2f;

	private float reloadTimer;

	private float shootTime = 0.5f;

	private float shootTimer;

	private float armReturnTime = 3f;

	private float armReturnTimer = 3f;

	private float afterShootTime = 3f;

	private float afterShootTimer;

	private float coolDownTime = 3f;

	private float coolDownTimer;

	private const float armMaxAngle = 100f;

	private float currentArmAngle = 100f;

	private bool grabbed;

	private bool shot;

	private bool reloadSound;

	private bool shakeSound;

	private int maxShotsBeforeBreak = 15;

	private PhysGrabObject physGrabObject;

	private ValuableObject valuableObject;

	private float animPhase;

	private float animWeight;

	private float animBlendSpeed = 4f;

	private float headRockSpeed = 4f;

	private const float headRockAngle = 8f;

	private float scarfWiggleSpeed = 5f;

	private const float scarfWiggleAngle = 15f;

	private void Start()
	{
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		valuableObject = ((Component)this).GetComponent<ValuableObject>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		SetArmAngle(currentArmAngle);
	}

	private void Update()
	{
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		armRotationLoop.PlayLoop(shakeSound, 1f, 1f);
		shakeSound = false;
		LineRendererLogic();
		HurtColliderTimer();
		bool active = physGrabObject.grabbed && !shot && coolDownTimer <= 0f && reloadTimer <= 0f;
		UpdateCowboyAnimations(active);
		if (coolDownTimer > 0f)
		{
			coolDownTimer -= Time.deltaTime;
			if (coolDownTimer <= 0f)
			{
				coolDownTimer = 0f;
			}
			return;
		}
		if (shot && shootTimer <= 0f)
		{
			afterShootTimer -= Time.deltaTime;
			if (afterShootTimer <= 0f)
			{
				shot = false;
				coolDownTimer = coolDownTime;
			}
		}
		if (shootTimer > 0f)
		{
			AnimateShooting();
			shootTimer -= Time.deltaTime;
		}
		else if (grabbed && armRotationTimer >= armRotationTime && !shot)
		{
			if (reloadTimer >= reloadTime)
			{
				Shoot();
				return;
			}
			if (!reloadSound)
			{
				reload.Play(((Component)this).transform.position);
				reloadSound = true;
			}
			reloadTimer += Time.deltaTime;
		}
		else if (physGrabObject.grabbed && !shot)
		{
			if (!grabbed)
			{
				grabbed = true;
				armRotationTimer = (1f - currentArmAngle / 100f) * armRotationTime;
				reloadTimer = 0f;
				reloadSound = false;
			}
			armRotationTimer += Time.deltaTime;
			float num = armRotationTimer / armRotationTime;
			SetArmAngle(Mathf.Lerp(100f, 0f, num));
			ShakeRotatingArm();
			shakeSound = true;
		}
		else if (!(currentArmAngle >= 100f))
		{
			if (grabbed)
			{
				armRotationTimer = (1f - currentArmAngle / 100f) * armRotationTime;
				armReturnTimer = currentArmAngle / 100f * armReturnTime;
				reloadTimer = 0f;
				reloadSound = false;
				grabbed = false;
			}
			shakeSound = true;
			armReturnTimer += Time.deltaTime;
			float num2 = armReturnTimer / armReturnTime;
			SetArmAngle(Mathf.Lerp(0f, 100f, num2));
			ShakeRotatingArm();
		}
	}

	private void SetArmAngle(float angle)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		currentArmAngle = angle;
		arm.localRotation = Quaternion.Euler(angle, 0f, 0f);
	}

	private void Shoot()
	{
		shot = true;
		afterShootTimer = afterShootTime;
		reloadTimer = 0f;
		reloadSound = false;
		shootTimer = shootTime;
		FireBullet();
	}

	private void FireBullet()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		LoseValue();
		breakEffect.Play(true);
		sparks.Play(armShakeTransform.position);
		physGrabObject.rb.AddForce(-gunTip.forward * recoilForce, (ForceMode)1);
		Vector3 val = gunTip.position + gunTip.forward * bulletRange;
		bool flag = false;
		RaycastHit[] array = Physics.SphereCastAll(gunTip.position, bulletSphereRadius, gunTip.forward, bulletRange, LayerMask.GetMask(new string[1] { "Player" }) + LayerMask.GetMask(new string[1] { "PhysGrabObject" }));
		for (int i = 0; i < array.Length; i++)
		{
			RaycastHit val2 = array[i];
			if (((Component)val2.transform).gameObject.layer != LayerMask.NameToLayer("Player"))
			{
				continue;
			}
			PlayerController componentInParent = ((Component)val2.transform).GetComponentInParent<PlayerController>();
			PlayerAvatar playerAvatar = ((!Object.op_Implicit((Object)(object)componentInParent)) ? ((Component)val2.transform).GetComponentInParent<PlayerAvatar>() : componentInParent.playerAvatarScript);
			bool flag2 = true;
			if (val2.point != Vector3.zero)
			{
				Vector3 val3 = val2.point - gunTip.position;
				RaycastHit[] array2 = Physics.RaycastAll(gunTip.position, val3, val3.magnitude, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()), (QueryTriggerInteraction)1);
				for (int j = 0; j < array2.Length; j++)
				{
					RaycastHit val4 = array2[j];
					if (((Component)val4.transform).gameObject.layer != LayerMask.NameToLayer("Player"))
					{
						flag2 = false;
						break;
					}
				}
			}
			if (flag2)
			{
				if (val2.point != Vector3.zero)
				{
					val = val2.point;
					flag = true;
				}
				else if (Object.op_Implicit((Object)(object)playerAvatar))
				{
					val = playerAvatar.PlayerVisionTarget.VisionTransform.position;
					flag = true;
				}
				break;
			}
		}
		if (!flag)
		{
			int num = LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()) + LayerMask.GetMask(new string[1] { "Enemy" });
			RaycastHit val5 = default;
			if (Physics.Raycast(gunTip.position, gunTip.forward, ref val5, bulletRange, num, (QueryTriggerInteraction)1))
			{
				val = val5.point;
			}
		}
		val -= gunTip.forward * 0.1f;
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("BulletRPC", (RpcTarget)0, new object[1] { val });
		}
		else
		{
			BulletRPC(val);
		}
	}

	[PunRPC]
	private void BulletRPC(Vector3 _hitPosition, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return;
		}
		Vector3 val = _hitPosition - gunTip.position;
		shoot.Play(((Component)gunTip).transform.position);
		if (Object.op_Implicit((Object)(object)lineRenderer))
		{
			((Component)lineRenderer).gameObject.SetActive(true);
			lineRenderer.positionCount = 4;
			lineRenderer.SetPosition(0, gunTip.position);
			lineRenderer.SetPosition(1, gunTip.position + val.normalized * 0.5f);
			lineRenderer.SetPosition(2, _hitPosition - val.normalized * 0.5f);
			lineRenderer.SetPosition(3, _hitPosition);
			lineRendererActive = true;
			lineRendererLerp = 0f;
		}
		if (Object.op_Implicit((Object)(object)hurtCollider))
		{
			((Component)hurtCollider).transform.position = _hitPosition;
			((Component)hurtCollider).transform.rotation = Quaternion.LookRotation(gunTip.forward);
			hurtCollider.hasCustomRaycastPosition = true;
			hurtCollider.customRaycastPosition = ((Component)hurtCollider).transform.InverseTransformPoint(gunTip.position);
			((Component)hurtCollider).gameObject.SetActive(true);
			hurtColliderTimer = 0.25f;
		}
		foreach (ParticleSystem shootEffect in shootEffects)
		{
			shootEffect.Play();
		}
		soundShoot.Play(gunTip.position);
		SemiFunc.CameraShakeImpactDistance(gunTip.position, shakeStrength, shakeDuration, shakeDistanceMin, shakeDistanceMax);
		SemiFunc.CameraShakeDistance(gunTip.position, shakeStrength, shakeDuration, shakeDistanceMin, shakeDistanceMax);
	}

	private void LineRendererLogic()
	{
		if (lineRendererActive && Object.op_Implicit((Object)(object)lineRenderer))
		{
			lineRenderer.widthMultiplier = lineRendererWidthCurve.Evaluate(lineRendererLerp);
			lineRendererLerp += Time.deltaTime * 5f;
			if (lineRendererLerp >= 1f)
			{
				((Component)lineRenderer).gameObject.SetActive(false);
				lineRendererActive = false;
			}
		}
	}

	private void HurtColliderTimer()
	{
		if (hurtColliderTimer > 0f)
		{
			hurtColliderTimer -= Time.deltaTime;
			if (hurtColliderTimer <= 0f && Object.op_Implicit((Object)(object)hurtCollider))
			{
				((Component)hurtCollider).gameObject.SetActive(false);
			}
		}
	}

	private void AnimateShooting()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		float num = (shootTime - shootTimer) / shootTime;
		float num2 = animCurve.Evaluate(num);
		arm.localRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(-30f, 0f, 0f), num2);
	}

	private void UpdateCowboyAnimations(bool active)
	{
		animWeight = Mathf.MoveTowards(animWeight, active ? 1f : 0f, Time.deltaTime * animBlendSpeed);
		if (active)
		{
			animPhase += Time.deltaTime;
		}
		else if (animWeight <= 0f)
		{
			animPhase = 0f;
		}
		mechanicWhirringLoop.PlayLoop(active, 0.7f, 1f);
		RockHead();
		WiggleScarves();
	}

	private void RockHead()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Sin(animPhase * headRockSpeed) * 8f * animWeight;
		cowboyNeck.localRotation = Quaternion.Euler(0f, 0f, num);
	}

	private void WiggleScarves()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		float num = (Mathf.Sin(animPhase * scarfWiggleSpeed) + 1f) * 0.5f * 15f * animWeight;
		if ((Object)(object)scarf1 != (Object)null)
		{
			scarf1.localRotation = Quaternion.Euler(0f, num, 0f);
		}
		if ((Object)(object)scarf2 != (Object)null)
		{
			scarf2.localRotation = Quaternion.Euler(0f, 0f - num, 0f);
		}
	}

	private void ShakeRotatingArm()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		armShakeTransform.localRotation = Quaternion.Euler(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
	}

	private void LoseValue()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		float valueLost = Mathf.Ceil(valuableObject.dollarValueOriginal / (float)maxShotsBeforeBreak);
		physGrabObject.impactDetector.Break(valueLost, ((Component)this).transform.position, physGrabObject.impactDetector.breakLevelLight, _forceBreak: true);
	}
}
