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
		int lastFrameCount =-1;
		[DllImport(SimulImports.renderer_dll)]
		protected static extern System.IntPtr UnityGetOverlayFunc();
		[DllImport(SimulImports.renderer_dll)]
		protected static extern System.IntPtr UnityGetPostTranslucentFunc();
		[DllImport(SimulImports.renderer_dll)]
		protected static extern System.IntPtr UnityGetPostTranslucentFuncWithData(); 

		protected float[] cubemapTransformMatrix = new float[16];
		protected float[] rainDepthMatrix = new float[16];
		protected float[] rainDepthProjection = new float[16];
		protected float rainDepthTextureScale;
		protected override int InternalGetViewId()
		{
			return StaticGetOrAddView((System.IntPtr)view_ident);
		}
		// We will STORE the activeTexture from the camera and hope it's valid next frame.
		private RenderTexture activeTexture = null;
		public RenderTexture inscatterRT;
		public RenderTexture cloudShadowRT;
		public RenderTexture lossRT;
		public RenderTexture cloudVisibilityRT;
		public bool FlipOverlays				= false;
		public RenderTexture showDepthTexture	= null;

		RenderTextureHolder _cloudShadowRT		= new RenderTextureHolder();
		RenderTextureHolder _inscatterRT		= new RenderTextureHolder();
		RenderTextureHolder _lossRT				= new RenderTextureHolder();
		RenderTextureHolder _cloudVisibilityRT	= new RenderTextureHolder();

		RenderTextureHolder _rainDepthRT		= new RenderTextureHolder();
		protected RenderTextureHolder reflectionProbeTexture = new RenderTextureHolder();
		protected CommandBuffer overlay_buf 				= null;
		protected CommandBuffer post_translucent_buf		= null;
		/// Blitbuf is now only for in-editor use.
		protected CommandBuffer blitbuf = null;
		protected CommandBuffer deferred_buf = null; 

		/// <summary>
		/// This material is used the blit the camera's depth to a render target
		/// that we'll send to the plugin
		/// </summary>
		Material depthMaterial = null;

		//Mesh screenQuad = null;

		/// <summary>
		/// If true, both XR eyes are expected to be rendered to the same texture.
		/// </summary>
		public bool ShareBuffersForVR = true;
		public TrueSkyRainDepthCamera RainDepthCamera = null;
		/// <summary>
		/// Generates an apropiate RenderStyle acording with this camera settings
		/// and takes into account if stereo (XR) is enabled.
		/// </summary>
		/// <returns> A RenderStyle used by the plugin </returns>
		public override RenderStyle GetRenderStyle()
		{
#if !UNITY_SWITCH
			UnityEngine.XR.XRSettings.showDeviceView = true;
#endif
			RenderStyle r = base.GetRenderStyle();
			if (trueSKY.GetTrueSky().DepthBlending)
			{
				r = r | RenderStyle.DEPTH_BLENDING;
			}
			Camera cam = GetComponent<Camera>();
			if (cam.stereoEnabled)
			{
				StereoTargetEyeMask activeEye = cam.stereoTargetEye;
				r = r | RenderStyle.VR_STYLE;
				if (activeEye == StereoTargetEyeMask.Right)
				{
					r = r | RenderStyle.VR_STYLE_ALTERNATE_EYE;
				}
				if (activeEye == StereoTargetEyeMask.Both && ShareBuffersForVR)
				{
					r = r | RenderStyle.VR_STYLE_SIDE_BY_SIDE;
				}
			}
			return r;
		}

		protected override int GetRequiredDepthTextureWidth()
		{
			var cam = GetComponent<Camera>();
			if (cam.stereoEnabled && cam.stereoTargetEye == StereoTargetEyeMask.Both)
			{
#if UNITY_SWITCH
                return 0;
#else
				return UnityEngine.XR.XRSettings.eyeTextureDesc.width;
#endif
            }
            else
            {
                return base.GetRequiredDepthTextureWidth();
            }
		}

		private void OnBeginFrameRendering(ScriptableRenderContext scriptableRenderContext, Camera camera)
		{
			HDRPPreRender(scriptableRenderContext, camera);
		}
		private void OnEndFrameRendering(ScriptableRenderContext scriptableRenderContext, Camera camera)
		{
			HDRPPostRender(camera);
		}

		void OnEnable()
        {
            // Actually create the hardware resources of the render targets
            if (cloudShadowRT)
                cloudShadowRT.Create();
            if(lossRT)
                lossRT.Create();
            if(inscatterRT)
                inscatterRT.Create();
            if(cloudVisibilityRT)
                cloudVisibilityRT.Create();

			RenderPipelineManager.beginCameraRendering += OnBeginFrameRendering;
			RenderPipelineManager.endCameraRendering += OnEndFrameRendering;
		}

        public bool IsPPStak
        {
            get
            {
                return System.Type.GetType("UnityEngine.PostProcessing.PostProcessingBehaviour") != null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan;
			}
        }

		public bool editorMode
		{
			get
			{
				return IsPPStak || (Application.isEditor && !Application.isPlaying);
			}
		}

        private void Start()
        {
            var probes = FindObjectsOfType<TrueSkyCubemapProbe>();
            if(probes.Length <= 0)
            {
                Debug.LogWarning("Could not find a TrueSkyCubemapProbe object");
            }
            else
            {
                for (int i = 0; i < probes.Length; i++) 
                {
                    // We will ignore disabled probes:
                    if(probes[i].enabled && probes[i].isActiveAndEnabled)
                    {
                        reflectionProbeTexture.renderTexture = probes[i].GetRenderTexture();
                        break;
                    }
                }
            }
        }

		void OnDestroy()
		{
			OnDisable();
		}

		void OnDisable()
		{
			RemoveCommandBuffers();
			RenderPipelineManager.beginCameraRendering -= OnBeginFrameRendering;
			RenderPipelineManager.endCameraRendering += OnEndFrameRendering;
		}

		void RemoveCommandBuffers()
		{
			RemoveBuffer("trueSKY store state");
			RemoveBuffer("trueSKY render");
			RemoveBuffer("trueSKY depth");
			RemoveBuffer("trueSKY overlay");
			RemoveBuffer("trueSKY post translucent");
            RemoveBuffer("trueSKY depth blit");
		}
		UnityViewStruct unityViewStruct=new UnityViewStruct();
		System.IntPtr unityViewStructPtr = Marshal.AllocHGlobal(Marshal.SizeOf(new UnityViewStruct()));
		void HDRPPreRender(ScriptableRenderContext src, Camera cam)
		{
			if (!enabled||!gameObject.activeInHierarchy)
			{
				UnityEngine.Debug.Log("Failed to draw");
				return;
			}
			cam.depthTextureMode|=DepthTextureMode.Depth;
			PreRender();
            if (mainCommandBuffer == null) 
			{
				RemoveCommandBuffers();
				mainCommandBuffer           = new CommandBuffer();
				mainCommandBuffer.name      = "trueSKY render";
				overlay_buf                 = new CommandBuffer();
				overlay_buf.name            = "trueSKY overlay";
				post_translucent_buf        = new CommandBuffer();
				post_translucent_buf.name   = "trueSKY post translucent";
				deferred_buf                = new CommandBuffer();
                deferred_buf.name           = "trueSKY deferred contexts";
				blitbuf                     = new CommandBuffer();
				blitbuf.name                = "trueSKY depth blit";
				cbuf_view_id                = -1;
			}
			if (cbuf_view_id != InternalGetViewId()) 
			{
				cam.RemoveCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
				cam.RemoveCommandBuffers(CameraEvent.AfterForwardAlpha);
				cam.RemoveCommandBuffers(CameraEvent.AfterEverything);
			}
            CommandBuffer[] bufs = cam.GetCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
			if(editorMode)
				PrepareDepthMaterial();
			int requiredNumber = 1 + (editorMode ? 2 : 0);
            if (bufs.Length != requiredNumber) 
			{
				RemoveCommandBuffers();
				if(editorMode)
					cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, blitbuf);
				cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, mainCommandBuffer);
				cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, post_translucent_buf);
				cam.AddCommandBuffer(CameraEvent.AfterEverything, overlay_buf);
				//if (editorMode)
					//cam.AddCommandBuffer(CameraEvent.AfterEverything, deferred_buf); 
			}
            mainCommandBuffer.Clear();
			blitbuf.Clear();
			overlay_buf.Clear();
			post_translucent_buf.Clear();
			deferred_buf.Clear();
            cbuf_view_id = InternalGetViewId();
			if (editorMode)
			{
				blitbuf.SetRenderTarget((RenderTexture)depthTexture.renderTexture);
				blitbuf.DrawProcedural(Matrix4x4.identity, depthMaterial, 0, MeshTopology.Triangles, 6);
				blitbuf.SetRenderTarget(Graphics.activeColorBuffer);
			}
			if (lastFrameCount == Time.renderedFrameCount)
			{
				duplicateFrames++;
				if (duplicateFrames >= 10)
				{
					lastFrameCount = localFrameCount++;
					//UnityEngine.Debug.Log("RenderedFrameCount is not increasing. Value is " + Time.renderedFrameCount);
					//return;
				}
			}
			else
			{
				duplicateFrames = 0;
			}
			PrepareMatrices();
			unityViewStruct.nativeColourRenderBuffer = (System.IntPtr)0;
			unityViewStruct.nativeDepthRenderBuffer = (System.IntPtr)0;
			if (activeTexture!=null)
			{
				unityViewStruct.nativeColourRenderBuffer = activeTexture.colorBuffer.GetNativeRenderBufferPtr();
				if (!editorMode )
					unityViewStruct.nativeDepthRenderBuffer = activeTexture.depthBuffer.GetNativeRenderBufferPtr();
			}
			Marshal.StructureToPtr(unityViewStruct, unityViewStructPtr, true);
			mainCommandBuffer.IssuePluginEventAndData(UnityGetRenderEventFuncWithData(),TRUESKY_EVENT_ID + cbuf_view_id, unityViewStructPtr);
			post_translucent_buf.IssuePluginEventAndData(UnityGetPostTranslucentFuncWithData(), TRUESKY_EVENT_ID + cbuf_view_id, unityViewStructPtr);
			overlay_buf.IssuePluginEventAndData(UnityGetOverlayFuncWithData(), TRUESKY_EVENT_ID + cbuf_view_id, unityViewStructPtr);

			//src.SetupCameraProperties(cam);
			src.ExecuteCommandBuffer(mainCommandBuffer);
			src.ExecuteCommandBuffer(post_translucent_buf);
			src.ExecuteCommandBuffer(overlay_buf);
		}
		int duplicateFrames = 0;
		int localFrameCount = 0;
		void HDRPPostRender(Camera cam)
		{
			activeTexture = cam.activeTexture;
		}

		void PrepareMatrices()
		{
			Viewport[] targetViewports	= new Viewport[3];
			RenderStyle renderStyle		= GetRenderStyle();
			int view_id 				= InternalGetViewId();

			if (view_id >= 0)
			{
				Camera cam = GetComponent<Camera>();

				// View and projection: non-stereo rendering
				Matrix4x4 m = cam.worldToCameraMatrix;
				bool toTexture = cam.allowHDR || cam.allowMSAA || cam.renderingPath == RenderingPath.DeferredShading || cam.targetTexture;
				Matrix4x4 p = GL.GetGPUProjectionMatrix(cam.projectionMatrix, toTexture);

				ViewMatrixToTrueSkyFormat(renderStyle, m, viewMatrices);
				ProjMatrixToTrueSkyFormat(renderStyle, p, projMatrices);

				if ((renderStyle & RenderStyle.VR_STYLE) == RenderStyle.VR_STYLE)
				{
					// View matrix: left & right eyes
					Matrix4x4 l = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
					Matrix4x4 r = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
					ViewMatrixToTrueSkyFormat(renderStyle, l, viewMatrices, 1);
					ViewMatrixToTrueSkyFormat(renderStyle, r, viewMatrices, 2);

					// Projection matrix: left & right eyes
					Matrix4x4 pl = GL.GetGPUProjectionMatrix(cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left), true);
					Matrix4x4 pr = GL.GetGPUProjectionMatrix(cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), true);
					ProjMatrixToTrueSkyFormat(renderStyle, pl, projMatrices, 1);
					ProjMatrixToTrueSkyFormat(renderStyle, pr, projMatrices, 2);
				}

				ProjMatrixToTrueSkyFormat(RenderStyle.UNITY_STYLE, p, overlayProjMatrix);

                // Query depth size
                int depthWidth      = cam.pixelWidth;
                int depthHeight     = cam.pixelHeight; 

				depthViewports[0].x = depthViewports[0].y = 0;
                depthViewports[0].z = depthWidth;
                depthViewports[0].w = depthHeight;

				// There are now three viewports. 1 and 2 are for left and right eyes in VR.
				targetViewports[0].x = targetViewports[0].y = 0;
				if (cam.actualRenderingPath != RenderingPath.DeferredLighting &&
					cam.actualRenderingPath != RenderingPath.DeferredShading)
				{
					Vector3 screen_0        = cam.ViewportToScreenPoint(new Vector3(0,0,0));
					targetViewports[0].x    = (int)(screen_0.x);
					targetViewports[0].y    = (int)(screen_0.y);
				}
				for (int i = 0; i < 3; i++)
				{
					targetViewports[i].w        = depthWidth;
					targetViewports[i].h        = depthHeight;
				}

