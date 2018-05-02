using UnityEngine;
using System.Collections;

namespace simul
{
    [ExecuteInEditMode]
    /// This probe component will update the material trueSKYSkybox, which can be used as the skybox for lighting.
    public class TrueSkyCubemapProbe : MonoBehaviour
    {
        private Camera dummyCam                     = null;
        private RenderTexture cubemapRenderTexture  = null;
        public int textureSize                      = 32;
        public float exposure                       = 1.0F;
        public float gamma                          = 0.44F;
        public float updatePeriodSeconds            = 0.5F;
        float _updatePeriodSeconds                  = 0.0F;
        public bool skyOnly                         = true;

        bool _initialized                                   = false;
        public RenderTextureFormat renderTextureFormat      = RenderTextureFormat.Default;
        private TrueSkyCameraCubemap trueSkyCameraCubemap   = null;
        int last_face                                       = -1;
        // Flips the projection matrix of the cubemap probe
        public bool flipProbeY                              = true;

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
            if (dummyCam)
            {
                DestroyImmediate(dummyCam);
                dummyCam = null;
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
#if !UNITY_SWITCH
            if(UnityEngine.XR.XRSettings.enabled && !UnityEngine.XR.XRSettings.isDeviceActive)
            {
                return;
            }
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

        /// <summary>
        /// This is the function that creates the cubemap images
        /// </summary>
        void DoUpdate()
        {
            if (textureSize != 8 && textureSize != 16 && textureSize != 32 && textureSize != 64
                && textureSize != 128 && textureSize != 256 && textureSize != 512)
                textureSize = 32;   // if textureSize (inc. from inspector) isn't pow of 2 & between 8-512, then overwrite with default
                                    
            if (dummyCam == null)
            {
                GameObject aDummyCamObject  = new GameObject("CubemapCamera1", typeof(Camera));
                aDummyCamObject.hideFlags   = HideFlags.HideAndDontSave;
                dummyCam                    = aDummyCamObject.GetComponent<Camera>();
                dummyCam.enabled            = false;
                dummyCam.backgroundColor    = new Color(0, 0, 0, 0);
                dummyCam.renderingPath      = RenderingPath.DeferredLighting;
                dummyCam.depthTextureMode   |= DepthTextureMode.Depth;
                trueSkyCameraCubemap        = aDummyCamObject.AddComponent<TrueSkyCameraCubemap>();
                _initialized                = false;
            }

            // Null checks
            if (dummyCam == null)
                return;
            if (cubemapRenderTexture == null)
                CreateTexture();
            // Don't render if is not ready yet
            if(!cubemapRenderTexture.IsCreated())
            {
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

            int faceMask = 63;
            if (_initialized && last_face >= 0)
            {
                faceMask = 1 << last_face;
            }
            last_face = last_face + 1;
            last_face = last_face % 6;

            // Disable any renderer attached to this object which may get in the way of our cam
            if (GetComponent<Renderer>())
                GetComponent<Renderer>().enabled = false;
            
            // TO-DO: this is as it should be (once we integrate the Skylights we won't have it anyway) 
            // if (skyOnly)
                dummyCam.cullingMask = 0;

            // Render to the cubemap (using the mask to only render a face at a time)
            bool renderResult = false;
            renderResult = dummyCam.RenderToCubemap(cubemapRenderTexture, faceMask);
            if (!renderResult)
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

        void CreateTexture()
        {
            if (cubemapRenderTexture == null
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
                cubemapRenderTexture.Create();
                _initialized = false;
            }
        }
    }
}