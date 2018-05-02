using UnityEngine;
using UnityEditor;
using System.IO;

namespace simul
{
	public static class CustomAssetUtility
	{
		public static T CreateAsset<T>(string assetPathAndName = "") where T : ScriptableObject
		{
			T asset = ScriptableObject.CreateInstance<T>();
			if (assetPathAndName.Length == 0)
			{
				string path = AssetDatabase.GetAssetPath(Selection.activeObject);
				if (path == "")
				{
					path = "Assets";
				}
				else if (Path.GetExtension(path) != "")
				{
					path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
				}

				assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + ".asset");
			}
            Debug.Log(assetPathAndName);
			AssetDatabase.CreateAsset(asset, assetPathAndName);

			AssetDatabase.SaveAssets();
			EditorUtility.FocusProjectWindow();
			return asset;
		}
	}
}