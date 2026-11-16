using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Main.Scripts.Configs.Collection
{
	/// <summary>
	/// The game rules every collection pack shares: rarity tiers (points, colors, reveal feel), float grades, the
	/// re-catch cooldown and the completion bonus. Packs (Packs/&lt;id&gt;/pack.json) bring the characters and may
	/// rename/recolor tiers and grades, but scoring and reveal drama stay the same across every party.
	/// </summary>
	[CreateAssetMenu(fileName = "CollectionConfig", menuName = "Configs/CollectionConfig")]
	public sealed class CollectionConfig : ScriptableObject, ICollectionRules
	{
		[SerializeField, Min(0)] private int _completionBonusPoints = 100;
		[SerializeField] private List<RarityTierDefinition> _rarityTiers = new();

		[Header("Float (catch quality)")]
		[Tooltip("A character can be caught again after this many seconds; only a better (lower) float raises the score.")]
		[SerializeField, Min(0)] private int _recatchCooldownSeconds = 180;
		[Tooltip("Best grade first. Each grade covers floats up to its Max Float.")]
		[SerializeField] private List<FloatGradeDefinition> _floatGrades = new();

		public int CompletionBonusPoints => _completionBonusPoints;
		public IReadOnlyList<RarityTierDefinition> RarityTiers => _rarityTiers;
		public IReadOnlyList<FloatGradeDefinition> FloatGrades => _floatGrades;
		public int RecatchCooldownSeconds => _recatchCooldownSeconds;

		public FloatGradeDefinition GetFloatGrade(float value) => FindFloatGrade(_floatGrades, value);

		public RarityTierDefinition GetTier(CharacterRarity rarity) => FindTier(_rarityTiers, rarity);

		public static FloatGradeDefinition FindFloatGrade(IReadOnlyList<FloatGradeDefinition> grades, float value)
		{
			if (grades.Count == 0)
				throw new InvalidOperationException($"[{nameof(CollectionConfig)}] No float grades configured.");

			foreach (var grade in grades)
			{
				if (value < grade.MaxFloat)
					return grade;
			}

			return grades[grades.Count - 1];
		}

		public static RarityTierDefinition FindTier(IReadOnlyList<RarityTierDefinition> tiers, CharacterRarity rarity)
		{
			foreach (var tier in tiers)
			{
				if (tier.Rarity == rarity)
					return tier;
			}

			throw new InvalidOperationException($"[{nameof(CollectionConfig)}] No rarity tier configured for {rarity}.");
		}

		private void OnValidate()
		{
			_floatGrades.Sort((a, b) => a.MaxFloat.CompareTo(b.MaxFloat));
		}

#if UNITY_EDITOR
		public void EditorSetFloat(int recatchCooldownSeconds, List<FloatGradeDefinition> grades)
		{
			_recatchCooldownSeconds = recatchCooldownSeconds;
			_floatGrades = grades;
		}

		public void EditorSetTiers(int completionBonus, List<RarityTierDefinition> tiers)
		{
			_completionBonusPoints = completionBonus;
			_rarityTiers = tiers;
		}

		public List<RarityTierDefinition> EditorTiers => _rarityTiers;
#endif
	}
}
