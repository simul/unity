using UnityEngine;
using System.Collections;

#if USING_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif
using static simul.TrueSkyPluginRenderFunctionImporter;
namespace simul
{
	[ExecuteInEditMode]
	/// This probe component will update the material trueSKYSkybox, which can be used as the skybox for lighting.
	public class TrueSkyCubemapProbe : MonoBehaviour
	{
		private Camera dummyCam = null;

		private RenderTexture cubemapRenderTexture = null;
		public int textureSize = 32;
		public float exposure = 1.0F;
		public float gamma = 1.0F;//0.44F
		public float updatePeriodSeconds = 0.5F;
		float _updatePeriodSeconds = 0.0F;
		public bool skyOnly = false;

		bool _initialized = false;
		public RenderTextureFormat renderTextureFormat = RenderTextureFormat.Default;
		private TrueSkyCameraCubemap trueSkyCameraCubemap = null;
		int last_face = -1;
		// Flips the projection matrix of the cubemap probe
		public bool flipProbeY = true;

		/*These three variables are only temporary to provide access to the default skylight 
		 * created by the plugin that right now is used only for water reflections, a larger 
		 * update will be required to rewrite camera rpobes for this purpose instead.
		 */
		[SerializeField]
		bool _skylightAllMips = false;
		public bool SkylightAllMips
		{
			get
			{
				return _skylightAllMips;
			}
			set
			{
				if (_skylightAllMips != value)
				{
					_skylightAllMips = value;
					StaticSetRenderBool("defaultskylightallmips", value);
				}
			}
		}

		[SerializeField]
		bool _skylightAllFaces = false;
		public bool SkylightAllFaces
		{
			get
			{
				return _skylightAllFaces;
			}
			set
			{
				if (_skylightAllFaces != value)
				{
					_skylightAllFaces = value;
					StaticSetRenderBool("defaultskylightallfaces", value);
				}
			}
		}

		[SerializeField]
		int _skylightAmortization = 2;
		public int SkylightAmortization
		{
			get
			{
				return _skylightAmortization;
			}
			set
			{
				if (_skylightAmortization != value)
				{
					_skylightAmortization = value;
					StaticSetRenderInt("defaultskylightamortization", value);
				}
			}
		}


		// Variables solely for HDRP
#if USING_HDRP
		private HDAdditionalCameraData HDRPdummyCam = null;
		private GameObject trueSkyCubemapProbe = null;
#endif
		private int faceMask = 63;
		public int GetFaceMask() { return faceMask; }


		public RenderTexture GetRenderTexture()
		{
			if (cubemapRenderTexture == null)
				return null;
			return cubemapRenderTexture;
		}

		void Start()
		{
			_initialized = false;
		}

		void OnDisable()
		{
#if USING_HDRP
			if (trueSkyCubemapProbe)
			{
				DestroyImmediate(trueSkyCubemapProbe);
			}

			if (HDRPdummyCam)
			{
				DestroyImmediate(HDRPdummyCam);
			}
#endif
			if (dummyCam)
			{
				DestroyImmediate(dummyCam);
			}
			if (trueSkyCameraCubemap)
			{
				trueSkyCameraCubemap.Cleanup();
				trueSkyCameraCubemap = null;
			}
		}

		void Awake()
		{
			CreateTexture();
			if (cubemapRenderTexture)
				cubemapRenderTexture.DiscardContents();
			_initialized = false;
		}

		void Update()
		{
#if !UNITY_GAMECORE
#if !UNITY_SWITCH
			if (UnityEngine.XR.XRSettings.enabled && !UnityEngine.XR.XRSettings.isDeviceActive)
			{
				return;
			}
#endif
#endif
			// Has the update frequency changed?
			if (_updatePeriodSeconds != updatePeriodSeconds)
			{
				CancelInvoke();
				_updatePeriodSeconds = updatePeriodSeconds;
				// If nonzero, start the periodic updates.
				if (_updatePeriodSeconds > 0.0F)
				{
					InvokeRepeating("UpdateCustom", 0.0F, _updatePeriodSeconds);
					_initialized = false;
				}
			}
			// If it's zero, update once per frame.
			if (!Application.isPlaying || _updatePeriodSeconds <= 0.0F)
			{
				if (trueSkyCameraCubemap)
				{
					trueSkyCameraCubemap.GetComponent<TrueSkyCameraCubemap>().DoFlipY = flipProbeY;
				}
				DoUpdate();
			}
		}

