﻿using UnityEngine; 
using System.Collections;
using UnityEditor;
using System.Runtime.InteropServices;
using System;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace simul
{
	public class TrueSkySetupWizard : EditorWindow
	{
		enum Stage
		{
			PRE_START, START, FIND_SEQUENCE, FIND_CAMERA, FIND_TRUESKY, FIND_SUN, FINISH
		};
		Stage stage = Stage.PRE_START;

		[MenuItem("GameObject/Remove trueSKY from Scene", false, 200000)]
		public static void RemoveTrueSky()
		{
			UnityEngine.Object[] objects = FindObjectsOfType(typeof(Light));
			foreach (UnityEngine.Object t in objects)
			{
				Light l = (Light)t;
				if (l.GetComponent<TrueSkyLight>() != null)
					DestroyImmediate(l.GetComponent<TrueSkyLight>());
			}
			objects = FindObjectsOfType(typeof(Camera));
			foreach (UnityEngine.Object t in objects)
			{
				Camera c = (Camera)t;
				if (c.GetComponent<TrueSkyCamera>() != null)
					DestroyImmediate(c.GetComponent<TrueSkyCamera>());
			}
			objects = FindObjectsOfType(typeof(TrueSkyCameraBase));
			foreach (UnityEngine.Object t in objects)
			{
				MonoBehaviour b = (MonoBehaviour)t;
				if (b.GetComponent<TrueSkyCameraBase>() != null)
					DestroyImmediate(b.GetComponent<TrueSkyCameraBase>());
			}
			objects = FindObjectsOfType(typeof(TrueSkyCubemapProbe));
			foreach (UnityEngine.Object t in objects)
			{
				MonoBehaviour b = (MonoBehaviour)t;
				if (b.GetComponent<TrueSkyCubemapProbe>() != null)
					DestroyImmediate(b.GetComponent<TrueSkyCubemapProbe>());
			}
			objects = FindObjectsOfType(typeof(trueSKY));
			foreach (UnityEngine.Object o in objects)
			{
				trueSKY ts = (trueSKY)o;
				if (ts != null && ts.gameObject != null)
				{
					DestroyImmediate(ts.gameObject);
					break;
				}
			}
		}
		[MenuItem("GameObject/Create Other/Initialize trueSKY in Scene", false, 100000)]
		public static void InitTrueSkySequence1()
		{
			InitTrueSkySequence();
		}

		[MenuItem("GameObject/Initialize trueSKY in Scene", false, 100000)]
		public static void InitTrueSkySequence()
		{
			TrueSkySetupWizard w = (TrueSkySetupWizard)EditorWindow.GetWindow(typeof(TrueSkySetupWizard));
			Texture iconTexture = Resources.Load("trueSKY Icon") as Texture;
			w.titleContent = new GUIContent(" trueSKY", iconTexture);
		}

		void GetSceneFilename()
		{
            string n = "";
#if UNITY_5_4_OR_NEWER
            n = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
#else
            n = EditorApplication.currentScene;
#endif
            sceneFilename = n;
		}

		void OnGUI()
		{   
			if (stage == Stage.PRE_START)
			{
				// DirectoryCopy.CopyPluginsAndGizmosToAssetsFolder();
				stage = Stage.START;
			}
			GUIStyle textStyle = new GUIStyle();
			textStyle.wordWrap = true;

			if (EditorGUIUtility.isProSkin)
				textStyle.normal.textColor = Color.white;
			GUILayout.Label("Initialize trueSKY in Scene", EditorStyles.boldLabel);

			if (stage == Stage.START)
			{
				GetSceneFilename();
				if (sceneFilename.Length > 0)
					GUILayout.Label("This wizard will initialize trueSKY for the current scene:\n\n" + sceneFilename + ".", textStyle);
				else
				{
					GUILayout.Label("This wizard will initialize trueSKY for the current scene.\nThe current scene has not yet been saved - plase do this first, so the wizard knows where to put the trueSKY data.", textStyle);
				}
			}
			if (stage == Stage.FIND_SEQUENCE)
			{
				FindSequence();
				if (sequence != null)
				{
					GUILayout.Label("A sequence was found in the current scene directory. You can change it if necessary:", textStyle);
				}
				else
				{
					GUILayout.Label("No sequence was found in the current scene directory, You can select one, or one will be created.", textStyle);
				}
				sequence = (Sequence)EditorGUILayout.ObjectField("Sequence Asset", sequence, typeof(Sequence), false);
			}
			if (stage == Stage.FIND_CAMERA)
			{  
				FindCamera();
				if (Camera.main != null) 									// Case 1: Main Camera exists 
				{
					GUILayout.Label ("A main camera was found in the current scene. The TrueSkyCamera script will be assigned to this camera.", textStyle);

					if (Camera.allCamerasCount > 1) 						// Case 2: The are other cameras (in addition to Main Camera)	
					{			
						GUILayout.Label ("Additionally, check the box to assign the script to all cameras.", textStyle);	
						multipleCameras = GUILayout.Toggle (multipleCameras, "Assign TrueSkyCamera to all cameras");
					}

					mainCamera = Camera.main;
				} 
				else if (Camera.allCamerasCount >= 1)  						// Case 3: The are other cameras (but no Main Camera)
				{
					GUILayout.Label ("No main camera was found in the current scene. You can select another camera or check the option to make a new main camera and assign it to this.", textStyle);
					GUILayout.Label ("Additionally, check the box to assign the script to all cameras", textStyle);
					createAMainCamera = GUILayout.Toggle(createAMainCamera, "Create a new Main Camera and attach the TrueSkyCamera script to it.");
					multipleCameras = GUILayout.Toggle(multipleCameras, "Assign TrueSkyCamera to all cameras");	

				}
				else                                                    	// Case 4: No Cameras
				{
					GUILayout.Label("No main camera or other cameras found in the current scene. You can select one or check the box to have one created for you, and the script assigned to this.", textStyle);
					createAMainCamera = GUILayout.Toggle(createAMainCamera, "Create a new Main Camera and attach the TrueSkyCamera script to it.");
				}

				if (!createAMainCamera  && mainCamera == null)										// Don't give option to select other camera if there is/will be a main camera
					mainCamera = (Camera)EditorGUILayout.ObjectField("Camera", mainCamera, typeof(Camera), true);

			}
			if (stage == Stage.FIND_TRUESKY)
			{
				FindTrueSky();
				if (trueSky != null)
				{
                    GUILayout.Label("A trueSKY GameObject " + trueSky.name + " was found in the current scene.", textStyle);
				}
				else
				{
                    GUILayout.Label("No trueSKY GameObject was found in the current scene, one will be created.", textStyle);
				}
			}
			if (stage == Stage.FIND_SUN)
			{ 
				UnityEngine.Light[] lights;
				lights              = FindObjectsOfType (typeof(Light)) as Light[];
				int directionalLights = 0;
                
				foreach (Light t in lights) 
				{
					Light l = (Light)t;
					if (l.type == LightType.Directional)
						directionalLights++;
				}
				if (directionalLights == 0)
                {
					GUILayout.Label ("No directional light on scene, one will be created.", textStyle);
                }
				else if (directionalLights >= 1)
                {
					GUILayout.Label ("There's 1 or more directional lights on the scene. TrueSKY only needs one directional light.");
                    lightGameObject = lights[0].gameObject;
                    lightComponent  = lightGameObject.GetComponent<TrueSkyLight>();
                } 
			}
			if (stage == Stage.FINISH)
			{
				GUILayout.Label("When you click Finish, trueSKY will be initialized for this scene.", textStyle);

				//EditorGUILayout.LabelField("Remove standard distance fog",GUILayout.Width(48));
				removeFog = GUILayout.Toggle(removeFog, "Remove standard distance fog");
				//EditorGUILayout.LabelField("Remove skybox from camera",GUILayout.Width(48));
				removeSkybox = GUILayout.Toggle(removeSkybox, "Remove default skybox from camera");
				if (Camera.main == null && !createAMainCamera) 
				{
					createCubemapProbeObj = GUILayout.Toggle (createCubemapProbeObj, "Add trueSKY Cubemap Probe to trueSKY Object (will replace any existing)");

					createCubemapProbeCam = false;  	// set to false as no main camera, so can't attach to cam
				} 
				else 
				{
					createCubemapProbeCam = GUILayout.Toggle (createCubemapProbeCam, "Add trueSKY Cubemap Probe to main camera (will replace any existing)");

					createCubemapProbeObj = false;	 	// want to assign to main cam as it exists, so don't allow option to assign to obj
				}

				GUILayout.Label ("\n\nTo view more information on using trueSKY for Unity, along with code reference\npages and a detailed explanation of the sequencer, please click the button below\n");

				if (GUILayout.Button ("Launch Documentation"))
					Application.OpenURL ("http://docs.simul.co/unity");

			}
			GUILayout.FlexibleSpace();
			textStyle.alignment = TextAnchor.MiddleRight;
			GUILayout.Label(GetBottomText(), textStyle);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (stage == Stage.START)
			{
				if (GUILayout.Button("Cancel"))
					Close();
				if (sceneFilename.Length == 0)
					GUI.enabled = false;
				if (GUILayout.Button("Next"))
					OnWizardNext();
				if (sceneFilename.Length == 0)
					GUI.enabled = true;
			}
			else if (stage < Stage.FINISH)
			{
				if (GUILayout.Button("Back"))
					OnWizardBack();
				if (GUILayout.Button("Next"))
					OnWizardNext();
			}
			else
			{
				if (GUILayout.Button("Back"))
					OnWizardBack();
				if (GUILayout.Button("Finish"))
				{
					Finish();
					Close();
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
		}

		string GetBottomText()
		{
			if (stage == Stage.START)
				return "Click Next to begin.";
			else if (stage < Stage.FINISH)
				return "Click Next to proceed.";
			else
				return "Click Finish to create the trueSKY assets, objects, and components.";
		}

		// When the user pressed the "Apply" button OnWizardOtherButton is called.
		void OnWizardOtherButton()
		{
			Close();
		}

		void OnWizardBack()
		{
			stage--;
		}

		Sequence        sequence = null;
		Camera          mainCamera = null; 
		trueSKY         trueSky = null;
		GameObject      lightGameObject = null;
		TrueSkyLight    lightComponent;

		public bool     removeFog = true;
		public bool     removeSkybox = true;
		public bool     createCubemapProbeCam = true;
		public bool     createCubemapProbeObj = true;
		public bool     multipleCameras = false;  
		public bool     createAMainCamera = false;

		string          sceneFilename;

		void FindSequence()
		{
			if (sequence == null) 				 	// to stop GUILayout.ObjectField selections being overwritten
			{
                string projPath     = UnityEngine.Application.dataPath;
                string relativePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                relativePath        = relativePath.Remove(0, 7);    //remove Assets/
                string curScenPath  = projPath + "/" + relativePath;
                UnityEngine.Debug.Log("Current scene path:" + curScenPath);     
                           
				// 1. Is there a sequence asset in the current scene's assets directory?
				string dir = Path.GetDirectoryName (curScenPath);
				// Find any sequence asset:
				string[] assetFiles = Directory.GetFiles (dir, "*.asset");
				foreach (string p in assetFiles) 
				{
					Sequence sq = AssetDatabase.LoadAssetAtPath (p, typeof(Sequence)) as Sequence;
					if (sq != null) 
					{
						sequence = sq;
					}
				}
			}
		}

        void FindCamera()
		{
			// Now we find the main camera, and add the TrueSkyCamera.cs script to it, IF it is not already present:
			if (mainCamera == null) 				// to stop GUILayout.ObjectField selections being overwritten
				mainCamera = Camera.main;
		}

		void Finish()
		{   
			TrueSkyCamera trueSkyCamera;

			if (sequence == null)
			{
                // Build asset path and name (it has to be relative)
                string relativePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

                string sequenceFilename = relativePath.Replace(".unity", "_sq.asset");
				sequence = CustomAssetUtility.CreateAsset<Sequence>(sequenceFilename);
			}
			if (createAMainCamera) 		// if user has requested a main camera to be created (as none already)
			{  
				GameObject MainCam = new GameObject("Main Camera"); 
				MainCam.gameObject.AddComponent<Camera>();
				MainCam.tag = "MainCamera";
				mainCamera = MainCam.GetComponent<Camera>();
			}
			if (multipleCameras) 	// if user has requested the script o be assigned to cameras
			{   
				Camera[] cams = new Camera[Camera.allCamerasCount];    		 // find all cameras
				if (Camera.allCamerasCount >= 1)		
					Array.Copy (Camera.allCameras, cams, Camera.allCamerasCount);  

				for (int i = 0; i < cams.Length; i++) 	 
				{  
					trueSkyCamera = cams[i].gameObject.GetComponent<TrueSkyCamera>();
					if(trueSkyCamera == null)
						cams[i].gameObject.AddComponent <TrueSkyCamera> ();
				}  

			} 
			if (mainCamera==null)         			// if mainCamera still = null, inform user script wasn't assigned + how to assign it
			{
				if (Camera.allCamerasCount < 1)
					UnityEngine.Debug.LogWarning ("Can't find any cameras for trueSky Camera script. Please add a camera manually and repeat the wizard to assign the script to the camera of your choice/all cameras. Alternatively, check the option to automatically create a main camera with the script assigned.");
				else if (!multipleCameras)
					UnityEngine.Debug.LogWarning ("Can't find a main camera for trueSKy Camera script, but other cameras found. Repeat the wizard and assign the script to the camera of your choice/all cameras");
			} 
			else 
			{ 
				trueSkyCamera = mainCamera.gameObject.GetComponent<TrueSkyCamera> ();
				if (trueSkyCamera == null)
					mainCamera.gameObject.AddComponent<TrueSkyCamera> ();
			}
			if (trueSky == null)
			{
				GameObject g = new GameObject("trueSky");
				trueSky = g.AddComponent<trueSKY>();
			}
			if (createCubemapProbeCam || createCubemapProbeObj) {  			// must be after trueSKY obj assigned, in case assigning probe to this instead of mainCam
 
				UnityEngine.Object[] objects = FindObjectsOfType(typeof(TrueSkyCubemapProbe));

				foreach (UnityEngine.Object t in objects) 		// Destroy any other cubemap probes 
				{
					MonoBehaviour b = (MonoBehaviour)t;
					if (b.GetComponent<TrueSkyCubemapProbe> () != null)
						DestroyImmediate (b.GetComponent<TrueSkyCubemapProbe> ());
				}

				if (createCubemapProbeCam) 
					mainCamera.gameObject.AddComponent<TrueSkyCubemapProbe> ();

				else if (createCubemapProbeObj) 	
					trueSky.gameObject.AddComponent<TrueSkyCubemapProbe> ();

				Material trueSKYSkyboxMat = Resources.Load ("trueSKYSkybox", typeof(Material)) as Material;
				RenderSettings.skybox = trueSKYSkyboxMat;
			}  

            // If there is not light on the scene, add one:
            if(lightGameObject == null)
            {
                lightGameObject = new GameObject("TrueSkyLight");
                Light dirLight  = lightGameObject.AddComponent<Light>();
                dirLight.type   = LightType.Directional;
                lightComponent  = lightGameObject.AddComponent<TrueSkyLight>();
            }
            // If there is a light, but without the component, add it:
			if (lightComponent == null)
			{
                lightComponent = lightGameObject.AddComponent<TrueSkyLight>();
            }
			if (removeFog)
			{
				RenderSettings.fog = false;
			}
			if (removeSkybox && mainCamera != null)
			{
				if (mainCamera.clearFlags != CameraClearFlags.SolidColor)
				{
					mainCamera.clearFlags = CameraClearFlags.SolidColor;
					mainCamera.backgroundColor = Color.black;
				}
			}

			// Now the sequence must be assigned to the trueSKY object.
			trueSky.sequence    = sequence;
			trueSky.time        = 0.5F;
		}

		void FindTrueSky()
		{
			// And we need a trueSKY object in the scene.
			UnityEngine.Object[] trueSkies;
			trueSkies = FindObjectsOfType(typeof(trueSKY));
			foreach (UnityEngine.Object t in trueSkies)
			{
				trueSky = (trueSKY)t;
			}
		}
		
		void OnWizardNext()
		{
			stage++;
		}
	}
}