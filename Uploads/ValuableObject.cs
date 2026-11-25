using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

public class ValuableObject : MonoBehaviour
{
	public enum Status
	{
		WIP,
		Finalized
	}

	[Serializable]
	public class ExtractionPointTracking
	{
		public GameObject extractionPoint;

		public int closestRingReached;

		public float localPlayerHaulToExtraction;

		public ExtractionPointTracking(GameObject _extractionPoint)
		{
			extractionPoint = _extractionPoint;
			closestRingReached = -1;
			localPlayerHaulToExtraction = 0f;
		}
	}

	public Status status;

	[Space(40f)]
	[Header("Presets")]
	public Durability durabilityPreset;

	public Value valuePreset;

	public PhysAttribute physAttributePreset;

	public PhysAudio audioPreset;

	[Range(0.5f, 3f)]
	public float audioPresetPitch = 1f;

	public Gradient particleColors;

	[Space(70f)]
	public ValuableVolume.Type volumeType;

	public bool debugVolume = true;

	private PhysGrabObjectImpactDetector impactDetector;

	[Space(20f)]
	internal float dollarValueOriginal = 100f;

	internal float dollarValueCurrent = 100f;

	internal bool dollarValueSet;

	internal int dollarValueOverride;

	internal bool halloweenTowerRewarded;

	private Rigidbody rb;

	private PhotonView photonView;

	internal Vector3 spawnPosition;

	private NavMeshObstacle navMeshObstacle;

	private bool inStartRoom;

	private float inStartRoomCheckTimer;

	internal bool committedToExtraction;

	internal RoomVolumeCheck roomVolumeCheck;

	internal PhysGrabObject physGrabObject;

	internal bool discovered;

	internal bool discoveredReminder;

	private float discoveredReminderTimer;

	private List<ExtractionPointTracking> extractionPointTrackings = new List<ExtractionPointTracking>();

	private bool extractionTrackingInitialized;

	private Vector3 lastExtractionTrackingPosition;

	private Dictionary<PhysGrabber, float> playerGrabbedTime = new Dictionary<PhysGrabber, float>();

	private Dictionary<PhysGrabCart, Dictionary<PhysGrabber, float>> cartSnapshots = new Dictionary<PhysGrabCart, Dictionary<PhysGrabber, float>>();

	private float lastGrabbedTimeUpdate;

	[SerializeField]
	private DebugValuableMeshes debugValuableMeshes;

