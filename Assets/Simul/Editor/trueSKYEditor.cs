using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
//Used for File IO
using System.IO;

namespace simul
{
	[CustomEditor(typeof(trueSKY))]
	public class trueSKYEditor : Editor
	{
		[MenuItem("Help/trueSKY Documentation...", false, 1000)]
		public static void ShowTrueSkyDocs()
		{
			string path = @"http://docs.simul.co/unity";// "Simul/Documentation/TrueSkyUnity.html";
			//
			Help.BrowseURL(path);
		}
		bool recomp = false;
		static bool showProfiling = false;
		Vector2 scroll;
		
		[MenuItem("Window/trueSky/Onscreen Profiling #&p", false, 200000)]
		public static void Profiling()
		{
			trueSKY.OnscreenProfiling=!trueSKY.OnscreenProfiling;
		}
		[MenuItem("Window/trueSky/Show Compositing #&o", false, 200000)]
		public static void ShowCompositing()
		{
			trueSKY.ShowCompositing=!trueSKY.ShowCompositing;
		}
		[MenuItem("Window/trueSky/Cycle Compositing View #&v", false, 200000)]
		public static void CycleCompositingView()
		{
			trueSKY.CycleCompositingView();
		}
		[MenuItem("Window/trueSky/ShowCloudCrossSections #&c", false, 200000)]
		public static void ShowCloudCrossSections()
		{
			trueSKY.ShowCloudCrossSections=!trueSKY.ShowCloudCrossSections;
		}
		[MenuItem("Window/trueSky/ShowAtmosphericTables &a", false, 200000)]
		public static void ShowAtmosphericTables()
		{
			trueSKY.ShowAtmosphericTables=!trueSKY.ShowAtmosphericTables;
		}
		[MenuItem("Window/trueSky/Show Celestial Display #&s", false, 200000)]
		public static void ShowCelestialDisplay()
		{
			trueSKY.ShowCelestialDisplay=!trueSKY.ShowCelestialDisplay;
		}
		[MenuItem("Window/trueSky/Show Rain Textures #&p", false, 200000)]
		public static void ShowRainTextures()
		{
			trueSKY.ShowRainTextures=!trueSKY.ShowRainTextures;
		}
		[MenuItem("Window/trueSky/Show Cubemaps #&m", false, 200000)]
		public static void ShowCubemaps()
		{
			trueSKY.ShowCubemaps=!trueSKY.ShowCubemaps;
		}
		
