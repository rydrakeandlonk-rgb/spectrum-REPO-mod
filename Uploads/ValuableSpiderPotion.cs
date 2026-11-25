using System.Collections.Generic;
using UnityEngine;

public class ValuableSpiderPotion : MonoBehaviour
{
	public ParticleSystem destroyParticles;

	public ParticleSystem destroyParticlesArachnophobia;

	[Space]
	public GameObject particlesOnOtherPlayers;

	private PhysGrabObject physgrabObject;

	[Space]
	public PhysAudio physAudio;

	public PhysAudio physAudioArachnophobia;

	private List<PlayerAvatar> allPlayers;

	private int playerCount;

	private float activationRadius = 3f;

	private Vector3 valuablePosition;

	private List<PlayerAvatar> affectedPlayers;

	[Header("Screen Spider Effect")]
	public GameObject screenSpiderEffect;

	private void Awake()
	{
		physgrabObject = ((Component)this).GetComponent<PhysGrabObject>();
	}

	public void DestroyParticles()
	{
		if (SemiFunc.Arachnophobia())
		{
			((Component)destroyParticlesArachnophobia).gameObject.transform.parent = null;
			destroyParticlesArachnophobia.Play();
		}
		else
		{
			((Component)destroyParticles).gameObject.transform.parent = null;
			destroyParticles.Play();
		}
		ActivateScreenSpiderEffect();
	}

	private void ActivateScreenSpiderEffect()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		allPlayers = SemiFunc.PlayerGetList();
		valuablePosition = ((Component)this).transform.position;
		affectedPlayers = new List<PlayerAvatar>();
		for (int i = 0; i < allPlayers.Count; i++)
		{
			if (!(Vector3.Distance(valuablePosition, ((Component)allPlayers[i]).transform.position) > activationRadius))
			{
				affectedPlayers.Add(allPlayers[i]);
			}
		}
		if (affectedPlayers.Contains(PlayerAvatar.instance))
		{
			CameraGlitch.Instance.PlayLong();
			Transform transform = ((Component)PlayerAvatar.instance.localCamera).transform;
			GameObject val = Object.Instantiate<GameObject>(screenSpiderEffect, transform.position, Quaternion.identity, transform);
			val.transform.localPosition = Vector3.zero;
			val.transform.localRotation = Quaternion.identity;
			val.GetComponent<ScreenSpiderEffect>().playerAvatar = PlayerAvatar.instance;
		}
		SpawnParticlesOnAffectedPlayers();
	}

	private void SpawnParticlesOnAffectedPlayers()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < affectedPlayers.Count; i++)
		{
			if (!affectedPlayers[i].isLocal || affectedPlayers[i].localCamera.GetOverrideActive())
			{
				Object.Instantiate<GameObject>(particlesOnOtherPlayers, ((Component)affectedPlayers[i]).transform.position, Quaternion.identity).GetComponent<SpiderSoundLoop>().playerAvatar = affectedPlayers[i];
			}
		}
	}

	public void ArachnophobiaActive()
	{
		physgrabObject.impactDetector.impactAudio = physAudioArachnophobia;
	}

	public void ArachnophobiaInactive()
	{
		physgrabObject.impactDetector.impactAudio = physAudio;
	}
}
