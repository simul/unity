using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace simul
{
	[CustomEditor(typeof(TrueSkyWaterObject))]
	public class TrueSkyWaterObjectEditor : Editor
	{
		[MenuItem("GameObject/Create Other/Create trueSKY Water Object", false, 200000)]
		public static void CreateWaterObject()
		{
			trueSKY trueSky = trueSKY.GetTrueSky();

			if (trueSky && trueSky.SimulVersion >= trueSky.MakeSimulVersion(4, 2))
			{
				GameObject g = new GameObject("Water Object");
				g.AddComponent<TrueSkyWaterObject>();
			}
			else
			{
				UnityEngine.Debug.LogWarning("No compatible Truesky version installed, cannot create water objects.");
			}
		}

		[MenuItem("CONTEXT/TrueSkyWaterBuoyancy/Create Water Probe", false, 200000)]
		public static void CreateWaterProbe()
		{
			trueSKY trueSky = trueSKY.GetTrueSky();

			if (trueSky && trueSky.SimulVersion >= trueSky.MakeSimulVersion(4, 2))
			{
				GameObject g = new GameObject("Water Probe");
				g.AddComponent<TrueSkyWaterProbe>();
				g.transform.parent = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.OnlyUserModifiable)[0];
				g.transform.localPosition = new Vector3(0.0f,0.0f,0.0f);
			}
			else
			{
				UnityEngine.Debug.LogWarning("No compatible Truesky version installed, cannot create water probes.");
			}
		}

		[SerializeField]
		static bool advanced = false;
		public override void OnInspectorGUI()
		{
			UnityEngine.Object[] trueSkies;
			trueSKY trueSky = null;
			trueSkies = FindObjectsOfType(typeof(trueSKY));
			foreach (UnityEngine.Object t in trueSkies)
			{
				trueSky = (trueSKY)t;
			}

			if (trueSky && trueSky.SimulVersion >= trueSky.MakeSimulVersion(4, 2))
			{
				TrueSkyWaterObject waterObject = (TrueSkyWaterObject)target;

				EditorGUILayout.Space();
			
				EditorGUILayout.BeginVertical();
				{
					waterObject.Render = EditorGUILayout.Toggle("Render", waterObject.Render);
					waterObject.BoundlessOcean = EditorGUILayout.Toggle("Boundless Ocean", waterObject.BoundlessOcean);
					waterObject.BeaufortScale = EditorGUILayout.Slider("Beaufort Scale", waterObject.BeaufortScale, 0.0f, 12.0f);
					waterObject.WindDirection = EditorGUILayout.Slider("Wind Direction", waterObject.WindDirection, 0.0f, 1.0f);
					waterObject.WindDependency = EditorGUILayout.Slider("Wind Dependency", waterObject.WindDependency, 0.0f, 1.0f);
					waterObject.Scattering = EditorGUILayout.Vector3Field("Scattering", waterObject.Scattering);
					if (waterObject.Scattering.x < 0.0f)
						waterObject.Scattering = new Vector3 (0.0f, waterObject.Scattering.y, waterObject.Scattering.z);
					if (waterObject.Scattering.y < 0.0f)
						waterObject.Scattering = new Vector3(waterObject.Scattering.x, 0.0f, waterObject.Scattering.z);
					if (waterObject.Scattering.z < 0.0f)
						waterObject.Scattering = new Vector3(waterObject.Scattering.x, waterObject.Scattering.y, 0.0f);
					waterObject.Absorption = EditorGUILayout.Vector3Field("Absorption", waterObject.Absorption);
					if (waterObject.Absorption.x < 0.0f)
						waterObject.Absorption = new Vector3(0.0f, waterObject.Absorption.y, waterObject.Absorption.z);
					if (waterObject.Absorption.y < 0.0f)
						waterObject.Absorption = new Vector3(waterObject.Absorption.x, 0.0f, waterObject.Absorption.z);
					if (waterObject.Absorption.z < 0.0f)
						waterObject.Absorption = new Vector3(waterObject.Absorption.x, waterObject.Absorption.y, 0.0f);
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space();
				EditorGUILayout.BeginVertical();
				{
					advanced = EditorGUILayout.Foldout(advanced, "Advanced Water Options");
					if (advanced)
					{
						waterObject.AdvancedWaterOptions = EditorGUILayout.Toggle("Enable Advanced Water Options", waterObject.AdvancedWaterOptions);
						if (waterObject.AdvancedWaterOptions)
						{
							waterObject.WindSpeed = EditorGUILayout.Slider("Wind Speed", waterObject.WindSpeed, 0.0f, 200.0f);
							waterObject.WaveAmplitude = EditorGUILayout.Slider("Wave Amplitude", waterObject.WaveAmplitude, 0.0f, 1.0f);
							waterObject.ChoppyScale = EditorGUILayout.Slider("ChoppyScale", waterObject.ChoppyScale, 0.0f, 4.0f);
						}
						if (waterObject.BoundlessOcean)
						{
							EditorGUILayout.Space();
							waterObject.EnableFoam = EditorGUILayout.Toggle("Enable Foam", waterObject.EnableFoam);
							if (waterObject.EnableFoam && waterObject.AdvancedWaterOptions)
							{
								waterObject.FoamHeight = EditorGUILayout.Slider("Foam Height", waterObject.FoamHeight, 0.0f, 20.0f);
								waterObject.FoamChurn = EditorGUILayout.Slider("Foam FoamChurn", waterObject.FoamChurn, 0.0f, 20.0f);
							}
						}
					}
					EditorGUILayout.Space();
				}
				EditorGUILayout.EndVertical();
			}
		}
	}
}
