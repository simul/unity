#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

//Code taken and edited from Unity Forum by user Psycho8Vegemite - https://forum.unity.com/threads/hdrp-lwrp-detection-from-editor-script.540642/#post-6401309

namespace simul
{
    public static class DefineRenderPipelineMacros
    {
        private const bool LOG_NEW_DEFINE_SYMBOLS = true;

        private const string HDRP_PACKAGE = "render-pipelines.high-definition";
        private const string URP_PACKAGE = "render-pipelines.universal";

        private const string TAG_HDRP = "USING_HDRP";
        private const string TAG_URP = "USING_URP";

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            ListRequest packagesRequest = Client.List(true);
            LoadPackages(packagesRequest);
        }

        private static void LoadPackages(ListRequest request)
        {
            if (request == null)
                return;

            // Wait for request to complete
            for (int i = 0; i < 1000; i++)
            {
                if (request.Result != null)
                    break;
                Task.Delay(1).Wait();
            }
            if (request.Result == null)
                return;

            // Find out what packages are installed
            var packagesList = request.Result.ToList();

            bool hasHDRP = packagesList.Find(x => x.name.Contains(HDRP_PACKAGE)) != null;
            bool hasURP = packagesList.Find(x => x.name.Contains(URP_PACKAGE)) != null;

            if (hasHDRP && hasURP)
                Debug.LogError("RenderPipeline Packages: Both the HDRP and URP seem to be installed. This may cause incompatibility issues.");

            DefinePreProcessors(hasHDRP, hasURP);
        }

        private static void DefinePreProcessors(bool defineHDRP, bool defineURP)
        {
            string originalDefineSymbols;
            string newDefineSymbols;

            List<string> defined;
            BuildTargetGroup platform = EditorUserBuildSettings.selectedBuildTargetGroup;


            originalDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(platform);
            defined = originalDefineSymbols.Split(';').Where(x => !String.IsNullOrWhiteSpace(x)).ToList();

            Action<bool, string> AppendRemoveTag = (stat, tag) =>
            {
                if (stat && !defined.Contains(tag))
                    defined.Add(tag);
                else if (!stat && defined.Contains(tag))
                    defined.Remove(tag);
            };

            AppendRemoveTag(defineHDRP, TAG_HDRP);
            AppendRemoveTag(defineURP, TAG_URP);

            newDefineSymbols = string.Join(";", defined);

            string log = "";
            if (originalDefineSymbols != newDefineSymbols)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(platform, newDefineSymbols);
                log += "Platform "+ platform.ToString() + "Old Define Symbols: " + originalDefineSymbols;
                log += "Platform "+ platform.ToString() + "New Define Symbols: " + newDefineSymbols;
            }

            if (LOG_NEW_DEFINE_SYMBOLS && !String.IsNullOrEmpty(log))
                Debug.Log("PlayerSetting Define Symbols have been updated. Log: " + log);
        }
    }
}
#endif