#if USING_HDRP
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

using static simul.TrueSkyPluginRenderFunctionImporter;
using static simul.TrueSkyCameraBase;

delegate void PFN_RenderCubemapFace(int x);

namespace simul
{
	public class TrueSkyHDRPCustomPass : CustomPass
	{
		public TrueSkyHDRPCustomPass()
		{
			view_ident = last_view_ident + 1;
			last_view_ident++;
		}
		~TrueSkyHDRPCustomPass()
		{
			StaticRemoveView(view_id);
		}

		UnityViewStruct unityViewStruct;
		System.IntPtr unityViewStructPtr;

		int lastFrameCount = -1;
		protected int cbuf_view_id = -1;
		protected int view_ident = 0;
		protected int view_id = -1;
		protected static int last_view_ident = 0;

		protected int InternalGetViewId()
		{
			return StaticGetOrAddView((System.IntPtr)view_ident);
		}
		protected override void Setup(ScriptableRenderContext src, CommandBuffer cmd)
		{
			ts = trueSKY.GetTrueSky();
			tsValid = true;

			viewMatrices = new float[48];
			projMatrices = new float[48];
			depthViewports = new int4[3];
			overlayProjMatrix = new float[16];

			depthTexture = new RenderTextureHolder();

			unityViewStruct = new UnityViewStruct();
			unityViewStructPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UnityViewStruct)));
		}

#if UNITY_2020_2_OR_NEWER
		protected override void Execute(CustomPassContext ctx)
		{
			ScriptableRenderContext src = ctx.renderContext;
			CommandBuffer cmd = ctx.cmd;
			HDCamera camera = ctx.hdCamera;
			CullingResults cullingResult = ctx.cullingResults;

			RTHandle colour = ctx.cameraColorBuffer;
			RTHandle depth = ctx.cameraDepthBuffer;

			InternalExecute(src, cmd, camera, cullingResult, colour, depth);
		}
#else
		protected virtual void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
		{
			RTHandle colour, depth;
			GetCameraBuffers(out colour, out depth);

			InternalExecute(renderContext, cmd, hdCamera, cullingResult, colour, depth);
		}
