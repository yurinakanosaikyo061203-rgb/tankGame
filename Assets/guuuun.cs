using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class guuuun : MonoBehaviour
{
    

    // privateの状態でもInspector上から設定できるようにするテクニック。
    [SerializeField]
    private GameObject shellPrefab;

    [SerializeField]
    private AudioClip shotSound;
    float shotSpeed = 100.0f;

    void Update()
    {
        // もしもSpaceキーを押したならば（条件）
        // 「Space」の部分を変更することで他のキーにすることができる（ポイント）
        if (Input.GetMouseButtonDown(0))
        {
            // 砲弾のプレハブを実体化（インスタンス化）する。
            GameObject shell = Instantiate(shellPrefab, transform.position, Quaternion.identity);

            // 砲弾に付いているRigidbodyコンポーネントにアクセスする。
            Rigidbody shellRb = shell.GetComponent<Rigidbody>();

            // forward（青軸＝Z軸）の方向に力を加える。
            shellRb.AddForce(transform.forward * shotSpeed);

            // 発射した砲弾を３秒後に破壊する。
            // （重要な考え方）不要になった砲弾はメモリー上から削除すること。
            Destroy(shell, 3.0f);

            // 砲弾の発射音を出す。
            AudioSource.PlayClipAtPoint(shotSound, transform.position);
        }
    }
}