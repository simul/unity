using UnityEngine;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Collections.Generic;

using static simul.TrueSkyPluginRenderFunctionImporter;
using static simul.TrueSkyCameraBase;

namespace simul
{
	[ExecuteInEditMode]
	public class TrueSkyCamera : TrueSkyCameraBase
	{
		int lastFrameCount =-1;

		protected float[] cubemapTransformMatrix = new float[16];
		protected float[] rainDepthMatrix = new float[16];
		protected float[] rainDepthProjection = new float[16];
		protected float rainDepthTextureScale;
		protected override int InternalGetViewId()
		{
			return StaticGetOrAddView((System.IntPtr)view_ident);
		}
		// We will STORE the activeTexture from the camera and hope it's valid next frame.
		RenderTexture activeTexture = null;
		RenderBuffer activeColourBuffer ;
		public bool FlipOverlays				= false;
		public RenderTexture showDepthTexture	= null;

		RenderTextureHolder _rainDepthRT		= new RenderTextureHolder();
		protected RenderTextureHolder reflectionProbeTexture = new RenderTextureHolder();
		protected CommandBuffer overlay_buf 				= null;
		protected CommandBuffer post_translucent_buf		= null;
		protected CommandBuffer ui_buf						= null;
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
#if !UNITY_GAMECORE 
#if !UNITY_SWITCH 
			UnityEngine.XR.XRSettings.showDeviceView = true;
#endif
#endif
			RenderStyle r = base.GetRenderStyle();
			if (trueSKY.GetTrueSky() && trueSKY.GetTrueSky().DepthBlending)
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
#if UNITY_SWITCH || UNITY_GAMECORE
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

		void OnEnable()
		{
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
		}

