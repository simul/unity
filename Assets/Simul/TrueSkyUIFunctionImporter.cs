using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using static simul.TrueSkyCameraBase;

namespace simul
{
	class TrueSkyUIFunctionImporter
	{
#if USING_TRUESKY_4_3
#if UNITY_IPHONE || UNITY_XBOX360
		// On iOS and Xbox 360 plugins are statically linked into
		// the executable, so we have to use __Internal as the
		// library name.
		public const string editor_dll ="__Internal";
#else
		public const string editor_dll = "TrueSkyUI_MD";
#endif

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void TGetColourTableCallback(uint x, int r, int g, int b, float[] arr);
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void TOnSequenceChangeCallback(int hwnd, string newSequenceState);
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void TOnTimeChangedCallback(int hwnd, float time);
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void TDeferredRenderingCallback();
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void TTrueSkyUILogCallback(string output);
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void TGenericDataCallback(int hwnd, Int64 size, IntPtr data);
		public delegate string FAlloc(int size);

		public enum Style
		{
			DEFAULT_STYLE = 0,
			UNREAL_STYLE = 1,
			UNITY_STYLE = 2,
			UNITY_STYLE_DEFERRED = 6,
			VISION_STYLE = 8
		};

		//-----------------------------
		//-----DllImport Functions-----
		//-----------------------------
		[DllImport(editor_dll)] public static extern void PushStyleSheetPath(string path);
		[DllImport(editor_dll)] public static extern void EnableUILogging(string logfile);
		[DllImport(editor_dll)] public static extern void SetHighDPIAware(bool value);
		[DllImport(editor_dll)] public static extern int SetRenderingInterface(IntPtr OwnerHWND, IntPtr RenderingInterface);
		[DllImport(editor_dll)] public static extern void OpenUI(IntPtr OwnerHWND, int[] pVisibleRect, int[] pParentRect, IntPtr Env, Style style, string skin);
		[DllImport(editor_dll)] public static extern void CloseUI(IntPtr OwnerHWND);
		[DllImport(editor_dll)] public static extern void UpdateUI();
		[DllImport(editor_dll)] public static extern void HideUI(IntPtr OwnerHWND);
		[DllImport(editor_dll)] public static extern void GetSimulVersion(IntPtr major, IntPtr minor, IntPtr build);
		[DllImport(editor_dll)] public static extern void SetView(int view_id, float[] viewMatrix, float[] projMatrix);
		[DllImport(editor_dll)] public static extern void SetGetColourTableCallback(TGetColourTableCallback CallbackFunc);
		[DllImport(editor_dll)] public static extern void SetOnTimeChangedCallback(TOnTimeChangedCallback CallbackFunc);
		[DllImport(editor_dll)] public static extern void SetOnPropertiesChangedCallback(TOnSequenceChangeCallback CallbackFunc);
		[DllImport(editor_dll)] public static extern void SetDeferredRenderCallback(TDeferredRenderingCallback CallbackFunc);
		[DllImport(editor_dll)] public static extern void SetTrueSkyUILogCallback(TTrueSkyUILogCallback CallbackFunc);
		[DllImport(editor_dll)] public static extern void SetGenericDataCallback(TGenericDataCallback CallbackFunc);
		[DllImport(editor_dll)] public static extern void StaticSetUIString(IntPtr OwnerHWND, string name, string InputText);
		[DllImport(editor_dll)] public static extern int StaticGetString(IntPtr OwnerHWND, string name, StringBuilder str, int len);
		[DllImport(editor_dll)] public static extern void StaticSetSequence(IntPtr OwnerHWND, string SequenceAsText, int length_hint);
		[DllImport(editor_dll)] public static extern void StaticGetSequence(IntPtr OwnerHWND, FAlloc Alloc);
		[DllImport(editor_dll)] public static extern void StaticSetMatrix(IntPtr hwnd, string name, float[] value);
		[DllImport(editor_dll)] public static extern void StaticSetFloat(IntPtr OwnerHWND, string name, float value);
		[DllImport(editor_dll)] public static extern float StaticGetFloat(IntPtr OwnerHWND, string name);
		[DllImport(editor_dll)] public static extern void StaticSet(IntPtr hwnd, string name, int count, Variant[] value);
		[DllImport(editor_dll)] public static extern void StaticGet(IntPtr hwnd, string name, int count, Variant[] value);
		[DllImport(editor_dll)] public static extern void StaticSetString(IntPtr OwnerHWND, string name, string value);
		//[DllImport(editor_dll)] public static extern void UnityRenderUI(int eventID, int pipeline, IntPtr unityViewStruct);
#endif //USING_TRUESKY_4_3
	}
}
