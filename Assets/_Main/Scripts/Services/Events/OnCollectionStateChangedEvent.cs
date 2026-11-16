using System;
using System.Collections.Generic;
using _Main.Scripts._ProjectAgnostic.Services.EventBus;
using _Main.Scripts.Ecs.Collection.Cmp;

namespace _Main.Scripts.Services.Events
{
	/// <summary>Immutable copy of one collection's progress, safe to keep on the UI side.</summary>
	public sealed class CollectionSnapshot
	{
		public static readonly CollectionSnapshot Empty = new(string.Empty, new Dictionary<string, CaughtCreature>(), null, null, 0, 0);

		private readonly Dictionary<string, CaughtCreature> _caught;

		public string PackId { get; }

		/// <summary>Character id → what was caught (times, catch count, best float).</summary>
		public IReadOnlyDictionary<string, CaughtCreature> Caught => _caught;
		public long? FirstScanUnixSeconds { get; }
		public long? CompletedUnixSeconds { get; }

		/// <summary>This collection's points (tier + float bonuses + completion bonus).</summary>
		public int TotalPoints { get; }
		public int TotalCharacters { get; }
		public int CollectedCount => _caught.Count;
		public bool IsComplete => CompletedUnixSeconds.HasValue;

		public CollectionSnapshot(string packId, IReadOnlyDictionary<string, CaughtCreature> caught, long? firstScan, long? completed, int totalPoints, int totalCharacters)
		{
			PackId = packId;
			_caught = new Dictionary<string, CaughtCreature>(StringComparer.OrdinalIgnoreCase);
			foreach (var pair in caught)
				_caught[pair.Key] = pair.Value;
			FirstScanUnixSeconds = firstScan;
			CompletedUnixSeconds = completed;
			TotalPoints = totalPoints;
			TotalCharacters = totalCharacters;
		}

		public bool IsCollected(string characterId) => _caught.ContainsKey(characterId);

		public bool TryGet(string characterId, out CaughtCreature creature) => _caught.TryGetValue(characterId, out creature);
	}

	/// <summary>Every collection's snapshot plus the lifetime score.</summary>
	public sealed class AlbumSnapshot
	{
		public static readonly AlbumSnapshot Empty = new(new Dictionary<string, CollectionSnapshot>(), 0);

		private readonly Dictionary<string, CollectionSnapshot> _collections;

		public IReadOnlyDictionary<string, CollectionSnapshot> Collections => _collections;

		/// <summary>Points over every collection.</summary>
		public int TotalScore { get; }

		public AlbumSnapshot(Dictionary<string, CollectionSnapshot> collections, int totalScore)
		{
			_collections = new Dictionary<string, CollectionSnapshot>(collections, StringComparer.OrdinalIgnoreCase);
			TotalScore = totalScore;
		}

		/// <summary>The pack's snapshot, or an empty one with <paramref name="totalCharacters"/> slots when nothing was caught yet.</summary>
		public CollectionSnapshot Get(string packId, int totalCharacters)
			=> _collections.TryGetValue(packId, out var snapshot)
				? snapshot
				: new CollectionSnapshot(packId, new Dictionary<string, CaughtCreature>(), null, null, 0, totalCharacters);
	}

	/// <summary>Published after every change to any collection (restore, new catch, upgrade, completion, reset).</summary>
	public readonly struct OnCollectionStateChangedEvent : IEventBusEvent
	{
		public AlbumSnapshot Album { get; }

		public OnCollectionStateChangedEvent(AlbumSnapshot album)
		{
			Album = album;
		}
	}
}
