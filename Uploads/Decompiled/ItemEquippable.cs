using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

public class ItemEquippable : MonoBehaviourPunCallbacks
{
	public enum ItemState
	{
		Idle,
		Equipping,
		Equipped,
		Unequipping
	}

	[FormerlySerializedAs("_currentState")]
	[SerializeField]
	private ItemState currentState;

	public Sprite ItemIcon;

	private InventorySpot equippedSpot;

	private int ownerPlayerId = -1;

	internal bool isEquipped;

	internal bool isEquippedPrev;

	internal float wasEquippedTimer;

	internal bool isUnequipping;

	private bool isForceUnequipping;

	internal bool isEquipping;

	private float isUnequippingTimer;

	private float isEquippingTimer;

	public LayerMask ObstructionLayers;

	public SemiFunc.emojiIcon itemEmojiIcon;

	internal string itemEmoji;

	public bool hasInventoryPing;

	public Sound pingSound;

	public bool playPingForAllPlayers = true;

	internal int inventorySpotIndex;

	internal float unequipTimer;

	internal float equipTimer;

	private float pingActiveTimer;

	private const float animationDuration = 0.4f;

	private PhysGrabObject physGrabObject;

	private bool stateStart = true;

	private float itemEquipCubeShowTimer;

	private Vector3 teleportPosition;

	private float teleportPositionForcedTimer;

	private float teleportOffset;

	private float forceGrabTimer;

	private bool wasForceGrabbed;

	private readonly List<PhysGrabber> releaseGrabbersRequested = new List<PhysGrabber>();

	internal PhysGrabber latestOwner;

	private Coroutine animateCoroutine;

	private Rigidbody rb => ((Component)this).GetComponent<Rigidbody>();

