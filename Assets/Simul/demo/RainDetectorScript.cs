using UnityEngine;
using System.Collections;
// Specify using simul to use the trueSKY class:
using simul;

[ExecuteInEditMode]
public class RainDetectorScript: MonoBehaviour
{
	void Start ()
    {
	
	}
	void Update ()
    {
	// We're going to demo aspects of trueSKY with this script.
		VolumeQueryResult rs = trueSKY.GetTrueSky().GetCloudQuery(2, this.transform.position);
		Material mat = this.GetComponent<Renderer>().sharedMaterial;
		if (mat != null)
		{
			if (rs.precipitation > 0.1)
			{
				mat.SetColor("_EmissionColor", new Color(4.0F, .1F, .1F));
			}
			else
			{
				mat.SetColor("_EmissionColor", new Color(.1F, 1, .1F));
			}
		}
	}
}
