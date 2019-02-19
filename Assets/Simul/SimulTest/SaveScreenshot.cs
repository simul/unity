using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SaveScreenshot : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
	}
	bool got_screenshot = false;
	string filename = "screenshot.png";
	private void Update()
	{
		//Wait for 25 frames
		if (UnityEngine.Time.renderedFrameCount < 25)
			return;

        if (!got_screenshot)
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
                    }
                }
                catch (System.Exception)
                {

                }
            }
            try
            {
                string fullPath = filename;
				if (!filename.Contains(":"))
					fullPath=Application.dataPath + "/../Screenshots/" + filename;
				fullPath = fullPath.Replace("\\", "/");
				string path = fullPath.Substring(0, fullPath.LastIndexOf('/'));
				string name = fullPath.Substring(path.Length + 1);
				System.IO.Directory.CreateDirectory(path);
				Texture2D texture2d = ScreenCapture.CaptureScreenshotAsTexture(1);
                // Encode texture into PNG
                byte[] bytes = texture2d.EncodeToPNG();
                Object.Destroy(texture2d);
                // For testing purposes, also write to a file in the project folder
                File.WriteAllBytes(fullPath, bytes);
				got_screenshot = true;
			}
            catch (System.Exception e)
			{
				UnityEngine.Debug.Log(e.ToString());
				Application.Quit(1);
            }
        }
        else
            Application.Quit(0);
	}
}
