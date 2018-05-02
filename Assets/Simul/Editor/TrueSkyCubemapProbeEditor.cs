using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
//Used for File IO
using System.IO;

namespace simul
{
    [CustomEditor(typeof(TrueSkyCubemapProbe))]
    public class TrueSkyCubemapProbeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            TrueSkyCubemapProbe t = (TrueSkyCubemapProbe)target;

            t.textureSize = EditorGUILayout.IntSlider("Texture Size", t.textureSize, 8, 512);
            t.renderTextureFormat = (RenderTextureFormat)EditorGUILayout.EnumPopup("Format", (System.Enum)t.renderTextureFormat);
            t.exposure = EditorGUILayout.Slider("Exposure", t.exposure, 0.0F, 10.0F);
            t.gamma = EditorGUILayout.Slider("Gamma", t.gamma, 0.0F, 2.0F);
            t.updatePeriodSeconds = EditorGUILayout.FloatField("Update Seconds", t.updatePeriodSeconds);
            t.skyOnly = EditorGUILayout.Toggle("Only sky", t.skyOnly);
            t.flipProbeY = EditorGUILayout.Toggle("Flip Probe Y", t.flipProbeY);

            //t.renderTextureFormat = EditorGUILayout;
            GUILayout.Label(t.GetRenderTexture(), GUILayout.ExpandWidth(true));
            GUILayout.Label("View Id " + t.GetViewId());
            //EditorGUI.DrawPreviewTexture(new Rect(25, 60, 100, 100), t.GetRenderTexture());
        }
    }
}