using System.Collections.Generic;

namespace _Main.Scripts.Ecs.Collection.Cmp
{
	/// <summary>
	/// Every collection's progress. Timestamps are real wall-clock unix seconds (not the pause-shifted session clock).
	/// Progress of packs this build doesn't have is kept as-is, so it survives until the pack arrives.
	/// </summary>
	public struct CollectionSinglCmp
	{
		/// <summary>Pack id → progress. Case-insensitive keys.</summary>
		public Dictionary<string, PackProgress> Packs;

		/// <summary>Set by any writer; <c>PublishCollectionStateSys</c> publishes a snapshot and clears it.</summary>
		public bool IsDirty;
	}

	/// <summary>One collection's progress.</summary>
	public sealed class PackProgress
	{
		/// <summary>Character id → what the player caught. Case-insensitive keys.</summary>
		public readonly Dictionary<string, CaughtCreature> Caught = new(System.StringComparer.OrdinalIgnoreCase);
		public long? FirstScanUnixSeconds;
		public long? CompletedUnixSeconds;
	}
}
