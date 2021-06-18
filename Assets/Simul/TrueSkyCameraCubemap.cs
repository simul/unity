#define USE_ONPOSTRENDER
using UnityEngine;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Collections.Generic;

using static simul.TrueSkyPluginRenderFunctionImporter;

namespace simul
{
    [ExecuteInEditMode]
    public class TrueSkyCameraCubemap : TrueSkyCameraBase
    {
        private bool doFlipY = true;
        // Set from the TrueSkyCubemapProbe
        public bool DoFlipY
        {
            get { return doFlipY; }
            set { doFlipY = value; }
        }

        Material PrepareDepthMaterial()
        {
            Material mat = null;
            if (_deferredDepthMaterial == null)
            {
                _deferredShader = Resources.Load("DeferredDepthShader", typeof(Shader)) as Shader;
                if (_deferredShader != null)
                    _deferredDepthMaterial = new Material(_deferredShader);
                else
                    UnityEngine.Debug.LogError("Shader not found: trueSKY needs DeferredDepthShader.shader, located in the Assets/Simul/Resources directory");
            }
            mat = _deferredDepthMaterial;
            _deferredDepthMaterial.SetFloat("xoffset", 0.0f);
            _deferredDepthMaterial.SetFloat("xscale", 1.0f);
            return mat;
        }

        Viewport[] targetViewport = new Viewport[3];
        void PrepareMatrices()
        {
            RenderStyle renderStyle = GetRenderStyle() | RenderStyle.CUBEMAP_STYLE;
            view_id = InternalGetViewId();
            if (view_id >= 0)
			{
				Camera cam = GetComponent<Camera>();
				Matrix4x4 m = cam.worldToCameraMatrix;
                Matrix4x4 p = cam.projectionMatrix;
				// https://docs.unity3d.com/ScriptReference/Camera-projectionMatrix.html
				if (doFlipY)
                {
                    p[1, 1] = -1.0f;
                }
                ViewMatrixToTrueSkyFormat(renderStyle, m, viewMatrices);
                ProjMatrixToTrueSkyFormat(renderStyle, p, projMatrices);
				// Query depth size
				int depthWidth = cam.pixelWidth;
				int depthHeight = cam.pixelHeight;

                depthViewports[0].x = depthViewports[0].y = 0;
                depthViewports[0].z = depthWidth;
                depthViewports[0].w = depthHeight;

                targetViewport[0].x = targetViewport[0].y = 0;
                targetViewport[0].w = depthWidth;
                targetViewport[0].h = depthHeight;

				unityViewStruct.view_id = view_id;
				unityViewStruct.framenumber = Time.renderedFrameCount;
				unityViewStruct.exposure = exposure;
				unityViewStruct.gamma = gamma;
				unityViewStruct.viewMatrices4x4 = viewMatrices;
				unityViewStruct.projMatrices4x4 = projMatrices;
				unityViewStruct.overlayProjMatrix4x4 = overlayProjMatrix;
				unityViewStruct.depthTexture =  (System.IntPtr)0;
				unityViewStruct.depthViewports = depthViewports;
				unityViewStruct.targetViewports = targetViewport;
				unityViewStruct.renderStyle = renderStyle;
				unityViewStruct.unityRenderOptions = UnityRenderOptions.DEFAULT;
				unityViewStruct.colourTexture = Graphics.activeColorBuffer.GetNativeRenderBufferPtr();
                unityViewStruct.colourTextureArrayIndex = -1;
			}
		}
		UnityViewStruct unityViewStruct = new UnityViewStruct();
		System.IntPtr unityViewStructPtr = Marshal.AllocHGlobal(Marshal.SizeOf(new UnityViewStruct()));
		void OnPreRender()
        {
            EnsureDepthTexture();
            Camera cam = GetComponent<Camera>();
            if (mainCommandBuffer == null)
            {
                mainCommandBuffer             = new CommandBuffer();
                mainCommandBuffer.name        = "render trueSKY";
                cbuf_view_id    = -1;
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

            mainCommandBuffer.ClearRenderTarget(true, true, new Color(0.0F, 0.0F, 0.0F, 1.0F), 1.0F);

            int faceMask = FindObjectOfType<TrueSkyCubemapProbe>().GetFaceMask();
            if (faceMask == 63) { faceMask = 1; }

            if (prevFaceMask != faceMask)
                prevFaceMask = faceMask;
            else
                return;

            if (cam.activeTexture == null)
                return;

            unityViewStruct.nativeColourRenderBuffer = cam.activeTexture.colorBuffer.GetNativeRenderBufferPtr();
            unityViewStruct.nativeDepthRenderBuffer = cam.activeTexture.depthBuffer.GetNativeRenderBufferPtr();
            unityViewStruct.colourResourceState = ResourceState.RenderTarget;
            unityViewStruct.depthResourceState = ResourceState.DepthWrite;
            unityViewStruct.colourTextureArrayIndex = (int)ToCubemapFace(faceMask);

            Marshal.StructureToPtr(unityViewStruct, unityViewStructPtr, !UsingIL2CPP());
            mainCommandBuffer.IssuePluginEventAndData(UnityGetRenderEventFuncWithData(), TRUESKY_EVENT_ID + cbuf_view_id, unityViewStructPtr);
		}
        private int prevFaceMask = 0;

        CubemapFace ToCubemapFace(int faceMask)
        {
            return (CubemapFace)Mathf.Log(faceMask, 2);
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
        }
    }
}