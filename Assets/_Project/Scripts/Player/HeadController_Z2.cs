using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadController_Z2 : MonoBehaviour
{
    public AimController_Z acz;

    [Header("上下回転の速度(度/秒)")]
    public float speed = 90f;
    [Header("最小/最大ピッチ角(度)")]
    public float minPitch = -15f;
    public float maxPitch = 30f;

    void Update()
    {
        if (acz == null) return;
        if (transform.parent == null) return;

        Vector3 dirWorld = acz.targetPos - transform.position;
        if (dirWorld.sqrMagnitude < 0.0001f) return;

        dirWorld.Normalize();
        Vector3 dirLocal = transform.parent.InverseTransformDirection(dirWorld);

        float desiredPitch = Mathf.Atan2(dirLocal.y, dirLocal.z) * Mathf.Rad2Deg;

        desiredPitch = Mathf.Clamp(desiredPitch, minPitch, maxPitch);

        float currentPitch = transform.localEulerAngles.x;

        float newPitch = Mathf.MoveTowardsAngle(
            currentPitch,
            desiredPitch,
            speed * Time.deltaTime
        );

        transform.localRotation = Quaternion.Euler(newPitch, 0f, 0f);
    }   
}