#endif

		private void InternalExecute(ScriptableRenderContext src, CommandBuffer cmd, HDCamera camera, CullingResults cullingResult, RTHandle colour, RTHandle depth)
		{
			bool mainCamera = camera.camera.tag.Equals("MainCamera"); //Do we want to force trueSKY to only render in Main Camera?
			bool cubemapProbe = camera.camera.name.Equals("TrueSkyCubemapProbe"); //If we are hiding this then it might be ok. But using the set name isn't great.

			//Don't draw to the scene view. This should never be removed!
			if (camera.camera.cameraType == CameraType.SceneView)
				return;

			if (camera.camera.gameObject.layer != trueSKY.GetTrueSky().trueSKYLayerIndex && (mainCamera || cubemapProbe))
				return;

			//Fill-in UnityViewStruct
			PrepareMatrices(camera);

			//Set up Render Targets
			RenderBuffer rbColour = colour.rt.colorBuffer;
			RenderBuffer rbDepth = depth.rt.depthBuffer;
			bool msaa = (colour.rt.antiAliasing > 1);
			if (cubemapProbe && colour.rt.width != colour.rt.height)
			{
				if (camera.camera.activeTexture == null)
					return;
				//Unity has set the wrong buffers from the CustomPass, and we should use the ones within the camera.
				rbColour = camera.camera.activeTexture.colorBuffer;
				rbDepth = camera.camera.activeTexture.depthBuffer;
			}

			unityViewStruct.nativeColourRenderBuffer = rbColour.GetNativeRenderBufferPtr();
			unityViewStruct.nativeDepthRenderBuffer = rbDepth.GetNativeRenderBufferPtr();
			unityViewStruct.colourResourceState = msaa ? ResourceState.ResolveSource : ResourceState.RenderTarget;
			unityViewStruct.depthResourceState = ResourceState.DepthWrite;
			
			//Execute CmdBuffer
			cbuf_view_id = InternalGetViewId();
			
			if (mainCamera) //Main view render
			{
				bool il2cppScripting = simul.trueSKY.GetTrueSky().UsingIL2CPP;
				Marshal.StructureToPtr(unityViewStruct, unityViewStructPtr, !il2cppScripting);
#if UNITY_PS5
				PrepareTestMaterial();
					// Draw quad on current rt. This SEEMS to be needed to force unity to activate its rendertarget/depth target. Sadly.
					cmd.DrawProcedural(Matrix4x4.identity, testMaterial, 0, MeshTopology.Quads, 4);
#endif
				if (injectionPoint == CustomPassInjectionPoint.BeforePreRefraction)
					cmd.IssuePluginEventAndData(UnityGetRenderEventFuncWithData(), GetTRUESKY_EVENT_ID() + cbuf_view_id, unityViewStructPtr);
				else if (injectionPoint == CustomPassInjectionPoint.BeforePostProcess)
					cmd.IssuePluginEventAndData(UnityGetPostTranslucentFuncWithData(), GetTRUESKY_EVENT_ID() + cbuf_view_id, unityViewStructPtr);
				else if (injectionPoint == CustomPassInjectionPoint.AfterPostProcess)
					cmd.IssuePluginEventAndData(UnityGetOverlayFuncWithData(), GetTRUESKY_EVENT_ID() + cbuf_view_id, unityViewStructPtr);
				else
					return;
			}
			if(cubemapProbe) //Cubemap view render
			{
				if (injectionPoint == CustomPassInjectionPoint.BeforePreRefraction)
				{
					unityViewStruct.renderStyle |= RenderStyle.CUBEMAP_STYLE;
					unityViewStruct.exposure = GameObject.FindObjectOfType<TrueSkyCubemapProbe>().exposure;
					unityViewStruct.gamma = GameObject.FindObjectOfType<TrueSkyCubemapProbe>().gamma;

					PFN_RenderCubemapFace RenderCubemapFace = _faceMask =>
					{
						UpdateViewMatricsForCubemapFace(camera, _faceMask, GameObject.FindObjectOfType<TrueSkyCubemapProbe>().flipProbeY);
						unityViewStruct.colourTextureArrayIndex = (int)ToCubemapFace(_faceMask);

						bool il2cppScripting = simul.trueSKY.GetTrueSky().UsingIL2CPP;
						Marshal.StructureToPtr(unityViewStruct, unityViewStructPtr, !il2cppScripting);

						cmd.SetRenderTarget(rbColour, 0, ToCubemapFace(_faceMask), 0);
						cmd.ClearRenderTarget(true, true, new Color(0.0F, 0.0F, 0.0F, 1.0F), 1.0F);
						cmd.IssuePluginEventAndData(UnityGetRenderEventFuncWithData(), GetTRUESKY_EVENT_ID() + cbuf_view_id, unityViewStructPtr);
					};

					int faceMask = GameObject.FindObjectOfType<TrueSkyCubemapProbe>().GetFaceMask();
					if (faceMask == 63)
					{
						for (int i = 0; i < 6; i++)
						{
							int _faceMask = 1 << i;
							RenderCubemapFace(_faceMask);
						}
					}
					else
					{
						RenderCubemapFace(faceMask);
					}
				}
				else
					return;
			}
		}

#if UNITY_PS5
		Material testMaterial=null;
		Shader testQuadShader=null;
		void PrepareTestMaterial()
		{
			if (testMaterial == null)
			{
				testQuadShader = Resources.Load("TestQuadShader", typeof(Shader)) as Shader;
				if (testQuadShader != null)
					testMaterial = new Material(testQuadShader);
			}
		}
