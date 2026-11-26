using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemCartLaser : MonoBehaviour
{
	public SemiLaser semiLaser;

	public Transform muzzleTransform;

	private ItemCartCannonMain cartCannonMain;

	private PhotonView photonView;

	private PhysGrabObject physGrabObject;

	private ItemBattery itemBattery;

	public Sound soundHit;

	public AnimationCurve animationCurveBuildup;

	public AnimationCurve animationCurveHatchType1;

	public AnimationCurve animationCurveHatchType2;

	public AnimationCurve animationCurveWarningLight;

	private Vector3 movingPieceStartPosition;

	public Sound soundBuildupStart;

	public Sound soundBuildupLoop;

	public Sound soundGoingBack;

	private PhysGrabObjectImpactDetector physGrabObjectImpactDetector;

	private bool animationEvent1;

	private bool animationEvent2;

	public Transform animationEventTransform;

	public Transform shootParticlesTransform;

	private List<ParticleSystem> particles = new List<ParticleSystem>();

	private List<ParticleSystem> shootParticles = new List<ParticleSystem>();

	private ParticleScriptExplosion particleScriptExplosion;

	private int statePrev;

	internal int stateCurrent;

	private bool stateStart;

	public HurtCollider laserHurtCollider;

	public Transform hatch1Right;

	public Transform hatch1Left;

	public Transform hatch2Right;

	public Transform hatch2Left;

	public MeshRenderer warningLight1MeshRenderer;

	public MeshRenderer warningLight2MeshRenderer;

	public Light warningLight1Light;

	public Light warningLight2Light;

	public ParticleSystem hatchLeft;

	public ParticleSystem hatchRight;

	private void Start()
	{
		cartCannonMain = ((Component)this).GetComponent<ItemCartCannonMain>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		itemBattery = ((Component)this).GetComponent<ItemBattery>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		particleScriptExplosion = ((Component)this).GetComponent<ParticleScriptExplosion>();
		physGrabObjectImpactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		shootParticles = new List<ParticleSystem>(((Component)shootParticlesTransform).GetComponentsInChildren<ParticleSystem>());
	}

	private void StateInactive()
	{
		if (stateStart)
		{
			ResetHatches();
			stateStart = false;
		}
	}

	private void StateActive()
	{
		if (stateStart)
		{
			ResetHatches();
			stateStart = false;
		}
	}

	private void StateBuildup()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			ResetHatches();
			soundBuildupStart.Play(((Component)this).transform.position);
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
	}

	private void StateShooting()
	{
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			ResetHatches();
			ShootParticles(_play: true);
			GameDirector.instance.CameraShake.ShakeDistance(3f, 2f, 10f, muzzleTransform.position, 0.15f);
			GameDirector.instance.CameraImpact.ShakeDistance(7f, 2f, 10f, muzzleTransform.position, 0.08f);
			stateStart = false;
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
		Vector3 endPosition = muzzleTransform.position + muzzleTransform.forward * 15f;
		bool isHitting = false;
		RaycastHit val = default;
		if (Physics.Raycast(muzzleTransform.position, muzzleTransform.forward, ref val, 15f, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct())))
		{
			endPosition = val.point;
			isHitting = true;
		}
		semiLaser.LaserActive(muzzleTransform.position, endPosition, isHitting, 3f, 6f, 2f);
		GameDirector.instance.CameraShake.ShakeDistance(2f, 2f, 8f, muzzleTransform.position, 0.1f);
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)physGrabObject) && Object.op_Implicit((Object)(object)physGrabObject.lastPlayerGrabbing))
		{
			SemiFunc.PlayerShopKeeperLaserTickHost(physGrabObject.lastPlayerGrabbing);
		}
	}

	private void StateGoingBack()
	{
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			ResetHatches();
			itemBattery.RemoveFullBar(1);
			soundGoingBack.Play(((Component)this).transform.position);
			stateStart = false;
			HatchWarningLightsTurnOn();
		}
		float num = animationCurveHatchType1.Evaluate(cartCannonMain.stateTimer / cartCannonMain.stateTimerMax);
		float num2 = animationCurveHatchType2.Evaluate(cartCannonMain.stateTimer / cartCannonMain.stateTimerMax);
		float num3 = animationCurveWarningLight.Evaluate(cartCannonMain.stateTimer / cartCannonMain.stateTimerMax);
		hatch1Left.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 25f, num));
		hatch1Right.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -25f, num));
		hatch2Left.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -120f, num2));
		hatch2Right.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 120f, num2));
		warningLight1Light.intensity = Mathf.Lerp(0f, 4f, num3);
		warningLight2Light.intensity = Mathf.Lerp(0f, 4f, num3);
		((Renderer)warningLight1MeshRenderer).material.SetColor("_EmissionColor", Color.Lerp(new Color(0f, 0f, 0f), new Color(1f, 0f, 0f), num3));
		((Renderer)warningLight2MeshRenderer).material.SetColor("_EmissionColor", Color.Lerp(new Color(0f, 0f, 0f), new Color(1f, 0f, 0f), num3));
		if (cartCannonMain.stateTimer > 0.2f && !hatchLeft.isPlaying)
		{
			hatchLeft.Play();
			hatchRight.Play();
		}
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
		bool playing = stateCurrent == 2;
		float pitchMultiplier = Mathf.Lerp(0.8f, 1.2f, cartCannonMain.stateTimer / cartCannonMain.stateTimerMax);
		soundBuildupLoop.PlayLoop(playing, 1f, 1f, pitchMultiplier);
		StateMachine();
	}

	private void ResetHatches()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		hatch1Left.localRotation = Quaternion.Euler(0f, 0f, 0f);
		hatch1Right.localRotation = Quaternion.Euler(0f, 0f, 0f);
		hatch2Left.localRotation = Quaternion.Euler(0f, 0f, 0f);
		hatch2Right.localRotation = Quaternion.Euler(0f, 0f, 0f);
		HatchWarningLightsTurnOff();
		ShootParticles(_play: false);
	}

	private void HatchWarningLightsTurnOn()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		((Behaviour)warningLight1Light).enabled = true;
		((Behaviour)warningLight2Light).enabled = true;
		((Renderer)warningLight1MeshRenderer).material.SetColor("_EmissionColor", new Color(1f, 0f, 0f));
		((Renderer)warningLight2MeshRenderer).material.SetColor("_EmissionColor", new Color(1f, 0f, 0f));
	}

	private void HatchWarningLightsTurnOff()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		((Behaviour)warningLight1Light).enabled = false;
		((Behaviour)warningLight2Light).enabled = false;
		((Renderer)warningLight1MeshRenderer).material.SetColor("_EmissionColor", new Color(0f, 0f, 0f));
		((Renderer)warningLight2MeshRenderer).material.SetColor("_EmissionColor", new Color(0f, 0f, 0f));
	}

	private void AnimationEvent1()
	{
		ParticlePlay();
		animationEvent1 = true;
	}

	private void AnimationEvent2()
	{
		ParticlePlay();
		animationEvent2 = true;
	}

	private void ShootParticles(bool _play)
	{
		foreach (ParticleSystem shootParticle in shootParticles)
		{
			if (_play)
			{
				shootParticle.Play();
			}
			else
			{
				shootParticle.Stop();
			}
		}
	}
}
