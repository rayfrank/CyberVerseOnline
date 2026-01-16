using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InfoItem : MonoBehaviour
{
    // Start is called before the first frame update
    public void Init(string text, int time) {
        TextMeshProUGUI textComponent = GetComponent<TextMeshProUGUI>();
        textComponent.text = $"{text}";
        Destroy(gameObject, time);
    }
}
