using _Main.Scripts._ProjectAgnostic.Services.EventBus;
using _Main.Scripts.Services.Qr;

namespace _Main.Scripts.Services.Events
{
	public enum QrScanOutcome
	{
		/// <summary>First catch of this character.</summary>
		NewCharacter = 0,
		/// <summary>Caught again too soon: the character is still "resting".</summary>
		OnCooldown = 1,
		/// <summary>Caught again with a rarer (lower) float than before.</summary>
		FloatUpgrade = 2,
		/// <summary>Caught again, but the float isn't better than the best one.</summary>
		NoImprovement = 3,
		Invalid = 4
	}

	/// <summary>Published by <c>ProcessQrScanSys</c> once per scanned payload.</summary>
	public readonly struct OnQrScanResolvedEvent : IEventBusEvent
	{
		public QrScanOutcome Outcome { get; }

		/// <summary>Empty for <see cref="QrScanOutcome.Invalid"/>.</summary>
		public string PackId { get; }

		/// <summary>Empty for <see cref="QrScanOutcome.Invalid"/>.</summary>
		public string CharacterId { get; }

		/// <summary>Score gained by this scan (tier + float bonus for a first catch, the bonus difference for an upgrade).</summary>
		public int PointsAwarded { get; }

		/// <summary>The float rolled by this catch (unset for cooldown/invalid).</summary>
		public float Float { get; }

		/// <summary>Best float before this scan (the new best stays in the snapshot).</summary>
		public float PreviousBestFloat { get; }

		public int CooldownRemainingSeconds { get; }

		/// <summary>True when this scan collected the last missing character of its pack.</summary>
		public bool CompletesCollection { get; }

		public QrRejectionReason RejectionReason { get; }

		public OnQrScanResolvedEvent(QrScanOutcome outcome, string packId, string characterId, int pointsAwarded = 0, float value = 1f, float previousBest = 1f,
			int cooldownRemaining = 0, bool completesCollection = false, QrRejectionReason rejectionReason = default)
		{
			Outcome = outcome;
			PackId = packId;
			CharacterId = characterId;
			PointsAwarded = pointsAwarded;
			Float = value;
			PreviousBestFloat = previousBest;
			CooldownRemainingSeconds = cooldownRemaining;
			CompletesCollection = completesCollection;
			RejectionReason = rejectionReason;
		}
	}
}
