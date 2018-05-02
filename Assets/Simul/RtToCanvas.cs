using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RtToCanvas : MonoBehaviour
{
    public RenderTexture RtToShow;
    private RawImage mCanvasImage;

    private void Start()
    {
        mCanvasImage = GetComponent<RawImage>();
        mCanvasImage.texture = RtToShow;
    }

}
