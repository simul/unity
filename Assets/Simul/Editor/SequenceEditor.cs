using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace simul
{
    public class ClipboardHelper
    {
        private static PropertyInfo m_systemCopyBufferProperty = null;
        private static PropertyInfo GetSystemCopyBufferProperty()
        {
            if(m_systemCopyBufferProperty == null)
            {
                Type T = typeof(GUIUtility);
                m_systemCopyBufferProperty = T.GetProperty("systemCopyBuffer", BindingFlags.Static | BindingFlags.NonPublic);
                if(m_systemCopyBufferProperty == null)
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

    [CustomEditor(typeof(Sequence))]
    public class SequenceEditor : Editor
    {
        public static Sequence sequence = null;

        #region Imports
        [DllImport(SequenceEditorImports.editor_dll)]
        private static extern int StaticGetString(System.IntPtr OwnerHWND, string name, StringBuilder str, int len);
        [DllImport(SequenceEditorImports.editor_dll)]
        private static extern void StaticSetSequence(System.IntPtr OwnerHWND, string SequenceAsText, int length_hint);
        #endregion

        static bool show = false;
        static bool copy = false;
        static bool paste = false;

        public override void OnInspectorGUI()
        {
            SequenceEditorImports.CopyDependencyDllsToProjectDir();

            if(sequence != target)
            {
                sequence = (Sequence)target;
                SequencerManager.SetSequence(sequence);
            }

            EditorGUILayout.BeginVertical();
            if(GUILayout.Button("Show Sequencer"))
            {
                show = true;
            }
            if(GUILayout.Button("Copy"))
            {
                copy = true;
            }
            if(GUILayout.Button("Paste"))
            {
                paste = true;
            }
            EditorGUILayout.EndVertical();
            if(Event.current.type == EventType.Repaint)
            {
                if(show)
                    EditorApplication.delayCall += ShowSequencer;
                if(paste)
                    EditorApplication.delayCall += Paste;
                if(copy)
                    EditorApplication.delayCall += Copy;
                show = false;
                copy = false;
                paste = false;
            }
        }

        public static void ShowSequencer()
        {
            SequencerManager.OpenSequencer();
        }

        public static void Copy()
        {
            if(sequence == null)
            {
                UnityEngine.Debug.LogError("Null sequence");
                return;
            }
            StringBuilder str = new StringBuilder("", 20);
            try
            {
                int newlen = StaticGetString(SequencerManager.Handle, "Sequence", str, 16);
                if(newlen > 0)
                {
                    str = new StringBuilder("", newlen);
                    StaticGetString(SequencerManager.Handle, "Sequence", str, newlen);
                }
                ClipboardHelper.clipBoard = str.ToString();
            }
            catch(Exception exc)
            {
                UnityEngine.Debug.Log(exc.ToString());
            }
        }

        public static void Paste()
        {
            if(sequence == null)
            {
                UnityEngine.Debug.LogError("Null sequence");
                return;
            }
            string txt = ClipboardHelper.clipBoard;

            if(txt.Length > 0)
            {
                StaticSetSequence(SequencerManager.Handle, txt, txt.Length + 1);
                //onPropertiesChangedCallback(handle, txt);
            }
        }
    }
}