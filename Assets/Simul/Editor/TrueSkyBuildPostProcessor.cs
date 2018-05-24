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
		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
		{
            // Check supported targets
			if (target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64 && 
                target != BuildTarget.PS4               && target != BuildTarget.WSAPlayer          && 
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

            Debug.Log("Build directory is: " + buildDirectory);

            // Copy shaders
			string assetsPath = Environment.CurrentDirectory + s + "Assets";
			string shaderbinSource = trueSKY.GetShaderbinSourceDir(targetstr);
			string shaderbinBuild = buildDirectory + s + "Simul" + s + "shaderbin";
			DirectoryCopy.Copy(shaderbinSource, shaderbinBuild, true, true);
			Debug.Log("DirectoryCopy: " + shaderbinBuild + "->" + shaderbinBuild);

			string simul = assetsPath + s + "Simul";
			// Copy media
			string MediaSource = simul + s + "Media";
			string MediaBuild = buildDirectory + s + "Simul" + s + "Media";
			DirectoryCopy.Copy(MediaSource, MediaBuild, true, false, false, false);
            Debug.Log("DirectoryCopy: " + MediaSource + "->" + MediaBuild);

        }
	}
}