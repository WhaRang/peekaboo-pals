using _Main.Scripts._ProjectAgnostic.Services.EventBus;

namespace _Main.Scripts.Services.Events
{
	/// <summary>Published once per pack, on the tick its last character is collected.</summary>
	public readonly struct OnCollectionCompletedEvent : IEventBusEvent
	{
		public string PackId { get; }
		public int BonusPoints { get; }
		public long CompletionDurationInSeconds { get; }

		public OnCollectionCompletedEvent(string packId, int bonusPoints, long completionDurationInSeconds)
		{
			PackId = packId;
			BonusPoints = bonusPoints;
			CompletionDurationInSeconds = completionDurationInSeconds;
		}
	}
}
