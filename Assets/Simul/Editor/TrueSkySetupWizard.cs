using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Runtime.InteropServices;
using System;
using System.Text;
using System.Diagnostics;
using System.IO;

#if USING_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif


namespace simul
{
	public class TrueSkySetupWizard : EditorWindow
	{
		public TrueSkySetupWizard()
		{
			minSize = new Vector2(400.0F, 300.0F);
			maxSize = new Vector2(400.0F, 300.0F);
		}
		enum Stage
		{
			PRE_START, START, FIND_SEQUENCE, FIND_CAMERA, FIND_TRUESKY, FIND_SUN, FINISH
		};
		Stage stage = Stage.PRE_START;

		//Be careful adding more than 6 due to UI spacing issues.
		string[] currentIssues = {
			"Standard: No Dynamic Lighting with Lightning Strikes in clouds",
			"HDRP: Lightning strikes not present",
			"Using Manual cloud positioning can cause irregular cloud movement",
		};

		[MenuItem("GameObject/Remove trueSKY from Scene", false, 200000)]
		public static void RemoveTrueSky()
		{
			UnityEngine.Object[] objects = FindObjectsOfType(typeof(Light));
			foreach (UnityEngine.Object t in objects)
			{
				Light l = (Light)t;
				if (l.GetComponent<TrueSkyDirectionalLight>() != null)
					DestroyImmediate(l.GetComponent<TrueSkyDirectionalLight>());
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

		[MenuItem("GameObject/Initialize trueSKY in Scene", false, 100000)]
		public static void InitTrueSkySequence()
		{
			TrueSkySetupWizard w = (TrueSkySetupWizard)EditorWindow.GetWindow(typeof(TrueSkySetupWizard), true, "Initialize trueSKY", true);
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
			textStyle.margin = new RectOffset(12, 12, 4, 4);
			textStyle.wordWrap = true;

			GUIStyle defaultButtonStyle = new GUIStyle(GUI.skin.button);
			if (EditorGUIUtility.isProSkin)
			{
				textStyle.normal.textColor = Color.white;
				defaultButtonStyle.normal.textColor = Color.white;
			}
			else
			{
				defaultButtonStyle.normal.textColor = Color.black;
			}
			GUILayout.Label("Initialize trueSKY in Scene", EditorStyles.boldLabel);

			if (stage == Stage.START)
			{
				GetSceneFilename();
				if (sceneFilename.Length > 0)
					GUILayout.Label("This wizard will initialize trueSKY for the current scene.\n\n\tScene: " + sceneFilename, textStyle);
				else
				{
					GUILayout.Label("This wizard will initialize trueSKY for the current scene.\nThe current scene has not yet been saved - plase do this first, so the wizard knows where to put the trueSKY data.", textStyle);
				}
#if USING_HDRP
				GUILayout.Label("trueSKY will configure for HDRP", textStyle);
#endif
#if USING_URP
			GUILayout.Label("trueSKY does not currently support URP. Please make sure you are using either HDRP or standard", EditorStyles.boldLabel);
#endif
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
				if (Camera.main != null)                                    // Case 1: Main Camera exists 
				{
					GUILayout.Label("A main camera was found in the current scene. The TrueSkyCamera script will be assigned to this camera.", textStyle);

					if (Camera.allCamerasCount > 1)                         // Case 2: The are other cameras (in addition to Main Camera)	
					{
						GUILayout.Label("Additionally, check the box to assign the script to all cameras.", textStyle);
						multipleCameras = GUILayout.Toggle(multipleCameras, "Assign TrueSkyCamera to all cameras");
					}

					mainCamera = Camera.main;
				}
				else if (Camera.allCamerasCount >= 1)                       // Case 3: The are other cameras (but no Main Camera)
				{
					GUILayout.Label("No main camera was found in the current scene. You can select another camera or check the option to make a new main camera and assign it to this.", textStyle);
					GUILayout.Label("Additionally, check the box to assign the script to all cameras", textStyle);
					createAMainCamera = GUILayout.Toggle(createAMainCamera, "Create a new Main Camera and attach the TrueSkyCamera script to it.");
					multipleCameras = GUILayout.Toggle(multipleCameras, "Assign TrueSkyCamera to all cameras");

				}
				else                                                        // Case 4: No Cameras
				{
					GUILayout.Label("No main camera or other cameras found in the current scene. You can select one or check the box to have one created for you, and the script assigned to this.", textStyle);
					createAMainCamera = GUILayout.Toggle(createAMainCamera, "Create a new Main Camera and attach the TrueSkyCamera script to it.");
				}

				if (!createAMainCamera && mainCamera == null)                                       // Don't give option to select other camera if there is/will be a main camera
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

				lights = FindObjectsOfType(typeof(Light)) as Light[];
				int directionalLights = 0;

				foreach (Light t in lights)
				{
					Light l = (Light)t;
					if (l.type == LightType.Directional)
					{
						directionalLights++;
						lightGameObject = l.gameObject;
					}
				}
				if (directionalLights == 0)
				{
					GUILayout.Label("No directional light on scene, one will be created.", textStyle);
				}
				else if (directionalLights == 1)
				{
					GUILayout.Label("A Directional Light was found in the scene, the trueSKY Light Script will be applied.", textStyle);
					lightComponent = lightGameObject.GetComponent<TrueSkyDirectionalLight>();
				}
				else if (directionalLights >= 1)
				{
					GUILayout.Label("There's 1 or more directional lights on the scene. TrueSKY only needs one directional light.", textStyle);
					lightComponent = lightGameObject.GetComponent<TrueSkyDirectionalLight>();
				}
			}
			if (stage == Stage.FINISH)
			{
				createCubemapProbe = GUILayout.Toggle(createCubemapProbe, "Add trueSKY Cubemap Probe to trueSKY Object");
				removeFog = GUILayout.Toggle(removeFog, "Remove standard distance fog");
				removeSkybox = GUILayout.Toggle(removeSkybox, "Remove default skybox from camera");

				//if (Camera.main == null && !createAMainCamera) 
				//{
				//	createCubemapProbeCam = false;  	// set to false as no main camera, so can't attach to cam
				//} 
				//else 
				//{
				//	createCubemapProbeCam = GUILayout.Toggle (createCubemapProbeCam, "Add trueSKY Cubemap Probe to main camera (will replace any existing)", textStyle);
				//	createCubemapProbeObj = false;	 	// want to assign to main cam as it exists, so don't allow option to assign to obj
				//}

				GUILayout.Label("\n\nTo view more information on using trueSKY for Unity, along with code reference pages and a detailed explanation of the sequencer, please click the button below.", textStyle);

				if (GUILayout.Button("Launch Documentation", defaultButtonStyle))
					Application.OpenURL("https://docs.simul.co/unity");

				GUILayout.Label("\n\nOur currently known issues thread in our Q&A forum will let you see what we are working on.", textStyle);

				if (GUILayout.Button("Currently known issues", defaultButtonStyle))
					Application.OpenURL("https://simul.co/question/currently-known-issues-in-the-latest-update/");

				textStyle.fontSize = 0; //default
				textStyle.alignment = TextAnchor.UpperLeft;

				for (int i = 0; i< currentIssues.Length; ++i)
				{
					string issuePrefix = "\n" + (i+1) + ". ";
					GUILayout.Label(issuePrefix + currentIssues[i], textStyle);
				}


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
				if (GUILayout.Button("Next", defaultButtonStyle))
					OnWizardNext();
				if (sceneFilename.Length == 0)
					GUI.enabled = true;
			}
			else if (stage < Stage.FINISH)
			{
				if (GUILayout.Button("Back"))
					OnWizardBack();
				if (GUILayout.Button("Next", defaultButtonStyle))
					OnWizardNext();
			}
			else
			{
				minSize = new Vector2(550.0F, 550.0F);
				maxSize = new Vector2(550.0F, 550.0F);
				if (GUILayout.Button("Back"))
					OnWizardBack();
				if (GUILayout.Button("Finish", defaultButtonStyle))
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

		Sequence sequence = null;
		Camera mainCamera = null;
		trueSKY trueSky = null;
		GameObject lightGameObject = null;
		TrueSkyDirectionalLight lightComponent;

		public bool removeFog = true;
		public bool removeSkybox = true;
		public bool createCubemapProbe = true;
		public bool multipleCameras = false;
		public bool createAMainCamera = false;

		string sceneFilename;

		void FindSequence()
		{
			if (sequence == null)                   // to stop GUILayout.ObjectField selections being overwritten
			{
				string projPath = UnityEngine.Application.dataPath;
				string relativePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
				relativePath = relativePath.Remove(0, 7);    //remove Assets/
				string curScenPath = projPath + "/" + relativePath;
				//UnityEngine.Debug.Log("Current scene path:" + curScenPath);     

				// 1. Is there a sequence asset in the current scene's assets directory?
				string dir = Path.GetDirectoryName(curScenPath);
				// Find any sequence asset:
				string[] assetFiles = Directory.GetFiles(dir, "*.asset");
				foreach (string p in assetFiles)
				{
					Sequence sq = AssetDatabase.LoadAssetAtPath(p, typeof(Sequence)) as Sequence;
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
			if (mainCamera == null)                 // to stop GUILayout.ObjectField selections being overwritten
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
			if (trueSky == null)
			{
				GameObject g = new GameObject("trueSky");
				trueSky = g.AddComponent<trueSKY>();
			}

			// Open tag+Layer manager
			SerializedObject tagLayerManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
			SerializedProperty layersProp = tagLayerManager.FindProperty("layers");

			// Adding a Layer/Tag
			string ts_layer = "trueSKY";
			int ts_layer_index = trueSky.trueSKYLayerIndex;
			// First check if it is not already present
			bool found = false;

			var newLayer = LayerMask.NameToLayer("trueSKY");
			if (newLayer > -1)
			{
				found = true;
			}

			// if not found, add it
			if (!found)
			{
				layersProp.InsertArrayElementAtIndex(ts_layer_index);
				SerializedProperty n = layersProp.GetArrayElementAtIndex(ts_layer_index);
				n.stringValue = ts_layer;
			}
			tagLayerManager.ApplyModifiedProperties();


			if (createAMainCamera)      // if user has requested a main camera to be created (as none already)
			{
				GameObject MainCam = new GameObject("Main Camera");
				MainCam.gameObject.AddComponent<Camera>();
				MainCam.tag = "MainCamera";
				mainCamera = MainCam.GetComponent<Camera>();
#if USING_HDRP
				MainCam.AddComponent<HDAdditionalCameraData>();
#endif
			}

			if (multipleCameras)    // if user has requested the script to be assigned to all cameras
			{
				Camera[] cams = new Camera[Camera.allCamerasCount];          // find all cameras
				if (Camera.allCamerasCount >= 1)
					Array.Copy(Camera.allCameras, cams, Camera.allCamerasCount);

				for (int i = 0; i < cams.Length; i++)
				{
					trueSkyCamera = cams[i].gameObject.GetComponent<TrueSkyCamera>();
					if (trueSkyCamera == null)
					{
#if !USING_HDRP
						cams[i].gameObject.AddComponent<TrueSkyCamera>();
#endif
						cams[i].gameObject.layer = ts_layer_index;
					}
				}
			}
			if (mainCamera == null)                     // if mainCamera still = null, inform user script wasn't assigned + how to assign it
			{
				if (Camera.allCamerasCount < 1)
					UnityEngine.Debug.LogWarning("Can't find any cameras for trueSky Camera script. Please add a camera manually and repeat the wizard to assign the script to the camera of your choice/all cameras. Alternatively, check the option to automatically create a main camera with the script assigned.");
				else if (!multipleCameras)
					UnityEngine.Debug.LogWarning("Can't find a main camera for trueSKy Camera script, but other cameras found. Repeat the wizard and assign the script to the camera of your choice/all cameras");
			}
			else
			{
#if USING_HDRP
				simul.TrueSkyHDRPCustomPass TrueSkyMainPass = new simul.TrueSkyHDRPCustomPass();
                simul.TrueSkyHDRPCustomPass TrueSkyTranslucentPass = new simul.TrueSkyHDRPCustomPass();
				simul.TrueSkyHDRPCustomPass TrueSkyOverlayPass = new simul.TrueSkyHDRPCustomPass();
				simul.TrueSkyHDRPCustomPass TrueSkyUIPass = new simul.TrueSkyHDRPCustomPass();
				CustomPassVolume MainPassVolume = trueSky.gameObject.GetComponent<CustomPassVolume>();
                if (MainPassVolume == null)
                {
                    TrueSkyMainPass.name = "trueSKY - Before Pre Refraction(Main Render)";
                    MainPassVolume = trueSky.gameObject.AddComponent<CustomPassVolume>();
                    MainPassVolume.injectionPoint = CustomPassInjectionPoint.BeforePreRefraction;
                    MainPassVolume.customPasses.Add(TrueSkyMainPass);

                    CustomPassVolume TranslucentVolume;
                    TrueSkyTranslucentPass.name = "trueSKY - Before Post Process(Translucent Effects)";
                    TranslucentVolume = trueSky.gameObject.AddComponent<CustomPassVolume>();
                    TranslucentVolume.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;
                    TranslucentVolume.customPasses.Add(TrueSkyTranslucentPass);

					CustomPassVolume OverlayVolume;
					TrueSkyOverlayPass.name = "trueSKY - After Post Process(Overlay)";
					OverlayVolume = trueSky.gameObject.AddComponent<CustomPassVolume>();
					OverlayVolume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
					OverlayVolume.customPasses.Add(TrueSkyOverlayPass);
					TrueSkyOverlayPass.enabled = false; //disabled by default. 

                    CustomPassVolume UIVolume;
                    TrueSkyUIPass.name = "trueSKY - After Everything";
                    UIVolume = trueSky.gameObject.AddComponent<CustomPassVolume>();
                    UIVolume.injectionPoint = CustomPassInjectionPoint.AfterOpaqueDepthAndNormal;
                    UIVolume.customPasses.Add(TrueSkyUIPass);
                    TrueSkyUIPass.enabled = true; 
                }
                if (UnityEngine.Rendering.GraphicsSettings.allConfiguredRenderPipelines.Length > 0)
				{
					trueSky.HDRP_RenderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.allConfiguredRenderPipelines[0];
				}
#else
					trueSkyCamera = mainCamera.gameObject.GetComponent<TrueSkyCamera>();
					if (trueSkyCamera == null)
						mainCamera.gameObject.AddComponent<TrueSkyCamera>();
#endif
				mainCamera.gameObject.layer = ts_layer_index;
			}
			if (createCubemapProbe)
			{           // must be after trueSKY obj assigned, in case assigning probe to this instead of mainCam

				UnityEngine.Object[] objects = FindObjectsOfType(typeof(TrueSkyCubemapProbe));

				if (trueSky.gameObject.GetComponent<TrueSkyCubemapProbe>() != null)
					DestroyImmediate(trueSky.gameObject.GetComponent<TrueSkyCubemapProbe>());

				trueSky.gameObject.AddComponent<TrueSkyCubemapProbe>();

				Material trueSKYSkyboxMat = Resources.Load("trueSKYSkybox", typeof(Material)) as Material;
				RenderSettings.skybox = trueSKYSkyboxMat;
			}
			// If there is not light on the scene, add one:
			if (lightGameObject == null)
			{
				lightGameObject = new GameObject("TrueSkyDirectionalLight");
				Light dirLight = lightGameObject.AddComponent<Light>();
				dirLight.type = LightType.Directional;
				lightComponent = lightGameObject.AddComponent<TrueSkyDirectionalLight>();
			}
			// If there is a light, but without the component, add it:
			if (lightComponent == null)
			{
				lightComponent = lightGameObject.AddComponent<TrueSkyDirectionalLight>();
			}
			RenderSettings.sun = lightGameObject.GetComponent<Light>();

#if USING_HDRP
			lightComponent.Units = TrueSkyDirectionalLight.LightUnits.Photometric;
#else
			lightComponent.Units = TrueSkyDirectionalLight.LightUnits.Radiometric;
#endif
			if (removeFog)
			{
				RenderSettings.fog = false;
			}
			if (removeSkybox && mainCamera != null)
			{
#if USING_HDRP
				HDAdditionalCameraData mHDAdditionalCameraData = mainCamera.GetComponent<HDAdditionalCameraData>();

				if (mHDAdditionalCameraData)
				{
					mHDAdditionalCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
					mHDAdditionalCameraData.backgroundColorHDR = Color.black;
				}
#endif
				if (mainCamera.clearFlags != CameraClearFlags.SolidColor)
				{
					mainCamera.clearFlags = CameraClearFlags.SolidColor;
					mainCamera.backgroundColor = Color.black;
				}
			}
			if(mainCamera!=null)
			{ 
				// Set the Near and Far clipping planes on the main camera.
				mainCamera.nearClipPlane = 0.1f;
				mainCamera.farClipPlane = 300000.0f;
			}
			// Now the sequence must be assigned to the trueSKY object.
			trueSky.sequence = sequence;
			trueSky.TrueSKYTime = 12.0F;
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