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
		public Shader _shader = null;
		// Use this for initialization
		void Start()
		{
			Camera cam = GetComponent<Camera>();
			//cam.depthTextureMode = DepthTextureMode.None;

			cam.SetReplacementShader(_shader, "RenderType");
		}


		public void Update()
		{
			Camera cam = GetComponent<Camera>();
			cam.renderingPath = RenderingPath.Forward;
			//cam.SetTargetBuffers(gBuffer.colorBuffer, gBuffer.depthBuffer);
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color(1.0F,0, 0, 0);
			cam.RenderWithShader(_shader, "RenderType");

		}
	}
}