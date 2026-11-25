using UnityEngine;

public class ValuableDesertSnowGlobe : MonoBehaviour
{
	public ParticleSystem snowParticles;

	public float shakeRotationWeight = 0.5f;

	public float shakeSpeedMin = 1.5f;

	public float shakeSpeedMax = 8f;

	public float shakeRampSpeed = 4f;

	public float settleTime = 3f;

	public float noisePositionMax = 1.2f;

	public float noisePositionSettled = 0.01f;

	private FakeVelocities fakeVelocities;

	private NoiseModule noiseModule;

	private float shakeIntensity;

	private float spawnGraceTimer = 1f;

	private bool particlesPlaying;

	private void Start()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		fakeVelocities = ((Component)this).GetComponent<FakeVelocities>();
		noiseModule = snowParticles.noise;
		noiseModule.enabled = true;
		noiseModule.positionAmount = MinMaxCurve.op_Implicit(noisePositionSettled);
		snowParticles.Stop();
		snowParticles.Clear();
		particlesPlaying = false;
	}

	private void Update()
	{
		SpawnGraceTick();
		ShakeDetectionLogic();
		SettleTick();
		ApplyNoiseLogic();
		ParticleLogic();
	}

	private void SpawnGraceTick()
	{
		if (spawnGraceTimer > 0f)
		{
			spawnGraceTimer -= Time.deltaTime;
		}
	}

	private void ShakeDetectionLogic()
	{
		if (!(spawnGraceTimer > 0f))
		{
			float num = fakeVelocities.velocity.magnitude + fakeVelocities.angularVelocity.magnitude * shakeRotationWeight;
			float num2 = Mathf.InverseLerp(shakeSpeedMin, shakeSpeedMax, num);
			if (num2 > shakeIntensity)
			{
				shakeIntensity = Mathf.MoveTowards(shakeIntensity, num2, shakeRampSpeed * Time.deltaTime);
			}
		}
	}

	private void SettleTick()
	{
		if (shakeIntensity > 0f)
		{
			shakeIntensity -= Time.deltaTime / settleTime;
		}
		if (shakeIntensity < 0f)
		{
			shakeIntensity = 0f;
		}
	}

	private void ApplyNoiseLogic()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		noiseModule.positionAmount = MinMaxCurve.op_Implicit(Mathf.Lerp(noisePositionSettled, noisePositionMax, shakeIntensity));
	}

	private void ParticleLogic()
	{
		if (shakeIntensity > 0f)
		{
			if (!particlesPlaying)
			{
				snowParticles.Play();
				particlesPlaying = true;
			}
		}
		else if (particlesPlaying)
		{
			snowParticles.Stop();
			particlesPlaying = false;
		}
	}
}
