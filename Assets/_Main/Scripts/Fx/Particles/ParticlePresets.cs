using _Main.Scripts.Configs.Ui;
using UnityEngine;

namespace _Main.Scripts.Fx.Particles
{
	/// <summary>The game's particle vocabulary: confetti, sparkles, bats, candy, embers, snow, fog, swirls, fireworks.</summary>
	public static class ParticlePresets
	{
		private static readonly Color[] White = { Color.white };

		public static ParticleBurst Confetti(Color[] colors) => new()
		{
			Shapes = new[] { ParticleShape.Confetti, ParticleShape.Square, ParticleShape.Star, ParticleShape.Heart },
			Colors = colors,
			Speed = new Vector2(700f, 1500f),
			Angle = new Vector2(40f, 140f),
			Size = new Vector2(22f, 42f),
			EndSizeFactor = 0.8f,
			Life = new Vector2(1.4f, 2.4f),
			Gravity = -1900f,
			Drag = 2.2f,
			Spin = new Vector2(180f, 720f),
			WobbleAmplitude = 120f,
			WobbleFrequency = 7f,
			FadeOutFrom = 0.75f
		};

		public static ParticleBurst Sparkles(Color color) => new()
		{
			Shapes = new[] { ParticleShape.Sparkle, ParticleShape.Star },
			Colors = new[] { color, Color.white },
			Speed = new Vector2(150f, 900f),
			Angle = new Vector2(0f, 360f),
			Size = new Vector2(28f, 70f),
			EndSizeFactor = 0.1f,
			Life = new Vector2(0.5f, 1.1f),
			Drag = 3f,
			Spin = new Vector2(60f, 240f),
			FadeOutFrom = 0.5f
		};

		public static ParticleBurst Glints(Color color) => new()
		{
			Shapes = new[] { ParticleShape.Sparkle },
			Colors = new[] { color, Color.white },
			Speed = new Vector2(0f, 40f),
			Angle = new Vector2(0f, 360f),
			Size = new Vector2(30f, 60f),
			EndSizeFactor = 0f,
			Life = new Vector2(0.5f, 0.9f),
			Spin = new Vector2(40f, 120f),
			FadeIn = true,
			FadeOutFrom = 0.4f
		};

		/// <param name="spooky">Halloween collections throw little pumpkins in with the sweets.</param>
		public static ParticleBurst Candy(Color[] colors, bool spooky = false) => new()
		{
			Shapes = spooky ? new[] { ParticleShape.Candy, ParticleShape.Pumpkin, ParticleShape.Star } : new[] { ParticleShape.Candy, ParticleShape.Heart, ParticleShape.Star },
			Colors = colors,
			Speed = new Vector2(600f, 1300f),
			Angle = new Vector2(30f, 150f),
			Size = new Vector2(46f, 78f),
			EndSizeFactor = 0.9f,
			Life = new Vector2(1.2f, 2f),
			Gravity = -2200f,
			Drag = 0.8f,
			Spin = new Vector2(90f, 360f),
			FadeOutFrom = 0.8f
		};

		public static ParticleBurst Bats(Color color) => new()
		{
			Shapes = new[] { ParticleShape.Bat },
			Colors = new[] { color },
			Speed = new Vector2(400f, 900f),
			Angle = new Vector2(0f, 360f),
			Size = new Vector2(50f, 90f),
			EndSizeFactor = 0.6f,
			Life = new Vector2(1f, 1.8f),
			Drag = 0.6f,
			Swirl = 40f,
			FlapFrequency = 18f,
			WobbleAmplitude = 160f,
			WobbleFrequency = 5f,
			FadeOutFrom = 0.6f
		};

		public static ParticleBurst Swirl(Color color, float radius) => new()
		{
			Shapes = new[] { ParticleShape.SoftDot, ParticleShape.Sparkle, ParticleShape.Swirl },
			Colors = new[] { color, Color.white },
			Speed = new Vector2(500f, 800f),
			Angle = new Vector2(0f, 360f),
			Size = new Vector2(24f, 56f),
			EndSizeFactor = 0.2f,
			Life = new Vector2(0.9f, 1.4f),
			SpawnRadius = radius,
			Swirl = 260f,
			Drag = 0.4f,
			Spin = new Vector2(90f, 300f),
			FadeIn = true,
			FadeOutFrom = 0.5f
		};

		/// <summary>Particles sucked into the center: the build-up before a big reveal.</summary>
		public static ParticleBurst Implode(Color color, float radius) => new()
		{
			Shapes = new[] { ParticleShape.SoftDot, ParticleShape.Sparkle, ParticleShape.Streak },
			Colors = new[] { color, Color.white },
			Angle = new Vector2(0f, 360f),
			Size = new Vector2(20f, 46f),
			EndSizeFactor = 0.2f,
			Life = new Vector2(0.5f, 0.9f),
			SpawnRadius = radius,
			Inward = true,
			AlignToVelocity = true,
			FadeIn = true,
			FadeOutFrom = 0.85f
		};

