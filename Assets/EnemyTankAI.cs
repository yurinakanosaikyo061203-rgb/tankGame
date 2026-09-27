using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyTankAI : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Transform _transform;
    private NavMeshAgent _agent;
    private Transform _attackTarget;
    private EnemyTurretAI _turretAI; // ★砲塔スクリプトへの参照を追加

    [Header("スペック設定")]
    [SerializeField] private float horsepower = 700f;
    [SerializeField] private float maxSpeed = 15.3f;
    [SerializeField] private float maxBackSpeed = 4.2f;
    [SerializeField] private float rotateSpeed = 50.0f;
    [SerializeField] private float turnForwardPower = 0.5f;

    [Header("索敵・戦闘設定")]
    [SerializeField] private float scanRadius = 1000f;
    [SerializeField] private float attackDistance = 200f;
    [SerializeField] private LayerMask obstacleLayer;

    private float _stuckTimer = 0f;
    private float _backTimer = 0f;
    private float _escapeTurnTimer = 0f;
    private float _escapeForwardTimer = 0f;
    private bool _isBacking = false;
    private bool _isEscapingTurn = false;
    private bool _isEscapingForward = false;
    private Vector3 _lastPosition;
    private float _posCheckTimer = 0f;
    private float _scanTimer = 0f;
    private bool _hasLineOfSight = false;

    public Transform AttackTarget => _attackTarget;
    public bool HasLineOfSight => _hasLineOfSight;
    public LayerMask ObstacleLayer => obstacleLayer; // ★砲塔側から参照できるように公開

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _transform = transform;
        _agent = GetComponent<NavMeshAgent>();
        _turretAI = GetComponent<EnemyTurretAI>(); // 自動取得
    }

    private void Start()
    {
        _rigidbody.mass = 53000f;
        _rigidbody.linearDamping = 1.0f;
        _rigidbody.angularDamping = 4.0f;

        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.stoppingDistance = 0f;
        _agent.acceleration = 9999f;
        _agent.speed = 99f;

        _lastPosition = _transform.position;
    }

    private void Update()
    {
        _scanTimer += Time.deltaTime;
        if (_scanTimer >= 0.5f)
        {
            _scanTimer = 0f;
            FindNearestTarget();

            // ★【修正】自分（足元）からの視線ではなく、大砲（マズル）から狙えるかをチェックする
            if (_turretAI != null)
            {
                _hasLineOfSight = _turretAI.CheckMuzzleLineOfSight();
            }
            else
            {
                _hasLineOfSight = false;
            }
        }

        if (_attackTarget == null) return;

        if (_agent.isOnNavMesh) _agent.nextPosition = _transform.position;

        float distanceToTarget = Vector3.Distance(_transform.position, _attackTarget.position);

        // ★大砲の弾が通るルートが確保できて、かつ200m以内なら足を止める
        if (_hasLineOfSight && distanceToTarget <= attackDistance)
        {
            _agent.ResetPath();
        }
        else
        {
            // 大砲が壁で遮られている、または遠すぎる場合は、狙える位置まで回り込んで追いかける
            _agent.SetDestination(_attackTarget.position);
        }

        // スタック判定
        if (!_isBacking && !_isEscapingTurn && !_isEscapingForward)
        {
            bool shouldMove = _agent.hasPath && (!_hasLineOfSight || distanceToTarget > attackDistance);
            _posCheckTimer += Time.deltaTime;
            if (_posCheckTimer >= 0.2f)
            {
                _posCheckTimer = 0f;
                float checkDist = Vector3.Distance(_transform.position, _lastPosition);
                if (shouldMove && checkDist < 0.12f)
                {
                    _stuckTimer += 0.2f;
                    if (_stuckTimer > 1.5f) { _isBacking = true; _backTimer = 1.5f; _stuckTimer = 0f; }
                }
                else _stuckTimer = 0f;
                _lastPosition = _transform.position;
            }
        }
        else if (_isBacking)
        {
            _backTimer -= Time.deltaTime;
            if (_backTimer <= 0f) { _isBacking = false; _isEscapingTurn = true; _escapeTurnTimer = 1.0f; }
        }
        else if (_isEscapingTurn)
        {
            _escapeTurnTimer -= Time.deltaTime;
            if (_escapeTurnTimer <= 0f) { _isEscapingTurn = false; _isEscapingForward = true; _escapeForwardTimer = 1.0f; }
        }
        else if (_isEscapingForward)
        {
            _escapeForwardTimer -= Time.deltaTime;
            if (_escapeForwardTimer <= 0f) { _isEscapingForward = false; _stuckTimer = 0f; _posCheckTimer = 0f; _lastPosition = _transform.position; _agent.ResetPath(); }
        }
    }

    private void FixedUpdate()
    {
        if (_attackTarget == null) { ApplyStopBrake(); return; }
        if (_isBacking) { ExecuteMovement(0f, -1f); return; }
        if (_isEscapingTurn) { ExecuteMovement(1f, 0f); return; }
        if (_isEscapingForward) { ExecuteMovement(0f, 1f); return; }

        float distanceToTarget = Vector3.Distance(_transform.position, _attackTarget.position);
        if (_hasLineOfSight && distanceToTarget <= attackDistance) { ApplyStopBrake(); return; }

        Vector3 targetDirection = (_agent.hasPath && _agent.steeringTarget != Vector3.zero && !_agent.pathPending) ?
            (_agent.steeringTarget - _transform.position).normalized : (_attackTarget.position - _transform.position).normalized;

        Vector3 localDir = _transform.InverseTransformDirection(targetDirection);
        float angleToTarget = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        float horizontalInput = Mathf.Abs(angleToTarget) > 8f ? (angleToTarget > 0f ? 1f : -1f) : 0f;
        float verticalInput = Mathf.Abs(angleToTarget) < 45f ? 1f : 0f;

        if (Mathf.Abs(horizontalInput) > 0.05f && Mathf.Abs(verticalInput) <= 0.05f) verticalInput = turnForwardPower;

        ExecuteMovement(horizontalInput, verticalInput);
    }

    private void FindNearestTarget()
    {
        List<Transform> potentialTargets = new List<Transform>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) potentialTargets.Add(player.transform);

        CompanionTankAI[] companions = FindObjectsByType<CompanionTankAI>(FindObjectsSortMode.None);
        foreach (var c in companions) potentialTargets.Add(c.transform);

        float closestDistance = scanRadius;
        Transform closestTarget = null;
        foreach (Transform t in potentialTargets)
        {
            float distance = Vector3.Distance(_transform.position, t.position);
            if (distance < closestDistance) { closestDistance = distance; closestTarget = t; }
        }
        _attackTarget = closestTarget;
    }

    private void ExecuteMovement(float horizontal, float vertical)
    {
        float currentForwardSpeed = _transform.InverseTransformDirection(_rigidbody.linearVelocity).z;
        float totalForce = horsepower * 1800f;

        if (Mathf.Abs(horizontal) > 0.05f)
        {
            Quaternion turnRotation = Quaternion.Euler(0f, horizontal * rotateSpeed * Time.fixedDeltaTime, 0f);
            _rigidbody.MoveRotation(_rigidbody.rotation * turnRotation);
        }

        if (vertical > 0.05f && currentForwardSpeed < maxSpeed)
        {
            _rigidbody.AddRelativeForce(Vector3.forward * (totalForce * vertical), ForceMode.Force);
            if (currentForwardSpeed > maxSpeed * 0.6f)
            {
                Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
                localVelocity.z = Mathf.Lerp(localVelocity.z, maxSpeed, Time.fixedDeltaTime * 1.5f);
                _rigidbody.linearVelocity = _transform.TransformDirection(localVelocity);
            }
        }
        else if (vertical < -0.05f && currentForwardSpeed > -maxBackSpeed)
        {
            _rigidbody.AddRelativeForce(Vector3.back * (totalForce * Mathf.Abs(vertical) * 0.8f), ForceMode.Force);
            if (currentForwardSpeed < -(maxBackSpeed * 0.5f))
            {
                Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
                localVelocity.z = Mathf.Lerp(localVelocity.z, -maxBackSpeed, Time.fixedDeltaTime * 2.0f);
                _rigidbody.linearVelocity = _transform.TransformDirection(localVelocity);
            }
        }
        else if (Mathf.Abs(horizontal) <= 0.05f) ApplyStopBrake();
    }

    private void ApplyStopBrake()
    {
        float currentForwardSpeed = _transform.InverseTransformDirection(_rigidbody.linearVelocity).z;
        if (Mathf.Abs(currentForwardSpeed) > 0.1f)
        {
            Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
            _rigidbody.AddRelativeForce(Vector3.back * (localVelocity.z * 25000f), ForceMode.Force);
        }
        else
        {
            Vector3 localVel = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
            localVel.z = 0f; _rigidbody.linearVelocity = _transform.TransformDirection(localVel);
        }
    }
}
