using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
//Used for File IO
using System.IO;
using System.Collections;
using UnityEditor.Build.Reporting;

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
			trueSKY.OnscreenProfiling = !trueSKY.OnscreenProfiling;
		}
		[MenuItem("Window/trueSky/Show Compositing #&o", false, 200000)]
		public static void ShowCompositing()
		{
			trueSKY.ShowCompositing = !trueSKY.ShowCompositing;
		}
		[MenuItem("Window/trueSky/Cycle Compositing View #&v", false, 200000)]
		public static void CycleCompositingView()
		{
			trueSKY.CycleCompositingView();
		}
		[MenuItem("Window/trueSky/Show Cloud CrossSections #&c", false, 200000)]
		public static void ShowCloudCrossSections()
		{
			trueSKY.ShowCloudCrossSections = !trueSKY.ShowCloudCrossSections;
		}
		[MenuItem("Window/trueSky/Show Atmospheric Tables &a", false, 200000)]
		public static void ShowAtmosphericTables()
		{
			trueSKY.ShowAtmosphericTables = !trueSKY.ShowAtmosphericTables;
		}
		[MenuItem("Window/trueSky/Show Celestial Display #&s", false, 200000)]
		public static void ShowCelestialDisplay()
		{
			trueSKY.ShowCelestialDisplay = !trueSKY.ShowCelestialDisplay;
		}
		[MenuItem("Window/trueSky/Show Rain Textures #&p", false, 200000)]
		public static void ShowRainTextures()
		{
			trueSKY.ShowRainTextures = !trueSKY.ShowRainTextures;
		}
		[MenuItem("Window/trueSky/Show Water Textures #&p", false, 200000)]
		public static void ShowWaterTextures()
		{
			trueSKY.ShowWaterTextures = !trueSKY.ShowWaterTextures;
		}
		[MenuItem("Window/trueSky/Show Cubemaps #&m", false, 200000)]
		public static void ShowCubemaps()
		{
			trueSKY.ShowCubemaps = !trueSKY.ShowCubemaps;
		}

		[MenuItem("Window/trueSky/Recompile Shaders #&r", false, 200000)]
		public static void RecompileShaders()
		{
			trueSKY.RecompileShaders();
		}
		[MenuItem("Window/trueSky/Build SimulTest #&b", false, 200000)]
		public static void BuildTest()
		{
			BuildSimulTest("C:/temp/x64", "x64");
		}
		[SerializeField]
		static bool celestial = false;
		[SerializeField]
		static bool clouds = false;
		[SerializeField]
		static bool atmospherics = false;
		[SerializeField]
		static bool noise = false;
		[SerializeField]
		static bool debugging = false;
		[SerializeField]
		static bool lighting = false;
		[SerializeField]
		bool interpolation = false;
		[SerializeField]
		static bool precipitation = false;
        [SerializeField]
        static bool rainbows = false;
        [SerializeField]
		static bool water = false;
        [SerializeField]
        static bool buildOptions = false;
        public override void OnInspectorGUI()
		{
			trueSKY trueSky = (trueSKY)target;

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			{
				// General truesky settings
				trueSky.sequence = (Sequence)EditorGUILayout.ObjectField("Sequence Asset", trueSky.sequence, typeof(Sequence), false);
				trueSky.MetresPerUnit = EditorGUILayout.FloatField("Metres per Unit", trueSky.MetresPerUnit);
				if (trueSky.SimulVersion < trueSky.MakeSimulVersion(4, 2))
				{
					trueSky.CloudThresholdDistanceKm = EditorGUILayout.Slider("Threshold Distance (km)", trueSky.CloudThresholdDistanceKm, 0.0F, 10.0F);
					trueSky.DepthBlending = EditorGUILayout.Toggle("Depth Blending", trueSky.DepthBlending);
					trueSky.MinimumStarPixelSize = EditorGUILayout.FloatField("Minimum Star Pixel Size", trueSky.MinimumStarPixelSize);
				}
				EditorGUILayout.Space();

				// Interpolation settings
				interpolation = EditorGUILayout.Foldout(interpolation, "Update");
				if (interpolation)
				{
					trueSky.time = EditorGUILayout.FloatField("Time", trueSky.time);
					trueSky.speed = EditorGUILayout.FloatField("Speed", trueSky.speed);
					string[] options = new string[]
						{
							"Fixed Number", "Fixed Gametime", "Fixed Realtime",
						};
					trueSky.InterpolationMode = EditorGUILayout.Popup("Interpolation Mode", trueSky.InterpolationMode, options);
					trueSky.InstantUpdate = EditorGUILayout.Toggle("Instant Update", trueSky.InstantUpdate);
					if (trueSky.SimulVersion >= trueSky.MakeSimulVersion(4, 2))
					{
						trueSky.HighDetailProportion = EditorGUILayout.Slider("High Detail", trueSky.HighDetailProportion, 0.0F, 1.0F);
						trueSky.MediumDetailProportion = EditorGUILayout.Slider("Medium Detail", trueSky.MediumDetailProportion, trueSky.HighDetailProportion, 1.0F);

						trueSky.OriginLatitude = EditorGUILayout.Slider("Latitude", trueSky.OriginLatitude, -90.0F, 90.0F);
						trueSky.OriginLongitude = EditorGUILayout.Slider("Longitude", trueSky.OriginLongitude, -180.0F, 180.0F);
						trueSky.OriginHeading = EditorGUILayout.Slider("Heading", trueSky.OriginHeading, -180.0F, 180.0F);

						trueSky.SkylightAllMips = EditorGUILayout.Toggle("Update All Skylight Mips", trueSky.SkylightAllMips);
						trueSky.SkylightAllFaces = EditorGUILayout.Toggle("Update All Skylight Faces", trueSky.SkylightAllFaces);
						trueSky.SkylightAmortization = EditorGUILayout.IntSlider("Skylight Amortization", trueSky.SkylightAmortization, 1, 8);
					}
					EditorGUILayout.Space();
				}

				// Rendering settings
				if (trueSky.SimulVersion >= trueSky.MakeSimulVersion(4, 1))
				{
					GUIStyle cloudsStyle = new GUIStyle();
					cloudsStyle.active.textColor = new Color(0.5F, 0.4F, 0.4F, 0.5F);
					clouds = EditorGUILayout.Foldout(clouds, "Clouds");
					if (clouds)
					{
						string[] options = new string[]
						{
							"Grid", "Fixed",
						};
						EditorGUILayout.BeginVertical(cloudsStyle);
						trueSky.IntegrationScheme = EditorGUILayout.Popup("Integration Scheme", trueSky.IntegrationScheme, options);
						trueSky.CubemapResolution = EditorGUILayout.IntSlider("Cubemap Resolution", trueSky.CubemapResolution, 16, 2048);
						if (trueSky.SimulVersion > trueSky.MakeSimulVersion(4, 1))
						{
							trueSky.MaxCloudDistanceKm = EditorGUILayout.Slider("Max Cloud Distance (km)", trueSky.MaxCloudDistanceKm, 100.0F, 1000.0F);
							trueSky.RenderGridXKm = EditorGUILayout.Slider("Render Grid X (km)", trueSky.RenderGridXKm, 0.01F, 10.0F);
							trueSky.RenderGridZKm = EditorGUILayout.Slider("Render Grid Z (km)", trueSky.RenderGridZKm, 0.01F, 10.0F);
						}
						trueSky.CloudSteps = EditorGUILayout.IntSlider("Cloud Steps", trueSky.CloudSteps, 60, 300);
						trueSky.Amortization = EditorGUILayout.IntSlider("Amortization", trueSky.Amortization, 1, 4);
						trueSky.CloudThresholdDistanceKm = EditorGUILayout.Slider("Threshold Distance (km)", trueSky.CloudThresholdDistanceKm, 0.0F, 10.0F);
						trueSky.DepthSamplingPixelRange = EditorGUILayout.Slider("Depth Sampling Range", trueSky.DepthSamplingPixelRange, 0.0f, 4.0f);
						trueSky.DepthBlending = EditorGUILayout.Toggle("Depth Blending", trueSky.DepthBlending);
						EditorGUILayout.EndVertical();
					}
				}
				// Atmospheric settings
				atmospherics = EditorGUILayout.Foldout(atmospherics, "Atmospherics");
				if (atmospherics)
				{
					trueSky.AtmosphericsAmortization = EditorGUILayout.IntSlider("Atmospherics Amortization", trueSky.AtmosphericsAmortization, 1, 4);
					trueSky.GodRaysGrid = EditorGUILayout.Vector3Field("God Rays Grid", trueSky.GodRaysGrid);
                    if (trueSky.SimulVersion > trueSky.MakeSimulVersion(4, 1))
                    {
                        trueSky.CrepuscularRaysStrength = EditorGUILayout.Slider("Crepuscular Rays Strength", trueSky.CrepuscularRaysStrength, 0.0F, 1.0F);
                    }
                }

				if (trueSky.SimulVersion >= trueSky.MakeSimulVersion(4, 2))
				{
					lighting = EditorGUILayout.Foldout(lighting, "Lighting");
					if (lighting)
					{
						trueSky.DirectLight = EditorGUILayout.Slider("Direct Light", trueSky.DirectLight, 0.0F, 4.0F);
						trueSky.IndirectLight = EditorGUILayout.Slider("Indirect Light", trueSky.IndirectLight, 0.0F, 4.0F);
						trueSky.AmbientLight = EditorGUILayout.Slider("Ambient Light", trueSky.AmbientLight, 0.0F, 4.0F);
						trueSky.Extinction = EditorGUILayout.Slider("Extinction (per km)", trueSky.Extinction, 0.0F, 12.0F);
						trueSky.MieAsymmetry = EditorGUILayout.Slider("Mie Asymmetry", trueSky.MieAsymmetry, 0.0F, 0.999F);
					}

					// Noise settings
					noise = EditorGUILayout.Foldout(noise, "Noise");
					if (noise)
					{
						// Edge
						EditorGUILayout.LabelField("Edge Noise", EditorStyles.boldLabel);
						trueSky.EdgeNoisePersistence = EditorGUILayout.Slider("Persistence", trueSky.EdgeNoisePersistence, 0.0f, 1.0f);
						trueSky.EdgeNoiseFrequency = EditorGUILayout.IntSlider("Frequency", trueSky.EdgeNoiseFrequency, 1, 16);
						trueSky.EdgeNoiseTextureSize = EditorGUILayout.IntSlider("Texture Size", trueSky.EdgeNoiseTextureSize, 32, 256);
						trueSky.EdgeNoiseWavelengthKm = EditorGUILayout.Slider("Wavelength Km", trueSky.EdgeNoiseWavelengthKm, 0.01f, 50.0f);
						trueSky.MaxFractalAmplitudeKm = EditorGUILayout.Slider("Amplitude Km", trueSky.MaxFractalAmplitudeKm, 0.0f, 20.0f);

						EditorGUILayout.Space();
						EditorGUILayout.LabelField("Cell Noise", EditorStyles.boldLabel);
						trueSky.CellNoiseTextureSize = EditorGUILayout.IntSlider("Texture Size", trueSky.CellNoiseTextureSize, 32, 256);
						trueSky.CellNoiseWavelengthKm = EditorGUILayout.Slider("Wavelength Km", trueSky.CellNoiseWavelengthKm, 0.01f, 50.0f);
						EditorGUILayout.Space();
					}

					// Precipitation settings
					precipitation = EditorGUILayout.Foldout(precipitation, "Precipitation");
					if (precipitation)
					{
						trueSky.SimulationTimeRain = EditorGUILayout.Toggle("Sim Time Rain", trueSky.SimulationTimeRain);
						if (trueSky.SimulVersion >= trueSky.MakeSimulVersion(4, 2))
						{
							trueSky.MaxPrecipitationParticles = EditorGUILayout.IntField("Max Particles", trueSky.MaxPrecipitationParticles);
							trueSky.PrecipitationRadiusMetres = EditorGUILayout.Slider("Radius (m)", trueSky.PrecipitationRadiusMetres, 0.5F, 100.0F);
							trueSky.RainFallSpeedMS = EditorGUILayout.Slider("Rain fall speed (m/s)", trueSky.RainFallSpeedMS, 0.0F, 20.0F);
							trueSky.SnowFallSpeedMS = EditorGUILayout.Slider("Snow fall speed (m/s)", trueSky.SnowFallSpeedMS, 0.0F, 20.0F);
							trueSky.RainDropSizeMm = EditorGUILayout.Slider("Raindrop Size (mm)", trueSky.RainDropSizeMm, 0.05F, 20.0F);
							trueSky.SnowFlakeSizeMm = EditorGUILayout.Slider("Snowflake Size (mm)", trueSky.SnowFlakeSizeMm, 0.05F, 20.0F);
							trueSky.PrecipitationWindEffect = EditorGUILayout.Slider("WindEffect", trueSky.PrecipitationWindEffect, 0.0F, 1.0F);
							trueSky.PrecipitationWaver = EditorGUILayout.Slider("Waver", trueSky.PrecipitationWaver, 0.0F, 5.0F);
							trueSky.PrecipitationWaverTimescaleS = EditorGUILayout.Slider("WaverTimescaleS", trueSky.PrecipitationWaverTimescaleS, 0.1F, 60.0F);
							trueSky.PrecipitationThresholdKm = EditorGUILayout.Slider("ThresholdKm", trueSky.PrecipitationThresholdKm, 0.5F, 20.0F);
						}
						EditorGUILayout.Space();
					}

                    //Rainbow settings
                    rainbows = EditorGUILayout.Foldout(rainbows, "Rainbows");
                    if (rainbows)
                    {
                        if (trueSky.SimulVersion >= trueSky.MakeSimulVersion(4, 2))
                        {
                            trueSky.AutomaticRainbowPosition = EditorGUILayout.Toggle("Automatic Rainbow Position", trueSky.AutomaticRainbowPosition);
                            trueSky.RainbowElevation= EditorGUILayout.Slider("Rainbow Elevation", trueSky.RainbowElevation, -90.0F, 0.0F);
                            trueSky.RainbowAzimuth = EditorGUILayout.Slider("Rainbow Azimuth", trueSky.RainbowAzimuth, 0.0F, 360.0F);
                            trueSky.RainbowIntensity = EditorGUILayout.Slider("Rainbow Intensity", trueSky.RainbowIntensity, 0.0F, 10.0F);
                            trueSky.RainbowDepthPoint= EditorGUILayout.Slider("Rainbow Depth Point", trueSky.RainbowDepthPoint, 0.0F, 1.0F);
                            trueSky.AllowOccludedRainbows = EditorGUILayout.Toggle("Allow Occluded Rainbows", trueSky.AllowOccludedRainbows);
                            trueSky.AllowLunarRainbows = EditorGUILayout.Toggle("Allow Lunar Rainbows", trueSky.AllowLunarRainbows);
                        }
                        EditorGUILayout.Space();
                    }

					celestial = EditorGUILayout.Foldout(celestial, "Celestial");
					if (celestial)
					{
						trueSky.MaxSunRadiance = EditorGUILayout.FloatField("Max Sun Radiance", trueSky.MaxSunRadiance);
						trueSky.AdjustSunRadius = EditorGUILayout.Toggle("Adjust Sun Radius", trueSky.AdjustSunRadius);
						trueSky.backgroundTexture = (Texture)EditorGUILayout.ObjectField("Cosmic Background", trueSky.backgroundTexture, typeof(Texture), false);
						trueSky.moonTexture = (Texture)EditorGUILayout.ObjectField("Moon Texture", trueSky.moonTexture, typeof(Texture), false);
						trueSky.MinimumStarPixelSize = EditorGUILayout.FloatField("Minimum Star Pixel Size", trueSky.MinimumStarPixelSize);
						EditorGUILayout.Space();
					}

					water = EditorGUILayout.Foldout(water, "Water");
					if (water)
					{
						trueSky.RenderWater = EditorGUILayout.Toggle("Render Water", trueSky.RenderWater);
						trueSky.WaterFullResolution = EditorGUILayout.Toggle("Full Resolution Water", trueSky.WaterFullResolution);
						trueSky.EnableReflections = EditorGUILayout.Toggle("Enable Reflections", trueSky.EnableReflections);
						if (trueSky.EnableReflections)
						{
							trueSky.WaterFullResolutionReflections = EditorGUILayout.Toggle("Full Resolution Reflections", trueSky.WaterFullResolutionReflections);
							trueSky.WaterReflectionSteps = EditorGUILayout.IntSlider("Reflection Steps", trueSky.WaterReflectionSteps, 10, 100);
							trueSky.WaterReflectionPixelStep = EditorGUILayout.IntSlider("Pixel Steps", trueSky.WaterReflectionPixelStep, 1, 10);
							trueSky.WaterReflectionDistance = EditorGUILayout.Slider("Reflection Distance", trueSky.WaterReflectionDistance, 1000, 40000);
						}
						EditorGUILayout.Space();
					}
				}
				// Sound settings
				debugging = EditorGUILayout.Foldout(debugging, "Debugging");
				if (debugging)
				{
					trueSky.RenderInEditMode = EditorGUILayout.Toggle("Render in Edit Mode", trueSky.RenderInEditMode);
					string gv = SystemInfo.graphicsDeviceVersion;
					EditorGUILayout.LabelField("Unity Renderer", gv);
					if (gv.Contains("Direct3D 11") || gv.Contains("Vulkan")||gv.Contains("Direct3D 12"))
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
					showProfiling = EditorGUILayout.Foldout(showProfiling, "Profiling");

					if (showProfiling)
					{
						trueSky.maxGpuProfileLevel = EditorGUILayout.IntSlider("GPU Level", trueSky.maxGpuProfileLevel, 0, 8);
						trueSky.maxCpuProfileLevel = EditorGUILayout.IntSlider("CPU Level", trueSky.maxCpuProfileLevel, 0, 8);
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

					EditorGUILayout.Space();
				}
                buildOptions = EditorGUILayout.Foldout(buildOptions, "Build Options");
                if (buildOptions)
                {
                    trueSky.UsingIL2CPP = EditorGUILayout.Toggle("Use IL2CPP", trueSky.UsingIL2CPP);
                    EditorGUILayout.LabelField("Default enabled and locked for Xbox Series X (Scarlett) projects building with GameCore - Scarlett.");
                    EditorGUILayout.Space();
                }

            }
			EditorGUILayout.BeginHorizontal();
			if (trueSKY.advancedMode)
			{
				if (GUILayout.Button("Export Package"))
				{
					string simul_dir = "C:/Simul/4.2/Simul";
					string dir = simul_dir + "/Products/TrueSky/Release/";
					string version = "4.2.0.";
					string version_file = simul_dir + "/version.txt";
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
						UnityEngine.Debug.Log("The version string file could not be read: " + version_file);
					}
					string filenameRoot = "trueSKYPlugin-Unity2018-" + version;
					string[] aFilePaths = Directory.GetFiles(dir, filenameRoot + "*.unitypackage");
					int largest = 1;
					foreach (string p in aFilePaths)
					{
						string pat = filenameRoot + @"(.*)\.unitypackage";
						// Instantiate the regular expression object.
						Regex r = new Regex(pat, RegexOptions.IgnoreCase);
						// Match the regular expression pattern against a text string.
						Match m = r.Match(p);
						while (m.Success)
						{
							{
								Group g = m.Groups[1];
								string numstr = g.ToString();
								int ct = Convert.ToInt32(numstr);
								if (ct > largest)
									largest = ct;
							}
							m = m.NextMatch();
						}
					}
					largest++;
					string fileName = dir + filenameRoot + largest.ToString("D4") + ".unitypackage";
					ExportPackage(fileName, "x64");
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();

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

		/// <summary>
		/// This command is run from the CI server to test that the just-installed trueSKY package is ok.
		/// </summary>
		static void Test()
		{
			UnityEngine.Debug.Log("Test installed trueSKY Package");
			EditorApplication.Exit(0);
		}

		static void BuildSimulTestCmdLine()
		{
			Application.SetStackTraceLogType(LogType.Error | LogType.Assert | LogType.Exception | LogType.Warning | LogType.Log, StackTraceLogType.None);
			string f = CommandLineReader.GetCustomArgument("Path");
			f = f.Replace("\"", "");
			string p = CommandLineReader.GetCustomArgument("Platform");
			UnityEngine.Debug.Log("ExportPackageCmdLine " + f + ", " + p);
			BuildSimulTest(f, p);
		}
		static void BuildSimulTest(string path, string platform)
		{
			BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
			buildPlayerOptions.scenes = new[] { "Assets/Simul/SimulTest/TestLevel.unity" };
			
			string fullPath = path;
			buildPlayerOptions.locationPathName = fullPath+"/SimulTest.exe";
			try
			{
				if (Directory.Exists(fullPath))
					System.IO.Directory.Delete(fullPath, true);
			}
			catch(Exception )
			{

			}

			if (platform == "x64")
			{
				buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
			}
			else if (platform == "Linux")
			{
				buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
			}
			else if (platform == "XboxOne")
			{
				buildPlayerOptions.target = BuildTarget.XboxOne;
			}
			else if (platform == "PS4")
			{
				buildPlayerOptions.target = BuildTarget.PS4;
			}
			else
			{
				UnityEngine.Debug.LogError("Unknown platform:" + platform);
				return;
			}
			buildPlayerOptions.options = BuildOptions.Development|BuildOptions.AllowDebugging;
			BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
			BuildSummary summary = report.summary;
			if (summary.result == BuildResult.Succeeded)
			{
				Debug.Log("SimulTest Build succeeded: " + summary.totalSize + " bytes");
			}
			if (summary.result == BuildResult.Failed)
			{
				Debug.LogError("SimulTest Build failed");
			}
		}

		static void ExportPackageCmdLine()
		{
			Application.SetStackTraceLogType(LogType.Error | LogType.Assert | LogType.Exception | LogType.Warning | LogType.Log, StackTraceLogType.None);
			string f = CommandLineReader.GetCustomArgument("Filename");
			f = f.Replace("\"", "");
			f = f.Replace("\\", "/");
			string p = CommandLineReader.GetCustomArgument("Platform");
			UnityEngine.Debug.Log("ExportPackageCmdLine " + f + ", " + p);
			ExportPackage(f, p);
		}

		static void ExportPackage(string fileName, string platform)
		{
			if (platform == "x64")
			{
				AssetDatabase.ExportPackage("Assets/Simul", fileName, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

				UnityEngine.Debug.Log("Exported: " + fileName);
			}
			else if (platform == "PS4")
			{
				string[] paths =
				{
					"Assets/Simul/shaderbin/ps4",
					"Assets/Simul/Plugins/PS4"
				};
				AssetDatabase.ExportPackage(paths, fileName, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

				UnityEngine.Debug.Log("Exported: " + fileName);
			}
			else
			{
				UnityEngine.Debug.Log("Unknown platform:" + platform);
			}
		}
	}
}