using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class reticle : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    //private bool isOn = false;
    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = false;
    }
    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            spriteRenderer.enabled = true;
            //isOn = !isOn; // 現在の値を反転
            //spriteRenderer.enabled = isOn; // 反転した値を適用
        }
    }
}
