using System.Collections.Generic;

namespace _Main.Scripts.Ecs.GameSaving.Data
{
	/// <summary>
	/// The serialized session snapshot. Extend this with game-specific sections (board state,
	/// inventory, ...) and fill them in <c>SaveGameStateSys</c> / restore them in <c>StartSessionSys</c>.
	/// </summary>
	public struct GameSaveData
	{
		public TimeSaveData Time;
		public ScoreSaveData Score;

		/// <summary>
		/// Saves from before collection packs (Halloween 2026 only). Read once and migrated into
		/// <see cref="Packs"/>; never written again.
		/// </summary>
		public CollectionSaveData Collection;

		/// <summary>Every collection's progress, including packs this build doesn't have (kept untouched).</summary>
		public List<CollectionSaveData>? Packs;
	}

	public struct TimeSaveData
	{
		public long SessionStartTimeStamp;
		public long LastRecordedServerTimeStamp;
		public int RemainingPauseDurationInSeconds;
	}

	public struct ScoreSaveData
	{
		public int CurrentScore;
		public int CurrentMultiplier;
		public bool ComboActivated;
		public long? LastComboTriggerTimestampUnixSeconds;
	}

	public struct CollectionSaveData
	{
		/// <summary>Null in the pre-pack save (it was always Halloween 2026).</summary>
		public string? PackId;
		public List<CollectedCharacterSaveData>? Collected;
		public long? FirstScanUnixSeconds;
		public long? CompletedUnixSeconds;
	}

	public struct CollectedCharacterSaveData
	{
		public string Id;
		/// <summary>First catch (kept under its original name for older saves).</summary>
		public long CollectedAtUnixSeconds;
		public long LastCaughtAtUnixSeconds;
		/// <summary>Null in saves made before floats existed.</summary>
		public float? BestFloat;
		public int CatchCount;
	}
}
