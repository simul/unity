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
			if (target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64 && target != BuildTarget.PS4 && target != BuildTarget.WSAPlayer)
			{
				Debug.LogError("trueSKY Build Postprocessor: don't know this platform: " + target.ToString());
				return;
			}
            
			char s = Path.DirectorySeparatorChar;
			Debug.Log("trueSKY Build Postprocessor: pathToBuiltProject is: " + pathToBuiltProject);
			string buildDirectory = pathToBuiltProject.Replace(".exe", "_Data");// Path.GetDirectoryName(pathToBuiltProject);

            // Per-platform changes
			if(target== BuildTarget.PS4)
			{
				buildDirectory +=s+"Media" + s;
			}
            if(target == BuildTarget.WSAPlayer)
            {
                buildDirectory += s + Application.productName + s + "Data";
            }
			
            // Copy shaders
			string assetsPath = Environment.CurrentDirectory + s + "Assets";
			string simul = assetsPath + s + "Simul";
            // Custom shader binary folder
            string shaderFolderSrt = "shaderbin";
            if (target == BuildTarget.PS4) shaderFolderSrt = "shaderbinps4";
            string shaderbinSource = simul + s + shaderFolderSrt;

			string shaderbinBuild = buildDirectory + s + "Simul" + s + "shaderbin";
			DirectoryCopy.Copy(shaderbinSource, shaderbinBuild, true, false, false, false);
			Debug.Log("trueSKY Build Postprocessor: shader binaries to: " + shaderbinBuild);

            // Copy media
			string MediaSource = simul + s + "Media";
			string MediaBuild = buildDirectory + s + "Simul" + s + "Media";
			DirectoryCopy.Copy(MediaSource, MediaBuild, true, false, false, false);
			Debug.Log("trueSKY Build Postprocessor: Media to: " + MediaBuild);
		}
	}
}