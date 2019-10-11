using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using UnityEditor;
//Used for File IO
using System.IO;
using System.Diagnostics;

namespace simul
{
	public class ClipboardHelper
	{
		private static PropertyInfo m_systemCopyBufferProperty = null;
		private static PropertyInfo GetSystemCopyBufferProperty()
		{
			if (m_systemCopyBufferProperty == null)
			{
				Type T = typeof(GUIUtility);
				m_systemCopyBufferProperty = T.GetProperty("systemCopyBuffer", BindingFlags.Static | BindingFlags.NonPublic);
				if (m_systemCopyBufferProperty == null)
					throw new Exception("Can't access internal member 'GUIUtility.systemCopyBuffer' it may have been removed / renamed");
			}
			return m_systemCopyBufferProperty;
		}
		public static string clipBoard
		{
			get
			{
				return GUIUtility.systemCopyBuffer;
			}
			set
			{
				GUIUtility.systemCopyBuffer = value;
			}
		}
	}
	class SequenceEditorImports
	{
		static SequenceEditorImports()
		{
			CopyDependencyDllsToProjectDir();
		}
		public static void ReplaceDepthCameraWithTrueSkyCamera()
		{
			UnityEngine.Object[] brokenList = Resources.FindObjectsOfTypeAll(typeof(Camera));
			foreach (UnityEngine.Object o in brokenList)
			{
				UnityEngine.Debug.Log(o);
				GameObject g = (GameObject)o;
				Component[] components = g.GetComponents<Component>();
				for (int i = 0; i < components.Length; i++)
				{
					if (components[i] == null)
					{
						g.AddComponent<TrueSkyCamera>();
					}
				}
			}
		}
		public static void Init()
		{
			ReplaceDepthCameraWithTrueSkyCamera();
		}
		public static bool CopyDependencyDllsToProjectDir()
		{
			String currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
			char s = Path.DirectorySeparatorChar;
			String platformPath = Environment.CurrentDirectory + s + "Assets" + s + "Simul" + s + "Plugins" + s;
			
			String dllPath1 = platformPath + s + "x86" + s + "dependencies";
			String dllPath2 = platformPath + s + "x86_64" + s + "dependencies";
			if (currentPath.Contains(dllPath1) == false)
			{
				currentPath=dllPath1 + Path.PathSeparator + currentPath;
			}
			if (currentPath.Contains(dllPath2) == false)
			{
				currentPath = dllPath2 + Path.PathSeparator + currentPath;
			}
			Environment.SetEnvironmentVariable("PATH",  currentPath, EnvironmentVariableTarget.Process);
			if (!File.Exists(Environment.GetEnvironmentVariable("WINDIR") + @"\system32\msvcr110.dll"))
			{
				if(EditorUtility.DisplayDialog("Visual Studio Redistributable", "The trueSKY UI requires the Visual Studio redistributable to be installed.", "Install", "Not now"))
				{
					UnityEngine.Debug.Log("Can't find msvcr110.dll - will install");
					// Use ProcessStartInfo class
					ProcessStartInfo startInfo = new ProcessStartInfo();
					startInfo.CreateNoWindow = false;
					startInfo.UseShellExecute = false;
					startInfo.WindowStyle = ProcessWindowStyle.Hidden;
					startInfo.FileName = dllPath1 + s + "vcredist_x86.exe";
				
					try
					{
						// Start the process with the info we specified.
						// Call WaitForExit and then the using statement will close.
						using (Process exeProcess = Process.Start(startInfo))
						{
							exeProcess.WaitForExit();
						}
					}
					catch
					{
						// Log error.
						UnityEngine.Debug.LogError("Error installing vc redist x86");
					}
					startInfo.FileName = dllPath2 + s + "vcredist_x64.exe";
					try
					{
						using (Process exeProcess = Process.Start(startInfo))
						{
							exeProcess.WaitForExit();
						}
					}
					catch
					{
						// Log error.
						UnityEngine.Debug.LogError("Error installing vc redist x64");
					}
				}
			}
			return true;
		}
#if UNITY_IPHONE || UNITY_XBOX360
	// On iOS and Xbox 360 plugins are statically linked into
	// the executable, so we have to use __Internal as the
	// library name.
	public const string editor_dll ="__Internal";
#else
	public const string editor_dll = "TrueSkyUI_MD";
#endif
	}
	[CustomEditor(typeof(Sequence))]
	public class SequenceEditor : Editor
	{
		public static Sequence sequence = null;
		SequenceEditor()
		{
			//UnityEngine.Debug.Log("SequenceEditor constr");
		}
		~SequenceEditor()
		{
			// Leave these active - we DO NOT need an instance of SequenceEditor to process the callbacks.
			//SetOnPropertiesChangedCallback(null);
			//SetOnTimeChangedCallback(null);
			//UnityEngine.Debug.Log("~SequenceEditor destr");
			//HideSequencer();
		}
        void OnEnable()
        {
            SequenceEditorImports.CopyDependencyDllsToProjectDir();
			//show_when_possible = true;
		}
		#region GetHandle
		public class HandleFinder
		{
			public bool bUnityHandleSet = false;
			public HandleRef unityWindowHandle;
			public bool EnumWindowsCallBack(IntPtr hWnd, IntPtr lParam)
			{
				int procid;
				GetWindowThreadProcessId(new HandleRef(this, hWnd), out procid);

				int currentPID = System.Diagnostics.Process.GetCurrentProcess().Id;

				if (procid == currentPID)
				{
					unityWindowHandle = new HandleRef(this, hWnd);
					bUnityHandleSet = true;
					return false;
				}
				return true;
			}
			[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
			private static extern int GetWindowThreadProcessId(HandleRef handle, out int processId);
		}
		private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr extraData);
		public static System.IntPtr handle
		{
			get
			{
				if (_handle == (System.IntPtr)0)
				{
					HandleFinder hf = new HandleFinder();
					if (!hf.bUnityHandleSet)
					{
						IntPtr extraData = (IntPtr)0;
						EnumWindows(hf.EnumWindowsCallBack, extraData);
					}
					if (!hf.bUnityHandleSet)
						return _handle;
					_handle = (System.IntPtr)hf.unityWindowHandle;// Process.GetCurrentProcess().MainWindowHandle;
				}
				return _handle;
			}
			set
			{
			}
		}
		static System.IntPtr _handle = (System.IntPtr)0;
		#endregion
		#region Imports
		// We want Unity-style controls, but we have to specify this.
		enum Style
		{
			DEFAULT_STYLE = 0
			,UNREAL_STYLE = 1
			,UNITY_STYLE = 2
			,UNITY_STYLE_DEFERRED = 6
			,VISION_STYLE = 8
		};
		[DllImport("kernel32", SetLastError = true)]
		static extern IntPtr LoadLibrary(string lpFileName);
		[DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		static extern UIntPtr GetProcAddress(IntPtr hModule, string procName);

		static bool LibraryHasExport(string lib,string procName)
		{
			IntPtr hModule = LoadLibrary(lib);
			if(hModule != (IntPtr)0)
			{
				if (GetProcAddress(hModule, procName) != (UIntPtr)0)
					return true;
			}
			return false;
		}

		[DllImport(SequenceEditorImports.editor_dll)]
		private static extern void EnableUILogging(string logfile);
		[DllImport(SequenceEditorImports.editor_dll)]
		private static extern void OpenUI(System.IntPtr OwnerHWND, int[] pVisibleRect, int[] pParentRect, System.IntPtr Env, Style style,string skin);
		[DllImport(SequenceEditorImports.editor_dll)]
		private static extern void CloseUI(System.IntPtr OwnerHWND);
        [DllImport(SequenceEditorImports.editor_dll)]
        private static extern void UpdateUI(System.IntPtr OwnerHWND);
        [DllImport(SequenceEditorImports.editor_dll)]
		private static extern void HideUI(System.IntPtr OwnerHWND);

		[DllImport(SequenceEditorImports.editor_dll)]
		private static extern int SetRenderingInterface(System.IntPtr OwnerHWND, System.IntPtr RenderingInterface);

		[DllImport(SequenceEditorImports.editor_dll)]
		private static extern int StaticGetString(System.IntPtr OwnerHWND, string name, StringBuilder str, int len);
		[DllImport(SequenceEditorImports.editor_dll)]
		private static extern void StaticSetString(System.IntPtr OwnerHWND, string name, string value);
		[DllImport(SequenceEditorImports.editor_dll)]
		private static extern void StaticSetSequence(System.IntPtr OwnerHWND, string SequenceAsText,int length_hint);
		[DllImport(SequenceEditorImports.editor_dll)]
		private static extern void StaticSetFloat(System.IntPtr OwnerHWND, string name, float value);
		[DllImport(SequenceEditorImports.editor_dll)]
		private static extern void StaticSetMatrix(System.IntPtr OwnerHWND, string name, float[] value);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		delegate void TOnSequenceChangeCallback(int hwnd, string newSequenceState);
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		delegate void TOnTimeChangedCallback(int hwnd, float time);

		[DllImport(SequenceEditorImports.editor_dll)]
		private static extern void SetOnPropertiesChangedCallback(TOnSequenceChangeCallback CallbackFunc);
		[DllImport(SequenceEditorImports.editor_dll)]
		private static extern void SetOnTimeChangedCallback(TOnTimeChangedCallback CallbackFunc);

		static TOnSequenceChangeCallback onPropertiesChangedCallback =
			(int hwnd, string newSequenceState) =>
			{
				sequence.Load(newSequenceState);

				UnityEngine.Object[] trueSkies;
				trueSkies = FindObjectsOfType(typeof(trueSKY));
				foreach (UnityEngine.Object t in trueSkies)
				{
					trueSKY trueSky = (trueSKY)t;
					if (trueSky.sequence == sequence)
						trueSky.Reload();
				}
				EditorUtility.SetDirty(sequence);
				AssetDatabase.SaveAssets();
			};
		static TOnTimeChangedCallback onTimeChangedCallback =
			(int hwnd, float time) =>
			{
				UnityEngine.Object[] trueSkies;
				trueSkies = FindObjectsOfType(typeof(trueSKY));
				foreach (UnityEngine.Object t in trueSkies)
				{
					trueSKY trueSky = (trueSKY)t;
					if (trueSky.sequence == sequence)
					{
						trueSky.JumpToTime(time);
						EditorUtility.SetDirty(trueSky);
					}
				}
			};
		#endregion
		static trueSKY trueSky()
		{
			UnityEngine.Object[] trueSkies;
			trueSkies = FindObjectsOfType(typeof(trueSKY));
			foreach (UnityEngine.Object t in trueSkies)
			{
				trueSKY trueSky = (trueSKY)t;
				if (trueSky.sequence == sequence)
					return trueSky;
			}
			return null;
		}
		static bool show_when_possible = false;
		public void OnDisable()
		{
			//if(_handle!=(System.IntPtr)0)
				//CloseUI(_handle);
			//_handle=(System.IntPtr)0;
		}

        static void CloseDueToPlayModeStateChange(PlayModeStateChange state)
	    {
			if(_handle!=(System.IntPtr)0)	
				CloseUI(_handle);	
			_handle=(System.IntPtr)0;	
		}				

        static bool show = false;
		static bool copy = false;
		static bool paste = false;
		static protected float[] viewMatrix = new float[16];
	//	[DrawGizmo(GizmoType.NotInSelectionHierarchy)]
		static void RenderCustomGizmo(Transform objectTransform, GizmoType gizmoType)
		{
			//UnityEngine.Debug.Log("update");
			Matrix4x4 m = Camera.current.worldToCameraMatrix;
			TrueSkyCameraBase.ViewMatrixToTrueSkyFormat(TrueSkyCameraBase.RenderStyle.UNITY_STYLE_DEFERRED, m,viewMatrix);
			//StaticSetMatrix(handle, "ViewMatrix", viewMatrix);
		}
		public override void OnInspectorGUI()
		{
			SequenceEditorImports.CopyDependencyDllsToProjectDir();
			sequence = (Sequence)target;
			EditorGUILayout.BeginVertical();
			if (GUILayout.Button("Show Sequencer...") || show_when_possible)
			{
				show = true;
			}
			if (GUILayout.Button("Copy"))
			{
				copy = true;
			}
			if (GUILayout.Button("Paste"))
			{
				paste = true;
			}
			EditorGUILayout.EndVertical();
			if (Event.current.type == EventType.Repaint)
			{
				if (show)
					EditorApplication.delayCall += ShowSequencer;
				if (paste)
					EditorApplication.delayCall += Paste;
				if (copy)
					EditorApplication.delayCall += Copy;
				show = false;
				copy = false;
				paste = false;
			}
		}
		private static float[] MatrixToFloatArray(Matrix4x4 m)
		{
			float[] a = { m.m00,m.m01,m.m02,m.m03
						   ,m.m10,m.m11,m.m12,m.m13
						   ,m.m20,m.m21,m.m22,m.m23
						   ,m.m30,m.m31,m.m32,m.m33};
			return a;
		}
		public static void Copy()
		{
			if (sequence == null)
			{
				UnityEngine.Debug.LogError("Null sequence");
				return;
			}
			StringBuilder str=new StringBuilder("",20);
			try
			{
				int newlen=StaticGetString(handle,"Sequence",str,16);
				if(newlen>0)
				{
					str=new StringBuilder("",newlen);
					StaticGetString(handle,"Sequence",str,newlen);
				}
				ClipboardHelper.clipBoard=str.ToString();
			}
			catch(Exception exc)
			{
				UnityEngine.Debug.Log(exc.ToString());
			}
		}
		public static void Paste()
		{
			if (sequence == null)
			{
				UnityEngine.Debug.LogError("Null sequence");
				return;
			}
			string txt=ClipboardHelper.clipBoard;

			if (txt.Length > 0)
			{
				StaticSetSequence(handle, txt, txt.Length + 1);
				//onPropertiesChangedCallback(handle, txt);
			}
		}
		public static void ShowSequencer()
		{
			if (sequence == null)
			{
				UnityEngine.Debug.LogError("Null sequence");
				return;
			}
			string local_path = AssetDatabase.GetAssetPath(sequence);
			if (!local_path.Contains(".asset"))
			{
				UnityEngine.Debug.LogError("Filename not found for this sequence " + sequence.ToString());
				return;
			}
			show_when_possible = false;
			// Disable this when not needed:
			EnableUILogging("trueSKYUnityUI.log");
			const int menuBarOffset = 32;
			int[] r = { 16, 16 + menuBarOffset, 800, 600 };
			System.IntPtr Env = (System.IntPtr)0;
			SetOnPropertiesChangedCallback(onPropertiesChangedCallback);
			SetOnTimeChangedCallback(onTimeChangedCallback);
			trueSKY t = trueSky();

			string skin = "unity";
			if (EditorGUIUtility.isProSkin)
				skin = "unity_dark";
			OpenUI(handle, r, r, Env, Style.UNITY_STYLE,skin);

			if (LibraryHasExport("TrueSkyPluginRender_MT.dll","GetRenderingInterface"))
				SetRenderingInterface(handle, trueSKY.GetRenderingInterface());

			// Edit the .sq file that this asset is imported from.
			local_path = local_path.Replace(".asset", ".sq");
			//UnityEngine.Debug.Log("local path is "+local_path);
			string filename = Path.GetFullPath(Path.Combine(Path.Combine(Application.dataPath, ".."), local_path));
			//UnityEngine.Debug.Log("Filename is "+filename);
			StaticSetString(handle, "filename_dont_load", filename);
			if (sequence.SequenceAsText != null)
				StaticSetSequence(handle, sequence.SequenceAsText, sequence.SequenceAsText.Length+1);

			// Initialize time from the trueSKY object
			if (t != null)
				StaticSetFloat(handle, "time", t.time);
            EditorApplication.playModeStateChanged += CloseDueToPlayModeStateChange;
            EditorApplication.update += UpdateSequencer;
        }
		protected static float[] viewmat = new float[16];
		public static void HideSequencer()
		{
            EditorApplication.playModeStateChanged -= CloseDueToPlayModeStateChange;
            HideUI(_handle);
		}

        public static void UpdateSequencer()
        {
            UpdateUI(handle);
        }
	}
}