#if !UNITY_SWITCH
				// If we are doing XR we need to setup the additional viewports
				if ((renderStyle & RenderStyle.VR_STYLE) == RenderStyle.VR_STYLE)
				{
					int fullEyeWidth = UnityEngine.XR.XRSettings.eyeTextureDesc.width;
					int halfEyeWidth = fullEyeWidth / 2;
					int eyeHeight = UnityEngine.XR.XRSettings.eyeTextureDesc.height;

					// This is the viewport that we reset to (default vp):
                    // it must cover all the texture
                    depthViewports[0].x = targetViewports[0].x = 0;
                    depthViewports[0].y = targetViewports[0].y = 0;
                    depthViewports[0].z = targetViewports[0].w = fullEyeWidth;
                    depthViewports[0].w = targetViewports[0].h = eyeHeight;

					// Left eye viewports
					depthViewports[1].x = targetViewports[1].x = 0;
					depthViewports[1].y = targetViewports[1].y = 0;
					depthViewports[1].z = targetViewports[1].w = halfEyeWidth;
					depthViewports[1].w = targetViewports[1].h = eyeHeight;

					// Right eye viewports
					depthViewports[2].x = targetViewports[2].x = halfEyeWidth;
					depthViewports[2].y = targetViewports[2].y = 0;
					depthViewports[2].z = targetViewports[2].w = halfEyeWidth;
					depthViewports[2].w = targetViewports[2].h = eyeHeight;
				}