		void RemoveCommandBuffers()
		{
			RemoveBuffer("trueSKY store state");
			RemoveBuffer("trueSKY render");
			RemoveBuffer("trueSKY depth");
			RemoveBuffer("trueSKY overlay");
			RemoveBuffer("trueSKY post translucent");
			RemoveBuffer("trueSKY depth blit");
			RemoveBuffer("trueSKY UI");
			RemoveBuffer("trueSKY deferred contexts");
		}
		UnityViewStruct unityViewStruct=new UnityViewStruct();
		System.IntPtr unityViewStructPtr = Marshal.AllocHGlobal(Marshal.SizeOf(new UnityViewStruct()));
		System.IntPtr postTransViewStructPtr = Marshal.AllocHGlobal(Marshal.SizeOf(new UnityViewStruct())); 
		System.IntPtr overlayViewStructPtr = Marshal.AllocHGlobal(Marshal.SizeOf(new UnityViewStruct())); 
		System.IntPtr EditorUIStructPtr = Marshal.AllocHGlobal(Marshal.SizeOf(new UnityViewStruct())); 
		void OnPreRender()
		{
			if (!enabled || !gameObject.activeInHierarchy)
			{
				UnityEngine.Debug.Log("Failed to draw");
				return;
			}
			if (trueSKY.GetTrueSky()==null)
				return;
			GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
			PreRender();
			Camera cam = GetComponent<Camera>();
			if (mainCommandBuffer == null)
			{
				RemoveCommandBuffers();
				mainCommandBuffer = new CommandBuffer();
				mainCommandBuffer.name = "trueSKY render";
				overlay_buf = new CommandBuffer();
				overlay_buf.name = "trueSKY overlay";
				post_translucent_buf = new CommandBuffer();
				post_translucent_buf.name = "trueSKY post translucent";
				deferred_buf = new CommandBuffer();
				deferred_buf.name = "trueSKY deferred contexts";
				blitbuf = new CommandBuffer();
                blitbuf.name = "trueSKY depth blit";
                ui_buf = new CommandBuffer();
                ui_buf.name = "trueSKY UI";
				

				cbuf_view_id = -1;
			}
			if (cbuf_view_id != InternalGetViewId())
			{
				cam.RemoveCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
				cam.RemoveCommandBuffers(CameraEvent.AfterForwardAlpha);
				cam.RemoveCommandBuffers(CameraEvent.AfterEverything);
			}
			CommandBuffer[] bufs = cam.GetCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
			//if(editorMode)
				PrepareDepthMaterial();
			bool do_overlays=false;
			bool do_post_trans=true;
			bool do_editor_UI=true;
			int requiredNumber = 2+(do_overlays?1:0) + (do_post_trans ? 1 : 0)+(editorMode?1:0)+ (do_editor_UI ? 1 : 0);
			if (bufs.Length != requiredNumber)
			{
				RemoveCommandBuffers();
				if(editorMode)
					cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, blitbuf);
				cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, mainCommandBuffer);
				if (do_post_trans)
					cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, post_translucent_buf);
				if(do_overlays)
					cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, overlay_buf);
				//if (editorMode)
				cam.AddCommandBuffer(CameraEvent.AfterEverything, deferred_buf); 
				if(do_editor_UI)
					cam.AddCommandBuffer(CameraEvent.AfterEverything, ui_buf); 
			}
			mainCommandBuffer.Clear();
			blitbuf.Clear();
			overlay_buf.Clear();
			post_translucent_buf.Clear();
			deferred_buf.Clear();
			ui_buf.Clear();
            cbuf_view_id = InternalGetViewId();
			//if (editorMode)
			{
				/*blitbuf.SetRenderTarget((RenderTexture)depthTexture.renderTexture);
				blitbuf.DrawProcedural(Matrix4x4.identity, depthMaterial, 0, MeshTopology.Triangles, 6);
				blitbuf.SetRenderTarget(Graphics.activeColorBuffer);*/
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
			if (activeTexture != null)
			{
				unityViewStruct.nativeColourRenderBuffer = activeTexture.colorBuffer.GetNativeRenderBufferPtr();
				unityViewStruct.nativeDepthRenderBuffer = activeTexture.depthBuffer.GetNativeRenderBufferPtr();
				unityViewStruct.colourResourceState = activeTexture.antiAliasing > 1 ? ResourceState.ResolveSource : ResourceState.RenderTarget;
				unityViewStruct.depthResourceState = ResourceState.DepthWrite;
				unityViewStruct.colourTextureArrayIndex = 0;
			}
			else
			{
				unityViewStruct.externalDepthTexture = depthTexture.renderTexture.GetNativeTexturePtr();
				unityViewStruct.colourTexture = Graphics.activeColorBuffer.GetNativeRenderBufferPtr();
				unityViewStruct.depthResourceState = ResourceState.DepthWrite;
				unityViewStruct.colourResourceState = ResourceState.ResolveSource;
				unityViewStruct.colourTextureArrayIndex = 0;
				//unityViewStruct.colourTexture = cam.
				//unityViewStruct.colourTexture = cam.activeTexture.GetNativeTexturePtr();
				//return;
			}
            trueSKY ts = trueSKY.GetTrueSky();
            bool il2cppScripting = UsingIL2CPP();
			Marshal.StructureToPtr(unityViewStruct, unityViewStructPtr, !il2cppScripting);
			mainCommandBuffer.IssuePluginEventAndData(UnityGetRenderEventFuncWithData(), TRUESKY_EVENT_ID + cbuf_view_id, unityViewStructPtr);
			var renderStyle= unityViewStruct.renderStyle;
			unityViewStruct.renderStyle=renderStyle|RenderStyle.POST_TRANSLUCENT;
			Marshal.StructureToPtr(unityViewStruct, postTransViewStructPtr, !il2cppScripting);
			unityViewStruct.renderStyle = renderStyle | RenderStyle.UNITY_STYLE| RenderStyle.DRAW_OVERLAYS;
			post_translucent_buf.IssuePluginEventAndData(UnityGetPostTranslucentFuncWithData(), TRUESKY_EVENT_ID + cbuf_view_id, postTransViewStructPtr);
			Marshal.StructureToPtr(unityViewStruct, overlayViewStructPtr, !il2cppScripting);
			overlay_buf.IssuePluginEventAndData(UnityGetOverlayFuncWithData(), TRUESKY_EVENT_ID + cbuf_view_id, overlayViewStructPtr);




			if (ts.GlobalViewTexture.renderTexture)
			{            
                unityViewStruct.nativeColourRenderBuffer = ts.GlobalViewTexture.renderTexture.colorBuffer.GetNativeRenderBufferPtr();
                unityViewStruct.nativeDepthRenderBuffer = (System.IntPtr)0;
                unityViewStruct.colourResourceState = ResourceState.RenderTarget;
                unityViewStruct.gamma = 1.0f;
				unityViewStruct.exposure = 1.0f;
				unityViewStruct.framenumber = UIFramenumber;

                unityViewStruct.targetViewports[0].x = 0;
                unityViewStruct.targetViewports[0].y = 0;
                unityViewStruct.targetViewports[0].w = ts.GlobalViewTexture.renderTexture.width;
                unityViewStruct.targetViewports[0].h = ts.GlobalViewTexture.renderTexture.height;
                unityViewStruct.renderStyle = RenderStyle.UNITY_STYLE | RenderStyle.DRAW_PROPERTIES_UI | RenderStyle.CLEAR_SCREEN;
				Marshal.StructureToPtr(unityViewStruct, EditorUIStructPtr, !il2cppScripting);
				//ui_buf.IssuePluginEventAndData(UnityGetEditorUIFuncWithData(), TRUESKY_EVENT_ID + cbuf_view_id + 5, EditorUIStructPtr);
				UIFramenumber++;

            }

        }
		int duplicateFrames = 0;
		int localFrameCount = 0;
		void OnPostRender()
		{
			Camera cam = GetComponent<Camera>();
			activeTexture = cam.activeTexture;
			
			activeColourBuffer = Display.displays[cam.targetDisplay].colorBuffer;// Graphics.activeColorBuffer;
		}

		void PrepareMatrices()
		{
			Viewport[] targetViewports	= new Viewport[3];
			RenderStyle renderStyle		= GetRenderStyle();
			int view_id 				= InternalGetViewId();
			trueSKY ts					= trueSKY.GetTrueSky();
			if (view_id >= 0)
			{
				Camera cam = GetComponent<Camera>();

				// View and projection: non-stereo rendering
				Matrix4x4 m = cam.worldToCameraMatrix;
				bool toTexture = cam.allowHDR
								|| (QualitySettings.antiAliasing > 0 && cam.allowMSAA)
								|| cam.actualRenderingPath == RenderingPath.DeferredShading
								|| cam.actualRenderingPath == RenderingPath.DeferredLighting;
								//|| (activeTexture != null);

				Matrix4x4 p = GL.GetGPUProjectionMatrix(cam.projectionMatrix, toTexture);

				ViewMatrixToTrueSkyFormat(m, viewMatrices);
				ProjMatrixToTrueSkyFormat(renderStyle, p, projMatrices);

				if ((renderStyle & RenderStyle.VR_STYLE) == RenderStyle.VR_STYLE)
				{
					// View matrix: left & right eyes
					Matrix4x4 l = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
					Matrix4x4 r = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
					ViewMatrixToTrueSkyFormat(l, viewMatrices, 1);
					ViewMatrixToTrueSkyFormat(r, viewMatrices, 2);

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

#if !UNITY_GAMECORE
#if !UNITY_SWITCH
				// If we are doing XR we need to setup the additional viewports
				if ((renderStyle & RenderStyle.VR_STYLE) == RenderStyle.VR_STYLE)
				{
					if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePass)
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
					else if(UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass
					|| UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
					{
						int fullEyeWidth = UnityEngine.XR.XRSettings.eyeTextureDesc.width;
						int eyeHeight = UnityEngine.XR.XRSettings.eyeTextureDesc.height;

						// This is the viewport that we reset to (default vp):
						// it must cover all the texture
						depthViewports[0].x = targetViewports[0].x = 0;
						depthViewports[0].y = targetViewports[0].y = 0;
						depthViewports[0].z = targetViewports[0].w = fullEyeWidth * 2;
						depthViewports[0].w = targetViewports[0].h = eyeHeight;

						// Left eye viewports
						depthViewports[1].x = targetViewports[1].x = 0;
						depthViewports[1].y = targetViewports[1].y = 0;
						depthViewports[1].z = targetViewports[1].w = fullEyeWidth;
						depthViewports[1].w = targetViewports[1].h = eyeHeight;

						// Right eye viewports
						depthViewports[2].x = targetViewports[2].x = fullEyeWidth;
						depthViewports[2].y = targetViewports[2].y = 0;
						depthViewports[2].z = targetViewports[2].w = fullEyeWidth;
						depthViewports[2].w = targetViewports[2].h = eyeHeight;
					}
				}
#endif
#endif
				UnityRenderOptions unityRenderOptions = UnityRenderOptions.DEFAULT;
				if (FlipOverlays)
					unityRenderOptions = unityRenderOptions | UnityRenderOptions.FLIP_OVERLAYS;
				if (ShareBuffersForVR)
					unityRenderOptions = unityRenderOptions | UnityRenderOptions.NO_SEPARATION;


				unityViewStruct.view_id= view_id;
				unityViewStruct.framenumber=Time.renderedFrameCount;
				unityViewStruct.exposure=exposure;
				unityViewStruct.gamma=gamma;
				unityViewStruct.viewMatrices4x4=viewMatrices;
				unityViewStruct.projMatrices4x4=projMatrices;
				unityViewStruct.overlayProjMatrix4x4=overlayProjMatrix;
				unityViewStruct.depthTexture = depthTexture.GetNative();
				unityViewStruct.depthViewports= depthViewports;
				unityViewStruct.targetViewports=targetViewports;
				unityViewStruct.renderStyle=renderStyle;
				unityViewStruct.unityRenderOptions=unityRenderOptions;
				unityViewStruct.colourTextureArrayIndex = -1;

				lastFrameCount = Time.renderedFrameCount;
				InitExternalTexture(ref ts.InscatterTexture.externalTexture, ts.inscatterRT);
				ts.InscatterTexture.renderTexture = ts.inscatterRT;
                InitExternalTexture(ref ts.InscatterTexture.externalTexture, ts.inscatterRT);
                ts.LossTexture.renderTexture = ts.lossRT;
                InitExternalTexture(ref ts.LossTexture.externalTexture, ts.inscatterRT);
                ts.CloudVisibilityTexture.renderTexture = ts.cloudVisibilityRT;
                InitExternalTexture(ref ts.CloudVisibilityTexture.externalTexture, ts.inscatterRT);
                ts.CloudShadowTexture.renderTexture = ts.cloudShadowRT;
				//InitExternalTexture(ref ts.CloudShadowTexture.externalTexture, ts.inscatterRT);
				//ts.GlobalViewTexture.renderTexture = ts.globalViewRT;



                Marshal.StructureToPtr(ts.InscatterTexture.externalTexture, ts.InscatterTexture.GetExternalTexturePtr(), !trueSKY.GetTrueSky().UsingIL2CPP);
                StaticSetRenderTexture2("inscatter2D", ts.InscatterTexture.GetExternalTexturePtr());
                Marshal.StructureToPtr(ts.LossTexture.externalTexture, ts.LossTexture.GetExternalTexturePtr(), !trueSKY.GetTrueSky().UsingIL2CPP);
                StaticSetRenderTexture2("Loss2D", ts.LossTexture.GetExternalTexturePtr());
                Marshal.StructureToPtr(ts.CloudVisibilityTexture.externalTexture, ts.CloudVisibilityTexture.GetExternalTexturePtr(), !trueSKY.GetTrueSky().UsingIL2CPP);
                StaticSetRenderTexture2("CloudVisibilityRT", ts.CloudVisibilityTexture.GetExternalTexturePtr());
                Marshal.StructureToPtr(ts.CloudShadowTexture.externalTexture, ts.CloudShadowTexture.GetExternalTexturePtr(), !trueSKY.GetTrueSky().UsingIL2CPP);
                StaticSetRenderTexture2("CloudShadowRT", ts.CloudShadowTexture.GetExternalTexturePtr());  
				//Marshal.StructureToPtr(ts.GlobalViewTexture.externalTexture, ts.GlobalViewTexture.GetExternalTexturePtr(), !trueSKY.GetTrueSky().UsingIL2CPP);
               // StaticSetRenderTexture2("GlobalViewRT", ts.GlobalViewTexture.GetExternalTexturePtr());

                if (reflectionProbeTexture.renderTexture)
				{
					//StaticSetRenderTexture2("Cubemap", reflectionProbeTexture.GetNative());
				}
				
				MatrixTransform(cubemapTransformMatrix);
				StaticSetMatrix4x4("CubemapTransform", cubemapTransformMatrix);

				if (RainDepthCamera != null)
					_rainDepthRT.renderTexture = RainDepthCamera.targetTexture;
               //StaticSetRenderTexture2("RainDepthTexture", _rainDepthRT.GetNative());
				if (RainDepthCamera != null)
				{
					ViewMatrixToTrueSkyFormat(RainDepthCamera.matrix, rainDepthMatrix, 0, true);
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
			Camera cam = GetComponent<Camera>();
			//SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR)
			bool toTexture = (HDROutputSettings.main.active && cam.allowHDR)
								|| (QualitySettings.antiAliasing > 0 && cam.allowMSAA)
								|| cam.actualRenderingPath == RenderingPath.DeferredShading
								|| cam.actualRenderingPath == RenderingPath.DeferredLighting
								|| cam.targetTexture;

			if (!toTexture)
			{
				
			}
			else
			{

			}
		}
	}
}