using System.Collections.Generic;

namespace _Main.Scripts.Configs.Collection
{
	/// <summary>Rules shared by every collection pack (the <see cref="CollectionConfig"/> asset).</summary>
	public interface ICollectionRules
	{
		int CompletionBonusPoints { get; }
		IReadOnlyList<RarityTierDefinition> RarityTiers { get; }

		/// <summary>Float grades, best (lowest floats) first.</summary>
		IReadOnlyList<FloatGradeDefinition> FloatGrades { get; }

		/// <summary>Seconds before the same character can be caught again (a new float roll).</summary>
		int RecatchCooldownSeconds { get; }

		RarityTierDefinition GetTier(CharacterRarity rarity);
		FloatGradeDefinition GetFloatGrade(float value);
	}

	/// <summary>
	/// One collection (one party): its characters plus the shared rules, with the pack's own tier/grade names and
	/// colors applied. Everything the collection systems and the UI need to know about "a collection".
	/// </summary>
	public interface ICollectionPack
	{
		string Id { get; }
		string Title { get; }
		PackTheme Theme { get; }
		PackTexts Texts { get; }

		int CompletionBonusPoints { get; }
		IReadOnlyList<CharacterDefinition> Characters { get; }
		IReadOnlyList<RarityTierDefinition> RarityTiers { get; }

		/// <summary>Float grades, best (lowest floats) first.</summary>
		IReadOnlyList<FloatGradeDefinition> FloatGrades { get; }

		int RecatchCooldownSeconds { get; }

		RarityTierDefinition GetTier(CharacterRarity rarity);
		bool TryGetCharacter(string id, out CharacterDefinition character);

		/// <summary>Tier points only (no float bonus).</summary>
		int GetPoints(CharacterDefinition character);

		FloatGradeDefinition GetFloatGrade(float value);
	}
}
