using UnityEngine;

public class turretrotate : MonoBehaviour
{
    float rotationSpeed = 8.0f;
    float mouse_X;
    float mouse_Y;
    public GameObject player;

    void Update()
    {
        
        // マウスをクリックした場合
        //if (Input.GetMouseButton(0))
        //{
        mouse_X = Input.GetAxis("Mouse X") * rotationSpeed;
        mouse_Y = Input.GetAxis("Mouse Y") * rotationSpeed;
        transform.RotateAround(player.transform.position, Vector3.up, mouse_X);
        transform.RotateAround(player.transform.position, transform.right, mouse_Y);
       
        //player.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + 180,0);   // プレイヤーの向きを変更させる処理
        //player.transform.rotation = Quaternion.Euler(-transform.eulerAngles.x,transform.eulerAngles.y + 180, 0);
    }
}