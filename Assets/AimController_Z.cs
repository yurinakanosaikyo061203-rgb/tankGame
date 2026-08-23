using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AimController_Z : MonoBehaviour
{
    public Vector3 targetPos;
    public Image aimImage;

    void Update()
    {
        // 「マウスの位置」と「照準器の位置」を同期させる。
        transform.position = Input.mousePosition;

        RaycastHit hit;

        // MainCameraからマウスの位置にRayを飛ばす
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);


        if (Physics.Raycast(ray, out hit))
        {
            // RayがColliderと衝突した地点の座標を取得
            targetPos = hit.point;
           

            if (hit.transform.CompareTag("Enemy"))
            {
                // 照準器を赤色に変化させる。
                aimImage.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
            }
            else
            {
                // 照準器の色は白
                aimImage.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }
        }
        else
        {
            float distance = 100f;
            targetPos = ray.origin + ray.direction * distance;
            aimImage.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        }
    }
}
