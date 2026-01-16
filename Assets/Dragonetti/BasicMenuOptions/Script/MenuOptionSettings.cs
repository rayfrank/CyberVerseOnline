using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BasicMenuOptions{
public class MenuOptionSettings : MonoBehaviour
{
    [Header("Time scale and cursor disappear in these script")]
    [SerializeField]
    private GameObject canvasOptions;

    public bool menuOpen;
    
    void Start()
    {
        menuOpen = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuOpen)
                MenuOpen();
            else
                MenuClose();
        }
    }

    public void MenuOpen()
    {
        if (menuOpen)
        {
            Time.timeScale = 0;
            canvasOptions.SetActive(true);
            menuOpen = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

        }
    }

    public void MenuClose()
    {
        if (!menuOpen)
        {
            Time.timeScale = 1;
            canvasOptions.SetActive(false);
            menuOpen = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

}
}