#endif
		private void UpdateViewMatricsForCubemapFace(HDCamera camera, int faceMask, bool flipProbeY)
		{
			Vector3 positive_y = new Vector3(0.0f, 1.0f, 0.0f);
			Vector3 positive_x = new Vector3(1.0f, 0.0f, 0.0f);
			Matrix4x4 m = camera.camera.worldToCameraMatrix;
			switch (faceMask)
			{
				case 1:
						m *= Matrix4x4.Rotate(Quaternion.AngleAxis(270.0f, positive_y)); break;
				case 2:
						m *= Matrix4x4.Rotate(Quaternion.AngleAxis(090.0f, positive_y)); break;
				case 4:
						m *= Matrix4x4.Rotate(Quaternion.AngleAxis(090.0f, positive_x)); break;
				case 8:
						m *= Matrix4x4.Rotate(Quaternion.AngleAxis(270.0f, positive_x)); break;
				case 16:
						break;
				case 32:
						m *= Matrix4x4.Rotate(Quaternion.AngleAxis(180.0f, positive_y)); break;
				default:
					return;
			}
			ViewMatrixToTrueSkyFormat_HDRP(GetRenderStyle(camera.camera), m, viewMatrices);
			unityViewStruct.viewMatrices4x4 = viewMatrices;

			if (flipProbeY)
			{
				Matrix4x4 p = camera.camera.projectionMatrix;
				p[1, 1] = -1.0f;

				ProjMatrixToTrueSkyFormat_HDRP(GetRenderStyle(camera.camera), p, projMatrices);
				unityViewStruct.projMatrices4x4 = projMatrices;
			}
		}
		CubemapFace ToCubemapFace(int faceMask)
		{
			return (CubemapFace)Mathf.Log(faceMask, 2);
		}

		protected override void Cleanup()
		{
			tsValid = false;
		}

		protected float[] viewMatrices;
		protected float[] projMatrices;
		protected int4[] depthViewports;
		protected float[] overlayProjMatrix;

		public bool FlipOverlays = false;
		public float exposure = 0.5F;
		public float gamma = 0.5F;
		public bool flippedView = false;
		public bool ShareBuffersForVR = true;

		protected RenderTextureHolder depthTexture;

		void PrepareMatrices(HDCamera camera)
		{
			Camera cam = camera.camera;
			Viewport[] targetViewports = new Viewport[3];
			RenderStyle renderStyle = GetRenderStyle(cam);
			int view_id = InternalGetViewId();

			if (view_id >= 0)
			{

				// View and projection: non-stereo rendering
				Matrix4x4 m = cam.worldToCameraMatrix;
				bool toTexture = cam.allowHDR || cam.allowMSAA || cam.renderingPath == RenderingPath.DeferredShading || cam.targetTexture;
				Matrix4x4 p = GL.GetGPUProjectionMatrix(cam.projectionMatrix, toTexture);

				ViewMatrixToTrueSkyFormat_HDRP(renderStyle, m, viewMatrices);
				ProjMatrixToTrueSkyFormat_HDRP(renderStyle, p, projMatrices);

				if ((renderStyle & RenderStyle.VR_STYLE) == RenderStyle.VR_STYLE)
				{
					// View matrix: left & right eyes
					Matrix4x4 l = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
					Matrix4x4 r = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
					ViewMatrixToTrueSkyFormat_HDRP(renderStyle, l, viewMatrices, 1);
					ViewMatrixToTrueSkyFormat_HDRP(renderStyle, r, viewMatrices, 2);

					// Projection matrix: left & right eyes
					Matrix4x4 pl = GL.GetGPUProjectionMatrix(cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left), true);
					Matrix4x4 pr = GL.GetGPUProjectionMatrix(cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), true);
					ProjMatrixToTrueSkyFormat_HDRP(renderStyle, pl, projMatrices, 1);
					ProjMatrixToTrueSkyFormat_HDRP(renderStyle, pr, projMatrices, 2);
				}

				ProjMatrixToTrueSkyFormat_HDRP(RenderStyle.UNITY_STYLE, p, overlayProjMatrix);

				// Query depth size
				int depthWidth = cam.pixelWidth;
				int depthHeight = cam.pixelHeight;

				depthViewports[0].x = depthViewports[0].y = 0;
				depthViewports[0].z = depthWidth;
				depthViewports[0].w = depthHeight;

				// There are now three viewports. 1 and 2 are for left and right eyes in VR.
				targetViewports[0].x = targetViewports[0].y = 0;
				if (cam.actualRenderingPath != RenderingPath.DeferredLighting &&
					cam.actualRenderingPath != RenderingPath.DeferredShading)
				{
					Vector3 screen_0 = cam.ViewportToScreenPoint(new Vector3(0, 0, 0));
					targetViewports[0].x = (int)(screen_0.x);
					targetViewports[0].y = (int)(screen_0.y);
				}
				for (int i = 0; i < 3; i++)
				{
					targetViewports[i].w = depthWidth;
					targetViewports[i].h = depthHeight;
				}

