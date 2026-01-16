using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoPanel : MonoBehaviour {

    public static InfoPanel instance = null;
    public int messageTime;
    [SerializeField] private GameObject infoPrefab;

    void Awake() {
        instance = this;
    }

    public void AddMessage(string message) {
        GameObject infoObject = Instantiate(infoPrefab, transform);
        infoObject.GetComponent<InfoItem>().Init(message, messageTime);
    }

}
