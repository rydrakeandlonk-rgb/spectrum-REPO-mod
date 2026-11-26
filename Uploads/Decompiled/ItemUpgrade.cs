using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class ItemUpgrade : MonoBehaviour
{
	public UnityEvent upgradeEvent;

	public bool isPlayerUpgrade;

	public ColorPresets colorPreset;

	internal Color beamColor;

	private Rigidbody rb;

	private Vector3 attachPoint;

	internal bool itemActivated;

	private PhotonView photonView;

	private PhysGrabObjectImpactDetector impactDetector;

	private ItemAttributes itemAttributes;

	private Transform particleEffects;

	private Transform onSwitchTransform;

	internal Vector3 connectionPoint;

	internal Transform lastPlayerToTouch;

	private PhysGrabObject physGrabObject;

	private PhysicsMaterial physicMaterialOriginal;

	private ItemToggle itemToggle;

	internal PlayerAvatar playerAvatarTarget;

	private Camera cameraMain;

	private bool upgradeDone;

	private bool pushedOrPulled;

	private ITargetingCondition customTargetingCondition;

	private void Awake()
	{
		itemAttributes = ((Component)this).GetComponent<ItemAttributes>();
		impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		customTargetingCondition = ((Component)this).GetComponent<ITargetingCondition>();
		particleEffects = ((Component)this).transform.Find("Particle Effects");
		cameraMain = Camera.main;
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		rb = ((Component)this).GetComponent<Rigidbody>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
	}

	private void Start()
	{
		Collider[] componentsInChildren = ((Component)this).GetComponentsInChildren<Collider>();
		int num = 0;
		if (num < componentsInChildren.Length)
		{
			Collider val = componentsInChildren[num];
			physicMaterialOriginal = val.material;
		}
		if (SemiFunc.RunIsShop())
		{
			((Behaviour)itemToggle).enabled = false;
		}
		physGrabObject.clientNonKinematic = true;
	}

	private void Update()
	{
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		if (physGrabObject.playerGrabbing.Count > 0)
		{
			bool flag = false;
			foreach (PhysGrabber item in physGrabObject.playerGrabbing)
			{
				if (item.isRotating)
				{
					flag = true;
				}
			}
			float dist = 0.5f;
			if (physGrabObject.grabbed)
			{
				if (physGrabObject.grabbedLocal && !pushedOrPulled)
				{
					PhysGrabber.instance.OverrideGrabDistance(dist);
				}
				if (PhysGrabber.instance.isPulling || PhysGrabber.instance.isPushing)
				{
					pushedOrPulled = true;
				}
			}
			else
			{
				pushedOrPulled = false;
			}
			if (!flag && !pushedOrPulled)
			{
				Quaternion turnX = Quaternion.Euler(45f, 0f, 0f);
				Quaternion turnY = Quaternion.Euler(45f, 180f, 0f);
				Quaternion identity = Quaternion.identity;
				physGrabObject.TurnXYZ(turnX, turnY, identity);
			}
		}
		else
		{
			pushedOrPulled = false;
		}
	}

	public void PlayerUpgrade()
	{
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		if (upgradeDone || !isPlayerUpgrade || !itemToggle.toggleState)
		{
			return;
		}
		Transform val = ((Component)this).transform.Find("Mesh");
		if (Object.op_Implicit((Object)(object)val))
		{
			((Renderer)((Component)val).GetComponent<MeshRenderer>()).enabled = false;
		}
		PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(itemToggle.playerTogglePhotonID);
		if (Object.op_Implicit((Object)(object)playerAvatar))
		{
			upgradeEvent.Invoke();
		}
		particleEffects.parent = null;
		((Component)particleEffects).gameObject.SetActive(true);
		if (Object.op_Implicit((Object)(object)playerAvatar) && playerAvatar.isLocal)
		{
			StatsUI.instance.Fetch();
			StatsUI.instance.ShowStats();
			CameraGlitch.Instance.PlayUpgrade();
		}
		else
		{
			GameDirector.instance.CameraImpact.ShakeDistance(5f, 1f, 6f, ((Component)this).transform.position, 0.2f);
		}
		if (Object.op_Implicit((Object)(object)playerAvatar) && (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient))
		{
			playerAvatar.playerHealth.MaterialEffectOverride(PlayerHealth.Effect.Upgrade);
		}
		if (StatsManager.instance.itemsPurchased.TryGetValue(((Object)itemAttributes.item).name, out var value))
		{
			value--;
			if (value > 0)
			{
				StatsManager.instance.itemsPurchased[((Object)itemAttributes.item).name] = value;
			}
			else
			{
				StatsManager.instance.itemsPurchased.Remove(((Object)itemAttributes.item).name);
			}
		}
		impactDetector.DestroyObject(effects: false);
		upgradeDone = true;
	}

	private void ButtonToggleLogic(bool activated)
	{
		itemActivated = activated;
	}

	public void ButtonToggle()
	{
		itemActivated = !itemActivated;
		if (GameManager.instance.gameMode == 0)
		{
			ButtonToggleLogic(itemActivated);
		}
		else if (PhotonNetwork.IsMasterClient)
		{
			photonView.RPC("ButtonToggleRPC", (RpcTarget)0, new object[1] { itemActivated });
		}
	}

	[PunRPC]
	private void ButtonToggleRPC(bool activated, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			ButtonToggleLogic(activated);
		}
	}
}
