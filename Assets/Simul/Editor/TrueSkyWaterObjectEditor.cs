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
		[MenuItem("GameObject/Create trueSKY Water Object", false, 150000)]
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

			EditorGUI.BeginChangeCheck();

			if (trueSky && trueSky.SimulVersion >= trueSky.MakeSimulVersion(4, 2))
			{
				TrueSkyWaterObject waterObject = (TrueSkyWaterObject)target;

				Undo.RecordObject(waterObject, "Change Value");

				EditorGUILayout.Space();
			
				EditorGUILayout.BeginVertical();
				{
					waterObject.Render = EditorGUILayout.Toggle("Render", waterObject.Render);
					waterObject.BoundlessOcean = EditorGUILayout.Toggle("Boundless Ocean", waterObject.BoundlessOcean);
					waterObject.BeaufortScale = EditorGUILayout.Slider("Beaufort Scale", waterObject.BeaufortScale, 0.0f, 12.0f);
					waterObject.WindDirection = EditorGUILayout.Slider("Wind Direction", waterObject.WindDirection, 0.0f, 1.0f);
					waterObject.WindDependency = EditorGUILayout.Slider("Wind Dependency", waterObject.WindDependency, 0.0f, 1.0f);
					waterObject.Scattering = EditorGUILayout.ColorField("Scattering", waterObject.Scattering);
					waterObject.Absorption = EditorGUILayout.ColorField("Absorption", waterObject.Absorption);
					if (trueSky.SimulVersion >= trueSky.MakeSimulVersion(4, 3))
					{
						waterObject.CustomMesh = (Mesh)EditorGUILayout.ObjectField("Custom Water Surface Mesh", waterObject.CustomMesh, typeof(Mesh), false);
					}
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space();
				EditorGUILayout.BeginVertical();
				{
					advanced = EditorGUILayout.Foldout(advanced, "Advanced Water Options");

					waterObject.ProfileBufferResolution = (int)EditorGUILayout.Slider("Profile Buffer Resolution", waterObject.ProfileBufferResolution, 512, 4096);

					if (advanced)
					{
						waterObject.AdvancedWaterOptions = EditorGUILayout.Toggle("Enable Advanced Water Options", waterObject.AdvancedWaterOptions);
						if (waterObject.AdvancedWaterOptions)
						{
							waterObject.WindSpeed = EditorGUILayout.Slider("Wind Speed", waterObject.WindSpeed, 0.0f, 40.0f);
							waterObject.WaveAmplitude = EditorGUILayout.Slider("Wave Amplitude", waterObject.WaveAmplitude, 0.0f, 2.0f);
							waterObject.MaxWaveLength = EditorGUILayout.Slider("Max WaveLength", waterObject.MaxWaveLength, 1.01f, 100.0f);
							waterObject.MinWaveLength = EditorGUILayout.Slider("Min WaveLength", waterObject.MinWaveLength, 0.01f, 1.0f);
							
						}
						if (waterObject.BoundlessOcean)
						{
							EditorGUILayout.Space();
							waterObject.EnableFoam = EditorGUILayout.Toggle("Enable Foam", waterObject.EnableFoam);
							if (waterObject.EnableFoam && waterObject.AdvancedWaterOptions)
							{
								waterObject.FoamStrength = EditorGUILayout.Slider("Foam Strength", waterObject.FoamStrength, 0.0f, 1.0f);
								//waterObject.FoamChurn = EditorGUILayout.Slider("Foam FoamChurn", waterObject.FoamChurn, 0.0f, 20.0f);
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
