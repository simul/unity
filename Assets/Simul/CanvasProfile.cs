using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using simul;

public class CanvasProfile : MonoBehaviour
{
    private UnityEngine.UI.Text mText;
    public bool Show = false;

    private void Start()
    {
        mText = GetComponent<UnityEngine.UI.Text>();   
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) || Show)
        {
            mText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width);
            mText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.height);
            mText.text = trueSKY.GetTrueSky().GetRenderString("Profiling");
        }
    }
}
