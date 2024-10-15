#if UNITY_EDITOR //Only run in unity editor, not a packaged game.
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
#if USING_TRUESKY_4_3
using static simul.TrueSkyUIFunctionImporter;
#elif USING_TRUESKY_4_4
using static simul.TrueSkyPluginRenderFunctionImporter;
#endif

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

    }

    [InitializeOnLoad]
    //Class for managing the sequencer; i.e. requesting show/hide, hooking up delegates, etc.
    public class SequencerManager
    {
        #region GetHandle
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern UIntPtr GetProcAddress(IntPtr hModule, string procName);

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
           //UnityEngine.Debug.LogError("Sequence Opened");
#if USING_TRUESKY_4_3
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

            //Disabled while "world view of clouds" is not being drawn to.
            if(LibraryHasExport("TrueSkyPluginRender_MT.dll", "StaticGetRenderingInterface"))
            {
                SetRenderingInterface(Handle, TrueSkyPluginRenderFunctionImporter.StaticGetRenderingInterface());
            }
            

            // Edit the .sq file that this asset is imported from.
            local_path = local_path.Replace(".asset", ".sq");
            string filename = Path.GetFullPath(Path.Combine(Path.Combine(Application.dataPath, ".."), local_path));
            StaticSetString(Handle, "filename_dont_load", filename);
			StaticSetSequence(Handle, currentSequence.SequenceAsText, currentSequence.SequenceAsText.Length + 1);

            trueSKY trueSKY = GetTrueSKY();
            // Initialize time from the trueSKY object

			if (trueSKY) StaticSetFloat(Handle, "time", trueSKY.TrueSKYTime / trueSKY.TimeUnits);

			EditorApplication.playModeStateChanged += CloseDueToPlayModeStateChange;
			EditorApplication.update += UpdateSequencer;
#endif //USING_TRUESKY_4_3
        }
#if USING_TRUESKY_4_3
        static void CloseDueToPlayModeStateChange(PlayModeStateChange state)
		{
			if (_handle != (System.IntPtr)0)
				CloseUI(_handle);
			_handle = (System.IntPtr)0;
        }

        public static void CloseSequencer()
        {
            CloseUI(Handle);
        }
        //Tell sequencer QT window to process events.
        static void UpdateSequencer()
        {
            UpdateUI();
        }
#endif //USING_TRUESKY_4_3
        //Set sequence to be edited in sequencer.
        public static void SetSequence(Sequence newSequence)
        {
            currentSequence = newSequence;
        }
        public static Sequence GetSequence()
        {
            return currentSequence;
        }

        public static void SaveCurrentSequence()
        {
            trueSKY trueSKY = GetTrueSKY();
            if (trueSKY)
            {
                IntPtr result = StaticGetSequence(0, trueSKY.MyAllocator);
                string sequenceData = "";

                if (result != IntPtr.Zero)
                {
                    sequenceData = Marshal.PtrToStringAnsi(result);
                    if (SequencerManager.GetSequence() != null)
                    {
                        SequencerManager.GetSequence().SequenceAsText = sequenceData;
                    }
                }
                else
                {
                    Console.WriteLine("Failed to retrieve sequence.");
                }
                AssetDatabase.SaveAssets();
            }
            
        }
        //Returns trueSKY object with the same sequence as is being edited.
        static trueSKY GetTrueSKY()
        {
#if USING_TRUESKY_4_4
            //we can assume there is only 1 trueSKY in the scene
            UnityEngine.Object[] trueSkies = UnityEngine.Object.FindObjectsOfType(typeof(trueSKY));
            trueSKY trueSKY = (trueSKY)trueSkies[0];
            if(trueSkies.Length > 1)
                UnityEngine.Debug.LogError("Multiple trueSKY instances found");
            return trueSKY;
#elif USING_TRUESKY_4_3
            UnityEngine.Object[] trueSkies = UnityEngine.Object.FindObjectsOfType(typeof(trueSKY));
            foreach (UnityEngine.Object t in trueSkies)
            {
                trueSKY trueSKY = (trueSKY)t;
                if (trueSKY.sequence == currentSequence) return trueSKY;
            }
            //no need to spam warnings about incorrect sequence
			//UnityEngine.Debug.LogError("Active trueSky not found with Current Sequence");
            return null;
#endif
        }

        //Link Simul back-end code with unity events/functions.
        static void LinkDelegates()
        {

#if USING_TRUESKY_4_3
            SetOnPropertiesChangedCallback(OnPropertiesChangedCallback);
            SetOnTimeChangedCallback(OnTimeChangedCallback);
			SetDeferredRenderCallback(DeferredRenderCallback);

            EditorApplication.update += UpdateSequencer;
#endif

#if USING_TRUESKY_4_4
            StaticSetOnSequenceChangeCallback(OnSequenceChangeCallback);
#endif
            
        }
#if USING_TRUESKY_4_3
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
		static TDeferredRenderingCallback DeferredRenderCallback =
			() =>
			{
				trueSKY trueSKY = GetTrueSKY();
				if (trueSKY)
				{
					EditorUtility.SetDirty(trueSKY);
				}
			};
#endif
        static TOnSequenceChangeCallback OnSequenceChangeCallback =
        (string val) =>
        {
            trueSKY trueSKY = GetTrueSKY();         
            if (trueSKY)
            {
                if(currentSequence != null)
                    EditorUtility.SetDirty(currentSequence);
                
              
                SetSequence(trueSKY.sequence);
                EditorUtility.SetDirty(trueSKY);
                //AssetDatabase.SaveAssets();
            }
        };
    }
}
#endif //UNITY_EDITOR