using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ItemAttributes : MonoBehaviour
{
	private enum IconState
	{
		Unstarted,
		Waiting,
		Complete
	}

	private static readonly HashSet<ItemAttributes> pendingIcons = new HashSet<ItemAttributes>();

	private static int iconWorkFrame = -1;

	private static double iconWorkTime;

	private IconState iconState;

	private Coroutine iconCoroutine;

	private Task bakedIconLoad;

	private Sprite generatedIcon;

	private bool iconShared;

	private static readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

	private Texture2D generatedIconTexture;

	private PhotonView photonView;

	public Item item;

	public Vector3 costOffset;

	internal SemiFunc.emojiIcon emojiIcon;

	internal ColorPresets colorPreset;

	internal int value;

	internal RoomVolumeCheck roomVolumeCheck;

	private float inStartRoomCheckTimer;

	internal bool blockExtraction;

	private ItemEquippable itemEquippable;

	private Transform itemVolume;

	internal bool shopItem;

	private PhysGrabObject physGrabObject;

	internal bool disableUI;

	internal string itemName;

	internal string instanceName;

	internal float showInfoTimer;

	internal bool hasIcon;

	public Sprite icon;

	private SemiFunc.itemType itemType;

	private ItemToggle itemToggle;

	private float isHeldTimer;

	private string itemTag = "";

	private string promptName = "";

	private string itemAssetName = "";

	private float itemValueMin;

	private float itemValueMax;

	private string cachedItemName;

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void RuntimeInitializeOnLoad()
	{
		pendingIcons.Clear();
		iconWorkFrame = -1;
		iconWorkTime = 0.0;
	}

	internal static bool IconsReady(int _sceneHandle)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		foreach (ItemAttributes pendingIcon in pendingIcons)
		{
			if (Object.op_Implicit((Object)(object)pendingIcon) && ((Component)pendingIcon).gameObject.activeInHierarchy)
			{
				Scene scene = ((Component)pendingIcon).gameObject.scene;
				if (scene.handle == SceneHandle.op_Implicit(_sceneHandle))
				{
					return false;
				}
			}
		}
		return true;
	}

	private static bool IconWorkReady()
	{
		if (iconWorkFrame != Time.frameCount)
		{
			iconWorkFrame = Time.frameCount;
			iconWorkTime = 0.0;
		}
		return iconWorkTime < 0.002;
	}

	private void OnEnable()
	{
		if (iconState != IconState.Complete)
		{
			hasIcon = false;
			pendingIcons.Add(this);
			if (iconState == IconState.Waiting && iconCoroutine == null)
			{
				iconCoroutine = ((MonoBehaviour)this).StartCoroutine(GenerateIcon());
			}
		}
	}

	private void OnDisable()
	{
		if (!((Component)this).gameObject.activeInHierarchy || iconState == IconState.Unstarted)
		{
			if (iconCoroutine != null)
			{
				((MonoBehaviour)this).StopCoroutine(iconCoroutine);
			}
			iconCoroutine = null;
			pendingIcons.Remove(this);
			hasIcon = true;
		}
	}

	private void OnValidate()
	{
		if (!SemiFunc.OnValidateCheck())
		{
			_ = ((Behaviour)this).enabled;
		}
	}

	private void Awake()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		if (Object.op_Implicit((Object)(object)item))
		{
			colorPreset = item.colorPreset;
		}
		else
		{
			colorPreset = null;
		}
		if (Object.op_Implicit((Object)(object)item))
		{
			emojiIcon = item.emojiIcon;
		}
		else
		{
			emojiIcon = SemiFunc.emojiIcon.drone_heal;
		}
		LocalizationManager.instance.AddLocalizationChangedCallback(RefreshLocalizations);
		RefreshLocalizations();
	}

	private void RefreshLocalizations()
	{
		if (Object.op_Implicit((Object)(object)item))
		{
			cachedItemName = item.itemNameLocalized?.GetLocalizedString() ?? item.itemName;
		}
	}

	private void Start()
	{
		try
		{
			StartSetup();
		}
		catch
		{
			if (iconCoroutine != null)
			{
				((MonoBehaviour)this).StopCoroutine(iconCoroutine);
			}
			IconComplete();
			throw;
		}
	}

	private void StartSetup()
	{
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		itemName = item.itemName;
		instanceName = null;
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		itemType = item.itemType;
		itemAssetName = ((Object)item).name;
		itemValueMin = item.value.valueMin;
		itemValueMax = item.value.valueMax;
		if (SemiFunc.RunIsShop())
		{
			shopItem = true;
		}
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			ItemVolume componentInChildren = ((Component)this).GetComponentInChildren<ItemVolume>();
			if (Object.op_Implicit((Object)(object)componentInChildren))
			{
				itemVolume = ((Component)componentInChildren).transform;
			}
			if (Object.op_Implicit((Object)(object)itemVolume))
			{
				Vector3 val = itemVolume.position - ((Component)this).transform.position;
				Transform transform = ((Component)this).transform;
				transform.position -= val;
				Rigidbody component = ((Component)this).GetComponent<Rigidbody>();
				component.position -= val;
				if (SemiFunc.IsMultiplayer())
				{
					((Component)this).GetComponent<PhotonTransformView>().Teleport(component.position, ((Component)this).transform.rotation);
				}
				Object.Destroy((Object)(object)((Component)itemVolume).gameObject);
			}
		}
		if (!shopItem)
		{
			ItemManager.instance.AddSpawnedItem(this);
		}
		((Component)this).transform.parent = LevelGenerator.Instance.ItemParent.transform;
		GetValue();
		roomVolumeCheck = ((Component)this).GetComponent<RoomVolumeCheck>();
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		if (Object.op_Implicit((Object)(object)itemEquippable))
		{
			itemEquippable.itemEmojiIcon = emojiIcon;
			itemEquippable.itemEmoji = emojiIcon.ToString();
		}
		iconState = IconState.Waiting;
		iconCoroutine = ((MonoBehaviour)this).StartCoroutine(GenerateIcon());
		((MonoBehaviour)this).StartCoroutine(LateStart());
	}

	private IEnumerator GenerateIcon()
	{
		yield return null;
		IEnumerator _generation = GenerateIconNow();
		try
		{
			while (true)
			{
				if (!IconWorkReady())
				{
					yield return null;
					continue;
				}
				double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
				bool flag;
				try
				{
					flag = _generation.MoveNext();
				}
				finally
				{
					iconWorkTime += Time.realtimeSinceStartupAsDouble - realtimeSinceStartupAsDouble;
				}
				if (flag)
				{
					yield return _generation.Current;
					continue;
				}
				break;
			}
		}
		finally
		{
			ItemAttributes itemAttributes = this;
			(_generation as IDisposable)?.Dispose();
			itemAttributes.IconComplete();
		}
	}

	private void IconComplete()
	{
		hasIcon = true;
		iconState = IconState.Complete;
		iconCoroutine = null;
		pendingIcons.Remove(this);
	}

	private IEnumerator GenerateIconNow()
	{
		SemiIconMaker _iconMaker = ((Component)this).GetComponentInChildren<SemiIconMaker>(true);
		try
		{
			if (!Object.op_Implicit((Object)(object)itemEquippable) || Object.op_Implicit((Object)(object)icon))
			{
				yield break;
			}
			string text = (Application.isEditor ? Path.Combine("Cache", "Editor") : "Cache");
			string _iconName = ((Object)((Component)this).gameObject).name.Replace("(Clone)", "");
			string _filePath = Path.Combine(new string[5]
			{
				Application.persistentDataPath,
				text,
				"Icons",
				"Items",
				_iconName.ToLowerInvariant() + ".png"
			});
			if (Object.op_Implicit((Object)(object)DebugComputerCheck.instance) && !DebugComputerCheck.instance.DebugDisable && DebugComputerCheck.instance.ItemDebug && DebugComputerCheck.instance.DisableItemIconCache)
			{
				_filePath = null;
			}
			if (_filePath != null && iconCache.TryGetValue(_iconName, out var val) && Object.op_Implicit((Object)(object)val))
			{
				icon = val;
				iconShared = true;
			}
			if (_filePath != null && !Object.op_Implicit((Object)(object)icon))
			{
				if (bakedIconLoad == null)
				{
					bakedIconLoad = SemiIconMaker.LoadBakedIcon("Items", ((Object)item).name + "|" + _iconName, (Object)(object)this, (Sprite _sprite) =>
					{
						icon = _sprite;
						generatedIcon = _sprite;
						generatedIconTexture = _sprite.texture;
					});
				}
				while (!bakedIconLoad.IsCompleted)
				{
					yield return null;
				}
			}
			if (!Object.op_Implicit((Object)(object)icon))
			{
				icon = SemiFunc.LoadSpriteFromFile(_filePath);
			}
			if (!Object.op_Implicit((Object)(object)icon) && Object.op_Implicit((Object)(object)_iconMaker))
			{
				SemiIconMaker.IconReadback _readback = IconCapture(_iconMaker, _filePath, _async: true);
				if (_readback != null)
				{
					double _deadline = Time.realtimeSinceStartupAsDouble + 5.0;
					while (!_readback.done && Time.realtimeSinceStartupAsDouble < _deadline)
					{
						yield return null;
					}
					if (_readback.done && !_readback.failed)
					{
						icon = _readback.CreateSprite(_filePath);
					}
					if (!Object.op_Implicit((Object)(object)icon))
					{
						IconCapture(_iconMaker, _filePath, _async: false);
					}
				}
			}
			else if (!Object.op_Implicit((Object)(object)icon))
			{
				Debug.LogWarning((object)("no IconMaker found in " + _iconName + "... add the SemiIconMaker prefab and line up its camera for a proper icon.. or make a custom icon and put it in the Item Attributes"));
			}
			generatedIcon = icon;
			if (Object.op_Implicit((Object)(object)generatedIcon))
			{
				generatedIconTexture = generatedIcon.texture;
			}
			if (Object.op_Implicit((Object)(object)icon) && !iconShared && _filePath != null)
			{
				iconCache[_iconName] = icon;
				iconShared = true;
			}
		}
		finally
		{
			if (Object.op_Implicit((Object)(object)_iconMaker))
			{
				((Component)_iconMaker).gameObject.SetActive(false);
			}
		}
	}

	private SemiIconMaker.IconReadback IconCapture(SemiIconMaker _iconMaker, string _filePath, bool _async)
	{
		BatteryVisualLogic componentInChildren = ((Component)this).GetComponentInChildren<BatteryVisualLogic>(true);
		bool active = Object.op_Implicit((Object)(object)componentInChildren) && ((Component)componentInChildren).gameObject.activeSelf;
		GameObject itemIconLights = ItemManager.instance.itemIconLights;
		bool activeSelf = itemIconLights.activeSelf;
		try
		{
			if (Object.op_Implicit((Object)(object)componentInChildren))
			{
				((Component)componentInChildren).gameObject.SetActive(false);
			}
			if (!activeSelf)
			{
				itemIconLights.SetActive(true);
			}
			if (_async)
			{
				SemiIconMaker.IconReadback iconReadback = _iconMaker.CreateIconReadback();
				if (iconReadback != null)
				{
					return iconReadback;
				}
			}
			icon = _iconMaker.CreateIconFromRenderTexture(_filePath);
			return null;
		}
		finally
		{
			if (Object.op_Implicit((Object)(object)componentInChildren))
			{
				((Component)componentInChildren).gameObject.SetActive(active);
			}
			if (Object.op_Implicit((Object)(object)itemIconLights) && !activeSelf)
			{
				itemIconLights.SetActive(false);
			}
		}
	}

	private IEnumerator LateStart()
	{
		while (!LevelGenerator.Instance.Generated)
		{
			yield return (object)new WaitForSeconds(0.1f);
		}
		photonView = ((Component)this).GetComponent<PhotonView>();
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			StatsManager.instance.ItemFetchName(itemAssetName, this, photonView.ViewID);
		}
	}

	public void GetValue()
	{
		if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
		{
			float num = Random.Range(itemValueMin, itemValueMax) * ShopManager.instance.itemValueMultiplier;
			if (num < 1000f)
			{
				num = 1000f;
			}
			num = Mathf.Ceil(num / 1000f);
			if (itemType == SemiFunc.itemType.item_upgrade)
			{
				num = ShopManager.instance.UpgradeValueGet(num, item);
			}
			else if (itemType == SemiFunc.itemType.healthPack)
			{
				num = ShopManager.instance.HealthPackValueGet(num);
			}
			else if (itemType == SemiFunc.itemType.power_crystal)
			{
				num = ShopManager.instance.CrystalValueGet(num);
			}
			value = (int)num;
			if (GameManager.Multiplayer() && photonView.ViewID != 0)
			{
				photonView.RPC("GetValueRPC", (RpcTarget)1, new object[1] { value });
			}
		}
	}

	[PunRPC]
	public void GetValueRPC(int _value, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			value = _value;
		}
	}

	public int ValueDiscount(float _discount, int _minimumValue = 0)
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return 0;
		}
		int num = value;
		int num2 = Mathf.Max(Mathf.CeilToInt((float)value * Mathf.Clamp01(_discount)), Mathf.Max(0, _minimumValue));
		if (num2 <= 0)
		{
			return 0;
		}
		value = Mathf.Max(0, value - num2);
		if (SemiFunc.IsMultiplayer() && Object.op_Implicit((Object)(object)photonView) && photonView.ViewID != 0)
		{
			photonView.RPC("GetValueRPC", (RpcTarget)1, new object[1] { value });
		}
		if (shopItem)
		{
			SemiFunc.ShopUpdateCost();
		}
		return num - value;
	}

	public void DisableUI(bool _disable)
	{
		disableUI = _disable;
	}

	private void ShopInTruckLogic()
	{
		if ((!SemiFunc.RunIsShop() && !RunManager.instance.levelIsShop) || !SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (inStartRoomCheckTimer > 0f)
		{
			inStartRoomCheckTimer -= Time.deltaTime;
			return;
		}
		bool flag = false;
		if (!blockExtraction)
		{
			foreach (RoomVolume currentRoom in roomVolumeCheck.CurrentRooms)
			{
				if (currentRoom.Extraction)
				{
					flag = true;
					break;
				}
			}
		}
		bool flag2 = ShopManager.instance.shoppingList.Contains(this);
		if (!flag & flag2)
		{
			ShopManager.instance.ShoppingListItemRemove(this);
		}
		inStartRoomCheckTimer = 0.5f;
		if (flag && !flag2)
		{
			ShopManager.instance.ShoppingListItemAdd(this);
		}
	}

	private void Update()
	{
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		if (showInfoTimer > 0f && physGrabObject.grabbedLocal)
		{
			itemTag = "";
			promptName = "";
			showInfoTimer = 0f;
		}
		if (showInfoTimer > 0f)
		{
			if (!PhysGrabber.instance.grabbed)
			{
				ShowingInfo();
				showInfoTimer -= Time.fixedDeltaTime;
			}
			else
			{
				showInfoTimer = 0f;
			}
		}
		ShopInTruckLogic();
		if (physGrabObject.playerGrabbing.Count > 0 && !disableUI && (Object)(object)PhysGrabber.instance.grabbedPhysGrabObject == (Object)(object)physGrabObject && PhysGrabber.instance.grabbed)
		{
			ShowingInfo();
		}
		if (isHeldTimer > 0f)
		{
			isHeldTimer -= Time.deltaTime;
		}
		if (isHeldTimer <= 0f && physGrabObject.grabbedLocal)
		{
			isHeldTimer = 0.2f;
		}
		if (isHeldTimer > 0f)
		{
			if (SemiFunc.RunIsShop() && !PhysGrabber.instance.grabbed && (Object)(object)PhysGrabber.instance.currentlyLookingAtItemAttributes == (Object)(object)this)
			{
				WorldSpaceUIValue.instance.Show(physGrabObject, value, _cost: true, costOffset);
			}
			SemiFunc.UIItemInfoText(this, promptName);
		}
		else if (itemTag != "")
		{
			itemTag = "";
			promptName = "";
		}
	}

	public void ShowingInfo()
	{
		if (isHeldTimer < 0f)
		{
			return;
		}
		bool grabbedLocal = physGrabObject.grabbedLocal;
		if ((!grabbedLocal && !SemiFunc.RunIsShop()) || DebugCommandHandler.instance.hideItemLabels)
		{
			return;
		}
		isHeldTimer = 0.2f;
		bool flag = SemiFunc.RunIsShop() && (itemType == SemiFunc.itemType.item_upgrade || itemType == SemiFunc.itemType.healthPack);
		ItemToggle itemToggle = this.itemToggle;
		if (Object.op_Implicit((Object)(object)itemToggle) && !itemToggle.disabled && !flag && itemTag == "")
		{
			itemTag = SemiFunc.InputDisplayReplaceTags("[interact]");
			if (grabbedLocal)
			{
				promptName = cachedItemName + " <color=#FFFFFF>[" + itemTag + "]</color>";
			}
			else
			{
				promptName = cachedItemName;
			}
		}
		else if (!flag && showInfoTimer <= 0f && Object.op_Implicit((Object)(object)itemToggle) && !itemToggle.disabled && itemTag != "")
		{
			promptName = cachedItemName + " <color=#FFFFFF>[" + itemTag + "]</color>";
		}
		else
		{
			promptName = cachedItemName;
		}
	}

	public void ShowInfo()
	{
		if (SemiFunc.RunIsShop() && !physGrabObject.grabbedLocal)
		{
			isHeldTimer = 0.1f;
			showInfoTimer = 0.1f;
		}
	}

	private void OnDestroy()
	{
		pendingIcons.Remove(this);
		if (!iconShared)
		{
			if (Object.op_Implicit((Object)(object)generatedIcon))
			{
				Object.Destroy((Object)(object)generatedIcon);
			}
			if (Object.op_Implicit((Object)(object)generatedIconTexture))
			{
				Object.Destroy((Object)(object)generatedIconTexture);
			}
		}
		LocalizationManager.instance.RemoveLocalizationChangedCallback(RefreshLocalizations);
	}
}
