using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Main.Scripts.Configs.Audio
{
	/// <summary>Named one-shot sounds used by the UI/game-feel layer. Rarity stingers and character voices live in the collection config.</summary>
	public enum GameSound
	{
		ButtonPress = 0,
		TabSwitch = 1,
		PopupOpen = 2,
		PopupClose = 3,
		ScanBeep = 4,
		CardAppear = 5,
		CardFlip = 6,
		CardWhoosh = 7,
		SlotFill = 8,
		PointsTick = 9,
		Duplicate = 10,
		Error = 11,
		CompletionFanfare = 12,
		Sparkle = 13,
		Rumble = 14,
		BuildUp = 15,
		NewBadge = 16,
		Firework = 17,
		Toggle = 18,
		FloatFire = 19,
		FloatJackpot = 20
	}

	public interface IAudioConfig
	{
		AudioClip? LobbyMusic { get; }
		float MusicVolume { get; }
		bool TryGetSound(GameSound sound, out AudioClip clip, out float volume);
	}

	[CreateAssetMenu(fileName = "AudioConfig", menuName = "Configs/AudioConfig")]
	public sealed class AudioConfig : ScriptableObject, IAudioConfig
	{
		[SerializeField] private AudioClip? _lobbyMusic;
		[SerializeField, Range(0f, 1f)] private float _musicVolume = 0.45f;
		[SerializeField] private List<SoundEntry> _sounds = new();

		public AudioClip? LobbyMusic => _lobbyMusic;
		public float MusicVolume => _musicVolume;

		public bool TryGetSound(GameSound sound, out AudioClip clip, out float volume)
		{
			foreach (var entry in _sounds)
			{
				if (entry.Sound == sound && entry.Clip != null)
				{
					clip = entry.Clip;
					volume = entry.Volume;
					return true;
				}
			}

			clip = null!;
			volume = 0f;
			return false;
		}

		[Serializable]
		public sealed class SoundEntry
		{
			[SerializeField] private GameSound _sound;
			[SerializeField] private AudioClip? _clip;
			[SerializeField, Range(0f, 1f)] private float _volume = 1f;

			public GameSound Sound => _sound;
			public AudioClip? Clip => _clip;
			public float Volume => _volume;

			public SoundEntry(GameSound sound, AudioClip? clip, float volume)
			{
				_sound = sound;
				_clip = clip;
				_volume = volume;
			}
		}

#if UNITY_EDITOR
		public void EditorSetData(AudioClip? music, float musicVolume, List<SoundEntry> sounds)
		{
			_lobbyMusic = music;
			_musicVolume = musicVolume;
			_sounds = sounds;
		}
#endif
	}
}
