using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BasicMenuOptions{

public class CharacterControl : MonoBehaviour
{
    public Vector3 deltaMove;
    public float speed = 1.0f;

    void Update()
    {
        deltaMove = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")) * speed * Time.deltaTime;
        this.transform.Translate(deltaMove);
    }


    }
}
