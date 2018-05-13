﻿//#define TRUESKY_LOGGING
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Text;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Diagnostics;
using UnityEngine.Rendering;
using System.Collections.Generic;

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
	 [StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct VolumeQueryResult
	{
		public vec3 pos_m;
		public int valid;
		public float density;
		public float direct_light;
		public float indirect_light;
		public float ambient_light;
		public float precipitation;
	};
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct LightingQueryResult
	{
		public vec3 pos;
		public int valid;
		public vec4 sunlight;      // we use vec4's here to avoid padding.
		public vec4 moonlight;
		public vec4 ambient;
	};
	public struct LineQueryResult
	{
		public vec3 pos1_m;
		public int valid;
		public vec3 pos2_m;
		public float density;
		public float visibility;
		public float optical_thickness_metres;
		public float first_contact_metres;
	};
	class SimulImports
	{ 
		static bool _initialized = false;
#if !UNITY_EDITOR && UNITY_SWITCH
        static bool _staticInitialized = false;
        [DllImport(renderer_dll)]
        private static extern void RegisterPlugin();
#endif
        [DllImport(renderer_dll)]
		private static extern void StaticPushPath(string name, string path);
		[DllImport(renderer_dll)]
		private static extern void StaticPopPath(string name);
#if SIMUL_DEBUG_CALLBACK
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		delegate void TDebugOutputCallback(string output);
		[DllImport(SimulImports.renderer_dll)]
		private static extern void StaticSetDebugOutputCallback(TDebugOutputCallback cb);

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
			return Assembly.Load("Assets\\Plugins\\x86\\dependencies\\" + args.Name);
#else
			return Assembly.Load("Assets\\Plugins\\x86_64\\dependencies\\" + args.Name);
#endif
		}
#endif

		int instanceCount = 0;
		SimulImports()
		{ 
			if (instanceCount==0)
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
		private static extern bool IsWow64Process([In] IntPtr hProcess,[Out] out bool wow64Process);
		

		public static void Init()
		{
#if !UNITY_EDITOR && UNITY_SWITCH
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

#if UNITY_EDITOR
	#if _WIN32
			public const string renderer_dll = @"TrueSkyPluginRender_MT";
	#else
			public const string renderer_dll = @"TrueSkyPluginRender_MT";
#endif
#else
#if UNITY_PS4
			public const string renderer_dll = @"TrueSkyPluginRender";
#elif UNITY_IPHONE || UNITY_SWITCH
			public const string renderer_dll = @"__Internal";
#elif _WIN32
			public const string renderer_dll = @"TrueSkyPluginRender_MT";
#else
			public const string renderer_dll = @"TrueSkyPluginRender_MT";
#endif
#endif
    }

    [ExecuteInEditMode]
	public class trueSKY : MonoBehaviour
	{
		#region Imports
		[DllImport(SimulImports.renderer_dll)]		private static extern void StaticEnableLogging(string logfile);
		[DllImport(SimulImports.renderer_dll)]		private static extern int StaticInitInterface();
		[DllImport(SimulImports.renderer_dll)]		private static extern void StaticPushPath(string name, string path);
		[DllImport(SimulImports.renderer_dll)]		private static extern int StaticPopPath(string name);
		[DllImport(SimulImports.renderer_dll)]		private static extern int StaticTick(float deltaTime);

		// We import StaticSetSequenceTxt(const char *) rather than StaticSetSequence(std::string), as const char * converts from c# string.
		[DllImport(SimulImports.renderer_dll)]		private static extern int StaticSetSequenceTxt(string SequenceInput);
		[DllImport(SimulImports.renderer_dll)]		private static extern int StaticSetRenderTexture(string name,System.IntPtr texture);
		[DllImport(SimulImports.renderer_dll)]		private static extern void StaticSetPointLight(int id,float[] pos,float min_radius,float max_radius,float[] irradiance);
		[DllImport(SimulImports.renderer_dll)]		private static extern void StaticCloudPointQuery(int id,System.IntPtr pos, System.IntPtr volumeQueryResult);
		[DllImport(SimulImports.renderer_dll)]		private static extern void StaticCloudLineQuery(int id,System.IntPtr startpos,System.IntPtr endpos, System.IntPtr volumeQueryResult);
		[DllImport(SimulImports.renderer_dll)]		private static extern void StaticLightingQuery(int id, System.IntPtr pos, System.IntPtr lightingQueryResult);
		[DllImport(SimulImports.renderer_dll)]		private static extern float StaticGetRenderFloat(string name);
		[DllImport(SimulImports.renderer_dll)]		private static extern void StaticSetRenderFloat(string name,float value);
		[DllImport(SimulImports.renderer_dll)]		private static extern bool StaticHasRenderFloat(string name);
		[DllImport(SimulImports.renderer_dll)]		private static extern bool StaticHasRenderInt(string name);
		[DllImport(SimulImports.renderer_dll)]		private static extern int StaticGetRenderInt(string name);
		[DllImport(SimulImports.renderer_dll)]		private static extern void StaticSetRenderInt(string name,int value);
		[DllImport(SimulImports.renderer_dll)] 		private static extern void StaticSetRenderBool(string name, bool value);
		[DllImport(SimulImports.renderer_dll)]		private static extern bool StaticGetRenderBool(string name);
		[DllImport(SimulImports.renderer_dll)]		private static extern float StaticGetRenderFloatAtPosition(string name,float[] pos);

		// These are for keyframe editing:
		[DllImport(SimulImports.renderer_dll)]		private static extern int	StaticRenderGetNumKeyframes			(int layer);
		[DllImport(SimulImports.renderer_dll)]		private static extern uint	StaticRenderInsertKeyframe			(int layer,float t );
		[DllImport(SimulImports.renderer_dll)]		private static extern void	StaticRenderDeleteKeyframe			(uint uid );
		[DllImport(SimulImports.renderer_dll)]		private static extern uint	StaticRenderGetKeyframeByIndex		(int layer,int index);
		[DllImport(SimulImports.renderer_dll)]		private static extern uint	GetInterpolatedCloudKeyframeUniqueId(int layer);
		[DllImport(SimulImports.renderer_dll)]		private static extern uint	GetInterpolatedSkyKeyframeUniqueId();

		// Getting and changing properties of keyframes.
		[DllImport(SimulImports.renderer_dll)]		private static extern bool StaticRenderKeyframeHasFloat(uint uid,string name);
		[DllImport(SimulImports.renderer_dll)]		private static extern void	StaticRenderKeyframeSetFloat	(uint uid,string name,float value);
		[DllImport(SimulImports.renderer_dll)]		private static extern float StaticRenderKeyframeGetFloat	(uint uid,string name);
		[DllImport(SimulImports.renderer_dll)]		private static extern bool StaticRenderKeyframeHasInt		(uint uid,string name);
		[DllImport(SimulImports.renderer_dll)]		private static extern void	StaticRenderKeyframeSetInt		(uint uid,string name,int value);
		[DllImport(SimulImports.renderer_dll)]		private static extern int	StaticRenderKeyframeGetInt		(uint uid,string name);
		[DllImport(SimulImports.renderer_dll)]		private static extern bool StaticRenderKeyframeHasBool		(uint uid,string name);
		[DllImport(SimulImports.renderer_dll)]		private static extern void	StaticRenderKeyframeSetBool		(uint uid,string name,bool value);
		[DllImport(SimulImports.renderer_dll)]		private static extern bool	StaticRenderKeyframeGetBool		(uint uid,string name);


		[DllImport(SimulImports.renderer_dll)]		private static extern int StaticGetRenderString(string name, StringBuilder str, int len);
		[DllImport(SimulImports.renderer_dll)]		private static extern void StaticSetRenderString(string name, string value);
		[DllImport(SimulImports.renderer_dll)]		public static extern void StaticTriggerAction(string name);

		[DllImport(SimulImports.renderer_dll)]		public static extern int GetNumStorms();
		[DllImport(SimulImports.renderer_dll)]		public static extern uint GetStormAtTime(float t);
		[DllImport(SimulImports.renderer_dll)]		public static extern uint GetStormByIndex(int i);

		#endregion
		#region API
		private static trueSKY trueSkySingleton = null;
		public trueSKY()
		{
		}
		~trueSKY()
		{
			if(this==trueSkySingleton)
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
		public void SetPointLight(int id,Vector3 pos,float min_radius,float max_radius,Vector3 irradiance)
		{
			Vector3 convertedPos = UnityToTrueSkyPosition (pos);   			// convert from Unity format to trueSKY  

			float[] p = { convertedPos.x, convertedPos.y, convertedPos.z };   
			float[] i = { irradiance.x, irradiance.z, irradiance.y };
			StaticSetPointLight(id, p, min_radius,max_radius, i);
		}
		public LightingQueryResult StaticLightingQuery(int id, Vector3 pos)
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
			Vector3 convertedPos = UnityToTrueSkyPosition (pos);  			// convert from Unity format to trueSKY

			VolumeQueryResult res=new VolumeQueryResult();
			IntPtr unmanagedPosPtr = Marshal.AllocHGlobal(12);
			IntPtr unmanagedResultPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VolumeQueryResult)));
			float [] p={convertedPos.x,convertedPos.y,convertedPos.z};  
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
			Vector3 convertedStartPos = UnityToTrueSkyPosition (startpos);
			Vector3 convertedEndPos = UnityToTrueSkyPosition (endpos);

			LineQueryResult res = new LineQueryResult();                     
			IntPtr unmanagedPosPtr1 = Marshal.AllocHGlobal(12);
			IntPtr unmanagedPosPtr2 = Marshal.AllocHGlobal(12);
			IntPtr unmanagedResultPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LineQueryResult)));
			// swap y and z because Unity
			float[] p1 = { convertedStartPos.x, convertedStartPos.y, convertedStartPos.z };		 
			float[] p2 = { convertedEndPos.x, convertedEndPos.y, convertedEndPos.z };				 
			Marshal.Copy(p1, 0, unmanagedPosPtr1, 3);
			Marshal.Copy(p2, 0, unmanagedPosPtr2, 3);
			StaticCloudLineQuery(id, unmanagedPosPtr1,unmanagedPosPtr2, unmanagedResultPtr);
			res = (LineQueryResult)Marshal.PtrToStructure(unmanagedResultPtr, typeof(LineQueryResult));
			// Call unmanaged code
			Marshal.FreeHGlobal(unmanagedPosPtr1);
			Marshal.FreeHGlobal(unmanagedPosPtr2);
			Marshal.FreeHGlobal(unmanagedResultPtr);
			return res;
		}
		public float GetCloudAtPosition(Vector3 pos)
		{
			Vector3 convertedPos = UnityToTrueSkyPosition (pos);
			float[] x= { convertedPos.x,convertedPos.y,convertedPos.z };		  
			float ret=StaticGetRenderFloatAtPosition("Cloud",x);
			return ret;
		}
		public float GetCloudShadowAtPosition (Vector3 pos)
		{
			Vector3 convertedPos = UnityToTrueSkyPosition (pos);
			float[] x= { convertedPos.x,convertedPos.y,convertedPos.z };		  
			float ret=StaticGetRenderFloatAtPosition("CloudShadow",x);
			return ret; 
		}
		public float GetPrecipitationAtPosition(Vector3 pos)
		{
			Vector3 convertedPos = UnityToTrueSkyPosition (pos);
			float[] x = { convertedPos.x, convertedPos.y, convertedPos.z };								 
			float ret = StaticGetRenderFloatAtPosition("Precipitation", x);
			return ret;
		}
		// These are for keyframe editing:
		public int GetNumSkyKeyframes()
		{
			return StaticRenderGetNumKeyframes(0);
		}
		public int GetNumCloudKeyframes()
		{
			return StaticRenderGetNumKeyframes(1);
		}
		public uint InsertSkyKeyframe(float t)
		{
			return StaticRenderInsertKeyframe(0,t);
		}
		public uint InsertCloudKeyframe(float t)
		{
			return StaticRenderInsertKeyframe(1,t);
		}
		public void DeleteKeyframe(uint uid)
		{
			StaticRenderDeleteKeyframe(uid);
		}
		
		public uint GetSkyKeyframeByIndex(int index)
		{
			return StaticRenderGetKeyframeByIndex(0,index);
		}

		public uint GetCloudKeyframeByIndex(int index)
		{
			return StaticRenderGetKeyframeByIndex(1,index);
		}

		public uint GetInterpolatedCloudKeyframe(int layer)
		{
			return GetInterpolatedCloudKeyframeUniqueId(layer);
		}
		public uint GetInterpolatedSkyKeyframe()
		{
			return GetInterpolatedSkyKeyframeUniqueId();
		}
		// Getting and changing properties of keyframes.
		public void SetKeyframeValue(uint uid,string name,object value)
		{
			//UnityEngine.Debug.Log("trueSKY.SetKeyframeValue "+uid+" "+name+" "+value);
			//UnityEngine.Debug.Log("type is "+value.GetType());
			if(value.GetType()==typeof(double))
			{
				//UnityEngine.Debug.Log("it's a double");
				double d=(double)value;
				StaticRenderKeyframeSetFloat(uid,name,(float)d);
			}
			else if(value.GetType()==typeof(float)||value.GetType()==typeof(double))
			{
				//UnityEngine.Debug.Log("it's a float");
				StaticRenderKeyframeSetFloat(uid,name,(float)value);
			}
			else if(value.GetType()==typeof(int))
			{
				//UnityEngine.Debug.Log("it's an int");
				StaticRenderKeyframeSetInt(uid,name,(int)value);
			}
			else if(value.GetType()==typeof(bool))
			{
				//UnityEngine.Debug.Log("it's a bool");
				StaticRenderKeyframeSetBool(uid,name,(bool)value);
			}
		}
		public object GetKeyframeValue(uint uid,string name)
		{
			if(StaticRenderKeyframeHasFloat(uid,name))
				return StaticRenderKeyframeGetFloat(uid,name);
			if(StaticRenderKeyframeHasInt(uid,name))
				return StaticRenderKeyframeGetInt(uid,name);
			return 0;
		}

		public uint GetStormUidByIndex(int index)
		{
			return GetStormByIndex (index);
		}
		public uint GetStormUidAtTime(float time)
		{
			return GetStormAtTime (time);
		}
		public float GetStormFloat(uint uid, string name)
		{  
			return StaticRenderKeyframeGetFloat(uid, name);
		}
		public void SetStormFloat(uint uid, string name, float value)
		{  
		 	StaticRenderKeyframeSetFloat(uid, name, value);
		}
		public int GetStormInt(uint uid, string name)
		{  
			return StaticRenderKeyframeGetInt(uid, name);
		}
		public void SetStormInt(uint uid, string name, int value)
		{  
			StaticRenderKeyframeSetInt(uid, name, value);
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
			Vector4 u_dir = UnityToTrueSkyMatrix() * (new Vector4(upos.x, upos.y, upos.z,1.0F));
			return new Vector3(u_dir.x, u_dir.z, u_dir.y);
		}
		static public Vector3 UnityToTrueSkyDirection(Vector3 u_dir)
		{
			Vector4 ts_dir = UnityToTrueSkyMatrix()*(new Vector4(u_dir.x, u_dir.y, u_dir.z,0));
			return new Vector3(ts_dir.x, ts_dir.z, ts_dir.y);
		}
		static public Matrix4x4 UnityToTrueSkyMatrix()
		{
			Matrix4x4 transform = trueSKY.GetTrueSky().transform.worldToLocalMatrix;
			float metresPerUnit = trueSKY.GetTrueSky().MetresPerUnit;
			Matrix4x4 scale		=new Matrix4x4();
			scale.SetTRS(new Vector3(0,0,0),new Quaternion(0,0,0,1.0F),new Vector3(metresPerUnit,metresPerUnit,metresPerUnit));
			transform=scale*transform;
			return transform;
		}

        #endregion

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
					StaticSetRenderFloat("MetresPerUnit", _metresPerUnit); 
					//if (!Application.isPlaying)
						//RepaintAll();
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
        float _minimumStarPixelSize = 1.0f;
        public float MinimumStarPixelSize
        {
            get
            {
                return _minimumStarPixelSize;
            }
            set
            {
                _minimumStarPixelSize = value;
                StaticSetRenderFloat("minimumstarpixelsize", _minimumStarPixelSize);
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
                _godRaysGrid = value;
                // To be safe, clamp to [8,64]
                _godRaysGrid.x = Mathf.Clamp((int)_godRaysGrid.x, 8, 64);
                _godRaysGrid.y = Mathf.Clamp((int)_godRaysGrid.y, 8, 64);
                _godRaysGrid.z = Mathf.Clamp((int)_godRaysGrid.z, 8, 64);
                StaticSetRenderInt("godraysgrid.x", (int)_godRaysGrid.x);
                StaticSetRenderInt("godraysgrid.y", (int)_godRaysGrid.y);
                StaticSetRenderInt("godraysgrid.z", (int)_godRaysGrid.z);
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
				_crepuscularRaysStrength = value;
				StaticSetRenderFloat("CrepuscularRaysStrength", _crepuscularRaysStrength);
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
                _depthSamplingPixelRange = value;
                StaticSetRenderFloat("depthsamplingpixelrange", _depthSamplingPixelRange);
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
                _maxSunRadiance = Mathf.Max(value, 0.0f);
                StaticSetRenderFloat("maxsunradiance", _maxSunRadiance);
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
				_adjustSunRadius = value;
				StaticSetRenderBool("adjustsunradius", _adjustSunRadius);
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
                _edgeNoiseFrequency = value;
                StaticSetRenderInt("edgenoisefrequency",_edgeNoiseFrequency);
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
                _edgeNoiseOctaves = value;
                StaticSetRenderInt("edgenoiseoctaves", _edgeNoiseOctaves);
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
                _edgeNoiseTextureSize = value;
                StaticSetRenderInt("edgenoisetexturesize", _edgeNoiseTextureSize);
            }
        }


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
				_CellNoiseTextureSize = value;
				StaticSetRenderInt("cellnoisetexturesize", _CellNoiseTextureSize);
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
                _edgeNoisePersistence = value;
                StaticSetRenderFloat("EdgeNoisePersistence", _edgeNoisePersistence);
            }
        }

        [SerializeField]
        float _edgeNoiseWavelengthKm = 2.5f;
        public float EdgeNoiseWavelengthKm
        {
            get
            {
                return _edgeNoiseWavelengthKm;
            }
            set
            {
                _edgeNoiseWavelengthKm = value;
                StaticSetRenderFloat("EdgeNoiseWavelengthKm", _edgeNoiseWavelengthKm);
            }
        }

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
                _worleyTextureSize = value;
                StaticSetRenderInt("WorleyTextureSize", _worleyTextureSize);
            }
        }

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
                _worleyWavelengthKm = value;
                StaticSetRenderFloat("WorleyWavelengthKm", _worleyWavelengthKm);
            }
        }

        //! Set a floating-point property of the Sky layer.
        void SetFloat(string str, float value)
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
		//! Get a floating-point property of the 3D cloud layer.
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
		public void SetSkyFloat(string name,float value)
		{
			SetFloat("sky:"+name,value);
		}
		//! Get a floating-point property of the Sky layer.
		public float GetSkyFloat(string name)
		{
			float value = 0.0F;
			try
			{
				value=StaticGetRenderFloat("sky:" + name);
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
			SetFloat("Clouds:" + name, value);
		}
		//! Get a floating-point property of the 3D cloud layer.
		public float GetCloudFloat(string name)
		{
			float value = 0.0F;
			try
			{
				value = StaticGetRenderFloat("Clouds:" + name);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return value;
		}

		public void SetStormCentre(float x, float y)
		{
			int num=GetNumStorms();
			for(int i=0;i<num;i++)	
			{
				uint s=GetStormByIndex(i);
				StaticRenderKeyframeSetFloat(s, "CentreKmx", x / 1000.0F);
				StaticRenderKeyframeSetFloat(s, "CentreKmy", y/ 1000.0F);
			}
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

		static public void RepaintAll()
		{
		}
		[SerializeField]
        float _time;
		/// <summary>
		/// Time in the sequence, set from some external script, e.g. the sequence editor, or modified per-frame by the speed value.
		/// </summary>
		/// <param name="t"></param>
		public float time
		{
			get
			{
#if TRUESKY_LOGGING
				Debug.Log("trueSKY get _time " + _time);
#endif
				return _time;
			}
			set
			{
				if (_time != value)
				{
					try
					{
						_time = value;
						StaticSetRenderFloat("Time", value);
						// What if, having changed this value, we now ask for a light colour before the next Update()?
						// so we force it:
						StaticTick(0.0f);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
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
			_time = value;
			StaticSetRenderFloat("JumpToTime", value);
			StaticTick(0.0f);
		}
		[SerializeField]
		float _speed = 10.0F;
		/// <summary>
		/// Rate of time in the sequence.
		/// </summary>
		/// <param name="t"></param>
		public float speed
		{
			get
			{
				return _speed;
			}
			set
			{
				_speed = value;
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
			StringBuilder str=new StringBuilder("",20);
			try
			{
				int newlen=StaticGetRenderString(s,str,16);
				if(newlen>0)
				{
					str=new StringBuilder("",newlen+2);
					StaticGetRenderString(s,str,newlen+1);
				}
			}
			catch(Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
			return str.ToString();
		}
		public void SetRenderString(string s,string val)
		{
			try
			{
				StaticSetRenderString(s,val);
			}
			catch(Exception exc)
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
		[SerializeField]
		static public bool _showCubemaps = false;
		[SerializeField]
		float _cloudShadowing = 0.5F;
		[SerializeField]
		float _cloudShadowSharpness=0.05F;
		[SerializeField]
		float _cloudThresholdDistanceKm = 1.0F; 
		[SerializeField]
		static public bool _showCloudCrossSections = false;
		[SerializeField]
		static public bool _showRainTextures = false;
		[SerializeField]
		bool _simulationTimeRain = false;

        [SerializeField]
        bool _changedAmortInEd = false;
        public bool ChangeAmortInEd
        {
            get { return _changedAmortInEd; }
            set { _changedAmortInEd = value; }
        }

        [SerializeField]
		int _amortization = 2;
		[SerializeField]
		int _atmosphericsAmortization=2;
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
						StaticSetRenderInt("Amortization", _amortization);
                        if (!Application.isPlaying)
                        {
                            ChangeAmortInEd = true;
                        }
                    }
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
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
						StaticSetRenderInt("AtmosphericsAmortization", _atmosphericsAmortization);
                        if (!Application.isPlaying)
                        {
                            ChangeAmortInEd = true;
                        }
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

		public bool SimulationTimeRain
		{
			get
			{
				return _simulationTimeRain;
			}
			set
			{
				if (_simulationTimeRain != value) try
					{
						_simulationTimeRain = value;
						StaticSetRenderBool("SimulationTimeRain", _simulationTimeRain);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		[SerializeField]
		static public bool _showCompositing = false;
		[SerializeField]
		static public bool _showFades = false;
		[SerializeField]
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

		public float CloudShadowing
		{
			get
			{
				return _cloudShadowing;
			}
			set
			{
				if (_cloudShadowing != value) try
					{
						_cloudShadowing = value;
						StaticSetRenderFloat("SimpleCloudShadowing", _cloudShadowing);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		
		public float CloudShadowSharpness
		{
			get
			{
				return _cloudShadowSharpness;
			}
			set
			{
				if(_cloudShadowSharpness!=value) try
					{
						_cloudShadowSharpness=value;
						StaticSetRenderFloat("SimpleCloudShadowSharpness",_cloudShadowSharpness);
					}
					catch(Exception exc)
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
				if(_cloudThresholdDistanceKm != value) try
					{
						_cloudThresholdDistanceKm = value;
						StaticSetRenderFloat("CloudThresholdDistanceKm", _cloudThresholdDistanceKm);

                    }
					catch(Exception exc)
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
		static public bool ShowCubemaps
		{
			get
			{
				return _showCubemaps;
			}
			set
			{
				bool v = value & _showCubemaps;
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
				if(_backgroundTexture!=value)
				{
					_backgroundTexture=value;
					if(_backgroundTexture!=null)
						StaticSetRenderTexture("Background",_backgroundTexture.GetNativeTexturePtr());
					else
						StaticSetRenderTexture("Background",(System.IntPtr)null);
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
				if(_moonTexture!=value)
				{
					_moonTexture=value;
					if(_moonTexture!=null)
						StaticSetRenderTexture("Moon",_moonTexture.GetNativeTexturePtr());
					else
						StaticSetRenderTexture("Moon",(System.IntPtr)null);
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
				StaticSetSequenceTxt(_sequence.SequenceAsText);
			}
			catch (Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
		}
		[SerializeField]
		int _CloudSteps = 200;
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
						StaticSetRenderInt("CloudSteps", _CloudSteps);
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
						StaticSetRenderInt("MaximumCubemapResolution",  _CubemapResolution);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		int _IntegrationScheme=0;
		[SerializeField]
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
					StaticSetRenderBool("gridrendering", _IntegrationScheme==0);
				}
				catch (Exception exc)
				{
					UnityEngine.Debug.Log(exc.ToString());
				}
			}
		}

		float _MaxCloudDistanceKm = 0;
		[SerializeField]
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
					StaticSetRenderFloat("render:maxclouddistancekm", _MaxCloudDistanceKm);
				}
				catch (Exception exc)
				{
					UnityEngine.Debug.Log(exc.ToString());
				}
			}
		}

		float _RenderGridXKm = 0.4F;
		[SerializeField]
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
						_RenderGridXKm = value;
						StaticSetRenderFloat("render:rendergridxkm", _RenderGridXKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}
		float __RenderGridZKm = 0.4F;
		[SerializeField]
		public float RenderGridZKm
		{
			get
			{
				return __RenderGridZKm;
			}
			set
			{
				if (__RenderGridZKm != value) try
					{
						__RenderGridZKm = value;
						StaticSetRenderFloat("render:rendergridzkm", __RenderGridZKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		float _MaxFractalAmplitudeKm =10.0F;
		[SerializeField]
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
						StaticSetRenderFloat("render:maxfractalamplitudekm", _MaxFractalAmplitudeKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		float _CellNoiseWavelengthKm = 8.7F;
		[SerializeField]
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
						StaticSetRenderFloat("render:cellnoisewavelengthkm", _CellNoiseWavelengthKm);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}


		float _DirectLight = 1.0F;
		[SerializeField]
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
						StaticSetRenderFloat("render:directlight", _DirectLight);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		float _IndirectLight = 1.0F;
		[SerializeField]
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
					StaticSetRenderFloat("render:indirectlight", _IndirectLight);
				}
				catch (Exception exc)
				{
					UnityEngine.Debug.Log(exc.ToString());
				}
			}
		}

		float _AmbientLight = 1.0F;
		[SerializeField]
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
						StaticSetRenderFloat("render:ambientlight", _AmbientLight);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		float _Extinction = 4.0F;
		[SerializeField]
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
						StaticSetRenderFloat("render:extinction", _Extinction);
					}
					catch (Exception exc)
					{
						UnityEngine.Debug.Log(exc.ToString());
					}
			}
		}

		float _MieAsymmetry = 0.87F;
		[SerializeField]
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
		
		bool _initialized = false;
		void Update()
		{
            try
			{
				if (!_initialized)
					Init();
				if (Application.isPlaying)
                {
					_time += Time.deltaTime * (_speed / (24.0F * 60.0F * 60.0F));
                }

                // Update simulation values
                //if (ChangeAmortInEd && Application.isPlaying)
                {
                    //UnityEngine.Debug.Log("Amortization:" + _amortization + " AtmosAmortization:" + _atmosphericsAmortization);
                    StaticSetRenderInt("AtmosphericsAmortization", _atmosphericsAmortization);
                    StaticSetRenderInt("Amortization", _amortization);
                    ChangeAmortInEd = false;
                }
				StaticSetRenderFloat("Time", _time);
				StaticSetRenderFloat("RealTime", Time.time);
				StaticTick(0.0f);
                SetNightTextures();
                StaticSetRenderBool("SimulationTimeRain", _simulationTimeRain);
                StaticSetRenderFloat("minimumstarpixelsize", _minimumStarPixelSize);
                StaticSetRenderFloat("maxsunradiance", _maxSunRadiance);
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
		public Vector3 getSunColour(Vector3 pos,int id=0)
		{
			if (!_initialized)
				Init();
			Vector3 convertedPos = UnityToTrueSkyPosition(pos);
			LightingQueryResult q = StaticLightingQuery( id+(int)234965824, convertedPos);
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
			LightingQueryResult q = StaticLightingQuery(id + (int)12849757, convertedPos);
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

#if UNITY_EDITOR
		static trueSKY()
		{
		}
#endif
		void OnDestroy()     
		{
            // Release what was accumulating beforehand
            if (_initialized) 			
			{  
				StaticPopPath ("ShaderBinaryPath");
				StaticPopPath ("ShaderPath");
				StaticPopPath ("TexturePath");
			}
		}

		void Init()
		{
            try
			{
				if (_initialized)
					return;
				float savedTime = _time;
				_initialized = true;

#if TRUESKY_LOGGING
				Debug.Log("trueSKY time restored from Unity scene as " + savedTime);
#endif

				SimulImports.Init();

#if TRUESKY_LOGGING
				StaticEnableLogging("trueSKYUnityRender.log");
#endif

#if UNITY_PS4
                StaticPushPath("ShaderBinaryPath", Application.dataPath + @"\Simul\shaderbin");
                StaticPushPath("ShaderPath", Application.dataPath + @"\Simul\shaderbin");
                StaticPushPath("TexturePath", Application.dataPath + @"\Simul\Media\Textures");
#else
                StaticPushPath("ShaderBinaryPath", Application.dataPath + @"\Simul\shaderbin");
                StaticPushPath("ShaderPath", Application.dataPath + @"\Simul\Platform\DirectX11\HLSL");
                StaticPushPath("TexturePath", Application.dataPath + @"\Simul\Media\Textures");
#endif
                StaticInitInterface();
				Reload();

#if TRUESKY_LOGGING
			float t=StaticGetRenderFloat("time");
			Debug.Log("trueSKY initial time from sequence " + t);
			Debug.Log("savedTime " + savedTime);
#endif

				time = savedTime;

#if TRUESKY_LOGGING
				Debug.Log("Now time is " + time);
#endif

				StaticSetRenderBool("RenderSky", true);
				StaticSetRenderBool("ReverseDepth", false);
				StaticSetRenderBool("EnableRendering", _renderInEditMode);
				StaticSetRenderBool("ShowFades", _showFades);
				StaticSetRenderBool("ShowCompositing", _showCompositing);
				StaticSetRenderBool("ShowCloudCrossSections", _showCloudCrossSections);
				StaticSetRenderBool("ShowRainTextures", _showRainTextures);
				StaticSetRenderBool("SimulationTimeRain", _simulationTimeRain);
                StaticSetRenderInt("MaximumCubemapResolution",_CubemapResolution);
				StaticSetRenderInt("CloudSteps", _CloudSteps);
				StaticSetRenderFloat("SimpleCloudShadowing", _cloudShadowing);
				StaticSetRenderFloat("SimpleCloudShadowSharpness", _cloudShadowSharpness);
				StaticSetRenderFloat("CloudThresholdDistanceKm", _cloudThresholdDistanceKm); 
				StaticSetRenderBool("OnscreenProfiling", _onscreenProfiling);
				StaticSetRenderInt("maxCpuProfileLevel", _maxCpuProfileLevel);
				StaticSetRenderInt("maxGpuProfileLevel", _maxGpuProfileLevel);
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