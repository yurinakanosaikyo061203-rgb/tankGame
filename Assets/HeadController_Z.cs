using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadController_Z : MonoBehaviour
{
    public AimController_Z acz;
    private Vector3 targetPosition;

    public float speed = 5f;

    private Vector3 currentDir;

    void Start()
    {
        currentDir = -transform.forward;
    }
    // （テクニック）「ターゲットの高さ」と「戦車ヘッドの高さ」を揃える。
    // これでヘッド部分が「真横に旋回」するようになる。
    void Update()
    {
        Vector3 target = acz.targetPos;
        target.y = transform.position.y;

        Vector3 targetDir = target - transform.position;

        if (targetDir.sqrMagnitude < 0.0001f) return;

        targetDir.Normalize();

        float step = speed * Time.deltaTime;
        currentDir = Vector3.RotateTowards(currentDir, targetDir, step, 0f);
        transform.rotation = Quaternion.LookRotation(-currentDir);
    }

}