using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Fx.Particles
{
	/// <summary>
	/// A whole UI particle system in ONE graphic: particles live in a preallocated array (no GameObjects, no GC) and
	/// are drawn as quads of a single atlas texture, so any number of particles is one draw call and one mesh rebuild.
	/// Coordinates are local to this rect (pivot at the center).
	/// </summary>
	[RequireComponent(typeof(CanvasRenderer))]
	public sealed class UiParticleGraphic : MaskableGraphic
	{
		private struct Particle
		{
			public Vector2 Position;
			public Vector2 Velocity;
			public float Rotation;
			public float Spin;
			public float Age;
			public float Life;
			public float StartSize;
			public float EndSize;
			public Color32 Color;
			public int Shape;
			public float Gravity;
			public float Drag;
			public float Swirl;
			public float WobbleAmplitude;
			public float WobbleFrequency;
			public float FlapFrequency;
			public float Phase;
			public float FadeOutFrom;
			public bool FadeIn;
			public bool AlignToVelocity;
		}

		private Particle[] _particles = new Particle[512];
		private int _count;
		private bool _hadParticlesLastFrame;
		private Texture? _atlas;
		private int _columns = 4;
		private int _rows = 4;

		/// <summary>Multiplies every emitted count (reduced motion uses a fraction).</summary>
		public float CountScale { get; set; } = 1f;
		public int AliveCount => _count;

		public override Texture mainTexture => _atlas != null ? _atlas : s_WhiteTexture;

		public void Setup(Texture? atlas, int columns, int rows, int capacity)
		{
			_atlas = atlas;
			_columns = Mathf.Max(1, columns);
			_rows = Mathf.Max(1, rows);
			_particles = new Particle[capacity];
			_count = 0;
			raycastTarget = false;
			SetMaterialDirty();
			SetVerticesDirty();
		}

		public void EmitAtWorld(Vector3 worldPosition, in ParticleBurst burst, int count)
		{
			Vector2 local = rectTransform.InverseTransformPoint(worldPosition);
			Emit(local, burst, count);
		}

		public void Emit(Vector2 localPosition, in ParticleBurst burst, int count)
		{
			count = Mathf.CeilToInt(count * CountScale);

			for (int n = 0; n < count; n++)
			{
				if (_count >= _particles.Length)
					return;

				ref var p = ref _particles[_count++];

				float angle = Random.Range(burst.Angle.x, burst.Angle.y) * Mathf.Deg2Rad;
				var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
				float speed = Random.Range(burst.Speed.x, burst.Speed.y);
				float life = Random.Range(burst.Life.x, burst.Life.y);

				var spawn = localPosition;
				if (burst.Inward)
				{
					spawn += direction * burst.SpawnRadius;
					p.Velocity = -direction * (burst.SpawnRadius / Mathf.Max(0.05f, life));
				}
				else
				{
					if (burst.SpawnRadius > 0f)
						spawn += Random.insideUnitCircle * burst.SpawnRadius;
					p.Velocity = direction * speed;
				}

				if (burst.SpawnBox != Vector2.zero)
					spawn += new Vector2(Random.Range(-0.5f, 0.5f) * burst.SpawnBox.x, Random.Range(-0.5f, 0.5f) * burst.SpawnBox.y);

				p.Position = spawn;
				p.Rotation = Random.Range(0f, 360f);
				p.Spin = Random.Range(burst.Spin.x, burst.Spin.y) * (Random.value < 0.5f ? -1f : 1f);
				p.Age = 0f;
				p.Life = Mathf.Max(0.05f, life);
				p.StartSize = Random.Range(burst.Size.x, burst.Size.y);
				p.EndSize = p.StartSize * burst.EndSizeFactor;
				p.Color = burst.Colors is { Length: > 0 } ? (Color32)burst.Colors[Random.Range(0, burst.Colors.Length)] : (Color32)Color.white;
				p.Shape = burst.Shapes is { Length: > 0 } ? (int)burst.Shapes[Random.Range(0, burst.Shapes.Length)] : 0;
				p.Gravity = burst.Gravity;
				p.Drag = burst.Drag;
				p.Swirl = burst.Swirl * Mathf.Deg2Rad;
				p.WobbleAmplitude = burst.WobbleAmplitude;
				p.WobbleFrequency = burst.WobbleFrequency;
				p.FlapFrequency = burst.FlapFrequency;
				p.Phase = Random.Range(0f, Mathf.PI * 2f);
				p.FadeIn = burst.FadeIn;
				p.FadeOutFrom = burst.FadeOutFrom <= 0f ? 0.6f : burst.FadeOutFrom;
				p.AlignToVelocity = burst.AlignToVelocity;
				if (p.AlignToVelocity)
					p.Rotation = angle * Mathf.Rad2Deg;
			}
		}

		public void Clear()
		{
			_count = 0;
			SetVerticesDirty();
		}

		private void Update()
		{
			if (_count == 0)
			{
				if (_hadParticlesLastFrame)
				{
					_hadParticlesLastFrame = false;
					SetVerticesDirty();
				}

				return;
			}

			float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

			for (int i = 0; i < _count; i++)
			{
				ref var p = ref _particles[i];
				p.Age += dt;

				if (p.Age >= p.Life)
				{
					_particles[i] = _particles[--_count];
					i--;
					continue;
				}

				if (p.Swirl != 0f)
				{
					float s = Mathf.Sin(p.Swirl * dt);
					float c = Mathf.Cos(p.Swirl * dt);
					p.Velocity = new Vector2(p.Velocity.x * c - p.Velocity.y * s, p.Velocity.x * s + p.Velocity.y * c);
				}

				p.Velocity.y += p.Gravity * dt;
				if (p.Drag > 0f)
					p.Velocity *= Mathf.Max(0f, 1f - p.Drag * dt);

				p.Position += p.Velocity * dt;
				if (p.WobbleAmplitude > 0f)
					p.Position.x += Mathf.Sin(p.Age * p.WobbleFrequency + p.Phase) * p.WobbleAmplitude * dt;

				p.Rotation = p.AlignToVelocity && p.Velocity.sqrMagnitude > 1f
					? Mathf.Atan2(p.Velocity.y, p.Velocity.x) * Mathf.Rad2Deg
					: p.Rotation + p.Spin * dt;
			}

			_hadParticlesLastFrame = true;
			SetVerticesDirty();
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			float cellW = 1f / _columns;
			float cellH = 1f / _rows;

			for (int i = 0; i < _count; i++)
			{
				ref var p = ref _particles[i];
				float t = p.Age / p.Life;

				float size = Mathf.Lerp(p.StartSize, p.EndSize, t);
				float alpha = t > p.FadeOutFrom ? 1f - (t - p.FadeOutFrom) / (1f - p.FadeOutFrom) : 1f;
				if (p.FadeIn)
					alpha *= Mathf.Clamp01(t * 5f);

				float halfW = size * 0.5f;
				float halfH = halfW;
				if (p.FlapFrequency > 0f)
					halfH *= 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(p.Age * p.FlapFrequency + p.Phase));
				if (p.AlignToVelocity)
					halfH *= 0.35f;

				float rad = p.Rotation * Mathf.Deg2Rad;
				float cos = Mathf.Cos(rad);
				float sin = Mathf.Sin(rad);
				var right = new Vector2(cos * halfW, sin * halfW);
				var up = new Vector2(-sin * halfH, cos * halfH);

				int column = p.Shape % _columns;
				int row = p.Shape / _columns;
				float u0 = column * cellW;
				float u1 = u0 + cellW;
				float v1 = 1f - row * cellH;
				float v0 = v1 - cellH;

				var color = p.Color;
				color.a = (byte)(color.a * Mathf.Clamp01(alpha) * this.color.a);

				int start = vh.currentVertCount;
				vh.AddVert(p.Position - right - up, color, new Vector4(u0, v0));
				vh.AddVert(p.Position - right + up, color, new Vector4(u0, v1));
				vh.AddVert(p.Position + right + up, color, new Vector4(u1, v1));
				vh.AddVert(p.Position + right - up, color, new Vector4(u1, v0));
				vh.AddTriangle(start, start + 1, start + 2);
				vh.AddTriangle(start + 2, start + 3, start);
			}
		}
	}
}
