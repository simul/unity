using UnityEngine;
using UnityEditor;
using simul;
using UnityEngine.UIElements;
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.GraphicsBuffer;

namespace simul
{
    public abstract class TrueSkyWindowBase : EditorWindow
    {
        protected RenderTexture customTexture;
        protected Texture2D tex2D;
        protected const int GlobalViewID = 3;
        protected const int PropertiesViewID = 2;
        protected const int SequencerViewID = 1;
        protected abstract RenderTexture GetRenderTexture(trueSKY ts);

        protected int autoSaveIncrement = 0;
        protected int autoSaveTimer = 500;

        protected void ManageEditorWindow(Event e, int ViewID)
        {
            trueSKY ts = trueSKY.GetTrueSky();
            if (ts == null)
            {
                Debug.LogError("trueSKY instance is null.");
                return;
            }
            if (simul.SequencerManager.GetSequence() == null)
            {
                simul.SequencerManager.SetSequence(ts.sequence);
            }

            wantsMouseMove = true;
            DrawTextureAndHandleResizing();
            HandleMouseInput(e, ViewID);
            HandleKeyInput(e, ViewID);


            if(autoSaveIncrement > autoSaveTimer)
            {
               SequencerManager.SaveCurrentSequence();
               autoSaveIncrement = 0;
            }
            autoSaveIncrement++;
            Repaint();
        }
        protected void DrawTextureAndHandleResizing()
        {
            trueSKY ts = trueSKY.GetTrueSky();
            if (ts == null)
            {
                GUILayout.Label("trueSKY instance is null.");
                return;
            }

            customTexture = GetRenderTexture(ts);

            if (customTexture != null)
            {
                int windowWidth = (int)Mathf.Max(300, Mathf.RoundToInt(position.width  - 5));
                int windowHeight = (int)Mathf.Max(300, Mathf.RoundToInt(position.height - 5));

                if (windowWidth != customTexture.width || windowHeight != customTexture.height)
                {
                    customTexture.Release();
                    customTexture.width = (int)windowWidth;
                    customTexture.height = (int)windowHeight;
                    customTexture.Create();
                }

                GUILayout.BeginArea(new Rect(0, 0, windowWidth, windowHeight));
                tex2D = ExtensionMethod.toTexture2D(customTexture);

                GUI.DrawTextureWithTexCoords(new Rect(0, 0, windowWidth, windowHeight), tex2D, new Rect(0, 1, 1, -1));

               //GUILayout.Label(tex2D);
                GUILayout.EndArea();

                //Repaint();
            }
            else
            {
                GUILayout.Label("Texture not found!");
            }
        }
        protected void HandleKeyInput(Event e, int viewID)
        {
            trueSKY ts = trueSKY.GetTrueSky();
            if (ts == null)
            {
                Debug.LogError("trueSKY instance is null.");
                return;
            }

            simul.UIEvent ev = new simul.UIEvent
            {
                view_id = viewID,
                value = new Variant(),
                type = UIEventType.NONE
            };


            //  e.control
            //  e.shift
            //  e.alt

            switch (e.type)
            {
                case EventType.KeyDown:
                    {
                        ev.value.Vec3Int[0] = (int)e.keyCode; //may need function to define key
                        ev.value.Vec3Int[1] = (int)e.keyCode;
                        ev.type = UIEventType.KEY_DOWN;
                        break;
                    }


                case EventType.KeyUp:
                    {
                        ev.value.Vec3Int[0] = (int)e.keyCode; //may need function to define key
                        ev.value.Vec3Int[1] = (int)e.keyCode;
                        ev.type = UIEventType.KEY_UP;
                        break;
                    }
                default:
                    break;
            }

            if (ev.type != UIEventType.NONE)
            {
                ts.simulUIEvents.Add(ev);
                Event.current.Use();
            }
        }

