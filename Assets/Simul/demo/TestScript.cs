using UnityEngine;
using System.Collections;
// Specify using simul to use the trueSKY class:
using simul;

[ExecuteInEditMode]
public class TestScript : MonoBehaviour
{
	void Start ()
    {
	
	}
	void Update ()
    {
	// We're going to demo aspects of trueSKY with this script.
		VolumeQueryResult rs = trueSKY.GetTrueSky().GetCloudQuery(1, this.transform.position);
		if (rs.density > 0.5)
			{
				this.GetComponent<Renderer>().sharedMaterial.SetColor("_EmissionColor", new Color(4.0F, .1F, .1F));
				Vector3 irr = new Vector3(1.0F, 0.1F, 0.1F);
				trueSKY.GetTrueSky().SetPointLight(1,this.transform.position,1.0F,4000.0F,irr);
			}
			else
			{
				this.GetComponent<Renderer>().sharedMaterial.SetColor("_EmissionColor", new Color(.1F, 1, .1F));
			}
	}
}
