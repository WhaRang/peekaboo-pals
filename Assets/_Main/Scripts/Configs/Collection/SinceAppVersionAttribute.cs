using System;

namespace _Main.Scripts.Configs.Collection
{
	/// <summary>
	/// Marks a pack feature (an ambient style, a signature effect…) added after the first downloadable app version.
	/// Build Packs raises a pack's minimum app version to the newest feature it uses, so phones with an older app show
	/// "update the app" instead of a half-working collection. Tag every new enum member with the version that adds it.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class SinceAppVersionAttribute : Attribute
	{
		public string Version { get; }

		public SinceAppVersionAttribute(string version)
		{
			Version = version;
		}
	}
}
