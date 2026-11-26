using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class ItemValuableBox : MonoBehaviour
{
	private class AbsorbingValuable
	{
		public GameObject meshCopy;

		public Vector3 startPosition;

		public Vector3 startScale;

		public Vector3 angularVelocity;

		public float effectiveDuration;

		public float timer;

		public float valueToAdd;
	}

	private class StoredValuableMesh
	{
		public Mesh mesh;

		public Material[] materials;

		public Vector3 scale;
	}

	public enum State
	{
		Idle,
		Open,
		Absorbing,
		Close,
		Full,
		OpenToExtractReady,
		ExtractReady,
		Extracting
	}

	public enum MessageType
	{
		Error,
		Neutral,
		Success
	}

	public enum Message
	{
		TooBig,
		TooFull,
		Extract
	}

	public State currentState;

	public PhysGrabObjectComponentInsideBoxChecker boxChecker;

	public Transform hatch1Transform;

	public Transform hatch2Transform;

	public Transform insidePoint;

	public TextScreen textScreen;

	public Transform textScreenAnimatorTransform;

	public GameObject valuableExtractingPrefab;

	public ParticleSystem breakParticles;

	public float valueLimit = 5000f;

	public ValuableVolume.Type typeLimit = ValuableVolume.Type.Small;

	public AnimationCurve animationCurveOpen;

	public AnimationCurve animationCurveClose;

	public float animationDuration = 0.25f;

	public float minimumOpenDuration = 1f;

	public float extractionDuration = 3f;

	public float extractUpwardSpeed = 2f;

	public float extractRotationSpeed = 90f;

	public float extractMaxDistance = 10f;

	public LocalizedAsset localizedMessageTooBig;

	public LocalizedAsset localizedMessageTooFull;

	public LocalizedAsset localizedMessageExtract;

	public Sound soundOpen;

	public Sound soundClose;

	public Sound soundAbsorb;

	public Sound soundMessageError;

	public Sound soundMessageSuccess;

	public Sound soundMessageNeutral;

	public Vector3 textScreenStartPosition;

	public Vector3 textScreenStartRotation;

	public Vector3 textScreenMidPosition = new Vector3(0f, 0.324f, -0.2635f);

	public Vector3 textScreenMidRotation = new Vector3(90f, 180f, 0f);

	public Vector3 textScreenEndPosition = new Vector3(0f, 0.1f, -0.2636f);

	public Vector3 textScreenEndRotation = new Vector3(90f, 180f, 0f);

	private PhotonView photonView;

	private RoomVolumeCheck roomVolumeCheck;

	private const float absorbAnimationDuration = 0.12f;

	private bool stateStart;

	private float stateTimer;

	private float currentValue;

	private List<PhysGrabObject> validObjectsInBox = new List<PhysGrabObject>();

	private List<AbsorbingValuable> activeAbsorptions = new List<AbsorbingValuable>();

	private Dictionary<PhysGrabObject, float> ungrabbedTimers = new Dictionary<PhysGrabObject, float>();

	private float showValueDelayTimer;

	private Vector3 textScreenOriginalPosition;

	private Quaternion textScreenOriginalRotation;

	private List<StoredValuableMesh> storedMeshes = new List<StoredValuableMesh>();

	private int extractionMeshIndex;

	private float extractionSpawnTimer;

	private float extractionSpawnInterval;

	private bool inExtractionPoint;

	private float extractionCheckTimer;

	private HashSet<PhysGrabObject> tooBigOffenders = new HashSet<PhysGrabObject>();

	private HashSet<PhysGrabObject> tooFullOffenders = new HashSet<PhysGrabObject>();

	private Message? activeStickyMessage;

	private float extractionValue;

	private static HashSet<PhysGrabObject> claimedValuables = new HashSet<PhysGrabObject>();

	public float CurrentValue => currentValue;

	public float ExtractionValue => extractionValue;

	public bool InExtractionPoint => inExtractionPoint;

	private void SetStartPosition()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)textScreenAnimatorTransform))
		{
			textScreenStartPosition = textScreenAnimatorTransform.localPosition;
			textScreenStartRotation = textScreenAnimatorTransform.localEulerAngles;
		}
	}

	private void ResetToStartPosition()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)textScreenAnimatorTransform))
		{
			textScreenAnimatorTransform.localPosition = textScreenStartPosition;
			textScreenAnimatorTransform.localEulerAngles = textScreenStartRotation;
		}
	}

	private void SetMidPosition()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)textScreenAnimatorTransform))
		{
			textScreenMidPosition = textScreenAnimatorTransform.localPosition;
			textScreenMidRotation = textScreenAnimatorTransform.localEulerAngles;
		}
	}

	private void ApplyMidPosition()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)textScreenAnimatorTransform))
		{
			textScreenAnimatorTransform.localPosition = textScreenMidPosition;
			textScreenAnimatorTransform.localEulerAngles = textScreenMidRotation;
		}
	}

	private void SetEndPosition()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)textScreenAnimatorTransform))
		{
			textScreenEndPosition = textScreenAnimatorTransform.localPosition;
			textScreenEndRotation = textScreenAnimatorTransform.localEulerAngles;
		}
	}

	private void ApplyEndPosition()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)textScreenAnimatorTransform))
		{
			textScreenAnimatorTransform.localPosition = textScreenEndPosition;
			textScreenAnimatorTransform.localEulerAngles = textScreenEndRotation;
		}
	}

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void RuntimeInitializeOnLoad()
	{
		claimedValuables = new HashSet<PhysGrabObject>();
	}

	private void Start()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected Obj, but got Unknown
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		photonView = ((Component)this).GetComponent<PhotonView>();
		roomVolumeCheck = ((Component)this).GetComponent<RoomVolumeCheck>();
		boxChecker.onObjectsFound.AddListener((UnityAction<List<PhysGrabObject>>)OnObjectsFound);
		boxChecker.onNoObjectsFound.AddListener((UnityAction)OnNoObjectsFound);
		if (Object.op_Implicit((Object)(object)textScreenAnimatorTransform))
		{
			textScreenOriginalPosition = textScreenAnimatorTransform.localPosition;
			textScreenOriginalRotation = textScreenAnimatorTransform.localRotation;
		}
		if (Object.op_Implicit((Object)(object)textScreen))
		{
			textScreen.SetValue((int)currentValue);
		}
	}

	private void Update()
	{
		StateMachine();
		UpdateAbsorptions();
		ShowValueDelayLogic();
		ExtractionPointCheck();
	}

	private void OnDisable()
	{
		CleanUpAbsorptions();
		tooBigOffenders.Clear();
		tooFullOffenders.Clear();
		activeStickyMessage = null;
	}

	private void OnDestroy()
	{
		CleanUpAbsorptions();
		DestroyEffect();
	}

	private void OnObjectsFound(List<PhysGrabObject> foundObjects)
	{
		validObjectsInBox.Clear();
		HashSet<PhysGrabObject> hashSet = new HashSet<PhysGrabObject>();
		HashSet<PhysGrabObject> hashSet2 = new HashSet<PhysGrabObject>();
		foreach (PhysGrabObject foundObject in foundObjects)
		{
			ValuableObject component = ((Component)foundObject).GetComponent<ValuableObject>();
			if (!Object.op_Implicit((Object)(object)component))
			{
				continue;
			}
			if (component.volumeType > typeLimit)
			{
				hashSet.Add(foundObject);
				continue;
			}
			if (currentValue + component.dollarValueCurrent > valueLimit)
			{
				hashSet2.Add(foundObject);
			}
			validObjectsInBox.Add(foundObject);
		}
		tooBigOffenders = hashSet;
		tooFullOffenders = hashSet2;
		UpdateStickyMessage();
		if (validObjectsInBox.Count > 0)
		{
			UpdateTextScreen();
		}
	}

	private void OnNoObjectsFound()
	{
		validObjectsInBox.Clear();
		tooBigOffenders.Clear();
		tooFullOffenders.Clear();
		UpdateStickyMessage();
	}

	private void UpdateStickyMessage()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		Message? message = null;
		if (tooBigOffenders.Count > 0)
		{
			message = Message.TooBig;
		}
		else if (tooFullOffenders.Count > 0)
		{
			message = Message.TooFull;
		}
		if (message != activeStickyMessage)
		{
			activeStickyMessage = message;
			if (message.HasValue)
			{
				ScreenMessage(message.Value, MessageType.Error);
			}
			else
			{
				ScreenMessageClear();
			}
		}
	}

	private void StateSet(State newState)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != newState && SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("StateSetRPC", (RpcTarget)0, new object[1] { (int)newState });
			}
			else
			{
				StateSetRPC((int)newState);
			}
		}
	}

	[PunRPC]
	private void StateSetRPC(int newState, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(info))
		{
			if (currentState != (State)newState)
			{
				stateStart = true;
				currentState = (State)newState;
			}
		}
	}

	private void StateMachine()
	{
		switch (currentState)
		{
		case State.Idle:
			StateIdle();
			break;
		case State.Open:
			StateOpen();
			break;
		case State.Absorbing:
			StateAbsorbing();
			break;
		case State.Close:
			StateClose();
			break;
		case State.Full:
			StateFull();
			break;
		case State.OpenToExtractReady:
			StateOpenToExtractReady();
			break;
		case State.ExtractReady:
			StateExtractReady();
			break;
		case State.Extracting:
			StateExtracting();
			break;
		}
	}

	private void StateIdle()
	{
		if (stateStart)
		{
			stateStart = false;
			stateTimer = 0f;
			ResetHatches();
			CleanUpDestroyedClaims();
			ungrabbedTimers.Clear();
			if (showValueDelayTimer <= 0f && Object.op_Implicit((Object)(object)textScreen))
			{
				textScreen.SetValue((int)currentValue);
			}
		}
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (currentValue >= valueLimit)
			{
				StateSet(State.Full);
			}
			else if (validObjectsInBox.Count > 0)
			{
				StateSet(State.Open);
			}
		}
	}

	private void StateOpen()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			stateTimer = 0f;
			soundOpen.Play(((Component)this).transform.position);
			UpdateTextScreen();
		}
		float num = Mathf.Clamp01(stateTimer / animationDuration);
		AnimateHatchesOpen(num);
		stateTimer += Time.deltaTime;
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		bool flag = num >= 1f;
		bool flag2 = stateTimer >= minimumOpenDuration;
		if (!flag)
		{
			return;
		}
		for (int num2 = validObjectsInBox.Count - 1; num2 >= 0; num2--)
		{
			PhysGrabObject physGrabObject = validObjectsInBox[num2];
			if (physGrabObject.grabbed)
			{
				ungrabbedTimers.Remove(physGrabObject);
			}
			else if (claimedValuables.Contains(physGrabObject))
			{
				ungrabbedTimers.Remove(physGrabObject);
				validObjectsInBox.RemoveAt(num2);
			}
			else
			{
				if (!ungrabbedTimers.ContainsKey(physGrabObject))
				{
					ungrabbedTimers[physGrabObject] = 0f;
				}
				ungrabbedTimers[physGrabObject] += Time.deltaTime;
				if (ungrabbedTimers[physGrabObject] >= 0.02f)
				{
					ValuableObject component = ((Component)physGrabObject).GetComponent<ValuableObject>();
					if (!Object.op_Implicit((Object)(object)component) || !(currentValue + component.dollarValueCurrent > valueLimit))
					{
						ungrabbedTimers.Remove(physGrabObject);
						validObjectsInBox.RemoveAt(num2);
						claimedValuables.Add(physGrabObject);
						StartAbsorb(physGrabObject);
						StateSet(State.Absorbing);
						return;
					}
					tooFullOffenders.Add(physGrabObject);
					UpdateStickyMessage();
					ungrabbedTimers.Remove(physGrabObject);
					validObjectsInBox.RemoveAt(num2);
				}
			}
		}
		if ((validObjectsInBox.Count == 0) & flag2)
		{
			StateSet(State.Close);
		}
	}

	private void StateAbsorbing()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			stateTimer = 0f;
			soundAbsorb.Play(((Component)this).transform.position);
		}
		stateTimer += Time.deltaTime;
		if (SemiFunc.IsMasterClientOrSingleplayer() && activeAbsorptions.Count == 0)
		{
			if (currentValue >= valueLimit)
			{
				StateSet(State.Full);
			}
			else
			{
				StateSet(State.Close);
			}
		}
	}

	private void UpdateAbsorptions()
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		if (activeAbsorptions.Count == 0)
		{
			return;
		}
		for (int num = activeAbsorptions.Count - 1; num >= 0; num--)
		{
			AbsorbingValuable absorbingValuable = activeAbsorptions[num];
			if (absorbingValuable == null || !Object.op_Implicit((Object)(object)absorbingValuable.meshCopy))
			{
				if (absorbingValuable != null && Object.op_Implicit((Object)(object)absorbingValuable.meshCopy))
				{
					Object.Destroy((Object)(object)absorbingValuable.meshCopy);
				}
				activeAbsorptions.RemoveAt(num);
			}
			else
			{
				try
				{
					float num2 = Mathf.Clamp01(absorbingValuable.timer / (absorbingValuable.effectiveDuration * 2.2f));
					float num3 = num2 * num2 * num2;
					absorbingValuable.meshCopy.transform.position = Vector3.Lerp(absorbingValuable.startPosition, insidePoint.position, num3);
					absorbingValuable.meshCopy.transform.localScale = Vector3.Lerp(absorbingValuable.startScale, Vector3.zero, num3);
					Transform transform = absorbingValuable.meshCopy.transform;
					transform.rotation *= Quaternion.Euler(absorbingValuable.angularVelocity * 57.29578f * Time.deltaTime);
					absorbingValuable.timer += Time.deltaTime;
					if (num2 >= 1f)
					{
						Object.Destroy((Object)(object)absorbingValuable.meshCopy);
						if (SemiFunc.IsMasterClientOrSingleplayer())
						{
							float num4 = currentValue + absorbingValuable.valueToAdd;
							num4 = Mathf.Min(num4, valueLimit);
							UpdateValue(num4);
						}
						activeAbsorptions.RemoveAt(num);
					}
				}
				catch (Exception ex)
				{
					Debug.LogWarning((object)("ItemValuableBox UpdateAbsorptions exception, force cleaning entry: " + ex.Message));
					if (Object.op_Implicit((Object)(object)absorbingValuable.meshCopy))
					{
						Object.Destroy((Object)(object)absorbingValuable.meshCopy);
					}
					activeAbsorptions.RemoveAt(num);
				}
			}
		}
	}

	private void CleanUpAbsorptions()
	{
		for (int num = activeAbsorptions.Count - 1; num >= 0; num--)
		{
			AbsorbingValuable absorbingValuable = activeAbsorptions[num];
			if (absorbingValuable != null && Object.op_Implicit((Object)(object)absorbingValuable.meshCopy))
			{
				Object.Destroy((Object)(object)absorbingValuable.meshCopy);
			}
		}
		activeAbsorptions.Clear();
	}

	private void StateClose()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			stateTimer = 0f;
			soundClose.Play(((Component)this).transform.position);
		}
		float num = Mathf.Clamp01(stateTimer / animationDuration);
		AnimateHatchesClose(num);
		stateTimer += Time.deltaTime;
		if (SemiFunc.IsMasterClientOrSingleplayer() && num >= 1f)
		{
			StateSet(State.Idle);
		}
	}

	private void StateFull()
	{
		if (stateStart)
		{
			stateStart = false;
			stateTimer = 0f;
			ResetHatches();
		}
	}

	private void StateOpenToExtractReady()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			stateTimer = 0f;
			soundOpen.Play(((Component)this).transform.position);
		}
		float num = Mathf.Clamp01(stateTimer / animationDuration);
		AnimateHatchesOpen(num);
		stateTimer += Time.deltaTime;
		if (SemiFunc.IsMasterClientOrSingleplayer() && num >= 1f)
		{
			StateSet(State.ExtractReady);
		}
	}

	private void StateExtractReady()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			if (Object.op_Implicit((Object)(object)textScreen))
			{
				textScreen.UpdateText(localizedMessageExtract?.GetLocalizedString() ?? "Extract", Color.green, Color.green);
			}
		}
	}

	private void StateExtracting()
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			stateTimer = 0f;
			extractionMeshIndex = 0;
			extractionSpawnTimer = 0f;
			extractionSpawnInterval = ((storedMeshes.Count > 0) ? (extractionDuration / (float)storedMeshes.Count) : extractionDuration);
			if (Object.op_Implicit((Object)(object)textScreen))
			{
				textScreen.UpdateText(localizedMessageExtract?.GetLocalizedString() ?? "Extract", Color.green, Color.green);
			}
		}
		extractionSpawnTimer += Time.deltaTime;
		while (extractionMeshIndex < storedMeshes.Count && extractionSpawnTimer >= extractionSpawnInterval)
		{
			extractionSpawnTimer -= extractionSpawnInterval;
			SpawnExtractingValuable(storedMeshes[extractionMeshIndex]);
			extractionMeshIndex++;
		}
		stateTimer += Time.deltaTime;
		if (SemiFunc.IsMasterClientOrSingleplayer() && stateTimer >= extractionDuration)
		{
			storedMeshes.Clear();
			UpdateValue(0f);
			StateSet(State.Close);
		}
	}

	private void SpawnExtractingValuable(StoredValuableMesh storedMesh)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)valuableExtractingPrefab))
		{
			ValuableExtracting component = Object.Instantiate<GameObject>(valuableExtractingPrefab, insidePoint.position, Quaternion.identity).GetComponent<ValuableExtracting>();
			if (Object.op_Implicit((Object)(object)component))
			{
				component.Initialize(storedMesh.mesh, storedMesh.materials, storedMesh.scale, extractUpwardSpeed, extractRotationSpeed, extractMaxDistance);
			}
		}
	}

	private void AnimateHatchesOpen(float t)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		float num = animationCurveOpen.Evaluate(t);
		float num2 = -40f;
		float num3 = 40f;
		float num4 = 0.5f;
		hatch1Transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, num2, num));
		hatch2Transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, num3, num));
		Vector3 localScale = hatch1Transform.localScale;
		Vector3 localScale2 = hatch2Transform.localScale;
		localScale.x = Mathf.Lerp(1f, num4, num);
		localScale2.x = Mathf.Lerp(1f, num4, num);
		hatch1Transform.localScale = localScale;
		hatch2Transform.localScale = localScale2;
		if (Object.op_Implicit((Object)(object)textScreenAnimatorTransform))
		{
			if (num < 0.5f)
			{
				float num5 = num * 2f;
				textScreenAnimatorTransform.localPosition = Vector3.Lerp(textScreenOriginalPosition, textScreenMidPosition, num5);
				textScreenAnimatorTransform.localRotation = Quaternion.Slerp(textScreenOriginalRotation, Quaternion.Euler(textScreenMidRotation), num5);
			}
			else
			{
				float num6 = (num - 0.5f) * 2f;
				textScreenAnimatorTransform.localPosition = Vector3.Lerp(textScreenMidPosition, textScreenEndPosition, num6);
				textScreenAnimatorTransform.localRotation = Quaternion.Slerp(Quaternion.Euler(textScreenMidRotation), Quaternion.Euler(textScreenEndRotation), num6);
			}
		}
	}

	private void AnimateHatchesClose(float t)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		float num = animationCurveClose.Evaluate(t);
		float num2 = -40f;
		float num3 = 40f;
		float num4 = 0.5f;
		hatch1Transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(num2, 0f, num));
		hatch2Transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(num3, 0f, num));
		Vector3 localScale = hatch1Transform.localScale;
		Vector3 localScale2 = hatch2Transform.localScale;
		localScale.x = Mathf.Lerp(num4, 1f, num);
		localScale2.x = Mathf.Lerp(num4, 1f, num);
		hatch1Transform.localScale = localScale;
		hatch2Transform.localScale = localScale2;
		if (Object.op_Implicit((Object)(object)textScreenAnimatorTransform))
		{
			if (num < 0.5f)
			{
				float num5 = num * 2f;
				textScreenAnimatorTransform.localPosition = Vector3.Lerp(textScreenEndPosition, textScreenMidPosition, num5);
				textScreenAnimatorTransform.localRotation = Quaternion.Slerp(Quaternion.Euler(textScreenEndRotation), Quaternion.Euler(textScreenMidRotation), num5);
			}
			else
			{
				float num6 = (num - 0.5f) * 2f;
				textScreenAnimatorTransform.localPosition = Vector3.Lerp(textScreenMidPosition, textScreenOriginalPosition, num6);
				textScreenAnimatorTransform.localRotation = Quaternion.Slerp(Quaternion.Euler(textScreenMidRotation), textScreenOriginalRotation, num6);
			}
		}
	}

	private void ResetHatches()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		hatch1Transform.localEulerAngles = Vector3.zero;
		hatch2Transform.localEulerAngles = Vector3.zero;
		hatch1Transform.localScale = Vector3.one;
		hatch2Transform.localScale = Vector3.one;
		if (Object.op_Implicit((Object)(object)textScreenAnimatorTransform))
		{
			textScreenAnimatorTransform.localPosition = textScreenOriginalPosition;
			textScreenAnimatorTransform.localRotation = textScreenOriginalRotation;
		}
	}

	private void StartAbsorb(PhysGrabObject target)
	{
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("StartAbsorbRPC", (RpcTarget)0, new object[1] { target.photonView.ViewID });
		}
		else
		{
			StartAbsorbLocal(target);
		}
	}

	private void StartAbsorbLocal(PhysGrabObject target)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		AbsorbingValuable absorbingValuable = new AbsorbingValuable();
		absorbingValuable.meshCopy = CreateMeshCopy(target);
		absorbingValuable.startPosition = absorbingValuable.meshCopy.transform.position;
		absorbingValuable.startScale = absorbingValuable.meshCopy.transform.localScale;
		absorbingValuable.angularVelocity = target.rbAngularVelocity;
		absorbingValuable.timer = 0f;
		ValuableObject component = ((Component)target).GetComponent<ValuableObject>();
		absorbingValuable.valueToAdd = (Object.op_Implicit((Object)(object)component) ? component.dollarValueCurrent : 0f);
		Vector3 rbVelocity = target.rbVelocity;
		Vector3 val = insidePoint.position - absorbingValuable.startPosition;
		float magnitude = val.magnitude;
		Vector3 normalized = val.normalized;
		float num = Vector3.Dot(rbVelocity, normalized);
		if (num > 0.1f)
		{
			float num2 = magnitude / num;
			absorbingValuable.effectiveDuration = Mathf.Min(0.12f, num2);
		}
		else
		{
			absorbingValuable.effectiveDuration = 0.12f;
		}
		if (float.IsNaN(absorbingValuable.effectiveDuration) || float.IsInfinity(absorbingValuable.effectiveDuration))
		{
			absorbingValuable.effectiveDuration = 0.1f;
		}
		absorbingValuable.effectiveDuration = Mathf.Clamp(absorbingValuable.effectiveDuration, 0.03f, 5f);
		StoreMeshData(target);
		activeAbsorptions.Add(absorbingValuable);
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			target.DestroyPhysGrabObject();
		}
	}

	private void StoreMeshData(PhysGrabObject target)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		MeshRenderer[] componentsInChildren = ((Component)target).GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer val in componentsInChildren)
		{
			MeshFilter component = ((Component)val).GetComponent<MeshFilter>();
			if (Object.op_Implicit((Object)(object)component))
			{
				StoredValuableMesh storedValuableMesh = new StoredValuableMesh();
				storedValuableMesh.mesh = component.sharedMesh;
				storedValuableMesh.materials = ((Renderer)val).sharedMaterials;
				storedValuableMesh.scale = ((Component)val).transform.lossyScale;
				storedMeshes.Add(storedValuableMesh);
			}
		}
	}

	[PunRPC]
	private void StartAbsorbRPC(int targetViewID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return;
		}
		PhotonView val = PhotonView.Find(targetViewID);
		if (Object.op_Implicit((Object)(object)val))
		{
			PhysGrabObject component = ((Component)val).GetComponent<PhysGrabObject>();
			if (Object.op_Implicit((Object)(object)component))
			{
				StartAbsorbLocal(component);
			}
		}
	}

	private GameObject CreateMeshCopy(PhysGrabObject target)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected Obj, but got Unknown
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("AbsorbingMesh");
		val.transform.position = ((Component)target).transform.position;
		val.transform.rotation = ((Component)target).transform.rotation;
		val.transform.localScale = Vector3.one;
		MeshRenderer[] componentsInChildren = ((Component)target).GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer val2 in componentsInChildren)
		{
			MeshFilter component = ((Component)val2).GetComponent<MeshFilter>();
			if (Object.op_Implicit((Object)(object)component))
			{
				GameObject val3 = new GameObject("MeshCopy");
				val3.transform.SetParent(val.transform, false);
				val3.transform.position = ((Component)val2).transform.position;
				val3.transform.rotation = ((Component)val2).transform.rotation;
				val3.transform.localScale = ((Component)val2).transform.lossyScale;
				val3.AddComponent<MeshFilter>().sharedMesh = component.sharedMesh;
				((Renderer)val3.AddComponent<MeshRenderer>()).sharedMaterials = ((Renderer)val2).sharedMaterials;
			}
		}
		return val;
	}

	private void UpdateValue(float newValue)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("UpdateValueRPC", (RpcTarget)0, new object[1] { newValue });
			}
			else
			{
				UpdateValueRPC(newValue);
			}
		}
	}

	[PunRPC]
	private void UpdateValueRPC(float value, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(info))
		{
			currentValue = value;
			UpdateTextScreen();
			showValueDelayTimer = 1.3f;
		}
	}

	private void UpdateTextScreen()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		float num = currentValue / valueLimit * 100f;
		string newText = Mathf.RoundToInt(num) + "%";
		Color val = SemiFunc.ColorDifficultyGet(0f, 100f, num);
		if (Object.op_Implicit((Object)(object)textScreen))
		{
			textScreen.UpdateText(newText, val, val);
		}
	}

	public void ScreenMessage(Message message, MessageType messageType)
	{
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("ScreenMessageRPC", (RpcTarget)0, new object[2]
			{
				(int)message,
				(int)messageType
			});
		}
		else
		{
			ScreenMessageLocal(message, messageType);
		}
	}

	[PunRPC]
	private void ScreenMessageRPC(int message, int messageType)
	{
		ScreenMessageLocal((Message)message, (MessageType)messageType);
	}

	private void ScreenMessageLocal(Message message, MessageType messageType)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		Color val = messageType switch
		{
			MessageType.Error => Color.red, 
			MessageType.Success => Color.green, 
			_ => Color.yellow, 
		};
		string newText = message switch
		{
			Message.TooBig => localizedMessageTooBig?.GetLocalizedString() ?? "Too big", 
			Message.TooFull => localizedMessageTooFull?.GetLocalizedString() ?? "Too full", 
			Message.Extract => localizedMessageExtract?.GetLocalizedString() ?? "Extract", 
			_ => "", 
		};
		switch (messageType)
		{
		case MessageType.Error:
			if (soundMessageError != null)
			{
				soundMessageError.Play(((Component)this).transform.position);
			}
			break;
		case MessageType.Success:
			if (soundMessageSuccess != null)
			{
				soundMessageSuccess.Play(((Component)this).transform.position);
			}
			break;
		case MessageType.Neutral:
			if (soundMessageNeutral != null)
			{
				soundMessageNeutral.Play(((Component)this).transform.position);
			}
			break;
		}
		if (message == Message.Extract && currentState != State.OpenToExtractReady && currentState != State.ExtractReady && currentState != State.Extracting)
		{
			StateSet(State.OpenToExtractReady);
		}
		if (Object.op_Implicit((Object)(object)textScreen))
		{
			textScreen.UpdateText(newText, val, val);
		}
	}

	private void ScreenMessageClear()
	{
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("ScreenMessageClearRPC", (RpcTarget)0, Array.Empty<object>());
		}
		else
		{
			ScreenMessageClearLocal();
		}
	}

	[PunRPC]
	private void ScreenMessageClearRPC()
	{
		ScreenMessageClearLocal();
	}

	private void ScreenMessageClearLocal()
	{
		if (Object.op_Implicit((Object)(object)textScreen))
		{
			textScreen.SetValue((int)currentValue);
		}
	}

	private void ShowValueDelayLogic()
	{
		if (!(showValueDelayTimer > 0f))
		{
			return;
		}
		showValueDelayTimer -= Time.deltaTime;
		if (showValueDelayTimer <= 0f)
		{
			showValueDelayTimer = 0f;
			if (Object.op_Implicit((Object)(object)textScreen))
			{
				textScreen.SetValue((int)currentValue);
			}
		}
	}

	private void DestroyEffect()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (currentValue > 0f && Object.op_Implicit((Object)(object)WorldSpaceUIParent.instance))
		{
			WorldSpaceUIParent.instance.ValueLostCreate(((Component)this).transform.position, (int)currentValue);
		}
		if (Object.op_Implicit((Object)(object)breakParticles))
		{
			breakParticles.Play(true);
		}
	}

	public void ExplodeValuableBox()
	{
		DestroyEffect();
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			UpdateValue(0f);
		}
		else
		{
			currentValue = 0f;
			if (Object.op_Implicit((Object)(object)textScreen))
			{
				textScreen.SetValue(0);
			}
		}
		((Component)this).gameObject.SetActive(false);
	}

	public void TriggerExtractReady()
	{
		StateSet(State.OpenToExtractReady);
	}

	public void TriggerExtracting()
	{
		extractionValue = currentValue;
		StateSet(State.Extracting);
	}

	public void CancelExtraction()
	{
		if (currentState == State.OpenToExtractReady || currentState == State.ExtractReady || currentState == State.Extracting)
		{
			StateSet(State.Close);
		}
	}

	private void ExtractionPointCheck()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer() || !Object.op_Implicit((Object)(object)roomVolumeCheck))
		{
			return;
		}
		if (extractionCheckTimer > 0f)
		{
			extractionCheckTimer -= Time.deltaTime;
			return;
		}
		extractionCheckTimer = 0.5f;
		bool flag = inExtractionPoint;
		inExtractionPoint = roomVolumeCheck.inExtractionPoint;
		if (inExtractionPoint && !flag)
		{
			AddToExtractionHaul();
		}
		else if (!inExtractionPoint & flag)
		{
			RemoveFromExtractionHaul();
		}
	}

	private void AddToExtractionHaul()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && !RoundDirector.instance.valuableBoxHaulList.Contains(this))
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("AddToExtractionHaulRPC", (RpcTarget)0, Array.Empty<object>());
			}
			else
			{
				AddToExtractionHaulLocal();
			}
			ScreenMessage(Message.Extract, MessageType.Success);
		}
	}

	[PunRPC]
	private void AddToExtractionHaulRPC(PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(info))
		{
			AddToExtractionHaulLocal();
		}
	}

	private void AddToExtractionHaulLocal()
	{
		if (!RoundDirector.instance.valuableBoxHaulList.Contains(this))
		{
			RoundDirector.instance.valuableBoxHaulList.Add(this);
		}
	}

	private void RemoveFromExtractionHaul()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (RoundDirector.instance.valuableBoxHaulList.Contains(this))
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("RemoveFromExtractionHaulRPC", (RpcTarget)0, Array.Empty<object>());
			}
			else
			{
				RemoveFromExtractionHaulLocal();
			}
		}
		if (currentState == State.OpenToExtractReady || currentState == State.ExtractReady)
		{
			StateSet(State.Close);
		}
	}

	[PunRPC]
	private void RemoveFromExtractionHaulRPC(PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(info))
		{
			RemoveFromExtractionHaulLocal();
		}
	}

	private void RemoveFromExtractionHaulLocal()
	{
		if (RoundDirector.instance.valuableBoxHaulList.Contains(this))
		{
			RoundDirector.instance.valuableBoxHaulList.Remove(this);
		}
	}

	public void OnExtracted()
	{
		RemoveFromExtractionHaul();
		inExtractionPoint = false;
	}

	private void CleanUpDestroyedClaims()
	{
		List<PhysGrabObject> list = new List<PhysGrabObject>();
		foreach (PhysGrabObject claimedValuable in claimedValuables)
		{
			if ((Object)(object)claimedValuable == (Object)null)
			{
				list.Add(claimedValuable);
			}
		}
		foreach (PhysGrabObject item in list)
		{
			claimedValuables.Remove(item);
		}
	}

	public ItemValuableBox()
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
	}
}
