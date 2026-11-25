using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ValuableScale : MonoBehaviour
{
	public enum State
	{
		Idle,
		Active,
		Error,
		Winner,
		Loser
	}

	internal State currentState;

	private float stateTimer;

	private bool stateImpulse;

	public Sound errorSound;

	public Sound winnerSound;

	public Sound loserSound;

	public Sound numberScrollLoop;

	public TextMeshPro displayText;

	public TextMeshPro displayTextUnit;

	public GameObject displayBackground;

	public float flickerInterval = 0.1f;

	public int maxFlickerCount = 6;

	public int decimalPlaces = 3;

	public AnimationCurve weightCurve;

	private Material displayBackgroundMaterial;

	private int currentValue;

	private string[] units = new string[5] { "kg", "???", "Øøm", ":(", ":)" };

	private Color originalColor;

	private Color originalBackgroundColor;

	private int finalWeightValue;

	private int finalWeightUnit;

	private bool playLoop;

	private float flickerTimer;

	private bool postFlickerUpdate;

	private int totalFlickerCount;

	private int flickerCount;

	private PhotonView photonView;

	private PhysGrabObject physGrabObject;

	private float checkTimer;

	public Collider triggerCollider;

	private List<PlayerAvatar> players = new List<PlayerAvatar>();

	private Dictionary<PhysGrabObject, float> physGrabObjectsLastMass = new Dictionary<PhysGrabObject, float>();

	private void Awake()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		originalColor = ((Graphic)displayText).color;
		photonView = ((Component)this).GetComponentInParent<PhotonView>();
		displayBackgroundMaterial = ((Renderer)displayBackground.GetComponent<MeshRenderer>()).material;
		originalBackgroundColor = displayBackgroundMaterial.color;
	}

	private void Update()
	{
		numberScrollLoop.PlayLoop(playLoop, 50f, 50f);
		stateTimer -= Time.deltaTime;
		StateMachine();
	}

	public void OnBreak()
	{
		UpdateState(State.Error);
	}

	[PunRPC]
	private void UpdateStateRPC(State _state, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			currentState = _state;
			stateImpulse = true;
			StateMachine();
		}
	}

	[PunRPC]
	private void UpdateWeightRPC(float _weightValue, int _weightUnit, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			string text = SemiFunc.WeightGetString(_weightValue, decimalPlaces);
			UpdateText(null, null, text, _weightUnit);
		}
	}

	private void StateMachine()
	{
		switch (currentState)
		{
		case State.Idle:
			StateIdle();
			break;
		case State.Active:
			StateActive();
			break;
		case State.Error:
			StateError();
			break;
		case State.Winner:
			StateWinner();
			break;
		case State.Loser:
			StateLoser();
			break;
		}
	}

	public void UpdateState(State _state)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && _state != currentState)
		{
			if (GameManager.Multiplayer())
			{
				photonView.RPC("UpdateStateRPC", (RpcTarget)0, new object[1] { _state });
			}
			else
			{
				UpdateStateRPC(_state);
			}
		}
	}

	private void Impulse()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		playLoop = true;
		totalFlickerCount = Random.Range(3, maxFlickerCount);
		flickerCount = 0;
		flickerTimer = 0f;
		UpdateText(originalColor, originalBackgroundColor, "");
	}

	private void StateIdle()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			stateImpulse = false;
			Impulse();
			stateTimer = 1f;
		}
		Flicker();
		if (postFlickerUpdate)
		{
			UpdateText(originalColor, originalBackgroundColor, "00000000000");
			postFlickerUpdate = false;
		}
		if (!(stateTimer > 0f))
		{
			PerformCoreLogic();
		}
	}

	private void StateActive()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			stateImpulse = false;
			Impulse();
			stateTimer = 1f;
		}
		Flicker();
		if (postFlickerUpdate)
		{
			UpdateText(originalColor, originalBackgroundColor);
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				CalculateWeight();
			}
			postFlickerUpdate = false;
		}
		if (!(stateTimer > 0f))
		{
			PerformCoreLogic();
		}
	}

	private void StateWinner()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			stateImpulse = false;
			Impulse();
			stateTimer = 2f;
		}
		Flicker();
		if (postFlickerUpdate)
		{
			UpdateText(Color.green, Color.black, "WINNER");
			postFlickerUpdate = false;
			winnerSound.Play(((Component)this).transform.position);
		}
		if (!(stateTimer > 0f))
		{
			PerformCoreLogic();
		}
	}

	private void StateError()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			stateImpulse = false;
			Impulse();
			stateTimer = 3f;
		}
		Flicker();
		if (postFlickerUpdate)
		{
			UpdateText(Color.yellow, Color.black, "ERROR");
			postFlickerUpdate = false;
			errorSound.Play(((Component)this).transform.position);
		}
		PerformCoreLogic();
	}

	private void StateLoser()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			stateImpulse = false;
			Impulse();
			stateTimer = 2f;
		}
		Flicker();
		if (postFlickerUpdate)
		{
			UpdateText(Color.red, Color.black, "LOSER");
			postFlickerUpdate = false;
			loserSound.Play(((Component)this).transform.position);
		}
		if (!(stateTimer > 0f))
		{
			PerformCoreLogic();
		}
	}

	private void UpdateText(Nullable _color = null, Nullable _backgroundColor = null, string _displayText = null, int? _unitText = null)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (_color.HasValue)
		{
			((Graphic)displayText).color = _color.Value;
		}
		if (_backgroundColor.HasValue)
		{
			displayBackgroundMaterial.color = _backgroundColor.Value;
		}
		if (_displayText != null)
		{
			((TMP_Text)displayText).text = _displayText;
		}
		if (_unitText.HasValue)
		{
			((TMP_Text)displayTextUnit).text = units[_unitText.Value];
		}
		else
		{
			((TMP_Text)displayTextUnit).text = "";
		}
	}

	private void Flicker()
	{
		if (flickerCount < totalFlickerCount)
		{
			flickerTimer += Time.deltaTime;
			if ((float)flickerCount * flickerInterval <= flickerTimer)
			{
				flickerCount++;
				int num = Mathf.RoundToInt(Mathf.Pow(Random.value, 5f) * 1E+09f);
				((TMP_Text)displayText).text = num.ToString("N0").Replace(",", " ");
			}
		}
		else if (playLoop)
		{
			postFlickerUpdate = true;
			playLoop = false;
		}
	}

	private void PerformCoreLogic()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && SemiFunc.PerSecond(5f, (Object)(object)this))
		{
			if (!IsUpright())
			{
				UpdateState(State.Error);
			}
			else
			{
				WeighItems();
			}
		}
	}

	private bool IsUpright()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return ((Component)this).transform.up.y >= 0.75f;
	}

	private void WeighItems()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = ((Component)triggerCollider).transform.position;
		Collider val = triggerCollider;
		Vector3 size = ((BoxCollider)((val is BoxCollider) ? val : null)).size;
		size.x *= Mathf.Abs(((Component)triggerCollider).transform.lossyScale.x);
		size.y *= Mathf.Abs(((Component)triggerCollider).transform.lossyScale.y);
		size.z *= Mathf.Abs(((Component)triggerCollider).transform.lossyScale.z);
		Collider[] array = Physics.OverlapBox(position, size / 2f, ((Component)triggerCollider).transform.rotation, LayerMask.op_Implicit(SemiFunc.LayerMaskGetPlayersAndPhysObjects()), (QueryTriggerInteraction)2);
		bool flag = false;
		List<PlayerAvatar> list = new List<PlayerAvatar>();
		List<PhysGrabObject> list2 = new List<PhysGrabObject>();
		Collider[] array2 = array;
		foreach (Collider val2 in array2)
		{
			PlayerAvatar playerAvatar = ((Component)val2).GetComponentInParent<PlayerAvatar>();
			PlayerController componentInParent = ((Component)val2).GetComponentInParent<PlayerController>();
			if (Object.op_Implicit((Object)(object)componentInParent))
			{
				playerAvatar = componentInParent.playerAvatarScript;
			}
			PlayerTumble componentInParent2 = ((Component)val2).GetComponentInParent<PlayerTumble>();
			if (Object.op_Implicit((Object)(object)componentInParent2) && Object.op_Implicit((Object)(object)componentInParent2.playerAvatar))
			{
				playerAvatar = componentInParent2.playerAvatar;
			}
			if (!Object.op_Implicit((Object)(object)playerAvatar))
			{
				PlayerAvatarVisuals componentInParent3 = ((Component)val2).GetComponentInParent<PlayerAvatarVisuals>();
				if (Object.op_Implicit((Object)(object)componentInParent3) && Object.op_Implicit((Object)(object)componentInParent3.playerAvatar))
				{
					playerAvatar = componentInParent3.playerAvatar;
				}
			}
			if (Object.op_Implicit((Object)(object)playerAvatar))
			{
				list.Add(playerAvatar);
				if (!players.Contains(playerAvatar))
				{
					players.Add(playerAvatar);
					flag = true;
				}
				continue;
			}
			PhysGrabObject componentInParent4 = ((Component)val2).GetComponentInParent<PhysGrabObject>();
			if (Object.op_Implicit((Object)(object)componentInParent4))
			{
				if ((Object)(object)componentInParent4 == (Object)(object)this.physGrabObject)
				{
					return;
				}
				list2.Add(componentInParent4);
				if (!physGrabObjectsLastMass.ContainsKey(componentInParent4))
				{
					physGrabObjectsLastMass.Add(componentInParent4, componentInParent4.rb.mass);
					flag = true;
				}
			}
		}
		for (int num = players.Count - 1; num >= 0; num--)
		{
			if (!list.Contains(players[num]))
			{
				players.RemoveAt(num);
				flag = true;
			}
		}
		foreach (PhysGrabObject item in physGrabObjectsLastMass.Keys.ToList())
		{
			if (!list2.Contains(item))
			{
				physGrabObjectsLastMass.Remove(item);
				flag = true;
			}
		}
		if (players.Count == 1 && physGrabObjectsLastMass.Count == 0)
		{
			PlayerAvatar playerAvatar2 = players[0];
			if (SessionManager.instance.crownedPlayerSteamID == playerAvatar2.steamID)
			{
				UpdateState(State.Winner);
			}
			else
			{
				UpdateState(State.Loser);
			}
		}
		else
		{
			if (stateTimer > 0f)
			{
				return;
			}
			if (players.Count >= 2)
			{
				UpdateState(State.Error);
				return;
			}
			if (physGrabObjectsLastMass.Count == 0)
			{
				UpdateState(State.Idle);
				return;
			}
			if (!flag)
			{
				for (int j = 0; j < list2.Count; j++)
				{
					PhysGrabObject physGrabObject = list2[j];
					float mass = physGrabObject.rb.mass;
					if (mass != physGrabObjectsLastMass[physGrabObject])
					{
						physGrabObjectsLastMass[physGrabObject] = mass;
						flag = true;
					}
				}
			}
			if ((currentState == State.Active) & flag)
			{
				stateImpulse = true;
				CalculateWeight();
			}
			else
			{
				UpdateState(State.Active);
			}
		}
	}

	private void CalculateWeight()
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		if (Random.Range(0, 20) == 0)
		{
			SetRandomWeight();
			return;
		}
		float num = 0f;
		foreach (var (physGrabObject2, num3) in physGrabObjectsLastMass)
		{
			num += weightCurve.Evaluate(num3);
			float num4 = (float)new Random(((Object)physGrabObject2).GetInstanceID()).NextDouble() * 0.2f - 0.1f;
			num += num * num4;
		}
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("UpdateWeightRPC", (RpcTarget)0, new object[2] { num, 0 });
		}
		else
		{
			UpdateWeightRPC(num, 0);
		}
	}

	private void SetRandomWeight()
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.RoundToInt(Mathf.Pow(Random.value, 5f) * 199999f);
		if (num < 1f)
		{
			num++;
		}
		int num2 = Random.Range(1, units.Length);
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("UpdateWeightRPC", (RpcTarget)0, new object[2] { num, num2 });
		}
		else
		{
			UpdateWeightRPC(num, num2);
		}
	}
}
