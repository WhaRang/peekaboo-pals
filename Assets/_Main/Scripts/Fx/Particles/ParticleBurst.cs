using _Main.Scripts.Configs.Ui;
using UnityEngine;

namespace _Main.Scripts.Fx.Particles
{
	/// <summary>Emission settings for one burst. Ranges are (min, max); angles in degrees, 0 = right, 90 = up.</summary>
	public struct ParticleBurst
	{
		public ParticleShape[] Shapes;
		public Color[] Colors;
		public Vector2 Speed;
		public Vector2 Angle;
		public Vector2 Size;
		public float EndSizeFactor;
		public Vector2 Life;
		public float Gravity;
		public float Drag;
		public Vector2 Spin;
		public float SpawnRadius;
		public Vector2 SpawnBox;
		/// <summary>Degrees per second the velocity vector turns: spirals and swirls.</summary>
		public float Swirl;
		public float WobbleAmplitude;
		public float WobbleFrequency;
		/// <summary>Wing-flap frequency (vertical squash), for bats.</summary>
		public float FlapFrequency;
		public bool FadeIn;
		/// <summary>Spawn on a ring of <see cref="SpawnRadius"/> and fly towards the center (charge-up).</summary>
		public bool Inward;
		public bool AlignToVelocity;
		/// <summary>Fraction of life after which the alpha starts fading out (default 0.6).</summary>
		public float FadeOutFrom;

		public static ParticleBurst Create(ParticleShape shape, Color color) => new()
		{
			Shapes = new[] { shape },
			Colors = new[] { color },
			Speed = new Vector2(200f, 600f),
			Angle = new Vector2(0f, 360f),
			Size = new Vector2(20f, 40f),
			EndSizeFactor = 0.3f,
			Life = new Vector2(0.6f, 1.2f),
			Drag = 1.5f,
			FadeOutFrom = 0.6f
		};
	}
}
