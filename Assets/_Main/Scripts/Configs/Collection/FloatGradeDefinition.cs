using System;
using UnityEngine;

namespace _Main.Scripts.Configs.Collection
{
	/// <summary>
	/// One band of the catch "float" (0 = flawless, 1 = crumbly), CS-skin style. A catch first picks a grade by
	/// <see cref="Weight"/>, then a uniform float inside the grade's range, so low floats are rare by construction.
	/// The look fields drive how the card renders (metallic + contrasty when rare, pale + scratched when not).
	/// </summary>
	[Serializable]
	public sealed class FloatGradeDefinition
	{
		[SerializeField] private string _name = "Well Spooked";
		[Tooltip("Exclusive upper bound of this grade. Grades are sorted by this; the last one should end at 1.")]
		[SerializeField, Range(0.0001f, 1f)] private float _maxFloat = 0.38f;
		[Tooltip("Relative chance of rolling this grade (all weights are normalized).")]
		[SerializeField, Min(0f)] private float _weight = 30f;
		[Tooltip("Points on top of the character's tier points when this is the best float caught for it.")]
		[SerializeField, Min(0)] private int _bonusPoints;
		[SerializeField] private Color _color = Color.white;

		[Header("Card look")]
		[SerializeField, Range(0f, 1f)] private float _metallic;
		[SerializeField, Range(0f, 2f)] private float _saturation = 1f;
		[SerializeField, Range(0.3f, 2f)] private float _contrast = 1f;
		[Tooltip("How washed-out (pale) the art looks.")]
		[SerializeField, Range(0f, 1f)] private float _wash;
		[SerializeField, Range(0f, 1f)] private float _scratches;

		[Header("Reveal")]
		[Tooltip("0 = a puff of smoke, 1 = a towering bonfire.")]
		[SerializeField, Range(0f, 1f)] private float _flameIntensity;
		[SerializeField] private Color _flameColor = new(1f, 0.55f, 0.1f);
		[SerializeField] private Color _flameTipColor = new(1f, 0.9f, 0.35f);

		public string Name => _name;
		public float MaxFloat => _maxFloat;
		public float Weight => _weight;
		public int BonusPoints => _bonusPoints;
		public Color Color => _color;
		public float Metallic => _metallic;
		public float Saturation => _saturation;
		public float Contrast => _contrast;
		public float Wash => _wash;
		public float Scratches => _scratches;
		public float FlameIntensity => _flameIntensity;
		public Color FlameColor => _flameColor;
		public Color FlameTipColor => _flameTipColor;

		/// <summary>A copy with a pack's own name for this grade (empty keeps the default).</summary>
		public FloatGradeDefinition WithName(string? name)
		{
			var copy = (FloatGradeDefinition)MemberwiseClone();
			if (!string.IsNullOrWhiteSpace(name))
				copy._name = name!.Trim();
			return copy;
		}

#if UNITY_EDITOR
		public static FloatGradeDefinition EditorCreate(string name, float maxFloat, float weight, int bonus, Color color,
			float metallic, float saturation, float contrast, float wash, float scratches, float flame, Color flameColor, Color flameTip)
		{
			return new FloatGradeDefinition
			{
				_name = name, _maxFloat = maxFloat, _weight = weight, _bonusPoints = bonus, _color = color,
				_metallic = metallic, _saturation = saturation, _contrast = contrast, _wash = wash, _scratches = scratches,
				_flameIntensity = flame, _flameColor = flameColor, _flameTipColor = flameTip
			};
		}
#endif
	}
}
