using Photon.Pun;
using UnityEngine;

public class ItemGunTranq : MonoBehaviour
{
	private PhotonView photonView;

	private void Start()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
	}

	public void MakeHitPlayersHigh()
	{
		if (SemiFunc.IsMultiplayer())
		{
			PlayerAvatar instance = PlayerAvatar.instance;
			instance.OverridePupilSize(3f, 4, 1f, 1f, 5f, 0.5f, 1.8f);
			photonView.RPC("SlowDownVoiceRPC", (RpcTarget)1, new object[1] { instance.photonView.ViewID });
		}
	}

	[PunRPC]
	public void SlowDownVoiceRPC(int _photonID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(_photonID);
		if (Object.op_Implicit((Object)(object)playerAvatar) && SemiFunc.OwnerOnlyRPC(_info, playerAvatar.photonView) && Object.op_Implicit((Object)(object)playerAvatar.voiceChat))
		{
			playerAvatar.voiceChat.OverridePitch(0.65f, 1f, 1f, 3f, 0.1f, 20f);
		}
	}
}
