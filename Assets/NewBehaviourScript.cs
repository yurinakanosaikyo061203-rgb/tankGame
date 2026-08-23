// スクリプト例（カメラにアタッチする）
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public float distanceFromCamera = 10f; // カメラからの距離
    public float speed = 5f; // 追従速度

    void Update()
    {
        // 1. マウスのスクリーン座標を取得
        Vector3 mouseScreenPosition = Input.mousePosition;

        // 2. スクリーン座標をワールド座標に変換
        // カメラからの距離を指定する必要がある
        mouseScreenPosition.z = distanceFromCamera;
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

        // 3. マウスの方向にカメラを向ける
        // transform.LookAt(mouseWorldPosition);

        // 3. カメラをスムーズにマウスの方向へ向かせる（スムーズな回転）
        Vector3 targetDirection = mouseWorldPosition - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
    }
}