		public static ParticleBurst Shockwave(Color color) => new()
		{
			Shapes = new[] { ParticleShape.Streak },
			Colors = new[] { color, Color.white },
			Speed = new Vector2(1400f, 2600f),
			Angle = new Vector2(0f, 360f),
			Size = new Vector2(80f, 160f),
			EndSizeFactor = 0.3f,
			Life = new Vector2(0.35f, 0.6f),
			Drag = 3.5f,
			AlignToVelocity = true,
			FadeOutFrom = 0.3f
		};

		public static ParticleBurst Firework(Color color) => new()
		{
			Shapes = new[] { ParticleShape.SoftDot, ParticleShape.Sparkle, ParticleShape.Star },
			Colors = new[] { color, Color.Lerp(color, Color.white, 0.6f) },
			Speed = new Vector2(450f, 800f),
			Angle = new Vector2(0f, 360f),
			Size = new Vector2(20f, 40f),
			EndSizeFactor = 0.3f,
			Life = new Vector2(0.9f, 1.5f),
			Gravity = -500f,
			Drag = 2.2f,
			Spin = new Vector2(0f, 180f),
			FadeOutFrom = 0.45f
		};

		public static ParticleBurst Ring(Color color) => new()
		{
			Shapes = new[] { ParticleShape.Ring },
			Colors = new[] { color },
			Speed = Vector2.zero,
			Angle = Vector2.zero,
			Size = new Vector2(120f, 120f),
			EndSizeFactor = 9f,
			Life = new Vector2(0.55f, 0.55f),
			FadeOutFrom = 0.2f
		};

		public static ParticleBurst Puff(Color color) => new()
		{
			Shapes = new[] { ParticleShape.Fog, ParticleShape.SoftDot },
			Colors = new[] { color },
			Speed = new Vector2(80f, 260f),
			Angle = new Vector2(0f, 360f),
			Size = new Vector2(70f, 140f),
			EndSizeFactor = 1.6f,
			Life = new Vector2(0.5f, 0.9f),
			Drag = 3f,
			FadeOutFrom = 0.2f
		};

		/// <summary>Rising fire for the float reveal; <paramref name="intensity"/> 0..1 scales speed and size.</summary>
		public static ParticleBurst Flames(Color color, Color tip, float intensity) => new()
		{
			Shapes = new[] { ParticleShape.Flame, ParticleShape.FlameTongue, ParticleShape.Flame, ParticleShape.Spark },
			Colors = new[] { color, tip, Color.Lerp(color, tip, 0.5f) },
			Speed = new Vector2(120f, 380f) * (0.6f + 0.7f * intensity),
			Angle = new Vector2(70f, 110f),
			Size = new Vector2(34f, 78f) * (0.65f + 0.9f * intensity),
			EndSizeFactor = 0.15f,
			Life = new Vector2(0.4f, 0.85f),
			Gravity = 420f,
			Drag = 0.8f,
			Spin = new Vector2(0f, 40f),
			WobbleAmplitude = 70f,
			WobbleFrequency = 8f,
			FadeIn = true,
			FadeOutFrom = 0.35f
		};

		public static ParticleBurst Smoke(Color color) => new()
		{
			Shapes = new[] { ParticleShape.Smoke },
			Colors = new[] { color },
			Speed = new Vector2(40f, 120f),
			Angle = new Vector2(75f, 105f),
			Size = new Vector2(70f, 130f),
			EndSizeFactor = 2f,
			Life = new Vector2(0.9f, 1.6f),
			WobbleAmplitude = 30f,
			WobbleFrequency = 2f,
			FadeIn = true,
			FadeOutFrom = 0.3f
		};

		// ---- Ambient (lobby background) ----

		public static ParticleBurst Embers(Color color) => new()
		{
			Shapes = new[] { ParticleShape.Ember, ParticleShape.SoftDot },
			Colors = new[] { color, Color.Lerp(color, Color.white, 0.4f) },
			Speed = new Vector2(40f, 120f),
			Angle = new Vector2(70f, 110f),
			Size = new Vector2(10f, 24f),
			EndSizeFactor = 0.3f,
			Life = new Vector2(5f, 9f),
			WobbleAmplitude = 40f,
			WobbleFrequency = 1.5f,
			FadeIn = true,
			FadeOutFrom = 0.6f
		};

		public static ParticleBurst Fog(Color color) => new()
		{
			Shapes = new[] { ParticleShape.Fog },
			Colors = new[] { color },
			Speed = new Vector2(20f, 60f),
			Angle = new Vector2(-10f, 10f),
			Size = new Vector2(420f, 700f),
			EndSizeFactor = 1.3f,
			Life = new Vector2(12f, 18f),
			WobbleAmplitude = 20f,
			WobbleFrequency = 0.4f,
			FadeIn = true,
			FadeOutFrom = 0.55f
		};

