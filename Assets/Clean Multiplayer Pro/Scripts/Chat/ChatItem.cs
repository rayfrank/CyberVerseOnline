#if CMPSETUP_COMPLETE
using TMPro;
using UnityEngine;

public class ChatItem : MonoBehaviour
{
    public void Init(bool isLeft, Chat chat)
    {
        var textComponent = GetComponent<TextMeshProUGUI>();
        textComponent.text = isLeft
            ? $"<uppercase><b>{chat.Sender}</b></uppercase>\n{chat.Message}"
            : $"<color=orange><uppercase><b>{chat.Sender}</b></uppercase></color>\n{chat.Message}";
    }
}

#endif