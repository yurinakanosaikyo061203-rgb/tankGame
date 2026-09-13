using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic; // Listを使うための道具箱

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyTankAI : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Transform _transform;
    private NavMeshAgent _agent;
    private Transform _attackTarget;

    [Header("スペック設定")]
    [SerializeField] private float horsepower = 700f;     // パンターIIの実車馬力
    [SerializeField] private float maxSpeed = 15.3f;       // 最高時速 55km/h相当
    [SerializeField] private float maxBackSpeed = 4.2f;    // 後退時速 15km/h相当
    [SerializeField] private float rotateSpeed = 50.0f;    // 旋回速度
    [SerializeField] private float turnForwardPower = 0.5f; // 旋回時の前進力

    [Header("索敵設定")]
    [SerializeField] private float scanRadius = 1000f;      // プレイヤー側を探す範囲（m）
    [SerializeField] private float attackDistance = 100f;   // この距離まで近づいたら足を止めて狙う

    private float _scanTimer = 0f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _transform = transform;
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // ★【53トンに設定】32トンから引き上げ、重戦車級の重さに
        _rigidbody.mass = 53000f;
        _rigidbody.linearDamping = 1.0f;
        _rigidbody.angularDamping = 4.0f;

        // NavMeshAgentの自動ブレーキなどを完全無効化
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.stoppingDistance = 0f;
        _agent.acceleration = 9999f;
        _agent.speed = 99f;
    }

    private void Update()
    {
        // 0.5秒おきに一番近いターゲットをスキャン
        _scanTimer += Time.deltaTime;
        if (_scanTimer >= 0.5f)
        {
            _scanTimer = 0f;
            FindNearestTarget();
        }

        // Agentの論理位置を現在の物理位置に毎フレーム強制同期
        if (_agent.isOnNavMesh)
        {
            _agent.nextPosition = _transform.position;
        }

        // 行き先の決定（ターゲットがいたらそこへNavMeshを伸ばす）
        if (_attackTarget != null)
        {
            _agent.SetDestination(_attackTarget.position);
        }
    }

    private void FixedUpdate()
    {
        if (_attackTarget == null)
        {
            ApplyStopBrake();
            return;
        }

        float distanceToTarget = Vector3.Distance(_transform.position, _attackTarget.position);

        // 設定した攻撃距離（例:30m）より近づいたら足を止めて狙う
        if (distanceToTarget <= attackDistance)
        {
            ApplyStopBrake();
            return;
        }

        // NavMeshAgentが指示する「次の経由地」への方向（無ければ直接ターゲット）
        Vector3 targetDirection = Vector3.zero;
        if (_agent.hasPath && _agent.steeringTarget != Vector3.zero && !_agent.pathPending)
        {
            targetDirection = (_agent.steeringTarget - _transform.position).normalized;
        }
        else
        {
            targetDirection = (_attackTarget.position - _transform.position).normalized;
        }

        // ★【ここを修正】通常モデル（Zプラスが正面）のローカル空間に変換
        Vector3 localDir = _transform.InverseTransformDirection(targetDirection);

        // Zプラス（正面）を基準にした、目標への角度（-180〜180）を計算
        float angleToTarget = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        float horizontalInput = 0f;
        float verticalInput = 0f;

        // 旋回入力の計算（ターゲットが右にあれば右（1）、左にあれば左（-1））
        if (Mathf.Abs(angleToTarget) > 8f)
        {
            horizontalInput = angleToTarget > 0f ? 1f : -1f;
        }

        // 前進入力の計算（目標が正面45度以内なら進む）
        if (Mathf.Abs(angleToTarget) < 45f)
        {
            verticalInput = 1f;
        }

        // 信地旋回用の自動前進
        if (Mathf.Abs(horizontalInput) > 0.05f && Mathf.Abs(verticalInput) <= 0.05f)
        {
            verticalInput = turnForwardPower;
        }

        ExecuteMovement(horizontalInput, verticalInput);
    }

    private void FindNearestTarget()
    {
        List<Transform> potentialTargets = new List<Transform>();

        // Playerタグ（自分）を探す
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) potentialTargets.Add(player.transform);

        // 仲間（CompanionTankAIを持つオブジェクト）を探す
        CompanionTankAI[] companions = FindObjectsByType<CompanionTankAI>(FindObjectsSortMode.None);
        foreach (var c in companions)
        {
            potentialTargets.Add(c.transform);
        }

        float closestDistance = scanRadius;
        Transform closestTarget = null;

        foreach (Transform t in potentialTargets)
        {
            float distance = Vector3.Distance(_transform.position, t.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = t;
            }
        }

        _attackTarget = closestTarget;
    }

    private void ExecuteMovement(float horizontal, float vertical)
    {
        // ★【通常モデル用】前進がZプラス方向なので、符号をそのまま取得
        float currentForwardSpeed = _transform.InverseTransformDirection(_rigidbody.linearVelocity).z;

        // 53トンの巨体を動かすため、加える力の倍率をプレイヤー（1200）より高い【1800】に引き上げ！
        float totalForce = horsepower * 1800f;

        // 旋回
        if (Mathf.Abs(horizontal) > 0.05f && Mathf.Abs(vertical) > 0.05f)
        {
            Quaternion turnRotation = Quaternion.Euler(0f, horizontal * rotateSpeed * Time.fixedDeltaTime, 0f);
            _rigidbody.MoveRotation(_rigidbody.rotation * turnRotation);
        }

        // 前進
        if (vertical > 0.05f)
        {
            if (currentForwardSpeed < maxSpeed)
            {
                // ★【通常モデル用】Vector3.back から Vector3.forward に変更
                _rigidbody.AddRelativeForce(Vector3.forward * (totalForce * vertical), ForceMode.Force);

                if (currentForwardSpeed > maxSpeed * 0.6f)
                {
                    Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
                    localVelocity.z = Mathf.Lerp(localVelocity.z, maxSpeed, Time.fixedDeltaTime * 1.5f);
                    _rigidbody.linearVelocity = _transform.TransformDirection(localVelocity);
                }
            }
        }
        else
        {
            ApplyStopBrake();
        }
    }

    private void ApplyStopBrake()
    {
        float currentForwardSpeed = _transform.InverseTransformDirection(_rigidbody.linearVelocity).z;
        if (Mathf.Abs(currentForwardSpeed) > 0.1f)
        {
            // ★【通常モデル用】速度の反対（後ろ向き）にブレーキをかけるため Vector3.back に変更
            Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
            _rigidbody.AddRelativeForce(Vector3.back * (localVelocity.z * 25000f), ForceMode.Force);
        }
        else
        {
            Vector3 localVel = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
            localVel.z = 0f;
            _rigidbody.linearVelocity = _transform.TransformDirection(localVel);
        }
    }
}
