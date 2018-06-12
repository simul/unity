using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace simul
{
	public class TrueSkyWaterBuoyancy : MonoBehaviour
	{
		public TrueSkyWaterBuoyancy()
		{
			
		}

		~TrueSkyWaterBuoyancy()
		{

		}

		void Update()
		{
			TrueSkyWaterProbe[] WaterProbes;

			WaterProbes = GetComponentsInChildren<TrueSkyWaterProbe>();

			if (WaterProbes.Length != 0)
			{
				Rigidbody rb = GetComponent<Rigidbody>();

				if (rb)
				{
					int i;

					float AdjustedDepth = 0.0f;
					float sampledDepth = 0.0f;
					Vector3 directionalForce = new Vector3(0.0f, 0.0f, 0.0f);
					int activeProbeCount = 0;

					for (i = 0; i < WaterProbes.Length; i++)
					{
						WaterProbes[i].UpdateProbeValues();
						if (WaterProbes[i].IsActive())
						{
							activeProbeCount++;
							sampledDepth = WaterProbes[i].GetDepth();
							directionalForce = WaterProbes[i].GetDirection();

							AdjustedDepth = sampledDepth;

							float BuoyancyForce;
							float volume;
							float R = WaterProbes[i].Radius;
							AdjustedDepth += R;

							if (AdjustedDepth > 0.0)
							{
								if (AdjustedDepth > 2.0f * R)
								{
									volume = (4.0f / 3.0f) * Mathf.PI * R * R* R;// 
									sampledDepth = R;
									//AdjustedDepth = 2.0f * R;
								}
								else
								{
									if (sampledDepth > 0.0)
										AdjustedDepth = R - sampledDepth;

									float h = AdjustedDepth;// Cast<UTrueSkyWaterProbeComponent>(waterProbes[0])->GetDepth();

									volume = ((Mathf.PI * h * h) / 3.0f) * ((3 * R) - h);

									//float d = R - h;
									//volume = PI * h * ((R*R) - (d*d) - (h * d) - ((h * h) / 3));

									if (sampledDepth > 0.0)
										volume = ((4.0f / 3.0f) * Mathf.PI * R * R * R ) - volume;// bounds.GetSphere().GetVolume() - volume;
								}
								BuoyancyForce = volume * 9.81f * 0.1f;

								if (BuoyancyForce< 0.0)
								{
									return;
								}

								float c = 0.2f;
								double dragArea = 0;
	
								Vector3 direction = rb.GetPointVelocity(WaterProbes[i].transform.position);
								direction.Normalize();

								//direction = FVector(0.0, 1.0, 0.1);

								if (AdjustedDepth > 2.0f * R)
								{
									dragArea = (Mathf.PI * R * R);
								}
								else
								{
									Vector3 temp = new Vector3(0.0f, 0.0f, 1.0f);
									float angle = Vector3.Angle(direction, temp);
									angle = (Mathf.PI / 2) - angle;
									double h = R - (Math.Abs(sampledDepth) / Math.Cos(angle));

									if (h< 0 || h> R)
										dragArea = 0;
									else
										dragArea = ((R* R) * Math.Cosh((R - h) / R)) - ((R - h) * Math.Sqrt((2 * R* h) - (h* h)));

									if (sampledDepth > 0.0)
										dragArea = (Math.PI * R * R) - dragArea;
								}

								double DragForce = 0.5f * c * dragArea * Math.Pow(rb.GetPointVelocity(WaterProbes[i].transform.position).magnitude, 2.0);
								double WaterMovementForce = 0.5f * c * dragArea * Math.Pow(directionalForce.magnitude, 2.0);

								directionalForce.y = 0.0f;

								Vector3 WorldBuoyancyForce = new Vector3(0.0f, BuoyancyForce, 0.0f);
								Vector3 WorldDragForce = (float)DragForce * -direction;
								Vector3 WorldWaterMovementForce = (float)WaterMovementForce * (directionalForce / directionalForce.magnitude);
								Vector3 ForceLocation = WaterProbes[i].transform.position - new Vector3(0.0f, (sampledDepth - R) / 2.0f, 0.0f);

								rb.AddForceAtPosition(WorldBuoyancyForce, ForceLocation);
								rb.AddForceAtPosition(WorldDragForce, ForceLocation);
								rb.AddForceAtPosition(WorldWaterMovementForce, WaterProbes[i].transform.position);
							}
						}
					}
				}
			}
		}
	}
}
