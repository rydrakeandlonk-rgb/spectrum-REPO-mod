using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ValuableDirector : MonoBehaviour
{
	public enum ValuableDebug
	{
		Normal,
		All,
		None
	}

	public enum CosmeticWorldObjectDebug
	{
		Normal,
		All,
		None
	}

	private enum VolumeSetupState
	{
		Idle,
		Running,
		Ready,
		Cancelled
	}

	[Serializable]
	public class CosmeticWorldObjectSetup
	{
		public PrefabRef prefab;

		public ValuableVolume.Type volume;

		public SemiFunc.Rarity rarity;

		public AnimationCurve chanceCurve;
	}

	public static ValuableDirector instance;

	private PhotonView PhotonView;

	internal ValuableDebug valuableDebug;

	internal CosmeticWorldObjectDebug cosmeticWorldObjectDebug;

	internal bool setupComplete;

	internal bool valuablesSpawned;

	private VolumeSetupState volumeSetupState;

	private const double volumeSetupFrameBudget = 0.004;

	private const double spawnFrameTime = 0.002;

	private List<Player> valuableSpawnPlayerReadyList = new List<Player>();

	internal int valuableSpawnPlayerReady;

	internal int valuableSpawnAmount;

	internal int valuableTargetAmount = -1;

	private List<Player> switchSetupPlayerReadyList = new List<Player>();

	internal int switchSetupPlayerReady;

	internal string resourcePath = "Valuables/";

	public AnimationCurve totalMaxValueCurve1;

	public AnimationCurve totalMaxValueCurve2;

	private float totalMaxValue;

	private float totalCurrentValue;

	private int totalMaxAmount = 50;

	[Space(20f)]
	public AnimationCurve tinyMaxAmountCurve1;

	public AnimationCurve tinyMaxAmountCurve2;

	public int tinyChance;

	private int tinyMaxAmount;

	internal string tinyPath = "01 Tiny";

	private List<PrefabRef> tinyValuables = new List<PrefabRef>();

	private List<ValuableVolume> tinyVolumes = new List<ValuableVolume>();

	[Space]
	public AnimationCurve smallMaxAmountCurve1;

	public AnimationCurve smallMaxAmountCurve2;

	public int smallChance;

	private int smallMaxAmount;

	internal string smallPath = "02 Small";

	private List<PrefabRef> smallValuables = new List<PrefabRef>();

	private List<ValuableVolume> smallVolumes = new List<ValuableVolume>();

	[Space]
	public AnimationCurve mediumMaxAmountCurve1;

	public AnimationCurve mediumMaxAmountCurve2;

	public int mediumChance;

	private int mediumMaxAmount;

	internal string mediumPath = "03 Medium";

	private List<PrefabRef> mediumValuables = new List<PrefabRef>();

	private List<ValuableVolume> mediumVolumes = new List<ValuableVolume>();

	[Space]
	public AnimationCurve bigMaxAmountCurve1;

	public AnimationCurve bigMaxAmountCurve2;

	public int bigChance;

	private int bigMaxAmount;

	internal string bigPath = "04 Big";

	private List<PrefabRef> bigValuables = new List<PrefabRef>();

	private List<ValuableVolume> bigVolumes = new List<ValuableVolume>();

	[Space]
	public AnimationCurve wideMaxAmountCurve1;

	public AnimationCurve wideMaxAmountCurve2;

	public int wideChance;

	private int wideMaxAmount;

	internal string widePath = "05 Wide";

	private List<PrefabRef> wideValuables = new List<PrefabRef>();

	private List<ValuableVolume> wideVolumes = new List<ValuableVolume>();

	[Space]
	public AnimationCurve tallMaxAmountCurve1;

	public AnimationCurve tallMaxAmountCurve2;

	public int tallChance;

	private int tallMaxAmount;

	internal string tallPath = "06 Tall";

	private List<PrefabRef> tallValuables = new List<PrefabRef>();

	private List<ValuableVolume> tallVolumes = new List<ValuableVolume>();

	[Space]
	public AnimationCurve veryTallMaxAmountCurve1;

	public AnimationCurve veryTallMaxAmountCurve2;

	public int veryTallChance;

	private int veryTallMaxAmount;

	internal string veryTallPath = "07 Very Tall";

	private List<PrefabRef> veryTallValuables = new List<PrefabRef>();

	private List<ValuableVolume> veryTallVolumes = new List<ValuableVolume>();

	[Space(20f)]
	public List<ValuableObject> valuableList = new List<ValuableObject>();

	public AnimationCurve cosmeticWorldObjectsSpawnCurve;

	public int cosmeticWorldObjectsLevelLoop;

	public int cosmeticWorldObjectsLevelLoopsMax;

	[Space]
	public List<CosmeticWorldObjectSetup> cosmeticWorldObjectSetups;

	private List<Player> cosmeticWorldObjectSpawnPlayerReadyList = new List<Player>();

	internal int cosmeticWorldObjectSpawnPlayerReady;

	internal int cosmeticWorldObjectSpawnAmount;

	internal int cosmeticWorldObjectTargetAmount = -1;

	private float softLockCheckTimer;

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void RuntimeInitializeOnLoad()
	{
		instance = null;
	}

	private void Awake()
	{
		instance = this;
		PhotonView = ((Component)this).GetComponent<PhotonView>();
	}

	private void Start()
	{
		if (SemiFunc.IsMultiplayer() && !SemiFunc.IsMasterClient())
		{
			((MonoBehaviour)this).StartCoroutine(SetupClient());
		}
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			((MonoBehaviour)this).StartCoroutine(SoftLockLogic());
		}
	}

	private IEnumerator SoftLockLogic()
	{
		if (!LevelGenerator.Instance.Generated)
		{
			yield return (object)new WaitForSeconds(0.1f);
		}
		while (true)
		{
			yield return (object)new WaitForSeconds(5f);
			if (!SoftLockExtractionCheck())
			{
				continue;
			}
			float _totalValue = 0f;
			List<ValuableObject> list = Object.FindObjectsByType<ValuableObject>((FindObjectsInactive)0, (FindObjectsSortMode)0).ToList();
			foreach (ValuableObject item in list)
			{
				if (Object.op_Implicit((Object)(object)item) && (!Object.op_Implicit((Object)(object)item.physGrabObject) || !item.physGrabObject.dead))
				{
					_totalValue += item.dollarValueCurrent;
					yield return null;
				}
			}
			if (!(_totalValue < (float)RoundDirector.instance.extractionHaulGoal))
			{
				continue;
			}
			float _spawnTargetValue = (float)RoundDirector.instance.extractionHaulGoal - _totalValue;
			List<ValuableVolume> _valuableVolumes = Object.FindObjectsByType<ValuableVolume>((FindObjectsInactive)0, (FindObjectsSortMode)0).ToList();
			_valuableVolumes = _valuableVolumes.FindAll((ValuableVolume x) => x.VolumeType <= ValuableVolume.Type.Big);
			if (_valuableVolumes.Count <= 0)
			{
				continue;
			}
			int _volumeIndex = 0;
			while (_spawnTargetValue > 0f && SoftLockExtractionCheck())
			{
				ValuableVolume valuableVolume = _valuableVolumes[_volumeIndex];
				if (Object.op_Implicit((Object)(object)valuableVolume))
				{
					bool flag = false;
					foreach (PlayerAvatar item2 in SemiFunc.PlayerGetList())
					{
						if (Object.op_Implicit((Object)(object)item2) && Vector3.Distance(((Component)item2).transform.position, ((Component)valuableVolume).transform.position) < 20f)
						{
							flag = true;
							break;
						}
					}
					if (!flag && valuableVolume.OverlapBoxCheck(SemiFunc.LayerMaskGetPhysGrabObject()).Length == 0)
					{
						PrefabRef prefabRef = null;
						switch (valuableVolume.VolumeType)
						{
						case ValuableVolume.Type.Tiny:
							if (tinyValuables.Count > 0)
							{
								prefabRef = tinyValuables[Random.Range(0, tinyValuables.Count)];
							}
							break;
						case ValuableVolume.Type.Small:
							if (smallValuables.Count > 0)
							{
								prefabRef = smallValuables[Random.Range(0, smallValuables.Count)];
							}
							break;
						case ValuableVolume.Type.Medium:
							if (mediumValuables.Count > 0)
							{
								prefabRef = mediumValuables[Random.Range(0, mediumValuables.Count)];
							}
							break;
						case ValuableVolume.Type.Big:
							if (bigValuables.Count > 0)
							{
								prefabRef = bigValuables[Random.Range(0, bigValuables.Count)];
							}
							break;
						}
						if (prefabRef != null)
						{
							ValuableObject valuableObject = SpawnValuable(prefabRef, valuableVolume);
							if (!Object.op_Implicit((Object)(object)valuableObject))
							{
								break;
							}
							_spawnTargetValue -= valuableObject.dollarValueCurrent;
						}
					}
				}
				yield return (object)new WaitForSeconds(0.1f);
				_volumeIndex++;
				if (_volumeIndex >= _valuableVolumes.Count)
				{
					_volumeIndex = 0;
				}
			}
		}
	}

	private bool SoftLockExtractionCheck()
	{
		if (valuableDebug == ValuableDebug.Normal && RoundDirector.instance.extractionPointActive && Object.op_Implicit((Object)(object)RoundDirector.instance.extractionPointCurrent) && RoundDirector.instance.extractionPointCurrent.currentState == ExtractionPoint.State.Active)
		{
			return true;
		}
		return false;
	}

	public IEnumerator SetupClient()
	{
		while (valuableTargetAmount == -1)
		{
			yield return (object)new WaitForSeconds(0.1f);
		}
		while (valuableSpawnAmount < valuableTargetAmount)
		{
			yield return (object)new WaitForSeconds(0.1f);
		}
		PhotonView.RPC("PlayerReadyValuableRPC", (RpcTarget)2, Array.Empty<object>());
		while (cosmeticWorldObjectTargetAmount == -1)
		{
			yield return (object)new WaitForSeconds(0.1f);
		}
		while (cosmeticWorldObjectSpawnAmount < cosmeticWorldObjectTargetAmount)
		{
			yield return (object)new WaitForSeconds(0.1f);
		}
		PhotonView.RPC("PlayerReadyCosmeticWorldObjectRPC", (RpcTarget)2, Array.Empty<object>());
	}

	public IEnumerator SetupHost()
	{
		if (SemiFunc.RunGetDifficultyMultiplier2() > 0f && !SemiFunc.RunIsArena())
		{
			float num = SemiFunc.RunGetDifficultyMultiplier2();
			totalMaxValue = Mathf.RoundToInt(totalMaxValueCurve2.Evaluate(num));
			tinyMaxAmount = Mathf.RoundToInt(tinyMaxAmountCurve2.Evaluate(num));
			smallMaxAmount = Mathf.RoundToInt(smallMaxAmountCurve2.Evaluate(num));
			mediumMaxAmount = Mathf.RoundToInt(mediumMaxAmountCurve2.Evaluate(num));
			bigMaxAmount = Mathf.RoundToInt(bigMaxAmountCurve2.Evaluate(num));
			wideMaxAmount = Mathf.RoundToInt(wideMaxAmountCurve2.Evaluate(num));
			tallMaxAmount = Mathf.RoundToInt(tallMaxAmountCurve2.Evaluate(num));
			veryTallMaxAmount = Mathf.RoundToInt(veryTallMaxAmountCurve2.Evaluate(num));
		}
		else
		{
			float num2 = SemiFunc.RunGetDifficultyMultiplier1();
			if (SemiFunc.RunIsArena())
			{
				num2 = 0.75f;
			}
			totalMaxValue = Mathf.RoundToInt(totalMaxValueCurve1.Evaluate(num2));
			tinyMaxAmount = Mathf.RoundToInt(tinyMaxAmountCurve1.Evaluate(num2));
			smallMaxAmount = Mathf.RoundToInt(smallMaxAmountCurve1.Evaluate(num2));
			mediumMaxAmount = Mathf.RoundToInt(mediumMaxAmountCurve1.Evaluate(num2));
			bigMaxAmount = Mathf.RoundToInt(bigMaxAmountCurve1.Evaluate(num2));
			wideMaxAmount = Mathf.RoundToInt(wideMaxAmountCurve1.Evaluate(num2));
			tallMaxAmount = Mathf.RoundToInt(tallMaxAmountCurve1.Evaluate(num2));
			veryTallMaxAmount = Mathf.RoundToInt(veryTallMaxAmountCurve1.Evaluate(num2));
		}
		if (SemiFunc.RunIsArena())
		{
			totalMaxAmount /= 2;
			tinyMaxAmount /= 3;
			smallMaxAmount /= 3;
			mediumMaxAmount /= 3;
			bigMaxAmount /= 3;
			wideMaxAmount /= 2;
			tallMaxAmount /= 2;
			veryTallMaxAmount /= 2;
		}
		foreach (LevelValuables valuablePreset in LevelGenerator.Instance.Level.ValuablePresets)
		{
			tinyValuables.AddRange(valuablePreset.tiny);
			smallValuables.AddRange(valuablePreset.small);
			mediumValuables.AddRange(valuablePreset.medium);
			bigValuables.AddRange(valuablePreset.big);
			wideValuables.AddRange(valuablePreset.wide);
			tallValuables.AddRange(valuablePreset.tall);
			veryTallValuables.AddRange(valuablePreset.veryTall);
		}
		if (Object.op_Implicit((Object)(object)GameManager.instance) && GameManager.instance.IsWIPActive(DebugCommandHandler.WIPTypes.Valuable))
		{
			tinyValuables = GetPossibleValuables(tinyValuables);
			smallValuables = GetPossibleValuables(smallValuables);
			mediumValuables = GetPossibleValuables(mediumValuables);
			bigValuables = GetPossibleValuables(bigValuables);
			wideValuables = GetPossibleValuables(wideValuables);
			tallValuables = GetPossibleValuables(tallValuables);
			veryTallValuables = GetPossibleValuables(veryTallValuables);
		}
		List<ValuableVolume> list = Object.FindObjectsByType<ValuableVolume>((FindObjectsSortMode)0).ToList();
		tinyVolumes = list.FindAll((ValuableVolume x) => x.VolumeType == ValuableVolume.Type.Tiny);
		tinyVolumes.Shuffle();
		smallVolumes = list.FindAll((ValuableVolume x) => x.VolumeType == ValuableVolume.Type.Small);
		smallVolumes.Shuffle();
		mediumVolumes = list.FindAll((ValuableVolume x) => x.VolumeType == ValuableVolume.Type.Medium);
		mediumVolumes.Shuffle();
		bigVolumes = list.FindAll((ValuableVolume x) => x.VolumeType == ValuableVolume.Type.Big);
		bigVolumes.Shuffle();
		wideVolumes = list.FindAll((ValuableVolume x) => x.VolumeType == ValuableVolume.Type.Wide);
		wideVolumes.Shuffle();
		tallVolumes = list.FindAll((ValuableVolume x) => x.VolumeType == ValuableVolume.Type.Tall);
		tallVolumes.Shuffle();
		veryTallVolumes = list.FindAll((ValuableVolume x) => x.VolumeType == ValuableVolume.Type.VeryTall);
		veryTallVolumes.Shuffle();
		if (valuableDebug == ValuableDebug.All)
		{
			totalMaxAmount = list.Count;
			totalMaxValue = 99999f;
			tinyMaxAmount = tinyVolumes.Count;
			smallMaxAmount = smallVolumes.Count;
			mediumMaxAmount = mediumVolumes.Count;
			bigMaxAmount = bigVolumes.Count;
			wideMaxAmount = wideVolumes.Count;
			tallMaxAmount = tallVolumes.Count;
			veryTallMaxAmount = veryTallVolumes.Count;
		}
		if (valuableDebug == ValuableDebug.None || LevelGenerator.Instance.Level.ValuablePresets.Count <= 0)
		{
			totalMaxAmount = 0;
			tinyMaxAmount = 0;
			smallMaxAmount = 0;
			mediumMaxAmount = 0;
			bigMaxAmount = 0;
			wideMaxAmount = 0;
			tallMaxAmount = 0;
			veryTallMaxAmount = 0;
		}
		valuableTargetAmount = 0;
		string[] _names = new string[7] { "Tiny", "Small", "Medium", "Big", "Wide", "Tall", "Very Tall" };
		int[] _maxAmount = new int[7] { tinyMaxAmount, smallMaxAmount, mediumMaxAmount, bigMaxAmount, wideMaxAmount, tallMaxAmount, veryTallMaxAmount };
		List<ValuableVolume>[] _volumes = new List<ValuableVolume>[7] { tinyVolumes, smallVolumes, mediumVolumes, bigVolumes, wideVolumes, tallVolumes, veryTallVolumes };
		_ = new string[7] { tinyPath, smallPath, mediumPath, bigPath, widePath, tallPath, veryTallPath };
		int[] _chance = new int[7] { tinyChance, smallChance, mediumChance, bigChance, wideChance, tallChance, veryTallChance };
		List<PrefabRef>[] _valuables = new List<PrefabRef>[7] { tinyValuables, smallValuables, mediumValuables, bigValuables, wideValuables, tallValuables, veryTallValuables };
		int[] _volumeIndex = new int[7];
		yield return LevelGenerator.Instance.PrefabsPrepare(_valuables.SelectMany((List<PrefabRef> _list) => _list));
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		for (int _i = 0; _i < totalMaxAmount; _i++)
		{
			float num3 = -1f;
			int num4 = -1;
			for (int num5 = 0; num5 < _names.Length; num5++)
			{
				if (_volumeIndex[num5] < _maxAmount[num5] && _volumeIndex[num5] < _volumes[num5].Count && _valuables[num5].Count != 0)
				{
					int num6 = Random.Range(0, _chance[num5]);
					if ((float)num6 > num3)
					{
						num3 = num6;
						num4 = num5;
					}
				}
			}
			if (num4 == -1)
			{
				break;
			}
			ValuableVolume volume = _volumes[num4][_volumeIndex[num4]];
			PrefabRef valuable = _valuables[num4][Random.Range(0, _valuables[num4].Count)];
			SpawnValuable(valuable, volume);
			_volumeIndex[num4]++;
			if (Time.realtimeSinceStartupAsDouble - realtimeSinceStartupAsDouble >= 0.002)
			{
				yield return null;
				realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
			}
		}
		if (valuableTargetAmount < totalMaxAmount && Object.op_Implicit((Object)(object)DebugComputerCheck.instance) && (DebugComputerCheck.instance.DebugDisable || !DebugComputerCheck.instance.LevelDebug || !DebugComputerCheck.instance.ModuleOverrideActive || DebugComputerCheck.instance.ModuleOverride == null))
		{
			for (int num7 = 0; num7 < _names.Length; num7++)
			{
				if (_volumeIndex[num7] < _maxAmount[num7])
				{
					Debug.LogError((object)("Could not spawn enough ''" + _names[num7] + "'' valuables!"));
				}
			}
		}
		if (SemiFunc.IsMultiplayer())
		{
			PhotonView.RPC("ValuablesTargetSetRPC", (RpcTarget)0, new object[1] { valuableTargetAmount });
		}
		valuableSpawnPlayerReady++;
		while (!SetupPlayersReady(valuableSpawnPlayerReadyList))
		{
			yield return null;
		}
		cosmeticWorldObjectTargetAmount = 0;
		if (cosmeticWorldObjectDebug == CosmeticWorldObjectDebug.Normal && SemiFunc.RunIsLevel())
		{
			yield return LevelGenerator.Instance.PrefabsPrepare(cosmeticWorldObjectSetups.Select((CosmeticWorldObjectSetup _setup) => _setup.prefab));
			int _i = CosmeticWorldObjectLevelLoopsGet();
			int _cosmeticWorldObjectLevelLoopsClamped = CosmeticWorldObjectLevelLoopsClampedGet();
			double realtimeSinceStartupAsDouble2 = Time.realtimeSinceStartupAsDouble;
			for (int _i2 = 0; _i2 <= _cosmeticWorldObjectLevelLoopsClamped; _i2++)
			{
				float num8 = ((_i2 >= _i) ? ((float)(RunManager.instance.levelsCompleted % cosmeticWorldObjectsLevelLoop) / (float)cosmeticWorldObjectsLevelLoop) : 1f);
				int num9 = Mathf.RoundToInt(cosmeticWorldObjectsSpawnCurve.Evaluate(num8) * 100f);
				if (RunManager.instance.cosmeticWorldObjectBadLuckCount > 0)
				{
					num9 += Mathf.RoundToInt((float)RunManager.instance.cosmeticWorldObjectBadLuckCount * 25f);
				}
				if (Random.Range(0, 100) >= num9)
				{
					continue;
				}
				CosmeticWorldObjectSetup cosmeticWorldObjectSetup = null;
				float num10 = -1f;
				foreach (CosmeticWorldObjectSetup cosmeticWorldObjectSetup2 in cosmeticWorldObjectSetups)
				{
					int num11 = Mathf.RoundToInt(cosmeticWorldObjectSetup2.chanceCurve.Evaluate(num8) * 100f);
					int num12 = Random.Range(0, num11);
					if ((float)num12 > num10)
					{
						num10 = num12;
						cosmeticWorldObjectSetup = cosmeticWorldObjectSetup2;
					}
				}
				if (cosmeticWorldObjectSetup != null)
				{
					SpawnCosmeticWorldObject(cosmeticWorldObjectSetup.rarity, _volumeIndex, _volumes);
					if (Time.realtimeSinceStartupAsDouble - realtimeSinceStartupAsDouble2 >= 0.002)
					{
						yield return null;
						realtimeSinceStartupAsDouble2 = Time.realtimeSinceStartupAsDouble;
					}
				}
			}
		}
		else if (cosmeticWorldObjectDebug == CosmeticWorldObjectDebug.All)
		{
			SpawnCosmeticWorldObject(SemiFunc.Rarity.Common, _volumeIndex, _volumes);
			yield return null;
			SpawnCosmeticWorldObject(SemiFunc.Rarity.Uncommon, _volumeIndex, _volumes);
			yield return null;
			SpawnCosmeticWorldObject(SemiFunc.Rarity.Rare, _volumeIndex, _volumes);
			yield return null;
			SpawnCosmeticWorldObject(SemiFunc.Rarity.UltraRare, _volumeIndex, _volumes);
			yield return null;
		}
		if (SemiFunc.IsMultiplayer())
		{
			PhotonView.RPC("CosmeticWorldObjectTargetSetRPC", (RpcTarget)0, new object[1] { cosmeticWorldObjectTargetAmount });
		}
		yield return null;
		cosmeticWorldObjectSpawnPlayerReady++;
		while (!SetupPlayersReady(cosmeticWorldObjectSpawnPlayerReadyList))
		{
			yield return null;
		}
		VolumesAndSwitchSetup();
		while (!VolumesAndSwitchReady())
		{
			yield return null;
		}
		setupComplete = true;
	}

	private List<PrefabRef> GetPossibleValuables(List<PrefabRef> _valuables)
	{
		List<PrefabRef> list = _valuables.Where((PrefabRef x) => x.IsFinalized).ToList();
		if (list.Count <= 0)
		{
			return _valuables;
		}
		return list;
	}

	private ValuableObject SpawnValuable(PrefabRef _valuable, ValuableVolume _volume)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = ((GameManager.instance.gameMode != 0) ? PhotonNetwork.InstantiateRoomObject(_valuable.ResourcePath, ((Component)_volume).transform.position, ((Component)_volume).transform.rotation, (byte)0, (object[])null) : Object.Instantiate<GameObject>(_valuable.Prefab, ((Component)_volume).transform.position, ((Component)_volume).transform.rotation));
		if (!Object.op_Implicit((Object)(object)val))
		{
			Debug.LogError((object)("couldn't spawn valuable " + _valuable.ResourcePath + " in module " + SemiFunc.ModuleNameGet(((Component)_volume).transform) + "... check its prefab ref in the valuable list... it needs a prefab inside a Resources folder or a loaded asset bundle... skipping it so the level can finish loading"), (Object)(object)_volume);
			return null;
		}
		ValuableObject component = val.GetComponent<ValuableObject>();
		component.DollarValueSetLogic();
		valuableTargetAmount++;
		totalCurrentValue += component.dollarValueCurrent * 0.001f;
		if (totalCurrentValue > totalMaxValue)
		{
			totalMaxAmount = valuableTargetAmount;
		}
		return component;
	}

	[PunRPC]
	private void ValuablesTargetSetRPC(int _amount, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			valuableTargetAmount = _amount;
		}
	}

	[PunRPC]
	private void PlayerReadyValuableRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && !valuableSpawnPlayerReadyList.Contains(_info.Sender))
		{
			valuableSpawnPlayerReadyList.Add(_info.Sender);
			valuableSpawnPlayerReady++;
		}
	}

	public int CosmeticWorldObjectLevelLoopsGet()
	{
		return RunManager.instance.levelsCompleted / cosmeticWorldObjectsLevelLoop;
	}

	public int CosmeticWorldObjectLevelLoopsClampedGet()
	{
		return Mathf.Clamp(CosmeticWorldObjectLevelLoopsGet(), 0, cosmeticWorldObjectsLevelLoopsMax);
	}

	private void SpawnCosmeticWorldObject(SemiFunc.Rarity _cosmeticRarity, int[] _volumeIndex, List<ValuableVolume>[] _volumes)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		int volume = (int)cosmeticWorldObjectSetups[(int)_cosmeticRarity].volume;
		if (_volumeIndex[volume] < _volumes[volume].Count)
		{
			ValuableVolume valuableVolume = _volumes[volume][_volumeIndex[volume]];
			_volumeIndex[volume]++;
			PrefabRef prefab = cosmeticWorldObjectSetups[(int)_cosmeticRarity].prefab;
			GameObject val = ((GameManager.instance.gameMode != 0) ? PhotonNetwork.InstantiateRoomObject(prefab.ResourcePath, ((Component)valuableVolume).transform.position, ((Component)valuableVolume).transform.rotation, (byte)0, (object[])null) : Object.Instantiate<GameObject>(prefab.Prefab, ((Component)valuableVolume).transform.position, ((Component)valuableVolume).transform.rotation));
			if (!Object.op_Implicit((Object)(object)val))
			{
				Debug.LogError((object)("couldn't spawn cosmetic world object " + prefab.ResourcePath + " in module " + SemiFunc.ModuleNameGet(((Component)valuableVolume).transform) + "... check its prefab ref in the cosmetic world object setups... it needs a prefab inside a Resources folder... skipping it so the level can finish loading"), (Object)(object)valuableVolume);
			}
			else
			{
				cosmeticWorldObjectTargetAmount++;
				RunManager.instance.cosmeticWorldObjectBadLuckCount = 0;
			}
		}
	}

	[PunRPC]
	private void CosmeticWorldObjectTargetSetRPC(int _amount, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			cosmeticWorldObjectTargetAmount = _amount;
		}
	}

	[PunRPC]
	private void PlayerReadyCosmeticWorldObjectRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && !cosmeticWorldObjectSpawnPlayerReadyList.Contains(_info.Sender))
		{
			cosmeticWorldObjectSpawnPlayerReadyList.Add(_info.Sender);
			cosmeticWorldObjectSpawnPlayerReady++;
		}
	}

	public void VolumesAndSwitchSetup()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (GameManager.instance.gameMode == 0)
		{
			VolumesAndSwitchSetupRPC();
		}
		else
		{
			PhotonView.RPC("VolumesAndSwitchSetupRPC", (RpcTarget)0, Array.Empty<object>());
		}
	}

	[PunRPC]
	private void VolumesAndSwitchSetupRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && volumeSetupState == VolumeSetupState.Idle && VolumesAndSwitchSetupActive())
		{
			volumeSetupState = VolumeSetupState.Running;
			((MonoBehaviour)this).StartCoroutine(VolumesAndSwitchSetupRoutine());
		}
	}

	private IEnumerator VolumesAndSwitchSetupRoutine()
	{
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		ValuableVolume[] array = Object.FindObjectsByType<ValuableVolume>((FindObjectsInactive)1, (FindObjectsSortMode)0);
		ValuableVolume[] array2 = array;
		foreach (ValuableVolume _volume in array2)
		{
			if (Time.realtimeSinceStartupAsDouble - realtimeSinceStartupAsDouble >= 0.004)
			{
				yield return null;
				realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
			}
			if (!VolumesAndSwitchSetupActive())
			{
				yield break;
			}
			if (Object.op_Implicit((Object)(object)_volume))
			{
				try
				{
					_volume.Setup();
				}
				catch (Exception ex)
				{
					Debug.LogError((object)("valuable volume " + ((Object)_volume).name + " in module " + SemiFunc.ModuleNameGet(((Component)_volume).transform) + " broke during setup... the exception below shows where... skipping it so loading can finish"), (Object)(object)_volume);
					Debug.LogException(ex, (Object)(object)_volume);
				}
			}
		}
		ValuablePropSwitch[] array3 = Object.FindObjectsByType<ValuablePropSwitch>((FindObjectsInactive)1, (FindObjectsSortMode)0);
		ValuablePropSwitch[] array4 = array3;
		foreach (ValuablePropSwitch _switch in array4)
		{
			if (Time.realtimeSinceStartupAsDouble - realtimeSinceStartupAsDouble >= 0.004)
			{
				yield return null;
				realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
			}
			if (!VolumesAndSwitchSetupActive())
			{
				yield break;
			}
			if (Object.op_Implicit((Object)(object)_switch))
			{
				try
				{
					_switch.Setup();
				}
				catch (Exception ex2)
				{
					Debug.LogError((object)("valuable prop switch " + ((Object)_switch).name + " in module " + SemiFunc.ModuleNameGet(((Component)_switch).transform) + " broke during setup... the exception below shows where... skipping it so loading can finish"), (Object)(object)_switch);
					Debug.LogException(ex2, (Object)(object)_switch);
				}
			}
		}
		if (VolumesAndSwitchSetupActive())
		{
			volumeSetupState = VolumeSetupState.Ready;
			if (GameManager.instance.gameMode == 0)
			{
				VolumesAndSwitchReadyRPC();
			}
			else
			{
				PhotonView.RPC("VolumesAndSwitchReadyRPC", (RpcTarget)2, Array.Empty<object>());
			}
		}
	}

	private bool VolumesAndSwitchSetupActive()
	{
		if (((Behaviour)this).isActiveAndEnabled && (!Object.op_Implicit((Object)(object)NetworkManager.instance) || !NetworkManager.instance.leavePhotonRoom))
		{
			return true;
		}
		volumeSetupState = VolumeSetupState.Cancelled;
		return false;
	}

	private bool VolumesAndSwitchReady()
	{
		if (volumeSetupState == VolumeSetupState.Ready)
		{
			return SetupPlayersReady(switchSetupPlayerReadyList);
		}
		return false;
	}

	private bool SetupPlayersReady(List<Player> _readyPlayers)
	{
		if (!SemiFunc.IsMultiplayer())
		{
			return true;
		}
		Room currentRoom = PhotonNetwork.CurrentRoom;
		if (currentRoom == null || currentRoom.PlayerCount == 0)
		{
			return false;
		}
		foreach (Player value in currentRoom.Players.Values)
		{
			if (!value.IsLocal && !_readyPlayers.Contains(value))
			{
				return false;
			}
		}
		return true;
	}

	[PunRPC]
	private void VolumesAndSwitchReadyRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && !switchSetupPlayerReadyList.Contains(_info.Sender))
		{
			switchSetupPlayerReadyList.Add(_info.Sender);
			switchSetupPlayerReady++;
		}
	}
}
