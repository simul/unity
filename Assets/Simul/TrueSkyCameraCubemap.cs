﻿#define USE_ONPOSTRENDER
using UnityEngine;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace simul
{
    [ExecuteInEditMode]
    public class TrueSkyCameraCubemap : TrueSkyCameraBase
    {
        private bool doFlipY                = true;
        Viewport[] targetViewport           = new Viewport[3];

        RenderTexture activeTexture         = null;
        UnityViewStruct unityViewStruct     = new UnityViewStruct();
        System.IntPtr unityViewStructPtr    = Marshal.AllocHGlobal(Marshal.SizeOf(new UnityViewStruct()));

        /// <summary>
        /// Sets if we should flip vertically the probe
        /// </summary>
        public bool DoFlipY
        {
            get { return doFlipY; }
            set { doFlipY = value; }
        }

        void PrepareMatrices()
        {
            RenderStyle renderStyle = GetRenderStyle() | RenderStyle.CUBEMAP_STYLE;
            view_id                 = InternalGetViewId();
            Camera cam              = GetComponent<Camera>();
            int depthWidth          = cam.pixelWidth;
            int depthHeight         = cam.pixelHeight;

            if (view_id >= 0)
            {
                Matrix4x4 m = cam.worldToCameraMatrix;
                Matrix4x4 p = cam.projectionMatrix;
                // https://docs.unity3d.com/ScriptReference/Camera-projectionMatrix.html
                if (doFlipY)
                {
                    p[1, 1] = -1.0f;
                }
                ViewMatrixToTrueSkyFormat(renderStyle, m, viewMatrices);
                ProjMatrixToTrueSkyFormat(renderStyle, p, projMatrices);

                depthViewports[0].x = depthViewports[0].y = 0;
                depthViewports[0].z = depthWidth;
                depthViewports[0].w = depthHeight;

                targetViewport[0].x = targetViewport[0].y = 0;
                targetViewport[0].w = depthWidth;
                targetViewport[0].h = depthHeight;
                targetViewport[0].znear = 0.0f;
                targetViewport[0].zfar  = 1.0f;

                UnitySetRenderFrameValues
                (
                    view_id
                    , viewMatrices
                    , projMatrices
                    , cproj
                    , System.IntPtr.Zero/*depthTexture.GetNative()*/
                    , depthViewports
                    , targetViewport
                    , renderStyle
                    , exposure
                    , gamma
                    , Time.frameCount
                    , UnityRenderOptions.DEFAULT
                    , Graphics.activeColorBuffer.GetNativeRenderBufferPtr()
                );
            }
        }

        void OnPreRender()
        {
            Camera cam = GetComponent<Camera>();
            if (mainCommandBuffer == null)
            {
                mainCommandBuffer       = new CommandBuffer();
                mainCommandBuffer.name  = "render trueSKY";
                cbuf_view_id            = -1;
            }
            if (cbuf_view_id != InternalGetViewId())
            {
                cam.RemoveCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
            }
            PrepareMatrices();
            CommandBuffer[] bufs = cam.GetCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
            if (bufs.Length != 2)
            {
                cam.RemoveCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
                cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, mainCommandBuffer);
            }
            mainCommandBuffer.Clear();
            cbuf_view_id = InternalGetViewId();

            // Query the color and depth buffers
            unityViewStruct.nativeColourRenderBuffer    = (System.IntPtr)0;
            unityViewStruct.nativeDepthRenderBuffer     = (System.IntPtr)0;
            if (activeTexture != null)
            {
                unityViewStruct.nativeColourRenderBuffer    = activeTexture.colorBuffer.GetNativeRenderBufferPtr();
                unityViewStruct.nativeDepthRenderBuffer     = activeTexture.depthBuffer.GetNativeRenderBufferPtr();
            }
            Marshal.StructureToPtr(unityViewStruct, unityViewStructPtr, true);
            mainCommandBuffer.ClearRenderTarget(true, true, new Color(0.0F, 0.0F, 0.0F, 1.0F), 1.0F);
            mainCommandBuffer.IssuePluginEventAndData(UnityGetRenderEventFuncWithData(), TRUESKY_EVENT_ID + cbuf_view_id, unityViewStructPtr);
        }

        float[] cview = new float[16];
        float[] cproj = new float[16];
        public float[] ViewMatrixToTrueSkyCubemapFormat(RenderStyle renderStyle)
        {
            Matrix4x4 m = GetComponent<Camera>().worldToCameraMatrix;
            ViewMatrixToTrueSkyFormat(renderStyle, m, cview);
            return cview;
        }

        public void Cleanup() 
        {
            // Called from trueskycubemapprobe when destroyed
            StaticRemoveView(view_id);
        }

        private void OnPostRender()
        {
            activeTexture = GetComponent<Camera>().activeTexture;
        }
    }
}