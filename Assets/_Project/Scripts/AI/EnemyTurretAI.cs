using UnityEngine;
using System.Collections;

public class EnemyTurretAI : MonoBehaviour
{
    private EnemyTankAI _aiMovement;

    [Header("砲塔・砲身オブジェクト設定")]
    [SerializeField] private Transform turretObject;
    [SerializeField] private Transform barrelObject;
    [SerializeField] private Transform muzzlePoint; // プレイヤーの"Fire_Point"と同じ

    [Header("射撃・砲塔スペック")]
    [SerializeField] private GameObject shellPrefab; // プレイヤーと同じ弾のプレハブ
    [SerializeField] private GameObject firePrefab;  // プレイヤーと同じ発砲エフェクトのプレハブ
    [SerializeField] private float attackForce = 100.0f;
    [SerializeField] private float bulletVelocity = 250.0f;
    [SerializeField] private float spawnOffset = 1.0f;
    [SerializeField] private float fireInterval = 7.0f;
    [SerializeField] private float turretRotateSpeed = 15f;
    [SerializeField] private float maxElevation = 20f;
    [SerializeField] private float maxDepression = -8f;

    [Header("発砲オーディオ設定")]
    [SerializeField] private AudioSource shotAudioSource; // ★新設：発砲音を鳴らすためのスピーカー
    [SerializeField] private AudioClip shotClip;         // ★新設：主砲の発砲音（SE）

    private float _fireTimer = 0f;

    private void Awake()
    {
        _aiMovement = GetComponent<EnemyTankAI>();
    }

    private void Update()
    {
        if (_aiMovement == null || _aiMovement.AttackTarget == null) return;

        Transform target = _aiMovement.AttackTarget;

        // 1. 砲塔の左右旋回計算
        Vector3 targetPosLocal = transform.InverseTransformPoint(target.position);
        targetPosLocal.y = 0f;
        Quaternion targetTurretRot = Quaternion.LookRotation(targetPosLocal, Vector3.up);
        turretObject.localRotation = Quaternion.RotateTowards(turretObject.localRotation, targetTurretRot, turretRotateSpeed * Time.deltaTime);

        // 2. 砲身の上下俯仰計算
        Vector3 targetPosTurretLocal = turretObject.InverseTransformPoint(target.position);
        float flatDistance = new Vector2(targetPosTurretLocal.x, targetPosTurretLocal.z).magnitude;
        float targetPitchAngle = -Mathf.Atan2(targetPosTurretLocal.y, flatDistance) * Mathf.Rad2Deg;
        targetPitchAngle = Mathf.Clamp(targetPitchAngle, -maxElevation, -maxDepression);
        barrelObject.localRotation = Quaternion.RotateTowards(barrelObject.localRotation, Quaternion.Euler(targetPitchAngle, 0f, 0f), turretRotateSpeed * Time.deltaTime);

        // 3. 自動射撃ロジック
        _fireTimer += Time.deltaTime;

        if (_aiMovement.HasLineOfSight && _fireTimer >= fireInterval)
        {
            Vector3 forwardVector = turretObject.forward;
            Vector3 targetDir = (target.position - turretObject.position).normalized;

            if (Vector3.Angle(forwardVector, targetDir) < 5f)
            {
                StartCoroutine(Generate_Enemy_Bullet());
                _fireTimer = 0f;
            }
        }
    }

    public bool CheckMuzzleLineOfSight()
    {
        if (_aiMovement == null || _aiMovement.AttackTarget == null || muzzlePoint == null)
            return false;

        Transform target = _aiMovement.AttackTarget;
        Vector3 startPos = muzzlePoint.position;
        Vector3 targetPos = target.position + Vector3.up * 1.5f;
        Vector3 dir = (targetPos - startPos).normalized;
        float dist = Vector3.Distance(startPos, targetPos);

        if (Physics.Raycast(startPos, dir, out RaycastHit hit, dist, _aiMovement.ObstacleLayer))
        {
            return false;
        }
        return true;
    }

    private IEnumerator Generate_Enemy_Bullet()
    {
        if (shellPrefab == null || muzzlePoint == null) yield break;

        // ★【修正ポイント1】発砲音を再生（重なっても綺麗に鳴るPlayOneShot）
        if (shotAudioSource != null && shotClip != null)
        {
            shotAudioSource.PlayOneShot(shotClip);
        }

        // ★【修正ポイント2】発砲炎エフェクトをプレイヤーと「全く同じ親子関係」で生成
        if (firePrefab != null)
        {
            // プレイヤーのコード(Instantiate(..., thisTransform))に合わせ、muzzlePointを親にして生成
            Instantiate(firePrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
        }

        // 弾プレハブの生成
        Vector3 spawnPosition = muzzlePoint.position + muzzlePoint.forward * spawnOffset;
        GameObject bulletObject = Instantiate(shellPrefab, spawnPosition, muzzlePoint.rotation);

        var bulletScript = bulletObject.GetComponent<ChobiAssets.KTP.Bullet_Nav_CS>();
        if (bulletScript != null)
        {
            bulletScript.attackForce = attackForce;
        }

        bulletObject.tag = "Finish";
        bulletObject.layer = ChobiAssets.KTP.Layer_Settings_CS.Bullet_Layer;

        yield return new WaitForFixedUpdate();

        Rigidbody shellRb = bulletObject.GetComponent<Rigidbody>();
        if (shellRb != null)
        {
            shellRb.linearVelocity = bulletObject.transform.forward * bulletVelocity;
        }
    }
}
