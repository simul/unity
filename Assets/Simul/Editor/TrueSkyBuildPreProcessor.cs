using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using simul;

namespace simul
{
	class TrueSkyBuildPreProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder { get { return 0; } }
		public void OnPreprocessBuild(BuildReport report)
		{
			OnPreprocessBuild(report.summary.platform, report.summary.outputPath);
		}

		public void OnPreprocessBuild(BuildTarget target, string pathToBuiltProject)
		{
			char s = Path.DirectorySeparatorChar;
			//string buildDirectory = pathToBuiltProject.Replace(".exe", "_Data");
			// If building for ps4 also copy to StreamingAssets folder
			if (target == BuildTarget.PS4)
			{
				string shaderbinSource = trueSKY.GetShaderbinSourceDir("ps4");
				string assetsPath = Environment.CurrentDirectory + s + "Assets";
				string simul = assetsPath + s + "Simul";
				string MediaSource = simul + s + "Media";
				string saDir = Application.streamingAssetsPath + s + "Simul" + s + "shaderbin" + s + "ps4";
				DirectoryCopy.Copy(shaderbinSource, saDir, true, true, false, false);
				Debug.Log("DirectoryCopy: " + shaderbinSource + "->" + saDir);
				saDir = Application.streamingAssetsPath + s + "Simul" + s + "Media";
				DirectoryCopy.Copy(MediaSource, saDir, true, true, false, false);
				Debug.Log("DirectoryCopy: " + MediaSource + "->" + saDir);
			}
#if UNITY_PS5
			if (target == BuildTarget.PS5)
			{
				string shaderbinSource = trueSKY.GetShaderbinSourceDir("ps5");
				string assetsPath = Environment.CurrentDirectory + s + "Assets";
				string simul = assetsPath + s + "Simul";
				string MediaSource = simul + s + "Media";
				string saDir = Application.streamingAssetsPath + s + "Simul" + s + "shaderbin" + s + "ps5";
				DirectoryCopy.Copy(shaderbinSource, saDir, true, true, false, false);
				Debug.Log("DirectoryCopy: " + shaderbinSource + "->" + saDir);
				saDir = Application.streamingAssetsPath + s + "Simul" + s + "Media";
				DirectoryCopy.Copy(MediaSource, saDir, true, true, false, false);
				Debug.Log("DirectoryCopy: " + MediaSource + "->" + saDir);
			}
#endif
#if UNITY_GAMECORE
			if(target == BuildTarget.GameCoreScarlett)
			{
				string shaderbinSource = Environment.CurrentDirectory + s + "Assets/Plugins/GameCoreXboxSeries/shaderbin";
				string assetsPath = Environment.CurrentDirectory + s + "Assets";
				string simulPath = assetsPath + s + "Simul";
				string mediaPath = simulPath + s + "Media";

				string stagingAreaPath;
				string fixedPath = pathToBuiltProject;
				int lastSep = fixedPath.LastIndexOf("/");
				fixedPath = fixedPath.Remove(lastSep);
				stagingAreaPath = fixedPath + "/Temp/StagingArea";

				DirectoryCopy.Copy(shaderbinSource, stagingAreaPath, true, true, false, false);
				Debug.Log("DirectoryCopy: " + shaderbinSource + "->" + stagingAreaPath);
				DirectoryCopy.Copy(mediaPath, stagingAreaPath, true, true, false, false);
				Debug.Log("DirectoryCopy: " + mediaPath + "->" + stagingAreaPath);
			}
			if (target == BuildTarget.GameCoreXboxOne)
			{
				string shaderbinSource = Environment.CurrentDirectory + s + "Assets/Plugins/GameCoreXboxOne/shaderbin";
				string assetsPath = Environment.CurrentDirectory + s + "Assets";
				string simulPath = assetsPath + s + "Simul";
				string mediaPath = simulPath + s + "Media";

				string stagingAreaPath;
				string fixedPath = pathToBuiltProject;
				int lastSep = fixedPath.LastIndexOf("/");
				fixedPath = fixedPath.Remove(lastSep);
				stagingAreaPath = fixedPath + "/Temp/StagingArea";

				DirectoryCopy.Copy(shaderbinSource, stagingAreaPath, true, true, false, false);
				Debug.Log("DirectoryCopy: " + shaderbinSource + "->" + stagingAreaPath);
				DirectoryCopy.Copy(mediaPath, stagingAreaPath, true, true, false, false);
				Debug.Log("DirectoryCopy: " + mediaPath + "->" + stagingAreaPath);
			}
#endif
		}
	}
}
