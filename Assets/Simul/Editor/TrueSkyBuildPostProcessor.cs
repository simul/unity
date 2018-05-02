using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System;

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

            // Per-platform changes
			if(target == BuildTarget.PS4)
			{
                buildDirectory += s + "Media" + s;
			}
            if(target == BuildTarget.WSAPlayer)
            {
                buildDirectory += s + Application.productName + s + "Data";
            }
            if(target == BuildTarget.Switch)
            {
                string fixedPath    = pathToBuiltProject;
                int lastSep         = fixedPath.LastIndexOf("/");
                fixedPath           = fixedPath.Remove(lastSep);
                buildDirectory      = fixedPath + "/StagingArea/Data";
            }

            Debug.Log("Build directory is: " + buildDirectory);

			
            // Copy shaders
			string assetsPath = Environment.CurrentDirectory + s + "Assets";
			string simul = assetsPath + s + "Simul";
            // Custom shader binary folder
            string shaderFolderSrt;
            if (target == BuildTarget.PS4)
            {
                shaderFolderSrt = "shaderbinps4";
            }
            else if (target == BuildTarget.Switch)
            {
                shaderFolderSrt = "shaderbinnx";
            }
            else
            {
                shaderFolderSrt = "shaderbin";
            }
            string shaderbinSource = simul + s + shaderFolderSrt;
			string shaderbinBuild = buildDirectory + s + "Simul" + s + "shaderbin";
			DirectoryCopy.Copy(shaderbinSource, shaderbinBuild, true, true);
			Debug.Log("DirectoryCopy: " + shaderbinBuild + "->" + shaderbinBuild);

            // Copy media
			string MediaSource = simul + s + "Media";
			string MediaBuild = buildDirectory + s + "Simul" + s + "Media";
			DirectoryCopy.Copy(MediaSource, MediaBuild, true, false, false, false);
            Debug.Log("DirectoryCopy: " + MediaSource + "->" + MediaBuild);

            // If building for ps4 also copy to StreamingAssets folder
            if (target == BuildTarget.PS4)
            {
                string saDir = buildDirectory + s + "StreamingAssets" + s + "Simul" + s + "shaderbin";
                DirectoryCopy.Copy(shaderbinSource, saDir, true, true, false, false);
                Debug.Log("DirectoryCopy: " + shaderbinSource + "->" + saDir);
                saDir = buildDirectory + s + "StreamingAssets" + s + "Simul" + s + "Media";
                DirectoryCopy.Copy(MediaSource, saDir, true, true, false, false);
                Debug.Log("DirectoryCopy: " + MediaSource + "->" + saDir);
            }
        }
	}
}