		[MenuItem("Window/trueSky/Recompile Shaders #&r", false, 200000)]
		public static void RecompileShaders()
		{
			trueSKY.RecompileShaders();
		}
        public override void OnInspectorGUI()
        {
            trueSKY trueSky = (trueSKY)target;

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            {
                // Interpolation settings
                EditorGUILayout.LabelField("Interpolation", EditorStyles.boldLabel);
                {
                    trueSky.time = EditorGUILayout.FloatField("Time", trueSky.time);
                    trueSky.speed = EditorGUILayout.FloatField("Speed", trueSky.speed);
                    EditorGUILayout.Space();
                }

                // Precipitation settings
                EditorGUILayout.LabelField("Precipitation",EditorStyles.boldLabel);
                {
                    trueSky.SimulationTimeRain = EditorGUILayout.Toggle("Sim Time Rain", trueSky.SimulationTimeRain);
                    EditorGUILayout.Space();
                }

                // General truesky settings
                EditorGUILayout.LabelField("TrueSky", EditorStyles.boldLabel);
                {
                    trueSky.MetresPerUnit = EditorGUILayout.FloatField("Metres per Unit", trueSky.MetresPerUnit);
                    trueSky.RenderInEditMode = EditorGUILayout.Toggle("Render in Edit Mode", trueSky.RenderInEditMode);
                    trueSky.sequence = (Sequence)EditorGUILayout.ObjectField("Sequence Asset", trueSky.sequence, typeof(Sequence), false);
                    trueSky.CloudThresholdDistanceKm = EditorGUILayout.Slider("Threshold Distance (km)", trueSky.CloudThresholdDistanceKm, 0.0F, 10.0F);
                    trueSky.DepthBlending = EditorGUILayout.Toggle("Depth Blending", trueSky.DepthBlending);
                    trueSky.MinimumStarPixelSize = EditorGUILayout.FloatField("Minimum Start Pixel Size", trueSky.MinimumStarPixelSize);
                    EditorGUILayout.Space();
                }

                // Rendering settings
                EditorGUILayout.LabelField("TrueSkyRendering", EditorStyles.boldLabel);
                {
                    trueSky.CloudShadowing = EditorGUILayout.Slider("Cloud Shadowing", trueSky.CloudShadowing, 0.0F, 1.0F);
                    trueSky.CloudShadowSharpness = EditorGUILayout.Slider("Shadow Sharpness", trueSky.CloudShadowSharpness, 0.0F, 0.1F);
                    trueSky.Amortization = EditorGUILayout.IntSlider("Amortization", trueSky.Amortization, 1, 4);
                    trueSky.AtmosphericsAmortization = EditorGUILayout.IntSlider("Atmospherics Amortization", trueSky.AtmosphericsAmortization, 1, 4);
                    trueSky.CloudSteps = EditorGUILayout.IntSlider("Cloud Steps", trueSky.CloudSteps, 60, 250);
                    trueSky.CubemapResolution = EditorGUILayout.IntSlider("Cubemap Resolution", trueSky.CubemapResolution, 16, 2048);
                    trueSky.GodRaysGrid = EditorGUILayout.Vector3Field("God Rays Grid", trueSky.GodRaysGrid);
                    trueSky.DepthSamplingPixelRange = EditorGUILayout.Slider("Depth Sampling Range",trueSky.DepthSamplingPixelRange, 0.0f, 4.0f);
                    trueSky.MaxSunRadiance = EditorGUILayout.FloatField("Max Sun Radiance", trueSky.MaxSunRadiance);
                    EditorGUILayout.Space();
                }

                // Textures
                EditorGUILayout.LabelField("TrueSkyTextures", EditorStyles.boldLabel);
                {
                    trueSky.backgroundTexture = (Texture)EditorGUILayout.ObjectField("Cosmic Background", trueSky.backgroundTexture, typeof(Texture), false);
                    trueSky.moonTexture = (Texture)EditorGUILayout.ObjectField("Moon Texture", trueSky.moonTexture, typeof(Texture), false);
                    EditorGUILayout.Space();
                }

                // Sound settings
                /*
                EditorGUILayout.LabelField("Sound",EditorStyles.boldLabel);
                {

                    EditorGUILayout.Space();
                }
                */

                // Debugging options
                EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
                {
                    string gv = SystemInfo.graphicsDeviceVersion;
                    EditorGUILayout.LabelField("Unity Renderer", gv);
                    if (gv.Contains("Direct3D 11"))
                        EditorGUILayout.LabelField("GOOD", GUILayout.Width(48));
                    else
                        EditorGUILayout.LabelField("Unsupported", GUILayout.Width(48));
                    //	string cons=trueSky.GetRenderString("ConstellationNames");			
                    {
                        string f = EditorGUILayout.TextField("Find Constellation: ", "");
                        if (f.Length > 0)
                            trueSky.SetRenderString("HighlightConstellation", f);
                    }
                    string hcons = trueSky.GetRenderString("HighlightConstellationNames");
                    if (hcons.Length > 0)
                    {
                        EditorGUILayout.TextArea(hcons);
                    }
                    if (trueSKY.advancedMode)
                    {
                        if (GUILayout.Button("Recompile Shaders"))
                        {
                            recomp = true;
                        }
                    }
                    trueSky.maxGpuProfileLevel = EditorGUILayout.IntSlider("GPU Level", trueSky.maxGpuProfileLevel, 0, 8);
                    trueSky.maxCpuProfileLevel = EditorGUILayout.IntSlider("CPU Level", trueSky.maxCpuProfileLevel, 0, 8);
                    showProfiling = EditorGUILayout.BeginToggleGroup("Profiling", showProfiling);
                    {
                        if (showProfiling)
                        {
                            trueSky.SetBool("Profiling", true);
                            GUIStyle style = new GUIStyle();
                            style.richText = true;
                            string perf = "";
                            if (EditorGUIUtility.isProSkin)
                                perf = "<color=white>";
                            perf += trueSky.GetRenderString("Profiling");
                            if (EditorGUIUtility.isProSkin)
                                perf += "</color>";
                            scroll = EditorGUILayout.BeginScrollView(scroll);
                            EditorGUILayout.TextArea(perf, style, GUILayout.Height(1024));//
                            EditorGUILayout.EndScrollView();
                        }
                        else
                            trueSky.SetBool("Profiling", false);
                    }
                    EditorGUILayout.EndToggleGroup();
                    EditorGUILayout.Space();
                }
            }
            EditorGUILayout.EndVertical();
		

#if (UNITY_4_3 || UNITY_4_4)
#else
		EditorGUILayout.BeginHorizontal();
		if(trueSKY.advancedMode)
		if(GUILayout.Button("Export Package"))
		{
			string simul_dir = "C:/Simul/master/Simul";
			string dir=simul_dir+"/Products/TrueSky/Release/";
			string version = "3.50.0.";
			string version_file=simul_dir+"/version.txt";
			try
			{
				using (StreamReader sr = new StreamReader(version_file))
				{
					version = sr.ReadToEnd();
				}
			}
			catch (Exception e)
			{
				UnityEngine.Debug.Log(e);
				UnityEngine.Debug.Log("The version string file could not be read: "+version_file);
			}
			string filenameRoot = "trueSKYAlpha-Unity5-" + version;
			string[] aFilePaths=Directory.GetFiles(dir,filenameRoot+"*.unitypackage");
			int largest=1;
			foreach(string p in aFilePaths)
			{
				string pat = filenameRoot + @"(.*)\.unitypackage";
				  // Instantiate the regular expression object.
				Regex r = new Regex(pat, RegexOptions.IgnoreCase);
			  // Match the regular expression pattern against a text string.
				Match m=r.Match(p);
				while (m.Success)
				{
					{
						Group g = m.Groups[1];
						string numstr = g.ToString();
						int ct=Convert.ToInt32(numstr);
						if(ct>largest)
							largest = ct;
					}
					m = m.NextMatch();
				}
			}
			largest++;
			string fileName=dir+filenameRoot+largest.ToString("D4")+".unitypackage";
			//UnityEngine.Debug.Log(fileName);
			ExportPackage(fileName);
		}
        EditorGUILayout.EndHorizontal();
#endif
			if (GUI.changed)
			{
				EditorUtility.SetDirty(target);
				SceneView.RepaintAll();
			}
			if (recomp)
			{
				trueSKY.RecompileShaders();
				recomp = false;
			}
		}
#if (UNITY_4_3 || UNITY_4_4)
#else
	/// <summary>
	/// This command is run from the CI server to test that the just-installed trueSKY package is ok.
	/// </summary>
	static void Test()
	{
		UnityEngine.Debug.Log("Test installed trueSKY Package");
		EditorApplication.Exit(0);
	}
	static void ExportPackageCmdLine()
	{
		UnityEngine.Debug.Log("ExportPackageCmdLine");
		ExportPackage(CommandLineReader.GetCustomArgument("Filename"));
	}
	static void ExportPackage(string fileName)
	{
		UnityEngine.Debug.Log("trueSKYEditor.ExportPackage: "+fileName);
		List<string> paths=new List<string>();
		AddAssetPathToExport("Simul",paths);
		AssetDatabase.ExportPackage(paths.ToArray(),fileName,ExportPackageOptions.Default);
		UnityEngine.Debug.Log("Exported: "+fileName);
	}
	static void AddAssetPathToExport(string asset_path,List<string> paths)
	{
		UnityEngine.Debug.LogError("Application.dataPath: "+Application.dataPath);
		string full_path=Path.GetFullPath(Path.Combine(Application.dataPath,asset_path));
		UnityEngine.Debug.Log("Add Path: "+full_path);
		string[] aFilePaths=Directory.GetFiles(full_path,"*.*",SearchOption.AllDirectories);
 
		// enumerate through the list of files loading the assets they represent and getting their type
		foreach (string p in aFilePaths)
		{
			string asset_filename=p.Substring(Application.dataPath.Length-6);// remove asset path and slash.
			if(!asset_filename.Contains(".meta"))
				continue;
			asset_filename=asset_filename.Replace(".meta","");
			if(paths.Contains(asset_filename))
				continue;
			asset_filename=asset_filename.Replace("\\","/");
			//UnityEngine.Debug.Log("Add: "+asset_filename);
			paths.Add(asset_filename);
         }
	}
#endif
	}
}