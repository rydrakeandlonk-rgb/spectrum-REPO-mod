using System;
using Photon.Pun;
using UnityEngine;

public class ItemHealthPack : MonoBehaviour
{
	public int healAmount;

	private ItemToggle itemToggle;

	private ItemEquippable itemEquippable;

	private ItemAttributes itemAttributes;

	private PhotonView photonView;

	private PhysGrabObject physGrabObject;

	private NotValuableObject notValuableObject;

	[Space]
	public ParticleSystem[] particles;

	public ParticleSystem[] rejectParticles;

	[Space]
	public PropLight propLight;

	public AnimationCurve lightIntensityCurve;

	private float lightIntensityLerp;

	public MeshRenderer mesh;

	private Material material;

	private Color materialEmissionOriginal;

	private int materialPropertyEmission = Shader.PropertyToID("_EmissionColor");

	[Space]
	public Sound soundUse;

	public Sound soundReject;

	private bool used;

	private void Start()
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		itemAttributes = ((Component)this).GetComponent<ItemAttributes>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		material = ((Renderer)mesh).material;
		materialEmissionOriginal = material.GetColor(materialPropertyEmission);
		notValuableObject = ((Component)this).GetComponent<NotValuableObject>();
		notValuableObject.hasHealth = false;
	}

	private void Update()
	{
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.RunIsShop() || RunManager.instance.levelIsShop)
		{
			return;
		}
		LightLogic();
		if (!SemiFunc.IsMasterClientOrSingleplayer() || !itemToggle.toggleState || used)
		{
			return;
		}
		PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(itemToggle.playerTogglePhotonID);
		if (!Object.op_Implicit((Object)(object)playerAvatar))
		{
			return;
		}
		if (playerAvatar.playerHealth.health >= playerAvatar.playerHealth.maxHealth)
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("RejectRPC", (RpcTarget)0, Array.Empty<object>());
			}
			else
			{
				RejectRPC();
			}
			itemToggle.ToggleItem(toggle: false);
			physGrabObject.rb.AddForce(Vector3.up * 2f, (ForceMode)1);
			physGrabObject.rb.AddTorque(-((Component)physGrabObject).transform.right * 0.05f, (ForceMode)1);
		}
		else
		{
			playerAvatar.playerHealth.HealOther(healAmount, effect: true);
			StatsManager.instance.ItemRemove(itemAttributes.instanceName);
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("UsedRPC", (RpcTarget)0, Array.Empty<object>());
			}
			else
			{
				UsedRPC();
			}
			itemToggle.ToggleDisable(_disable: true);
		}
	}

	private void LightLogic()
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		if (used && lightIntensityLerp < 1f)
		{
			lightIntensityLerp += 1f * Time.deltaTime;
			propLight.lightComponent.intensity = lightIntensityCurve.Evaluate(lightIntensityLerp);
			propLight.originalIntensity = propLight.lightComponent.intensity;
			material.SetColor(materialPropertyEmission, Color.Lerp(Color.black, materialEmissionOriginal, lightIntensityCurve.Evaluate(lightIntensityLerp)));
		}
	}

	[PunRPC]
	private void UsedRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			notValuableObject.hasHealth = true;
			GameDirector.instance.CameraImpact.ShakeDistance(5f, 1f, 6f, ((Component)this).transform.position, 0.2f);
			itemAttributes.DisableUI(_disable: true);
			Object.Destroy((Object)(object)itemEquippable);
			ParticleSystem[] array = particles;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Play();
			}
			soundUse.Play(((Component)this).transform.position);
			used = true;
		}
	}

	[PunRPC]
	private void RejectRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return;
		}
		PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(itemToggle.playerTogglePhotonID);
		if (Object.op_Implicit((Object)(object)playerAvatar) && playerAvatar.isLocal)
		{
			if (SemiFunc.IsMultiplayer())
			{
				playerAvatar.physGrabber.OverrideGrabRelease(photonView.ViewID, 1f);
			}
			else
			{
				playerAvatar.physGrabber.ReleaseObject(photonView.ViewID, 1f);
			}
		}
		ParticleSystem[] array = rejectParticles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Play();
		}
		GameDirector.instance.CameraImpact.ShakeDistance(5f, 1f, 6f, ((Component)this).transform.position, 0.2f);
		soundReject.Play(((Component)this).transform.position);
	}

	public void OnDestroy()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		ParticleSystem[] array = particles;
		foreach (ParticleSystem val in array)
		{
			if (Object.op_Implicit((Object)(object)val) && val.isPlaying)
			{
				((Component)val).transform.SetParent((Transform)null);
				MainModule main = val.main;
				main.stopAction = (ParticleSystemStopAction)2;
			}
		}
		array = rejectParticles;
		foreach (ParticleSystem val2 in array)
		{
			if (Object.op_Implicit((Object)(object)val2) && val2.isPlaying)
			{
				((Component)val2).transform.SetParent((Transform)null);
				MainModule main2 = val2.main;
				main2.stopAction = (ParticleSystemStopAction)2;
			}
		}
	}
}
