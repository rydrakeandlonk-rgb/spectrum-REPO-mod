using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ValuableMusicInstrument : MonoBehaviour
{
	public enum Instrument
	{
		Harmonica,
		PanFlute,
		MouthHarp,
		Ocarina
	}

	public enum State
	{
		Idle,
		Grabbed,
		Attached,
		Detaching
	}

	private bool debug;

	public Instrument instrument;

	public float alignmentDistance = 0.35f;

	public bool useCustomAlignmentPoint;

	public Transform customAlignmentPoint;

	public Sound sfxAttach;

	public Sound sfxDetach;

	public List<Sound> sfxLoops;

	public Sound sfxOneShots;

	public float loudnessReleaseSpeed = 2f;

	public float loudnessThreshold = 0.1f;

	private float semitone = 0.041666f;

	public Sound sfxHarmonicaLoop01;

	public Sound sfxHarmonicaLoop02;

	public Sound sfxHarmonicaLoop03;

	public Sound sfxHarmonicaFillIntro;

	public Sound sfxHarmonicaFillLoop;

	private float harmonicaFillPitch = 0.5f;

	private bool harmonicaFillImpulse;

	private AudioSource harmonicaFillIntroSource;

	public Sound sfxMouthHarpLoudShort;

	public Sound sfxMouthHarpLoudLong;

	public Sound sfxMouthHarpQuiet;

	public Transform mouthHarpSpringTransform;

	private float mouthHarpSpringAmplitude = 8f;

	private float mouthHarpSpringFrequency = 10f;

	private float mouthHarpSpringTimerMax = 0.6f;

	private float mouthHarpSpringTimer;

	private bool mouthHarpSpringPlaying;

	private int mouthHarpGrabbersPrevious;

	private float mouthHarpToneInterval = 0.33f;

	private float mouthHarpToneTimer;

	private bool mouthHarpPlayLongNext;

	public List<Sound> sfxPanFluteLoops;

	public Transform panFluteLoopPosition;

	public float panFluteLoudnessBoost = 4f;

	private float panFluteLookRange = 90f;

	private Vector3 panFluteForwardReference;

	private Vector3 panFluteUpReference;

	private float panFlutePitchOffset;

	private float panFluteOneShotInterval = 0.1f;

	private float panFluteOneShotTimer;

	private int panFluteLoopIndexPrevious = -1;

	private State currentState;

	private float stateTimer;

	private bool stateImpulse;

	private float detachCooldown = 1f;

	private Rigidbody rb;

	private PhysGrabObject physGrabObject;

	private PlayerAvatar attachedPlayer;

	private PhotonView photonView;

	private float attachDistance = 1f;

	private float attachTimer;

	private bool active;

	private int activeLoopIndex;

	private float currentLoudness;

	private float investigationRadius = 7.5f;

	private void Start()
	{
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected Obj, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		rb = ((Component)this).GetComponent<Rigidbody>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		sfxHarmonicaLoop01.StartTimeOverride = 0f;
		sfxHarmonicaLoop02.StartTimeOverride = 0f;
		sfxHarmonicaLoop03.StartTimeOverride = 0f;
		sfxHarmonicaFillLoop.StartTimeOverride = 0f;
		foreach (Sound sfxPanFluteLoop in sfxPanFluteLoops)
		{
			sfxPanFluteLoop.StartTimeOverride = 0f;
		}
		if (!useCustomAlignmentPoint)
		{
			GameObject val = new GameObject("Custom Alignment Point");
			val.transform.SetParent(((Component)physGrabObject).transform);
			customAlignmentPoint = val.transform;
			customAlignmentPoint.localRotation = Quaternion.identity;
		}
		if (instrument != Instrument.PanFlute)
		{
			return;
		}
		float volumeRandom = Random.Range(0f, 0.5f);
		foreach (Sound sfxPanFluteLoop2 in sfxPanFluteLoops)
		{
			sfxPanFluteLoop2.PitchRandom = 0f;
			sfxPanFluteLoop2.VolumeRandom = volumeRandom;
		}
	}

	private void Update()
	{
		DebugRuben();
		StateMachine(isFixedUpdate: false);
		SoundUpdate();
		VisualsUpdate();
	}

	private void FixedUpdate()
	{
		StateMachine(isFixedUpdate: true);
	}

	private void StateMachine(bool isFixedUpdate)
	{
		switch (currentState)
		{
		case State.Idle:
			StateIdle(isFixedUpdate);
			break;
		case State.Grabbed:
			StateGrabbed(isFixedUpdate);
			break;
		case State.Attached:
			StateAttached(isFixedUpdate);
			break;
		case State.Detaching:
			StateDetaching(isFixedUpdate);
			break;
		}
	}

	private void SetState(State newState)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && currentState != newState)
		{
			int num = -1;
			if (Object.op_Implicit((Object)(object)attachedPlayer) && SemiFunc.IsMultiplayer())
			{
				num = attachedPlayer.photonView.ViewID;
			}
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("SetStateRPC", (RpcTarget)0, new object[2] { newState, num });
			}
			else
			{
				SetStateRPC(newState, num);
			}
		}
	}

	private void StateIdle(bool isFixedUpdate)
	{
		if (IsGrabbed())
		{
			SetState(State.Grabbed);
		}
	}

	private void StateGrabbed(bool isFixedUpdate)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (isFixedUpdate)
		{
			return;
		}
		if (!IsGrabbed())
		{
			SetState(State.Idle);
			return;
		}
		PlayerAvatar playerAvatar = SemiFunc.PlayerGetNearestPlayerAvatarWithinRange(attachDistance, ((Component)this).transform.position);
		if (Object.op_Implicit((Object)(object)playerAvatar))
		{
			attachedPlayer = playerAvatar;
			SetState(State.Attached);
		}
	}

	private void StateAttached(bool isFixedUpdate)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0402: Unknown result type (might be due to invalid IL or missing references)
		//IL_0407: Unknown result type (might be due to invalid IL or missing references)
		//IL_0414: Unknown result type (might be due to invalid IL or missing references)
		//IL_0421: Unknown result type (might be due to invalid IL or missing references)
		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
		//IL_042e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_0445: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			attachTimer = 1f;
			sfxAttach.Play(((Component)this).transform);
			if (!useCustomAlignmentPoint)
			{
				customAlignmentPoint.position = physGrabObject.centerPoint;
			}
			if (instrument == Instrument.PanFlute)
			{
				panFluteForwardReference = ((Component)attachedPlayer).transform.forward;
				panFluteUpReference = ((Component)attachedPlayer).transform.up;
			}
			EnemySetInvestigate();
			stateImpulse = false;
		}
		if (SemiFunc.PerSecond(5f, (Object)(object)this))
		{
			foreach (PhysGrabber item in physGrabObject.playerGrabbing)
			{
				if (item.isLocal)
				{
					item.OverrideGrabDistance(Vector3.Distance(item.playerAvatar.localCamera.GetOverrideTransform().position, item.physGrabPointPullerPosition));
				}
			}
		}
		if (!isFixedUpdate && attachTimer > 0f)
		{
			attachTimer -= Time.deltaTime;
		}
		if (!IsGrabbed() || !Object.op_Implicit((Object)(object)attachedPlayer) || attachedPlayer.isDisabled || AnyGrabbingPlayerPushingOrPulling())
		{
			SetState(State.Detaching);
			return;
		}
		if (SemiFunc.IsMultiplayer() && attachedPlayer.voiceChatFetched)
		{
			attachedPlayer.voiceChat.OverrideVolumeMute();
		}
		Vector3 val = attachedPlayer.playerAvatarVisuals.headUpTransform.forward * alignmentDistance;
		if (!attachedPlayer.isCrawling)
		{
			float num = 0.81f;
			float num2 = Vector3.Dot(attachedPlayer.playerAvatarVisuals.headUpTransform.forward, Vector3.up);
			val *= 1f + Mathf.Max(num2 * num, 0f);
		}
		Vector3 val2 = attachedPlayer.playerAvatarVisuals.headUpTransform.up * 0.05f;
		Vector3 val3 = val + val2;
		Vector3 val4 = attachedPlayer.playerAvatarVisuals.headUpTransform.position + val3;
		Quaternion val5 = Quaternion.LookRotation(attachedPlayer.playerAvatarVisuals.headUpTransform.position - customAlignmentPoint.position, attachedPlayer.playerAvatarVisuals.headUpTransform.up);
		active = currentState == State.Attached;
		if (debug)
		{
			Debug.DrawLine(customAlignmentPoint.position, val4, Color.magenta);
			Debug.DrawRay(val4, Vector3.up, Color.green, 0.2f);
			Debug.DrawRay(attachedPlayer.playerAvatarVisuals.headUpTransform.position, attachedPlayer.playerAvatarVisuals.headUpTransform.forward, Color.red, 0.2f);
			Debug.DrawRay(physGrabObject.centerPoint, Vector3.up, Color.yellow, 0.2f);
		}
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (!isFixedUpdate)
		{
			if (!physGrabObject.isRotating)
			{
				physGrabObject.OverrideTorqueStrength(0f);
				physGrabObject.OverrideAngularDrag(20f);
			}
		}
		else
		{
			Vector3 val6 = SemiFunc.PhysFollowPosition(((Component)customAlignmentPoint).transform.position, val4, rb.linearVelocity, 8f);
			rb.AddForce(val6 * 10f * rb.mass * Time.fixedDeltaTime, (ForceMode)1);
			physGrabObject.OverrideZeroGravity();
			Quaternion val7 = Quaternion.Inverse(((Component)rb).transform.rotation) * customAlignmentPoint.rotation;
			Quaternion targetRotation = val5 * Quaternion.Inverse(val7);
			Vector3 val8 = SemiFunc.PhysFollowRotation(((Component)rb).transform, targetRotation, rb, 5f);
			rb.AddTorque(val8 * 10f * rb.mass * Time.fixedDeltaTime, (ForceMode)1);
		}
	}

	private void StateDetaching(bool isFixedUpdate)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			attachedPlayer = null;
			active = false;
			stateTimer = detachCooldown;
			sfxDetach.Play(((Component)this).transform);
			EnemyDirector.instance.SetInvestigate(((Component)this).transform.position, investigationRadius);
			stateImpulse = false;
		}
		if (isFixedUpdate)
		{
			return;
		}
		stateTimer -= Time.deltaTime;
		if (!(stateTimer > 0f))
		{
			if (IsGrabbed())
			{
				SetState(State.Grabbed);
				return;
			}
			SetState(State.Idle);
			activeLoopIndex = -1;
		}
	}

	private bool IsGrabbed()
	{
		return physGrabObject.playerGrabbing.Count != 0;
	}

	private bool IsLouderThan(float threshold)
	{
		return currentLoudness > threshold;
	}

	private bool AnyGrabbingPlayerPushingOrPulling()
	{
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if ((item.isPushing || item.isPulling) && attachTimer <= 0f)
			{
				return true;
			}
		}
		return false;
	}

	private float GetAttachedPlayerLoudness()
	{
		float num = 0f;
		if (!Object.op_Implicit((Object)(object)attachedPlayer))
		{
			return num;
		}
		if (!SemiFunc.IsMultiplayer())
		{
			return 1f;
		}
		if (attachedPlayer.voiceChatFetched && attachedPlayer.voiceChat.clipLoudness > 0.005f)
		{
			num += attachedPlayer.voiceChat.clipLoudness;
		}
		return Mathf.Clamp(num * 8f, 0f, 1f);
	}

	private void EnemySetInvestigate()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		EnemyDirector.instance.SetInvestigate(((Component)this).transform.position, investigationRadius);
	}

	private void SoundUpdate()
	{
		LoudnessUpdate();
		switch (instrument)
		{
		case Instrument.Harmonica:
			SoundHarmonica();
			break;
		case Instrument.PanFlute:
			SoundPanFlute();
			break;
		case Instrument.MouthHarp:
			SoundMouthHarp();
			break;
		}
	}

	private void LoopInvestigate(float threshold)
	{
		if (active && IsLouderThan(threshold) && SemiFunc.PerSecond(2f, (Object)(object)this))
		{
			EnemySetInvestigate();
		}
	}

	private void LoudnessUpdate()
	{
		float num = Mathf.Max(0f, GetAttachedPlayerLoudness());
		if (num >= currentLoudness)
		{
			currentLoudness = num;
			return;
		}
		loudnessReleaseSpeed = 1.5f;
		currentLoudness = Mathf.MoveTowards(currentLoudness, num, loudnessReleaseSpeed * Time.deltaTime);
	}

	private void SoundHarmonica()
	{
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		LoopInvestigate(loudnessThreshold);
		sfxHarmonicaLoop01.PlayLoop(active, 3f, 1f, 1f, ActiveLoopVolume(0));
		sfxHarmonicaLoop02.PlayLoop(active, 3f, 1f, 1f, ActiveLoopVolume(1));
		sfxHarmonicaLoop03.PlayLoop(active, 3f, 1f, 1f, ActiveLoopVolume(2));
		sfxHarmonicaFillLoop.PlayLoop(active, 0.5f, 1f, harmonicaFillPitch, ActiveLoopVolume(3));
		if (activeLoopIndex == 3)
		{
			if (!harmonicaFillImpulse)
			{
				harmonicaFillIntroSource = sfxHarmonicaFillIntro.Play(((Component)this).transform.position);
				harmonicaFillImpulse = true;
			}
			if (Object.op_Implicit((Object)(object)harmonicaFillIntroSource))
			{
				harmonicaFillIntroSource.pitch = harmonicaFillPitch;
			}
		}
		else
		{
			harmonicaFillImpulse = false;
		}
		if (!Object.op_Implicit((Object)(object)attachedPlayer))
		{
			activeLoopIndex = -1;
			return;
		}
		float num = Vector3.Dot(((Component)attachedPlayer.localCamera).transform.forward, Vector3.up);
		if (num < 0.55f)
		{
			if (!(num < -0.3f))
			{
				if (num < 0.2f)
				{
					activeLoopIndex = 1;
				}
				else
				{
					activeLoopIndex = 2;
				}
			}
			else
			{
				activeLoopIndex = 0;
			}
		}
		else if (num >= 0.55f)
		{
			activeLoopIndex = 3;
			harmonicaFillPitch = Mathf.Lerp(1f - semitone * 4f, 1f, Mathf.InverseLerp(0.55f, 0.94f, num));
		}
		else
		{
			activeLoopIndex = -1;
		}
		float ActiveLoopVolume(int _loopIndex)
		{
			if (activeLoopIndex == _loopIndex)
			{
				return currentLoudness;
			}
			return 0f;
		}
	}

	private void SoundMouthHarp()
	{
		if (!active)
		{
			mouthHarpToneTimer = 0f;
		}
		else if (mouthHarpToneTimer > 0f)
		{
			mouthHarpToneTimer -= Time.deltaTime;
		}
		else if (IsLouderThan(loudnessThreshold) || !SemiFunc.IsMultiplayer())
		{
			MouthHarpPlayTone();
			mouthHarpToneTimer = mouthHarpToneInterval;
		}
	}

	private void SoundPanFlute()
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		LoopInvestigate(loudnessThreshold);
		if (panFluteOneShotTimer > 0f)
		{
			panFluteOneShotTimer -= Time.deltaTime;
		}
		for (int i = 0; i < sfxPanFluteLoops.Count; i++)
		{
			sfxPanFluteLoops[i].PlayLoop(active, 3f, 1f, 1f + panFlutePitchOffset, ActiveLoopVolume(i));
		}
		if (!Object.op_Implicit((Object)(object)attachedPlayer))
		{
			activeLoopIndex = -1;
			panFluteLoopIndexPrevious = -1;
			return;
		}
		Vector3 val = Vector3.ProjectOnPlane(attachedPlayer.playerAvatarVisuals.headUpTransform.forward, panFluteUpReference);
		float num = Vector3.SignedAngle(panFluteForwardReference, val, panFluteUpReference);
		float num2 = Mathf.Clamp(num, 0f - panFluteLookRange, panFluteLookRange);
		float num3 = num - num2;
		if (num3 != 0f)
		{
			panFluteForwardReference = Quaternion.AngleAxis(num3, panFluteUpReference) * panFluteForwardReference;
		}
		float num4 = Mathf.InverseLerp(0f - panFluteLookRange, panFluteLookRange, num2) * (float)sfxPanFluteLoops.Count;
		activeLoopIndex = Mathf.Clamp(Mathf.FloorToInt(num4), 0, sfxPanFluteLoops.Count - 1);
		if (activeLoopIndex != panFluteLoopIndexPrevious && panFluteOneShotTimer <= 0f && IsLouderThan(loudnessThreshold))
		{
			panFluteOneShotTimer = panFluteOneShotInterval;
		}
		panFluteLoopIndexPrevious = activeLoopIndex;
		float num5 = num4 - (float)activeLoopIndex;
		float num6 = semitone / 2f;
		panFlutePitchOffset = Mathf.Lerp(0f - num6, num6, num5);
		float ActiveLoopVolume(int _loopIndex)
		{
			if (activeLoopIndex == _loopIndex)
			{
				return Mathf.Clamp01(currentLoudness * panFluteLoudnessBoost);
			}
			return 0f;
		}
	}

	private void MouthHarpPlayTone()
	{
		Sound sound = sfxMouthHarpLoudShort;
		if (mouthHarpPlayLongNext)
		{
			sound = sfxMouthHarpLoudLong;
		}
		mouthHarpPlayLongNext = !mouthHarpPlayLongNext;
		sound.Play(((Component)this).transform);
		EnemySetInvestigate();
		MouthHarpTriggerSpring();
	}

	private void MouthHarpPluck()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		Sound sound = sfxMouthHarpQuiet;
		if (currentState == State.Attached)
		{
			sound = sfxMouthHarpLoudLong;
		}
		sound.Play(((Component)this).transform);
		EnemyDirector.instance.SetInvestigate(((Component)this).transform.position, investigationRadius);
		MouthHarpTriggerSpring();
	}

	private void VisualsUpdate()
	{
		switch (instrument)
		{
		case Instrument.Harmonica:
			VisualsHarmonica();
			break;
		case Instrument.MouthHarp:
			VisualsMouthHarp();
			break;
		case Instrument.PanFlute:
			VisualsPanFlute();
			break;
		}
	}

	private void VisualsHarmonica()
	{
	}

	private void VisualsMouthHarp()
	{
		int count = physGrabObject.playerGrabbing.Count;
		if (count > mouthHarpGrabbersPrevious)
		{
			MouthHarpPluck();
		}
		mouthHarpGrabbersPrevious = count;
		MouthHarpSpringAnimUpdate();
	}

	private void VisualsPanFlute()
	{
	}

	private void MouthHarpTriggerSpring()
	{
		mouthHarpSpringTimer = 0f;
		mouthHarpSpringPlaying = true;
	}

	private void MouthHarpSpringAnimUpdate()
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (mouthHarpSpringPlaying)
		{
			mouthHarpSpringTimer += Time.deltaTime;
			float num = mouthHarpSpringTimer / mouthHarpSpringTimerMax;
			if (num >= 1f)
			{
				mouthHarpSpringPlaying = false;
				mouthHarpSpringTransform.localRotation = Quaternion.identity;
			}
			else
			{
				float num2 = mouthHarpSpringAmplitude * Mathf.Sin(MathF.PI * 2f * mouthHarpSpringFrequency * num) * (1f - num);
				mouthHarpSpringTransform.localRotation = Quaternion.Euler(num2, 0f, 0f);
			}
		}
	}

	[PunRPC]
	private void SetStateRPC(State state, int attachedPlayerViewID, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(info))
		{
			currentState = state;
			stateTimer = 0f;
			stateImpulse = true;
			if (SemiFunc.IsMultiplayer())
			{
				attachedPlayer = SemiFunc.PlayerAvatarGetFromPhotonID(attachedPlayerViewID);
			}
		}
	}

	private void DebugRuben()
	{
		if (SemiFunc.KeyDownRuben((KeyCode)111))
		{
			debug = !debug;
			SemiLogger.LogRuben("Debug: " + debug);
		}
		if (SemiFunc.KeyDownRuben((KeyCode)101) && debug)
		{
			int index = 2;
			SemiFunc.DebugSoundIndex(sfxDetach, index, ((Component)this).transform);
		}
	}
}
