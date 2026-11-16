using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace _Main.Scripts.Editor
{
	/// <summary>Command-line builds: the Android APK, and a macOS dev player with the screenshot autopilot.</summary>
	public static class SpookyDevBuilds
	{
		[MenuItem("Party/Build Android APK", priority = 40)]
		public static void BuildAndroid()
		{
			EditorUserBuildSettings.buildAppBundle = false;
			Build(BuildTarget.Android, "Builds/Android/SpookyCollector.apk", BuildOptions.None, null);
		}

		/// <summary>
		/// Browser build for iPhone (and anyone else): one HTTPS link, no store. Gzip with decompression fallback works
		/// on any static host (itch.io, Netlify, GitHub Pages) without server configuration.
		/// </summary>
		[MenuItem("Party/Build WebGL (iPhone + browsers)", priority = 41)]
		public static void BuildWebGL() => BuildWebGLInternal(BuildOptions.None, null, "Builds/WebGL");

		/// <summary>Dev WebGL build with the scan self-test (open index.html?scantest).</summary>
		public static void BuildWebGLScanTest() => BuildWebGLInternal(BuildOptions.Development, new[] { "SPOOKY_AUTOPILOT" }, "Builds/WebGLScanTest");

		private static void BuildWebGLInternal(BuildOptions options, string[]? defines, string path)
		{
			PlayerSettings.WebGL.template = "PROJECT:Spooky";
			PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
			PlayerSettings.WebGL.decompressionFallback = true;
			PlayerSettings.WebGL.dataCaching = true;
			PlayerSettings.WebGL.nameFilesAsHashes = false;
			if (Build(BuildTarget.WebGL, path, options, defines))
				File.WriteAllText(Path.Combine(path, "_headers"), NetlifyHeaders);
		}

		/// <summary>
		/// Netlify: serve Unity's gzip files with the right encoding/types so browsers decompress natively, and make
		/// browsers re-check everything on each visit (a new deploy is picked up right away, unchanged files come from cache).
		/// </summary>
		private const string NetlifyHeaders =
@"# Netlify: serve Unity's gzip files with the right encoding/types so browsers decompress natively (faster loading).
/*
  Cache-Control: public, max-age=0, must-revalidate
/Build/*.data.unityweb
  Content-Encoding: gzip
  Content-Type: application/octet-stream
/Build/*.framework.js.unityweb
  Content-Encoding: gzip
  Content-Type: application/javascript
/Build/*.wasm.unityweb
  Content-Encoding: gzip
  Content-Type: application/wasm
";

		public static void BuildMacAutopilot()
		{
			Build(BuildTarget.StandaloneOSX, "Builds/Mac/SpookyAutopilot.app", BuildOptions.Development, new[] { "SPOOKY_AUTOPILOT" });
		}

		private static bool Build(BuildTarget target, string path, BuildOptions options, string[]? defines)
		{
			SpookyProjectBuilder.ConfigurePlayer();
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
			{
				scenes = new[] { SpookyProjectBuilder.ScenePath },
				locationPathName = path,
				target = target,
				options = options,
				extraScriptingDefines = defines
			});

			var summary = report.summary;
			Debug.Log($"[SpookyDevBuilds] {target}: {summary.result}, {summary.totalErrors} errors, {summary.totalSize / (1024 * 1024)} MB, {summary.totalTime}");
			if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
				EditorApplication.Exit(1);
			return summary.result == BuildResult.Succeeded;
		}
	}
}
