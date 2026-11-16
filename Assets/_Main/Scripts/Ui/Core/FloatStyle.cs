using _Main.Scripts.Configs.Collection;
using UnityEngine;

namespace _Main.Scripts.Ui.Core
{
	/// <summary>How floats are printed and how grades map to UI looks.</summary>
	public static class FloatStyle
	{
		public static string Short(float value) => value.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);
		public static string Long(float value) => value.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);

		/// <summary>0 for the worst grade … 1 for the best, based on the grade's position in the config.</summary>
		public static float Rarity01(ICollectionPack config, float value)
		{
			var grades = config.FloatGrades;
			if (grades.Count <= 1)
				return 0f;
			var grade = config.GetFloatGrade(value);
			int index = 0;
			for (int i = 0; i < grades.Count; i++)
			{
				if (grades[i] == grade)
					index = i;
			}

			return 1f - index / (float)(grades.Count - 1);
		}

		/// <summary>Chip/label color; the top grade shimmers through its flame colors.</summary>
		public static Color AnimatedColor(FloatGradeDefinition grade, float time)
		{
			if (grade.FlameIntensity < 0.95f)
				return grade.Color;
			return Color.Lerp(grade.FlameColor, grade.FlameTipColor, 0.5f + 0.5f * Mathf.Sin(time * 3f));
		}
	}
}
