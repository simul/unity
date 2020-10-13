using UnityEngine;
using System.Collections;
namespace simul
{
	[System.Serializable]
	public class Sequence : ScriptableObject
	{
		Sequence()
		{
           SequenceAsText = "";
		}
		~Sequence()
		{
		}
		public string SequenceAsText;
		public void Load(string source)
		{
			SequenceAsText = source;
		}
		public void Init()
		{
			UnityEngine.Debug.Log("Sequence Init");
		}
	}
}