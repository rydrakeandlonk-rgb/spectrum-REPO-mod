using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

public class ItemStaffTorque : MonoBehaviour
{
	public enum State
	{
		Idle,
		Grabbed,
		Charging,
		Loaded,
		Casting,
		Lowering
	}

	public ParticleSystem particleSparkle;

	public ParticleSystem loadedParticles;

	public ParticleSystem denyParticles;

	public ParticleSystem chargeUpParticles;

	public Sound denySound;

	public Sound denySoundUI;

	public Sound chargeUpSound;

	public Sound lightning01Sound;

	public Sound castSound;

	public Sound choir01Sound;

	public Sound choir02Sound;

	public Sound chargeUpSoundLoop;

	public Sound loadedLowSoundLoop;

	public Sound loadedHighSoundLoop;

	[FormerlySerializedAs("bulletPrefab")]
	public PrefabRef projectilePrefab;

	[FormerlySerializedAs("bulletSpawnPoint")]
	public Transform projectileSpawnPoint;

	public Material crystalMaterial;

	public Renderer crystalRenderer;

	public Transform screenArrow;

	private ItemBattery itemBattery;

	private ItemToggle itemToggle;

	private ItemEquippable itemEquippable;

	private HurtCollider hurtCollider;

	private PropLight propLight;

	private PhysGrabObject physGrabObject;

	private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

	private Color offColor = new Color(0f, 0f, 0f, 1f);

	private Color loadedColor = new Color(1f, 0f, 0.5f, 1f);

	private float crystalLoadedMultiplier = 1f;

	private float initialLightIntensity = 5f;

	private bool choirPlaying;

	private bool choirLocked;

	private float lastChoirPitch01 = 1f;

	private float lastChoirPitch02 = 1f;

	private float lastChoirVolume01;

	private float lastChoirVolume02;

	private bool emissionLocked;

	private bool chargeEmissionLocked;

	private bool loadedSoundLowActive;

	private Color lightOverrideColor;

	private Color lightOverrideColorDefault;

	private Color crystalOverrideColorDefault;

	private float lightOverrideIntensity;

	private float lightOverrideIntensityDefault;

	private float lightOverrideRange;

	private float lightOverrideRangeDefault;

	private float lightOverrideTimer;

	private float lightOverrideFadeIn;

	private float lightOverrideFadeOut;

	private float lightOverrideLerp;

	private bool lightOverrideActive;

	private int lightOverridePhase;

	private float batteryTimer;

	private bool prevToggleState;

	private float castTimer;

	private float castTimerMax;

	private float droppedTimer;

	private float cameraPitch;

	private bool debug;

	private float chargingTimer;

	private float chargingTimerMax;

	private float lookUpProgress;

	private float activationThreshold = -50f;

	private List<ParticleSystem> loadedParticleChildren = new List<ParticleSystem>();

	private List<float> originalEmissionRates = new List<float>();

	public State currentState;

	private bool stateImpulse;

	private void GetComponents()
	{
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		itemBattery = ((Component)this).GetComponent<ItemBattery>();
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		hurtCollider = ((Component)this).GetComponentInChildren<HurtCollider>();
		propLight = ((Component)this).GetComponentInChildren<PropLight>();
	}

