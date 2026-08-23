using UnityEngine;

public class CameraLookAtMouse : MonoBehaviour
{
    // カメラコンポーネント（自動的に取得される）
    private Camera mainCamera;

    void Start()
    {
        // メインカメラのコンポーネントを取得
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found! Please tag a camera as 'MainCamera'.");
        }
    }

    void Update()
    {
        // マウスカーソルのスクリーン座標を取得
        Vector3 mousePosition = Input.mousePosition;

        // スクリーン座標からワールド座標へ向かうレイを生成
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        // レイがコライダーに衝突したか判定
        // ここでは "Ground" レイヤーを持つオブジェクトのみを対象とする例
        // 必要に応じてレイヤーマスクを調整してください
        int layerMask = LayerMask.GetMask("Ground");

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            // 衝突地点のワールド座標を取得
            Vector3 targetPoint = hit.point;

            // カメラを衝突地点に向ける
            // LookAtはワールド座標を引数にとり、その点を見るようにカメラの向きを変更する
            transform.LookAt(targetPoint);
        }
    }
}
