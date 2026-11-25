using System;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class ValuableCauldronBox : MonoBehaviour
{
	private PhysGrabObjectImpactDetector impactDetector;

	private PhysGrabObject physGrabObject;

	public GameObject box;

	public GameObject cauldron;

	public ParticleSystem cardboardParticles;

	public ParticleSystem bitParticles;

	public Sound boxBreakSound;

	public PhysAudio cauldronPhysAudio;

	private bool trapTriggered;

	private PhotonView photonView;

	private ValuableObject valuableObject;

	private void Start()
	{
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		valuableObject = ((Component)this).GetComponent<ValuableObject>();
		impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		cauldron.SetActive(false);
	}

	public void TrapActivate()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		if (!trapTriggered && SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("TrapActivateRPC", (RpcTarget)0, Array.Empty<object>());
			}
			else
			{
				TrapActivateRPC();
			}
		}
	}

	[PunRPC]
	private void TrapActivateRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && !trapTriggered)
		{
			GrabRelease();
			boxBreakSound.Play(physGrabObject.centerPoint);
			cardboardParticles.Play(true);
			bitParticles.Play(true);
			Object.Destroy((Object)(object)box.gameObject);
			cauldron.SetActive(true);
			trapTriggered = true;
			impactDetector.ChangePhysAudio(cauldronPhysAudio);
		}
	}

	public void GrabRelease()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		bool flag = false;
		foreach (PhysGrabber item in physGrabObject.playerGrabbing.ToList())
		{
			if (!SemiFunc.IsMultiplayer())
			{
				item.ReleaseObject(photonView.ViewID);
			}
			else
			{
				item.OverrideGrabRelease(photonView.ViewID, 0.1f);
			}
			flag = true;
		}
		if (flag)
		{
			if (GameManager.instance.gameMode == 0)
			{
				GrabReleaseRPC();
			}
			else
			{
				photonView.RPC("GrabReleaseRPC", (RpcTarget)0, Array.Empty<object>());
			}
		}
	}

	[PunRPC]
	private void GrabReleaseRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			GameDirector.instance.CameraShake.ShakeDistance(5f, 3f, 8f, ((Component)this).transform.position, 0.5f);
			GameDirector.instance.CameraImpact.ShakeDistance(5f, 3f, 8f, ((Component)this).transform.position, 0.1f);
			physGrabObject.grabDisableTimer = 1f;
		}
	}
}
