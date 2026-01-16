using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BasicMenuOptions
{

    public class CameraController : MonoBehaviour
    {

        public GameObject mover;
        public MenuOptionSettings optionSettings;

        private Vector2 turn;

        public Slider sensitivityValue;


        void Update() // Example of how can we use sentivity slider.
        {

            if (optionSettings.menuOpen)
            {
                turn.x += Input.GetAxis("Mouse X") * sensitivityValue.value;
                turn.y += Input.GetAxis("Mouse Y") * sensitivityValue.value;
                mover.transform.localRotation = Quaternion.Euler(0, turn.x, 0);
                this.transform.localRotation = Quaternion.Euler(-turn.y, 0, 0);
            }

        }
    }
}
