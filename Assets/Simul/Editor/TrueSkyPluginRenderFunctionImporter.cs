using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace simul
{
	class TrueSkyPluginRenderFunctionImporter
	{
		#if UNITY_EDITOR
			#if _WIN32
				private const string renderer_dll = @"TrueSkyPluginRender_MT";
			#else
				private const string renderer_dll = @"TrueSkyPluginRender_MT";
			#endif
		#else
			#if UNITY_PS4
				private const string renderer_dll = @"TrueSkyPluginRender";
			#elif UNITY_PS5
				private const string renderer_dll = @"TrueSkyPluginRender";
			#elif UNITY_XBOXONE || UNITY_GAMECORE
				private const string renderer_dll = @"TrueSkyPluginRender_MD";
			#elif UNITY_IPHONE || UNITY_SWITCH
				private const string renderer_dll = @"__Internal";
			#elif _WIN32
				private const string renderer_dll = @"TrueSkyPluginRender_MT";
			#else			
				private const string renderer_dll = @"TrueSkyPluginRender_MT";
			#endif
		#endif

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void TDebugOutputCallback(string str);

		//-----------------------------
		//-----DllImport Functions-----
		//-----------------------------

		//trueSKY
		[DllImport(renderer_dll)] public static extern IntPtr StaticGetRenderingInterface();
		[DllImport(renderer_dll)] public static extern void StaticExecuteDeferredRendering();
		[DllImport(renderer_dll)] public static extern int StaticOnDeviceChanged(IntPtr device);
		[DllImport(renderer_dll)] public static extern void StaticSetGraphicsDevice(IntPtr device, int deviceType, int eventType);
		[DllImport(renderer_dll)] public static extern void StaticSetGraphicsDeviceAndContext(IntPtr device, IntPtr context, int deviceType, int eventType);
		[DllImport(renderer_dll)] public static extern int StaticInitInterface();
		[DllImport(renderer_dll)] public static extern bool StaticTriggerAction(string name);
		[DllImport(renderer_dll)] public static extern int StaticShutDownInterface();
		[DllImport(renderer_dll)] public static extern void StaticSetMemoryInterface(IntPtr memoryInterface);
		[DllImport(renderer_dll)] public static extern void StaticSetDebugOutputCallback(TDebugOutputCallback callback);
		[DllImport(renderer_dll)] public static extern int StaticRemoveView(int view_id);
		[DllImport(renderer_dll)] public static extern int StaticSetSequenceTxt(string txt);
		[DllImport(renderer_dll)] public static extern int StaticSetSequence(string SequenceInput);
		[DllImport(renderer_dll)] public static extern int StaticSetSequence2(string txt);
		[DllImport(renderer_dll)] public static extern IntPtr StaticGetEnvironment();
		[DllImport(renderer_dll)] public static extern void StaticRenderFrame2(IntPtr frameStruct);
		[DllImport(renderer_dll)] public static extern int StaticRenderFrame(IntPtr device, IntPtr pContext, int view_id, float[] viewMatrix4x4, float[] projMatrix4x4, IntPtr depthTexture, IntPtr colourTarget, Viewport depthViewports, Viewport viewports, RenderStyle s, float exposure, float gamma, int framenumber, IntPtr pMultiResConstants);
		[DllImport(renderer_dll)] public static extern void StaticRenderOverlays(IntPtr device, IntPtr pContext, IntPtr externalDepthTexture, float[] viewMatrix4x4, float[] projMatrix4x4, int view_id, IntPtr colourTarget, Viewport[] viewports);
		[DllImport(renderer_dll)] public static extern void StaticRenderOverlays2(IntPtr frameStruct);
		[DllImport(renderer_dll)] public static extern void StaticEnableLogging(string logfile);
		[DllImport(renderer_dll)] public static extern void StaticSetFileLoader(IntPtr fileLoader);
		[DllImport(renderer_dll)] public static extern void StaticCopySkylight(IntPtr pContext, int cube_id, float[] shValues, int shOrder, IntPtr targetTex, float[] engineToSimulMatrix4x4, int updateFrequency, float blend, float copy_exposure, float copy_gamma);
		[DllImport(renderer_dll)] public static extern void StaticCopySkylight2(IntPtr pContext, int cube_id, float[] shValues, int shOrder, IntPtr targetTex, float[] engineToSimulMatrix4x4, int updateFrequency, float blend, float copy_exposure, float copy_gamma, vec3[] ground_colour);
		[DllImport(renderer_dll)] public static extern void StaticCopySkylight3(IntPtr skylight);
		[DllImport(renderer_dll)] public static extern void StaticProbeSkylight(IntPtr pContext, int cube_id, int mip_size, int face_index, int x, int y, int w, int h, float[] targetValues);
		[DllImport(renderer_dll)] public static extern int StaticSetRenderTexture(string name, IntPtr texture);
		[DllImport(renderer_dll)] public static extern int StaticSetRenderTexture2(string name, IntPtr texture);
		[DllImport(renderer_dll)] public static extern int StaticSetCloudPlacementTexture(int index, IntPtr placement_tex, float[] origin_km, float[] extents_km);
		[DllImport(renderer_dll)] public static extern void StaticPushPath(string name, string path);
		[DllImport(renderer_dll)] public static extern int StaticPopPath(string name);
		[DllImport(renderer_dll)] public static extern int StaticGetOrAddView(IntPtr ident);
		[DllImport(renderer_dll)] public static extern void StaticExportCloudLayerToGeometry(string filename, int index);
		[DllImport(renderer_dll)] public static extern void StaticProcessQueries(int num, IntPtr queries);
		[DllImport(renderer_dll)] public static extern void StaticSetPointLight(int id, float[] pos, float min_radius, float max_radius, float[] irradiance);
		[DllImport(renderer_dll)] public static extern void StaticCloudPointQuery(int id, IntPtr pos, IntPtr volumeQueryResult);
		[DllImport(renderer_dll)] public static extern void CloudSphereInteraction(int id, float[] pos, float[] vel, float radius);
		[DllImport(renderer_dll)] public static extern void StaticLightingQuery(int id, IntPtr pos, IntPtr lightingQueryResult);
		[DllImport(renderer_dll)] public static extern void StaticCloudLineQuery(int id, IntPtr startpos, IntPtr endpos, IntPtr volumeQueryResult);
		[DllImport(renderer_dll)] public static extern void StaticSetRenderString(string name, string value);
		[DllImport(renderer_dll)] public static extern int StaticGetRenderString(string name, StringBuilder str, int len);
		[DllImport(renderer_dll)] public static extern void StaticSetRenderBool(string name, bool value);
		[DllImport(renderer_dll)] public static extern bool StaticGetRenderBool(string name);
		[DllImport(renderer_dll)] public static extern float StaticGetRenderFloatAtPosition(string name, float[] pos);
		[DllImport(renderer_dll)] public static extern float StaticGetFloatAtPosition(long enum_, float[] pos, int uid);
		[DllImport(renderer_dll)] public static extern float StaticGetRenderFloat(string name);
		[DllImport(renderer_dll)] public static extern void StaticSetRenderFloat(string name, float value);
		[DllImport(renderer_dll)] public static extern void StaticSetMatrix4x4(string name, float[] matrix4x4);
		[DllImport(renderer_dll)] public static extern bool StaticHasRenderFloat(string name);
		[DllImport(renderer_dll)] public static extern bool StaticHasRenderInt(string name);
		[DllImport(renderer_dll)] public static extern void StaticSetRender(string name, int numparams, Variant[] values);
		[DllImport(renderer_dll)] public static extern int StaticGetRenderInt(string name);
		[DllImport(renderer_dll)] public static extern void StaticGetRender(string name, int numparams, Variant[] values);
		[DllImport(renderer_dll)] public static extern void StaticSetRenderInt(string name, int value);

		//trueWATER
		[DllImport(renderer_dll)] public static extern uint StaticCreateBoundedWaterObject(uint ID, float[] dimension, float[] location);
		[DllImport(renderer_dll)] public static extern uint StaticCreateCustomWaterMesh(int ID, IntPtr newMesh, float[] vertices, float[] normals, uint[] indices);
		[DllImport(renderer_dll)] public static extern void StaticUpdateCustomWaterMesh(int ID, IntPtr newMesh);
		[DllImport(renderer_dll)] public static extern void StaticRemoveCustomWaterMesh(int ID);
		[DllImport(renderer_dll)] public static extern void StaticRemoveBoundedWaterObject(uint ID);
		[DllImport(renderer_dll)] public static extern uint StaticAddWaterProbe(IntPtr values);
		[DllImport(renderer_dll)] public static extern void StaticRemoveWaterProbe(int ID);
		[DllImport(renderer_dll)] public static extern void StaticGetWaterProbeValues(int ID, float[] result);
		[DllImport(renderer_dll)] public static extern void StaticUpdateWaterProbeValues(IntPtr values);
		[DllImport(renderer_dll)] public static extern uint StaticAddWaterBuoyancyObject(IntPtr newObject);
		[DllImport(renderer_dll)] public static extern void StaticUpdateWaterBuoyancyObjectValues(IntPtr values);
		[DllImport(renderer_dll)] public static extern float[] StaticGetWaterBuoyancyObjectResults(int ID);
		[DllImport(renderer_dll)] public static extern void StaticRemoveWaterBuoyancyObject(int ID);
		[DllImport(renderer_dll)] public static extern uint StaticAddWaterMaskObject(IntPtr newObject);
		[DllImport(renderer_dll)] public static extern void StaticUpdateWaterMaskObjectValues(IntPtr values);
		[DllImport(renderer_dll)] public static extern void StaticRemoveWaterMaskObject(int ID);
		[DllImport(renderer_dll)] public static extern uint StaticAddWaterParticleGenerator(IntPtr newGenerator, int newGeneratorType, IntPtr customPlaneTexture);
		[DllImport(renderer_dll)] public static extern void StaticUpdateWaterParticleGeneratorValues(IntPtr values, int generatorType, IntPtr customPlaneTexture);
		[DllImport(renderer_dll)] public static extern void StaticRemoveWaterParticleGenerator(int ID);
		[DllImport(renderer_dll)] public static extern void StaticSetWaterFloat(string name, int ID, float value);
		[DllImport(renderer_dll)] public static extern void StaticSetWaterInt(string name, int ID, int value);
		[DllImport(renderer_dll)] public static extern void StaticSetWaterBool(string name, int ID, bool value);
		[DllImport(renderer_dll)] public static extern void StaticSetWaterVector(string name, int ID, float[] value);
		[DllImport(renderer_dll)] public static extern float StaticGetWaterFloat(string name, int ID, float value);
		[DllImport(renderer_dll)] public static extern int StaticGetWaterInt(string name, int ID);
		[DllImport(renderer_dll)] public static extern bool StaticGetWaterBool(string name, int ID);
		[DllImport(renderer_dll)] public static extern bool StaticGetWaterVector(string name, int ID, float[] result);

		//trueSKY Keyframes
		[DllImport(renderer_dll)] public static extern bool StaticRenderKeyframeHasFloat(uint uid, string name);
		[DllImport(renderer_dll)] public static extern bool StaticRenderKeyframeHasInt(uint uid, string name);
		[DllImport(renderer_dll)] public static extern bool StaticRenderKeyframeHasBool(uint uid, string name);
		[DllImport(renderer_dll)] public static extern void StaticRenderKeyframeSetFloat(uint uid, string name, float value);
		[DllImport(renderer_dll)] public static extern void StaticRenderKeyframeSetInt(uint uid, string name, int value);
		[DllImport(renderer_dll)] public static extern void StaticRenderKeyframeSetBool(uint uid, string name, bool value);
		[DllImport(renderer_dll)] public static extern float StaticRenderKeyframeGetFloat(uint uid, string name);
		[DllImport(renderer_dll)] public static extern int StaticRenderKeyframeGetInt(uint uid, string name);
		[DllImport(renderer_dll)] public static extern bool StaticRenderKeyframeGetBool(uint uid, string name);
		
		//trueSKY Keyframers
		[DllImport(renderer_dll)] public static extern void StaticRenderKeyframerSetFloat(uint uid, string name, float value);
		[DllImport(renderer_dll)] public static extern void StaticRenderKeyframerSetInt(uint uid, string name, int value);
		[DllImport(renderer_dll)] public static extern float StaticRenderKeyframerGetFloat(uint uid, string name);
		[DllImport(renderer_dll)] public static extern int StaticRenderKeyframerGetInt(uint uid, string name);
		[DllImport(renderer_dll)] public static extern uint StaticRenderCreateCloudKeyframer(string name);
		[DllImport(renderer_dll)] public static extern uint StaticRenderDeleteCloudKeyframer(uint uid);
		[DllImport(renderer_dll)] public static extern uint StaticRenderInsertKeyframe(int layer, float t);
		[DllImport(renderer_dll)] public static extern void StaticRenderDeleteKeyframe(uint uid);
		[DllImport(renderer_dll)] public static extern int StaticRenderGetNumKeyframes(int layer);
		[DllImport(renderer_dll)] public static extern uint StaticRenderGetKeyframeByIndex(int layer, int index);
		[DllImport(renderer_dll)] public static extern uint GetInterpolatedCloudKeyframeUniqueId(int layer);
		[DllImport(renderer_dll)] public static extern uint GetInterpolatedSkyKeyframeUniqueId();
		[DllImport(renderer_dll)] public static extern uint GetCloudLayerUIDByIndex(int index);

		//trueSKY Other
		[DllImport(renderer_dll)] public static extern int GetNumStorms();
		[DllImport(renderer_dll)] public static extern uint GetStormAtTime(float t);
		[DllImport(renderer_dll)] public static extern uint GetStormByIndex(int i);
		[DllImport(renderer_dll)] public static extern int StaticGetLightningBolts(IntPtr s, int maxnum);
		[DllImport(renderer_dll)] public static extern int StaticGetRenderLightningState(vec3[] start, vec3[] end, float[] progress, float[] brightness);
		[DllImport(renderer_dll)] public static extern int StaticSpawnLightning(float[] startpos, float[] endpos, float magnitude, float[] colour);
		[DllImport(renderer_dll)] public static extern IntPtr StaticGetRenderInterfaceInstance();
		[DllImport(renderer_dll)] public static extern bool StaticFillColourTable(uint uid, int x, int y, int z, float[] target);
		[DllImport(renderer_dll)] public static extern long StaticGetEnum(string name);
		[DllImport(renderer_dll)] public static extern int StaticGet(long num, Variant[] v);
		[DllImport(renderer_dll)] public static extern int StaticSet(long num, Variant[] v);
		[DllImport(renderer_dll)] public static extern int StaticWaterSet(long num, int ID, Variant[] v);
		[DllImport(renderer_dll)] public static extern int StaticSetMoon(long id, IntPtr moons);
		[DllImport(renderer_dll)] public static extern int StaticSetExternalRenderValues(IntPtr values);
		[DllImport(renderer_dll)] public static extern int StaticSetExternalDynamicValues(IntPtr values);
		[DllImport(renderer_dll)] public static extern void StaticSetKeyframerMapTexture(uint uid, string PNGName);
		[DllImport(renderer_dll)] public static extern void StaticSetCloudKeyframePosition(uint uid, float[] LatLongHeadingDeg);
		[DllImport(renderer_dll)] public static extern int StaticSpawnLightning2(IntPtr startpos, IntPtr endpos, float magnitude, IntPtr colour);
		[DllImport(renderer_dll)] public static extern void GetSimulVersion(IntPtr major, IntPtr minor, IntPtr build);
		[DllImport(renderer_dll)] public static extern int StaticTick(float deltaTime);

		//Unity
		//[DllImport(renderer_dll)] public static extern void UnityPluginLoad(IntPtr unityInterfaces);
		//[DllImport(renderer_dll)] public static extern void UnityPluginUnload();
		[DllImport(renderer_dll)] public static extern void UnityRenderEvent(int eventID);
		[DllImport(renderer_dll)] public static extern IntPtr UnityGetRenderEventFuncWithData();
		[DllImport(renderer_dll)] public static extern IntPtr UnityGetOverlayFuncWithData();
		[DllImport(renderer_dll)] public static extern IntPtr UnityGetPostTranslucentFuncWithData();
		[DllImport(renderer_dll)] public static extern IntPtr UnityGetRenderEventFunc();
		[DllImport(renderer_dll)] public static extern IntPtr UnityGetOverlayFunc();
		[DllImport(renderer_dll)] public static extern IntPtr UnityGetPostTranslucentFunc();
		[DllImport(renderer_dll)] public static extern IntPtr UnityGetStoreStateFunc();
		[DllImport(renderer_dll)] public static extern IntPtr UnityGetExecuteDeferredFunc();
		//[DllImport(renderer_dll)] public static extern void UnitySetGraphicsDevice(IntPtr device, int deviceType, int eventType);
		//[DllImport(renderer_dll)] public static extern void UnitySetRenderFrameValues(int view_id, float[] viewMatrices4x4, float[] projMatrices4x4, float[] overlayProjMatrix4x4, IntPtr fullResDepthTexture2D, int4[] depthViewports, Viewport[] targetViewports, RenderStyle renderStyle, float exposure, float gamma, int framenumber, UnityRenderOptions unityRenderOptions, IntPtr colourTexture);
		
#if !UNITY_EDITOR && UNITY_SWITCH
		[DllImport(renderer_dll)] public static extern void RegisterPlugin();
#endif

	}
}
