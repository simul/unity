using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System;
using simul;

namespace simul
{
	public class TrueSkyBuildPostprocessor
	{
		static string ToPlatformName(BuildTarget target)
		{
			switch(target)
			{
				case BuildTarget.PS4:
					return "ps4";
				case BuildTarget.PS5:
					return "ps5";
				case BuildTarget.StandaloneWindows:
				case BuildTarget.StandaloneWindows64:
					return "x86_64";
				case BuildTarget.WSAPlayer:
					return "WSA";
			#if UNITY_GAMECORE
				case BuildTarget.GameCoreXboxSeries:
					return "XboxSeriesX";
				case BuildTarget.GameCoreXboxOne: 
			#endif 
				case BuildTarget.XboxOne:
					return "XboxOne";
				case BuildTarget.Switch:
					return "Switch";
				default:
					return "null";
			}
		}

		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
		{
			// Check supported targets
			if (target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64 && 
				target != BuildTarget.PS4               && target != BuildTarget.WSAPlayer           &&
#if UNITY_GAMECORE
				target != BuildTarget.GameCoreXboxSeries && target != BuildTarget.GameCoreXboxOne     &&
#endif
				target != BuildTarget.PS5 &&

				target != BuildTarget.Switch)
			{
				Debug.LogError("Trying to build for a non-supported platform! (" + target.ToString() + ")");
				return;
			}
			
			char s = Path.DirectorySeparatorChar;
			string buildDirectory = pathToBuiltProject.Replace(".exe", "_Data");
			string mediaDirectory = buildDirectory;
			 String targetstr = "x86_64";
			// Per-platform changes
			if(target == BuildTarget.PS4)
			{
				mediaDirectory += s + "Media" + s;
				targetstr = "ps4";
			}
			if (target == BuildTarget.PS5)
			{
				mediaDirectory += s + "Media" ;
				targetstr = "ps5";
			}
			if (target == BuildTarget.WSAPlayer)
			{
				mediaDirectory += s + Application.productName + s + "Data";
			}
			if(target == BuildTarget.Switch)
			{
				string nrsSourcePath = Environment.CurrentDirectory + s + "Assets"+s+"Simul"+s+"Plugins"+s+"Switch";
				string nrsTargetPath = mediaDirectory + s+ "program0.ncd"+s+"data" + s+"Plugins";
				Debug.Log("NRS from "+nrsSourcePath+" to "+nrsTargetPath);
				DirectoryCopy.Copy(nrsSourcePath, nrsTargetPath, true, true);
				//C:\Simul\Unity\2021\build_nx\2021.nspd\program0.ncd\data\Plugins
				return;
			}

			Debug.Log("Build directory is: " + mediaDirectory);

			// Copy shaders
			string assetsPath = Environment.CurrentDirectory + s + "Assets";
			string shaderbinSource = trueSKY.GetShaderbinSourceDir(targetstr);
			string shaderbinBuild = mediaDirectory + s + "Simul" + s + "shaderbin" + s + targetstr;
			DirectoryCopy.Copy(shaderbinSource, shaderbinBuild, true, true);
			Debug.Log("DirectoryCopy: " + shaderbinSource + "->" + shaderbinBuild);
			if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
			{
				shaderbinSource = trueSKY.GetShaderbinSourceDir("vulkan");
				shaderbinBuild = mediaDirectory + s + "Simul" + s + "shaderbin" + s + "vulkan";
				if (Directory.Exists(shaderbinSource))
				{
					DirectoryCopy.Copy(shaderbinSource, shaderbinBuild, true, true);
					Debug.Log("DirectoryCopy: " + shaderbinSource + "->" + shaderbinBuild);
				}
			}

			string simul = assetsPath + s + "Simul";
			// Copy media
			string MediaSource = simul + s + "Media";
			string MediaBuild = mediaDirectory + s + "Simul" + s + "Media";
			DirectoryCopy.Copy(MediaSource, MediaBuild, true, false, false, false);
			Debug.Log("DirectoryCopy: " + MediaSource + "->" + MediaBuild);

		}
	}
}