using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ValuableEgg : Trap
{
	public enum EggState
	{
		State1,
		State2,
		State3,
		State4,
		State5,
		Explode
	}

	[Header("Times")]
	public float explosionTime = 3f;

	public float crackRestTime = 0.5f;

	public Vector2 joltIntervalRange = new Vector2(0.2f, 1f);

	[Header("Physics")]
	public float torqueMultiplier = 0.5f;

	[Header("Camera Shake")]
	public float cameraShakeTime = 0.2f;

	public float cameraShakeStrength = 3f;

	public Vector2 cameraShakeBounds = new Vector2(1.5f, 5f);

	[Header("Arm & Leg Animation")]
	public Vector2 armRange = new Vector2(-45f, 45f);

	public Vector2 legRange = new Vector2(-25f, 20f);

	[Header("Transforms")]
	public Transform objectTransform;

	public Transform armL;

	public Transform armR;

	public Transform legL;

	public Transform legR;

	[Header("Materials")]
	public Material state2Material;

	public Material state3Material;

	public Material state4Material;

	public Material state5Material;

	[Header("Mesh renderer")]
	public MeshRenderer meshRenderer;

	[Header("Particles")]
	public ParticleSystem breakParticle;

	[Space]
	public Light pointLight;

	[Header("Sounds")]
	public Sound crackSound;

	public Sound screamSound;

	public Sound explosionSound;

	private EggState currentState;

	private ParticleScriptExplosion particleScriptExplosion;

	private Rigidbody rb;

	private Vector3 originalSize;

	private float explosionTimer;

	private float joltTimer;

	private bool effectsOn;

	protected override void Start()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		base.Start();
		originalSize = objectTransform.localScale;
		particleScriptExplosion = ((Component)this).GetComponent<ParticleScriptExplosion>();
		rb = ((Component)this).GetComponent<Rigidbody>();
	}

	protected override void Update()
	{
		screamSound.PlayLoop(currentState == EggState.State5, 1f, 1f);
		if (currentState == EggState.Explode)
		{
			Explode();
		}
		if (currentState == EggState.State5)
		{
			PlayEffects();
			AnimateArmsAndLegs();
		}
		if (SemiFunc.IsMasterClientOrSingleplayer() && currentState == EggState.State5)
		{
			explosionTimer += Time.deltaTime;
			if (explosionTimer >= explosionTime)
			{
				SetNextState();
			}
			enemyInvestigate = true;
		}
	}

	protected void FixedUpdate()
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		List<PhysGrabber> playerGrabbing = physGrabObject.playerGrabbing;
		bool flag = false;
		foreach (PhysGrabber item in playerGrabbing)
		{
			if (item.isRotating)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			Quaternion turnX = Quaternion.Euler(0f, -180f, 0f);
			Quaternion turnY = Quaternion.Euler(0f, 0f, 0f);
			Quaternion identity = Quaternion.identity;
			physGrabObject.TurnXYZ(turnX, turnY, identity);
			physGrabObject.OverrideTorqueStrength(2f + physGrabObject.massOriginal);
		}
		if (currentState == EggState.State5)
		{
			ScreamShake();
		}
	}

	[PunRPC]
	public void SetStateRPC(EggState _state, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			currentState = _state;
		}
	}

	private void SetState(EggState _state)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (!SemiFunc.IsMultiplayer())
			{
				SetStateRPC(_state);
				return;
			}
			photonView.RPC("SetStateRPC", (RpcTarget)0, new object[1] { _state });
		}
	}

	private void SetNextState()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			switch (currentState)
			{
			case EggState.State1:
				SetState(EggState.State2);
				break;
			case EggState.State2:
				SetState(EggState.State3);
				break;
			case EggState.State3:
				SetState(EggState.State4);
				break;
			case EggState.State4:
				SetState(EggState.State5);
				break;
			case EggState.State5:
				SetState(EggState.Explode);
				break;
			}
		}
	}

	public void Explode()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		particleScriptExplosion.Spawn(((Component)meshRenderer).transform.position, 1.5f, 100, 300);
		physGrabObject.dead = true;
		Object.Destroy((Object)(object)((Component)this).gameObject);
	}

	public void Crack()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == EggState.State5)
		{
			return;
		}
		crackSound.Play(physGrabObject.centerPoint);
		foreach (PhysGrabber item in new List<PhysGrabber>(physGrabObject.playerGrabbing))
		{
			item.OverrideGrabRelease(photonView.ViewID);
		}
		MainModule main = breakParticle.main;
		switch (currentState)
		{
		case EggState.State1:
			objectTransform.localScale = originalSize * 0.9f;
			((Renderer)meshRenderer).material = state2Material;
			break;
		case EggState.State2:
			objectTransform.localScale = originalSize * 0.7f;
			((Renderer)meshRenderer).material = state3Material;
			main.startColor = new MinMaxGradient(new Color(0.6f, 0.6f, 0f));
			break;
		case EggState.State3:
			objectTransform.localScale = originalSize * 0.5f;
			((Renderer)meshRenderer).material = state4Material;
			main.startColor = new MinMaxGradient(new Color(1f, 0.7f, 0f));
			break;
		case EggState.State4:
			objectTransform.localScale = originalSize * 0.25f;
			((Renderer)meshRenderer).material = state5Material;
			main.startColor = new MinMaxGradient(new Color(1f, 0.3f, 0f));
			EnableLimbs();
			break;
		}
		breakParticle.Play();
		CameraShake();
		SetNextState();
		SetMass();
	}

	private void SetMass()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			switch (currentState)
			{
			case EggState.State1:
				physGrabObject.OverrideMass(2f);
				physGrabObject.massOriginal = 2f;
				break;
			case EggState.State2:
				physGrabObject.OverrideMass(1.75f);
				physGrabObject.massOriginal = 1.75f;
				break;
			case EggState.State3:
				physGrabObject.OverrideMass(1.25f);
				physGrabObject.massOriginal = 1.25f;
				break;
			case EggState.State4:
				physGrabObject.OverrideMass(1f);
				physGrabObject.massOriginal = 1f;
				break;
			case EggState.State5:
				physGrabObject.OverrideMass(0.5f);
				physGrabObject.massOriginal = 0.5f;
				break;
			}
		}
	}

	private void EnableLimbs()
	{
		((Component)armL).gameObject.SetActive(true);
		((Component)armR).gameObject.SetActive(true);
		((Component)legL).gameObject.SetActive(true);
		((Component)legR).gameObject.SetActive(true);
	}

	private void ScreamShake()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		joltTimer -= Time.deltaTime;
		if (joltTimer <= 0f)
		{
			Vector3 insideUnitSphere = Random.insideUnitSphere;
			Vector3 val = insideUnitSphere.normalized * torqueMultiplier;
			rb.AddTorque(val, (ForceMode)1);
			joltTimer = Random.Range(joltIntervalRange.x, joltIntervalRange.y);
		}
	}

	private void CameraShake()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		GameDirector.instance.CameraShake.ShakeDistance(cameraShakeStrength, 3f, 8f, ((Component)this).transform.position, cameraShakeTime);
		GameDirector.instance.CameraImpact.ShakeDistance(cameraShakeStrength, cameraShakeBounds.x, cameraShakeBounds.y, ((Component)this).transform.position, cameraShakeTime);
	}

	private void PlayEffects()
	{
		if (!effectsOn)
		{
			((Behaviour)pointLight).enabled = true;
			effectsOn = true;
		}
	}

	private void AnimateArmsAndLegs()
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Lerp(armRange.x, armRange.y, Mathf.PingPong(Time.time * 8f, 1f));
		float num2 = Mathf.Lerp(armRange.x, armRange.y, Mathf.PingPong(Time.time * 8f + 0.5f, 1f));
		armL.localRotation = Quaternion.Euler(num, 0f, 0f);
		armR.localRotation = Quaternion.Euler(num2, 0f, 0f);
		float num3 = Mathf.Lerp(legRange.x, legRange.y, Mathf.PingPong(Time.time * 8f, 1f));
		float num4 = Mathf.Lerp(legRange.x, legRange.y, Mathf.PingPong(Time.time * 8f + 0.5f, 1f));
		legL.localRotation = Quaternion.Euler(num3, 0f, 0f);
		legR.localRotation = Quaternion.Euler(num4, 0f, 0f);
	}

	public ValuableEgg()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
	}
}
