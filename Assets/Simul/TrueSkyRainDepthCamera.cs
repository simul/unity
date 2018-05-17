using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace simul
{
	/*
	 * This should override the rendering for the camera to produce a linear depth image.
	 */
	[ExecuteInEditMode]
	public class TrueSkyRainDepthCamera : MonoBehaviour
	{
		private Camera _camera = null;
		private Shader _shader = null;
		private Matrix4x4 _matrix = new Matrix4x4();
		private RenderTexture _targetTexture=null;
		public Matrix4x4 matrix
		{
			get
			{
				return _matrix;
			}
		}
		public RenderTexture targetTexture
		{
			get
			{
				return _targetTexture;
			}
		}
		void OnDisable()
		{
			if (_camera)
			{
				_camera = null;
			}
		}
		// Use this for initialization
		void Start()
		{
		}
		public float widthMetres = 100.0F;
		public float depthMetres = 100.0F;
		public int textureSize = 128;
		public void Update()
		{
			if (!_shader)
			{
				_shader = Shader.Find("Unlit/LinearDepth");
			}
			if (!_targetTexture||_targetTexture.width!=textureSize)
			{
				_targetTexture = new RenderTexture(textureSize, textureSize, 32,RenderTextureFormat.RFloat);
			}
			if (_camera == null)
			{
				_camera = gameObject.GetComponent<Camera>();
			/*	if(!_camera)
				{
					_camera = gameObject.AddComponent<Camera>();
				}
				aDummyCamObject.hideFlags = HideFlags.HideAndDontSave;
				dummyCam = aDummyCamObject.GetComponent<Camera>();
				dummyCam.enabled = false;
				dummyCam.backgroundColor = new Color(0, 0, 0, 0);
				dummyCam.renderingPath = RenderingPath.DeferredLighting;
				dummyCam.depthTextureMode |= DepthTextureMode.Depth;
				trueSkyCameraCubemap = aDummyCamObject.AddComponent<TrueSkyCameraCubemap>();
				_initialized = false;*/
			}
			if (!_shader || !_targetTexture||!_camera)
				return;
			_camera.renderingPath = RenderingPath.Forward;
			_camera.clearFlags = CameraClearFlags.SolidColor;
			_camera.backgroundColor = new Color(1.0F,0, 0, 0);
			_camera.RenderWithShader(_shader, "RenderType");
			_camera.enabled = false;
			_camera.cameraType = CameraType.Game;
			_camera.orthographic = true;
			_camera.orthographicSize = widthMetres;
			_camera.nearClipPlane = 0.0F;
			_camera.farClipPlane= depthMetres;
			_camera.targetTexture = _targetTexture;

			Matrix4x4 proj = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, true);
			proj.m22 *= 2.0F;
			Matrix4x4 view = Matrix4x4.Rotate(new Quaternion(1.0F, 0, 0, 0.0F)) * _camera.worldToCameraMatrix;
			_matrix=proj.transpose* view;
		}
	}
}