	private void Start()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		List<Collider> list = new List<Collider>(((Component)this).GetComponentsInChildren<Collider>());
		if (list.Count <= 0)
		{
			return;
		}
		Bounds bounds = list[0].bounds;
		Bounds val = new Bounds(bounds.center, Vector3.zero);
		foreach (Collider item in list)
		{
			val.Encapsulate(item.bounds);
		}
		Vector3 extents = val.extents;
		teleportOffset = extents.magnitude;
	}

	public bool IsEquipped()
	{
		return currentState == ItemState.Equipped;
	}

	public PlayerAvatar GetOwnerPlayerAvatar()
	{
		if (!SemiFunc.IsMultiplayer())
		{
			return PlayerAvatar.instance;
		}
		if (ownerPlayerId == -1)
		{
			return null;
		}
		PhotonView val = PhotonView.Find(ownerPlayerId);
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		return ((Component)val).GetComponent<PlayerAvatar>();
	}

	public void SendPing(float amount, float frequency, float time)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		if (IsEquipped() && !(pingActiveTimer > 0f))
		{
			pingActiveTimer = time;
			if (SemiFunc.IsMultiplayer())
			{
				((MonoBehaviourPun)this).photonView.RPC("RPC_SendPing", (RpcTarget)0, new object[3] { amount, frequency, time });
			}
			else
			{
				RPC_SendPing(amount, frequency, time);
			}
		}
	}

	[PunRPC]
	private void RPC_SendPing(float amount, float frequency, float time, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return;
		}
		bool flag = !SemiFunc.IsMultiplayer() || PhysGrabber.instance.photonView.ViewID == ownerPlayerId;
		if (flag && inventorySpotIndex != -1)
		{
			InventorySpot spotByIndex = Inventory.instance.GetSpotByIndex(inventorySpotIndex);
			if ((Object)(object)spotByIndex != (Object)null)
			{
				spotByIndex.Ping(amount, frequency, time);
			}
		}
		if (!hasInventoryPing || pingSound == null || pingSound.Sounds == null || pingSound.Sounds.Length == 0)
		{
			return;
		}
		PlayerAvatar ownerPlayerAvatar = GetOwnerPlayerAvatar();
		if (!((Object)(object)ownerPlayerAvatar == (Object)null))
		{
			if (playPingForAllPlayers)
			{
				pingSound.Play(ownerPlayerAvatar.playerAvatarVisuals.headLookAtTransform.position);
			}
			else if (flag)
			{
				pingSound.Play(ownerPlayerAvatar.playerAvatarVisuals.headLookAtTransform.position);
			}
		}
	}

	private bool CollisionCheck()
	{
		return false;
	}

	public void RequestEquip(int spot, int requestingPlayerId = -1)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		if (IsEquipped() || currentState == ItemState.Unequipping)
		{
			return;
		}
		if (SemiFunc.IsMultiplayer())
		{
			PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(requestingPlayerId);
			if (Object.op_Implicit((Object)(object)playerAvatar) && (PhotonNetwork.IsMasterClient || playerAvatar.photonView.IsMine))
			{
				((MonoBehaviourPun)this).photonView.RPC("RPC_RequestEquip", (RpcTarget)2, new object[2] { spot, requestingPlayerId });
			}
		}
		else
		{
			RPC_RequestEquip(spot, -1);
		}
	}

	[PunRPC]
	private void RPC_RequestEquip(int spotIndex, int physGrabberPhotonViewID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() || spotIndex < 0 || spotIndex >= Inventory.instance.GetAllSpots().Count)
		{
			return;
		}
		bool flag = SemiFunc.IsMultiplayer();
		if (flag)
		{
			PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(physGrabberPhotonViewID);
			if (!Object.op_Implicit((Object)(object)playerAvatar) || !SemiFunc.MasterAndOwnerOnlyRPC(_info, playerAvatar.photonView))
			{
				return;
			}
		}
		if (currentState == ItemState.Idle)
		{
			if (flag)
			{
				((MonoBehaviourPun)this).photonView.RPC("RPC_UpdateItemState", (RpcTarget)0, new object[3] { 2, spotIndex, physGrabberPhotonViewID });
			}
			else
			{
				RPC_UpdateItemState(2, spotIndex, physGrabberPhotonViewID);
			}
		}
	}

	[PunRPC]
	private void RPC_UpdateItemState(int state, int spotIndex, int ownerId, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		bool flag = SemiFunc.IsMultiplayer();
		PlayerAvatar playerAvatar = PlayerAvatar.instance;
		if (flag)
		{
			PhotonView val = PhotonView.Find(ownerId);
			playerAvatar = ((val != null) ? ((Component)val).GetComponent<PlayerAvatar>() : null);
		}
		if (!Object.op_Implicit((Object)(object)playerAvatar) || !SemiFunc.MasterAndOwnerOnlyRPC(_info, playerAvatar.photonView))
		{
			return;
		}
		InventorySpot inventorySpot = null;
		if (flag)
		{
			if (PhysGrabber.instance.photonView.ViewID == ownerId && spotIndex != -1)
			{
				inventorySpot = Inventory.instance.GetSpotByIndex(spotIndex);
			}
		}
		else if (spotIndex != -1)
		{
			inventorySpot = Inventory.instance.GetSpotByIndex(spotIndex);
		}
		bool flag2 = false;
		if ((Object)(object)inventorySpot == (Object)null)
		{
			flag2 = true;
		}
		if ((Object)(object)inventorySpot != (Object)null && inventorySpot.IsOccupied())
		{
			flag2 = true;
		}
		if (Inventory.instance.IsItemEquipped(this))
		{
			flag2 = true;
		}
		if (state == 2)
		{
			string instanceName = ((Component)this).GetComponent<ItemAttributes>().instanceName;
			StatsManager.instance.PlayerInventoryUpdate(playerAvatar.steamID, instanceName, spotIndex);
			currentState = ItemState.Equipped;
			if (!flag2)
			{
				equippedSpot = inventorySpot;
				equippedSpot?.EquipItem(this);
				if (Object.op_Implicit((Object)(object)playerAvatar) && (Object)(object)playerAvatar.physGrabber.overrideGrabTarget == (Object)(object)physGrabObject)
				{
					playerAvatar.physGrabber.OverrideGrab(null, 0f);
				}
			}
			else
			{
				equippedSpot = null;
			}
		}
		else
		{
			equippedSpot?.UnequipItem();
			equippedSpot = null;
		}
		inventorySpotIndex = spotIndex;
		currentState = (ItemState)state;
		ownerPlayerId = ownerId;
		stateStart = true;
		UpdateVisuals();
	}

	private void IsEquippingAndUnequippingTimer()
	{
		if (isEquippingTimer > 0f)
		{
			if (isEquippingTimer <= 0f)
			{
				isEquipping = false;
			}
			isEquippingTimer -= Time.deltaTime;
		}
		if (isUnequippingTimer > 0f)
		{
			if (isUnequippingTimer <= 0f)
			{
				isUnequipping = false;
			}
			isUnequippingTimer -= Time.deltaTime;
		}
	}

	public void RequestUnequip()
	{
		if (IsEquipped())
		{
			currentState = ItemState.Unequipping;
			if (SemiFunc.IsMultiplayer())
			{
				((MonoBehaviourPun)this).photonView.RPC("RPC_StartUnequip", (RpcTarget)0, new object[1] { ownerPlayerId });
			}
			else
			{
				RPC_StartUnequip(ownerPlayerId);
			}
		}
	}

	[PunRPC]
	private void RPC_StartUnequip(int requestingPlayerId)
	{
		if (ownerPlayerId == requestingPlayerId && (!SemiFunc.IsMultiplayer() || PhysGrabber.instance.photonView.ViewID == ownerPlayerId))
		{
			PerformUnequip(requestingPlayerId);
		}
	}

	private void PerformUnequip(int requestingPlayerId)
	{
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		isForceUnequipping = false;
		unequipTimer = 0.4f;
		SetRotation();
		currentState = ItemState.Unequipping;
		physGrabObject.OverrideDeactivateReset();
		if (Object.op_Implicit((Object)(object)Camera.main))
		{
			RayHitTestNew(((Component)Camera.main).transform, 1f);
		}
		if (Object.op_Implicit((Object)(object)PhysGrabber.instance))
		{
			PhysGrabber.instance.pullerDistance = PhysGrabber.instance.minDistanceFromPlayer;
		}
		if (SemiFunc.IsMultiplayer())
		{
			((MonoBehaviourPun)this).photonView.RPC("RPC_CompleteUnequip", (RpcTarget)2, new object[3] { requestingPlayerId, teleportPosition, false });
		}
		else
		{
			RPC_CompleteUnequip(requestingPlayerId, teleportPosition, isForceUnequip: false);
		}
	}

	private bool RayHitTestNew(Transform _transform, float distance)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)_transform))
		{
			int num = LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()) & ~LayerMask.GetMask(new string[2] { "Ignore Raycast", "CollisionCheck" });
			RaycastHit val = default;
			if (Physics.Raycast(_transform.position, _transform.forward, ref val, distance, num))
			{
				teleportPosition = val.point + val.normal * teleportOffset;
			}
			else
			{
				teleportPosition = _transform.position + _transform.forward * distance;
			}
		}
		return CollisionCheck();
	}

	private bool RayHitTest(float distance)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		int num = LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()) & ~LayerMask.GetMask(new string[2] { "Ignore Raycast", "CollisionCheck" });
		if (Object.op_Implicit((Object)(object)Camera.main))
		{
			RaycastHit val = default;
			Physics.Raycast(((Component)Camera.main).transform.position, ((Component)Camera.main).transform.forward, ref val, distance, num);
		}
		return CollisionCheck();
	}

	private Vector3 GetUnequipPosition()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return ((Component)this).transform.position;
	}

	[PunRPC]
	private void RPC_CompleteUnequip(int physGrabberPhotonViewID, Vector3 teleportPos, bool isForceUnequip, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		PhysGrabber physGrabber;
		if (SemiFunc.IsMultiplayer())
		{
			PhotonView val = PhotonView.Find(physGrabberPhotonViewID);
			physGrabber = ((val != null) ? ((Component)val).GetComponent<PhysGrabber>() : null);
		}
		else
		{
			physGrabber = PhysGrabber.instance;
		}
		if (Object.op_Implicit((Object)(object)physGrabber) && SemiFunc.OwnerOnlyRPC(_info, physGrabber.photonView))
		{
			isForceUnequipping = isForceUnequip;
			StatsManager.instance.PlayerInventoryUpdate(physGrabber.playerAvatar.steamID, "", inventorySpotIndex);
			if (isForceUnequip)
			{
				physGrabObject.Teleport(teleportPos, Quaternion.identity);
			}
			else
			{
				Transform visionTransform = physGrabber.playerAvatar.PlayerVisionTarget.VisionTransform;
				physGrabObject.Teleport(teleportPos, Quaternion.LookRotation(((Component)visionTransform).transform.forward, Vector3.up));
			}
			rb.isKinematic = false;
			int num = (((Object)(object)equippedSpot != (Object)null) ? equippedSpot.inventorySpotIndex : (-1));
			equippedSpot?.UnequipItem();
			equippedSpot = null;
			ownerPlayerId = -1;
			if (SemiFunc.IsMultiplayer())
			{
				((MonoBehaviourPun)this).photonView.RPC("RPC_UpdateItemState", (RpcTarget)0, new object[3] { 3, num, physGrabberPhotonViewID });
			}
			else
			{
				RPC_UpdateItemState(3, num, -1);
			}
			teleportPosition = teleportPos;
			teleportPositionForcedTimer = 0.1f;
		}
	}

	private void UpdateVisuals()
	{
		if (currentState == ItemState.Equipped)
		{
			SetItemActive(isActive: false);
			return;
		}
		if (currentState == ItemState.Idle)
		{
			SetItemActive(isActive: true);
			return;
		}
		_ = currentState;
		_ = 3;
	}

	private void SetItemActive(bool isActive)
	{
	}

	private IEnumerator AnimateUnequip()
	{
		float duration = 0.2f;
		float elapsed = 0f;
		Vector3 originalScale = ((Component)this).transform.localScale;
		Vector3 targetScale = Vector3.one;
		List<Collider> colliders = new List<Collider>(((Component)this).GetComponentsInChildren<Collider>());
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			physGrabObject.OverrideMass(0.1f, 1f);
			physGrabObject.impactDetector.PlayerHitDisableSet();
		}
		isUnequipping = true;
		isUnequippingTimer = 0.2f;
		Collider _unequipCollider = null;
		bool _hasUnequipCollider = false;
		foreach (Collider item in colliders)
		{
			PhysGrabObjectBoxCollider component = ((Component)item).GetComponent<PhysGrabObjectBoxCollider>();
			if (Object.op_Implicit((Object)(object)component) && component.unEquipCollider)
			{
				item.enabled = true;
				_hasUnequipCollider = true;
				_unequipCollider = item;
				colliders.Remove(item);
				break;
			}
		}
		if (_hasUnequipCollider)
		{
			foreach (Collider item2 in colliders)
			{
				item2.enabled = false;
			}
		}
		else
		{
			foreach (Collider item3 in colliders)
			{
				item3.enabled = true;
			}
		}
		while (elapsed < duration)
		{
			float num = elapsed / duration;
			((Component)this).transform.localScale = Vector3.Lerp(originalScale, targetScale, num);
			elapsed += Time.deltaTime;
			yield return null;
		}
		if (_hasUnequipCollider)
		{
			_unequipCollider.enabled = false;
			foreach (Collider item4 in colliders)
			{
				item4.enabled = true;
			}
		}
		isUnequipping = false;
		isUnequippingTimer = 0f;
		((Component)this).transform.localScale = targetScale;
		wasForceGrabbed = false;
		if (!isForceUnequipping)
		{
			forceGrabTimer = 1.5f;
		}
		isForceUnequipping = false;
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			physGrabObject.OverrideMass(0.25f);
			while (physGrabObject.rb.mass < physGrabObject.massOriginal)
			{
				physGrabObject.OverrideMass(physGrabObject.rb.mass + 5f * Time.deltaTime);
				yield return null;
			}
		}
	}

	private void ForceGrab()
	{
		if (currentState != ItemState.Idle)
		{
			forceGrabTimer = 0f;
			return;
		}
		bool flag = PhysGrabber.instance.photonView.ViewID == ownerPlayerId;
		if (!(forceGrabTimer > 0f))
		{
			return;
		}
		if (!SemiFunc.IsMultiplayer() | flag)
		{
			PhysGrabber.instance.OverrideGrab(physGrabObject);
			if (SemiFunc.InputUp(InputKey.Grab))
			{
				forceGrabTimer = 0f;
			}
			else if ((Object)(object)PhysGrabber.instance.grabbedPhysGrabObject == (Object)(object)physGrabObject)
			{
				wasForceGrabbed = true;
			}
			else if (wasForceGrabbed)
			{
				forceGrabTimer = 0f;
			}
		}
		forceGrabTimer -= Time.deltaTime;
	}

	private IEnumerator AnimateEquip()
	{
		float duration = 0.1f;
		float elapsed = 0f;
		Vector3 originalScale = ((Component)this).transform.localScale;
		Vector3 targetScale = originalScale * 0.01f;
		List<Collider> list = new List<Collider>(((Component)this).GetComponentsInChildren<Collider>());
		isEquipping = true;
		isEquippingTimer = 0.2f;
		Bounds val = new Bounds(((Component)this).transform.position, Vector3.zero);
		foreach (Collider item in list)
		{
			val.Encapsulate(item.bounds);
		}
		SemiFunc.AwakeRigidbodyBox(val.center, Quaternion.identity, val.size);
		foreach (Collider item2 in list)
		{
			item2.enabled = false;
		}
		while (elapsed < duration)
		{
			float num = elapsed / duration;
			((Component)this).transform.localScale = Vector3.Lerp(originalScale, targetScale, num);
			elapsed += Time.deltaTime;
			yield return null;
		}
		isEquipping = false;
		isEquippingTimer = 0f;
		((Component)this).transform.localScale = targetScale;
	}

	public void ForceUnequip(Vector3 dropPosition, int physGrabberPhotonViewID)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != ItemState.Idle)
		{
			isForceUnequipping = true;
			unequipTimer = 0.4f;
			currentState = ItemState.Unequipping;
			physGrabObject.OverrideDeactivateReset();
			dropPosition += Random.insideUnitSphere * 0.2f;
			teleportPosition = dropPosition;
			if (SemiFunc.IsMultiplayer())
			{
				((MonoBehaviourPun)this).photonView.RPC("RPC_CompleteUnequip", (RpcTarget)2, new object[3] { physGrabberPhotonViewID, teleportPosition, true });
			}
			else
			{
				RPC_CompleteUnequip(physGrabberPhotonViewID, teleportPosition, isForceUnequip: true);
			}
		}
	}

	private void WasEquippedTimer()
	{
		if (isEquippedPrev != isEquipped)
		{
			wasEquippedTimer = 0.5f;
			isEquippedPrev = isEquipped;
		}
		if (wasEquippedTimer > 0f)
		{
			wasEquippedTimer -= Time.deltaTime;
		}
	}

	private void Update()
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && teleportPositionForcedTimer > 0f)
		{
			teleportPositionForcedTimer -= Time.deltaTime;
			if (!rb.isKinematic)
			{
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
			physGrabObject.Teleport(teleportPosition, ((Component)physGrabObject).transform.rotation);
		}
		if (SemiFunc.RunIsArena())
		{
			return;
		}
		if (pingActiveTimer > 0f)
		{
			pingActiveTimer -= Time.deltaTime;
		}
		ForceGrab();
		WasEquippedTimer();
		IsEquippingAndUnequippingTimer();
		switch (currentState)
		{
		case ItemState.Idle:
			StateIdle();
			break;
		case ItemState.Equipping:
			StateEquipping();
			break;
		case ItemState.Equipped:
			StateEquipped();
			break;
		case ItemState.Unequipping:
			StateUnequipping();
			break;
		}
		if (unequipTimer > 0f)
		{
			unequipTimer -= Time.deltaTime;
		}
		if (equipTimer > 0f)
		{
			equipTimer -= Time.deltaTime;
		}
		if (itemEquipCubeShowTimer > 0f)
		{
			itemEquipCubeShowTimer -= Time.deltaTime;
			if (itemEquipCubeShowTimer <= 0f)
			{
				Vector3 localScale = ((Component)this).transform.localScale;
				((Component)this).transform.localScale = Vector3.one;
				((Component)this).transform.localScale = localScale;
			}
		}
	}

	private void StateIdleStart()
	{
		if (stateStart)
		{
			stateStart = false;
		}
	}

	private void StateIdle()
	{
		if (currentState == ItemState.Idle)
		{
			StateIdleStart();
			isEquipped = false;
		}
	}

	private void StateEquippingStart()
	{
		if (stateStart)
		{
			stateStart = false;
		}
	}

	private void StateEquipping()
	{
		if (currentState == ItemState.Equipping)
		{
			StateEquippingStart();
			currentState = ItemState.Equipped;
			isEquipped = true;
		}
	}

	private void StateEquippedStart()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			releaseGrabbersRequested.Clear();
			AssetManager.instance.soundEquip.Play(physGrabObject.midPoint);
			if (animateCoroutine != null)
			{
				((MonoBehaviour)this).StopCoroutine(animateCoroutine);
				isUnequipping = false;
				isUnequippingTimer = 0f;
			}
			animateCoroutine = ((MonoBehaviour)this).StartCoroutine(AnimateEquip());
		}
	}

	private void StateEquipped()
	{
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != ItemState.Equipped)
		{
			return;
		}
		StateEquippedStart();
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			List<PhysGrabber> playerGrabbing = physGrabObject.playerGrabbing;
			for (int num = releaseGrabbersRequested.Count - 1; num >= 0; num--)
			{
				if (!Object.op_Implicit((Object)(object)releaseGrabbersRequested[num]) || !playerGrabbing.Contains(releaseGrabbersRequested[num]))
				{
					releaseGrabbersRequested.RemoveAt(num);
				}
			}
			foreach (PhysGrabber item in playerGrabbing.ToList())
			{
				if (!releaseGrabbersRequested.Contains(item))
				{
					releaseGrabbersRequested.Add(item);
					item.OverrideGrabRelease(((MonoBehaviourPun)this).photonView.ViewID);
				}
			}
		}
		if (!isEquipped)
		{
			equipTimer = 0.5f;
		}
		isEquipped = true;
		Vector3 localScale = ((Component)physGrabObject).transform.localScale;
		if (localScale.magnitude < 0.1f)
		{
			physGrabObject.OverrideDeactivate();
		}
	}

	private void StateUnequippingStart()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			AssetManager.instance.soundUnequip.Play(physGrabObject.midPoint);
			if (animateCoroutine != null)
			{
				((MonoBehaviour)this).StopCoroutine(animateCoroutine);
				isEquipping = false;
				isEquippingTimer = 0f;
			}
			animateCoroutine = ((MonoBehaviour)this).StartCoroutine(AnimateUnequip());
		}
	}

	private void StateUnequipping()
	{
		if (currentState == ItemState.Unequipping)
		{
			StateUnequippingStart();
			currentState = ItemState.Idle;
			isEquipped = false;
		}
	}

	private void SetRotation()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)Camera.main))
		{
			((Component)physGrabObject).transform.rotation = Quaternion.LookRotation(((Component)Camera.main).transform.forward, Vector3.up);
		}
		physGrabObject.rb.rotation = ((Component)physGrabObject).transform.rotation;
	}

	private void OnDestroy()
	{
		if (Object.op_Implicit((Object)(object)equippedSpot))
		{
			equippedSpot.UnequipItem();
		}
	}
}
