#if UNITY_EDITOR //Only run in unity editor, not a packaged game.
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace simul
{

    class SequencerManagerImports
    {
        static SequencerManagerImports()
        {
            CopyDependencyDllsToProjectDir();
        }
        public static void ReplaceDepthCameraWithTrueSkyCamera()
        {
            UnityEngine.Object[] brokenList = Resources.FindObjectsOfTypeAll(typeof(Camera));
            foreach(UnityEngine.Object o in brokenList)
            {
                UnityEngine.Debug.Log(o);
                GameObject g = (GameObject)o;
                Component[] components = g.GetComponents<Component>();
                for(int i = 0; i < components.Length; i++)
                {
                    if(components[i] == null)
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
            if(currentPath.Contains(dllPath1) == false)
            {
                currentPath = dllPath1 + Path.PathSeparator + currentPath;
            }
            if(currentPath.Contains(dllPath2) == false)
            {
                currentPath = dllPath2 + Path.PathSeparator + currentPath;
            }
            Environment.SetEnvironmentVariable("PATH", currentPath, EnvironmentVariableTarget.Process);
            if(!File.Exists(Environment.GetEnvironmentVariable("WINDIR") + @"\system32\msvcr110.dll"))
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
                        using(Process exeProcess = Process.Start(startInfo))
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
                        using(Process exeProcess = Process.Start(startInfo))
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

    [InitializeOnLoad]
    //Class for managing the sequencer; i.e. requesting show/hide, hooking up delegates, etc.
    public class SequencerManager
    {
        #region imports
        enum Style
        {
            DEFAULT_STYLE = 0,
            UNREAL_STYLE = 1,
            UNITY_STYLE = 2,
            UNITY_STYLE_DEFERRED = 6,
            VISION_STYLE = 8
        };

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void TOnSequenceChangeCallback(int hwnd, string newSequenceState);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void TOnTimeChangedCallback(int hwnd, float time);

        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern UIntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport(SequencerManagerImports.editor_dll)]
        private static extern void OpenUI(System.IntPtr OwnerHWND, int[] pVisibleRect, int[] pParentRect, System.IntPtr Env, Style style, string skin);
        [DllImport(SequencerManagerImports.editor_dll)]
        private static extern void CloseUI(System.IntPtr OwnerHWND);
        [DllImport(SequencerManagerImports.editor_dll)]
        private static extern void UpdateUI();
        [DllImport(SequencerManagerImports.editor_dll)]
        private static extern void HideUI(System.IntPtr OwnerHWND);
        [DllImport(SequencerManagerImports.editor_dll)]
        private static extern void EnableUILogging(string logfile);

        [DllImport(SequencerManagerImports.editor_dll)]
        private static extern int SetRenderingInterface(System.IntPtr OwnerHWND, System.IntPtr RenderingInterface);
        [DllImport(SequencerManagerImports.editor_dll)]
        private static extern void StaticSetString(System.IntPtr OwnerHWND, string name, string value);
        [DllImport(SequencerManagerImports.editor_dll)]
        private static extern void StaticSetSequence(System.IntPtr OwnerHWND, string SequenceAsText, int length_hint);
        [DllImport(SequencerManagerImports.editor_dll)]
        private static extern void StaticSetFloat(System.IntPtr OwnerHWND, string name, float value);

        [DllImport(SequencerManagerImports.editor_dll)]
        private static extern void SetOnPropertiesChangedCallback(TOnSequenceChangeCallback CallbackFunc);
        [DllImport(SequencerManagerImports.editor_dll)]
        private static extern void SetOnTimeChangedCallback(TOnTimeChangedCallback CallbackFunc);

        static bool LibraryHasExport(string lib, string procName)
        {
            IntPtr hModule = LoadLibrary(lib);
            if(hModule != (IntPtr)0)
            {
                if(GetProcAddress(hModule, procName) != (UIntPtr)0)
                    return true;
            }
            return false;
        }
        #endregion

        #region GetHandle
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr extraData);

        public class HandleFinder
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern int GetWindowThreadProcessId(HandleRef handle, out int processId);

            public bool isUnityHandleSet = false;
            public HandleRef unityWindowHandle;

            public bool EnumWindowsCallBack(IntPtr hWnd, IntPtr lParam)
            {
                int procid;
                GetWindowThreadProcessId(new HandleRef(this, hWnd), out procid);

                int currentPID = System.Diagnostics.Process.GetCurrentProcess().Id;

                if(procid == currentPID)
                {
                    unityWindowHandle = new HandleRef(this, hWnd);
                    isUnityHandleSet = true;
                    return false;
                }
                return true;
            }
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        static System.IntPtr _handle = (System.IntPtr)0;
        public static System.IntPtr Handle
        {
            get
            {
                if(_handle == (System.IntPtr)0)
                {
                    HandleFinder hf = new HandleFinder();
                    if(!hf.isUnityHandleSet)
                    {
                        IntPtr extraData = (IntPtr)0;
                        EnumWindows(hf.EnumWindowsCallBack, extraData);
                    }
                    if(hf.isUnityHandleSet) _handle = (System.IntPtr)hf.unityWindowHandle;
                }
                return _handle;
            }
            private set { }
        }
        #endregion

        //Sequence currently being edited.
        static Sequence currentSequence = null;

        static SequencerManager()
        {
            //When we change into play mode the scripts get reloaded, so we need to reload and relink everything.
            SequencerManagerImports.CopyDependencyDllsToProjectDir();
            //This needs to be delayed so Unity has time to load in the DLLs.
            EditorApplication.delayCall += LinkDelegates;
        }

        public static void OpenSequencer()
        {
            if(currentSequence == null)
            {
                UnityEngine.Debug.LogError("Null sequence");
                return;
            }

            string local_path = AssetDatabase.GetAssetPath(currentSequence);
            if(!local_path.Contains(".asset"))
            {
                UnityEngine.Debug.LogError("Filename not found for current sequence: " + currentSequence.ToString());
                return;
            }

            // Disable this when not needed:
            EnableUILogging("trueSKYUnityUI.log");

            const int menuBarOffset = 32;
            int[] r = { 16, 16 + menuBarOffset, 800, 600 };

            OpenUI(Handle, r, r, (System.IntPtr)0, Style.UNITY_STYLE, EditorGUIUtility.isProSkin ? "unity_dark" : "unity");

            /*Disabled while "world view of clouds" is not being drawn to.
            if(LibraryHasExport("TrueSkyPluginRender_MT.dll", "StaticGetRenderingInterface"))
            {
                SetRenderingInterface(Handle, simul.trueSKY.StaticGetRenderingInterface());
            }
            */

            // Edit the .sq file that this asset is imported from.
            local_path = local_path.Replace(".asset", ".sq");
            string filename = Path.GetFullPath(Path.Combine(Path.Combine(Application.dataPath, ".."), local_path));
            StaticSetString(Handle, "filename_dont_load", filename);
			StaticSetSequence(Handle, currentSequence.SequenceAsText, currentSequence.SequenceAsText.Length + 1);

            trueSKY trueSKY = GetTrueSKY();
            // Initialize time from the trueSKY object
            if(trueSKY) StaticSetFloat(Handle, "time", trueSKY.time);
        }

        public static void CloseSequencer()
        {
            CloseUI(Handle);
        }

        //Set sequence to be edited in sequencer.
        public static void SetSequence(Sequence newSequence)
        {
            currentSequence = newSequence;
        }

        //Tell sequencer QT window to process events.
        static void UpdateSequencer()
        {
            UpdateUI();
        }

        //Returns trueSKY object with the same sequence as is being edited.
        static trueSKY GetTrueSKY()
        {
            UnityEngine.Object[] trueSkies = UnityEngine.Object.FindObjectsOfType(typeof(trueSKY));
            foreach (UnityEngine.Object t in trueSkies)
            {
                trueSKY trueSKY = (trueSKY)t;
                if (trueSKY.sequence == currentSequence) return trueSKY;
            }
            return null;
        }

        //Link Simul back-end code with unity events/functions.
        static void LinkDelegates()
        {
            SetOnPropertiesChangedCallback(OnPropertiesChangedCallback);
            SetOnTimeChangedCallback(OnTimeChangedCallback);
            EditorApplication.update += UpdateSequencer;
        }

        //Delegate function for when the properties change in the sequencer window.
        static TOnSequenceChangeCallback OnPropertiesChangedCallback =
            (int hwnd, string newSequenceState) =>
            {
                currentSequence.Load(newSequenceState);

                trueSKY trueSKY = GetTrueSKY();
                if(trueSKY) trueSKY.Reload();

                EditorUtility.SetDirty(currentSequence);
                AssetDatabase.SaveAssets();
            };

        //Delegate function for when the time is changed in the sequencer window.
        static TOnTimeChangedCallback OnTimeChangedCallback =
            (int hwnd, float time) =>
            {
                trueSKY trueSKY = GetTrueSKY();
                if(trueSKY)
                {
                    trueSKY.JumpToTime(time);
                    EditorUtility.SetDirty(trueSKY);
                }
            };
    }
}
#endif //UNITY_EDITOR