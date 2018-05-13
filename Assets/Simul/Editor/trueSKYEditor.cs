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
		bool celestial = false;
		bool clouds = false;
		bool atmospherics = false;
		bool noise = false;
		bool lighting = false;
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
					if (trueSky.SimulVersion<trueSky.MakeSimulVersion(4, 2))
					{
						trueSky.CloudThresholdDistanceKm = EditorGUILayout.Slider("Threshold Distance (km)", trueSky.CloudThresholdDistanceKm, 0.0F, 10.0F);
						trueSky.DepthBlending = EditorGUILayout.Toggle("Depth Blending", trueSky.DepthBlending);
						trueSky.MinimumStarPixelSize = EditorGUILayout.FloatField("Minimum Star Pixel Size", trueSky.MinimumStarPixelSize);
					}
					EditorGUILayout.Space();
				}
				// Rendering settings
				EditorGUILayout.LabelField("TrueSkyRendering", EditorStyles.boldLabel);
				if (trueSky.SimulVersion >= trueSky.MakeSimulVersion(4, 2))
				{
					clouds = EditorGUILayout.BeginToggleGroup("clouds", clouds);
					if (clouds)
					{
						trueSky.IntegrationScheme = EditorGUILayout.IntSlider("Integration Scheme", trueSky.IntegrationScheme, 0, 1);
						trueSky.CubemapResolution = EditorGUILayout.IntSlider("Cubemap Resolution", trueSky.CubemapResolution, 16, 2048);
						trueSky.MaxCloudDistanceKm = EditorGUILayout.Slider("Max Cloud Distance (km)", trueSky.MaxCloudDistanceKm, 100.0F, 1000.0F);
						trueSky.RenderGridXKm = EditorGUILayout.Slider("Render Grid X (km)", trueSky.RenderGridXKm, 0.01F, 10.0F);
						trueSky.RenderGridZKm = EditorGUILayout.Slider("Render Grid Z (km)", trueSky.RenderGridZKm, 0.01F, 10.0F);
						trueSky.CloudSteps = EditorGUILayout.IntSlider("Cloud Steps", trueSky.CloudSteps, 60, 250);
						trueSky.Amortization = EditorGUILayout.IntSlider("Amortization", trueSky.Amortization, 1, 4);
						trueSky.CloudThresholdDistanceKm = EditorGUILayout.Slider("Threshold Distance (km)", trueSky.CloudThresholdDistanceKm, 0.0F, 10.0F);
						trueSky.DepthSamplingPixelRange = EditorGUILayout.Slider("Depth Sampling Range", trueSky.DepthSamplingPixelRange, 0.0f, 4.0f);
						trueSky.DepthBlending = EditorGUILayout.Toggle("Depth Blending", trueSky.DepthBlending);
					}
					EditorGUILayout.EndToggleGroup();
					atmospherics = EditorGUILayout.BeginToggleGroup("atmospherics", atmospherics);
					if (atmospherics)
					{
						trueSky.AtmosphericsAmortization = EditorGUILayout.IntSlider("Atmospherics Amortization", trueSky.AtmosphericsAmortization, 1, 4);
						trueSky.GodRaysGrid = EditorGUILayout.Vector3Field("God Rays Grid", trueSky.GodRaysGrid);
						trueSky.CrepuscularRaysStrength = EditorGUILayout.Slider("Crepuscular Rays Strength", trueSky.CrepuscularRaysStrength, 0.0F, 1.0F);
					}
					EditorGUILayout.EndToggleGroup();

					lighting = EditorGUILayout.BeginToggleGroup("lighting", lighting);
					if (lighting)
					{
						trueSky.DirectLight = EditorGUILayout.Slider("Direct Light", trueSky.DirectLight, 0.0F, 4.0F);
						trueSky.IndirectLight = EditorGUILayout.Slider("Indirect Light", trueSky.IndirectLight, 0.0F, 4.0F);
						trueSky.AmbientLight = EditorGUILayout.Slider("Ambient Light", trueSky.AmbientLight, 0.0F, 4.0F);
						trueSky.Extinction = EditorGUILayout.Slider("Extinction (per km)", trueSky.Extinction, 0.0F, 12.0F);
						trueSky.MieAsymmetry = EditorGUILayout.Slider("Mie Asymmetry", trueSky.MieAsymmetry, 0.0F, 0.999F);
					}
					EditorGUILayout.EndToggleGroup();

					celestial = EditorGUILayout.BeginToggleGroup("Celestial", celestial);
					if (celestial)
					{
						trueSky.MaxSunRadiance = EditorGUILayout.FloatField("Max Sun Radiance", trueSky.MaxSunRadiance);
						trueSky.AdjustSunRadius = EditorGUILayout.Toggle("Adjust Sun Radius", trueSky.AdjustSunRadius);
						trueSky.backgroundTexture = (Texture)EditorGUILayout.ObjectField("Cosmic Background", trueSky.backgroundTexture, typeof(Texture), false);
						trueSky.moonTexture = (Texture)EditorGUILayout.ObjectField("Moon Texture", trueSky.moonTexture, typeof(Texture), false);
						trueSky.MinimumStarPixelSize = EditorGUILayout.FloatField("Minimum Star Pixel Size", trueSky.MinimumStarPixelSize);
					}
					EditorGUILayout.EndToggleGroup();
					EditorGUILayout.Space();

					// Noise settings
					noise = EditorGUILayout.BeginToggleGroup("Noise", noise);
					if (noise)
					{
						// Edge
						EditorGUILayout.LabelField("Edge Noise Settings", EditorStyles.boldLabel);
						trueSky.EdgeNoisePersistence = EditorGUILayout.Slider("Edge Noise Persistence", trueSky.EdgeNoisePersistence, 0.0f, 10.0f);
						trueSky.EdgeNoiseFrequency = EditorGUILayout.IntSlider("Edge Noise Frequency", trueSky.EdgeNoiseFrequency, 1, 16);
						trueSky.EdgeNoiseTextureSize = EditorGUILayout.IntSlider("Edge Noise Texture Size", trueSky.EdgeNoiseTextureSize, 32, 256);
						trueSky.EdgeNoiseWavelengthKm = EditorGUILayout.Slider("Edge Noise Wavelength Km", trueSky.EdgeNoiseWavelengthKm, 0.01f, 50.0f);
						trueSky.MaxFractalAmplitudeKm = EditorGUILayout.Slider("Edge Noise Wavelength Km", trueSky.MaxFractalAmplitudeKm, 0.0f, 50.0f);
						trueSky.CellNoiseTextureSize = EditorGUILayout.IntSlider("Cell Noise Texture Size", trueSky.CellNoiseTextureSize, 32, 256);
						trueSky.CellNoiseWavelengthKm = EditorGUILayout.Slider("Cell Noise Wavelength Km", trueSky.CellNoiseWavelengthKm, 0.01f, 50.0f);
					}
					EditorGUILayout.Space();

					// Cloud
					{
						EditorGUILayout.LabelField("Cloud Noise Settings", EditorStyles.boldLabel);
						trueSky.WorleyWavelengthKm = EditorGUILayout.Slider("Worley Wavelength Km", trueSky.WorleyWavelengthKm, 0.0f, 50.0f);
						trueSky.WorleyTextureSize = EditorGUILayout.IntSlider("Worley Texture Size", trueSky.WorleyTextureSize, 8, 512);
						EditorGUILayout.Space();
					}
					EditorGUILayout.EndToggleGroup();
				}
					// Sound settings
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
			string filenameRoot = "trueSKYPlugin-Unity2017-" + version;
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
        Application.stackTraceLogType = StackTraceLogType.None;
		string f = CommandLineReader.GetCustomArgument("Filename");
		f = f.Replace("\"", "");
		UnityEngine.Debug.Log("ExportPackageCmdLine "+f);
		ExportPackage(f);
	}
	static void ExportPackage(string fileName)
	{
		UnityEngine.Debug.Log("C:/trueSKY.unitypackage =" + fileName+"? "+("C:/trueSKY.unitypackage" == fileName));
		AssetDatabase.ExportPackage("Assets/Simul", fileName, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

		UnityEngine.Debug.Log("Exported: "+fileName);
	}
#endif
	}
}