#endif
				UnityRenderOptions unityRenderOptions = UnityRenderOptions.DEFAULT;
				if (FlipOverlays)
					unityRenderOptions = unityRenderOptions | UnityRenderOptions.FLIP_OVERLAYS;
                if (ShareBuffersForVR)
                    unityRenderOptions = unityRenderOptions | UnityRenderOptions.NO_SEPARATION;

				UnitySetRenderFrameValues(view_id
					, viewMatrices
					, projMatrices
					, overlayProjMatrix
					, editorMode ? depthTexture.GetNative() : (System.IntPtr)0
					, depthViewports
					, targetViewports
					, renderStyle
					, exposure
					, gamma
					, Time.renderedFrameCount
					, unityRenderOptions
					, Graphics.activeColorBuffer.GetNativeRenderBufferPtr());


				unityViewStruct.view_id= view_id;
				unityViewStruct.framenumber=Time.renderedFrameCount;
				unityViewStruct.exposure=exposure;
				unityViewStruct.gamma=gamma;
				unityViewStruct.viewMatrices4x4=viewMatrices;
				unityViewStruct.projMatrices4x4=projMatrices;
				unityViewStruct.overlayProjMatrix4x4=overlayProjMatrix;
				unityViewStruct.depthTexture = editorMode ? depthTexture.GetNative() : (System.IntPtr)0;
				unityViewStruct.depthViewports= depthViewports;
				unityViewStruct.targetViewports=targetViewports;
				unityViewStruct.renderStyle=renderStyle;
				unityViewStruct.unityRenderOptions=unityRenderOptions;
				unityViewStruct.colourTexture= Graphics.activeColorBuffer.GetNativeRenderBufferPtr();


				lastFrameCount = Time.renderedFrameCount;
				_inscatterRT.renderTexture = inscatterRT;
				_cloudVisibilityRT.renderTexture = cloudVisibilityRT;
				_cloudShadowRT.renderTexture = cloudShadowRT;

				_lossRT.renderTexture = lossRT;
				StaticSetRenderTexture("inscatter2D", _inscatterRT.GetNative());
				StaticSetRenderTexture("Loss2D", _lossRT.GetNative());
				StaticSetRenderTexture("CloudVisibilityRT", _cloudVisibilityRT.GetNative());
				if (reflectionProbeTexture.renderTexture)
				{
					StaticSetRenderTexture("Cubemap", reflectionProbeTexture.GetNative());
				}
				StaticSetRenderTexture("CloudShadowRT", _cloudShadowRT.GetNative());
				MatrixTransform(cubemapTransformMatrix);
				StaticSetMatrix4x4("CubemapTransform", cubemapTransformMatrix);

				if (RainDepthCamera != null)
					_rainDepthRT.renderTexture = RainDepthCamera.targetTexture;
				StaticSetRenderTexture("RainDepthTexture", _rainDepthRT.GetNative());
				if (RainDepthCamera != null)
				{
					ViewMatrixToTrueSkyFormat(RenderStyle.UNITY_STYLE, RainDepthCamera.matrix, rainDepthMatrix, 0, true);
					rainDepthTextureScale = 1.0F;// DepthCamera.farClipPlane;
					StaticSetMatrix4x4("RainDepthTransform", rainDepthMatrix);
					StaticSetMatrix4x4("RainDepthProjection", rainDepthProjection);
					StaticSetRenderFloat("RainDepthTextureScale", rainDepthTextureScale);
				}
			}
		}

		void PrepareDepthMaterial()
		{
			RenderStyle renderStyle = GetRenderStyle();
			depthMaterial           = null;
            Camera cam              = GetComponent<Camera>();
            bool toTexture          = cam.allowHDR || cam.allowMSAA || cam.renderingPath == RenderingPath.DeferredShading;
            if (!toTexture && (renderStyle & RenderStyle.UNITY_STYLE_DEFERRED) != RenderStyle.UNITY_STYLE_DEFERRED)
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
		}
	}
}