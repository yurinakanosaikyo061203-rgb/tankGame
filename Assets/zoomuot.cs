// 例: マウスホイールで3Dカメラをズームイン/アウトする
using UnityEngine;

public class zoomuot : MonoBehaviour
{
    public Camera mainCam; // InspectorでMain Cameraをドラッグ＆ドロップ
    public float zoomSpeed = 10f;
    public float minFov = 15f;
    public float maxFov = 90f;

    void Update()
    {
        // マウスホイールのスクロール値を取得
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // Field of Viewを更新
        mainCam.fieldOfView -= scroll * zoomSpeed;

        // 値の範囲を制限
        mainCam.fieldOfView = Mathf.Clamp(mainCam.fieldOfView, minFov, maxFov);
    }
}