#if !UNITY_GAMECORE
#if !UNITY_SWITCH
				// If we are doing XR we need to setup the additional viewports
				if ((renderStyle & RenderStyle.VR_STYLE) == RenderStyle.VR_STYLE)
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
#endif
#endif

				UnityRenderOptions unityRenderOptions = UnityRenderOptions.DEFAULT;
				if (FlipOverlays)
					unityRenderOptions = unityRenderOptions | UnityRenderOptions.FLIP_OVERLAYS;
				if (ShareBuffersForVR)
					unityRenderOptions = unityRenderOptions | UnityRenderOptions.NO_SEPARATION;

				


				unityViewStruct.view_id = view_id;
				unityViewStruct.framenumber = Time.renderedFrameCount;
				unityViewStruct.exposure = exposure;
				unityViewStruct.gamma = gamma;
				unityViewStruct.viewMatrices4x4 = viewMatrices;
				unityViewStruct.projMatrices4x4 = projMatrices;
				unityViewStruct.overlayProjMatrix4x4 = overlayProjMatrix;
				unityViewStruct.depthTexture = depthTexture.GetNative();
				unityViewStruct.depthViewports = depthViewports;
				unityViewStruct.targetViewports = targetViewports;
				unityViewStruct.renderStyle = renderStyle;
				unityViewStruct.unityRenderOptions = unityRenderOptions;
				unityViewStruct.colourTexture = (System.IntPtr)0;
				unityViewStruct.colourTextureArrayIndex = -1;

				lastFrameCount = Time.renderedFrameCount;

				trueSKY ts = trueSKY.GetTrueSky();

				//
				ts.InscatterTexture.renderTexture = ts.inscatterRT;
				ts.LossTexture.renderTexture = ts.lossRT;
				ts.CloudVisibilityTexture.renderTexture = ts.cloudVisibilityRT;
				ts.CloudShadowTexture.renderTexture = ts.cloudShadowRT;

				StaticSetRenderTexture("inscatter2D", ts.InscatterTexture.GetNative());
				StaticSetRenderTexture("Loss2D", ts.LossTexture.GetNative());
				StaticSetRenderTexture("CloudVisibilityRT", ts.CloudVisibilityTexture.GetNative()); 
				StaticSetRenderTexture("CloudShadowRT", ts.CloudShadowTexture.GetNative());

				/*_inscatterRT.renderTexture = inscatterRT;
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
				}*/
			}
		}

		public RenderStyle GetBaseRenderStyle(Camera cam)
		{
			RenderStyle renderStyle = RenderStyle.UNITY_STYLE_DEFERRED;
			if (cam.actualRenderingPath != RenderingPath.DeferredLighting
				&& cam.actualRenderingPath != RenderingPath.DeferredShading)
			{
				if (flippedView)
					renderStyle = RenderStyle.UNITY_STYLE_DEFERRED;
				else
					renderStyle = RenderStyle.UNITY_STYLE;
			}
			return renderStyle;
		}
		public RenderStyle GetRenderStyle(Camera cam)
		{
#if !UNITY_GAMECORE
#if !UNITY_SWITCH
			UnityEngine.XR.XRSettings.showDeviceView = true;
#endif
#endif
			RenderStyle r = GetBaseRenderStyle(cam);
			if (trueSKY.GetTrueSky().DepthBlending)
			{
				r = r | RenderStyle.DEPTH_BLENDING;
			}
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
		public bool editorMode
		{
			get
			{
				return IsPPStak || (Application.isEditor && !Application.isPlaying);
			}
		}
		public bool IsPPStak
		{
			get
			{
				return System.Type.GetType("UnityEngine.PostProcessing.PostProcessingBehaviour") != null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan;
			}
		}

		protected trueSKY ts;
		protected bool tsValid = false;
		protected void ProjMatrixToTrueSkyFormat_HDRP(RenderStyle renderStyle, Matrix4x4 m, float[] proj, int offset = 0)
		{
			if (!tsValid)
				return;

			offset *= 16;
			float metresPerUnit = ts.MetresPerUnit;

			m = m.transpose;
			if ((renderStyle & RenderStyle.UNITY_STYLE_DEFERRED) == RenderStyle.UNITY_STYLE_DEFERRED && flippedView)
			{
				m = m * Matrix4x4.Scale(new Vector3(1, -1, 1));
			}

			proj[offset + 00] = m.m00;
			proj[offset + 01] = m.m01;
			proj[offset + 02] = m.m02;
			proj[offset + 03] = m.m03 * metresPerUnit;

			proj[offset + 04] = m.m10;
			proj[offset + 05] = -m.m11;
			proj[offset + 06] = m.m12;
			proj[offset + 07] = m.m13 * metresPerUnit;

			proj[offset + 08] = m.m20;
			proj[offset + 09] = m.m21;
			proj[offset + 10] = m.m22;
			proj[offset + 11] = m.m23 * metresPerUnit;

			proj[offset + 12] = m.m30;
			proj[offset + 13] = m.m31;
			proj[offset + 14] = m.m32;
			proj[offset + 15] = m.m33 * metresPerUnit;
		}
		protected void ViewMatrixToTrueSkyFormat_HDRP(RenderStyle renderStyle, Matrix4x4 m, float[] view, int offset = 0, bool swap_yz = true)
		{
			if (!tsValid)
				return;

			offset *= 16;
			float metresPerUnit = ts.MetresPerUnit;
			Matrix4x4 transform = ts.transform.worldToLocalMatrix.inverse;
			m = m * transform;
			m = m.transpose;
			Matrix4x4 n = m.inverse;
			Matrix4x4 y;
			if (swap_yz)
			{
				// Swap the y and z columns - this makes a left-handed matrix into right-handed:
				y.m00 = n.m00;
				y.m01 = n.m02;
				y.m02 = n.m01;
				y.m03 = n.m03;

				y.m10 = n.m10;
				y.m11 = n.m12;
				y.m12 = n.m11;
				y.m13 = n.m13;

				y.m20 = n.m20;
				y.m21 = n.m22;
				y.m22 = n.m21;
				y.m23 = n.m23;
				// Swap the position values as well, as Unity uses y=up, we use z:
				y.m30 = n.m30 * metresPerUnit;
				y.m31 = n.m32 * metresPerUnit;
				y.m32 = n.m31 * metresPerUnit;
				y.m33 = n.m33;
			}
			else
			{
				// Swap the y and z columns - this makes a left-handed matrix into right-handed:
				y.m00 = n.m00;
				y.m01 = n.m01;
				y.m02 = n.m02;
				y.m03 = n.m03;

				y.m10 = n.m10;
				y.m11 = n.m11;
				y.m12 = n.m12;
				y.m13 = n.m13;

				y.m20 = n.m20;
				y.m21 = n.m21;
				y.m22 = n.m22;
				y.m23 = n.m23;
				// Swap the position values as well, as Unity uses y=up, we use z:
				y.m30 = n.m30 * metresPerUnit;
				y.m31 = n.m31 * metresPerUnit;
				y.m32 = n.m32 * metresPerUnit;
				y.m33 = n.m33;
			}

			Matrix4x4 z = y.inverse;
			view[offset + 00] = z.m00;
			view[offset + 01] = z.m01;
			view[offset + 02] = z.m02;
			view[offset + 03] = z.m03;

			view[offset + 04] = z.m10;
			view[offset + 05] = z.m11;
			view[offset + 06] = z.m12;
			view[offset + 07] = z.m13;

			view[offset + 08] = z.m20;
			view[offset + 09] = z.m21;
			view[offset + 10] = z.m22;
			view[offset + 11] = z.m23;

			view[offset + 12] = z.m30;
			view[offset + 13] = z.m31;
			view[offset + 14] = z.m32;
			view[offset + 15] = z.m33;
		}
	}
}
#endif