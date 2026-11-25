using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class ValuableWizardCrystal : MonoBehaviour
{
	public PrefabRef affectPrefab;

	[Space]
	public float affectRadius = 3f;

	public float affectTimeMin = 5f;

	public float affectTimeMax = 15f;

	[Space]
	public float purpleSmokeRadius = 18f;

	public float purpleSmokeTimeMin = 3.5f;

	public float purpleSmokeTimeMax = 5f;

	public int purpleSmokeParticles = 36;

	[Space]
	public Sound rollStaffHitSound;

	[Space]
	public float floorSearchDistance = 6f;

	public float floorContactMemoryTime = 0.25f;

	private PhysGrabObject physGrabObject;

	private PhysGrabObjectImpactDetector impactDetector;

	private PhotonView photonView;

	private Vector3 floorHitPosition;

	private Vector3 floorHitNormal = Vector3.up;

	private float floorContactTimer;

	private void Awake()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected Obj, but got Unknown
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		if (Object.op_Implicit((Object)(object)impactDetector))
		{
			impactDetector.onDestroy.AddListener((UnityAction)Destroyed);
		}
	}

	private void Update()
	{
		if (floorContactTimer > 0f)
		{
			floorContactTimer -= Time.deltaTime;
		}
	}

	private void OnCollisionEnter(Collision _collision)
	{
		FloorContactSet(_collision);
	}

	private void OnCollisionStay(Collision _collision)
	{
		FloorContactSet(_collision);
	}

	private void OnDestroy()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected Obj, but got Unknown
		if (Object.op_Implicit((Object)(object)impactDetector))
		{
			impactDetector.onDestroy.RemoveListener((UnityAction)Destroyed);
		}
	}

	private void Destroyed()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = (Object.op_Implicit((Object)(object)physGrabObject) ? ((Component)physGrabObject).transform.TransformPoint(physGrabObject.midPointOffset) : ((Component)this).transform.position);
		PurpleSmokeSpawn(position);
		rollStaffHitSound.Play(position);
		FloorHitGet(out var _position, out var _);
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			List<PlayerAvatar> players = SemiFunc.PlayerGetAllPlayerAvatarWithinRange(affectRadius, _position);
			List<PhysGrabObject> physObjects = SemiFunc.PhysGrabObjectGetAllWithinRange(affectRadius, _position);
			if (SemiFunc.IsMultiplayer())
			{
				AffectMultiplayer(players, physObjects, _position);
			}
			else
			{
				AffectSingleplayer(players, physObjects, _position);
			}
		}
	}

	private void AffectMultiplayer(List<PlayerAvatar> _players, List<PhysGrabObject> _physObjects, Vector3 _position)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)photonView))
		{
			return;
		}
		List<int> list = new List<int>();
		List<float> list2 = new List<float>();
		List<float> list3 = new List<float>();
		List<float> list4 = new List<float>();
		List<float> list5 = new List<float>();
		foreach (PlayerAvatar _player in _players)
		{
			if (Object.op_Implicit((Object)(object)_player))
			{
				AffectTargetAdd(_player.photonView.ViewID, ((Component)_player).transform.position, _position, 0.5f, list, list2, list3, list4, list5);
			}
		}
		foreach (PhysGrabObject _physObject in _physObjects)
		{
			if (Object.op_Implicit((Object)(object)_physObject) && !((Object)(object)_physObject == (Object)(object)physGrabObject) && !_physObject.isPlayer && !_physObject.dead)
			{
				AffectTargetAdd(_physObject.photonView.ViewID, ((Component)_physObject).transform.position, _position, 1f, list, list2, list3, list4, list5);
			}
		}
		photonView.RPC("CrystalDestroyedEffectRPC", (RpcTarget)0, new object[6]
		{
			list.ToArray(),
			list2.ToArray(),
			list3.ToArray(),
			list4.ToArray(),
			list5.ToArray(),
			_position
		});
	}

	private void AffectTargetAdd(int _viewID, Vector3 _targetPosition, Vector3 _origin, float _timeMultiplier, List<int> _viewIDs, List<float> _times, List<float> _directionX, List<float> _directionY, List<float> _directionZ)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		if (_viewID > 0 && !_viewIDs.Contains(_viewID))
		{
			Vector3 val = AffectDirection(_targetPosition, _origin);
			_viewIDs.Add(_viewID);
			_times.Add(Random.Range(affectTimeMin, affectTimeMax) * _timeMultiplier);
			_directionX.Add(val.x);
			_directionY.Add(val.y);
			_directionZ.Add(val.z);
		}
	}

	[PunRPC]
	public void CrystalDestroyedEffectRPC(int[] _viewIDs, float[] _times, float[] _directionX, float[] _directionY, float[] _directionZ, Vector3 _position, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info) || affectPrefab == null || !affectPrefab.IsValid())
		{
			return;
		}
		for (int i = 0; i < _viewIDs.Length; i++)
		{
			PhotonView val = PhotonView.Find(_viewIDs[i]);
			if (Object.op_Implicit((Object)(object)val))
			{
				GameObject val2 = Object.Instantiate<GameObject>(affectPrefab.Prefab, ((Component)val).transform.position, Quaternion.identity);
				SemiAffect component = val2.GetComponent<SemiAffect>();
				if (!Object.op_Implicit((Object)(object)component))
				{
					Object.Destroy((Object)(object)val2);
					continue;
				}
				component.positionOfOriginalAreaOfEffect = _position;
				component.direction = new Vector3(_directionX[i], _directionY[i], _directionZ[i]);
				component.flatTorque = true;
				component.Setup(_viewIDs[i], _times[i]);
			}
		}
	}

	private void AffectSingleplayer(List<PlayerAvatar> _players, List<PhysGrabObject> _physObjects, Vector3 _position)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		if (affectPrefab == null || !affectPrefab.IsValid())
		{
			return;
		}
		foreach (PlayerAvatar _player in _players)
		{
			if (Object.op_Implicit((Object)(object)_player))
			{
				AffectSpawn(((Component)_player).transform, _player.tumble.physGrabObject, _player, AffectDirection(((Component)_player).transform.position, _position), Random.Range(affectTimeMin, affectTimeMax) * 0.5f, _position);
			}
		}
		List<PhysGrabObject> list = new List<PhysGrabObject>();
		foreach (PhysGrabObject _physObject in _physObjects)
		{
			if (Object.op_Implicit((Object)(object)_physObject) && !((Object)(object)_physObject == (Object)(object)physGrabObject) && !_physObject.isPlayer && !_physObject.dead && !list.Contains(_physObject))
			{
				list.Add(_physObject);
				AffectSpawn(((Component)_physObject).transform, _physObject, null, AffectDirection(((Component)_physObject).transform.position, _position), Random.Range(affectTimeMin, affectTimeMax), _position);
			}
		}
	}

	private void AffectSpawn(Transform _targetTransform, PhysGrabObject _targetPhysObject, PlayerAvatar _playerAvatar, Vector3 _direction, float _time, Vector3 _position)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = Object.Instantiate<GameObject>(affectPrefab.Prefab, _targetTransform.position, Quaternion.identity);
		SemiAffect component = val.GetComponent<SemiAffect>();
		if (!Object.op_Implicit((Object)(object)component))
		{
			Object.Destroy((Object)(object)val);
			return;
		}
		component.positionOfOriginalAreaOfEffect = _position;
		component.direction = _direction;
		component.flatTorque = true;
		component.SetupSingleplayer(_targetTransform, _targetPhysObject, _time, _playerAvatar);
	}

	private Vector3 AffectDirection(Vector3 _targetPosition, Vector3 _origin)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = _targetPosition - _origin;
		val.y = 0f;
		if (val.sqrMagnitude > 0.0001f)
		{
			return val.normalized;
		}
		Vector2 insideUnitCircle = Random.insideUnitCircle;
		if (insideUnitCircle.sqrMagnitude < 0.0001f)
		{
			return Vector3.forward;
		}
		insideUnitCircle.Normalize();
		return new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y);
	}

	private void PurpleSmokeSpawn(Vector3 _position)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Expected Obj, but got Unknown
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = Resources.Load<GameObject>("Effects/Part Prefab Explosion");
		if (Object.op_Implicit((Object)(object)val))
		{
			ParticlePrefabExplosion component = val.GetComponent<ParticlePrefabExplosion>();
			if (Object.op_Implicit((Object)(object)component) && Object.op_Implicit((Object)(object)component.particleSmoke))
			{
				float num = Mathf.Max(0.1f, purpleSmokeRadius);
				float num2 = Mathf.Max(0.1f, purpleSmokeTimeMin);
				float num3 = Mathf.Max(num2, purpleSmokeTimeMax);
				int num4 = Mathf.Clamp(purpleSmokeParticles, 1, 100);
				ParticleSystem val2 = Object.Instantiate<ParticleSystem>(component.particleSmoke, _position, Quaternion.identity);
				((Component)val2).transform.SetParent((Transform)null, true);
				MainModule main = val2.main;
				main.loop = false;
				main.playOnAwake = false;
				main.duration = 0.15f;
				main.startLifetime = new MinMaxCurve(num2, num3);
				main.startSpeed = new MinMaxCurve(num / num3 * 0.6f, num / num3 * 0.9f);
				main.startSize = new MinMaxCurve(num * 0.22f, num * 0.37f);
				main.startColor = MinMaxGradient.op_Implicit(Color.white);
				main.maxParticles = num4;
				main.simulationSpace = (ParticleSystemSimulationSpace)1;
				ShapeModule shape = val2.shape;
				shape.radius = num * 0.07f;
				shape.radiusThickness = 1f;
				EmissionModule emission = val2.emission;
				emission.rateOverTime = MinMaxCurve.op_Implicit(0f);
				emission.SetBursts(new Burst[1]
				{
					new Burst(0f, (short)num4)
				});
				Gradient val3 = new Gradient();
				val3.SetKeys(new GradientColorKey[3]
				{
					new GradientColorKey(new Color(0.85f, 0.22f, 1f), 0f),
					new GradientColorKey(new Color(0.55f, 0.08f, 0.85f), 0.55f),
					new GradientColorKey(new Color(0.22f, 0.02f, 0.4f), 1f)
				}, new GradientAlphaKey[3]
				{
					new GradientAlphaKey(0.6f, 0f),
					new GradientAlphaKey(0.35f, 0.65f),
					new GradientAlphaKey(0f, 1f)
				});
				ColorOverLifetimeModule colorOverLifetime = val2.colorOverLifetime;
				colorOverLifetime.enabled = true;
				colorOverLifetime.color = new MinMaxGradient(val3);
				main.stopAction = (ParticleSystemStopAction)2;
				val2.Play(true);
			}
		}
	}

	private void FloorContactSet(Collision _collision)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		int mask = LayerMask.GetMask(new string[2] { "Default", "StaticGrabObject" });
		int layer = ((Component)_collision.collider).gameObject.layer;
		if ((mask & (1 << layer)) == 0)
		{
			return;
		}
		float num = 0.25f;
		Vector3 val = Vector3.zero;
		Vector3 val2 = Vector3.up;
		for (int i = 0; i < _collision.contactCount; i++)
		{
			ContactPoint contact = _collision.GetContact(i);
			if (!(contact.normal.y <= num))
			{
				num = contact.normal.y;
				val = contact.point;
				val2 = contact.normal;
			}
		}
		if (!(num <= 0.25f))
		{
			floorHitPosition = val;
			floorHitNormal = val2;
			floorContactTimer = floorContactMemoryTime;
		}
	}

	private void FloorHitGet(out Vector3 _position, out Vector3 _normal)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		if (floorContactTimer > 0f)
		{
			_position = floorHitPosition;
			_normal = floorHitNormal;
			return;
		}
		Vector3 val = (Object.op_Implicit((Object)(object)physGrabObject) ? physGrabObject.centerPoint : ((Component)this).transform.position);
		Vector3 val2 = val + Vector3.up * 0.5f;
		int mask = LayerMask.GetMask(new string[2] { "Default", "StaticGrabObject" });
		RaycastHit val3 = default;
		if (Physics.Raycast(val2, Vector3.down, ref val3, floorSearchDistance + 0.5f, mask, (QueryTriggerInteraction)1))
		{
			_position = val3.point;
			_normal = val3.normal;
		}
		else
		{
			_position = val;
			_normal = Vector3.up;
		}
	}

	public ValuableWizardCrystal()
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
	}
}
