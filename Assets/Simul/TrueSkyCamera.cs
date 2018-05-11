using UnityEngine;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace simul 
{
	[ExecuteInEditMode]
	public class TrueSkyCamera : TrueSkyCameraBase
	{
        [DllImport(SimulImports.renderer_dll)]
        protected static extern System.IntPtr UnityGetOverlayFunc();
        [DllImport(SimulImports.renderer_dll)]
        protected static extern System.IntPtr UnityGetPostTranslucentFunc();

        protected float[] cubemapTransformMatrix = new float[16];
        protected override int InternalGetViewId()
		{
			return StaticGetOrAddView((System.IntPtr)view_ident);
		}

		public override RenderStyle GetRenderStyle()
		{
			UnityEngine.VR.VRSettings.showDeviceView = true;
			RenderStyle r = base.GetRenderStyle();
            if (trueSKY.GetTrueSky().DepthBlending)
				r = r | RenderStyle.DEPTH_BLENDING;
            Camera cam = GetComponent<Camera>();
#if UNITY_5_5_OR_NEWER
			if (cam.stereoEnabled)
			{
				StereoTargetEyeMask activeEye = cam.stereoTargetEye;
				r = r | RenderStyle.VR_STYLE;
				if (activeEye == StereoTargetEyeMask.Right)
					r = r | RenderStyle.VR_STYLE_ALTERNATE_EYE;
				if (activeEye == StereoTargetEyeMask.Both&&ShareBuffersForVR)
					r = r | RenderStyle.VR_STYLE_SIDE_BY_SIDE;
			}
#endif
            return r;
        }

        public enum VRPresetType { OculusRift = 0, OpenVR= 1, Custom= 2, Automatic=3 }
        public VRPresetType VRPreset = VRPresetType.OpenVR;
        public int VRTargetWidth = 1280;
        public bool ShareBuffersForVR = true;
       
        protected override int GetRequiredDepthTextureWidth()
		{
			var cam = GetComponent<Camera>();
            if (cam.stereoEnabled && cam.stereoTargetEye == StereoTargetEyeMask.Both)
			{
                int w = 0;
                if (VRPreset == VRPresetType.OpenVR)
                    w=1536;
                else if (VRPreset == VRPresetType.OculusRift)
                    w=1280;
                else if (VRPreset == VRPresetType.Custom)
                    w =VRTargetWidth;
                else
                    w= base.GetRequiredDepthTextureWidth();
                w *= 2;
                return w;
            }
            else
                return base.GetRequiredDepthTextureWidth();
		}
		public RenderTexture showDepthTexture=null;
		
		public RenderTexture inscatterRT;
		public RenderTexture cloudShadowRT;
		public RenderTexture lossRT;
		public RenderTexture cloudVisibilityRT;
		public bool FlipOverlays=false;
        RenderTextureHolder _cloudShadowRT = new RenderTextureHolder();
		RenderTextureHolder _inscatterRT=new RenderTextureHolder();
		RenderTextureHolder _lossRT=new RenderTextureHolder();
		RenderTextureHolder _cloudVisibilityRT=new RenderTextureHolder();
		
		protected RenderTextureHolder reflectionProbeTexture = new RenderTextureHolder();
		protected CommandBuffer overlay_buf=null;
		protected CommandBuffer post_translucent_buf = null;

        void OnEnable()
        {
            // Actuallya create the hardware resources of the render targets
            if (cloudShadowRT)
                cloudShadowRT.Create();
            if(lossRT)
                lossRT.Create();
            if(inscatterRT)
                inscatterRT.Create();
            if(cloudVisibilityRT)
                cloudVisibilityRT.Create();
        }

        private void Start()
        {
            // We'll use the cubemap rt from the cubemap probe to provide reflections to the rain
            reflectionProbeTexture.renderTexture = GameObject.FindObjectOfType<TrueSkyCubemapProbe>().GetRenderTexture();
            if(!reflectionProbeTexture.renderTexture)
                Debug.LogWarning("Could not find a TrueSkyCubemapProbe in the scene, this object is needed to provide reflections to the rain and as a ambient light source for your scene.");
        }

        void OnDestroy()
		{
			OnDisable();
		}

		void OnDisable()
		{
			RemoveCommandBuffers();
		}

		void RemoveCommandBuffers()
		{
			RemoveBuffer("trueSKY render");
			RemoveBuffer("trueSKY depth");
			RemoveBuffer("trueSKY overlay");
			RemoveBuffer ("trueSKY post translucent");
		}

		void OnPreRender()
		{
			if(!enabled||!gameObject.activeInHierarchy)
			{
				return;
			}
			GetComponent<Camera>().depthTextureMode|=DepthTextureMode.Depth;
			PreRender();

			Camera cam=GetComponent<Camera>();
			if(buf==null)
			{
				RemoveCommandBuffers();
				buf = new CommandBuffer();
				buf.name = "trueSKY render";
				blitbuf=new CommandBuffer();
				blitbuf.name = "trueSKY depth";
				overlay_buf=new CommandBuffer();
				overlay_buf.name = "trueSKY overlay";
				post_translucent_buf=new CommandBuffer();
				post_translucent_buf.name = "trueSKY post translucent";
				cbuf_view_id=-1;
			}
			if(cbuf_view_id!=InternalGetViewId())
			{
				cam.RemoveCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
				cam.RemoveCommandBuffers(CameraEvent.AfterForwardAlpha);
				cam.RemoveCommandBuffers(CameraEvent.AfterEverything);
			}
			CommandBuffer[] bufs=cam.GetCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
			PrepareDepthMaterial();
			if(bufs.Length!=2)
			{
				RemoveCommandBuffers();
				cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, blitbuf);
				cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, buf);
				cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, post_translucent_buf);
				cam.AddCommandBuffer(CameraEvent.AfterEverything, overlay_buf);
			}
			buf.Clear();
			blitbuf.Clear();
			overlay_buf.Clear();
			post_translucent_buf.Clear();
            cbuf_view_id =InternalGetViewId();
			blitbuf.Blit(_dummyTexture, (RenderTexture)depthTexture.renderTexture, depthMaterial);
			PrepareMatrices();
            if (showDepthTexture!=null)
				blitbuf.Blit(_dummyTexture, showDepthTexture, depthMaterial);
			
			buf.IssuePluginEvent(UnityGetRenderEventFunc(),TRUESKY_EVENT_ID + cbuf_view_id);
			overlay_buf.IssuePluginEvent(UnityGetOverlayFunc(),TRUESKY_EVENT_ID + cbuf_view_id);
			post_translucent_buf.IssuePluginEvent (UnityGetPostTranslucentFunc(), TRUESKY_EVENT_ID + cbuf_view_id);
		}

		Material depthMaterial = null;
		void PrepareDepthMaterial()
		{
			RenderStyle renderStyle = GetRenderStyle();
			depthMaterial = null;
			if ((renderStyle & RenderStyle.UNITY_STYLE_DEFERRED) != RenderStyle.UNITY_STYLE_DEFERRED)
			{
				if(_flippedDepthMaterial==null)
				{
					_flippedShader=Resources.Load("FlippedDepthShader",typeof(Shader)) as Shader;
					if(_flippedShader!=null)
						_flippedDepthMaterial=new Material(_flippedShader);
					else
						UnityEngine.Debug.LogError("Shader not found: trueSKY needs flippedDepthShader.shader, located in the Assets/Simul/Resources directory");
				}
				depthMaterial = _flippedDepthMaterial;
			}
			else
			{
				if(_deferredDepthMaterial==null)
				{
					_deferredShader=Resources.Load("DeferredDepthShader",typeof(Shader)) as Shader;
					if(_deferredShader!=null)
						_deferredDepthMaterial=new Material(_deferredShader);
					else
						UnityEngine.Debug.LogError("Shader not found: trueSKY needs DeferredDepthShader.shader, located in the Assets/Simul/Resources directory");
				}
				depthMaterial = _deferredDepthMaterial;
			}
			depthMaterial.SetFloat("xoffset",  0.0f);
			depthMaterial.SetFloat("xscale",  1.0f);
			Vector4 texSize = new Vector4(depthTexture.renderTexture.width, depthTexture.renderTexture.height);
			depthMaterial.SetVector("texSize", texSize);
		}

		public Rect LeftViewport = new Rect(0,0,1536,1680);
		public Rect RightViewport = new Rect(1536-80,0,1536,1680);
		public bool DefaultVrViewports = true;
		Viewport[] targetViewports = new Viewport[3];
		void PrepareMatrices()
		{
			RenderStyle renderStyle = GetRenderStyle();
			int view_id=InternalGetViewId();
			if (view_id >= 0)
			{
				Camera cam = GetComponent<Camera>();
				Matrix4x4 m = cam.worldToCameraMatrix;

                Matrix4x4 p =  GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
                ViewMatrixToTrueSkyFormat(renderStyle, m, viewMatrices);
                ProjMatrixToTrueSkyFormat(renderStyle, p, projMatrices);

                if ((renderStyle & RenderStyle.VR_STYLE) == RenderStyle.VR_STYLE)
                {
                    Matrix4x4 l = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
                    Matrix4x4 r = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
                    // Sadly, Unity can't be trusted to give us equivalent matrices to p using these functions:
                    Matrix4x4 pl = p;// cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                    Matrix4x4 pr = p;// cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                    ViewMatrixToTrueSkyFormat(renderStyle,l, viewMatrices,1);
                    ProjMatrixToTrueSkyFormat(renderStyle,pl, projMatrices,1);
                    ViewMatrixToTrueSkyFormat(renderStyle, r, viewMatrices,2);
                    ProjMatrixToTrueSkyFormat(renderStyle, pr, projMatrices,2);
                }

				ProjMatrixToTrueSkyFormat(RenderStyle.UNITY_STYLE, p,overlayProjMatrix);
				
				depthViewports[0].x=depthViewports[0].y=0;
				depthViewports[0].z=depthTexture.renderTexture.width;
				depthViewports[0].w=depthTexture.renderTexture.height;
				// There are now three viewports. 1 and 2 are for left and right eyes in VR.
				targetViewports[0].x = targetViewports[0].y = 0;
				if (cam.actualRenderingPath != RenderingPath.DeferredLighting
					&& cam.actualRenderingPath != RenderingPath.DeferredShading)
				{
					Vector3 screen_0 = cam.ViewportToScreenPoint(new Vector3(0,0,0));
					targetViewports[0].x = (int)(screen_0.x);
					targetViewports[0].y = (int)(screen_0.y);
				}
				for (int i = 0; i < 3; i++)
				{
					targetViewports[i].w = depthTexture.renderTexture.width;
					targetViewports[i].h = depthTexture.renderTexture.height;
					targetViewports[i].zfar = targetViewports[i].znear = 0.0F;
				}

				if ((renderStyle & RenderStyle.VR_STYLE) == RenderStyle.VR_STYLE)
				{
					if (DefaultVrViewports)
					{
						LeftViewport.x = 0;
						LeftViewport.y = 0;
                        LeftViewport.width = depthTexture.renderTexture.width/2;
						LeftViewport.height = UnityEngine.VR.VRSettings.eyeTextureHeight;
						RightViewport.x = depthTexture.renderTexture.width / 2;
                        RightViewport.y = 0;
						RightViewport.width = depthTexture.renderTexture.width / 2;
						RightViewport.height = UnityEngine.VR.VRSettings.eyeTextureHeight;
					}
					targetViewports[1].x = (int)LeftViewport.x;
					targetViewports[1].y = (int)LeftViewport.y;
					targetViewports[1].w = (int)LeftViewport.width;
					targetViewports[1].h = (int)LeftViewport.height;
					targetViewports[2].x = (int)RightViewport.x;
					targetViewports[2].y = (int)RightViewport.y;
					targetViewports[2].w = (int)RightViewport.width;
					targetViewports[2].h = (int)RightViewport.height;

                    depthViewports[1].x = 0;
                    depthViewports[1].y = 0;
                    depthViewports[1].z = (int)LeftViewport.width;
                    depthViewports[1].w = depthTexture.renderTexture.height;

                    depthViewports[2].x = (int)RightViewport.x;
                    depthViewports[2].y = 0;
                    depthViewports[2].z = (int)RightViewport.width;
                    depthViewports[2].w = depthTexture.renderTexture.height;
                }
                UnityRenderOptions unityRenderOptions;
                unityRenderOptions = UnityRenderOptions.DEFAULT;
                if (FlipOverlays)
                    unityRenderOptions = unityRenderOptions | UnityRenderOptions.FLIP_OVERLAYS;
                if (ShareBuffersForVR)
                    unityRenderOptions = unityRenderOptions | UnityRenderOptions.NO_SEPARATION;
                
                UnitySetRenderFrameValues(view_id
                    ,viewMatrices
                    ,projMatrices
                    ,overlayProjMatrix
					,depthTexture.GetNative()
					,depthViewports
					,targetViewports
					,renderStyle
					,exposure
					,gamma
					,Time.renderedFrameCount
                    ,unityRenderOptions);

                _inscatterRT.renderTexture = inscatterRT;
				_cloudVisibilityRT.renderTexture = cloudVisibilityRT;
				_cloudShadowRT.renderTexture = cloudShadowRT;
                _lossRT.renderTexture = lossRT;
				StaticSetRenderTexture("inscatter2D",_inscatterRT.GetNative());
				StaticSetRenderTexture("Loss2D", _lossRT.GetNative());
				StaticSetRenderTexture("CloudVisibilityRT", _cloudVisibilityRT.GetNative());
                if(reflectionProbeTexture.renderTexture)
                {
			        StaticSetRenderTexture("Cubemap", reflectionProbeTexture.GetNative());
                }
				StaticSetRenderTexture("CloudShadowRT", _cloudShadowRT.GetNative());
				MatrixTransform(cubemapTransformMatrix);
				StaticSetMatrix4x4("CubemapTransform", cubemapTransformMatrix);
			}
		}
	}
}