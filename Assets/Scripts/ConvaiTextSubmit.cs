using UnityEngine;
using TMPro;
using Convai.Scripts.Runtime.Core;

public class ConvaiTextSubmit : MonoBehaviour
{
    public ConvaiNPC npc;

    private bool isListening = false;

    void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                npc.StartListening();
                isListening = true;
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                if (isListening)
                {
                    npc.StopListening();
                    isListening = false;
                }
            }
        }
#else
        // Optional: mouse support in Editor
        if (Input.GetMouseButtonDown(0))
        {
            npc.StartListening();
            isListening = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            npc.StopListening();
            isListening = false;
        }
#endif
    }
}
