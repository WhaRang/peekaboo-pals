namespace _Main.Scripts.Ecs.Collection.Cmp
{
	/// <summary>What the player owns for one character: when it was first/last caught, how often, and the best float.</summary>
	public struct CaughtCreature
	{
		public long FirstCaughtAtUnixSeconds;
		public long LastCaughtAtUnixSeconds;
		/// <summary>Lowest (rarest) float caught so far, 0..1.</summary>
		public float BestFloat;
		public int CatchCount;
	}
}
