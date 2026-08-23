using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotation2 : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W) | Input.GetKey(KeyCode.A))
        {
            transform.Rotate(-60.0f, 0f, 0f);
        }
        else if (Input.GetKey(KeyCode.S) | Input.GetKey(KeyCode.D))
        {
            transform.Rotate(4.0f, 0f, 0f);
        }
    }
}
