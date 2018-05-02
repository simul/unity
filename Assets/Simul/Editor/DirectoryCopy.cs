using System;
using System.IO;
using UnityEngine;

namespace simul
	{
	public class DirectoryCopy
	{
		public static void CopyPluginsAndGizmosToAssetsFolder()
		{
			char s = Path.DirectorySeparatorChar;
			String assetsPath = Environment.CurrentDirectory + s + "Assets";
			// 1. The gizmos
			String gizmosPath = assetsPath + s + "Gizmos";
			DirectoryCopy.Copy(assetsPath + s + "Simul" + s + "Gizmos", gizmosPath, true, true, true,false,true);
			// 2. The plugins folder
			String pluginsPath = assetsPath + s + "Plugins";
			DirectoryCopy.Copy(assetsPath + s + "Simul" + s + "Plugins", pluginsPath, true, true, true,false,true);
		}
		public static void Copy(string sourceDirName, string destDirName, bool copySubDirs, bool skipMetas, bool newerOnly = false, bool report = false,bool add_underscore=false)
		{
			if (!Directory.Exists(destDirName))
				Directory.CreateDirectory(destDirName);
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);
			DirectoryInfo[] dirs = dir.GetDirectories();

			if (report)
				UnityEngine.Debug.Log("Copy: " + sourceDirName + " to " + destDirName);
			if (!dir.Exists)
			{
				UnityEngine.Debug.LogError("Source directory does not exist or could not be found: " + sourceDirName);
				return;
			}
			DirectoryInfo ddir = new DirectoryInfo(destDirName);
			if (!ddir.Exists)
			{
				UnityEngine.Debug.LogError("Dest directory does not exist or could not be found: " + destDirName);
				return;
			}
			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				if (skipMetas)
				{
					if (file.Name.Contains(".meta"))
						continue;
					if (file.Name.Contains(".pdb"))
						continue;
					if (file.Name.Contains(".fx"))
						continue;
					if (file.Name.Contains(".sl"))
						continue;
					if (file.Name.Contains(".hlsl"))
						continue;
					if (file.Name.Contains(".hs"))
						continue;
				}
				string targetPath = Path.Combine(destDirName, file.Name);
				if(add_underscore)
				{
					if(targetPath.LastIndexOf("_")==targetPath.Length-1)
						targetPath=targetPath.Substring(0,targetPath.Length-1);
					else
						targetPath=targetPath+"_";
				}
				{
					FileInfo targetFile = new FileInfo(targetPath);
					if(file.LastWriteTime < targetFile.LastWriteTime)
					{
						if (newerOnly)
							continue;
						else
							UnityEngine.Debug.LogWarning("Warning: copying older "+file.Name+" over newer.");
					}
				}
				try
				{
					file.CopyTo(targetPath, true);
				}
				catch (Exception )
				{
					// ignore failure to copy - we might already be using the file, in which case we don't need to re-copy.
				}
				if (report)
					UnityEngine.Debug.Log("    " + file.Name + " to " + targetPath);
			}

			// If copying subdirectories, copy them and their contents to new location. 
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					Copy(subdir.FullName, temppath, copySubDirs, skipMetas, newerOnly, report,add_underscore);
				}
			}
		}
	}
}