using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemCartCannon : MonoBehaviour
{
	private ItemCartCannonMain cartCannonMain;

	private PhotonView photonView;

	public AnimationCurve animationCurve;

	public GameObject bulletPrefab;

	private PhysGrabObject physGrabObject;

	private ItemBattery itemBattery;

	public Sound soundHit;

	public AnimationCurve animationCurveShoot;

	public AnimationCurve animationCurveShootRecoil;

	public Transform shootAnimationTransform;

	public Transform shootParticlesTransform;

	private Vector3 movingPieceStartPosition;

	private Vector3 movingPieceStartPositionRecoil;

	public Sound buildup1;

	public Sound buildup2;

	public Sound shootSound;

	public Sound shootSoundGlobal;

	private bool animationEvent1;

	private bool animationEvent2;

	public Transform animationEventTransform;

	public Transform shootNozzleRecoilTransform;

	private List<ParticleSystem> particles = new List<ParticleSystem>();

	private List<ParticleSystem> shootParticles = new List<ParticleSystem>();

	private bool doRecoil;

	private float recoilTimer;

	private float recoilTimerMax = 0.4f;

	private ParticleScriptExplosion particleScriptExplosion;

	private int statePrev;

	private int stateCurrent;

	private bool stateStart;

	private void Start()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		movingPieceStartPosition = shootAnimationTransform.localPosition;
		movingPieceStartPositionRecoil = shootNozzleRecoilTransform.localPosition;
		cartCannonMain = ((Component)this).GetComponent<ItemCartCannonMain>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		bulletPrefab.SetActive(false);
		itemBattery = ((Component)this).GetComponent<ItemBattery>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		particleScriptExplosion = ((Component)this).GetComponent<ParticleScriptExplosion>();
		particles = new List<ParticleSystem>(((Component)animationEventTransform).GetComponentsInChildren<ParticleSystem>());
		shootParticles = new List<ParticleSystem>(((Component)shootParticlesTransform).GetComponentsInChildren<ParticleSystem>());
	}

	private void StateInactive()
	{
		if (stateStart)
		{
			stateStart = false;
		}
	}

	private void StateActive()
	{
		if (stateStart)
		{
			stateStart = false;
		}
	}

	private void StateBuildup()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			buildup1.Play(((Component)this).transform.position);
			doRecoil = true;
			animationEvent2 = false;
			animationEvent1 = false;
			stateStart = false;
		}
		if (cartCannonMain.stateTimer > 0.08f && !animationEvent1)
		{
			AnimationEvent1();
		}
		if (cartCannonMain.stateTimer > 0.5f && !animationEvent2)
		{
			AnimationEvent2();
		}
		float num = animationCurveShoot.Evaluate(cartCannonMain.stateTimer / cartCannonMain.stateTimerMax);
		shootAnimationTransform.localPosition = new Vector3(movingPieceStartPosition.x, movingPieceStartPosition.y, movingPieceStartPosition.z - movingPieceStartPosition.z * num);
		if (doRecoil)
		{
			float num2 = animationCurveShootRecoil.Evaluate(recoilTimer / recoilTimerMax);
			shootNozzleRecoilTransform.localPosition = new Vector3(movingPieceStartPositionRecoil.x, movingPieceStartPositionRecoil.y, movingPieceStartPositionRecoil.z - 4f + 4f * num2);
			if (recoilTimer >= recoilTimerMax)
			{
				doRecoil = false;
				recoilTimer = 0f;
				shootNozzleRecoilTransform.localPosition = movingPieceStartPositionRecoil;
			}
			else
			{
				recoilTimer += Time.deltaTime;
			}
		}
	}

	private void StateShooting()
	{
		if (stateStart)
		{
			ShootLogic();
			stateStart = false;
		}
	}

	private void StateGoingBack()
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
		}
		float num = 1f - animationCurveShoot.Evaluate(cartCannonMain.stateTimer / cartCannonMain.stateTimerMax);
		shootAnimationTransform.localPosition = new Vector3(movingPieceStartPosition.x, movingPieceStartPosition.y, movingPieceStartPosition.z - movingPieceStartPosition.z * num);
	}

	private void ParticlePlay()
	{
		foreach (ParticleSystem particle in particles)
		{
			particle.Play();
		}
	}

	private void StateMachine()
	{
		switch (stateCurrent)
		{
		case 0:
			StateInactive();
			break;
		case 1:
			StateActive();
			break;
		case 2:
			StateBuildup();
			break;
		case 3:
			StateShooting();
			break;
		case 4:
			StateGoingBack();
			break;
		}
	}

	private void Update()
	{
		statePrev = stateCurrent;
		stateCurrent = (int)cartCannonMain.stateCurrent;
		if (stateCurrent != statePrev)
		{
			stateStart = true;
		}
		StateMachine();
	}

	private void ShootLogic()
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (Object.op_Implicit((Object)(object)physGrabObject) && Object.op_Implicit((Object)(object)physGrabObject.lastPlayerGrabbing))
			{
				SemiFunc.PlayerShopKeeperShotGunHost(physGrabObject.lastPlayerGrabbing);
			}
			Transform muzzle = cartCannonMain.muzzle;
			itemBattery.RemoveFullBar(1);
			physGrabObject.rb.AddForceAtPosition(-muzzle.forward * 20f, muzzle.position, (ForceMode)1);
			Vector3 endPosition = muzzle.position + muzzle.forward * 25f;
			bool hit = false;
			bool flag = false;
			Vector3 val = muzzle.forward;
			float num = 0.1f;
			float num2 = 25f;
			if (num > 0f)
			{
				float num3 = Random.Range(0f, num / 2f);
				float num4 = Random.Range(0f, 360f);
				Vector3 val2 = Vector3.Cross(val, Random.onUnitSphere);
				Vector3 normalized = val2.normalized;
				Quaternion val3 = Quaternion.AngleAxis(num3, normalized);
				val = Quaternion.AngleAxis(num4, val) * val3 * val;
				val = val.normalized;
			}
			RaycastHit val4 = default;
			bool flag2 = Physics.Raycast(muzzle.position, val, ref val4, num2, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()) + LayerMask.GetMask(new string[1] { "Enemy" }));
			if (flag2)
			{
				endPosition = val4.point;
				hit = true;
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				endPosition = muzzle.position + val * num2;
				hit = true;
			}
			ShootBullet(endPosition, hit, flag2);
		}
	}

	private void ShootBullet(Vector3 _endPosition, bool _hit, bool _surfaceHit)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("ShootBulletRPC", (RpcTarget)0, new object[3] { _endPosition, _hit, _surfaceHit });
			}
			else
			{
				ShootBulletRPC(_endPosition, _hit, _surfaceHit);
			}
		}
	}

	[PunRPC]
	public void ShootBulletRPC(Vector3 _endPosition, bool _hit, bool _surfaceHit, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			ShootParticlesPlay();
			Transform muzzle = cartCannonMain.muzzle;
			ItemGunBullet component = Object.Instantiate<GameObject>(bulletPrefab, muzzle.position, muzzle.rotation).GetComponent<ItemGunBullet>();
			component.hitPosition = _endPosition;
			component.bulletHit = _hit;
			component.surfaceHit = _surfaceHit;
			component.surfaceImpactStrength = 0f;
			component.shootLineWidthCurve = animationCurve;
			soundHit.Play(_endPosition);
			shootSound.Play(muzzle.position);
			GameDirector.instance.CameraShake.ShakeDistance(6f, 3f, 12f, muzzle.position, 0.15f);
			GameDirector.instance.CameraImpact.ShakeDistance(10f, 3f, 12f, muzzle.position, 0.08f);
			component.ActivateAll();
			particleScriptExplosion.PlayExplosionSoundMedium(_endPosition);
			if (_hit)
			{
				GameDirector.instance.CameraShake.ShakeDistance(6f, 4f, 8f, _endPosition, 0.5f);
				GameDirector.instance.CameraImpact.ShakeDistance(10f, 4f, 8f, _endPosition, 0.2f);
			}
			HurtCollider componentInChildren = ((Component)component).GetComponentInChildren<HurtCollider>(true);
			if (Object.op_Implicit((Object)(object)componentInChildren) && Object.op_Implicit((Object)(object)physGrabObject) && Object.op_Implicit((Object)(object)physGrabObject.lastPlayerGrabbing))
			{
				componentInChildren.playerCausingHurt = physGrabObject.lastPlayerGrabbing;
				componentInChildren.playerHitFullRuckus = true;
			}
		}
	}

	private void AnimationEvent1()
	{
		ParticlePlay();
		animationEvent1 = true;
	}

	private void AnimationEvent2()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		ParticlePlay();
		buildup2.Play(((Component)this).transform.position);
		animationEvent2 = true;
	}

	private void ShootParticlesPlay()
	{
		foreach (ParticleSystem shootParticle in shootParticles)
		{
			if (shootParticle.isPlaying)
			{
				shootParticle.Stop();
			}
			shootParticle.Play();
		}
	}
}
