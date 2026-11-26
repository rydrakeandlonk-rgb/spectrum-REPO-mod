using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class ItemToggle : MonoBehaviour
{
	[HideInInspector]
	public bool toggleState;

	public bool playSound;

	private bool fetchSound;

	internal bool toggleStatePrevious;

	private PhotonView photonView;

	private PhysGrabObject physGrabObject;

	private ItemEquippable itemEquippable;

	private Sound soundOn;

	private Sound soundOff;

	internal int playerTogglePhotonID;

	internal bool toggleImpulse;

	private float toggleImpulseTimer;

	internal bool disabled;

	public bool autoTurnOffWhenEquipped = true;

	public UnityEvent onToggle;

	private void Start()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
	}

	private void Update()
	{
		if (autoTurnOffWhenEquipped && SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)itemEquippable) && itemEquippable.isEquipped && toggleState)
		{
			ToggleItem(toggle: false);
		}
		if (playSound && !fetchSound)
		{
			soundOn = AssetManager.instance.soundDeviceTurnOn;
			soundOff = AssetManager.instance.soundDeviceTurnOff;
			fetchSound = true;
		}
		if (physGrabObject.heldByLocalPlayer && !disabled && PlayerController.instance.InputDisableTimer <= 0f && SemiFunc.InputDown(InputKey.Interact))
		{
			TutorialDirector.instance.playerUsedToggle = true;
			bool toggle = !toggleState;
			int player = SemiFunc.PhotonViewIDPlayerAvatarLocal();
			ToggleItem(toggle, player);
		}
		if (toggleImpulseTimer > 0f)
		{
			toggleImpulse = true;
			toggleImpulseTimer -= Time.deltaTime;
		}
		else
		{
			toggleImpulse = false;
		}
	}

	private void ToggleItemLogic(bool toggle, int player = -1)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		toggleStatePrevious = toggleState;
		toggleState = toggle;
		playerTogglePhotonID = player;
		onToggle.Invoke();
		if (playSound)
		{
			if (toggleState)
			{
				if (soundOn != null)
				{
					soundOn.Play(((Component)this).transform.position);
				}
			}
			else if (soundOff != null)
			{
				soundOff.Play(((Component)this).transform.position);
			}
		}
		toggleImpulseTimer = 0.2f;
	}

	public void ToggleItem(bool toggle, int player = -1)
	{
		if (GameManager.Multiplayer())
		{
			if (photonView.ViewID != 0)
			{
				photonView.RPC("ToggleItemRPC", (RpcTarget)0, new object[2] { toggle, player });
			}
		}
		else
		{
			ToggleItemLogic(toggle, player);
		}
	}

	[PunRPC]
	private void ToggleItemRPC(bool toggle, int player = -1, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		ToggleItemLogic(toggle, player);
	}

	public void ToggleDisable(bool _disable)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if (GameManager.Multiplayer())
		{
			photonView.RPC("ToggleDisableRPC", (RpcTarget)0, new object[1] { _disable });
		}
		else
		{
			ToggleDisableRPC(_disable);
		}
	}

	[PunRPC]
	private void ToggleDisableRPC(bool _disable, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			disabled = _disable;
		}
	}
}
