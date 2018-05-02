using UnityEngine;
using System.Collections;

namespace simul
{
	[ExecuteInEditMode]
	public class PhysicalPointLight : MonoBehaviour
	{
		Color  initialColour;
		// Use this for initialization
		void Start()
		{
			Material mat = this.GetComponent<Renderer>().sharedMaterial;
			Light p = GetComponent<Light>();
			if (mat != null)
			{
				initialColour= p.color;
			}
			
		}

		// Adjust the brightness of this light so it is relative to trueSKY.
		void Update()
		{
			float ref_rad = trueSKY.GetTrueSky().GetFloat("referencespectralradiance");
			//UnityEngine.Debug.Log("ref rad= "+ ref_rad);
			if (ref_rad <= 0.0F)
				return; 
			Light p = GetComponent<Light>();
			p.intensity = 1.0F / ref_rad;
			Material mat = this.GetComponent<Renderer>().sharedMaterial;
			initialColour = p.color;
			if (mat != null)
			{
				//UnityEngine.Debug.Log("initialColour= " + initialColour.r+ " "+initialColour.g+ " "+initialColour.b);
				Color c = p.intensity * initialColour;
				//UnityEngine.Debug.Log("new Colour= " + c.r + " " + c.g + " " + c.b);
				mat.SetColor("_EmissionColor", c);
			}
		}
	}
}