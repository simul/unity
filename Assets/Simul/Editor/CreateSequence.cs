using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Runtime.InteropServices;
using System;
using System.Text;
using System.Diagnostics;

namespace simul
{
	public class CreateSequence
	{
		[MenuItem("Assets/Create/trueSKY Sequence", false, 1000)]
		public static void CreateSequenceAsset()
		{
			DirectoryCopy.CopyPluginsAndGizmosToAssetsFolder();
			Sequence asset = CustomAssetUtility.CreateAsset<Sequence>();
			Selection.activeObject = asset;
		}
	}
}