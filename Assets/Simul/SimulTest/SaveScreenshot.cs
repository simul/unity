using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SaveScreenshot : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
	{
		string[] args = System.Environment.GetCommandLineArgs();
		foreach (string a in args)
		{
			try
			{
				string[] parts = a.Split('=');
				if (parts.Length == 2)
				{
					if (parts[0].CompareTo("-screenshotfile") == 0)
						filename = parts[1];
					if (parts[0].CompareTo("-wait") == 0)
						waitCount = System.Int32.Parse(parts[1]);
				}
				Debug.developerConsoleVisible = false;
			}
			catch (System.Exception)
			{

			}
		}
		UnityEngine.Debug.Log("screenshotfile " + filename);
		UnityEngine.Debug.Log("waitCount " + waitCount);
	}
	bool got_screenshot = false;
	int waitCount = 25;
	string filename = "screenshot.png";
	private void Update()
	{
		//Wait for 25 frames
		if (UnityEngine.Time.renderedFrameCount < waitCount)
			return;

        if (!got_screenshot)
        {
            try
            {
                string fullPath = filename;
				if (!filename.Contains(":"))
					fullPath=Application.persistentDataPath + "/" + filename;
				fullPath = fullPath.Replace("\\", "/");
				string path = fullPath.Substring(0, fullPath.LastIndexOf('/'));
				string name = fullPath.Substring(path.Length + 1);
				UnityEngine.Debug.Log("Creating directory "+ path);
				System.IO.Directory.CreateDirectory(path);
				Texture2D texture2d = ScreenCapture.CaptureScreenshotAsTexture(1);
				// Encode texture into PNG
				byte[] bytes;
				if(filename.Contains(".png"))
					bytes = texture2d.EncodeToPNG();
				else if (filename.Contains(".exr"))
					bytes = texture2d.EncodeToEXR();
				else
					bytes= texture2d.EncodeToJPG(100);
				UnityEngine.Debug.Log("Saving to "+ fullPath);
                // For testing purposes, also write to a file in the project folder
				FileStream fileStream = new FileStream(
					  fullPath, FileMode.Create,
					  FileAccess.ReadWrite, FileShare.ReadWrite);
				fileStream.Write(bytes, 0, bytes.Length);
				fileStream.Close();
				got_screenshot = true;
				Object.Destroy(texture2d);
			}
            catch (System.Exception e)
			{
				UnityEngine.Debug.Log(e.ToString());
    			Application.Quit();
            }
        }
        else
			Application.Quit();
	}
}
