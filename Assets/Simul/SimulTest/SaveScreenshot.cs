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
                UnityEngine.Debug.Log("argument " + a);
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
                string fullPath = Application.dataPath + "/../" + filename;
                UnityEngine.Debug.Log("Trying to save " + fullPath);
                got_screenshot = true;
                Texture2D texture2d = ScreenCapture.CaptureScreenshotAsTexture(1);
                // Encode texture into PNG
                byte[] bytes = texture2d.EncodeToPNG();
                Object.Destroy(texture2d);
                // For testing purposes, also write to a file in the project folder
                File.WriteAllBytes(fullPath, bytes);
            }
            catch (System.Exception)
            {
                Application.Quit(1);
            }
        }
        else
            Application.Quit(0);
	}
}
