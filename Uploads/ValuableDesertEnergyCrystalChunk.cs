using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class ValuableDesertEnergyCrystalChunk : MonoBehaviour
{
	public enum States
	{
		Idle,
		Active
	}

	internal States currentState;

	private bool stateStart;

	private PhysGrabObject physGrabObject;

	private PhysGrabObjectImpactDetector impactDetector;

	private PhotonView photonView;

	public Light crystalLight;

	public Sound soundCrystalLoop;

	public ParticleSystem crystalParticles;

	public MeshRenderer mainMeshRenderer;

	public List<Transform> crystals = new List<Transform>();

	public List<MeshRenderer> crystalMeshRenderers = new List<MeshRenderer>();

	public float wiggleSpeed = 150f;

	public float wiggleDegrees = 5f;

	public float settleSpeed = 250f;

	public float settleDegrees = 10f;

	public float settleDuration = 2f;

	public AnimationCurve settleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public AnimationCurve flickerOnCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public AnimationCurve flickerOffCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public float staminaRechargePerSecond = 30f;

	public float sprintSpeedMultiplier = 3f;

	public float sprintSpeedUpgradeHeadroom = 0.3f;

	[ColorUsage(true, true)]
	public Color beamEffectColor = new Color(4.2f, 4f, 0f);

	public float beamEffectStrength = 0.5f;

	public float headTwitchDegrees = 8f;

	public ParticleSystem chargeCloudParticles;

	public Sound soundChargingCloud;

	public float chargeCloudDurationMin = 1f;

	public float chargeCloudDurationMax = 2f;

	public float chargeCloudRadius = 3f;

	public float chargeCloudChargeMultiplier = 3f;

	private float chargeCloudTimer;

	private readonly List<ItemBattery> chargeCloudBatteries = new List<ItemBattery>();

	private Quaternion[] crystalBaseRotations;

	private float settleLerp = 1f;

	private float flickerOnTimer = -1f;

	private float flickerOffTimer = -1f;

	private float flickerMultiplier;

	private float soundPitchLerp;

	private bool energizedPrev;

	private void Start()
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected Obj, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected Obj, but got Unknown
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		impactDetector.onAllBreaks.AddListener((UnityAction)BreakImpulseChargeCloud);
		impactDetector.onDestroy.AddListener((UnityAction)DetachPlayingParticles);
		CrystalBaseRotationsSet();
	}

	private void Update()
	{
		StateMachine();
		FlickerEnergizedLogic();
		VisualGlowLogic();
		WiggleCrystalsLogic();
		CrystalParticlesLogic();
		PlayLoopSoundLogic();
		EnergizeLogic();
		ChargeCloudTick();
	}

	private void StateIdle()
	{
		if (stateStart)
		{
			stateStart = false;
		}
		if (SemiFunc.IsMasterClientOrSingleplayer() && physGrabObject.grabbed)
		{
			SetState(States.Active);
		}
	}

	private void StateActive()
	{
		if (stateStart)
		{
			stateStart = false;
		}
		if (SemiFunc.IsMasterClientOrSingleplayer() && !physGrabObject.grabbed)
		{
			SetState(States.Idle);
		}
	}

	private void StateMachine()
	{
		switch (currentState)
		{
		case States.Idle:
			StateIdle();
			break;
		case States.Active:
			StateActive();
			break;
		}
	}

	private void SetState(States _state)
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

	private bool Energized()
	{
		if (currentState != States.Active)
		{
			return chargeCloudTimer > 0f;
		}
		return true;
	}

	private void VisualGlowLogic()
	{
		FlickerTick();
		crystalLight.intensity = 4f * flickerMultiplier;
		if (flickerMultiplier > 0f && !((Component)crystalLight).gameObject.activeSelf)
		{
			((Component)crystalLight).gameObject.SetActive(true);
		}
		if (flickerMultiplier <= 0f && ((Component)crystalLight).gameObject.activeSelf)
		{
			((Component)crystalLight).gameObject.SetActive(false);
		}
		VisualGlowEmission();
		VisualMainMeshEmission();
	}

	private void FlickerTick()
	{
		if (flickerOnTimer >= 0f)
		{
			float num = Mathf.Clamp01(flickerOnTimer / 0.2f);
			flickerMultiplier = flickerOnCurve.Evaluate(num);
			flickerOnTimer += Time.deltaTime;
			if (num >= 1f)
			{
				flickerOnTimer = -1f;
				flickerMultiplier = 1f;
			}
		}
		if (flickerOffTimer >= 0f)
		{
			float num2 = Mathf.Clamp01(flickerOffTimer / 0.5f);
			flickerMultiplier = 1f - flickerOffCurve.Evaluate(num2);
			flickerOffTimer += Time.deltaTime;
			if (num2 >= 1f)
			{
				flickerOffTimer = -1f;
				flickerMultiplier = 0f;
			}
		}
	}

	private void FlickerEnergizedLogic()
	{
		bool flag = Energized();
		if (flag && !energizedPrev)
		{
			FlickerOn();
		}
		if (!flag && energizedPrev)
		{
			FlickerOff();
		}
		energizedPrev = flag;
	}

	private void FlickerOn()
	{
		flickerOnTimer = 0f;
		flickerOffTimer = -1f;
	}

	private void FlickerOff()
	{
		flickerOffTimer = 0f;
		flickerOnTimer = -1f;
	}

	private void EnergizeLogic()
	{
		if (Energized())
		{
			PitchUpGrabbersVoices();
			TwitchGrabbersHeads();
			EnergizeGrabBeams();
			EnergizeLocalPlayer();
		}
	}

	private void VisualGlowEmission()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		Color val = new Color(0.98f, 1f, 0f);
		if (!Energized())
		{
			val = Color.yellow;
		}
		for (int i = 0; i < crystalMeshRenderers.Count; i++)
		{
			float num = Mathf.Max((0.1f + Mathf.PingPong((Time.time + (float)i) * 5f, 1f)) * flickerMultiplier, 0.1f);
			((Renderer)crystalMeshRenderers[i]).material.SetColor("_EmissionColor", val * Mathf.LinearToGammaSpace(num));
		}
	}

	private void VisualMainMeshEmission()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)mainMeshRenderer))
		{
			Color val = new Color(0.98f, 1f, 0f);
			((Renderer)mainMeshRenderer).material.SetColor("_EmissionColor", val * Mathf.LinearToGammaSpace(flickerMultiplier));
		}
	}

	private void WiggleCrystalsLogic()
	{
		if (crystals.Count != 0)
		{
			if (Energized())
			{
				settleLerp = 0f;
				WiggleCrystalsApply(wiggleSpeed, wiggleDegrees, 1f);
			}
			else if (!(settleLerp >= 1f))
			{
				settleLerp = Mathf.Clamp01(settleLerp + Time.deltaTime / settleDuration);
				float multiplier = Mathf.Max(0f, 1f - settleCurve.Evaluate(settleLerp));
				WiggleCrystalsApply(settleSpeed, settleDegrees, multiplier);
			}
		}
	}

	private void WiggleCrystalsApply(float _speed, float _degrees, float _multiplier)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < crystals.Count; i++)
		{
			crystals[i].localRotation = crystalBaseRotations[i] * WiggleRotationGet(i, _speed, _degrees * _multiplier);
		}
	}

	private Quaternion WiggleRotationGet(int _index, float _speed, float _degrees)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		if (_degrees <= 0f)
		{
			return Quaternion.identity;
		}
		float num = Mathf.PingPong((Time.time + (float)_index) * _speed, _degrees) - _degrees * 0.5f;
		float num2 = Mathf.PingPong((Time.time + (float)_index + 0.33f) * _speed, _degrees) - _degrees * 0.5f;
		float num3 = Mathf.PingPong((Time.time + (float)_index + 0.66f) * _speed, _degrees) - _degrees * 0.5f;
		return Quaternion.Euler(num, num2, num3);
	}

	private void CrystalBaseRotationsSet()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		crystalBaseRotations = new Quaternion[crystals.Count];
		for (int i = 0; i < crystals.Count; i++)
		{
			crystalBaseRotations[i] = crystals[i].localRotation;
		}
	}

	private void CrystalParticlesLogic()
	{
		if (!Object.op_Implicit((Object)(object)crystalParticles))
		{
			return;
		}
		if (Energized())
		{
			if (!crystalParticles.isPlaying)
			{
				crystalParticles.Play(true);
			}
		}
		else if (crystalParticles.isPlaying)
		{
			crystalParticles.Stop(true, (ParticleSystemStopBehavior)1);
		}
	}

	private void TwitchGrabbersHeads()
	{
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (Object.op_Implicit((Object)(object)item))
			{
				item.playerAvatar.playerAvatarVisuals.LeanTwitchOverride(headTwitchDegrees);
			}
		}
	}

	private void EnergizeGrabBeams()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (Object.op_Implicit((Object)(object)item))
			{
				item.physGrabBeamComponent.OverrideBeamEffect(beamEffectColor, beamEffectStrength);
			}
		}
	}

	private void PitchUpGrabbersVoices()
	{
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (Object.op_Implicit((Object)(object)item) && !item.isLocal && Object.op_Implicit((Object)(object)item.playerAvatar) && Object.op_Implicit((Object)(object)item.playerAvatar.voiceChat))
			{
				item.playerAvatar.voiceChat.OverridePitch(2f, 1f, 2f);
			}
		}
	}

	private void EnergizeLocalPlayer()
	{
		if (physGrabObject.grabbedLocal)
		{
			PlayerAvatar instance = PlayerAvatar.instance;
			if (Object.op_Implicit((Object)(object)instance.voiceChat))
			{
				instance.voiceChat.OverridePitch(2f, 1f, 2f);
			}
			instance.OverridePupilSize(3f, 4, 1f, 1f, 5f, 0.5f);
			instance.playerHealth.EyeMaterialOverride(PlayerHealth.EyeOverrideState.Energy, 0.25f, 0);
			OverrideSpeedSoftCapped();
			PlayerController.instance.OverrideLookSpeed(100f, 2f, 1f);
			PlayerController.instance.OverrideAnimationSpeed(3f, 1f, 2f);
			StaminaRecharge();
			CameraZoom.Instance.OverrideZoomSet(85f, 0.1f, 0.5f, 1f, ((Component)this).gameObject, 0);
			PostProcessing.Instance.SaturationOverride(50f, 0.1f, 0.5f, 0.1f, ((Component)this).gameObject);
		}
	}

	private void OverrideSpeedSoftCapped()
	{
		PlayerController instance = PlayerController.instance;
		float sprintCap = (instance.playerOriginalSprintSpeed - instance.SprintSpeedUpgrades) * sprintSpeedMultiplier * (1f + sprintSpeedUpgradeHeadroom);
		instance.OverrideSpeed(sprintSpeedMultiplier, 0.1f, sprintCap);
	}

	private void StaminaRecharge()
	{
		PlayerController instance = PlayerController.instance;
		instance.EnergyCurrent = Mathf.Min(instance.EnergyCurrent + staminaRechargePerSecond * Time.deltaTime, instance.EnergyStart);
	}

	private void PlayLoopSoundLogic()
	{
		if (Energized())
		{
			soundPitchLerp = Mathf.Lerp(soundPitchLerp, 1f, Time.deltaTime * 2f);
		}
		else
		{
			soundPitchLerp = Mathf.Lerp(soundPitchLerp, 0f, Time.deltaTime * 10f);
		}
		float pitchMultiplier = Mathf.Lerp(0.5f, 2f, soundPitchLerp);
		soundCrystalLoop.PlayLoop(Energized(), 0.8f, 0.8f, pitchMultiplier);
	}

	private void BreakImpulseChargeCloud()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && !physGrabObject.dead)
		{
			float num = Random.Range(chargeCloudDurationMin, chargeCloudDurationMax);
			if (!SemiFunc.IsMultiplayer())
			{
				ChargeCloudStartRPC(num);
				return;
			}
			photonView.RPC("ChargeCloudStartRPC", (RpcTarget)0, new object[1] { num });
		}
	}

	private void ChargeCloudTick()
	{
		if (chargeCloudTimer <= 0f && Object.op_Implicit((Object)(object)chargeCloudParticles) && chargeCloudParticles.isEmitting)
		{
			chargeCloudParticles.Stop(true, (ParticleSystemStopBehavior)1);
		}
		if (chargeCloudTimer > 0f)
		{
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				ChargeItemsInRange();
			}
			chargeCloudTimer -= Time.deltaTime;
		}
	}

	private void ChargeItemsInRange()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		chargeCloudBatteries.Clear();
		foreach (PhysGrabObject item in SemiFunc.PhysGrabObjectGetAllWithinRange(chargeCloudRadius, physGrabObject.midPoint))
		{
			if (!((Object)(object)item == (Object)(object)physGrabObject))
			{
				ItemBattery component = ((Component)item).GetComponent<ItemBattery>();
				if (SemiFunc.BatteryChargeCondition(component) && !chargeCloudBatteries.Contains(component))
				{
					chargeCloudBatteries.Add(component);
				}
			}
		}
		if (chargeCloudBatteries.Count == 0)
		{
			return;
		}
		float chargeAmount = SemiFunc.BatteryGetChargeRate(3) * chargeCloudChargeMultiplier / (float)chargeCloudBatteries.Count;
		foreach (ItemBattery chargeCloudBattery in chargeCloudBatteries)
		{
			chargeCloudBattery.ChargeBattery(((Component)this).gameObject, chargeAmount);
		}
	}

	private void DetachPlayingParticles()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		ParticleSystem[] componentsInChildren = ((Component)this).GetComponentsInChildren<ParticleSystem>(true);
		foreach (ParticleSystem val in componentsInChildren)
		{
			if (val.isPlaying)
			{
				MainModule main = val.main;
				main.stopAction = (ParticleSystemStopAction)2;
				((Component)val).transform.SetParent((Transform)null, true);
				val.Stop(true, (ParticleSystemStopBehavior)1);
			}
		}
	}

	[PunRPC]
	public void SetStateRPC(States _state, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			currentState = _state;
			stateStart = true;
		}
	}

	[PunRPC]
	public void ChargeCloudStartRPC(float _duration, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return;
		}
		bool flag = chargeCloudTimer > 0f;
		chargeCloudTimer = _duration;
		if (!flag)
		{
			if (Object.op_Implicit((Object)(object)chargeCloudParticles) && !chargeCloudParticles.isPlaying)
			{
				chargeCloudParticles.Play(true);
			}
			soundChargingCloud.Play(((Component)this).transform);
		}
	}

	public ValuableDesertEnergyCrystalChunk()
	{
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
	}
}
