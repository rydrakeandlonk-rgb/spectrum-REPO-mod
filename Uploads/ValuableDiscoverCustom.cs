using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ValuableDiscoverCustom : MonoBehaviour
{
	private PhotonView photonView;

	internal PhysGrabObject physGrabObject;

	internal bool discovered;

	public float waitTime = 1f;

	[Space]
	public Color colorCorner;

	public Color colorMiddle;

	[Space]
	public bool valuableTrackerTracked;

	[Space]
	public bool hasCustomSound;

	public Sound customSound;

	[Space]
	public bool hasCustomMap;

	public List<MapCustom> customMap;

	private void Awake()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		foreach (MapCustom item in customMap)
		{
			((Behaviour)item).enabled = false;
		}
	}

	public bool Discover()
	{
		bool result = false;
		if (!discovered)
		{
			result = true;
			if (!GameManager.Multiplayer())
			{
				DiscoverRPC();
			}
			else
			{
				photonView.RPC("DiscoverRPC", (RpcTarget)0, Array.Empty<object>());
			}
		}
		ValuableDiscover.instance.New(physGrabObject, ValuableDiscoverGraphic.State.Custom, this);
		return result;
	}

	[PunRPC]
	private void DiscoverRPC()
	{
		discovered = true;
		foreach (MapCustom item in customMap)
		{
			((Behaviour)item).enabled = true;
			item.Add();
		}
	}
}