		/// <summary>Short, fast electric zaps that crackle off a robot.</summary>
		public static ParticleBurst Electric(Color color) => new()
		{
			Shapes = new[] { ParticleShape.Spark, ParticleShape.Streak },
			Colors = new[] { color, Color.white, color },
			Speed = new Vector2(500f, 1500f),
			Angle = new Vector2(0f, 360f),
			Size = new Vector2(26f, 64f),
			EndSizeFactor = 0.2f,
			Life = new Vector2(0.15f, 0.4f),
			Drag = 5f,
			AlignToVelocity = true,
			FadeOutFrom = 0.3f
		};

		/// <summary>Little square "pixels" drifting up and flickering out, like a hologram dissolving.</summary>
		public static ParticleBurst Pixels(Color color) => new()
		{
			Shapes = new[] { ParticleShape.Square },
			Colors = new[] { color, Color.white, new Color(color.r, color.g, color.b, 0.7f) },
			Speed = new Vector2(60f, 320f),
			Angle = new Vector2(30f, 150f),
			Size = new Vector2(10f, 24f),
			EndSizeFactor = 0.6f,
			Life = new Vector2(0.5f, 1.2f),
			Gravity = 200f,
			Drag = 1.5f,
			FadeIn = true,
			FadeOutFrom = 0.4f
		};

		/// <summary>Hearts floating up and swaying.</summary>
		public static ParticleBurst Hearts(Color[] colors) => new()
		{
			Shapes = new[] { ParticleShape.Heart },
			Colors = colors,
			Speed = new Vector2(200f, 650f),
			Angle = new Vector2(50f, 130f),
			Size = new Vector2(34f, 64f),
			EndSizeFactor = 0.5f,
			Life = new Vector2(1f, 1.8f),
			Gravity = 320f,
			Drag = 2f,
			Spin = new Vector2(0f, 40f),
			WobbleAmplitude = 60f,
			WobbleFrequency = 3f,
			FadeIn = true,
			FadeOutFrom = 0.6f
		};

		public static ParticleBurst Twinkle() => new()
		{
			Shapes = new[] { ParticleShape.Sparkle, ParticleShape.SoftDot },
			Colors = White,
			Size = new Vector2(10f, 26f),
			EndSizeFactor = 0.6f,
			Life = new Vector2(2f, 4.5f),
			Spin = new Vector2(10f, 40f),
			FadeIn = true,
			FadeOutFrom = 0.4f
		};

		/// <summary>Ambient: one soft snowflake drifting down from the top of the sky.</summary>
		public static ParticleBurst Snowfall(Color color) => new()
		{
			Shapes = new[] { ParticleShape.SoftDot, ParticleShape.SoftDot, ParticleShape.Sparkle },
			Colors = new[] { color, Color.Lerp(color, Color.white, 0.6f) },
			Speed = new Vector2(50f, 120f),
			Angle = new Vector2(250f, 290f),
			Size = new Vector2(10f, 30f),
			EndSizeFactor = 0.8f,
			Life = new Vector2(14f, 22f),
			Spin = new Vector2(10f, 60f),
			WobbleAmplitude = 70f,
			WobbleFrequency = 0.7f,
			FadeIn = true,
			FadeOutFrom = 0.8f
		};

		/// <summary>Ambient: a confetti flake tumbling slowly down (birthdays).</summary>
		public static ParticleBurst DriftingConfetti(Color[] colors) => new()
		{
			Shapes = new[] { ParticleShape.Confetti, ParticleShape.Square, ParticleShape.Star },
			Colors = colors,
			Speed = new Vector2(60f, 140f),
			Angle = new Vector2(250f, 290f),
			Size = new Vector2(16f, 30f),
			EndSizeFactor = 0.9f,
			Life = new Vector2(12f, 18f),
			Spin = new Vector2(90f, 300f),
			WobbleAmplitude = 90f,
			WobbleFrequency = 1.1f,
			FadeIn = true,
			FadeOutFrom = 0.8f
		};

		/// <summary>Ambient: a small heart floating up.</summary>
		public static ParticleBurst RisingHeart(Color color) => new()
		{
			Shapes = new[] { ParticleShape.Heart },
			Colors = new[] { color, Color.Lerp(color, Color.white, 0.35f) },
			Speed = new Vector2(40f, 110f),
			Angle = new Vector2(75f, 105f),
			Size = new Vector2(18f, 38f),
			EndSizeFactor = 0.5f,
			Life = new Vector2(6f, 10f),
			Spin = new Vector2(0f, 30f),
			WobbleAmplitude = 45f,
			WobbleFrequency = 1.3f,
			FadeIn = true,
			FadeOutFrom = 0.6f
		};

		public static ParticleBurst FlyingBat(Color color, bool leftToRight) => new()
		{
			Shapes = new[] { ParticleShape.Bat },
			Colors = new[] { color },
			Speed = new Vector2(220f, 380f),
			Angle = leftToRight ? new Vector2(-8f, 12f) : new Vector2(168f, 188f),
			Size = new Vector2(40f, 70f),
			EndSizeFactor = 1f,
			Life = new Vector2(7f, 9f),
			FlapFrequency = 14f,
			WobbleAmplitude = 60f,
			WobbleFrequency = 2.4f,
			FadeIn = true,
			FadeOutFrom = 0.9f
		};
	}
}
