//#define TRUESKY_LOGGING
using System;
using System.Text;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

using static simul.TrueSkyPluginRenderFunctionImporter;
using static simul.TrueSkyCameraBase;

namespace simul
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct vec3
	{
		public float x;
		public float y;
		public float z;
	}; 
	public struct vec4
	{
		public float x;
		public float y;
		public float z;
		public float w;
	}; 
	public struct int4
	{
		public int x, y, z, w;
	};
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct VolumeQueryResult
	{
		public vec4 pos_m;
		public int valid;
		public float density;
		public float direct_light;
		public float indirect_light;
		public float ambient_light;
		public float precipitation;
		public float rain_to_snow;
		public float padding;
	};
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct LightingQueryResult
	{
		public vec4 pos;
		public vec4 sunlight;      // we use vec4's here to avoid padding.
		public vec4 moonlight;
		public vec4 ambient;
		public vec3 padding;
		public int valid;
	};
	public struct LineQueryResult
	{
		public vec4 pos1_m;
		public vec4 pos2_m;
		public vec3 padding;
		public int valid;
		public float density;
		public float visibility;
		public float optical_thickness_metres;
		public float first_contact_metres;
	};
	public struct ExportLightningStrike
	{
		public int id;
		public vec3 pos;
		public vec3 endpos;
		public float brightness;
		public vec3 colour;
		public int age;
	};
	[StructLayout(LayoutKind.Explicit)]
	public struct Variant
	{
		[FieldOffset(0)] public float Float;
		[FieldOffset(0)] public int Int;
		[FieldOffset(0)] public double Double;
		[FieldOffset(0)] public long Int64;
		[FieldOffset(0)] public vec3 Vec3;
	};
	public struct Viewport
	{
		public int x, y, w, h;
	};
	public enum RenderStyle : uint
	{
		DEFAULT_STYLE = 0
			, UNITY_STYLE = 2
			, UNITY_STYLE_DEFERRED = 6
			, CUBEMAP_STYLE = 16
			, VR_STYLE = 32
			, VR_STYLE_ALTERNATE_EYE = 64
			, POST_TRANSLUCENT = 128
			, VR_STYLE_SIDE_BY_SIDE = 256
			, DEPTH_BLENDING = 512
			, DONT_COMPOSITE = 1024
			, CLEAR_SCREEN = 2048
			, DRAW_OVERLAYS = 4096
			, HIGH_DPI_AWARE = 16384
	};
	public enum UnityRenderOptions: uint
	{
		DEFAULT = 0
		, FLIP_OVERLAYS = 1      //! Compensate for Unity's texture flipping
		, NO_SEPARATION = 2      //! Faster
	};


	class SimulImports
	{
		static bool _initialized = false;
#if SIMUL_DEBUG_CALLBACK
		private static Mutex logMutex=new Mutex();
		static string debug_log;
		static TDebugOutputCallback debugOutputCallback =
			(string s) =>
			{
				logMutex.WaitOne();
				debug_log+=s;
				logMutex.ReleaseMutex();
			};

		static void OutputTrueSkyDebug()
		{
			logMutex.WaitOne();
			UnityEngine.Debug.Log(debug_log);
			logMutex.ReleaseMutex();
			debug_log="";
		}
#endif
		static SimulImports()
		{
		}

#if !UNITY_WSA
		private static Assembly SimulResolveEventHandler(object sender, System.ResolveEventArgs args)
		{
			UnityEngine.Debug.LogWarning("Resolving " + args.Name);
#if _WIN32
			return Assembly.Load("Assets/Plugins/x86/dependencies/" + args.Name);
#else
			return Assembly.Load("Assets/Plugins/x86_64/dependencies/" + args.Name);
#endif
		}
#endif

		int instanceCount = 0;
		SimulImports()
		{
			if (instanceCount == 0)
			{
#if !UNITY_WSA
				AppDomain currentDomain = AppDomain.CurrentDomain;
				currentDomain.AssemblyResolve += new ResolveEventHandler(SimulResolveEventHandler);
#endif
			}
			instanceCount++;
		}
		~SimulImports()
		{
			instanceCount--;
			if (_initialized && instanceCount == 0)
			{
				StaticPopPath("ShaderBinaryPath");
				StaticPopPath("ShaderPath");
				StaticPopPath("TexturePath");
#if SIMUL_DEBUG_CALLBACK
			StaticSetDebugOutputCallback(null);
#endif
			}
		}

		[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);


		public static void Init()
		{
#if !UNITY_EDITOR && SIMUL_STATIC_PLUGIN
			// For platforms that statically link trueSKY we need to register the plugin:
			if (!_staticInitialized)
			{
				RegisterPlugin();
				_staticInitialized = true;
			}
#endif
			if (_initialized)
			{
				return;
			}
			_initialized = true;
		}

	}
	public enum MoonPresets : UInt16
	{
		TheMoon = 0x0,
		AnotherMoon = 0x1
	};

	public struct Orbit
	{
		public double LongitudeOfAscendingNode;
		public double LongitudeOfAscendingNodeRate;
		public double Inclination;
		public double ArgumentOfPericentre;
		public double ArgumentOfPericentreRate;
		public double MeanDistance;
		public double Eccentricity;
		public double MeanAnomaly;
		public double MeanAnomalyRate;
	};

	public struct ExternalTexture
	{
		public static int static_version = 3;
		public int version;
		public IntPtr texturePtr;
		public int width;
		public int height;
		public int depth;
		public int pixelFormat;
		public uint numSamples;
		public uint resourceState;
	};

	public struct ExternalMoon
	{
		public static int static_version = 5; //Remove unused data relating to mesh
		public int version;
		public Orbit orbit;
		public float radiusArcMinutes;
		public string name;
		public bool illuminated;
		public ExternalTexture texture;
		public bool render;
		public vec3 colour;
		public float albedo;
	};

	public struct AuroralLayer
	{
		public float Base;
		public float Top;
		public float EmittedWavelength;
		public float Strength;

		public AuroralLayer(float _Base, float _Top, float _EmittedWavelength, float _Strength)
		{
			Base = _Base;
			Top = _Top;
			EmittedWavelength = _EmittedWavelength;
			Strength = _Strength;
		}

		public bool Valid()
		{
			return Base != 0.0f && Top != 0.0f && EmittedWavelength != 0.0f && Strength != 0.0f;
		}

		public vec4 ToVec4()
		{
			vec4 result;
			result.x = Base;
			result.y = Top;
			result.z = EmittedWavelength;
			result.w = Strength;

			return result;
		}
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ExternalRenderValues //these values dont change at runtime, unless explicitly called
    {

        public static int static_version = 10;  //HighDetailRange and Multiplier
		public int version;

        public float HighDetailProportion;         //!< For cloud volume update rate.
        public float MediumDetailProportion;           //!< For medium cloud volume update rate.

        public uint RenderSky;                      //!< Disable sky rendering, used primarily for when you only want water.

        public int MaximumCubemapResolution;       //!< Resolution to draw full-detail cloud buffers

        public int ShadowTextureSize;

        public uint Godrays_x;                     //Need converting to uint3
        public uint Godrays_y;
        public uint Godrays_z;

        public float PrecipitationRadiusMetres;

        public int EdgeNoiseTextureSize;
        public int WorleyTextureSize;

        public float RenderGridXKm;                    //!< Minimum grid width for raytracing.
        public float RenderGridZKm;                    //!< Minimum grid height for raytracing.

        public int WindowGridWidth;
        public int WindowGridHeight;

        public int WindowWidthKm;
        public int WindowHeightKm;

        public int DefaultNumSlices;
        public int DefaultAmortization;

        public float CloudThresholdDistanceKm;
        public float CloudDepthTemporalAlpha;
        public float DepthSamplingPixelRange;

        public int MaximumStarMagnitude;           //!< Largest magnitude of star to draw. Larger magnitudes are dimmer.

        public int IntegrationScheme;
        public int LightingMode;

        public int MaxFramesBetweenViewUpdates;
        public int AtmosphericsAmortization;
        public float RainNearThreshold;
		public float VirgaNearThresholdKm;
		public uint AutomaticSunPosition;
        public uint RealTimeWeatherEffects;

        public uint DoCloudRaytraceLighting;
        public uint RaysPerVoxel;
		public uint MaxRayRecursionDepth;

		public uint HighDetailMultiplier;          //Multiplier for number of grid steps. 
		public float HighDetailRangeKm;                //Range at which to apply increased grid steps.

	};

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct ExternalDynamicValues
	{
        public static int static_version = 3; //NearCloudExtinctionPerKm
        public int version;

		public float time;

		public float WindSpeedMS_X;
		public float WindSpeedMS_Y;
		public float WindSpeedMS_Z;

		public float MaxCloudDistanceKm;

		public float EdgeNoisePersistence;
		public int EdgeNoiseFrequency;
		public float EdgeNoiseWavelengthKm;
		public float CellNoiseWavelengthKm;
		public float MaxFractalAmplitudeKm;

		public float DirectLight;                  //!< The amount of direct light to be used for rendering.
		public float IndirectLight;                    //!< The amount of indirect or secondary light to be used for rendering.
		public float AmbientLight;                 //!< The amount of ambient light to be used for rendering.
		public float Extinction;                       //!< The amount of light scattered per metre - larger values produce darker clouds, default 0.05.
		public float MieAsymmetry;                 //!< Mie scattering eccentricity.

		public float MinimumStarPixelSize;         //!< Smallest pixel width to use drawing stars.
		public float StarBrightness;                   //!< Brightness multiplier for stars.
		public float CosmicBackgroundBrightness;       //!< Brightness multiplier for cosmic background.

		public float CloudShadowRangeKm;
		public float CloudShadowResolution;

		public int MaxPrecipitationParticles;
		public float RainFallSpeedMS;
		public float RainDropSizeMm;
		public float SnowFallSpeedMS;
		public float SnowFlakeSizeMm;
		public float PrecipitationWindEffect;
		public float PrecipitationWaver;
		public float PrecipitationWaverTimescaleS;
		public float PrecipitationThresholdKm;

		public uint AutomaticRainbowPosition;
		public float RainbowElevation;
		public float RainbowAzimuth;
		public float RainbowIntensity;
		public float RainbowDepthPoint;
		public uint AllowOccludedRainbow;
		public uint AllowLunarRainbow;

		public float CrepuscularRayStrength;

		public float OriginLatitude;
		public float OriginLongitude;
		public float OriginHeading;

		public float MaxSunRadiance;
		public uint AdjustSunRadius;

		public float CloudTintR;
		public float CloudTintG;
		public float CloudTintB;

		//Aurorae
		public float GeomagneticNorthPoleLatitude;
		public float GeomagneticNorthPoleLongitude;
		public float HighestLatitude;
		public float LowestLatitude;
		public float MaxBand;
		public float MinBand;
		public uint ShowAuroralOvalInCloudWindow;
		public float AuroraElectronFreeTime;
		public float AuroraElectronVolumeDensity;
		public float AuroralLayersIntensity;
		public vec4[] AuroraLayers;
		public UInt64 AuroraLayerCount;
		public float Start_Dawn1;
		public float End_Dawn1;
		public float Radius_Dawn1;
		public float OriginLatitude_Dawn1;
		public float OriginLongitude_Dawn1;
		public float Start_Dusk1;
		public float End_Dusk1;
		public float Radius_Dusk1;
		public float OriginLatitude_Dusk1;
		public float OriginLongitude_Dusk1;
		public float Start_Dawn2;
		public float End_Dawn2;
		public float Radius_Dawn2;
		public float OriginLatitude_Dawn2;
		public float OriginLongitude_Dawn2;
		public float Start_Dusk2;
		public float End_Dusk2;
		public float Radius_Dusk2;
		public float OriginLatitude_Dusk2;
		public float OriginLongitude_Dusk2;
		public int AuroraIntensityMapSize;
		public int AuroraTraceLength;

        public float NearCloudExtinctionPerKm;
		public int padi;
    };

	public class FMoon
	{
	
		public void SetOrbit(double LA, double LAR, double I, double AOP, double AOPR, double MD, double ECC, double MA, double MAR)
		{
			LongitudeOfAscendingNode = LA;
			LongitudeOfAscendingNodeRate = LAR;
			Inclination = I;
			ArgumentOfPericentre = AOP;
			ArgumentOfPericentreRate = AOPR;
			MeanDistance = MD;
			Eccentricity = ECC;
			MeanAnomaly = MA;
			MeanAnomalyRate = MAR;
		}
		public Orbit GetOrbit()
		{
			Orbit orb = new Orbit();
			orb.ArgumentOfPericentre = ArgumentOfPericentre;
			orb.ArgumentOfPericentreRate = ArgumentOfPericentreRate;
			orb.Eccentricity = Eccentricity;
			orb.Inclination = Inclination;
			orb.LongitudeOfAscendingNode = LongitudeOfAscendingNode;
			orb.LongitudeOfAscendingNodeRate = LongitudeOfAscendingNodeRate;
			orb.MeanAnomaly = MeanAnomaly;
			orb.MeanAnomalyRate = MeanAnomalyRate;
			orb.MeanDistance = MeanDistance;
			return orb;
		}

		public FMoon()
		{
			Name = "Moon ";// + index.ToString();
			Render = false;
			MoonTexture = null;
			Colour = new Color(0.136f, 0.136f, 0.136f);
			usePresets = true;
			MoonPreset = MoonPresets.TheMoon;
			Albedo = 0.136;
			RadiusArcMinutes = 16.0;
			LongitudeOfAscendingNode = 125.1228;
			LongitudeOfAscendingNodeRate = -0.0529538083;
			Inclination = 5.1454;
			ArgumentOfPericentre = 318.0634;
			ArgumentOfPericentreRate = 0.1643573223;
			MeanDistance = 60.2666;
			Eccentricity = 0.054900;
			MeanAnomaly = 115.3654;
			MeanAnomalyRate = 13.0649929509;
		}
		public String Name = "Moon";
		
		/** Texture to render, optional.*/
		public Texture MoonTexture;
		///** Optional 3D mesh to draw.*/
		//UPROPERTY(EditAnywhere,BlueprintReadWrite,Category="Visual")
		//UStaticMesh *StaticMesh;

		///** Material to use for mesh.*/
		//UPROPERTY(EditAnywhere,BlueprintReadWrite,Category="Visual")
		//UMaterialInterface *MeshMaterial;
		/** Colour from the moon - change from default to apply the effect .*/
		public Color Colour;// = new Color(0.136f, 0.136f, 0.136f);
		/** Should preset values be used, enabling will reset current values*/

		public bool usePresets = true;
		/** Select a preset (New Moons coming soon)*/
		public MoonPresets MoonPreset = MoonPresets.TheMoon;
		/** The proportion of light that is reflected by the moon.*/
		public double Albedo = 0.136;
		/** Rotation/Swivel of the orbit around the Earth */
		public double LongitudeOfAscendingNode;
		/** Rate of change of the Longitude of Acsending Node*/
		public double LongitudeOfAscendingNodeRate;
		/** The tilt of the orbit.*/
		public double Inclination;
		/** Angle from the body's ascending node to its periapsis*/
		public double ArgumentOfPericentre;

		/** Rate of change of the Argument of Pericentre*/
		public double ArgumentOfPericentreRate;
		
	/** Mean distance from Earth. Expressed in Earth's Equitorial Radii*/
		public double MeanDistance;
		/**Shape of orbit (0=circle, 0-1=ellipse, 1=parabola)*/
		public double Eccentricity;
		/** The fraction of an elliptical orbit's period that has elapsed since the orbiting body passed periapsis*/
		public double MeanAnomaly;
		/** Rate of Anomaly Change.*/
		public double MeanAnomalyRate;
		/** Radius of the Moon. Will affect lighting as more light is reflected*/
		public double RadiusArcMinutes;

		public bool Render;
		public bool DestroyMoon;
	};

	public class Aurorae
	{
		//Auroral Oval
		public float GeomagneticNorthPoleLatitude;
		public float GeomagneticNorthPoleLongitude;
		public float HighestLatitude;
		public float LowestLatitude;
		public float MaxBand;
		public float MinBand;
		public bool ShowAuroralOvalInCloudWindow;

		//Auroral Layers
		public List<AuroralLayer> AuroralLayers;
		public AuroralLayer EditAuroralLayer;
		public int EditAuroralLayerIndex;

		//Aurora Intensity
		public float AuroraElectronFreeTime;
		public float AuroraElectronVolumeDensity;
		public float AuroralLayersIntensity;

		//FAC
		public float Start_Dawn1;
		public float End_Dawn1;
		public float Radius_Dawn1;
		public float OriginLatitude_Dawn1;
		public float OriginLongitude_Dawn1;

		public float Start_Dusk1;
		public float End_Dusk1;
		public float Radius_Dusk1;
		public float OriginLatitude_Dusk1;
		public float OriginLongitude_Dusk1;

		public float Start_Dawn2;
		public float End_Dawn2;
		public float Radius_Dawn2;
		public float OriginLatitude_Dawn2;
		public float OriginLongitude_Dawn2;

		public float Start_Dusk2;
		public float End_Dusk2;
		public float Radius_Dusk2;
		public float OriginLatitude_Dusk2;
		public float OriginLongitude_Dusk2;

		//Other
		public int AuroraIntensityMapSize;
		public int AuroraTraceLength;

		public Aurorae()
		{
			GeomagneticNorthPoleLatitude = 80.65f;
			GeomagneticNorthPoleLongitude = -72.68f;
			HighestLatitude = 80.0f;
			LowestLatitude = 60.0f;
			MaxBand = 10.0f;
			MinBand = 3.0f;
			ShowAuroralOvalInCloudWindow = true;
			AuroraElectronFreeTime = 1.0f;
			AuroraElectronVolumeDensity = 1.0f;
			AuroralLayersIntensity = 1.0f;
			AuroralLayers = new List<AuroralLayer>();
			EditAuroralLayer = new AuroralLayer(0.0f, 0.0f, 0.0f, 0.0f);
			EditAuroralLayerIndex = -1;
			Start_Dawn1 = 180.0f;
			End_Dawn1 = -30.0f;
			Radius_Dawn1 = 19.0f;
			OriginLatitude_Dawn1 = 90.0f;
			OriginLongitude_Dawn1 = 0.0f;
			Start_Dusk1 = 0.0f;
			End_Dusk1 = -180.0f;
			Radius_Dusk1 = 20.0f;
			OriginLatitude_Dusk1 = 90.0f;
			OriginLongitude_Dusk1 = 0.0f;
			Start_Dawn2 = 150.0f;
			End_Dawn2 = 0.0f;
			Radius_Dawn2 = 21.0f;
			OriginLatitude_Dawn2 = 90.0f;
			OriginLongitude_Dawn2 = 0.0f;
			Start_Dusk2 = 30.0f;
			End_Dusk2 = -150.0f;
			Radius_Dusk2 = 22.0f;
			OriginLatitude_Dusk2 = 90.0f;
			OriginLongitude_Dusk2 = 0.0f;
			AuroraIntensityMapSize = 512;
			AuroraTraceLength = 100;

			SetDefaultAuroralLayers();
		}

		public vec4[] GetAuroralLayerVec4Array()
		{
			vec4[] al_vec4_array = new vec4[GetAuroralLayerCount()];
			int index = 0;
			foreach(AuroralLayer al in AuroralLayers)
			{
				al_vec4_array[index] = al.ToVec4();
				index++;
			}
			return al_vec4_array;
		}

		public int GetAuroralLayerCount()
		{
			return AuroralLayers.Count;
		} 

		public void SetDefaultAuroralLayers()
		{
			AuroralLayers.Clear();
			AuroralLayers.Add(new AuroralLayer(85.0f, 105.0f, 427.8f, 30.0f));
			AuroralLayers.Add(new AuroralLayer(85.0f, 105.0f, 670.0f, 18.0f));
			AuroralLayers.Add(new AuroralLayer(85.0f, 105.0f, 391.4f, 6.0f));
			AuroralLayers.Add(new AuroralLayer(100.0f, 210.0f, 557.7f, 100.0f));
			AuroralLayers.Add(new AuroralLayer(100.0f, 210.0f, 471.0f, 6.0f));
			AuroralLayers.Add(new AuroralLayer(200.0f, 250.0f, 630.0f, 30.0f));
			EditAuroralLayerIndex = 5;
		}

		public void SetDefaultFieldAlignedCurrents()
		{
			Start_Dawn1 = 180.0f;
			End_Dawn1 = -30.0f;
			Radius_Dawn1 = 19.0f;
			OriginLatitude_Dawn1 = 90.0f;
			OriginLongitude_Dawn1 = 0.0f;
			Start_Dusk1 = 0.0f;
			End_Dusk1 = -180.0f;
			Radius_Dusk1 = 20.0f;
			OriginLatitude_Dusk1 = 90.0f;
			OriginLongitude_Dusk1 = 0.0f;
			Start_Dawn2 = 150.0f;
			End_Dawn2 = 0.0f;
			Radius_Dawn2 = 21.0f;
			OriginLatitude_Dawn2 = 90.0f;
			OriginLongitude_Dawn2 = 0.0f;
			Start_Dusk2 = 30.0f;
			End_Dusk2 = -150.0f;
			Radius_Dusk2 = 22.0f;
			OriginLatitude_Dusk2 = 90.0f;
			OriginLongitude_Dusk2 = 0.0f;
		}
	}

	
	[ExecuteInEditMode]
	public class trueSKY : MonoBehaviour
	{
		#region API
		public int SimulVersionMajor = 0;
		public int SimulVersionMinor = 0;
		public int SimulVersionBuild = 0;


		public int SimulVersion
		{
			get
			{
				return MakeSimulVersion(SimulVersionMajor, SimulVersionMinor);
			}
		}
		public int MakeSimulVersion(int major, int minor)
		{
			return (major << 8) + minor;
		}
		private static trueSKY trueSkySingleton = null;

		public trueSKY()
		{

		}

		void OnEnable()
		{
			if (!cloudShadowRT)
			{
				cloudShadowRT = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
				cloudShadowRT.name = "CloudShadowRT";
				cloudShadowRT.Create();
			}
			if (!lossRT)
			{
				lossRT = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
				lossRT.name = "lossRT";
				lossRT.Create();		
			}
			if (!inscatterRT)
			{
				inscatterRT = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
				inscatterRT.name = "inscatterRT";
				inscatterRT.Create();			
			}
			if (!cloudVisibilityRT)
			{
				cloudVisibilityRT = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
				cloudVisibilityRT.name = "cloudVisibilityRT";
				cloudVisibilityRT.Create();
			}

			LossTexture.renderTexture = lossRT;
			InscatterTexture.renderTexture = inscatterRT;
			CloudVisibilityTexture.renderTexture = cloudVisibilityRT;
			CloudShadowTexture.renderTexture = cloudShadowRT;
		}

		~trueSKY()
		{
			if (this == trueSkySingleton)
				trueSkySingleton = null;
		}

		/// <summary>
		/// Get the trueSKY component in the scene.
		/// </summary>
		/// <returns></returns>
		public static trueSKY GetTrueSky()
		{
			if (trueSkySingleton == null)
				trueSkySingleton = GameObject.FindObjectOfType<trueSKY>();
			return trueSkySingleton;
		}
		public void SetPointLight(int id, Vector3 pos, float min_radius, float max_radius, Vector3 irradiance)
		{
			Vector3 convertedPos = UnityToTrueSkyPosition(pos);             // convert from Unity format to trueSKY  

			float[] p = { convertedPos.x, convertedPos.y, convertedPos.z };
			float[] i = { irradiance.x, irradiance.z, irradiance.y };
			StaticSetPointLight(id, p, min_radius, max_radius, i);
		}
		public LightingQueryResult LightingQuery(int id, Vector3 pos)
		{
			Vector3 convertedPos = UnityToTrueSkyPosition(pos);             // convert from Unity format to trueSKY

			LightingQueryResult res = new LightingQueryResult();
			IntPtr unmanagedPosPtr = Marshal.AllocHGlobal(12);
			IntPtr unmanagedResultPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LightingQueryResult)));
			float[] p = { convertedPos.x, convertedPos.y, convertedPos.z };
			Marshal.Copy(p, 0, unmanagedPosPtr, 3);
			StaticLightingQuery(id, unmanagedPosPtr, unmanagedResultPtr);
			res = (LightingQueryResult)Marshal.PtrToStructure(unmanagedResultPtr, typeof(LightingQueryResult));

			// Call unmanaged code
			Marshal.FreeHGlobal(unmanagedPosPtr);
			Marshal.FreeHGlobal(unmanagedResultPtr);
			return res;
		}
		public VolumeQueryResult GetCloudQuery(int id, Vector3 pos)
		{
			Vector3 convertedPos = UnityToTrueSkyPosition(pos);             // convert from Unity format to trueSKY

			VolumeQueryResult res = new VolumeQueryResult();
			IntPtr unmanagedPosPtr = Marshal.AllocHGlobal(12);
			IntPtr unmanagedResultPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VolumeQueryResult)));
			float[] p = { convertedPos.x, convertedPos.y, convertedPos.z };
			Marshal.Copy(p, 0, unmanagedPosPtr, 3);
			StaticCloudPointQuery(id, unmanagedPosPtr, unmanagedResultPtr);
			res = (VolumeQueryResult)Marshal.PtrToStructure(unmanagedResultPtr, typeof(VolumeQueryResult));

			// Call unmanaged code
			Marshal.FreeHGlobal(unmanagedPosPtr);
			Marshal.FreeHGlobal(unmanagedResultPtr);
			return res;
		}
		public LineQueryResult CloudLineQuery(int id, Vector3 startpos, Vector3 endpos)
		{
			Vector3 convertedStartPos = UnityToTrueSkyPosition(startpos);
			Vector3 convertedEndPos = UnityToTrueSkyPosition(endpos);

			LineQueryResult res = new LineQueryResult();
			IntPtr unmanagedPosPtr1 = Marshal.AllocHGlobal(12);
			IntPtr unmanagedPosPtr2 = Marshal.AllocHGlobal(12);
			IntPtr unmanagedResultPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LineQueryResult)));
			// swap y and z because Unity
			float[] p1 = { convertedStartPos.x, convertedStartPos.y, convertedStartPos.z };
			float[] p2 = { convertedEndPos.x, convertedEndPos.y, convertedEndPos.z };
			Marshal.Copy(p1, 0, unmanagedPosPtr1, 3);
			Marshal.Copy(p2, 0, unmanagedPosPtr2, 3);
			StaticCloudLineQuery(id, unmanagedPosPtr1, unmanagedPosPtr2, unmanagedResultPtr);
			res = (LineQueryResult)Marshal.PtrToStructure(unmanagedResultPtr, typeof(LineQueryResult));
			// Call unmanaged code
			Marshal.FreeHGlobal(unmanagedPosPtr1);
			Marshal.FreeHGlobal(unmanagedPosPtr2);
			Marshal.FreeHGlobal(unmanagedResultPtr);
			return res;
		}
		public float GetCloudAtPosition(Vector3 pos)
		{
			Vector3 convertedPos = UnityToTrueSkyPosition(pos);
			float[] x = { convertedPos.x, convertedPos.y, convertedPos.z };
			float ret = StaticGetRenderFloatAtPosition("Cloud", x);
			return ret;
		}
		public float GetCloudShadowAtPosition(Vector3 pos)
		{
			Vector3 convertedPos = UnityToTrueSkyPosition(pos);
			float[] x = { convertedPos.x, convertedPos.y, convertedPos.z };
			float ret = StaticGetRenderFloatAtPosition("CloudShadow", x);
			return ret;
		}
		public float GetPrecipitationAtPosition(Vector3 pos)
		{
			Vector3 convertedPos = UnityToTrueSkyPosition(pos);
			float[] x = { convertedPos.x, convertedPos.y, convertedPos.z };
			float ret = StaticGetRenderFloatAtPosition("Precipitation", x);
			return ret;
		}

		// Simul versions < 4.2      | Simul versions >= 4.2. Layer is a UID
		// Layer 0 = SkyKeyframe     | Layer/UID 0  = SkyKeyframe
		// Layer 1 = CloudKeyframe   | Layer/UID 1+ = CloudKeyframe
		// Layer 2 = Cloud2DKeyframe |

		// These are for keyframe editing:

		//Get Number of Keyframes
		public int GetNumSkyKeyframes()
		{
			return StaticRenderGetNumKeyframes(0);
		}
		public int GetNumCloudKeyframes(int layer)
		{
			if (SimulVersion < MakeSimulVersion(4, 2))
			{
				return StaticRenderGetNumKeyframes(1);
			}
			else
			{
				return StaticRenderGetNumKeyframes((int)GetCloudLayerByIndex(layer));
			}
		}
		public int GetNumCloud2DKeyframes()
		{
			if (SimulVersion < MakeSimulVersion(4, 2))
			{
				return StaticRenderGetNumKeyframes(2);
			}
			return -1;
		}

		//Insert Keyframes
		public uint InsertSkyKeyframe(float t)
		{
			return StaticRenderInsertKeyframe(0, t);
		}
		public uint InsertCloudKeyframe(float t, int layer)
		{
			if (SimulVersion < MakeSimulVersion(4, 2))
			{
				return StaticRenderInsertKeyframe(1, t);
			}
			else
			{ 
				return StaticRenderInsertKeyframe((int)GetCloudLayerByIndex(layer), t);
			}
		}
		public uint Insert2DCloudKeyframe(float t)
		{
			if (SimulVersion < MakeSimulVersion(4, 2))
			{
				return StaticRenderInsertKeyframe(2, t);
			}
			return 0;
		}

		//Delete Keyframe
		public void DeleteKeyframe(uint uid)
		{
			StaticRenderDeleteKeyframe(uid);
		}

		//Get Keyframe by index
		public uint GetSkyKeyframeByIndex(int index)
		{
			return StaticRenderGetKeyframeByIndex(0, index);
		}
		public uint GetCloudKeyframeByIndex(int index, int layer)
		{
			if (SimulVersion < MakeSimulVersion(4, 2))
			{
				return StaticRenderGetKeyframeByIndex(1, index);
			}
			else
			{
				return StaticRenderGetKeyframeByIndex((int)GetCloudLayerByIndex(layer), index);
			}
		}
		public uint GetCloud2DKeyframeByIndex(int index)
		{
			if (SimulVersion < MakeSimulVersion(4, 2))
			{
				return StaticRenderGetKeyframeByIndex(2, index);
			}
			return 0;
		}

		//Get interpolated Keyframe
		public uint GetInterpolatedSkyKeyframe()
		{
			return GetInterpolatedSkyKeyframeUniqueId();
		}
		public uint GetInterpolatedCloudKeyframe(int layer)
		{
			if (SimulVersion < MakeSimulVersion(4, 2))
			{
				return GetInterpolatedCloudKeyframeUniqueId(1);
			}
			else
			{
				return GetInterpolatedCloudKeyframeUniqueId((int)GetCloudLayerByIndex(layer));
			}
		}

		//Get CloudLayer UID from Index
		public uint GetCloudLayerByIndex(int index)
		{
			if (SimulVersion < MakeSimulVersion(4, 2))
			{
				return 1;
			}
			else
			{
				return GetCloudLayerUIDByIndex(index);
			}
		}

		// Getting and changing properties of keyframes.
		public void SetKeyframeValue(uint uid, string name, object value)
		{
			if (value.GetType() == typeof(double))
			{
				double d = (double)value;
				StaticRenderKeyframeSetFloat(uid, name, (float)d);
			}
			else if (value.GetType() == typeof(float) || value.GetType() == typeof(double))
			{
				StaticRenderKeyframeSetFloat(uid, name, (float)value);
			}
			else if (value.GetType() == typeof(int))
			{
				StaticRenderKeyframeSetInt(uid, name, (int)value);
			}
			else if (value.GetType() == typeof(bool))
			{
				StaticRenderKeyframeSetBool(uid, name, (bool)value);
			}
		}
		public T GetKeyframeValue<T>(uint uid, string name)
		{
			T result = (T)Convert.ChangeType(0, typeof(T));

			if (result.GetType() == typeof(double))
			{
				double value = (double)StaticRenderKeyframeGetFloat(uid, name);
				result = (T)Convert.ChangeType(value, typeof(T));
			}
			else if (result.GetType() == typeof(float))
			{
				float value = StaticRenderKeyframeGetFloat(uid, name);
				result = (T)Convert.ChangeType(value, typeof(T));
			}
			else if (result.GetType() == typeof(int))
			{
				int value = StaticRenderKeyframeGetInt(uid, name);
				result = (T)Convert.ChangeType(value, typeof(T));
			}
			else if (result.GetType() == typeof(bool))
			{
				bool value = StaticRenderKeyframeGetBool(uid, name);
				result = (T)Convert.ChangeType(value, typeof(T));
			}

			return result;
		}

		public void SetKeyframerValue(uint uid, string name, object value)
		{
			if (value.GetType() == typeof(double))
			{
				double d = (double)value;
				StaticRenderKeyframerSetFloat(uid, name, (float)d);
			}
			else if (value.GetType() == typeof(float))
			{
				StaticRenderKeyframerSetFloat(uid, name, (float)value);
			}
			else if (value.GetType() == typeof(int))
			{
				StaticRenderKeyframerSetInt(uid, name, (int)value);
			}
		}

		public T GetKeyframerValue<T>(uint uid, string name)
		{
			T result = (T)Convert.ChangeType(0, typeof(T));

			if (result.GetType() == typeof(double))
			{
				double value = (double)StaticRenderKeyframerGetFloat(uid, name);
				result = (T)Convert.ChangeType(value, typeof(T));
			}
			else if (result.GetType() == typeof(float))
			{
				float value = StaticRenderKeyframerGetFloat(uid, name);
				result = (T)Convert.ChangeType(value, typeof(T));
			}
			else if (result.GetType() == typeof(int))
			{
				int value = StaticRenderKeyframerGetInt(uid, name);
				result = (T)Convert.ChangeType(value, typeof(T));
			}

			return result;
		}

		public uint CreateCloudKeyframer(string name)
		{
			return StaticRenderCreateCloudKeyframer(name);
		}

		public void DeleteCloudKeyframer(uint uid)
		{
			StaticRenderDeleteCloudKeyframer(uid);
		}
		//Storms
		public uint GetStormUidByIndex(int index)
		{
			return GetStormByIndex(index);
		}
		public uint GetStormUidAtTime(float time)
		{
			return GetStormAtTime(time);
		}

		public T GetStormValue<T>(uint uid, string name)
		{
			return GetKeyframeValue<T>(uid, name);
		}
		public void SetStormValue(uint uid, string name, object value)
		{
			SetKeyframeValue(uid, name, value);
		}



		//public float GetStormFloat(uint uid, string name)
		//{
		//	return StaticRenderKeyframeGetFloat(uid, name);
		//}
		//public void SetStormFloat(uint uid, string name, float value)
		//{
		//	SetKeyframeValue(uid, name, value);
		//}
		//public int GetStormInt(uint uid, string name)
		//{
		//	return StaticRenderKeyframeGetInt(uid, name);
		//}
		//public void SetStormInt(uint uid, string name, int value)
		//{
		//	StaticRenderKeyframeSetInt(uid, name, value);
		//}
		//public bool GetStormBool(uint uid, string name)
		//{
		//	return StaticRenderKeyframeGetBool(uid, name);
		//}
		//public void SetStormBool(uint uid, string name, bool value)
		//{
		//	StaticRenderKeyframeSetBool(uid, name, value);
		//}
		/// <summary>
		/// Retrieve the active strike. end and start position in metres.
		/// </summary>
		public ExportLightningStrike GetCurrentStrike()
		{
			IntPtr unmanagedResultPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ExportLightningStrike)));
			StaticGetLightningBolts(unmanagedResultPtr, 1);

			var strike = (ExportLightningStrike)Marshal.PtrToStructure(unmanagedResultPtr, typeof(ExportLightningStrike));
			strike.pos.x /= MetresPerUnit;
			strike.pos.y /= MetresPerUnit;
			strike.pos.z /= MetresPerUnit;
			strike.endpos.x /= MetresPerUnit;
			strike.endpos.y /= MetresPerUnit;
			strike.endpos.z /= MetresPerUnit;

			var c = strike.pos.y;
			strike.pos.y = strike.pos.z;
			strike.pos.z = c;

			c = strike.endpos.y;
			strike.endpos.y = strike.endpos.z;
			strike.endpos.z = c;

			return strike;
		}
		/// <summary>
		/// Spawns a strike.
		/// </summary>
		/// <param name="start"> Staring position of the strike (unity units) </param>
		/// <param name="end"> End position of the strike (unity units) </param>
		public void SpawnStrike(Vector3 start, Vector3 end)
		{
			start *= MetresPerUnit;
			end *= MetresPerUnit;

			IntPtr unmanagedStart = Marshal.AllocHGlobal(sizeof(float) * 3);
			IntPtr unmanagedEnd = Marshal.AllocHGlobal(sizeof(float) * 3);
			IntPtr unmanagedColour = Marshal.AllocHGlobal(sizeof(float) * 3);
			float[] ns = { start.x, start.z, start.y };
			float[] ne = { end.x, end.z, end.y };
			float[] nc = { 1.0f, 1.0f, 1.0f };
			Marshal.Copy(ns, 0, unmanagedStart, 3);
			Marshal.Copy(ne, 0, unmanagedEnd, 3);
			Marshal.Copy(nc, 0, unmanagedColour, 3);

			StaticSpawnLightning2(unmanagedStart, unmanagedEnd, 0.0f, unmanagedColour);

			Marshal.FreeHGlobal(unmanagedStart);
			Marshal.FreeHGlobal(unmanagedEnd);
			Marshal.FreeHGlobal(unmanagedColour);
		}

		// --- Conversion functions for TrueSky position/direction <=> Unity position/direction ---
		static public Vector3 TrueSkyToUnityPosition(Vector3 ts_pos)
		{
			Matrix4x4 u2t = UnityToTrueSkyMatrix();
			Matrix4x4 t2u = u2t.inverse;
			Vector4 u_pos = t2u * (new Vector4(ts_pos.x, ts_pos.z, ts_pos.y, 1.0F));
			return new Vector3(u_pos.x, u_pos.y, u_pos.z);
		}
		static public Vector3 TrueSkyToUnityDirection(Vector3 ts_dir)
		{
			Matrix4x4 u2t = UnityToTrueSkyMatrix();
			Matrix4x4 t2u = u2t.inverse;
			Vector4 u_dir = t2u * (new Vector4(ts_dir.x, ts_dir.z, ts_dir.y, 0.0F));
			return new Vector3(u_dir.x, u_dir.y, u_dir.z);
		}
		static public Vector3 UnityToTrueSkyPosition(Vector3 upos)
		{
			Vector4 u_dir = UnityToTrueSkyMatrix() * (new Vector4(upos.x, upos.y, upos.z, 1.0F));
			return new Vector3(u_dir.x, u_dir.z, u_dir.y);
		}
		static public Vector3 UnityToTrueSkyDirection(Vector3 u_dir)
		{
			Vector4 ts_dir = UnityToTrueSkyMatrix() * (new Vector4(u_dir.x, u_dir.y, u_dir.z, 0));
			return new Vector3(ts_dir.x, ts_dir.z, ts_dir.y);
        }
        static public Matrix4x4 UnityToTrueSkyMatrix()
        {
            Matrix4x4 transform = trueSKY.GetTrueSky().transform.worldToLocalMatrix;
            float metresPerUnit = trueSKY.GetTrueSky().MetresPerUnit;
            Matrix4x4 scale = new Matrix4x4();
            scale.SetTRS(new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 1.0F), new Vector3(metresPerUnit, metresPerUnit, metresPerUnit));
            transform = scale * transform;
            return transform;
        }

		static public void SanitizeSize(ref int value, int minRes = 64, int maxRes = 2048)
		{// Rounds the cube map size to nearest power of two.
			value = Mathf.Clamp(Mathf.NextPowerOfTwo(value), minRes, maxRes);
		}

	#endregion
	public List<FMoon> _moons = new List<FMoon>();

		public void AddNewMoon()
		{
			if (_moons.Count < 10)
            {
				FMoon _temp;
				_temp = new FMoon();
                _moons.Add(_temp);
			}
		}

            public static void InitExternalTexture(ref ExternalTexture ext, Texture tex)
		{
			if (tex != null)
			{
				ext.texturePtr = tex.GetNativeTexturePtr();
				ext.width = tex.width;
				ext.height = tex.height;
				ext.depth = 0;
				ext.pixelFormat = 0;
				ext.numSamples = 0;
				ext.resourceState = 0;
			}
		}

		public Aurorae aurorae = new Aurorae();

		[SerializeField]
		float _metresPerUnit = 1.0f;
		public float MetresPerUnit
		{
			get
			{
				return _metresPerUnit;
			}
			set
			{
				if (_metresPerUnit != value) try
				{
					_metresPerUnit = value;
				}
				catch (Exception exc)
				{
					UnityEngine.Debug.Log(exc.ToString());
				}
			}
		}

		[SerializeField]
		bool _renderInEditMode = true;
		public bool RenderInEditMode
		{
			get
			{
				return _renderInEditMode;
			}
			set
			{
				if (_renderInEditMode != value) try
					{
						_renderInEditMode = value;
						StaticSetRenderBool("EnableRendering", Application.isPlaying || _renderInEditMode);
						//RepaintAll();
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _starBrightness = 1.0f;
		public float StarBrightness
		{
			get
			{
				return _starBrightness;
			}
			set
			{
				if (_starBrightness != value)
				{
					_starBrightness = value;
					StaticSetRenderFloat("render:StarBrightness", _starBrightness); //Need Fix
				}
			}
		}

		[SerializeField]
		float _backgroundBrightness = 0.1f;
		public float BackgroundBrightness
		{
			get
			{
				return _backgroundBrightness;
			}
			set
			{
				if (_backgroundBrightness != value)
				{
					_backgroundBrightness = value;
					StaticSetRenderFloat("render:BackgroundBrightness", _backgroundBrightness);
				}
			}
		}

		[SerializeField]
		int _maximumStarMagniute = 2;
		public int MaximumStarMagnitude
		{
			get
			{
				return _maximumStarMagniute;
			}
			set
			{
				if (_maximumStarMagniute != value)
				{
					_maximumStarMagniute = value;
					updateERV = true;
				}
				//StaticSetRenderFloat("render:MaximumStarMagniute", _maximumStarMagniute); //Need Fix
			}
		}

		float _minimumStarPixelSize = 1.0f;
		public float MinimumStarPixelSize
		{
			get
			{
				return _minimumStarPixelSize;
			}
			set
			{
				if (_minimumStarPixelSize != value)
				{
					_minimumStarPixelSize = value;
					StaticSetRenderFloat("render:minimumstarpixelsize", _minimumStarPixelSize);
				}
			}
		}

		//we could add a default light for night scenes


		[SerializeField]
		SortedSet<string> _highlightConstellation;
		public SortedSet<string> HighlightConstellation
		{
			get
			{
				return _highlightConstellation;
			}
			set
			{
				if (_highlightConstellation != value)
				{
					_highlightConstellation = value;
					StaticTriggerAction("clearhighlightconstellations");
					foreach (string c in _highlightConstellation)
					{
						StaticSetRenderString("HighlightConstellation", c);
					}
				}
			}
		}

		[SerializeField]
		bool _renderWater = false;
		public bool RenderWater
		{
			get
			{
				return _renderWater;
			}
			set
			{
				if (_renderWater != value) try
					{
						_renderWater = value;
						StaticSetRenderBool("renderWater", _renderWater);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		bool _waterFullResolution = true;
		public bool WaterFullResolution
		{
			get
			{
				return _waterFullResolution;
			}
			set
			{
				if (_waterFullResolution != value) try
					{
						_waterFullResolution = value;
						StaticSetRenderBool("waterfullresolution", _waterFullResolution);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		bool _enableReflections = true;
		public bool EnableReflections
		{
			get
			{
				return _enableReflections;
			}
			set
			{
				if (_enableReflections != value) try
					{
						_enableReflections = value;
						StaticSetRenderBool("enablewaterreflections", _enableReflections);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		bool _waterFullResolutionReflections = true;
		public bool WaterFullResolutionReflections
		{
			get
			{
				return _waterFullResolutionReflections;
			}
			set
			{
				if (_waterFullResolutionReflections != value) try
					{
						_waterFullResolutionReflections = value;
						StaticSetRenderBool("waterfullresolutionreflection", _waterFullResolutionReflections);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		int _waterReflectionSteps = 100;
		public int WaterReflectionSteps
		{
			get
			{
				return _waterReflectionSteps;
			}
			set
			{
				if (_waterReflectionSteps != value)
				{
					_waterReflectionSteps = value;
					StaticSetRenderInt("waterReflectionSteps", _waterReflectionSteps);
				}
			}
		}

		[SerializeField]
		int _waterReflectionPixelStep = 2;
		public int WaterReflectionPixelStep
		{
			get
			{
				return _waterReflectionPixelStep;
			}
			set
			{
				if (_waterReflectionPixelStep != value)
				{
					_waterReflectionPixelStep = value;
					StaticSetRenderInt("waterReflectionPixelStep", _waterReflectionPixelStep);
				}
			}
		}

		[SerializeField]
		float _waterReflectionDistance = 20000;
		public float WaterReflectionDistance
		{
			get
			{
				return _waterReflectionDistance;
			}
			set
			{
				if (_waterReflectionDistance != value)
				{
					_waterReflectionDistance = value;
					StaticSetRenderFloat("waterReflectionDistance", _waterReflectionDistance);
				}
			}
		}


		[SerializeField]
		Vector3 _godRaysGrid = new Vector3(64, 32, 32);
		public Vector3 GodRaysGrid
		{
			get
			{
				return _godRaysGrid;
			}
			set
			{
				if (_godRaysGrid != value)
				{
					_godRaysGrid = value;

					// To be safe, clamp to [8,64]
					_godRaysGrid.x = Mathf.Clamp((int)_godRaysGrid.x, 8, 64);
					_godRaysGrid.y = Mathf.Clamp((int)_godRaysGrid.y, 8, 64);
					_godRaysGrid.z = Mathf.Clamp((int)_godRaysGrid.z, 8, 64);
					updateERV = true;
				}
			
			}
		}

		[SerializeField]
		float _crepuscularRaysStrength = 1.0f;
		public float CrepuscularRaysStrength
		{
			get
			{
				return _crepuscularRaysStrength;
			}
			set
			{
				if (_crepuscularRaysStrength != value)
				{
					_crepuscularRaysStrength = value;
					//StaticSetRenderFloat("render:crepuscularraysstrength", _crepuscularRaysStrength);
				}
			}
		}


		[SerializeField]
		float _depthSamplingPixelRange = 1.5f;
		public float DepthSamplingPixelRange
		{
			get
			{
				return _depthSamplingPixelRange;
			}
			set
			{
				if (_depthSamplingPixelRange != value)
				{
					_depthSamplingPixelRange = value;
					updateERV = true;
				}
			}
		}

		[SerializeField]
		float _maxSunRadiance = 5000.0f;
		public float MaxSunRadiance
		{
			get
			{
				return _maxSunRadiance;
			}
			set
			{
				if (_maxSunRadiance != value)
				{
					_maxSunRadiance = Mathf.Max(value, 0.0f);
					StaticSetRenderFloat("maxsunradiance", _maxSunRadiance);
				}
			}
		}
		bool _adjustSunRadius = false;
		public bool AdjustSunRadius
		{
			get
			{
				return _adjustSunRadius;
			}
			set
			{
				if (_adjustSunRadius != value)
				{
					_adjustSunRadius = value;
					StaticSetRenderBool("adjustsunradius", _adjustSunRadius);
				}
			}
		}

		[SerializeField]
		int _edgeNoiseFrequency = 4;
		public int EdgeNoiseFrequency
		{
			get
			{
				return _edgeNoiseFrequency;
			}
			set
			{
				if (_edgeNoiseFrequency != value)
				{
					_edgeNoiseFrequency = value;
					StaticSetRenderInt("edgenoisefrequency", _edgeNoiseFrequency);
				}
			}
		}

		[SerializeField]
		int _edgeNoiseOctaves = 3;
		public int EdgeNoiseOctaves
		{
			get
			{
				return _edgeNoiseOctaves;
			}
			set
			{
				if (_edgeNoiseOctaves != value)
				{
					_edgeNoiseOctaves = value;
					StaticSetRenderInt("edgenoiseoctaves", _edgeNoiseOctaves);
				}
			}
		}

		[SerializeField]
		int _edgeNoiseTextureSize = 64;
		public int EdgeNoiseTextureSize
		{
			get
			{
				return _edgeNoiseTextureSize;
			}
			set
			{
				if (_edgeNoiseTextureSize != value)
                {
                    _edgeNoiseTextureSize = value;
					SanitizeSize(ref _edgeNoiseTextureSize, 32, 256);
					updateERV = true;
				}
			}
		}

		// 4.2 only
		[SerializeField]
		int _CellNoiseTextureSize = 64;
		public int CellNoiseTextureSize
		{
			get
			{
				return _CellNoiseTextureSize;
			}
			set
			{
				if (_CellNoiseTextureSize != value)
                {
                    _CellNoiseTextureSize = value;
					SanitizeSize(ref _CellNoiseTextureSize, 32);
                    StaticSetRenderInt("render:cellnoisetexturesize", _CellNoiseTextureSize);
				}
			}
		}

		[SerializeField]
		float _edgeNoisePersistence = 0.63f;
		public float EdgeNoisePersistence
		{
			get
			{
				return _edgeNoisePersistence;
			}
			set
			{
				if (_edgeNoisePersistence != value)
				{
					_edgeNoisePersistence = value;
					//StaticSetRenderFloat("render:EdgeNoisePersistence", _edgeNoisePersistence);
				}
			}
		}

		// 4.2 only
		[SerializeField]
		float _edgeNoiseWavelengthKm = 5.0f;
		public float EdgeNoiseWavelengthKm
		{
			get
			{
				return _edgeNoiseWavelengthKm;
			}
			set
			{
				if (_edgeNoiseWavelengthKm != value)
				{
					_edgeNoiseWavelengthKm = value;
					//StaticSetRenderFloat("render:EdgeNoiseWavelengthKm", _edgeNoiseWavelengthKm);
				}
			}
		}

		// 4.2 only
		[SerializeField]
		int _worleyTextureSize = 64;
		public int WorleyTextureSize
		{
			get
			{
				return _worleyTextureSize;
			}
			set
			{
				if (_worleyTextureSize != value)
				{
					_worleyTextureSize = value;
					updateERV = true;
				}
			}
		}

		// 4.2 only
		[SerializeField]
		float _worleyWavelengthKm = 8.7f;
		public float WorleyWavelengthKm
		{
			get
			{
				return _worleyWavelengthKm;
			}
			set
			{
				if (_worleyWavelengthKm != value)
				{
					_worleyWavelengthKm = value;
					StaticSetRenderFloat("WorleyWavelengthKm", _worleyWavelengthKm);
				}
			}
		}

		//! Set a floating-point property of trueSKY.
		public void SetFloat(string str, float value)
		{
			try
			{
				if (StaticHasRenderFloat(str))
					StaticSetRenderFloat(str, value);
				else
					UnityEngine.Debug.LogError("SetFloat: trueSKY has no float value: " + str);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.LogError(exc.ToString());
			}
		}
		//! Get a floating-point property of trueSKY.
		public float GetFloat(string str)
		{
			float value = 0.0F;
			try
			{
				value = StaticGetRenderFloat(str);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.LogError(exc.ToString());
			}
			return value;
		}

		//! Set a floating-point property of the Sky layer.
		public void SetSkyFloat(string name, float value)
		{
			SetFloat("sky:" + name, value);
		}
		//! Get a floating-point property of the Sky layer.
		public float GetSkyFloat(string name)
		{
			float value = 0.0F;
			try
			{
				value = StaticGetRenderFloat("sky:" + name);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return value;
		}

		//! Set a floating-point property of the 3D cloud layer.
		public void SetCloudFloat(string name, float value)
		{
			SetFloat("clouds:" + name, value);
		}
		//! Get a floating-point property of the 3D cloud layer.
		public float GetCloudFloat(string name)
		{
			float value = 0.0F;
			try
			{
				value = StaticGetRenderFloat("clouds:" + name);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return value;
		}

		//! Set a floating-point property of the 2D cloud layer.
		public void Set2DCloudFloat(string name, float value)
		{
			SetFloat("2DClouds:" + name, value);
		}
		//! Get a floating-point property of the 2D cloud layer.
		public float Get2DCloudFloat(string name)
		{
			float value = 0.0F;
			try
			{
				value = StaticGetRenderFloat("2DClouds:" + name);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return value;
		}

		//! Set an int property of the Sky layer.
		public void SetSkyInt(string name, int value)
		{
			try
			{
				StaticSetRenderInt("sky:" + name, value);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
		}
		//! Get an integer property of the Sky layer.
		public int GetSkyInt(string name)
		{
			int value = 0;
			try
			{
				value = StaticGetRenderInt("sky:" + name);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return value;
		}

		//! Set an integer property of the 3D cloud layer.
		public void SetCloudInt(string name, int value)
		{
			try
			{
				StaticSetRenderInt("Clouds:" + name, value);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
		}
		//! Get an integer property of the 3D cloud layer.
		public int GetCloudInt(string name)
		{
			int value = 0;
			try
			{
				value = StaticGetRenderInt("Clouds:" + name);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return value;
		}

		//! Set an integer property of the 2D cloud layer.
		public void Set2DCloudInt(string name, int value)
		{
			try
			{
				StaticSetRenderInt("2DClouds:" + name, value);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
		}
		//! Get an integer property of the 2D cloud layer.
		public int Get2DCloudInt(string name)
		{
			int value = 0;
			try
			{
				value = StaticGetRenderInt("2DClouds:" + name);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return value;
		}

		//! Sets the storm centre in metres. This method will apply the Metres Per Unit modifier
		public void SetStormCentre(float x, float y)
		{
			int num = GetNumStorms();
			for (int i = 0; i < num; i++)
			{
				uint s = GetStormByIndex(i);
				StaticRenderKeyframeSetFloat(s, "CentreKmx", (x * MetresPerUnit) / 1000.0F);
				StaticRenderKeyframeSetFloat(s, "CentreKmy", (y * MetresPerUnit) / 1000.0F);
			}
		}

		[SerializeField]
		float _trueSKYTime = 12;
		/// <summary>
		/// Time in the sequence, set from some external script, e.g. the sequence editor, or modified per-frame by the speed value.
		/// </summary>
		/// <param name="t"></param>
		public float TrueSKYTime
		{
			get
			{
#if TRUESKY_LOGGING
				Debug.Log("trueSKY get _time " + _time);
#endif
				return _trueSKYTime;
			}
			set
			{
				if (_trueSKYTime != value)
				{
					try
					{


						_trueSKYTime = value;
						Math.Round(_trueSKYTime, 2);
						StaticSetRenderFloat("Time", value / _timeUnits);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
				}
			}
		}
		[SerializeField]
		float _timeProgressionScale = 0.0F;
		/// <summary>
		/// Rate of time in the sequence.
		/// </summary>
		/// <param name="t"></param>
		public float TimeProgressionScale
		{
			get
			{
				return _timeProgressionScale;
			}
			set
			{
				if (_timeProgressionScale != value)
				{
					_timeProgressionScale = value;
				}
			}
		}

		[SerializeField]
		float _timeUnits = 24;
		/// <summary>
		/// Rate of time in the sequence.
		/// </summary>
		/// <param name="t"></param>
		public float TimeUnits
		{
			get
			{
				return _timeUnits;
			}
			set
			{
				if (_timeUnits != value)
				{
					Mathf.Clamp(_timeUnits, 0.1f, 86400);
					_timeUnits = value;
				}
			}
		}
		[SerializeField]
		bool _loop = false;
		public bool Loop
		{
			get
			{
				return _loop;
			}
			set
			{
				if (_loop != value)
				{
					_loop = value;
				}
			}
		}

		[SerializeField]
		float _loopStart = 11.0f;

		public float LoopStart
		{
			get
			{
				return _loopStart;
			}
			set
			{
				if (_loopStart != value)
				{
					_loopStart = value;
				}
			}
		}

		[SerializeField]
		float _loopEnd = 13.0f;
		public float LoopEnd
		{
			get
			{
				return _loopEnd;
			}
			set
			{
				if (_loopEnd != value)
				{
					_loopEnd = value;
				}
			}
		}

		[SerializeField]
		int _interpolationMode = 0;
		public int InterpolationMode
		{
			get
			{
				return _interpolationMode;
			}
			set
			{
				if (_interpolationMode != value) try
					{
						_interpolationMode = value;
						StaticSetRenderInt("interpolationmode", _interpolationMode);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		[SerializeField]
		int _interpolationSubdivisions = 8;
		public int InterpolationSubdivisions
		{
			get
			{
				return _interpolationSubdivisions;
			}
			set
			{
				if (_interpolationSubdivisions != value) try
					{
						_interpolationSubdivisions = Math.Max(1,Math.Min(value,64));
						StaticSetRenderInt("interpolationsubdivisions", _interpolationSubdivisions);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		[SerializeField]
		bool _instantUpdate = true;
		public bool InstantUpdate
		{
			get
			{
				return _instantUpdate;
			}
			set
			{
				if (_instantUpdate != value) try
					{
						_instantUpdate = value;
						StaticSetRenderBool("instantupdate", _instantUpdate);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}


		/// <summary>
		///  This is equivalent to setting trueSky.time, except that time "jumps" to the new value, so cached textures are reset.
		///  This avoid artefacts due to time changing fast in the editor.
		/// </summary>
		/// <param name="value"></param>
		public void JumpToTime(float value)
		{
			_trueSKYTime = value * _timeUnits;
			StaticSetRenderFloat("JumpToTime", value);
		}

		[SerializeField]
		float _HighDetailProportion = 0.2F;
		public float HighDetailProportion
		{
			get
			{
				return _HighDetailProportion;
			}
			set
			{
				if (_HighDetailProportion != value)
				{
					_HighDetailProportion = value;
					updateERV = true;
				}
			}
		}

		[SerializeField]
		float _MediumDetailProportion = 0.4F;
		/// <summary>
		/// Rate of time in the sequence.
		/// </summary>
		/// <param name="t"></param>
		public float MediumDetailProportion
		{
			get
			{
				return _MediumDetailProportion;
			}
			set
			{
				if (_MediumDetailProportion != value)
				{
					_MediumDetailProportion = value;
					updateERV = true;
				}
			}
		}
		[SerializeField]
		float _OriginLatitude = 0.0F;
		/// <summary>
		/// Latitude of the trueSKY object's origin.
		/// </summary>
		public float OriginLatitude
		{
			get
			{
				return _OriginLatitude;
			}
			set
			{
				if (_OriginLatitude != value)
				{
					_OriginLatitude = value;
					Variant[] _Variant = { new Variant() };
					_Variant[0].Vec3.x = _OriginLatitude;
					_Variant[0].Vec3.y = _OriginLongitude;
					_Variant[0].Vec3.z = _OriginHeading;
					StaticSetRender("render:originlatlongheadingdeg", 1, _Variant);
				}
			}
		}
		[SerializeField]
		float _OriginLongitude = 0.0F;
		/// <summary>
		/// Longitude of the trueSKY object's origin.
		/// </summary>
		public float OriginLongitude
		{
			get
			{
				return _OriginLongitude;
			}
			set
			{
				if (_OriginLongitude != value)
				{
					_OriginLongitude = value;
					Variant[] _Variant = { new Variant() };
					_Variant[0].Vec3.x = _OriginLatitude;
					_Variant[0].Vec3.y = _OriginLongitude;
					_Variant[0].Vec3.z = _OriginHeading;
					StaticSetRender("render:originlatlongheadingdeg", 1, _Variant);
				}
			}
		}
		[SerializeField]
		float _OriginHeading = 0.0F;
		/// <summary>
		/// Longitude of the trueSKY object's origin.
		/// </summary>
		public float OriginHeading
		{
			get
			{
				return _OriginHeading;
			}
			set
			{
				if (_OriginHeading != value)
				{
					_OriginHeading = value;
					Variant[] _Variant = { new Variant() };
					_Variant[0].Vec3.x = _OriginLatitude;
					_Variant[0].Vec3.y = _OriginLongitude;
					_Variant[0].Vec3.z = _OriginHeading;
					StaticSetRender("render:originlatlongheadingdeg", 1, _Variant);
				}
			}
		}

		static public bool advancedMode
		{
			get
			{
				string simul = Environment.GetEnvironmentVariable("SIMUL_BUILD");
				return (simul != null && simul.Length > 0);
			}
			set
			{
			}
		}
#if LICENSING
	public string LicenseKey
	{
		get
		{
			return _licenseKey;
		}
		set
		{
			if (_licenseKey != value) try
			 {
					_licenseKey=value;
					StaticSetRenderString("LicenseKey",_licenseKey);
				}
				catch (Exception exc)
				{
					UnityEngine.Debug.Log(exc.ToString());
				}
		}
	}
	[SerializeField]
	string _licenseKey = "";
#endif
		public string GetRenderString(string s)
		{
			StringBuilder str = new StringBuilder("", 20);
			try
			{
				int newlen = StaticGetRenderString(s, str, 16);
				if (newlen > 0)
				{
					str = new StringBuilder("", newlen + 2);
					StaticGetRenderString(s, str, newlen + 1);
				}
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return str.ToString();
		}
		public void SetRenderString(string s, string val)
		{
			try
			{
				StaticSetRenderString(s, val);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
		}

		public int GetInt(string s)
		{
			try
			{
				return StaticGetRenderInt(s);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return 0;
		}
		public void SetInt(string s, int val)
		{
			try
			{
				StaticSetRenderInt(s, val);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
		}
		public bool GetBool(string s)
		{
			try
			{
				return StaticGetRenderBool(s);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return false;
		}
		public void SetBool(string s, bool val)
		{
			try
			{
				StaticSetRenderBool(s, val);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
		}

		public string GetLicenseExpiration()
		{
			StringBuilder str = new StringBuilder("", 20);
			try
			{
				StaticGetRenderString("LicenseExpiration", str, 16);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return str.ToString();
		}
		
		
		static public bool _showCubemaps = false;
		[SerializeField]
		float _cloudShadowing = 0.5F;
		[SerializeField]
		float _cloudShadowSharpness = 0.05F;
		[SerializeField]
		float _cloudThresholdDistanceKm = 1.0F;

		public RenderTexture cloudShadowRT;
		public RenderTexture inscatterRT;
		public RenderTexture lossRT;
		public RenderTexture cloudVisibilityRT;

		RenderTextureHolder _cloudShadowRT = new RenderTextureHolder();	
		RenderTextureHolder _inscatterRT = new RenderTextureHolder();
		RenderTextureHolder _lossRT = new RenderTextureHolder();
		RenderTextureHolder _cloudVisibilityRT = new RenderTextureHolder();
		
		static public bool _showCloudCrossSections = false;
		static public bool _showRainTextures = false;
		static public bool _showAuroraeTextures = false;
		static public bool _showWaterTextures = false;
		
		//bool _simulationTimeRain = false;

		public int trueSKYLayerIndex = 14;

		int _MaxPrecipitationParticles = 100000;

		[SerializeField]
		int _amortization = 2;
		[SerializeField]
		int _atmosphericsAmortization = 2;

		[SerializeField]
		float _depthTemporalAlpha = 0.1f;
		public float DepthTemporalAlpha
		{
			get
			{
				return _depthTemporalAlpha;
			}
			set
			{
				if (_depthTemporalAlpha != value) try
					{
						_depthTemporalAlpha = value;
						updateERV = true;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}


			}
		}
		[SerializeField]
		bool _depthBlending = true;
		public int Amortization
		{
			get
			{
				return _amortization;
			}
			set
			{
				if (_amortization != value) try
                    {
                        _amortization = value;
						updateERV = true;
                        if (SimulVersion < MakeSimulVersion(4, 2))
							StaticSetRenderInt("Amortization", _amortization);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		Color _cloudTint = Color.white;
		public Color CloudTint
		{
			get
			{
				return _cloudTint;
			}
			set
			{
				if (_cloudTint != value) try
					{
						_cloudTint = value;
						//Variant[] _Variant = { new Variant() };
						//_Variant[0].Vec3.x = _cloudTint.r;
						//_Variant[0].Vec3.y = _cloudTint.g;
						//_Variant[0].Vec3.z = _cloudTint.b;
						//StaticSetRender("render:cloudtint", 1, _Variant);

					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

        [SerializeField]
        float _nearCloudExtinctionPerKm = 4.0f;
        public float NearCloudExtinctionPerKm
        {
            get
            {
                return _nearCloudExtinctionPerKm;
            }
            set
            {
                if (_nearCloudExtinctionPerKm != value)
                {
                    _nearCloudExtinctionPerKm = value;
                }
            }
        }
        


        public int AtmosphericsAmortization
		{
			get
			{
				return _atmosphericsAmortization;
			}
			set
			{
				if (_atmosphericsAmortization != value) try
                    {
                        _atmosphericsAmortization = value;
                        updateERV = true;
						if (SimulVersion < MakeSimulVersion(4, 2))
							StaticSetRenderInt("AtmosphericsAmortization", _atmosphericsAmortization);
					}
                    catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		public bool DepthBlending
		{
			get
			{
				return _depthBlending;
			}
			set
			{
				if (_depthBlending != value) try
					{
						_depthBlending = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		//public bool SimulationTimeRain
		//{
		//	get
		//	{
		//		return _simulationTimeRain;
		//	}
		//	set
		//	{
		//		if (_simulationTimeRain != value) try
		//			{
		//				_simulationTimeRain = value;
		//				StaticSetRenderBool("SimulationTimeRain", _simulationTimeRain);
		//			}
		//			catch (Exception exc)
		//			{
		//				UnityEngine.Debug.Log(exc.ToString());
		//			}
		//	}
		//}
		public int MaxPrecipitationParticles
		{
			get
			{
				return _MaxPrecipitationParticles;
			}
			set
			{
				if (_MaxPrecipitationParticles != value) try
					{
						_MaxPrecipitationParticles = value;
						StaticSetRenderInt("render:MaxPrecipitationParticles", _MaxPrecipitationParticles);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		[SerializeField]
		float _PrecipitationRadiusMetres = 6.0F;
		public float PrecipitationRadiusMetres
		{
			get
			{
				return _PrecipitationRadiusMetres;
			}
			set
			{
				if (_PrecipitationRadiusMetres != value) try
					{
						_PrecipitationRadiusMetres = value;
						updateERV = true;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		[SerializeField]
		float _RainFallSpeedMS = 6.0F;
		public float RainFallSpeedMS
		{
			get
			{
				return _RainFallSpeedMS;
			}
			set
			{
				if (_RainFallSpeedMS != value) try
					{
						_RainFallSpeedMS = value;
						StaticSetRenderFloat("render:rainfallspeedms", _RainFallSpeedMS);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		[SerializeField]
		float _SnowFallSpeedMS = 0.5F;
		public float SnowFallSpeedMS
		{
			get
			{
				return _SnowFallSpeedMS;
			}
			set
			{
				if (_SnowFallSpeedMS != value) try
					{
						_SnowFallSpeedMS = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _RainDropSizeMm = 2.5F;
		public float RainDropSizeMm
		{
			get
			{
				return _RainDropSizeMm;
			}
			set
			{
				if (_RainDropSizeMm != value) try
					{
						_RainDropSizeMm = value;
						if (SimulVersion < MakeSimulVersion(4, 2))
							StaticSetRenderFloat("render:raindropsizemm", _RainDropSizeMm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _SnowFlakeSizeMm = 5.0F;
		public float SnowFlakeSizeMm
		{
			get
			{
				return _SnowFlakeSizeMm;
			}
			set
			{
				if (_SnowFlakeSizeMm != value) try
					{
						_SnowFlakeSizeMm = value;
						if(SimulVersion < MakeSimulVersion(4,2))
							StaticSetRenderFloat("render:snowflakesizemm", _SnowFlakeSizeMm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _PrecipitationWindEffect = 0.25F;
		public float PrecipitationWindEffect
		{
			get
			{
				return _PrecipitationWindEffect;
			}
			set
			{
				if (_PrecipitationWindEffect != value) try
					{
						_PrecipitationWindEffect = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _PrecipitationWaver = 0.7F;
		public float PrecipitationWaver
		{
			get
			{
				return _PrecipitationWaver;
			}
			set
			{
				if (_PrecipitationWaver != value) try
					{
						_PrecipitationWaver = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _PrecipitationWaverTimescaleS = 4.0F;
		public float PrecipitationWaverTimescaleS
		{
			get
			{
				return _PrecipitationWaverTimescaleS;
			}
			set
			{
				if (_PrecipitationWaverTimescaleS != value) try
					{
						_PrecipitationWaverTimescaleS = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _PrecipitationThresholdKm = 0.2F;
		public float PrecipitationThresholdKm
		{
			get
			{
				return _PrecipitationThresholdKm;
			}
			set
			{
				if (_PrecipitationThresholdKm != value) try
					{
						_PrecipitationThresholdKm = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _RainNearThreshold = 3.0f;
        public float RainNearThreshold
        {
            get
            {
                return _RainNearThreshold;
            }
            set
            {
                if (RainNearThreshold != value) try
                    {
                        _RainNearThreshold = value;
                        updateERV = true;
                    }
                    catch (Exception exc)
                    {
                        UnityEngine.Debug.Log(exc.ToString());
                    }
            }
        }

		[SerializeField]
		float _VirgaNearThreshold = 20.0f;
		public float VirgaNearThreshold
		{
			get
			{
				return _VirgaNearThreshold;
			}
			set
			{
				if (VirgaNearThreshold != value) try
					{
						_VirgaNearThreshold = value;
						updateERV = true;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		bool _AutomaticRainbowPosition = true;

		public bool AutomaticRainbowPosition
		{
			get
			{
				return _AutomaticRainbowPosition;
			}
			set
			{
				if (_AutomaticRainbowPosition != value) try
					{
						_AutomaticRainbowPosition = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _RainbowElevation = 0.0F;
		public float RainbowElevation
		{
			get
			{
				return _RainbowElevation;
			}
			set
			{
				if (_RainbowElevation != value) try
					{
						_RainbowElevation = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _RainbowAzimuth = 0.0F;
		public float RainbowAzimuth
		{
			get
			{
				return _RainbowAzimuth;
			}
			set
			{
				if (_RainbowAzimuth != value) try
					{
						_RainbowAzimuth = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _RainbowIntensity = 1.0F;
		public float RainbowIntensity
		{
			get
			{
				return _RainbowIntensity;
			}
			set
			{
				if (_RainbowIntensity != value) try
					{
						_RainbowIntensity = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _RainbowDepthPoint = 1.0F;
		public float RainbowDepthPoint
		{
			get
			{
				return _RainbowDepthPoint;
			}
			set
			{
				if (_RainbowDepthPoint != value) try
					{
						_RainbowDepthPoint = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		bool _AllowOccludedRainbows = false;

		public bool AllowOccludedRainbows
		{
			get
			{
				return _AllowOccludedRainbows;
			}
			set
			{
				if (_AllowOccludedRainbows != value) try
					{
						_AllowOccludedRainbows = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		[SerializeField]
		bool _AllowLunarRainbows = true;

		public bool AllowLunarRainbows
		{
			get
			{
				return _AllowLunarRainbows;
			}
			set
			{
				if (_AllowLunarRainbows != value) try
					{
						_AllowLunarRainbows = value;
						
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}


		static public bool _showCompositing = false;

		static public bool _showFades = false;

		static public bool _showCelestials = false;
		[SerializeField]
		static bool _onscreenProfiling = false;
		[SerializeField]
		int _maxCpuProfileLevel = 4;
		[SerializeField]
		int _maxGpuProfileLevel = 4;
		public int maxCpuProfileLevel
		{
			get
			{
				return _maxCpuProfileLevel;
			}
			set
			{
				if (_maxCpuProfileLevel != value) try
					{
						_maxCpuProfileLevel = value;
						StaticSetRenderInt("maxCpuProfileLevel", _maxCpuProfileLevel);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		public int maxGpuProfileLevel
		{
			get
			{
				return _maxGpuProfileLevel;
			}
			set
			{
				if (_maxGpuProfileLevel != value) try
					{
						_maxGpuProfileLevel = value;
						StaticSetRenderInt("maxGpuProfileLevel", _maxGpuProfileLevel);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		public float CloudThresholdDistanceKm
		{
			get
			{
				return _cloudThresholdDistanceKm;
			}
			set
			{
				if (_cloudThresholdDistanceKm != value) try
					{
						_cloudThresholdDistanceKm = value;
						updateERV = true;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		static public bool OnscreenProfiling
		{
			get
			{
				return _onscreenProfiling;
			}
			set
			{
				if (_onscreenProfiling != value) try
					{
						_onscreenProfiling = value;
						StaticSetRenderBool("OnscreenProfiling", _onscreenProfiling);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		static public bool ShowCelestialDisplay
		{
			get
			{
				return _showCelestials;
			}
			set
			{
				if (_showCelestials != value) try
					{
						_showCelestials = value;
						StaticSetRenderBool("ShowCelestialDisplay", _showCelestials);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		static public bool ShowAtmosphericTables
		{
			get
			{
				return _showFades;
			}
			set
			{
				if (_showFades != value) try
					{
						_showFades = value;
						StaticSetRenderBool("ShowFades", _showFades);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		static public bool ShowCompositing
		{
			get
			{
				return _showCompositing;
			}
			set
			{
				if (_showCompositing != value) try
					{
						_showCompositing = value;
						StaticSetRenderBool("ShowCompositing", _showCompositing);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		static public bool ShowCloudCrossSections
		{
			get
			{
				return _showCloudCrossSections;
			}
			set
			{
				if (_showCloudCrossSections != value) try
					{
						_showCloudCrossSections = value;
						StaticSetRenderBool("ShowCloudCrossSections", _showCloudCrossSections);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		static public bool ShowRainTextures
		{
			get
			{
				return _showRainTextures;
			}
			set
			{
				if (_showRainTextures != value) try
					{
						_showRainTextures = value;
						StaticSetRenderBool("ShowRainTextures", _showRainTextures);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		static public bool ShowAuroraeTextures
		{
			get
			{
				return _showAuroraeTextures;
			}
			set
			{
				if (_showAuroraeTextures != value) try
					{
						_showAuroraeTextures = value;
						StaticSetRenderBool("ShowAuroraeTextures", _showAuroraeTextures);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		static public bool ShowWaterTextures
		{
			get
			{
				return _showWaterTextures;
			}
			set
			{
				if (_showWaterTextures != value) try
					{
						_showWaterTextures = value;
						StaticSetRenderBool("ShowWaterTextures", _showWaterTextures);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		static public bool ShowCubemaps
		{
			get
			{
				return _showCubemaps;
			}
			set
			{
				bool v = value;
				if (_showCubemaps != v) try
					{
						_showCubemaps = v;
						StaticSetRenderBool("ShowCubemaps", _showCubemaps);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		public static void RecompileShaders()
		{
			StaticTriggerAction("RecompileShaders");
		}
		public static void CycleCompositingView()
		{
			StaticTriggerAction("CycleCompositingView");
		}
		[SerializeField]
		Texture _backgroundTexture;
		public Texture backgroundTexture
		{
			get
			{
				return _backgroundTexture;
			}
			set
			{
				if (_backgroundTexture != value)
				{
					_backgroundTexture = value;
					if (_backgroundTexture != null)
						StaticSetRenderTexture("Background", _backgroundTexture.GetNativeTexturePtr());
					else
						StaticSetRenderTexture("Background", (System.IntPtr)null);
					Reload();
				}
			}
		}
		[SerializeField]
		Texture _moonTexture;
		public Texture moonTexture
		{
			get
			{
				return _moonTexture;
			}
			set
			{
				if (_moonTexture != value)
				{
					_moonTexture = value;
					if (_moonTexture != null)
						StaticSetRenderTexture("Moon", _moonTexture.GetNativeTexturePtr());
					else
						StaticSetRenderTexture("Moon", (System.IntPtr)null);
					Reload();
				}
			}
		}

		[SerializeField]
		Sequence _sequence;

		public Sequence sequence
		{
			get
			{
				return _sequence;
			}
			set
			{
				if (_sequence != value)
				{
					_sequence = value;
					Reload();
				}
			}
		}
		// The sequence .asset has changed, so we now reload the text in the asset.
		public void Reload()
		{
			if (_sequence == null)
				return;
			try
			{
				StaticSetSequence2(_sequence.SequenceAsText);
				StaticTriggerAction("Reset");
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
		}

		[SerializeField]
		bool _renderSky = true;

		public bool RenderSky
		{
			get
			{
				return _renderSky;
			}
			set
			{
				if (_renderSky != value)
				{
					_renderSky = value;
					updateERV = true;
				}
			}
		}

		[SerializeField]
		int _CloudSteps = 300;
		public int CloudSteps
		{
			get
			{
				return _CloudSteps;
			}
			set
			{
				if (CloudSteps != value) try
					{
						_CloudSteps = value;
						updateERV = true;
						//StaticSetRenderInt("CloudSteps", _CloudSteps);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		[SerializeField]
		int _CubemapResolution = 256;
		public int CubemapResolution
		{
			get
			{
				return _CubemapResolution;
			}
			set
			{
				if (_CubemapResolution != value) try
                    {
                        _CubemapResolution = value;
						SanitizeSize(ref _CubemapResolution);
						updateERV = true;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		Vector3 _WindSpeed = new Vector3(0.0f, 0.0f, 0.0f);
		public Vector3 WindSpeed
		{
			get
			{
				return _WindSpeed;
			}
			set
			{
				if (_WindSpeed != value) try
					{
						_WindSpeed = value;
						Variant[] _Variant = { new Variant() };
						_Variant[0].Vec3.x = _WindSpeed.x;
						_Variant[0].Vec3.y = _WindSpeed.y;
						_Variant[0].Vec3.z = _WindSpeed.z;
						StaticSetRender("render:windspeedms", 1, _Variant);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}

		}

		bool _RealTimeWeatherEffects;
		public bool RealTimeWeatherEffects
		{
			get
			{
				return _RealTimeWeatherEffects;
			}
			set
			{
				if (_RealTimeWeatherEffects != value) try
					{
						_RealTimeWeatherEffects = value;
						updateERV = true;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}

		}

		
		[SerializeField]
		int _IntegrationScheme = 2;
		public int IntegrationScheme
		{
			get
			{
				return _IntegrationScheme;
			}
			set
			{
				if (_IntegrationScheme != value) try
					{
						_IntegrationScheme = value;
						updateERV = true;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		[SerializeField]
		int _LightingMode = 0;
		public int LightingMode
		{
			get
			{
				return _LightingMode;
			}
			set
			{
				if (_LightingMode != value) try
					{
						_LightingMode = value;
						updateERV = true;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _MaxCloudDistanceKm = 400.0f;
		public float MaxCloudDistanceKm
		{
			get
			{
				return _MaxCloudDistanceKm;
			}
			set
			{
				if (_MaxCloudDistanceKm != value) try
					{
						_MaxCloudDistanceKm = value;
						//StaticSetRenderFloat("render:maxclouddistancekm", _MaxCloudDistanceKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _RenderGridXKm = 0.4F;
		public float RenderGridXKm
		{
			get
			{
				return _RenderGridXKm;
			}
			set
			{
				if (_RenderGridXKm != value) try
					{
						_RenderGridXKm = value; //or we set to ERV in here
						updateERV = true;       //StaticSetRenderFloat("render:rendergridxkm", _RenderGridXKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _RenderGridZKm = 0.4F;
		public float RenderGridZKm
		{
			get
			{
				return _RenderGridZKm;
			}
			set
			{
				if (_RenderGridZKm != value) try
					{
						_RenderGridZKm = value;
						updateERV = true;
						//StaticSetRenderFloat("render:rendergridzkm", _RenderGridZKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		[SerializeField]
		int _windowGridWidth = 512;
		public int WindowGridWidth
		{
			get
			{
				return _windowGridWidth;
			}
			set
			{
				if (_windowGridWidth != value) try
					{
						_windowGridWidth = value;
						SanitizeSize(ref _windowGridWidth);
						updateERV = true;
						//StaticSetRenderFloat("render:rendergridzkm", _RenderGridZKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		int _windowGridHeight = 32;
		public int WindowGridHeight
		{
			get
			{
				return _windowGridHeight;
			}
			set
			{
				if (_windowGridHeight != value) try
                    {
                        _windowGridHeight = value;
						SanitizeSize(ref _windowGridHeight, 8, 64);
						updateERV = true;
						//StaticSetRenderFloat("render:rendergridzkm", _RenderGridZKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}


		[SerializeField]
		int _windowWidthKm = 800;
		public int WindowWidthKm
		{
			get
			{
				return _windowWidthKm;
			}
			set
			{
				if (_windowWidthKm != value) try
					{
						_windowWidthKm = value;
						updateERV = true;
						//StaticSetRenderFloat("render:rendergridzkm", _RenderGridZKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		int _windowHeightKm = 10;
		public int WindowHeightKm
		{
			get
			{
				return _windowHeightKm;
			}
			set
			{
				if (_windowHeightKm != value) try
					{
						_windowHeightKm = value;
						updateERV = true;
						//StaticSetRenderFloat("render:rendergridzkm", _RenderGridZKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}




		[SerializeField]
		float _MaxFractalAmplitudeKm = 3.0F;
		public float MaxFractalAmplitudeKm
		{
			get
			{
				return _MaxFractalAmplitudeKm;
			}
			set
			{
				if (_MaxFractalAmplitudeKm != value) try
					{
						_MaxFractalAmplitudeKm = value;
						if (SimulVersion < MakeSimulVersion(4, 2))
							StaticSetRenderFloat("render:maxfractalamplitudekm", _MaxFractalAmplitudeKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _CellNoiseWavelengthKm = 8.7F;
		public float CellNoiseWavelengthKm
		{
			get
			{
				return _CellNoiseWavelengthKm;
			}
			set
			{
				if (_CellNoiseWavelengthKm != value) try
					{
						_CellNoiseWavelengthKm = value;
						if (SimulVersion < MakeSimulVersion(4, 2))
							StaticSetRenderFloat("WorleyWavelengthKm", _CellNoiseWavelengthKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _DirectLight = 1.0F;
		public float DirectLight
		{
			get
			{
				return _DirectLight;
			}
			set
			{
				if (_DirectLight != value) try
                    {
                        _DirectLight = value;
						if (SimulVersion < MakeSimulVersion(4, 2))
							StaticSetRenderFloat("render:directlight", _DirectLight);
					}
                    catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _IndirectLight = 1.0F;
		public float IndirectLight
		{
			get
			{
				return _IndirectLight;
			}
			set
			{
				if (_IndirectLight != value) try
                    {
                        _IndirectLight = value;
						if (SimulVersion < MakeSimulVersion(4, 2))
							StaticSetRenderFloat("render:indirectlight", _IndirectLight);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _AmbientLight = 1.0F;
		public float AmbientLight
		{
			get
			{
				return _AmbientLight;
			}
			set
			{
				if (_AmbientLight != value) try
                    {
                        _AmbientLight = value;
                        if (SimulVersion < MakeSimulVersion(4, 2))
							StaticSetRenderFloat("render:ambientlight", _AmbientLight);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _Extinction = 4.0F;
		public float Extinction
		{
			get
			{
				return _Extinction;
			}
			set
			{
				if (_Extinction != value) try
					{
						_Extinction = value;
						if(SimulVersion < MakeSimulVersion(4,2))
							StaticSetRenderFloat("render:extinction", _Extinction);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		float _MieAsymmetry = 0.87F;
		public float MieAsymmetry
		{
			get
			{
				return _MieAsymmetry;
			}
			set
			{
				if (_MieAsymmetry != value) try
					{
						_MieAsymmetry = value;
						StaticSetRenderFloat("render:mieasymmetry", _MieAsymmetry);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		int _shadowTextureRes = 256;
		public int ShadowTextureRes
		{
			get
			{
				return _shadowTextureRes;
			}
			set
			{
				if (_shadowTextureRes != value) try
                    {
                        _shadowTextureRes = value;
						SanitizeSize(ref _shadowTextureRes);
                        updateERV = true;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		//[SerializeField]
		//int _cloudShadowResolution = 256;
		//public int CloudShadowResolution
		//{
		//	get
		//	{
		//		return _cloudShadowResolution;
		//	}
		//	set
		//	{
		//		if (_cloudShadowResolution != value) try
  //                  {
  //                      _cloudShadowResolution = value;
		//				SanitizeSize(ref _cloudShadowResolution);
  //                      StaticSetRenderInt("cloudshadowresolution", _cloudShadowResolution);
		//			}
		//			catch (Exception exc)
		//			{
		//				UnityEngine.Debug.Log(exc.ToString());
		//			}
		//	}
		//}

		public RenderTextureHolder CloudShadowTexture
		{
			get
			{
				return _cloudShadowRT;
			}
			set
			{
				if (_cloudShadowRT != null) try
					{
						_cloudShadowRT = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		public RenderTextureHolder LossTexture
		{
			get
			{
				return _lossRT;
			}
			set
			{
				if (_lossRT != null) try
					{
						_lossRT = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		public RenderTextureHolder InscatterTexture
		{
			get
			{
				return _inscatterRT;
			}
			set
			{
				if (_inscatterRT != null) try
					{
						_inscatterRT = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		public RenderTextureHolder CloudVisibilityTexture
		{
			get
			{
				return _cloudVisibilityRT;
			}
			set
			{
				if (_cloudVisibilityRT != null) try
					{
						_cloudVisibilityRT = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		int _cloudShadowRangeKm = 300;
        public int CloudShadowRangeKm
        {
            get
            {
                return _cloudShadowRangeKm;
            }
            set
            {
                if (_cloudShadowRangeKm != value) try
                    {
                        _cloudShadowRangeKm = value;
                    }
                    catch (Exception exc)
                    {
                        UnityEngine.Debug.Log(exc.ToString());
                    }
            }
        }

		[SerializeField]
		int _highDetailMultiplier = 1;
        public int HighDetailMultiplier
        {
            get
            {
                return _highDetailMultiplier;
            }
            set
            {
                if (_highDetailMultiplier != value) try
                    {
						updateERV = true;
						_highDetailMultiplier = value;
                    }
                    catch (Exception exc)
                    {
                        UnityEngine.Debug.Log(exc.ToString());
                    }
            }
        }

		[SerializeField]
		float _highDetailRangeKm = 100.0f;
		public float HighDetailRangeKm
		{
			get
			{
				return _highDetailRangeKm;
			}
			set
			{
				if (_highDetailRangeKm != value) try
					{
						updateERV = true;
						_highDetailRangeKm = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}



		[SerializeField]
		RenderPipelineAsset _HDRP_RenderPipelineAsset = null;
		public RenderPipelineAsset HDRP_RenderPipelineAsset
		{
			get
			{
				return _HDRP_RenderPipelineAsset;
			}
			set
			{
				if (_HDRP_RenderPipelineAsset != value) try
					{
						_HDRP_RenderPipelineAsset = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		bool _LoadRenderPipelineAsset = false;
		public bool LoadRenderPipelineAsset
		{
			get
			{
				return _LoadRenderPipelineAsset;
			}
			set
			{
				if (_LoadRenderPipelineAsset != value) try
					{
						_LoadRenderPipelineAsset = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		[SerializeField]
		bool _UsingIL2CPP = false;
		public bool UsingIL2CPP
		{
#if UNITY_GAMECORE || UNITY_PS4 || UNITY_PS5 || UNITY_SWITCH
			get { _UsingIL2CPP = true;  return _UsingIL2CPP; }
			set { _UsingIL2CPP = true; }
#else
			get
			{
				return _UsingIL2CPP;
			}
			set
			{
				if (_UsingIL2CPP != value) try
					{
						_UsingIL2CPP = value;
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
#endif
		}

		//Returns true sizes do not match.
		private bool CheckSizeOfExternalRenderValues()
		{
			if (SimulVersion < MakeSimulVersion(4, 2))
				return false; //4.1 does not supprt ERV
			string str= "sizeof:ExternalRenderValues";
			int dllSize = StaticGetRenderInt(str);
			int thisSize = Marshal.SizeOf(typeof(ExternalRenderValues));
			bool wrong = (dllSize != thisSize) ? true : false;
			if (wrong)
			{
				UnityEngine.Debug.LogError("Struct sizes do not match for " + str + ". DLL size is " + dllSize.ToString() + ", EXE size is " + thisSize.ToString() + ". Please check your trueSKY version and/or update the trueSKY DLLs.");
			}
			return wrong;
		}

		//Returns true sizes do not match.
		private bool CheckSizeOfExternalDynamicValues()
		{
			if (SimulVersion < MakeSimulVersion(4, 2))
				return false; //4.1 does not supprt EDV
			string str = "sizeof:ExternalDynamicValues";
			int dllSize = StaticGetRenderInt(str);
			int thisSize = Marshal.SizeOf(typeof(ExternalDynamicValues));
			bool wrong = (dllSize != thisSize) ? true : false;
			if (wrong)
			{
				UnityEngine.Debug.LogError("Struct sizes do not match for " + str + ". DLL size is " + dllSize.ToString() + ", EXE size is " + thisSize.ToString() + ". Please check your trueSKY version and/or update the trueSKY DLLs.");
			}
			return wrong;
		}

		System.IntPtr Moonptr = Marshal.AllocHGlobal(Marshal.SizeOf(new ExternalMoon()));

        ExternalRenderValues ERV = new ExternalRenderValues();
		System.IntPtr ERVptr = Marshal.AllocHGlobal(Marshal.SizeOf(new ExternalRenderValues()));

		ExternalDynamicValues EDV = new ExternalDynamicValues();
		System.IntPtr EDVptr = Marshal.AllocHGlobal(Marshal.SizeOf(new ExternalDynamicValues()));

		public bool updateERV = true;

		public void UpdateExternalRender()
		{
			if (SimulVersion >= MakeSimulVersion(4, 2))
			{
				if (CheckSizeOfExternalRenderValues())
					return;

				ERV.version = ExternalRenderValues.static_version;
				ERV.RenderSky = Convert.ToUInt32(_renderSky);
				ERV.IntegrationScheme = _IntegrationScheme;
				ERV.LightingMode = _LightingMode;
				ERV.RenderGridXKm = _RenderGridXKm;
				ERV.RenderGridZKm = _RenderGridZKm;
				ERV.WindowGridWidth = _windowGridWidth;
				ERV.WindowGridHeight = _windowGridHeight;
				ERV.WindowWidthKm = _windowWidthKm;
				ERV.WindowHeightKm = _windowHeightKm;
				ERV.MaximumCubemapResolution = _CubemapResolution;
				ERV.DefaultNumSlices = _CloudSteps;
				ERV.DepthSamplingPixelRange = _depthSamplingPixelRange;
				ERV.EdgeNoiseTextureSize = _edgeNoiseTextureSize;
				ERV.Godrays_x = (uint)_godRaysGrid.x;
				ERV.Godrays_y = (uint)_godRaysGrid.y;
				ERV.Godrays_z = (uint)_godRaysGrid.z;
				ERV.HighDetailProportion = _HighDetailProportion;
				ERV.MediumDetailProportion = _MediumDetailProportion;
				ERV.WorleyTextureSize = _worleyTextureSize;
				ERV.AtmosphericsAmortization = _atmosphericsAmortization;
				ERV.CloudDepthTemporalAlpha = _depthTemporalAlpha;
				ERV.CloudThresholdDistanceKm = _cloudThresholdDistanceKm;
				ERV.DefaultAmortization = _amortization;
				ERV.MaxFramesBetweenViewUpdates = 100;
				ERV.PrecipitationRadiusMetres = _PrecipitationRadiusMetres;
				ERV.ShadowTextureSize = _shadowTextureRes;
                ERV.RainNearThreshold = _RainNearThreshold;
				ERV.VirgaNearThresholdKm = _VirgaNearThreshold;
                ERV.MaximumStarMagnitude = _maximumStarMagniute;
				ERV.RealTimeWeatherEffects = Convert.ToUInt32(_RealTimeWeatherEffects);
				ERV.HighDetailMultiplier = (uint)_highDetailMultiplier;
				ERV.HighDetailRangeKm = _highDetailRangeKm;

				Marshal.StructureToPtr(ERV, ERVptr, !GetTrueSky().UsingIL2CPP);
				StaticSetExternalRenderValues(ERVptr);
				//StaticTriggerAction("Reset");
			}
		}

		public void UpdateExternalDynamic()
		{
			if (SimulVersion >= MakeSimulVersion(4, 2))
			{
			if (CheckSizeOfExternalDynamicValues())
				return;

			EDV.version = ExternalDynamicValues.static_version;
			EDV.AdjustSunRadius = Convert.ToUInt32(_adjustSunRadius);
			EDV.AllowLunarRainbow = Convert.ToUInt32(_AllowLunarRainbows);
			EDV.AllowOccludedRainbow = Convert.ToUInt32(_AllowOccludedRainbows);
			EDV.AmbientLight = _AmbientLight;
			EDV.AutomaticRainbowPosition = Convert.ToUInt32(_AutomaticRainbowPosition);
			EDV.CellNoiseWavelengthKm = _CellNoiseWavelengthKm;
			EDV.CloudShadowRangeKm = _cloudShadowRangeKm;
			//EDV.CloudShadowStrength = _cloudShadowStrength;
			EDV.CosmicBackgroundBrightness = _backgroundBrightness;
			EDV.CrepuscularRayStrength = _crepuscularRaysStrength;
			EDV.DirectLight = _DirectLight;
			EDV.EdgeNoiseFrequency = _edgeNoiseFrequency;
			EDV.EdgeNoisePersistence = _edgeNoisePersistence;
			EDV.EdgeNoiseWavelengthKm = _edgeNoiseWavelengthKm;
			EDV.Extinction = _Extinction;
			EDV.IndirectLight = _IndirectLight;
			EDV.MaxCloudDistanceKm = _MaxCloudDistanceKm;
			EDV.MaxFractalAmplitudeKm = _MaxFractalAmplitudeKm;
			EDV.MaxPrecipitationParticles = _MaxPrecipitationParticles;
			EDV.MaxSunRadiance = _maxSunRadiance;
			EDV.MieAsymmetry = _MieAsymmetry;
			EDV.MinimumStarPixelSize = _minimumStarPixelSize;
			EDV.OriginHeading = _OriginHeading;
			EDV.OriginLatitude = _OriginLatitude;
			EDV.OriginLongitude = _OriginLongitude;
			EDV.PrecipitationThresholdKm = _PrecipitationThresholdKm;
			EDV.PrecipitationWaver = _PrecipitationWaver;
			EDV.PrecipitationWaverTimescaleS = _PrecipitationWaverTimescaleS;
			EDV.PrecipitationWindEffect = _PrecipitationWindEffect;
			EDV.RainbowAzimuth = _RainbowAzimuth;
			EDV.RainbowDepthPoint = _RainbowDepthPoint;
			EDV.RainbowElevation = _RainbowElevation;
			EDV.RainbowIntensity = _RainbowIntensity;
			EDV.RainDropSizeMm = _RainDropSizeMm;
			EDV.RainFallSpeedMS = _RainFallSpeedMS;
			EDV.SnowFallSpeedMS = _SnowFallSpeedMS;
			EDV.SnowFlakeSizeMm = _SnowFlakeSizeMm;
			EDV.StarBrightness = _starBrightness;
			EDV.WindSpeedMS_X = _WindSpeed.x;
			EDV.WindSpeedMS_Y = _WindSpeed.y;
			EDV.WindSpeedMS_Z = _WindSpeed.z;
			EDV.CloudTintR = _cloudTint.r;
			EDV.CloudTintG = _cloudTint.g;
			EDV.CloudTintB = _cloudTint.b;

			EDV.GeomagneticNorthPoleLatitude = aurorae.GeomagneticNorthPoleLatitude;
			EDV.GeomagneticNorthPoleLongitude = aurorae.GeomagneticNorthPoleLongitude;
			EDV.HighestLatitude = aurorae.HighestLatitude;
			EDV.LowestLatitude = aurorae.LowestLatitude;
			EDV.MaxBand = aurorae.MaxBand;
			EDV.MinBand = aurorae.MinBand;
			EDV.ShowAuroralOvalInCloudWindow = Convert.ToUInt32(aurorae.ShowAuroralOvalInCloudWindow);
			EDV.AuroraElectronFreeTime = aurorae.AuroraElectronFreeTime * 1e-12f;
			EDV.AuroraElectronVolumeDensity = aurorae.AuroraElectronVolumeDensity * 1e13f;
			EDV.AuroralLayersIntensity = aurorae.AuroralLayersIntensity;
			EDV.AuroraLayers = aurorae.GetAuroralLayerVec4Array();
			EDV.AuroraLayerCount = (UInt64)aurorae.GetAuroralLayerCount();
			EDV.Start_Dawn1 = aurorae.Start_Dawn1;
			EDV.End_Dawn1 = aurorae.End_Dawn1;
			EDV.Radius_Dawn1 = aurorae.Radius_Dawn1;
			EDV.OriginLatitude_Dawn1 = aurorae.OriginLatitude_Dawn1;
			EDV.OriginLongitude_Dawn1 = aurorae.OriginLongitude_Dawn1;
			EDV.Start_Dusk1 = aurorae.Start_Dusk1;
			EDV.End_Dusk1 = aurorae.End_Dusk1;
			EDV.Radius_Dusk1 = aurorae.Radius_Dusk1;
			EDV.OriginLatitude_Dusk1 = aurorae.OriginLatitude_Dusk1;
			EDV.OriginLongitude_Dusk1 = aurorae.OriginLongitude_Dusk1;
			EDV.Start_Dawn2 = aurorae.Start_Dawn2;
			EDV.End_Dawn2 = aurorae.End_Dawn2;
			EDV.Radius_Dawn2 = aurorae.Radius_Dawn2;
			EDV.OriginLatitude_Dawn2 = aurorae.OriginLatitude_Dawn2;
			EDV.OriginLongitude_Dawn2 = aurorae.OriginLongitude_Dawn2;
			EDV.Start_Dusk2 = aurorae.Start_Dusk2;
			EDV.End_Dusk2 = aurorae.End_Dusk2;
			EDV.Radius_Dusk2 = aurorae.Radius_Dusk2;
			EDV.OriginLatitude_Dusk2 = aurorae.OriginLatitude_Dusk2;
			EDV.OriginLongitude_Dusk2 = aurorae.OriginLongitude_Dusk2;
			EDV.AuroraIntensityMapSize = aurorae.AuroraIntensityMapSize;
			EDV.AuroraTraceLength = aurorae.AuroraTraceLength;
			EDV.NearCloudExtinctionPerKm = _nearCloudExtinctionPerKm;

			Marshal.StructureToPtr(EDV, EDVptr, !GetTrueSky().UsingIL2CPP);
			StaticSetExternalDynamicValues(EDVptr);
		}
		}

		bool _initialized = false;
		bool _rendering_initialized = false;
		bool isApplicationPlaying = false;
		void Update()
		{
			if (GraphicsSettings.renderPipelineAsset != HDRP_RenderPipelineAsset && LoadRenderPipelineAsset)
				GraphicsSettings.renderPipelineAsset = HDRP_RenderPipelineAsset;
			try
			{
				if (!_initialized)
					Init();
				if (Application.isPlaying)
				{
					if (!isApplicationPlaying)
					{
						StaticTriggerAction("Reset");
						isApplicationPlaying = true;
					}
					_trueSKYTime += Time.deltaTime * (_timeProgressionScale / (24.0F * 60.0F * 60.0F * _timeUnits));
					Variant[] _Variant = { new Variant() }; //temp fix.
					_Variant[0].Vec3.x = _OriginLatitude;
					_Variant[0].Vec3.y = _OriginLongitude;
					_Variant[0].Vec3.z = _OriginHeading;
					StaticSetRender("render:originlatlongheadingdeg", 1, _Variant);

				}
				else
					isApplicationPlaying = false;

				UpdateTime();

				StaticSetRenderFloat("RealTime", Time.time);

				if (updateERV)
				{
					if (SimulVersion >= MakeSimulVersion(4, 2))
					{
						UpdateExternalRender();
						updateERV = false;
					}
					else
					{
						StaticSetRenderBool("RenderSky", _renderSky);
						StaticSetRenderFloat("render:rendergridxkm", _RenderGridXKm);
						StaticSetRenderFloat("render:rendergridzkm", _RenderGridZKm);
						StaticSetRenderInt("MaximumCubemapResolution", _CubemapResolution);
						StaticSetRenderBool("gridrendering", _IntegrationScheme == 0);
						StaticSetRenderInt("CloudSteps", _CloudSteps);
						StaticSetRenderFloat("CloudThresholdDistanceKm", _cloudThresholdDistanceKm);
						StaticSetRenderFloat("depthsamplingpixelrange", _depthSamplingPixelRange);
						StaticSetRenderInt("render:edgenoisetexturesize", _edgeNoiseTextureSize);
						StaticSetRenderInt("godraysgrid.x", (int)_godRaysGrid.x);
						StaticSetRenderInt("godraysgrid.y", (int)_godRaysGrid.y);
						StaticSetRenderInt("godraysgrid.z", (int)_godRaysGrid.z);
						StaticSetRenderFloat("render:highdetailproportion", _HighDetailProportion);
						StaticSetRenderFloat("render:mediumdetailproportion", _MediumDetailProportion);
						StaticSetRenderInt("render:CellNoiseTextureSize", _worleyTextureSize);
						StaticSetRenderInt("render:AtmosphericsAmortization", _atmosphericsAmortization);
						StaticSetRenderInt("render:Amortization", _amortization);
						StaticSetRenderFloat("render:precipitationradiusmetres", _PrecipitationRadiusMetres);
						StaticSetRenderFloat("render:maxclouddistancekm", _MaxCloudDistanceKm);
						StaticSetRenderFloat("render:precipitationthresholdkm", _PrecipitationThresholdKm);
					}
				}
				if (SimulVersion >= MakeSimulVersion(4, 2))
				{
					UpdateExternalDynamic();
	
					foreach(var moon in _moons)
					{
						if (moon.Render && !moon.DestroyMoon)
						{
							ExternalMoon Moon = new ExternalMoon();
							Moon.version = ExternalMoon.static_version;
							Moon.orbit = moon.GetOrbit();
							Moon.name = moon.Name;
							Moon.radiusArcMinutes = (float)moon.RadiusArcMinutes;
							Moon.render = true;
							ExternalTexture tex = new ExternalTexture();
							tex.version = ExternalTexture.static_version;
							InitExternalTexture(ref tex, moon.MoonTexture);
							Moon.colour.x = moon.Colour.r;
							Moon.colour.y = moon.Colour.g;
							Moon.colour.z = moon.Colour.b;
							Moon.albedo = (float)moon.Albedo;					
							Marshal.StructureToPtr(Moon, Moonptr, !GetTrueSky().UsingIL2CPP); 
							StaticSetMoon(_moons.IndexOf(moon) + 1, Moonptr);
						}
						else
						{ 
							StaticSetMoon(_moons.IndexOf(moon) + 1, (System.IntPtr)null);
							if(moon.DestroyMoon)
								_moons.Remove(moon);
						}
					}
				}
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
		}

		/// <summary>
		/// Sun colour is given as a vector because Color class is clamped to [0,1] and irradiance can have arbitrary magnitude.
		/// </summary>
		/// <returns>Vector3</returns>


		public void UpdateTime()
		{
			if (Application.isPlaying)
			{
				if (Loop)
				{
					if (TrueSKYTime > LoopEnd)
						TrueSKYTime = LoopStart;
					else if (TrueSKYTime < LoopStart)
						TrueSKYTime = LoopStart;
				}

				//Allowing for personalised units of time (Day is 0-1, 0-24 or 0-100 etc.)
				if (TimeProgressionScale != 0)
					TrueSKYTime += (((TimeProgressionScale / (24.0F * 60.0F * 60.0F)) * TimeUnits) * Time.deltaTime);

			StaticSetRenderFloat("Time", _trueSKYTime/TimeUnits);
		}
		}
		public Vector3 getSunColour(Vector3 pos,int id=0)
		{
			if (!_initialized)
				Init();
			Vector3 convertedPos = UnityToTrueSkyPosition(pos);
			LightingQueryResult q = LightingQuery( id+(int)234965824, convertedPos);
			Vector3 c=new Vector3(0,0,0);
			try
			{
				c.x = q.sunlight.x;
				c.y = q.sunlight.y;
				c.z = q.sunlight.z;
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return c;
		}

		/// <summary>
		/// Moon colour is given as a vector because Color class is clamped to [0,1] and irradiance can have arbitrary magnitude.
		/// </summary>
		/// <returns>Vector3</returns>
		public Vector3 getMoonColour(Vector3 pos, int id = 0)
		{
			if (!_initialized)
				Init();
			Vector3 convertedPos = UnityToTrueSkyPosition(pos);
			LightingQueryResult q = LightingQuery(id + (int)12849757, convertedPos);
			Vector3 c = new Vector3(0, 0, 0);
			try
			{
				c.x = q.moonlight.x;
				c.y = q.moonlight.y;
				c.z = q.moonlight.z;
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return c;
		}

		/// <summary>
		/// Ambient colour is given as a vector because Color class is clamped to [0,1] and radiance can have arbitrary magnitude.
		/// </summary>
		/// <returns>Vector3</returns>AmbientRadianceRed
		public Vector3 getAmbientColour()
		{
			if (!_initialized)
				Init();
			Vector3 c = new Vector3(0, 0, 0);
			try
			{
				float r = StaticGetRenderFloat("AmbientRadianceRed");
				float g = StaticGetRenderFloat("AmbientRadianceGreen");
				float b = StaticGetRenderFloat("AmbientRadianceBlue");
				c.x = r;
				c.y = g;
				c.z = b;
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return c;
		}

		public float getCloudShadowScale()
		{
			if (!_initialized)
				Init();
			try
			{
				float r = StaticGetRenderFloat("cloudshadowscale.x");
				float metresPerUnit = trueSKY.GetTrueSky().MetresPerUnit;
				return 1000.0F * r/ metresPerUnit;
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return 1.0F;
		}

		public Vector3 getCloudShadowCentre()
		{
			if (!_initialized)
				Init();
			Vector3 pos = new Vector3(0, 0, 0);
			Vector3 c = new Vector3(0, 0, 0);
			try
			{
				c.x = StaticGetRenderFloat("cloudshadoworigin.X");
				c.y = StaticGetRenderFloat("cloudshadoworigin.Y");
				c.z = StaticGetRenderFloat("cloudshadoworigin.z");
				pos = TrueSkyToUnityPosition(c);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return pos;
		}


		public void setCloudShadowCentre(Vector3 pos)
		{
			if (!_initialized)
				Init();

			try
			{
				Vector3 position = UnityToTrueSkyPosition(pos);
				StaticSetRenderFloat("cloudshadoworigin.X", position.x);
				StaticSetRenderFloat("cloudshadoworigin.Y", position.y);
				StaticSetRenderFloat("cloudshadoworigin.z", position.z);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
		}

		/// <summary>
		/// Returns the rotation of the sun as a Quaternion, for Directional Light objects.
		/// </summary>
		/// <returns></returns>
		public Quaternion getSunRotation()
		{
			float az = 0.0F, el = 0.0F;
			try
			{
				az = StaticGetRenderFloat("SunAzimuthDegrees");
				el = StaticGetRenderFloat("SunElevationDegrees");
			}
			catch (Exception exc)
			{
				_initialized = false;
				UnityEngine.Debug.Log(exc.ToString());
			}
			Quaternion q=Quaternion.Euler(el,az+180.0F,0.0F);
			return q;
		}

		/// <summary>
		/// Returns the rotation of the moon as a Quaternion, for Directional Light objects.
		/// </summary>
		/// <returns></returns>
		public Quaternion getMoonRotation()
		{
			float az = 0.0F, el = 0.0F;
			try
			{
				az = StaticGetRenderFloat("MoonAzimuthDegrees");
				el = StaticGetRenderFloat("MoonElevationDegrees");
			}
			catch (Exception exc)
			{
				_initialized = false;
				UnityEngine.Debug.Log(exc.ToString());
			}
			Quaternion q=Quaternion.Euler(el,az+180.0F,0.0F);
			return q;
		} 

		void Awake()
		{
			if (trueSkySingleton != null)
			{
				UnityEngine.Debug.LogError("Only one trueSKY object should be instantiated.");
			}
			else
			{
				trueSkySingleton = this;
			}
		}
		public static string GetShaderbinSourceDir(string target)
		{
			char s = Path.DirectorySeparatorChar;
			string assetsPath = Environment.CurrentDirectory + s + "Assets";
			string simul = assetsPath + s + "Simul";
			// Custom shader binary folder
			string shaderFolderSrt;
			shaderFolderSrt = "shaderbin"+s+ target;
			string shaderbinSource = simul + s + shaderFolderSrt;
			return shaderbinSource;
		}

		void Init()
		{
			try
			{
				if (_initialized)
					return;
				float savedTime = _trueSKYTime; 
				_initialized = true;

#if TRUESKY_LOGGING
				Debug.Log("trueSKY time restored from Unity scene as " + savedTime);
#endif

				SimulImports.Init();

				// Get Simul version
				IntPtr ma = Marshal.AllocHGlobal(sizeof(int));
				IntPtr mi = Marshal.AllocHGlobal(sizeof(int));
				IntPtr bu = Marshal.AllocHGlobal(sizeof(int));
				GetSimulVersion(ma, mi, bu);
				SimulVersionMajor = Marshal.ReadInt32(ma);
				SimulVersionMinor = Marshal.ReadInt32(mi);
				SimulVersionBuild = Marshal.ReadInt32(bu);

				UnityEngine.Debug.Log("trueSKY version:" + SimulVersionMajor + "." + SimulVersionMinor + "." + SimulVersionBuild);

#if TRUESKY_LOGGING
				StaticEnableLogging("trueSKYUnityRender.log");
#endif

				// Push the shader and texture paths:
				if(!Application.isEditor)
				{
#if UNITY_PS4
					StaticPushPath("ShaderBinaryPath", Application.streamingAssetsPath + @"/Simul/shaderbin/ps4");
#elif UNITY_PS5
					StaticPushPath("ShaderBinaryPath", Application.streamingAssetsPath + @"/Simul/shaderbin/ps5");
#elif UNITY_SWITCH
					StaticPushPath("ShaderBinaryPath", Application.streamingAssetsPath + @"/Simul/shaderbin/nx");
					StaticPushPath("TexturePath", Application.streamingAssetsPath + @"/Simul/Media/textures");
#elif UNITY_WSA || UNITY_STANDALONE_WIN
				   if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11)
						StaticPushPath("ShaderBinaryPath", Application.dataPath + @"/Simul/shaderbin/x86_64/d3d11");
					else if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D12)
						StaticPushPath("ShaderBinaryPath", Application.dataPath + @"/Simul/shaderbin/x86_64/d3d12");
					else if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Vulkan)
						StaticPushPath("ShaderBinaryPath", Application.dataPath + @"/Simul/shaderbin/x86_64/vulkan");
					else
						StaticPushPath("ShaderBinaryPath", Application.dataPath + @"/Simul/shaderbin/x86_64");
#endif
					StaticPushPath("TexturePath", Application.dataPath + @"/Simul/Media/Textures");
#if UNITY_GAMECORE
					if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.GameCoreXboxSeries 
						|| SystemInfo.graphicsDeviceType == GraphicsDeviceType.GameCoreXboxOne)
					{
						StaticPushPath("ShaderBinaryPath", "");
						StaticPushPath("ShaderBinaryPath", "D3D12");
						StaticPushPath("ShaderPath", "");
						StaticPushPath("ShaderPath", "D3D12");
					}
#endif
				}
				else
				{
					if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11)
					{
						StaticPushPath("ShaderBinaryPath", Application.dataPath + @"/Simul/shaderbin/x86_64/d3d11");
						StaticPushPath("ShaderPath", Application.dataPath + @"/Simul/shaderbin/x86_64/d3d11");
					}
					else if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12)
					{
						StaticPushPath("ShaderBinaryPath", Application.dataPath + @"/Simul/shaderbin/x86_64/d3d12");
						StaticPushPath("ShaderPath", Application.dataPath + @"/Simul/shaderbin/x86_64/d3d12");
					}
					else if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
					{
						StaticPushPath("ShaderBinaryPath", Application.dataPath + @"/Simul/shaderbin/x86_64/vulkan");
						StaticPushPath("ShaderPath", Application.dataPath + @"/Simul/shaderbin/x86_64/vulkan");
					}
#if UNITY_GAMECORE
					else if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.GameCoreXboxSeries
						|| SystemInfo.graphicsDeviceType == GraphicsDeviceType.GameCoreXboxOne)
					{
						StaticPushPath("ShaderBinaryPath", "");
						StaticPushPath("ShaderBinaryPath", "D3D12");
						StaticPushPath("ShaderPath", "");
						StaticPushPath("ShaderPath", "D3D12");
					}
#endif
					else
					{
						StaticPushPath("ShaderBinaryPath", Application.dataPath + @"/Simul/shaderbin/x86_64");
						StaticPushPath("ShaderPath", Application.dataPath + @"/Simul/shaderbin/x86_64");
					}
					StaticPushPath("TexturePath", Application.dataPath + @"/Simul/Media/Textures");
				}

				StaticInitInterface();
				Reload();

#if TRUESKY_LOGGING
			float t=StaticGetRenderFloat("time");
			Debug.Log("trueSKY initial time from sequence " + t);
			Debug.Log("savedTime " + savedTime);
#endif

				TrueSKYTime = savedTime;

#if TRUESKY_LOGGING
				Debug.Log("Now time is " + time);
#endif
				InitRendering();
				//	StaticSetRenderBool("RenderSky", _renderSky);
				StaticSetRenderBool("RenderWater", _renderWater);
				StaticSetRenderBool("ReverseDepth", false);
				StaticSetRenderBool("EnableRendering", _renderInEditMode);
				StaticSetRenderBool("ShowFades", _showFades);
				StaticSetRenderBool("ShowCompositing", _showCompositing);
				StaticSetRenderBool("ShowCloudCrossSections", _showCloudCrossSections);
				StaticSetRenderBool("ShowRainTextures", _showRainTextures);
				//StaticSetRenderBool("SimulationTimeRain", _simulationTimeRain);
				StaticSetRenderBool("instantupdate", _instantUpdate);
				StaticSetRenderFloat("Time", _trueSKYTime / TimeUnits);
				//StaticSetRenderBool("gridrendering", _IntegrationScheme == 0);
				//StaticSetRenderInt("MaximumCubemapResolution", _CubemapResolution);
				//StaticSetRenderInt("CloudSteps", _CloudSteps);
				StaticSetRenderFloat("SimpleCloudShadowing", _cloudShadowing);
				StaticSetRenderFloat("SimpleCloudShadowSharpness", _cloudShadowSharpness);
				//StaticSetRenderFloat("CloudThresholdDistanceKm", _cloudThresholdDistanceKm); 
				StaticSetRenderBool("OnscreenProfiling", _onscreenProfiling);
				StaticSetRenderInt("maxCpuProfileLevel", _maxCpuProfileLevel);
				StaticSetRenderInt("maxGpuProfileLevel", _maxGpuProfileLevel);

				//StaticSetRenderFloat("minimumstarpixelsize", _minimumStarPixelSize);
				//StaticSetRenderFloat("render:crepuscularraysstrength", _crepuscularRaysStrength);
				//StaticSetRenderFloat("depthsamplingpixelrange", _depthSamplingPixelRange);
				StaticSetRenderFloat("maxsunradiance", _maxSunRadiance);

				SetNightTextures();		

#if UNITY_EDITOR
				StaticSetRenderBool("ShowCelestialDisplay",_showCelestials);
#endif

#if LICENSING
			StaticSetRenderString("LicenseKey",_licenseKey);
#endif
			}
			catch (Exception exc)
			{
				_initialized = false;
				print(exc.ToString());
			}
		}
		void InitRendering()
		{
			if (_rendering_initialized)
				return;
			try
			{

				UpdateExternalDynamic();
				UpdateExternalRender();
				AddNewMoon();
				_rendering_initialized = true;
			}
			catch (Exception )
			{
				_rendering_initialized = false;
			}
		}
		/// <summary>
		/// Sets the nights textures (background and moon)
		/// </summary>
		public void SetNightTextures()
		{
			if(_moonTexture)
			{
				StaticSetRenderTexture("Moon", _moonTexture.GetNativeTexturePtr());
			}
			if(_backgroundTexture)
			{
				StaticSetRenderTexture("Background", _backgroundTexture.GetNativeTexturePtr());
			}
		}
	}
}