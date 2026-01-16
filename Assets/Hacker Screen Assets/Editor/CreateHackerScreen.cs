/*
 * 
 * Developed by Olusola Olaoye, 2021
 * 
 * To only be used by those who purchased from the Unity asset store
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class CreateHackerScreen 
{

    private const string hacker_screen_parent_name = "Hacker Screens";


    [MenuItem("GameObject/Hacker Screen", false, -1)]
    private static void createHackerScreen()
    {

        GameObject hacker_screens_parent; // the parent object of the hacker screens

        if(GameObject.Find(hacker_screen_parent_name)) // if parent object exists
        {
            hacker_screens_parent = GameObject.Find(hacker_screen_parent_name);
        }
        else // if parent object does not exist
        {
            hacker_screens_parent = new GameObject(hacker_screen_parent_name);
        }

        // find hacker screen in resources folder
        Object hacker_screen_prefab = Resources.Load("prefabs/Hacker Screen");

        GameObject hacker_screen = (GameObject)  GameObject.Instantiate(hacker_screen_prefab, Vector3.zero, Quaternion.identity); // instantiate hacker screen

        hacker_screen.transform.SetParent(hacker_screens_parent.transform); // set parent

        hacker_screen.name = "Hacker Screen " + GameObject.FindObjectsOfType<CommandWriter>().Length; // name hacker screen
    }
}
