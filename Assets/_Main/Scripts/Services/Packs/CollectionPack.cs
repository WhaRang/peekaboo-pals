using System;
using System.Collections.Generic;
using _Main.Scripts.Configs.Collection;

namespace _Main.Scripts.Services.Packs
{
	/// <summary>
	/// A loaded collection pack: its characters, theme and texts on top of the shared <see cref="ICollectionRules"/>.
	/// Tiers and float grades are the rules' own, renamed/recolored by the pack; cooldown and completion bonus are read
	/// live from the rules, so every pack scores the same.
	/// </summary>
	public sealed class CollectionPack : ICollectionPack
	{
		private readonly ICollectionRules _rules;
		private readonly List<CharacterDefinition> _characters;
		private readonly Dictionary<string, CharacterDefinition> _byId = new(StringComparer.OrdinalIgnoreCase);

		public string Id { get; }
		public string Title { get; }
		public DateTime? Date { get; }
		public string Tagline { get; }
		public string? LegacyPrefix { get; }
		public PackTheme Theme { get; }
		public PackTexts Texts { get; }
		public IReadOnlyList<CharacterDefinition> Characters => _characters;
		public IReadOnlyList<RarityTierDefinition> RarityTiers { get; }
		public IReadOnlyList<FloatGradeDefinition> FloatGrades { get; }
		public int CompletionBonusPoints => _rules.CompletionBonusPoints;
		public int RecatchCooldownSeconds => _rules.RecatchCooldownSeconds;

		public CollectionPack(ICollectionRules rules, string id, string title, DateTime? date, string tagline, string? legacyPrefix,
			PackTheme theme, PackTexts texts, List<CharacterDefinition> characters,
			IReadOnlyList<RarityTierDefinition> tiers, IReadOnlyList<FloatGradeDefinition> floatGrades)
		{
			_rules = rules;
			Id = id;
			Title = title;
			Date = date;
			Tagline = tagline;
			LegacyPrefix = legacyPrefix;
			Theme = theme;
			Texts = texts;
			_characters = characters;
			RarityTiers = tiers;
			FloatGrades = floatGrades;
			foreach (var character in characters)
				_byId[character.Id] = character;
		}

		public RarityTierDefinition GetTier(CharacterRarity rarity) => CollectionConfig.FindTier(RarityTiers, rarity);

		public FloatGradeDefinition GetFloatGrade(float value) => CollectionConfig.FindFloatGrade(FloatGrades, value);

		public bool TryGetCharacter(string id, out CharacterDefinition character) => _byId.TryGetValue(id, out character!);

		public int GetPoints(CharacterDefinition character) => GetTier(character.Rarity).Points;
	}
}
