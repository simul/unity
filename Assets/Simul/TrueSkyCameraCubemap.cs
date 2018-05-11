#define USE_ONPOSTRENDER
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
        private bool doFlipY = true;
        // Seted from the TrueSkyCubemapProbe
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
                Matrix4x4 m = GetComponent<Camera>().worldToCameraMatrix;
                Matrix4x4 p = GetComponent<Camera>().projectionMatrix;
                // https://docs.unity3d.com/ScriptReference/Camera-projectionMatrix.html
                if (doFlipY)
                {
                    p[1, 1] = -1.0f;
                }
                ViewMatrixToTrueSkyFormat(renderStyle, m, viewMatrices);
                ProjMatrixToTrueSkyFormat(renderStyle, p, projMatrices);
                depthViewports[0].x = depthViewports[0].y = 0;
                depthViewports[0].z = depthTexture.renderTexture.width;
                depthViewports[0].w = depthTexture.renderTexture.height;
                targetViewport[0].x = targetViewport[0].y = 0;
                targetViewport[0].w = depthTexture.renderTexture.width;
                targetViewport[0].h = depthTexture.renderTexture.height;
                targetViewport[0].znear = targetViewport[0].zfar = 0.0F;
                UnitySetRenderFrameValues(view_id, viewMatrices, projMatrices, cproj
                            , depthTexture.GetNative(), depthViewports, targetViewport
                            , renderStyle, exposure, gamma, Time.renderedFrameCount, UnityRenderOptions.DEFAULT);

            }
        }

        void OnPreRender()
        {
            EnsureDepthTexture();
            Camera cam = GetComponent<Camera>();
            if (buf == null)
            {
                buf = new CommandBuffer();
                buf.name = "render trueSKY";
                blitbuf = new CommandBuffer();
                blitbuf.name = "copy depth texture";
                cbuf_view_id = -1;
            }
            if (cbuf_view_id != InternalGetViewId())
            {
                cam.RemoveCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
            }
            PrepareMatrices();
            CommandBuffer[] bufs = cam.GetCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
            Material mat = PrepareDepthMaterial();
            if (bufs.Length != 2)
            {
                cam.RemoveCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
                cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, blitbuf);
                cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, buf);
            }
            buf.Clear();
            blitbuf.Clear();
            cbuf_view_id = InternalGetViewId();
            blitbuf.Blit(_dummyTexture, (RenderTexture)depthTexture.renderTexture, mat);
            buf.ClearRenderTarget(true, true, new Color(0.0F, 0.0F, 0.0F, 1.0F), 1.0F);
            buf.IssuePluginEvent(UnityGetRenderEventFunc(), TRUESKY_EVENT_ID + cbuf_view_id);
        }

        float[] cview = new float[16];
        float[] cproj = new float[16];
        public float[] ViewMatrixToTrueSkyCubemapFormat(RenderStyle renderStyle)
        {
            Matrix4x4 m = GetComponent<Camera>().worldToCameraMatrix;
            ViewMatrixToTrueSkyFormat(renderStyle, m, cview);
            return cview;
        }

        public void Cleanup() // called from trueskycubemapprobe when destroyed
        {
            StaticRemoveView(view_id);
        }
    }
}