		public int GetViewId()
		{
			if (trueSkyCameraCubemap == null)
				return -1;
			return trueSkyCameraCubemap.GetViewId();
		}

		/// <summary>
		/// This is the periodic update called if updatePeriodSeconds>0.
		/// </summary>
		void UpdateCustom()
		{
			if (trueSkyCameraCubemap)
			{
				trueSkyCameraCubemap.GetComponent<TrueSkyCameraCubemap>().DoFlipY = flipProbeY;
			}
			DoUpdate();
		}

		void DoUpdate()
		{
			if (trueSKY.GetTrueSky() && trueSKY.GetTrueSky().HDRP_RenderPipelineAsset != null)
			{
			#if USING_HDRP
				DoUpdateHDRP();
			#endif
			}
			else
			{
				DoUpdateStandard();
			}
		}

		/// <summary>
		/// This is the function that creates the cubemap images
		/// </summary>
		void DoUpdateStandard()
		{
			textureSize = Mathf.Clamp(Mathf.ClosestPowerOfTwo(textureSize), 8, 512);
									
			if (dummyCam == null)
			{
				GameObject aDummyCamObject = new GameObject("CubemapCamera1", typeof(Camera));
				//UnityEngine.Debug.LogWarning("DoUpdateStandard");
				aDummyCamObject.gameObject.layer = trueSKY.GetTrueSky().trueSKYLayerIndex;
				aDummyCamObject.hideFlags        = HideFlags.HideAndDontSave;
				dummyCam                         = aDummyCamObject.GetComponent<Camera>();
				dummyCam.enabled                 = false;
				dummyCam.backgroundColor         = new Color(0, 0, 0, 0);
				dummyCam.renderingPath           = RenderingPath.DeferredLighting;
				dummyCam.depthTextureMode        |= DepthTextureMode.Depth;
				dummyCam.allowHDR                = false;
				dummyCam.allowMSAA               = false;
				trueSkyCameraCubemap             = aDummyCamObject.AddComponent<TrueSkyCameraCubemap>();
				_initialized                     = false;
			}

			// Null checks
			if (dummyCam == null)
				return;
			if (cubemapRenderTexture == null)
				CreateTexture();
			// Don't render if is not ready yet
			if (!cubemapRenderTexture.IsCreated())
			{
				CreateTexture();
				return;
			}
			// Setup camera to render
			dummyCam.gameObject.transform.position = transform.position;
			dummyCam.gameObject.transform.rotation = transform.rotation;
			if (trueSkyCameraCubemap != null)
			{
				trueSkyCameraCubemap.exposure = exposure;
				trueSkyCameraCubemap.gamma = gamma;
			}
			dummyCam.nearClipPlane = 0.1f;
			dummyCam.farClipPlane = 300000.0f;
			if (cubemapRenderTexture == null)
			{
				UnityEngine.Debug.Log("cubemapRenderTexture == null ");
				return;
			}
			if (dummyCam.targetTexture != cubemapRenderTexture)
			{
				dummyCam.targetTexture = cubemapRenderTexture;
				_initialized = false;
			}

		
			if (_initialized && last_face >= 0)
			{
				faceMask = 1 << last_face;
			}
			last_face = last_face + 1;
			last_face = last_face % 6;

			// Disable any renderer attached to this object which may get in the way of our cam
			if (GetComponent<Renderer>())
				GetComponent<Renderer>().enabled = false;

			if (skyOnly)
			{
				dummyCam.cullingMask = 0;
			}

			// Render to the cubemap (using the mask to only render a face at a time)
			if (!dummyCam.RenderToCubemap(cubemapRenderTexture, faceMask))
			{
				Debug.LogWarning("Failed to capture the probe");
			}

			// Re-enable the renderers
			_initialized = true;
			if (GetComponent<Renderer>())
			{
				GetComponent<Renderer>().enabled = true;
				if (GetComponent<Renderer>().sharedMaterial)
				{
					if (GetComponent<Renderer>().sharedMaterial.HasProperty("_Cube"))
						if (GetComponent<Renderer>().sharedMaterial.GetTexture("_Cube") != cubemapRenderTexture)
							GetComponent<Renderer>().sharedMaterial.SetTexture("_Cube", cubemapRenderTexture);
				}
			}

			// Set the cube texture
			Material trueSKYSkyboxMat = Resources.Load("trueSKYSkybox", typeof(Material)) as Material;
			if (trueSKYSkyboxMat)
			{
				if (trueSKYSkyboxMat.GetTexture("_Cube") != cubemapRenderTexture)
					trueSKYSkyboxMat.SetTexture("_Cube", cubemapRenderTexture);
			}
			else
				UnityEngine.Debug.LogWarning("Can't find Material 'trueSKYSkybox' - it should be in Simul/Resources.");
		}


#if USING_HDRP
		/// <summary>
		/// This is the function that creates the cubemap images for HDRP
		/// </summary>
		private void DoUpdateHDRP()
		{
			textureSize = Mathf.Clamp(Mathf.ClosestPowerOfTwo(textureSize), 8, 512);   

			CreateTexture();

			if (trueSkyCubemapProbe == null)
			{
				foreach (var cam in FindObjectsOfType(typeof(Camera)) as Camera[])
				{
					if (cam.name == "TrueSkyCubemapProbe")
					{
						trueSkyCubemapProbe = cam.gameObject;
						break;
					}
				}
				if (trueSkyCubemapProbe == null)
				{
					trueSkyCubemapProbe = new GameObject("TrueSkyCubemapProbe", typeof(Camera));
					trueSkyCubemapProbe.AddComponent<HDAdditionalCameraData>();
					UnityEngine.Debug.LogWarning("DoUpdateHDRP");

					trueSkyCubemapProbe.gameObject.layer = trueSKY.GetTrueSky().trueSKYLayerIndex;

					if (trueSkyCubemapProbe.GetComponent<Camera>() == null)
						trueSkyCubemapProbe.AddComponent<Camera>();
				}
				if(dummyCam == null)
				{
					dummyCam = trueSkyCubemapProbe.GetComponent<Camera>();
					HDRPdummyCam = trueSkyCubemapProbe.GetComponent<HDAdditionalCameraData>();
					trueSkyCubemapProbe.hideFlags = HideFlags.HideAndDontSave;
					dummyCam.enabled = true;
					dummyCam.clearFlags = CameraClearFlags.Color;
					dummyCam.backgroundColor = new Color(0, 0, 0, 0);
					dummyCam.renderingPath = RenderingPath.DeferredLighting;
					dummyCam.depthTextureMode |= DepthTextureMode.Depth;
					dummyCam.fieldOfView = 90.0f;
					dummyCam.targetTexture = cubemapRenderTexture;
					dummyCam.nearClipPlane = 0.1f;
					dummyCam.farClipPlane = 300000.0f;
				}

			}
			if (skyOnly)
			{
				dummyCam.cullingMask = 0;
			}
				

			// Set the cube texture
			Material trueSKYSkyboxMat = Resources.Load("trueSKYSkybox", typeof(Material)) as Material;
			if (trueSKYSkyboxMat)
			{
				if (trueSKYSkyboxMat.GetTexture("_Cube") != cubemapRenderTexture)
					trueSKYSkyboxMat.SetTexture("_Cube", cubemapRenderTexture);
			}
			else
				UnityEngine.Debug.LogWarning("Can't find Material 'trueSKYSkybox' - it should be in Simul/Resources.");
	
			faceMask *= 2;
			if (faceMask > 32)
				faceMask = 1;
		}
#endif
		void CreateTexture()
		{
			if (cubemapRenderTexture == null
				||!cubemapRenderTexture.IsCreated()
				|| cubemapRenderTexture.width != textureSize
				|| cubemapRenderTexture.depth != 24
				|| cubemapRenderTexture.format != renderTextureFormat
				|| cubemapRenderTexture.dimension != UnityEngine.Rendering.TextureDimension.Cube
			)
			{
				RenderTextureFormat rtf         = renderTextureFormat;
				cubemapRenderTexture            = new RenderTexture(textureSize, textureSize, 24, rtf, RenderTextureReadWrite.Linear);
				renderTextureFormat             = cubemapRenderTexture.format;
				cubemapRenderTexture.dimension  = UnityEngine.Rendering.TextureDimension.Cube;
				cubemapRenderTexture.name       = "trueSKY CubemapRenderTexture";

                cubemapRenderTexture.Create();
                _initialized = false;
			}
		}
	}
}