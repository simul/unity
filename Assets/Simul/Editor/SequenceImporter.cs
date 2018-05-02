using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace simul
{
	public class SequenceImporter : AssetPostprocessor
	{
		static string extension = ".sq";            // Our sequence text file extension
		static string newExtension = ".asset";     // Extension of newly created asset - it MUST be ".asset", nothing else is allowed...

		public static bool HasExtension(string asset)
		{
			return asset.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase);
		}

		public static string ConvertToInternalPath(string asset)
		{
			string left = asset.Substring(0, asset.Length - extension.Length);
			return left + newExtension;
		}

		// This is called always when importing something
		static void OnPostprocessAllAssets
		   (
			 string[] importedAssets,
			 string[] deletedAssets,
			 string[] movedAssets,
			 string[] movedFromAssetPaths
		   )
		{
			foreach (string asset in importedAssets)
			{
				// This is our detection of file - by extension
				if (HasExtension(asset))
				{
					ImportMyAsset(asset);
				}
			}
		}

		// Imports my asset from the file
		public static void ImportMyAsset(string asset)
		{
			if (!HasExtension(asset))
			{
				Debug.LogError("Cannot import \"" + asset + "\" as a sequence - need .sq extension.");
				return;
			}
			{
				TextReader r = new StreamReader(asset);
				string firstLine = r.ReadLine();
				r.Close();
				// Also we check first file line
				if (!firstLine.Equals("{", StringComparison.OrdinalIgnoreCase))
				{
					Debug.LogError("Cannot import sequence \"" + asset + "\": bad format!");
					return;
				}
			}
			//Debug.Log("Import Asset \""+asset+"\".");
			// Path to out new asset
			string newPath = ConvertToInternalPath(asset);

			// Sequence is imported asset type, it should derive from ScriptableObject, probably
			Sequence sq = AssetDatabase.LoadAssetAtPath(newPath, typeof(Sequence)) as Sequence;
			bool loaded = (sq != null);

			if (!loaded)
			{
				sq = ScriptableObject.CreateInstance<Sequence>();
			}
			else
			{
				// return; // Uncommenting here means that when the original file is changed, changes are ignored
			}
			// We read the text in from the file. This should never be done in-game, it's just to import into the serializable
			// ".Asset" that Unity will package-up.
			string allines = "";// StringBuilder sb = new StringBuilder();
			TextReader stream = new StreamReader(asset);
			string line;
			// Read and display lines from the file until the end of 
			// the file is reached.
			while ((line = stream.ReadLine()) != null)
			{
				//            sb.AppendLine(line);
				allines += line + "\n";
			}
			stream.Close();
			sq.Load(allines);

			//UnityEngine.Debug.Log(newPath);
			if (!loaded)
			{
				AssetDatabase.CreateAsset(sq, newPath);
			}
			EditorUtility.SetDirty(sq);
			AssetDatabase.SaveAssets();
		}
	}
}