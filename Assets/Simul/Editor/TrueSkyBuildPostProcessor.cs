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
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "x86_64";
                case BuildTarget.WSAPlayer:
                    return "WSA";
            #if UNITY_GAMECORE
                case BuildTarget.GameCoreScarlett:
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
                target != BuildTarget.GameCoreScarlett  && target != BuildTarget.GameCoreXboxOne     &&
            #endif
                target != BuildTarget.Switch)
			{
				Debug.LogError("Trying to build for a non-supported platform! (" + target.ToString() + ")");
				return;
			}
            
			char s = Path.DirectorySeparatorChar;
			string buildDirectory = pathToBuiltProject.Replace(".exe", "_Data");
			String targetstr = "x86_64";
            // Per-platform changes
			if(target == BuildTarget.PS4)
			{
                buildDirectory += s + "Media" + s;
				targetstr = "ps4";
			}
            if(target == BuildTarget.WSAPlayer)
            {
                buildDirectory += s + Application.productName + s + "Data";
            }
            if(target == BuildTarget.Switch)
            {
				targetstr = "nx";

				string fixedPath    = pathToBuiltProject;
                int lastSep         = fixedPath.LastIndexOf("/");
                fixedPath           = fixedPath.Remove(lastSep);
                buildDirectory      = fixedPath + "/StagingArea/Data";
            }
            #if UNITY_GAMECORE
            if (target == BuildTarget.GameCoreScarlett)
            {
                //buildDirectory += s + "Loose" + s + "Data" + s + "Plugins";
            }
            #endif

            Debug.Log("Build directory is: " + buildDirectory);

            // Copy shaders
			string assetsPath = Environment.CurrentDirectory + s + "Assets";
			string shaderbinSource = trueSKY.GetShaderbinSourceDir(targetstr);
			string shaderbinBuild = buildDirectory + s + "Simul" + s + "shaderbin" + s + targetstr;
			DirectoryCopy.Copy(shaderbinSource, shaderbinBuild, true, true);
			Debug.Log("DirectoryCopy: " + shaderbinSource + "->" + shaderbinBuild);
			if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
			{
				shaderbinSource = trueSKY.GetShaderbinSourceDir("vulkan");
				shaderbinBuild = buildDirectory + s + "Simul" + s + "shaderbin" + s + "vulkan";
				if (Directory.Exists(shaderbinSource))
				{
					DirectoryCopy.Copy(shaderbinSource, shaderbinBuild, true, true);
					Debug.Log("DirectoryCopy: " + shaderbinSource + "->" + shaderbinBuild);
				}
			}

			string simul = assetsPath + s + "Simul";
			// Copy media
			string MediaSource = simul + s + "Media";
			string MediaBuild = buildDirectory + s + "Simul" + s + "Media";
			DirectoryCopy.Copy(MediaSource, MediaBuild, true, false, false, false);
            Debug.Log("DirectoryCopy: " + MediaSource + "->" + MediaBuild);

        }
	}
}