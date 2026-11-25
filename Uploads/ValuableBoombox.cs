using System;
using Photon.Pun;
using UnityEngine;

public class ValuableBoombox : Trap
{
	[Header("Sounds")]
	public Sound soundBoomboxStart;

	public Sound soundBoomboxStop;

	public Sound soundBoomboxMusic;

	public Sound soundGlitch;

	[Space]
	private AudioClip[] audioClipMusicMain;

	public AudioClip audioClipMusicRobe;

	[Space]
	public Transform speaker1;

	public Transform speaker2;

	[Space]
	public Light light1;

	public Light light2;

	protected override void Start()
	{
		base.Start();
		((Behaviour)light1).enabled = false;
		((Behaviour)light2).enabled = false;
		audioClipMusicMain = soundBoomboxMusic.Sounds;
	}

	protected override void Update()
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		base.Update();
		if (!trapTriggered && physGrabObject.grabbed)
		{
			trapStart = true;
		}
		if (physGrabObject.grabbed)
		{
			soundBoomboxMusic.PlayLoop(playing: true, 2f, 2f);
			enemyInvestigate = true;
			float num = 1f + Mathf.Sin(Time.time * 80f) * 0.005f;
			speaker1.localScale = new Vector3(num, num, num);
			speaker2.localScale = new Vector3(num, num, num);
			float num2 = Mathf.PingPong(Time.time * 0.5f, 1f);
			float num3 = 1f;
			if (SemiFunc.Photosensitivity())
			{
				num2 = 0.75f;
				num3 = 0.5f;
			}
			Color val = Color.Lerp(((Component)speaker1).GetComponent<Renderer>().material.GetColor("_EmissionColor"), Color.HSVToRGB(num2, 1f, 1f), Time.deltaTime * 6000f);
			((Component)speaker1).GetComponent<Renderer>().material.SetColor("_EmissionColor", val);
			((Component)speaker2).GetComponent<Renderer>().material.SetColor("_EmissionColor", val);
			((Behaviour)light1).enabled = true;
			((Behaviour)light2).enabled = true;
			light1.color = val;
			light2.color = val;
			light1.intensity = Mathf.Lerp(light1.intensity, 4f, num3 * Time.deltaTime);
			light2.intensity = Mathf.Lerp(light2.intensity, 4f, num3 * Time.deltaTime);
		}
		else
		{
			StopEasterEgg();
			soundBoomboxMusic.PlayLoop(playing: false, 2f, 2f);
			if (((Behaviour)light1).enabled)
			{
				Color val2 = Color.Lerp(((Component)speaker1).GetComponent<Renderer>().material.GetColor("_EmissionColor"), Color.black, Time.deltaTime);
				((Component)speaker1).GetComponent<Renderer>().material.SetColor("_EmissionColor", val2);
				((Component)speaker2).GetComponent<Renderer>().material.SetColor("_EmissionColor", val2);
				speaker1.localScale = Vector3.Lerp(speaker1.localScale, Vector3.one, Time.deltaTime);
				speaker2.localScale = Vector3.Lerp(speaker2.localScale, Vector3.one, Time.deltaTime);
				light1.intensity = Mathf.Lerp(light1.intensity, 0f, Time.deltaTime);
				light2.intensity = Mathf.Lerp(light2.intensity, 0f, Time.deltaTime);
				if (light1.intensity < 0.01f)
				{
					((Component)speaker1).GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);
					((Component)speaker2).GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);
					((Behaviour)light1).enabled = false;
					((Behaviour)light2).enabled = false;
					speaker1.localScale = Vector3.one;
					speaker2.localScale = Vector3.one;
				}
			}
		}
		if (!physGrabObject.grabbed)
		{
			return;
		}
		float amount = Mathf.Sin(Time.time * 15f) * 0.5f;
		float num4 = Mathf.Sin(Time.time * 15f) * 25f;
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (item.isLocal)
			{
				CameraAim.Instance.AdditiveAimY(amount);
				item.OverrideGrabDistance(1f);
				item.OverrideDisableRotationControls();
				item.playerAvatar.playerExpression.OverrideExpressionSet(4, 100f);
				PlayerExpressionsUI.instance.playerExpression.OverrideExpressionSet(4, 100f);
				PlayerExpressionsUI.instance.playerAvatarVisuals.HeadTiltOverride(num4 * 0.5f);
			}
			else
			{
				item.playerAvatar.playerAvatarVisuals.HeadTiltOverride(num4);
			}
		}
	}

	public void StartEasterEggRobe()
	{
		if ((Object)(object)soundBoomboxMusic.Sounds[0] != (Object)(object)audioClipMusicRobe)
		{
			if (GameManager.Multiplayer())
			{
				photonView.RPC("StartEasterEggRobeRPC", (RpcTarget)0, Array.Empty<object>());
			}
			else
			{
				StartEasterEggRobeLocal();
			}
		}
	}

	private void StartEasterEggRobeLocal()
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)soundBoomboxMusic.Sounds[0] != (Object)(object)audioClipMusicRobe)
		{
			soundBoomboxMusic.Sounds = new AudioClip[1] { audioClipMusicRobe };
			soundBoomboxMusic.AudioInfoFetched = false;
			soundBoomboxMusic.StartTimeOverride = 0f;
			soundGlitch.Play(physGrabObject.centerPoint);
		}
	}

	[PunRPC]
	public void StartEasterEggRobeRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			StartEasterEggRobeLocal();
		}
	}

	public void StopEasterEgg()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)soundBoomboxMusic.Sounds[0] != (Object)(object)audioClipMusicRobe))
		{
			soundBoomboxMusic.Sounds = audioClipMusicMain;
			soundBoomboxMusic.AudioInfoFetched = false;
			soundBoomboxMusic.StartTimeOverride = 999999f;
			soundGlitch.Play(physGrabObject.centerPoint);
		}
	}
}
