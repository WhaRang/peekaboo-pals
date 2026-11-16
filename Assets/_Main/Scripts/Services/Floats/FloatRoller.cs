using _Main.Scripts.Configs.Collection;
using JetBrains.Annotations;
using UnityEngine;

namespace _Main.Scripts.Services.Floats
{
	public interface IFloatRoller
	{
		/// <summary>A fresh catch float in [0, 1): grade picked by weight, then uniform inside the grade.</summary>
		float Roll();
	}

	[UsedImplicitly]
	public sealed class FloatRoller : IFloatRoller
	{
		private readonly ICollectionRules _config;

		public FloatRoller(ICollectionRules config)
		{
			_config = config;
		}

#if SPOOKY_AUTOPILOT
		/// <summary>Dev autopilot only: the next roll returns this value.</summary>
		public static float? ForcedNext;
#endif

		public float Roll()
		{
#if SPOOKY_AUTOPILOT
			if (ForcedNext.HasValue)
			{
				float forced = ForcedNext.Value;
				ForcedNext = null;
				return forced;
			}
#endif
			var grades = _config.FloatGrades;
			if (grades.Count == 0)
				return Random.value;

			float total = 0f;
			foreach (var grade in grades)
				total += grade.Weight;

			float pick = Random.value * total;
			float lower = 0f;
			for (int i = 0; i < grades.Count; i++)
			{
				var grade = grades[i];
				if (pick < grade.Weight || i == grades.Count - 1)
					return Mathf.Clamp(Random.Range(lower, grade.MaxFloat), 0f, 0.99999f);

				pick -= grade.Weight;
				lower = grade.MaxFloat;
			}

			return Random.value;
		}
	}
}
