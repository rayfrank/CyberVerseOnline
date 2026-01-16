using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResizeBackgroundToText : MonoBehaviour
{
    public TextMeshProUGUI textChild;

    private Image imageComponent;

    void Start() {
        imageComponent = GetComponent<Image>();
    }

    public void ResizeImage() {
        if (imageComponent == null) { 
            imageComponent = GetComponent<Image>();
        }
        float preferredWidth = LayoutUtility.GetPreferredWidth(textChild.rectTransform);
        float preferredHeight = LayoutUtility.GetPreferredHeight(textChild.rectTransform);

        imageComponent.rectTransform.sizeDelta = new Vector2(preferredWidth, preferredHeight);
    }
}
