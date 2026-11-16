using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _Main.Scripts.Services.Packs.Dto;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace _Main.Scripts.Services.Packs
{
	/// <summary>
	/// The phone's copy of packs downloaded from the website, in persistentDataPath/Packs: one folder per pack
	/// (its manifest.json, pack.json and media) plus the website's last index.json. A pack is downloaded into a
	/// staging folder, every file is checked against its hash, and only then does it replace the old copy, so a
	/// dropped connection never leaves a broken pack behind. Unchanged files are copied from the old copy instead
	/// of downloaded again.
	/// </summary>
	internal sealed class PackCache
	{
		private const string ManifestFile = PackLibrary.ManifestFile;
		private const string StagingFolder = ".staging";
		private const int ParallelDownloads = 4;
		private const int FileTimeoutSeconds = 60;

		private readonly string _root;

		public PackCache()
		{
			_root = Path.Combine(Application.persistentDataPath, PackLibrary.FolderName);
		}

		public string FolderUrl(string packId) => new Uri(Path.Combine(_root, packId) + Path.DirectorySeparatorChar).AbsoluteUri;

		public PackIndexDto? ReadIndex()
		{
			var path = Path.Combine(_root, PackLibrary.IndexFile);
			return File.Exists(path) ? PackWeb.Parse<PackIndexDto>(SafeRead(path), "cached " + PackLibrary.IndexFile) : null;
		}

		public void WriteIndex(string json)
		{
			try
			{
				Directory.CreateDirectory(_root);
				File.WriteAllText(Path.Combine(_root, PackLibrary.IndexFile), json);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[{nameof(PackCache)}] Could not save the index: {e.Message}");
			}
		}

		/// <summary>Every complete cached pack, by id.</summary>
		public Dictionary<string, PackManifestDto> ReadPacks()
		{
			var packs = new Dictionary<string, PackManifestDto>(StringComparer.OrdinalIgnoreCase);
			if (!Directory.Exists(_root))
				return packs;

			foreach (var folder in Directory.GetDirectories(_root))
			{
				var id = Path.GetFileName(folder);
				if (id.StartsWith("."))
					continue;

				var manifestPath = Path.Combine(folder, ManifestFile);
				if (!File.Exists(manifestPath) || !File.Exists(Path.Combine(folder, PackLibrary.PackFile)))
					continue;

				var manifest = PackWeb.Parse<PackManifestDto>(SafeRead(manifestPath), $"cached {id}/{ManifestFile}");
				if (manifest != null && string.Equals(manifest.Id, id, StringComparison.OrdinalIgnoreCase))
					packs[id] = manifest;
			}

			return packs;
		}

		public void Delete(string packId)
		{
			try
			{
				var folder = Path.Combine(_root, packId);
				if (Directory.Exists(folder))
					Directory.Delete(folder, true);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[{nameof(PackCache)}] Could not delete {packId}: {e.Message}");
			}
		}

		/// <summary>Downloads one pack from <paramref name="remoteRoot"/> (the website's Packs folder) into the cache.</summary>
		public IEnumerator Download(string remoteRoot, PackIndexEntryDto entry, Action<bool> done)
		{
			string id = entry.Id!;
			string remoteFolder = remoteRoot + Uri.EscapeDataString(id) + "/";

			string? manifestJson = null;
			yield return PackWeb.GetText($"{remoteFolder}{ManifestFile}?v={entry.Hash}", 20, text => manifestJson = text);
			var manifest = PackWeb.Parse<PackManifestDto>(manifestJson, $"{id}/{ManifestFile}");
			if (manifest?.Files == null || manifest.Files.Count == 0 || manifest.Hash != entry.Hash)
			{
				// A different hash means the website is halfway through a deploy: try again next time.
				Debug.LogWarning($"[{nameof(PackCache)}] {id}: the online manifest doesn't match its index entry; skipping for now.");
				done(false);
				yield break;
			}

			if (manifest.Files.Any(f => !IsSafePath(f.Path)) || manifest.Files.All(f => f.Path != PackLibrary.PackFile))
			{
				Debug.LogError($"[{nameof(PackCache)}] {id}: the online manifest lists unexpected files; skipping.");
				done(false);
				yield break;
			}

			string target = Path.Combine(_root, id);
			string staging = Path.Combine(_root, StagingFolder, id);
			Dictionary<string, string> previous = new(StringComparer.Ordinal);
			var oldManifest = File.Exists(Path.Combine(target, ManifestFile)) ? PackWeb.Parse<PackManifestDto>(SafeRead(Path.Combine(target, ManifestFile)), ManifestFile) : null;
			foreach (var file in oldManifest?.Files ?? new List<PackFileDto>())
			{
				if (file.Path != null && file.Hash != null)
					previous[file.Path] = file.Hash;
			}

			bool ok = true;
			try
			{
				if (Directory.Exists(staging))
					Directory.Delete(staging, true);
				Directory.CreateDirectory(staging);
			}
			catch (Exception e)
			{
				Debug.LogError($"[{nameof(PackCache)}] {id}: no room to download ({e.Message}).");
				done(false);
				yield break;
			}

			var queue = PlanDownloads(manifest, staging, target, previous);
			if (queue == null)
			{
				done(false);
				yield break;
			}

			var running = new List<(UnityWebRequest Request, PackFileDto File, string Path)>();
			while (ok && (queue.Count > 0 || running.Count > 0))
			{
				while (queue.Count > 0 && running.Count < ParallelDownloads)
				{
					var file = queue.Dequeue();
					string destination = Path.Combine(staging, file.Path!);
					string url = remoteFolder + string.Join("/", file.Path!.Split('/').Select(Uri.EscapeDataString)) + "?v=" + file.Hash;
					var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET, new DownloadHandlerFile(destination) { removeFileOnAbort = true }, null)
					{
						timeout = FileTimeoutSeconds
					};
					request.SendWebRequest();
					running.Add((request, file, destination));
				}

				yield return null;

				for (int i = running.Count - 1; i >= 0; i--)
				{
					var (request, file, path) = running[i];
					if (!request.isDone)
						continue;

					running.RemoveAt(i);
					if (request.result != UnityWebRequest.Result.Success)
					{
						Debug.LogWarning($"[{nameof(PackCache)}] {id}: could not download {file.Path}: {request.error}");
						ok = false;
					}
					else if (!Verify(path, file))
					{
						Debug.LogWarning($"[{nameof(PackCache)}] {id}: {file.Path} arrived damaged.");
						ok = false;
					}

					request.Dispose();
				}
			}

			foreach (var (request, _, _) in running)
			{
				request.Abort();
				request.Dispose();
			}

			if (ok)
			{
				try
				{
					File.WriteAllText(Path.Combine(staging, ManifestFile), manifestJson);
					if (Directory.Exists(target))
						Directory.Delete(target, true);
					Directory.Move(staging, target);
					Debug.Log($"[{nameof(PackCache)}] {id}: downloaded ({manifest.Files.Sum(f => f.Size) / 1024} KB).");
				}
				catch (Exception e)
				{
					Debug.LogError($"[{nameof(PackCache)}] {id}: could not save the download: {e.Message}");
					ok = false;
				}
			}

			if (!ok)
			{
				try
				{
					if (Directory.Exists(staging))
						Directory.Delete(staging, true);
				}
				catch (Exception)
				{
					// Leftovers are wiped by the next attempt.
				}
			}

			done(ok);
		}

		/// <summary>Copies the files that didn't change from the old copy; returns the ones still to download (null on a disk error).</summary>
		private static Queue<PackFileDto>? PlanDownloads(PackManifestDto manifest, string staging, string target, Dictionary<string, string> previous)
		{
			var queue = new Queue<PackFileDto>();
			try
			{
				foreach (var file in manifest.Files!)
				{
					string destination = Path.Combine(staging, file.Path!);
					Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
					string existing = Path.Combine(target, file.Path!);
					if (previous.TryGetValue(file.Path!, out var oldHash) && oldHash == file.Hash && File.Exists(existing))
					{
						File.Copy(existing, destination, true);
						if (Verify(destination, file))
							continue;
					}

					queue.Enqueue(file);
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"[{nameof(PackCache)}] {manifest.Id}: could not prepare the download: {e.Message}");
				return null;
			}

			return queue;
		}

		private static bool Verify(string path, PackFileDto file)
		{
			try
			{
				return File.Exists(path) && PackHashes.OfBytes(File.ReadAllBytes(path)) == file.Hash;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static bool IsSafePath(string? path)
			=> !string.IsNullOrWhiteSpace(path) && !path!.Contains("..") && !path.Contains("\\") && !path.StartsWith("/") && !path.Contains(":")
				&& path != ManifestFile;

		private static string? SafeRead(string path)
		{
			try
			{
				return File.ReadAllText(path);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[{nameof(PackCache)}] Could not read {path}: {e.Message}");
				return null;
			}
		}
	}

	/// <summary>Small shared helpers for reading pack files over UnityWebRequest.</summary>
	internal static class PackWeb
	{
		public static IEnumerator GetText(string url, int timeoutSeconds, Action<string?> done, bool logErrors = true)
		{
			using var request = UnityWebRequest.Get(url);
			if (timeoutSeconds > 0)
				request.timeout = timeoutSeconds;
			yield return request.SendWebRequest();
			if (request.result == UnityWebRequest.Result.Success)
			{
				done(request.downloadHandler.text);
			}
			else
			{
				if (logErrors)
					Debug.LogWarning($"[{nameof(PackLibrary)}] Could not read {url}: {request.error}");
				done(null);
			}
		}

		public static T? Parse<T>(string? json, string what) where T : class
		{
			if (string.IsNullOrEmpty(json))
				return null;

			try
			{
				return JsonConvert.DeserializeObject<T>(json!);
			}
			catch (Exception e)
			{
				Debug.LogError($"[{nameof(PackLibrary)}] {what} is not valid JSON: {e.Message}");
				return null;
			}
		}
	}
}
