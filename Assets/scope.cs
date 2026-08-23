using UnityEngine;

public class scope : MonoBehaviour
{
    // ここに完成している「照準器の画像（GameObject）」をインスペクターからドラッグ＆ドロップします
    [SerializeField] private GameObject scopeImage;

    void Start()
    {
        // ゲーム開始時は照準器画像を非表示にしておく
        if (scopeImage != null)
        {
            scopeImage.SetActive(false);
        }
    }

    void Update()
    {
        // Shiftキーを押している間だけ表示する場合
        if (Input.GetKey(KeyCode.Mouse1))
        {
            scopeImage.SetActive(true);
        }
        else
        {
            scopeImage.SetActive(false);
        }

        /* 
        // 【補足】もし「Shiftを1回押したら表示、もう1回押したら非表示（トグル）」にしたい場合
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            scopeImage.SetActive(!scopeImage.activeSelf);
        }
        */
    }
}
