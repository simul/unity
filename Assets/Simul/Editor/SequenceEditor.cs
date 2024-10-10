using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
#if USING_TRUESKY_4_3
using static simul.TrueSkyUIFunctionImporter;
# elif USING_TRUESKY_4_4
using static simul.TrueSkyPluginRenderFunctionImporter;
#endif
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

#if USING_TRUESKY_4_3
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
            return true;
        }
    }
#endif // USING_TRUESKY_4_3

    [CustomEditor(typeof(Sequence))]
    public class SequenceEditor : Editor
    {
        public static Sequence sequence = null;

        static bool show = false;
        static bool showGlobalView = false;
        static bool showProperties = false;
        static bool showSequencer = false;
        static bool copy = false;
        static bool paste = false;
        public override void OnInspectorGUI()
        {
#if USING_TRUESKY_4_3
            //SequenceEditorImports.CopyDependencyDllsToProjectDir();
#endif
            if (sequence != target)
            {
                sequence = (Sequence)target;
                //
            }

            EditorGUILayout.BeginVertical();
#if USING_TRUESKY_4_3
            if(GUILayout.Button("Show Sequencer"))
            {
                show = true;
            }
#endif
#if USING_TRUESKY_4_4
            if (GUILayout.Button("Show Global View"))
            {
                showGlobalView = true;
            }
            if (GUILayout.Button("Show Sequencer"))
            {
                showSequencer = true;
            }            
            if (GUILayout.Button("Show Properties"))
            {
                showProperties = true;
            }
#endif
            if (GUILayout.Button("Copy"))
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
                if(showGlobalView)
                    EditorApplication.delayCall += ShowGlobalViewUI;   
                if(showProperties)
                    EditorApplication.delayCall += ShowPropertiesUI;   
                if(showSequencer)
                    EditorApplication.delayCall += ShowSequencerUI;

                show = false;
               copy = false;
               paste = false;
               showGlobalView = false;
               showProperties = false;
               showSequencer = false;
            }

        }

        public static void ShowSequencer()
        {
            SequencerManager.OpenSequencer();

        }
        public static void ShowGlobalViewUI()
        {
          simul.TrueSkyGlobalViewWindow.ShowWindow();
        }
        public static void ShowPropertiesUI()
        {
            simul.TrueSkyPropertiesWindow.ShowWindow();
        }
        public static void ShowSequencerUI()
        {
            simul.TrueSkySequencerWindow.ShowWindow();
        }

        public static void Copy()
        {
            if (sequence == null)
            {
                UnityEngine.Debug.LogError("Null sequence");
                return;
            }
#if USING_TRUESKY_4_3

            StringBuilder str = new StringBuilder("", 20);
            try
            {
                int newlen = StaticGetString(SequencerManager.Handle, "Sequence", str, 16);
                if (newlen > 0)
                {
                    str = new StringBuilder("", newlen);
                    StaticGetString(SequencerManager.Handle, "Sequence", str, newlen);
                }
                ClipboardHelper.clipBoard = str.ToString();
            }
#elif USING_TRUESKY_4_4
            try
            {

                IntPtr seqPtr = StaticGetSequence(0, trueSKY.MyAllocator);
                string currentSequence = Marshal.PtrToStringAnsi(seqPtr);
                ClipboardHelper.clipBoard = currentSequence;
            }
#endif
            catch (Exception exc)
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

            string txt = ClipboardHelper.clipBoard;

            if (txt.Length > 0 && txt.Contains("skyKeyframer"))
            {

#if USING_TRUESKY_4_3
                StaticSetSequence(SequencerManager.Handle, txt, txt.Length + 1);
#elif USING_TRUESKY_4_4
                StaticSetSequence2(txt);
#endif
                // onPropertiesChangedCallback(handle, txt);
            }
            else
            {
                UnityEngine.Debug.LogError("Sequence information not found in clipboard");
            }
        }
    }
}
