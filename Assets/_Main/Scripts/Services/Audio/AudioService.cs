using System;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Services.Settings;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Main.Scripts.Services.Audio
{
	public interface IAudioService
	{
		void Play(GameSound sound, float pitch = 1f, float volumeScale = 1f);
		void PlayClip(AudioClip? clip, float volume = 1f, float pitch = 1f);
		void StartMusic();

		/// <summary>Lowers the music under a big moment (reveals, fanfares) and restores it afterwards.</summary>
		void SetMusicDucked(bool ducked);
	}

	/// <summary>
	/// Pooled one-shot sources + a looping music source on a scene GameObject. The global mute is persisted in
	/// <see cref="ISettingsService"/> and applied through <see cref="AudioListener.volume"/>.
	/// </summary>
	[UsedImplicitly]
	public sealed class AudioService : IAudioService, IDisposable
	{
		private const int SfxVoices = 10;
		private const float DuckedMusicFactor = 0.25f;

		private readonly IAudioConfig _config;
		private readonly ISettingsService _settings;
		private readonly GameObject _root;
		private readonly AudioSource[] _sfx;
		private readonly AudioSource _music;
		private readonly AudioMusicFader _fader;
		private int _nextVoice;

		public AudioService(IAudioConfig config, ISettingsService settings)
		{
			_config = config;
			_settings = settings;

			_root = new GameObject("[AudioService]");
			// Nothing is audible without a listener; the scene camera normally has one.
			if (Object.FindAnyObjectByType<AudioListener>() == null)
				_root.AddComponent<AudioListener>();
			_sfx = new AudioSource[SfxVoices];
			for (int i = 0; i < SfxVoices; i++)
			{
				_sfx[i] = _root.AddComponent<AudioSource>();
				_sfx[i].playOnAwake = false;
			}

			_music = _root.AddComponent<AudioSource>();
			_music.playOnAwake = false;
			_music.loop = true;
			_music.volume = 0f;

			_fader = _root.AddComponent<AudioMusicFader>();
			_fader.Source = _music;

			_settings.Changed += ApplyMute;
			ApplyMute();
		}

		public void Play(GameSound sound, float pitch = 1f, float volumeScale = 1f)
		{
			if (_config.TryGetSound(sound, out var clip, out var volume))
				PlayClip(clip, volume * volumeScale, pitch);
		}

		public void PlayClip(AudioClip? clip, float volume = 1f, float pitch = 1f)
		{
			if (clip == null)
				return;

			// Round-robin voice stealing: the oldest one-shot gets cut, which is inaudible in practice.
			var source = _sfx[_nextVoice];
			_nextVoice = (_nextVoice + 1) % _sfx.Length;

			source.pitch = pitch;
			source.PlayOneShot(clip, volume);
		}

		public void StartMusic()
		{
			if (_config.LobbyMusic == null || _music.isPlaying)
				return;

			_music.clip = _config.LobbyMusic;
			_music.Play();
			_fader.TargetVolume = _config.MusicVolume;
		}

		public void SetMusicDucked(bool ducked)
		{
			_fader.TargetVolume = _config.MusicVolume * (ducked ? DuckedMusicFactor : 1f);
		}

		private void ApplyMute()
		{
			AudioListener.volume = _settings.IsMuted ? 0f : 1f;
		}

		public void Dispose()
		{
			_settings.Changed -= ApplyMute;
			if (_root != null)
				Object.Destroy(_root);
		}
	}

	/// <summary>Smoothly moves the music volume towards its target (fade-in, ducking).</summary>
	public sealed class AudioMusicFader : MonoBehaviour
	{
		public AudioSource? Source;
		public float TargetVolume;

		private void Update()
		{
			if (Source == null)
				return;

			Source.volume = Mathf.MoveTowards(Source.volume, TargetVolume, UnityEngine.Time.unscaledDeltaTime * 0.8f);
		}
	}
}