        private float lastMouseMoveTime = 0f;
       // private float mouseMoveThrottleTime = 0.05f;
       // private float mouseMoveDistanceThreshold = 0.2f;
        protected void HandleMouseInput(Event e, int viewID)
        {
            trueSKY ts = trueSKY.GetTrueSky();
            if (ts == null)
            {
                Debug.LogError("trueSKY instance is null.");
                return;
            }

            simul.UIEvent ev = new simul.UIEvent
            {
                view_id = viewID,
                value = new Variant()
            };

            switch (e.type)
            {
                case EventType.MouseDown:
                    {
                        ev.type = UIEventType.MOUSE_DOWN;
                        ev.value.Vec3Int.x = Mathf.RoundToInt(e.mousePosition.x);
                        ev.value.Vec3Int.y = Mathf.RoundToInt(e.mousePosition.y);
                        break;
                    }
                case EventType.MouseUp:
                    ev.type = UIEventType.MOUSE_UP;
                    ev.value.Vec3Int.x = Mathf.RoundToInt(e.mousePosition.x);
                    ev.value.Vec3Int.y = Mathf.RoundToInt(e.mousePosition.y);
                    ev.value.Vec3Int.z = 0; // No button held down
                    break;

                //case EventType.MouseDrag:
                //    ev.type = UIEventType.MOUSE_DRAG;
                //    ev.value.Vec3Int.x = Mathf.RoundToInt(e.mousePosition.x);
                //    ev.value.Vec3Int.y = Mathf.RoundToInt(e.mousePosition.y);
                //    break;

                case EventType.MouseMove:
                    //if (/*Time.realtimeSinceStartup - lastMouseMoveTime > mouseMoveThrottleTime && */e.delta.magnitude > mouseMoveDistanceThreshold)
                    {
                        ev.type = UIEventType.MOUSE_MOVE;
                        ev.value.Vec3Int.x = Mathf.RoundToInt(e.mousePosition.x);
                        ev.value.Vec3Int.y = Mathf.RoundToInt(e.mousePosition.y);
                        lastMouseMoveTime = Time.realtimeSinceStartup;
                    }
                    break;

                case EventType.ScrollWheel:
                    ev.type = UIEventType.MOUSE_WHEEL;
                    ev.value.Int = (int)(-e.delta.y * 100);
                    break;

                case EventType.DragPerform:
                case EventType.MouseDrag:
                    ev.type = UIEventType.MOUSE_MOVE;
                    ev.value.Vec3Int.x = Mathf.RoundToInt(e.mousePosition.x);
                    ev.value.Vec3Int.y = Mathf.RoundToInt(e.mousePosition.y);
                    break;

                default:
                    ev.type = UIEventType.NONE;
                    break;
            }

            switch (e.button)
            {
                case 0:
                    ev.value.Vec3Int.z = 1;
                    break;
                case 1:
                    ev.value.Vec3Int.z = 2;
                    break;
                case 2:
                    ev.value.Vec3Int.z = 4;
                    break;
                default:
                    ev.value.Vec3Int.z = 0;
                    break;
            }
            if (ev.type != UIEventType.NONE)
            { 
                ts.simulUIEvents.Add(ev);
                Event.current.Use();
            }
     
        }
        private void OnDisable()
        {
            SequencerManager.SaveCurrentSequence();
        }
    }

    public class TrueSkyGlobalViewWindow : TrueSkyWindowBase
    {
        [MenuItem("Window/trueSKY Global View")]
        public static void ShowWindow()
        {
            GetWindow<TrueSkyGlobalViewWindow>("Global View");
        }

        protected override RenderTexture GetRenderTexture(trueSKY ts)
        {
            return ts.GlobalViewTexture.renderTexture;
        }

        private void OnGUI()
        {
            ManageEditorWindow(Event.current, GlobalViewID);
        }
    }

    public class TrueSkyPropertiesWindow : TrueSkyWindowBase
    {
        [MenuItem("Window/trueSKY Properties")]
        public static void ShowWindow()
        {
            GetWindow<TrueSkyPropertiesWindow>("Properties");
        }

        protected override RenderTexture GetRenderTexture(trueSKY ts)
        {
            return ts.PropertiesTexture.renderTexture;
        }

        private void OnGUI()
        {
            ManageEditorWindow(Event.current, PropertiesViewID);
        }
    }

    public class TrueSkySequencerWindow : TrueSkyWindowBase
    {
        [MenuItem("Window/trueSKY Sequencer")]
        public static void ShowWindow()
        {
            GetWindow<TrueSkySequencerWindow>("Sequencer");
        }

        protected override RenderTexture GetRenderTexture(trueSKY ts)
        {
            return ts.SequencerTexture.renderTexture;
        }

        private void OnGUI()
        {
            ManageEditorWindow(Event.current, SequencerViewID);
        }
    }

    public static class ExtensionMethod
    {
        public static Texture2D toTexture2D(this RenderTexture rTex, bool opaque = true)
        {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGBA32, false);
            var old_rt = RenderTexture.active;
            RenderTexture.active = rTex;

            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();
            if (opaque)
            {
                Color[] pixels = tex.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i].a = 1.0f;
                }
                tex.SetPixels(pixels);
                tex.Apply();
            }

            RenderTexture.active = old_rt;
            return tex;
        }
    }
}
