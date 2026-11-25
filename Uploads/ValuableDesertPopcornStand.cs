using UnityEngine;

public class ValuableDesertPopcornStand : MonoBehaviour
{
	public PhysGrabObject physGrabObject;

	public ParticleSystem popcornParticles;

	public Sound audioPopLoop;

	public Light[] popcornLights;

	public float lightIntensityMax = 4f;

	public AnimationCurve lightOnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	public AnimationCurve lightOffCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	public float lightAnimDuration = 0.25f;

	public float revUpDuration = 4f;

	public float revDownDuration = 8f;

	public float popcornRateMax = 45f;

	public float pitchMin = 0.7f;

	public float pitchMax = 1.3f;

	public float volumeMin;

	public float volumeMax = 1f;

	public Sound audioLampOn;

	public Sound audioLampOff;

	public Sound audioLightFlicker;

	public float flickerFrequency = 100f;

	public AnimationCurve flickerCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

	public float investigateRangeMin = 1f;

	public float investigateRangeMax = 25f;

	private float revValue;

	private float revSmooth;

	private float investigateTimer;

	private EmissionModule popcornEmission;

	private bool lampIsOn;

	private bool wasPopping;

	private float lightAnimTimer;

	private float flickerSoundTimer;

	private void Awake()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)physGrabObject))
		{
			physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		}
		popcornEmission = popcornParticles.emission;
		popcornParticles.Stop(true, (ParticleSystemStopBehavior)1);
		lightAnimTimer = lightAnimDuration;
	}

	private void Update()
	{
		RevLogic();
		PopcornParticleLogic();
		PopLoopSoundLogic();
		LampTransitionLogic();
		LightAnimationLogic();
		LightFlickerSoundLogic();
		InvestigateLogic();
	}

	private void RevLogic()
	{
		if (physGrabObject.grabbed)
		{
			revValue += Time.deltaTime / revUpDuration;
		}
		else
		{
			revValue -= Time.deltaTime / revDownDuration;
		}
		revValue = Mathf.Clamp01(revValue);
		revSmooth = Mathf.SmoothStep(0f, 1f, revValue);
	}

	private void PopcornParticleLogic()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		popcornEmission.rateOverTime = MinMaxCurve.op_Implicit(revSmooth * popcornRateMax);
		bool flag = revValue > 0f;
		if (flag && !popcornParticles.isPlaying)
		{
			popcornParticles.Play(true);
		}
		if (!flag && popcornParticles.isPlaying)
		{
			popcornParticles.Stop(true, (ParticleSystemStopBehavior)1);
		}
	}

	private void PopLoopSoundLogic()
	{
		bool playing = revValue > 0f;
		float pitchMultiplier = Mathf.Lerp(pitchMin, pitchMax, revSmooth);
		float volumeMultiplier = Mathf.Lerp(volumeMin, volumeMax, revSmooth);
		audioPopLoop.PlayLoop(playing, 2f, 2f, pitchMultiplier, volumeMultiplier);
	}

	private void LampTransitionLogic()
	{
		bool flag = revValue > 0f;
		if (flag && !wasPopping)
		{
			LampOn();
		}
		if (!flag && wasPopping)
		{
			LampOff();
		}
		wasPopping = flag;
	}

	private void LampOn()
	{
		lampIsOn = true;
		lightAnimTimer = 0f;
		flickerSoundTimer = 0f;
		audioLampOn.Play(((Component)this).transform);
	}

	private void LampOff()
	{
		lampIsOn = false;
		lightAnimTimer = 0f;
		flickerSoundTimer = 0f;
		audioLampOff.Play(((Component)this).transform);
	}

	private void LightAnimationLogic()
	{
		if (lightAnimTimer < lightAnimDuration)
		{
			lightAnimTimer += Time.deltaTime;
		}
		float num = Mathf.Clamp01(lightAnimTimer / lightAnimDuration);
		float num2 = lightOnCurve.Evaluate(num);
		if (!lampIsOn)
		{
			num2 = 1f - lightOffCurve.Evaluate(num);
		}
		for (int i = 0; i < popcornLights.Length; i++)
		{
			popcornLights[i].intensity = lightIntensityMax * num2;
			((Behaviour)popcornLights[i]).enabled = num2 > 0f;
		}
	}

	private void LightFlickerSoundLogic()
	{
		if (lightAnimTimer >= lightAnimDuration)
		{
			flickerSoundTimer = 0f;
			return;
		}
		float num = Mathf.Clamp01(lightAnimTimer / lightAnimDuration);
		float num2 = flickerFrequency * flickerCurve.Evaluate(num);
		if (num2 <= 0f)
		{
			flickerSoundTimer = 0f;
			return;
		}
		float num3 = 1f / num2;
		flickerSoundTimer -= Time.deltaTime;
		while (flickerSoundTimer <= 0f)
		{
			flickerSoundTimer += num3;
			audioLightFlicker.Play(((Component)this).transform);
		}
	}

	private void InvestigateLogic()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (revValue <= 0f)
			{
				investigateTimer = 0f;
				return;
			}
			if (investigateTimer > 0f)
			{
				investigateTimer -= Time.deltaTime;
				return;
			}
			investigateTimer = 1f;
			float range = Mathf.Lerp(investigateRangeMin, investigateRangeMax, revSmooth);
			SemiFunc.EnemyInvestigate(physGrabObject.centerPoint, range);
		}
	}
}
