using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ValuableTray : Trap
{
	public List<Transform> trayThings = new List<Transform>();

	public List<ParticleSystem> trayParticles = new List<ParticleSystem>();

	public List<Collider> trayColliders = new List<Collider>();

	private float dropThreshold = 1.5f;

	private float dropTimer;

	private int dropped = -1;

	private int previousDropped;

	protected override void Start()
	{
		base.Start();
		dropTimer = dropThreshold;
		previousDropped = dropped;
	}

	protected override void Update()
	{
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (dropped >= trayThings.Count)
		{
			return;
		}
		if (dropped != previousDropped && dropped < trayThings.Count)
		{
			((Component)trayThings[dropped]).gameObject.SetActive(false);
			trayParticles[dropped].Play();
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				physGrabObject.impactDetector.BreakMedium(physGrabObject.centerPoint, _forceBreak: true);
			}
			previousDropped = dropped;
			if (dropped == 0)
			{
				((Component)trayColliders[0]).gameObject.SetActive(false);
			}
			else if (dropped == 1)
			{
				((Component)trayColliders[1]).gameObject.SetActive(false);
			}
			else if (dropped == 3)
			{
				((Component)trayColliders[2]).gameObject.SetActive(false);
			}
			else if (dropped == 5)
			{
				((Component)trayColliders[3]).gameObject.SetActive(false);
			}
			else if (dropped == 7)
			{
				((Component)trayColliders[4]).gameObject.SetActive(false);
			}
		}
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			float num = Vector3.Dot(((Component)this).transform.up, Vector3.up);
			dropThreshold = GetDropThreshold(num);
			dropTimer += Time.deltaTime;
			if (num < 0.85f && dropTimer >= dropThreshold)
			{
				DropThing();
			}
		}
	}

	private float GetDropThreshold(float tilt)
	{
		if (tilt > 0.85f)
		{
			return dropThreshold;
		}
		if (tilt > 0.8f)
		{
			return 1f;
		}
		if (tilt > 0.5f)
		{
			return 0.5f;
		}
		if (tilt > 0.3f)
		{
			return 0.4f;
		}
		return 0.1f;
	}

	private void DropThing()
	{
		if (dropped < trayThings.Count)
		{
			dropTimer = 0f;
			UpdateDropAmount(dropped + 1);
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				physGrabObject.OverrideMass(4f - (float)dropped * 0.25f);
				physGrabObject.massOriginal = 4f - (float)dropped * 0.25f;
			}
		}
	}

	[PunRPC]
	public void UpdateDropAmountRPC(int _index, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			dropped = _index;
		}
	}

	private void UpdateDropAmount(int _index)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && dropped < trayThings.Count && dropped != _index)
		{
			if (!SemiFunc.IsMultiplayer())
			{
				UpdateDropAmountRPC(_index);
				return;
			}
			photonView.RPC("UpdateDropAmountRPC", (RpcTarget)0, new object[1] { _index });
		}
	}
}
