using System.Collections.Generic;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Ecs.Collection.Cmp;
using _Main.Scripts.Services.Packs;

namespace _Main.Scripts.Ecs.Collection.Sys
{
	/// <summary>Pure collection rules shared by the collection systems, the save restore and the UI.</summary>
	public static class CollectionRules
	{
		public static bool IsEveryCharacterCollected(ICollectionPack pack, IReadOnlyDictionary<string, CaughtCreature> caught)
		{
			if (pack.Characters.Count == 0)
				return false;

			foreach (var character in pack.Characters)
			{
				if (!caught.ContainsKey(character.Id))
					return false;
			}

			return true;
		}

		/// <summary>What one caught character is worth: its tier points plus the bonus of its best float's grade.</summary>
		public static int CharacterPoints(ICollectionPack pack, CharacterDefinition character, float bestFloat)
			=> pack.GetPoints(character) + FloatBonus(pack, bestFloat);

		public static int FloatBonus(ICollectionPack pack, float value)
			=> pack.FloatGrades.Count == 0 ? 0 : pack.GetFloatGrade(value).BonusPoints;

		/// <summary>One collection's score: every caught character (tier + best-float bonus), plus the completion bonus once complete.</summary>
		public static int CalculatePoints(ICollectionPack pack, IReadOnlyDictionary<string, CaughtCreature> caught, bool isComplete)
		{
			int points = 0;
			foreach (var character in pack.Characters)
			{
				if (caught.TryGetValue(character.Id, out var creature))
					points += CharacterPoints(pack, character, creature.BestFloat);
			}

			return isComplete ? points + pack.CompletionBonusPoints : points;
		}

		/// <summary>The total score over every collection this build knows (unknown packs count once they arrive).</summary>
		public static int CalculateTotalPoints(IPackLibrary library, IReadOnlyDictionary<string, PackProgress> packs)
		{
			int total = 0;
			foreach (var pair in packs)
			{
				if (library.TryGetPack(pair.Key, out var pack))
					total += CalculatePoints(pack, pair.Value.Caught, pair.Value.CompletedUnixSeconds.HasValue);
			}

			return total;
		}

		public static PackProgress GetOrCreate(this in CollectionSinglCmp collection, string packId)
		{
			if (!collection.Packs.TryGetValue(packId, out var progress))
			{
				progress = new PackProgress();
				collection.Packs[packId] = progress;
			}

			return progress;
		}
	}
}