	private void Awake()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected Obj, but got Unknown
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		GetComponents();
		crystalMaterial = new Material(crystalMaterial);
		crystalRenderer.material = crystalMaterial;
		((Component)hurtCollider).gameObject.SetActive(false);
		((Component)screenArrow).gameObject.SetActive(false);
		loadedParticles.Stop();
		loadedParticleChildren.Add(loadedParticles);
		List<float> list = originalEmissionRates;
		EmissionModule emission = loadedParticles.emission;
		list.Add(emission.rateOverTimeMultiplier);
		foreach (Transform item in ((Component)loadedParticles).transform)
		{
			ParticleSystem component = ((Component)item).GetComponent<ParticleSystem>();
			if (Object.op_Implicit((Object)(object)component))
			{
				loadedParticleChildren.Add(component);
				List<float> list2 = originalEmissionRates;
				emission = component.emission;
				list2.Add(emission.rateOverTimeMultiplier);
			}
		}
	}

	private void Update()
	{
		SetCameraPitch();
		DebugRuben();
		StateMachine();
		UpdateChargeShake();
		UpdateCrystalEmission();
		LightOverrideTick();
		UpdateScreenArrow();
		UpdateLoadedParticleEmissionRate();
		UpdateChargeParticleEmissionRate();
		UpdateChoirSounds();
		UpdateLoadedSoundLoops();
		UpdateChargeSoundLoop();
		UpdateBatteryUI();
		UpdateInvestigation();
	}

	private void DebugRuben()
	{
		if (debug)
		{
			castTimerMax = 0.05f;
			chargingTimerMax = 0.05f;
		}
		else
		{
			castTimerMax = 0.5f;
			chargingTimerMax = 0.5f;
		}
		if (SemiFunc.KeyDownRuben((KeyCode)117))
		{
			debug = !debug;
			SemiLogger.LogRuben($"Debug mode: {debug}");
		}
		if (SemiFunc.KeyDownRuben((KeyCode)327) && !itemEquippable.isEquipped)
		{
			CastSpell(launch: true);
		}
	}

	private void UpdateInvestigation()
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && SemiFunc.PerSecond(1f, (Object)(object)this) && IsGrabbed() && !IsBroken())
		{
			float num = Mathf.Clamp01(Mathf.InverseLerp(0f, activationThreshold, cameraPitch));
			if ((currentState == State.Grabbed || currentState == State.Charging) && num > 0.05f)
			{
				float range = Mathf.Lerp(8f, 18f, num);
				SemiFunc.EnemyInvestigate(((Component)this).transform.position, range);
			}
		}
	}

	private void StateMachine()
	{
		switch (currentState)
		{
		case State.Idle:
			StateIdle();
			break;
		case State.Grabbed:
			StateGrabbed();
			break;
		case State.Charging:
			StateCharging();
			break;
		case State.Loaded:
			StateLoaded();
			break;
		case State.Casting:
			StateCasting();
			break;
		case State.Lowering:
			StateLowering();
			break;
		}
	}

	private void StateSet(State _newState)
	{
		if (currentState != _newState)
		{
			currentState = _newState;
			stateImpulse = true;
		}
	}

	private void StateIdle()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			((Component)screenArrow).gameObject.SetActive(false);
			loadedParticles.Stop(true);
			chargeUpParticles.Stop(true);
			chargingTimer = 0f;
			castTimer = 0f;
		}
		if (IsGrabbed())
		{
			StateSet(State.Grabbed);
		}
	}

	private void StateGrabbed()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			if (!IsBroken())
			{
				((Component)screenArrow).gameObject.SetActive(true);
			}
			if (!IsBroken())
			{
				chargeUpParticles.Play(true);
			}
		}
		if (!IsGrabbed())
		{
			StateSet(State.Idle);
			return;
		}
		AlignToWorldUp();
		OnTogglePressed();
		if (cameraPitch < activationThreshold && !IsBroken())
		{
			StateSet(State.Charging);
		}
	}

	private void StateCharging()
	{
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			stateImpulse = false;
			if (!IsBroken())
			{
				((Component)screenArrow).gameObject.SetActive(true);
			}
			chargingTimer = 0f;
			chargeUpSound.Play(projectileSpawnPoint);
			ChargeImpact(2f);
		}
		if (!IsGrabbed())
		{
			StateSet(State.Idle);
			return;
		}
		AlignToWorldUp();
		OnTogglePressed();
		if (cameraPitch >= activationThreshold)
		{
			StateSet(State.Grabbed);
			return;
		}
		chargingTimer += Time.deltaTime;
		if (chargingTimer >= chargingTimerMax)
		{
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				physGrabObject.rb.AddForceAtPosition(projectileSpawnPoint.forward * 3f, projectileSpawnPoint.position, (ForceMode)1);
			}
			LightningSFX();
			loadedParticles.Play();
			OverrideLightAndCrystal(Color.white, 25f, 10f, 0.1f, 0.05f, 0.5f);
			StateSet(State.Loaded);
		}
	}

	private void StateLoaded()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			stateImpulse = false;
			((Component)screenArrow).gameObject.SetActive(false);
			SemiFunc.EnemyInvestigate(((Component)this).transform.position, 7.5f);
		}
		if (!IsGrabbed())
		{
			denyParticles.Play(true);
			denySound.Play(projectileSpawnPoint);
			OverrideLightAndCrystal(loadedColor, 1f, 0f, 0.1f);
			StateSet(State.Idle);
			return;
		}
		OnTogglePressed();
		if (!physGrabObject.isRotating)
		{
			Quaternion turnX = Quaternion.Euler(75f, 0f, 0f);
			Quaternion turnY = Quaternion.Euler(0f, 0f, 0f);
			Quaternion turnZ = Quaternion.Euler(0f, 0f, 0f);
			physGrabObject.TurnXYZ(turnX, turnY, turnZ);
		}
	}

	private void StateCasting()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			castSound.Play(projectileSpawnPoint);
			ChargeImpact(3f);
			((Component)screenArrow).gameObject.SetActive(false);
			loadedParticles.Stop();
			chargeUpParticles.Stop(true);
			castTimer = 0f;
		}
		if (!IsGrabbed())
		{
			StateSet(State.Idle);
			return;
		}
		castTimer += Time.deltaTime;
		if (castTimer >= castTimerMax)
		{
			CastSpell(launch: true);
			StateSet(State.Lowering);
		}
	}

	private void StateLowering()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			droppedTimer = 0f;
		}
		if (!IsGrabbed())
		{
			droppedTimer += Time.deltaTime;
			if (droppedTimer > 2f)
			{
				StateSet(State.Idle);
			}
			return;
		}
		droppedTimer = 0f;
		AlignToWorldUp();
		OnTogglePressed();
		if (cameraPitch >= activationThreshold)
		{
			StateSet(State.Grabbed);
		}
	}

	private void OnTogglePressed()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		if (itemToggle.toggleState == prevToggleState)
		{
			return;
		}
		prevToggleState = itemToggle.toggleState;
		if (currentState == State.Loaded)
		{
			StateSet(State.Casting);
			return;
		}
		OverrideLightAndCrystal(loadedColor, 1f, 0f, 0.1f);
		denySound.Play(projectileSpawnPoint);
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			Rigidbody rb = physGrabObject.rb;
			Vector3 insideUnitSphere = Random.insideUnitSphere;
			rb.AddForce(insideUnitSphere.normalized * 1f, (ForceMode)1);
		}
		denyParticles.Play(true);
		if (physGrabObject.grabbedLocal && !IsBroken())
		{
			denySoundUI.Play(projectileSpawnPoint);
			BatteryUI.instance.DenyShake();
			SemiFunc.CameraShake(0.5f, 0.1f);
			SemiFunc.CameraShakeImpact(1f, 0.1f);
		}
	}

	private void CastSpell(bool launch)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		if (itemBattery.batteryLifeInt <= 0 || physGrabObject.impactDetector.inCart)
		{
			return;
		}
		OverrideLightAndCrystal(Color.white, 15f, 0f, 0.1f);
		SemiFunc.CameraShakeDistance(projectileSpawnPoint.position, 3f, 0.1f, 3f, 8f);
		SemiFunc.CameraShakeImpactDistance(projectileSpawnPoint.position, 6f, 0.1f, 3f, 8f);
		batteryTimer = 0.5f;
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (Object.op_Implicit((Object)(object)physGrabObject.lastPlayerGrabbing))
		{
			SemiFunc.PlayerShopKeeperStaffCastHost(physGrabObject.lastPlayerGrabbing);
		}
		ConsumeAmmo();
		if (physGrabObject.grabbedLocal)
		{
			PlayerController.instance.ForceImpulse(-projectileSpawnPoint.forward * 10f);
		}
		physGrabObject.rb.AddForceAtPosition(-projectileSpawnPoint.forward * 5f, projectileSpawnPoint.position, (ForceMode)1);
		Vector3 val = ((physGrabObject.playerGrabbing.Count <= 0) ? projectileSpawnPoint.forward : physGrabObject.playerGrabbing[0].playerAvatar.localCamera.GetOverrideTransform().forward);
		float num = 0.8f;
		if (physGrabObject.isRotating)
		{
			num = 0f;
		}
		Vector3 val2 = Vector3.Lerp(projectileSpawnPoint.forward, val, num);
		GameObject val3 = ((!SemiFunc.IsMultiplayer()) ? Object.Instantiate<GameObject>(projectilePrefab.Prefab, projectileSpawnPoint.position, Quaternion.LookRotation(val2)) : PhotonNetwork.Instantiate(projectilePrefab.ResourcePath, projectileSpawnPoint.position, Quaternion.LookRotation(val2), (byte)0, (object[])null));
		SlowProjectile slowProjectile = ((val3 != null) ? val3.GetComponent<SlowProjectile>() : null);
		if (!Object.op_Implicit((Object)(object)slowProjectile))
		{
			return;
		}
		slowProjectile.SetSpawner(((Component)this).gameObject);
		slowProjectile.playerCausingHurt = physGrabObject.lastPlayerGrabbing;
		slowProjectile.investigateRadiusOnHit = 24f;
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
			slowProjectile.Launch(val2);
		}
	}

	private void ConsumeAmmo()
	{
		itemBattery.RemoveFullBar(1);
	}

	private void UpdateScreenArrow()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (((Component)screenArrow).gameObject.activeSelf)
		{
			float num = 0.025f;
			float num2 = 10f;
			float num3 = 8f;
			float num4 = Mathf.Sin(Mathf.Floor(Time.time * num3) / num3 * num2) * num;
			Vector3 localPosition = new Vector3(0f, num4, 0f);
			screenArrow.localPosition = localPosition;
		}
	}

	private void UpdateChargeParticleEmissionRate()
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		if (!IsGrabbed() || IsBroken())
		{
			chargeUpParticles.Stop(true);
			loadedParticles.Stop(true);
			return;
		}
		if (cameraPitch > activationThreshold && currentState == State.Loaded)
		{
			chargeEmissionLocked = true;
		}
		if (currentState != State.Loaded)
		{
			chargeEmissionLocked = false;
		}
		float num = 5f;
		num = ((!chargeEmissionLocked) ? Mathf.Lerp(0f, 25f, Mathf.InverseLerp(0f, activationThreshold, cameraPitch)) : 5f);
		EmissionModule emission = chargeUpParticles.emission;
		emission.rateOverTime = MinMaxCurve.op_Implicit(num);
		foreach (Transform item in ((Component)chargeUpParticles).transform)
		{
			ParticleSystem component = ((Component)item).GetComponent<ParticleSystem>();
			if (Object.op_Implicit((Object)(object)component))
			{
				EmissionModule emission2 = component.emission;
				emission2.rateOverTime = MinMaxCurve.op_Implicit(num);
			}
		}
	}

	private void UpdateLoadedParticleEmissionRate()
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != State.Loaded && currentState != State.Casting)
		{
			emissionLocked = false;
		}
		else
		{
			if (!SemiFunc.PerSecond(10f, (Object)(object)this))
			{
				return;
			}
			if (currentState == State.Loaded && cameraPitch > activationThreshold)
			{
				emissionLocked = true;
			}
			float num = 1f;
			if (emissionLocked)
			{
				num = 0.1f;
			}
			if (currentState == State.Casting)
			{
				num = 1f;
			}
			float num2 = num;
			for (int i = 0; i < loadedParticleChildren.Count; i++)
			{
				EmissionModule emission = loadedParticleChildren[i].emission;
				float num3 = num2;
				if (i == 0)
				{
					num3 = Mathf.Lerp(0.01f, 1f, num);
				}
				emission.rateOverTimeMultiplier = originalEmissionRates[i] * num3;
			}
		}
	}

	private void UpdateCrystalEmission()
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		bool flag = currentState == State.Loaded || currentState == State.Casting;
		float num = 0f;
		float intensity = 0f;
		if (IsGrabbed() && !IsBroken())
		{
			if (flag)
			{
				num = crystalLoadedMultiplier;
				intensity = initialLightIntensity;
				float num2 = Mathf.Sin(Time.time * 12f) * 1f;
				num += num2 / 10f;
				intensity += num2 / 1f;
			}
			else
			{
				num = Mathf.Lerp(0f, crystalLoadedMultiplier, lookUpProgress);
				intensity = Mathf.Lerp(0f, initialLightIntensity, lookUpProgress);
			}
		}
		Color val = loadedColor * (num * 3f);
		if (lightOverrideActive)
		{
			lightOverrideIntensityDefault = intensity;
			crystalOverrideColorDefault = val;
		}
		else
		{
			crystalMaterial.SetColor(EmissionColorID, val);
			propLight.SetIntensity(intensity);
		}
	}

	public void OverrideLightAndCrystal(Color _color, float _intensity, float _holdTime, float _fadeIn = 0.2f, float _fadeOut = 0.2f)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		OverrideLightAndCrystal(_color, _intensity, -1f, _holdTime, _fadeIn, _fadeOut);
	}

	public void OverrideLightAndCrystal(Color _color, float _intensity, float _range, float _holdTime, float _fadeIn, float _fadeOut)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		if (!lightOverrideActive)
		{
			lightOverrideColorDefault = propLight.lightComponent.color;
			lightOverrideIntensityDefault = propLight.lightComponent.intensity;
			lightOverrideRangeDefault = propLight.lightComponent.range;
			crystalOverrideColorDefault = crystalMaterial.GetColor(EmissionColorID);
		}
		lightOverrideColor = _color;
		lightOverrideIntensity = _intensity;
		lightOverrideRange = ((_range < 0f) ? lightOverrideRangeDefault : _range);
		lightOverrideTimer = _holdTime;
		lightOverrideFadeIn = _fadeIn;
		lightOverrideFadeOut = _fadeOut;
		lightOverrideLerp = 0f;
		lightOverridePhase = 0;
		lightOverrideActive = true;
	}

	private void LightOverrideTick()
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		if (!lightOverrideActive)
		{
			return;
		}
		if (lightOverridePhase == 0)
		{
			lightOverrideLerp = Mathf.MoveTowards(lightOverrideLerp, 1f, Time.deltaTime / lightOverrideFadeIn);
			if (lightOverrideLerp >= 0.999f)
			{
				lightOverrideLerp = 1f;
				lightOverridePhase = 1;
			}
		}
		else if (lightOverridePhase == 1)
		{
			lightOverrideTimer -= Time.deltaTime;
			if (lightOverrideTimer <= 0f)
			{
				lightOverridePhase = 2;
			}
		}
		else
		{
			lightOverrideLerp = Mathf.MoveTowards(lightOverrideLerp, 0f, Time.deltaTime / lightOverrideFadeOut);
		}
		propLight.lightComponent.color = Color.Lerp(lightOverrideColorDefault, lightOverrideColor, lightOverrideLerp);
		propLight.lightComponent.intensity = Mathf.Lerp(lightOverrideIntensityDefault, lightOverrideIntensity, lightOverrideLerp);
		propLight.lightComponent.range = Mathf.Lerp(lightOverrideRangeDefault, lightOverrideRange, lightOverrideLerp);
		crystalMaterial.SetColor(EmissionColorID, Color.Lerp(crystalOverrideColorDefault, lightOverrideColor, lightOverrideLerp));
		if (lightOverrideLerp <= 0.001f)
		{
			lightOverrideLerp = 0f;
			lightOverrideActive = false;
		}
	}

	private void UpdateChoirSounds()
	{
		bool flag = currentState == State.Loaded || currentState == State.Casting;
		if (!flag)
		{
			choirLocked = false;
		}
		if (physGrabObject.playerGrabbing.Count <= 0)
		{
			choir01Sound.PlayLoop(playing: false, 2f, 1f, lastChoirPitch01, lastChoirVolume01);
			choir02Sound.PlayLoop(playing: false, 2f, 1f, lastChoirPitch02, lastChoirVolume02);
			return;
		}
		float num = Mathf.InverseLerp(-10f, activationThreshold, cameraPitch);
		float num2 = Mathf.InverseLerp(0f, activationThreshold, cameraPitch);
		if (flag && cameraPitch > activationThreshold)
		{
			choirLocked = true;
		}
		choirPlaying = cameraPitch < 0f && !choirLocked && !IsBroken();
		float num3 = Mathf.InverseLerp(0f, -15f, cameraPitch);
		float num4 = Mathf.InverseLerp(-15f, -40f, cameraPitch);
		float pitchMultiplier = (choirLocked ? 1f : Mathf.Lerp(0.75f, 1f, num));
		float pitchMultiplier2 = (choirLocked ? 1f : Mathf.Lerp(0.75f, 1f, num2));
		float volumeMultiplier = num3 * (1f - num4);
		float volumeMultiplier2 = num4;
		lastChoirPitch01 = pitchMultiplier;
		lastChoirPitch02 = pitchMultiplier2;
		lastChoirVolume01 = volumeMultiplier;
		lastChoirVolume02 = volumeMultiplier2;
		choir01Sound.PlayLoop(choirPlaying, 2f, 1f, pitchMultiplier, volumeMultiplier);
		choir02Sound.PlayLoop(choirPlaying, 2f, 1f, pitchMultiplier2, volumeMultiplier2);
	}

	private void UpdateChargeSoundLoop()
	{
		bool playing = currentState == State.Grabbed;
		float volumeMultiplier = Mathf.Clamp01(Mathf.InverseLerp(0f, activationThreshold, cameraPitch));
		if (IsBroken())
		{
			volumeMultiplier = 0f;
		}
		chargeUpSoundLoop.PlayLoop(playing, 2f, 1f, 1f, volumeMultiplier);
	}

	private void UpdateLoadedSoundLoops()
	{
		if (currentState != State.Loaded && currentState != State.Casting)
		{
			loadedHighSoundLoop.PlayLoop(playing: false, 2f, 1f);
			loadedLowSoundLoop.PlayLoop(playing: false, 2f, 1f);
			loadedSoundLowActive = false;
			return;
		}
		if (cameraPitch > activationThreshold)
		{
			loadedSoundLowActive = true;
		}
		loadedHighSoundLoop.PlayLoop(!loadedSoundLowActive, 2f, 1f);
		loadedLowSoundLoop.PlayLoop(loadedSoundLowActive, 2f, 1f);
	}

	private void UpdateChargeShake()
	{
		if (!physGrabObject.grabbedLocal || itemEquippable.isEquipped || IsBroken())
		{
			return;
		}
		float strengthAdd = 0f;
		if (currentState == State.Grabbed)
		{
			strengthAdd = Mathf.Lerp(0f, 4f, lookUpProgress);
		}
		else if (currentState == State.Charging)
		{
			float num = Mathf.Clamp01(chargingTimer / Mathf.Max(chargingTimerMax, 0.01f));
			if (stateImpulse)
			{
				num = 0f;
			}
			strengthAdd = Mathf.Lerp(4f, 6f, num);
		}
		else if (currentState == State.Casting)
		{
			float num2 = Mathf.Clamp01(castTimer / Mathf.Max(castTimerMax, 0.01f));
			if (stateImpulse)
			{
				num2 = 0f;
			}
			strengthAdd = Mathf.Lerp(2f, 5f, num2);
		}
		GameDirector.instance.CameraShake.Shake(strengthAdd, 0.1f);
	}

	private void LightningSFX()
	{
		lightning01Sound.Play(projectileSpawnPoint);
		ChargeImpact(5f);
	}

	private void ChargeImpact(float _strength)
	{
		if (physGrabObject.grabbedLocal && !itemEquippable.isEquipped && !IsBroken())
		{
			SemiFunc.CameraShakeImpact(_strength, 0.08f);
		}
	}

	private void UpdateBatteryUI()
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		if (physGrabObject.grabbedLocal)
		{
			bool num = currentState == State.Loaded || currentState == State.Casting || batteryTimer > 0f || IsBroken();
			if (batteryTimer > 0f)
			{
				batteryTimer -= Time.deltaTime;
			}
			if (!num)
			{
				BatteryUI.instance.OverrideColor(Color.grey, 0.3f);
			}
		}
	}

	private void AlignToWorldUp()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && currentState != State.Loaded && currentState != State.Casting)
		{
			PhysGrabber physGrabber = physGrabObject.playerGrabbing[0];
			Transform overrideTransform = physGrabber.playerAvatar.localCamera.GetOverrideTransform();
			Quaternion val = Quaternion.Inverse(overrideTransform.rotation);
			Vector3 forward = overrideTransform.forward;
			forward.y = 0f;
			forward.Normalize();
			physGrabber.cameraRelativeGrabbedForward = val * forward;
			physGrabber.cameraRelativeGrabbedUp = val * Vector3.up;
		}
	}

	private bool IsBroken()
	{
		return itemBattery.batteryLifeInt <= 0;
	}

	private bool IsGrabbed()
	{
		return physGrabObject.playerGrabbing.Count > 0;
	}

	private void SetCameraPitch()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (physGrabObject.playerGrabbing.Count > 0)
		{
			float num = physGrabObject.playerGrabbing[0].playerAvatar.localCamera.GetOverrideTransform().eulerAngles.x;
			if (num > 180f)
			{
				num -= 360f;
			}
			cameraPitch = num;
			lookUpProgress = Mathf.InverseLerp(0f, activationThreshold, cameraPitch);
		}
	}

	public ItemStaffTorque()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
	}
}