	private void Awake()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		photonView = ((Component)this).GetComponent<PhotonView>();
		spawnPosition = ((Component)this).transform.position;
		if (SemiFunc.GameEventIsHalloween() && !Object.op_Implicit((Object)(object)((Component)this).GetComponent<ExtractionHalloweenContact>()))
		{
			((Component)this).gameObject.AddComponent<ExtractionHalloweenContact>();
		}
	}

	private void Start()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		roomVolumeCheck = ((Component)this).GetComponent<RoomVolumeCheck>();
		impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		float num = Mathf.Max(new float[3]
		{
			((Component)this).transform.localScale.x,
			((Component)this).transform.localScale.y,
			((Component)this).transform.localScale.z
		});
		if (num > 1f)
		{
			float num2 = Mathf.Clamp01((num - 1f) / 1f);
			audioPresetPitch = Mathf.Lerp(audioPresetPitch, 0.65f, num2);
		}
		navMeshObstacle = ((Component)this).GetComponent<NavMeshObstacle>();
		if (Object.op_Implicit((Object)(object)navMeshObstacle))
		{
			Debug.LogError((object)(((Object)((Component)this).gameObject).name + " has a NavMeshObstacle component. Please remove it."));
		}
		((MonoBehaviour)this).StartCoroutine(DollarValueSet());
		rb = ((Component)this).GetComponent<Rigidbody>();
		if (Object.op_Implicit((Object)(object)physAttributePreset))
		{
			if (Object.op_Implicit((Object)(object)rb))
			{
				rb.mass = physAttributePreset.mass;
			}
			if (Object.op_Implicit((Object)(object)physGrabObject))
			{
				physGrabObject.massOriginal = physAttributePreset.mass;
			}
		}
		else if (Object.op_Implicit((Object)(object)rb) || Object.op_Implicit((Object)(object)physGrabObject) || Object.op_Implicit((Object)(object)impactDetector))
		{
			Debug.LogError((object)(((Object)((Component)this).gameObject).name + " is missing a PhysAttributePreset."), (Object)(object)((Component)this).gameObject);
		}
		if (!LevelGenerator.Instance.Generated)
		{
			ValuableDirector.instance.valuableSpawnAmount++;
		}
		ValuableDirector.instance.valuableList.Add(this);
		if (volumeType <= ValuableVolume.Type.Small)
		{
			physGrabObject.clientNonKinematic = true;
		}
	}

	private void AddToDollarHaulList()
	{
		if (GameManager.instance.gameMode == 1)
		{
			photonView.RPC("AddToDollarHaulListRPC", (RpcTarget)0, Array.Empty<object>());
		}
		else if (Object.op_Implicit((Object)(object)((Component)this).GetComponent<ValuableObject>()) && !RoundDirector.instance.dollarHaulList.Contains(((Component)this).gameObject))
		{
			RoundDirector.instance.dollarHaulList.Add(((Component)this).gameObject);
		}
	}

	[PunRPC]
	public void AddToDollarHaulListRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && Object.op_Implicit((Object)(object)((Component)this).GetComponent<ValuableObject>()) && !RoundDirector.instance.dollarHaulList.Contains(((Component)this).gameObject))
		{
			RoundDirector.instance.dollarHaulList.Add(((Component)this).gameObject);
		}
	}

	private void RemoveFromDollarHaulList()
	{
		if (GameManager.instance.gameMode == 1)
		{
			photonView.RPC("RemoveFromDollarHaulListRPC", (RpcTarget)0, Array.Empty<object>());
		}
		else if (Object.op_Implicit((Object)(object)((Component)this).GetComponent<ValuableObject>()) && RoundDirector.instance.dollarHaulList.Contains(((Component)this).gameObject))
		{
			RoundDirector.instance.dollarHaulList.Remove(((Component)this).gameObject);
		}
	}

	[PunRPC]
	public void RemoveFromDollarHaulListRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && Object.op_Implicit((Object)(object)((Component)this).GetComponent<ValuableObject>()) && RoundDirector.instance.dollarHaulList.Contains(((Component)this).gameObject))
		{
			RoundDirector.instance.dollarHaulList.Remove(((Component)this).gameObject);
		}
	}

	private void Update()
	{
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		if (GameManager.instance.gameMode == 0 || PhotonNetwork.IsMasterClient)
		{
			if (inStartRoomCheckTimer > 0f)
			{
				inStartRoomCheckTimer -= Time.deltaTime;
			}
			else
			{
				bool flag = false;
				foreach (RoomVolume currentRoom in roomVolumeCheck.CurrentRooms)
				{
					if (currentRoom.Extraction)
					{
						if (!inStartRoom)
						{
							AddToDollarHaulList();
							inStartRoom = true;
						}
						flag = true;
					}
				}
				if (!flag && inStartRoom)
				{
					if (!committedToExtraction)
					{
						RemoveFromDollarHaulList();
					}
					inStartRoom = false;
				}
				inStartRoomCheckTimer = 0.5f;
			}
		}
		if (!extractionTrackingInitialized)
		{
			InitializeExtractionTracking();
		}
		else if (Vector3.Distance(((Component)this).transform.position, lastExtractionTrackingPosition) >= 2f)
		{
			UpdateExtractionTracking();
			lastExtractionTrackingPosition = ((Component)this).transform.position;
		}
		UpdatePlayerGrabbedTime();
		UpdateCartSnapshots();
		DiscoverReminderLogic();
	}

	private IEnumerator DollarValueSet()
	{
		yield return (object)new WaitForSeconds(0.05f);
		while (LevelGenerator.Instance.State <= LevelGenerator.LevelState.Valuable)
		{
			yield return (object)new WaitForSeconds(0.05f);
		}
		while (SemiFunc.IsMultiplayer() && photonView.ViewID == 0)
		{
			yield return (object)new WaitForSeconds(0.05f);
		}
		if (!SemiFunc.IsMultiplayer())
		{
			DollarValueSetLogic();
		}
		else if (SemiFunc.IsMasterClient())
		{
			DollarValueSetLogic();
			photonView.RPC("DollarValueSetRPC", (RpcTarget)1, new object[1] { dollarValueCurrent });
		}
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			RoundDirector.instance.haulGoalMax += (int)dollarValueCurrent;
		}
	}

	public void DollarValueSetLogic()
	{
		if (!dollarValueSet)
		{
			if (dollarValueOverride != 0)
			{
				dollarValueOriginal = dollarValueOverride;
				dollarValueCurrent = dollarValueOverride;
			}
			else
			{
				dollarValueOriginal = Mathf.Round(Random.Range(valuePreset.valueMin, valuePreset.valueMax));
				dollarValueOriginal = Mathf.Round(dollarValueOriginal / 100f) * 100f;
				dollarValueCurrent = dollarValueOriginal;
			}
			dollarValueSet = true;
		}
	}

	private void DiscoverReminderLogic()
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		if (!discovered || discoveredReminder)
		{
			return;
		}
		if (discoveredReminderTimer > 0f)
		{
			discoveredReminderTimer -= Time.deltaTime;
			return;
		}
		discoveredReminderTimer = Random.Range(2f, 5f);
		if (!physGrabObject.impactDetector.inCart && Object.op_Implicit((Object)(object)PlayerController.instance) && ((Behaviour)PlayerController.instance).isActiveAndEnabled)
		{
			if (Vector3.Distance(((Component)this).transform.position, ((Component)PlayerController.instance).transform.position) > 20f)
			{
				discoveredReminder = true;
			}
		}
		else
		{
			discoveredReminder = false;
		}
	}

	public bool Discover(ValuableDiscoverGraphic.State _state)
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
				if (photonView.ViewID == 0)
				{
					return false;
				}
				photonView.RPC("DiscoverRPC", (RpcTarget)0, Array.Empty<object>());
			}
		}
		ValuableDiscover.instance.New(physGrabObject, _state);
		return result;
	}

	[PunRPC]
	private void DiscoverRPC()
	{
		discovered = true;
		Map.Instance.AddValuable(this);
	}

	[PunRPC]
	public void DollarValueSetRPC(float value, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			dollarValueOriginal = value;
			dollarValueCurrent = value;
			dollarValueSet = true;
		}
	}

	private void InitializeExtractionTracking()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		if (LevelGenerator.Instance.State <= LevelGenerator.LevelState.Valuable || (Object)(object)RoundDirector.instance == (Object)null || RoundDirector.instance.extractionPointList == null)
		{
			return;
		}
		foreach (GameObject extractionPoint in RoundDirector.instance.extractionPointList)
		{
			if (Object.op_Implicit((Object)(object)extractionPoint))
			{
				ExtractionPointTracking extractionPointTracking = new ExtractionPointTracking(extractionPoint);
				float num = Vector3.Distance(((Component)this).transform.position, extractionPoint.transform.position);
				if (num < 60f)
				{
					int closestRingReached = Mathf.FloorToInt(num / 2f);
					extractionPointTracking.closestRingReached = closestRingReached;
				}
				extractionPointTrackings.Add(extractionPointTracking);
			}
		}
		lastExtractionTrackingPosition = ((Component)this).transform.position;
		extractionTrackingInitialized = true;
	}

	private void UpdateExtractionTracking()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		if (!extractionTrackingInitialized)
		{
			return;
		}
		foreach (ExtractionPointTracking extractionPointTracking in extractionPointTrackings)
		{
			if (!Object.op_Implicit((Object)(object)extractionPointTracking.extractionPoint))
			{
				continue;
			}
			float num = Vector3.Distance(((Component)this).transform.position, extractionPointTracking.extractionPoint.transform.position);
			if (num >= 60f)
			{
				continue;
			}
			int num2 = Mathf.FloorToInt(num / 2f);
			if (extractionPointTracking.closestRingReached == -1)
			{
				extractionPointTracking.closestRingReached = num2;
			}
			else if (num2 < extractionPointTracking.closestRingReached)
			{
				if (physGrabObject.grabbedLocal)
				{
					extractionPointTracking.localPlayerHaulToExtraction++;
				}
				else if (impactDetector.inCart)
				{
					float localPlayerPercentageForCurrentCart = GetLocalPlayerPercentageForCurrentCart();
					extractionPointTracking.localPlayerHaulToExtraction += 0.2f * localPlayerPercentageForCurrentCart;
				}
				extractionPointTracking.closestRingReached = num2;
			}
		}
	}

	public float GetLocalPlayerHaulScore(GameObject _extractionPoint)
	{
		foreach (ExtractionPointTracking extractionPointTracking in extractionPointTrackings)
		{
			if ((Object)(object)extractionPointTracking.extractionPoint == (Object)(object)_extractionPoint)
			{
				return extractionPointTracking.localPlayerHaulToExtraction;
			}
		}
		return 0f;
	}

	private PhysGrabCart GetCurrentCart()
	{
		if (!Object.op_Implicit((Object)(object)impactDetector))
		{
			return null;
		}
		return impactDetector.currentCart;
	}

	private float GetLocalPlayerPercentageForCurrentCart()
	{
		PhysGrabCart currentCart = GetCurrentCart();
		if (!Object.op_Implicit((Object)(object)currentCart))
		{
			return 0f;
		}
		if (!cartSnapshots.ContainsKey(currentCart))
		{
			return 0f;
		}
		Dictionary<PhysGrabber, float> dictionary = cartSnapshots[currentCart];
		float num = 0f;
		float num2 = 0f;
		foreach (KeyValuePair<PhysGrabber, float> item in dictionary)
		{
			if (Object.op_Implicit((Object)(object)item.Key))
			{
				num += item.Value;
				bool flag = true;
				if ((!GameManager.Multiplayer()) ? item.Key.isLocal : item.Key.photonView.IsMine)
				{
					num2 = item.Value;
				}
			}
		}
		if (num <= 0f)
		{
			return 0f;
		}
		return num2 / num;
	}

	private void UpdatePlayerGrabbedTime()
	{
		if (impactDetector.inCart || physGrabObject.playerGrabbing.Count == 0)
		{
			return;
		}
		float num = Time.time - lastGrabbedTimeUpdate;
		lastGrabbedTimeUpdate = Time.time;
		float num2 = num / (float)physGrabObject.playerGrabbing.Count;
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (Object.op_Implicit((Object)(object)item))
			{
				if (!playerGrabbedTime.ContainsKey(item))
				{
					playerGrabbedTime[item] = 0f;
				}
				playerGrabbedTime[item] += num2;
			}
		}
	}

	private void UpdateCartSnapshots()
	{
		if (impactDetector.inCart)
		{
			PhysGrabCart currentCart = GetCurrentCart();
			if (Object.op_Implicit((Object)(object)currentCart) && !cartSnapshots.ContainsKey(currentCart))
			{
				Dictionary<PhysGrabber, float> value = new Dictionary<PhysGrabber, float>(playerGrabbedTime);
				cartSnapshots[currentCart] = value;
			}
		}
	}

	private void OnDrawGizmos()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		if (debugVolume)
		{
			Gizmos.color = new Color(1f, 0.92f, 0.02f, 0.2f);
			Gizmos.matrix = ((Component)this).transform.localToWorldMatrix;
			Mesh val = null;
			if (Object.op_Implicit((Object)(object)debugValuableMeshes))
			{
				val = volumeType switch
				{
					ValuableVolume.Type.Tiny => debugValuableMeshes.tiny, 
					ValuableVolume.Type.Small => debugValuableMeshes.small, 
					ValuableVolume.Type.Medium => debugValuableMeshes.medium, 
					ValuableVolume.Type.Big => debugValuableMeshes.big, 
					ValuableVolume.Type.Wide => debugValuableMeshes.wide, 
					ValuableVolume.Type.Tall => debugValuableMeshes.tall, 
					ValuableVolume.Type.VeryTall => debugValuableMeshes.veryTall, 
					_ => (Mesh)null, 
				};
			}
			Gizmos.DrawMesh(val, Vector3.zero, Quaternion.identity, Vector3.one);
			Gizmos.DrawWireMesh(val, Vector3.zero, Quaternion.identity, Vector3.one);
		}
	}

	internal Color GetStatusColor()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		return status switch
		{
			Status.WIP => Color32.op_Implicit(new Color32(byte.MaxValue, (byte)16, (byte)0, byte.MaxValue)), 
			Status.Finalized => Color32.op_Implicit(new Color32((byte)0, byte.MaxValue, (byte)2, byte.MaxValue)), 
			_ => Color.white, 
		};
	}
}
