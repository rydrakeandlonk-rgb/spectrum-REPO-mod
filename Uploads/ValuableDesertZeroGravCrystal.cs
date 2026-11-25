using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ValuableDesertZeroGravCrystal : MonoBehaviour
{
	private class AffectedPlayer
	{
		public PlayerAvatar playerAvatar;

		public SemiAffect affect;
	}

	private PhysGrabObject physGrabObject;

	private PhotonView photonView;

	public PrefabRef zeroGravityAffectPrefab;

	public Sound soundCrystalActiveLoop;

	public List<MeshRenderer> emissionMeshRenderers = new List<MeshRenderer>();

	public float lingerTime = 2f;

	[ColorUsage(true, true)]
	public Color beamEffectColor = new Color(0.8f, 3.2f, 4.2f);

	public float beamEffectStrength = 0.5f;

	[ColorUsage(true, true)]
	public Color emissionPulsateColor = new Color(0f, 0.1f, 0.5f);

	public float emissionPulsateSpeed = 0.4f;

	public float emissionBlendTime = 1f;

	private List<AffectedPlayer> affectedPlayers = new List<AffectedPlayer>();

	private SemiAffect crystalAffect;

	private float crystalActiveTimer;

	private float emissionActiveLerp;

	private Color[] emissionOriginalColors;

	private void Start()
	{
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		EmissionOriginalColorsFetch();
	}

	private void Update()
	{
		CrystalActiveLogic();
		GrabberBeamsLogic();
		PlayLoopSoundLogic();
		EmissionPulsateLogic();
		CrystalActiveTick();
		AffectedPlayersCleanup();
		AffectRenewLogic();
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			AffectSpawnLogic();
		}
	}

	private void CrystalActiveLogic()
	{
		if (physGrabObject.playerGrabbing.Count > 0 || AffectIsActive(crystalAffect))
		{
			CrystalActiveOverride();
		}
	}

	private void CrystalActiveOverride()
	{
		crystalActiveTimer = 0.1f;
	}

	private void CrystalActiveTick()
	{
		if (crystalActiveTimer > 0f)
		{
			crystalActiveTimer -= Time.deltaTime;
		}
	}

	private void PlayLoopSoundLogic()
	{
		bool playing = crystalActiveTimer > 0f;
		soundCrystalActiveLoop.PlayLoop(playing, 1f, 3f);
	}

	private void EmissionOriginalColorsFetch()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		emissionOriginalColors = new Color[emissionMeshRenderers.Count];
		for (int i = 0; i < emissionMeshRenderers.Count; i++)
		{
			emissionOriginalColors[i] = ((Renderer)emissionMeshRenderers[i]).material.GetColor("_EmissionColor");
		}
	}

	private void EmissionPulsateLogic()
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		if (crystalActiveTimer > 0f)
		{
			num = 1f;
		}
		emissionActiveLerp = Mathf.MoveTowards(emissionActiveLerp, num, Time.deltaTime / emissionBlendTime);
		float num2 = Mathf.PerlinNoise(Time.time * emissionPulsateSpeed, 0.5f);
		float num3 = emissionActiveLerp * Mathf.InverseLerp(0.2f, 0.8f, num2);
		for (int i = 0; i < emissionMeshRenderers.Count; i++)
		{
			Color val = Color.Lerp(emissionOriginalColors[i], emissionPulsateColor, num3);
			((Renderer)emissionMeshRenderers[i]).material.SetColor("_EmissionColor", val);
		}
	}

	private void GrabberBeamsLogic()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (Object.op_Implicit((Object)(object)item))
			{
				item.physGrabBeamComponent.OverrideBeamEffect(beamEffectColor, beamEffectStrength);
			}
		}
	}

	private void AffectedPlayersCleanup()
	{
		for (int num = affectedPlayers.Count - 1; num >= 0; num--)
		{
			if (!Object.op_Implicit((Object)(object)affectedPlayers[num].affect) || !Object.op_Implicit((Object)(object)affectedPlayers[num].playerAvatar))
			{
				affectedPlayers.RemoveAt(num);
			}
		}
	}

	private void AffectRenewLogic()
	{
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (Object.op_Implicit((Object)(object)item) && Object.op_Implicit((Object)(object)item.playerAvatar))
			{
				SemiAffect affect = AffectGet(item.playerAvatar);
				if (AffectIsActive(affect))
				{
					AffectTimerSetFull(affect);
				}
			}
		}
		if (physGrabObject.playerGrabbing.Count > 0 && AffectIsActive(crystalAffect))
		{
			AffectTimerSetFull(crystalAffect);
		}
	}

	private void AffectSpawnLogic()
	{
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (Object.op_Implicit((Object)(object)item) && Object.op_Implicit((Object)(object)item.playerAvatar) && !AffectIsActive(AffectGet(item.playerAvatar)))
			{
				PlayerAffectStart(item.playerAvatar);
			}
		}
		if (physGrabObject.playerGrabbing.Count > 0 && !AffectIsActive(crystalAffect))
		{
			CrystalAffectStart();
		}
	}

	private void AffectTimerSetFull(SemiAffect _affect)
	{
		_affect.timer = _affect.timerTotal;
	}

	private bool AffectIsActive(SemiAffect _affect)
	{
		if (Object.op_Implicit((Object)(object)_affect))
		{
			return _affect.timer > 0f;
		}
		return false;
	}

	private SemiAffect AffectGet(PlayerAvatar _playerAvatar)
	{
		foreach (AffectedPlayer affectedPlayer in affectedPlayers)
		{
			if ((Object)(object)affectedPlayer.playerAvatar == (Object)(object)_playerAvatar)
			{
				return affectedPlayer.affect;
			}
		}
		return null;
	}

	private void AffectRegister(PlayerAvatar _playerAvatar, SemiAffect _affect)
	{
		foreach (AffectedPlayer affectedPlayer2 in affectedPlayers)
		{
			if ((Object)(object)affectedPlayer2.playerAvatar == (Object)(object)_playerAvatar)
			{
				affectedPlayer2.affect = _affect;
				return;
			}
		}
		AffectedPlayer affectedPlayer = new AffectedPlayer();
		affectedPlayer.playerAvatar = _playerAvatar;
		affectedPlayer.affect = _affect;
		affectedPlayers.Add(affectedPlayer);
	}

	private void PlayerAffectStart(PlayerAvatar _playerAvatar)
	{
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("PlayerZeroGravityAffectRPC", (RpcTarget)0, new object[2]
			{
				_playerAvatar.photonView.ViewID,
				AffectSpawnTime()
			});
		}
		else
		{
			PlayerAffectSpawn(_playerAvatar, AffectSpawnTime());
		}
	}

	private void CrystalAffectStart()
	{
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("CrystalZeroGravityAffectRPC", (RpcTarget)0, new object[1] { AffectSpawnTime() });
		}
		else
		{
			CrystalAffectSpawn(AffectSpawnTime());
		}
	}

	private float AffectSpawnTime()
	{
		return lingerTime * 0.5f;
	}

	private void PlayerAffectSpawn(PlayerAvatar _playerAvatar, float _affectTime)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		SemiAffect component = Object.Instantiate<GameObject>(zeroGravityAffectPrefab.Prefab, ((Component)_playerAvatar).transform.position, Quaternion.identity).GetComponent<SemiAffect>();
		if (SemiFunc.IsMultiplayer())
		{
			component.Setup(_playerAvatar.photonView.ViewID, _affectTime);
		}
		else
		{
			component.SetupSingleplayer(((Component)_playerAvatar).transform, _playerAvatar.tumble.physGrabObject, _affectTime, _playerAvatar);
		}
		AffectRegister(_playerAvatar, component);
	}

	private void CrystalAffectSpawn(float _affectTime)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = Object.Instantiate<GameObject>(zeroGravityAffectPrefab.Prefab, ((Component)physGrabObject).transform.position, Quaternion.identity);
		SemiAffect component = val.GetComponent<SemiAffect>();
		val.GetComponent<SemiAffectZeroGravity>().keepGrabbersAttached = true;
		if (SemiFunc.IsMultiplayer())
		{
			component.Setup(photonView.ViewID, _affectTime);
		}
		else
		{
			component.SetupSingleplayer(((Component)physGrabObject).transform, physGrabObject, _affectTime);
		}
		crystalAffect = component;
	}

	[PunRPC]
	private void PlayerZeroGravityAffectRPC(int _playerViewID, float _affectTime, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(_playerViewID);
			if (Object.op_Implicit((Object)(object)playerAvatar))
			{
				PlayerAffectSpawn(playerAvatar, _affectTime);
			}
		}
	}

	[PunRPC]
	private void CrystalZeroGravityAffectRPC(float _affectTime, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			CrystalAffectSpawn(_affectTime);
		}
	}

	public ValuableDesertZeroGravCrystal()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
	}
}
