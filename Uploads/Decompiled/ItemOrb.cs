using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemOrb : MonoBehaviour
{
	public enum State
	{
		Off,
		Appearing,
		Active,
		NoBattery,
		Disappearing
	}

	public enum OrbType
	{
		Constant,
		Pulse
	}

	public State currentState;

	private bool stateStart;

	private float stateTimer;

	[HideInInspector]
	public SemiFunc.emojiIcon emojiIcon;

	public Texture orbIcon;

	public float orbRadius = 1f;

	private float orbRadiusOriginal = 1f;

	private float orbRadiusMultiplier = 1f;

	private ItemToggle itemToggle;

	[HideInInspector]
	public float batteryDrainRate = 0.1f;

	[HideInInspector]
	public bool itemActive;

	private Transform sphereEffectTransform;

	private float sphereEffectScaleLerp;

	private PhysGrabObject physGrabObject;

	internal List<PhysGrabObject> objectAffected = new List<PhysGrabObject>();

	internal bool localPlayerAffected;

	private float sphereCheckTimer;

	private List<Transform> spherePieces = new List<Transform>();

	private Transform sphereCore;

	[HideInInspector]
	public ColorPresets colorPresets;

	public BatteryDrainPresets batteryDrainPreset;

	[HideInInspector]
	public Color orbColor;

	private Color orbColorLight;

	[HideInInspector]
	public Color batteryColor;

	private ItemBattery itemBattery;

	private ItemEquippable itemEquippable;

	private ItemAttributes itemAttributes;

	public Sound soundOrbBoot;

	public Sound soundOrbShutdown;

	public Sound soundOrbLoop;

	public OrbType orbType;

	public bool targetValuables = true;

	public bool targetPlayers = true;

	public bool targetEnemies = true;

	public bool targetNonValuables = true;

	private ITargetingCondition customTargetingCondition;

	private void Start()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		customTargetingCondition = ((Component)this).GetComponent<ITargetingCondition>();
		itemBattery = ((Component)this).GetComponent<ItemBattery>();
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		itemAttributes = ((Component)this).GetComponent<ItemAttributes>();
		emojiIcon = itemAttributes.emojiIcon;
		colorPresets = itemAttributes.colorPreset;
		orbColor = colorPresets.GetColorMain();
		orbColorLight = colorPresets.GetColorLight();
		batteryColor = orbColorLight;
		itemBattery.batteryColor = batteryColor;
		batteryDrainRate = batteryDrainPreset.batteryDrainRate;
		itemBattery.batteryDrainRate = batteryDrainRate;
		itemEquippable.itemEmoji = emojiIcon.ToString();
		SetupVisuals(colorPresets);
		sphereEffectTransform = ((Component)this).transform.Find("sphere effect");
		Material material = ((Component)((Component)this).transform.Find("sphere effect/AreaEffect/effect")).GetComponent<Renderer>().material;
		Material material2 = ((Component)((Component)this).transform.Find("sphere effect/AreaEffect/outline_inside")).GetComponent<Renderer>().material;
		Material material3 = ((Component)((Component)this).transform.Find("sphere effect/AreaEffect/outline")).GetComponent<Renderer>().material;
		Color val = new Color(orbColorLight.r, orbColorLight.g, orbColorLight.b, 0.5f);
		Color val2 = new Color(orbColor.r, orbColor.g, orbColor.b, 0.1f);
		if (Object.op_Implicit((Object)(object)material))
		{
			material.SetColor("_Color", val2);
		}
		if (Object.op_Implicit((Object)(object)material2))
		{
			material2.SetColor("_EdgeColor", val);
		}
		if (Object.op_Implicit((Object)(object)material3))
		{
			material3.SetColor("_EdgeColor", val);
		}
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		((Component)sphereEffectTransform).transform.localScale = new Vector3(0f, 0f, 0f);
		((Component)sphereEffectTransform).gameObject.SetActive(false);
		orbRadiusOriginal = orbRadius;
		physGrabObject.clientNonKinematic = true;
	}

	public void SetupVisuals(ColorPresets _colors, Func<Renderer, Material> _materialGet = null)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected Obj, but got Unknown
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected Obj, but got Unknown
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Expected Obj, but got Unknown
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		if (_materialGet == null)
		{
			_materialGet = (Renderer _renderer) => _renderer.material;
		}
		orbColor = _colors.GetColorMain();
		orbColorLight = _colors.GetColorLight();
		spherePieces.Clear();
		ItemLight component = ((Component)this).GetComponent<ItemLight>();
		if (Object.op_Implicit((Object)(object)component))
		{
			component.itemLight.color = orbColor;
		}
		Transform val = ((Component)this).transform.Find("Item Orb Mesh/Top/Piece1/Orb Icon");
		if (Object.op_Implicit((Object)(object)val))
		{
			_materialGet(((Component)val).GetComponent<Renderer>()).SetTexture("_EmissionMap", orbIcon);
			_materialGet(((Component)val).GetComponent<Renderer>()).SetColor("_EmissionColor", orbColor);
		}
		Transform val2 = null;
		foreach (Transform item in ((Component)this).transform)
		{
			Transform val3 = item;
			if (((Object)val3).name == "Item Orb Mesh")
			{
				val2 = val3;
			}
		}
		if ((Object)(object)val2 == (Object)null)
		{
			Debug.LogWarning((object)("Item Orb Mesh not found in" + ((Object)((Component)this).gameObject).name));
		}
		foreach (Transform item2 in val2)
		{
			Transform val4 = item2;
			foreach (Transform item3 in val4)
			{
				Transform val5 = item3;
				if (((Object)val5).name.Contains("Piece"))
				{
					spherePieces.Add(val5);
					_materialGet(((Component)val5).GetComponent<Renderer>()).SetColor("_EmissionColor", orbColor);
				}
			}
			if (((Object)val4).name.Contains("Core"))
			{
				sphereCore = val4;
				_materialGet(((Component)val4).GetComponent<Renderer>()).SetColor("_EmissionColor", orbColorLight);
			}
		}
	}

	private void StateSet(State _newState)
	{
		if (currentState != _newState)
		{
			stateStart = true;
			currentState = _newState;
		}
	}

	private void StateMachine()
	{
		switch (currentState)
		{
		case State.Off:
			StateOff();
			break;
		case State.Appearing:
			StateAppearing();
			break;
		case State.Active:
			StateActive();
			break;
		case State.NoBattery:
			StateNoBattery();
			break;
		case State.Disappearing:
			StateDisappearing();
			break;
		}
	}

	private void StateOff()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			sphereEffectScaleLerp = 0f;
			sphereEffectTransform.localScale = Vector3.zero;
			((Component)sphereEffectTransform).gameObject.SetActive(false);
			objectAffected.Clear();
			localPlayerAffected = false;
		}
		if (itemToggle.toggleState && itemBattery.batteryLife > 0f)
		{
			StateSet(State.Appearing);
		}
	}

	private void StateAppearing()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			itemActive = true;
			itemBattery.batteryActive = true;
			((Component)sphereEffectTransform).gameObject.SetActive(true);
			soundOrbBoot.Play(((Component)this).transform.position);
		}
		OrbAnimateAppear();
		SphereAnimatePieces();
		if (sphereEffectScaleLerp >= 1f)
		{
			StateSet(State.Active);
		}
	}

	private void StateActive()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			sphereEffectScaleLerp = 1f;
			float num = orbRadius;
			sphereEffectTransform.localScale = new Vector3(num, num, num);
		}
		if (orbType == OrbType.Constant)
		{
			OrbConstantLogic();
		}
		if (orbType == OrbType.Pulse)
		{
			OrbPulseLogic();
		}
		SphereAnimatePieces();
		if (!itemToggle.toggleState)
		{
			StateSet(State.Disappearing);
		}
		else if (SemiFunc.IsMasterClientOrSingleplayer() && itemBattery.batteryLife <= 0f)
		{
			StateSet(State.NoBattery);
		}
	}

	private void StateNoBattery()
	{
		if (stateStart)
		{
			stateStart = false;
			stateTimer = 0f;
			objectAffected.Clear();
			localPlayerAffected = false;
		}
		if (!itemBattery.batteryActive)
		{
			itemBattery.BatteryToggle(toggle: true);
		}
		SphereAnimatePieces();
		stateTimer += Time.deltaTime;
		if (stateTimer >= 1.5f)
		{
			itemToggle.ToggleItem(toggle: false);
			StateSet(State.Disappearing);
		}
	}

	private void StateDisappearing()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			itemActive = false;
			itemBattery.batteryActive = false;
			soundOrbShutdown.Play(((Component)this).transform.position);
			objectAffected.Clear();
			localPlayerAffected = false;
			SphereAnimatePiecesBack();
		}
		OrbAnimateDisappear();
		if (sphereEffectScaleLerp <= 0f)
		{
			StateSet(State.Off);
		}
	}

	public void RejectFromCube()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			itemToggle.ToggleItem(toggle: false);
			StateSet(State.Disappearing);
			StateDisappearing();
		}
	}

	private bool CubeRejectCheck()
	{
		if (currentState != State.Active || !itemToggle.toggleState || itemBattery.batteryLife <= 0f)
		{
			return false;
		}
		if (!ValuableCubeOfKnowledge.ItemRejectInRange(physGrabObject, orbRadius))
		{
			return false;
		}
		objectAffected.Clear();
		localPlayerAffected = false;
		itemActive = false;
		return true;
	}

	private void Update()
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.RunIsLevel() && !SemiFunc.RunIsLobby() && !SemiFunc.RunIsShop() && !SemiFunc.RunIsArena() && !SemiFunc.RunIsTutorial())
		{
			return;
		}
		orbRadius = orbRadiusOriginal * orbRadiusMultiplier;
		if (!CubeRejectCheck())
		{
			StateMachine();
			soundOrbLoop.PlayLoop(itemActive, 0.5f, 0.5f);
			if (((Component)sphereEffectTransform).gameObject.activeSelf)
			{
				sphereEffectTransform.rotation = Quaternion.identity;
			}
		}
	}

	private void SphereAnimatePieces()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		foreach (Transform spherePiece in spherePieces)
		{
			float num2 = Mathf.Sin(Time.time * 50f + (float)num) * 0.1f;
			spherePiece.localScale = new Vector3(1f + num2, 1f + num2, 1f + num2);
			num++;
		}
		float num3 = Mathf.Sin(Time.time * 30f) * 0.2f;
		sphereCore.localScale = new Vector3(1f + num3, 1f + num3, 1f + num3);
	}

	private void SphereAnimatePiecesBack()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		foreach (Transform spherePiece in spherePieces)
		{
			spherePiece.localScale = Vector3.one;
		}
		sphereCore.localScale = Vector3.one;
	}

	private void OrbConstantLogic()
	{
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		if (itemBattery.batteryLife <= 0f)
		{
			objectAffected.Clear();
			localPlayerAffected = false;
		}
		else
		{
			if (!itemActive)
			{
				return;
			}
			sphereCheckTimer += Time.deltaTime;
			if (!(sphereCheckTimer > 0.1f))
			{
				return;
			}
			objectAffected.Clear();
			sphereCheckTimer = 0f;
			if (itemBattery.batteryLife <= 0f)
			{
				return;
			}
			if (targetEnemies || targetNonValuables || targetValuables)
			{
				objectAffected = SemiFunc.PhysGrabObjectGetAllWithinRange(orbRadius, ((Component)this).transform.position);
				if (!targetEnemies || !targetNonValuables || !targetValuables)
				{
					List<PhysGrabObject> list = new List<PhysGrabObject>();
					foreach (PhysGrabObject item in objectAffected)
					{
						bool flag = customTargetingCondition != null && customTargetingCondition.CustomTargetingCondition(((Component)item).gameObject);
						if (customTargetingCondition == null)
						{
							flag = true;
						}
						if ((targetEnemies && item.isEnemy) & flag)
						{
							list.Add(item);
						}
						if ((targetNonValuables && item.isNonValuable) & flag)
						{
							list.Add(item);
						}
						if ((targetValuables && item.isValuable) & flag)
						{
							list.Add(item);
						}
					}
					objectAffected.Clear();
					objectAffected = list;
				}
			}
			if (targetPlayers)
			{
				localPlayerAffected = SemiFunc.LocalPlayerOverlapCheck(orbRadius, ((Component)this).transform.position);
			}
		}
	}

	private void OrbPulseLogic()
	{
	}

	private void OrbAnimateAppear()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Lerp(0f, orbRadius, sphereEffectScaleLerp);
		sphereEffectTransform.localScale = new Vector3(num, num, num);
		if (sphereEffectScaleLerp < 1f)
		{
			sphereEffectScaleLerp += 10f * Time.deltaTime;
		}
		else
		{
			sphereEffectScaleLerp = 1f;
		}
	}

	private void OrbAnimateDisappear()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if (((Component)sphereEffectTransform).gameObject.activeSelf)
		{
			float num = Mathf.Lerp(0f, orbRadius, sphereEffectScaleLerp);
			sphereEffectTransform.localScale = new Vector3(num, num, num);
			if (sphereEffectScaleLerp > 0f)
			{
				sphereEffectScaleLerp -= 10f * Time.deltaTime;
				return;
			}
			sphereEffectScaleLerp = 0f;
			((Component)sphereEffectTransform).gameObject.SetActive(false);
		}
	}
}
