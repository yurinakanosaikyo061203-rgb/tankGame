using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CompanionTankAI : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Transform _transform;
    private Transform _playerTransform;

    [Header("スペック設定")]
    [SerializeField] private float horsepower = 650f;
    [SerializeField] private float maxSpeed = 16f;
    [SerializeField] private float maxBackSpeed = 3f;
    [SerializeField] private float rotateSpeed = 75.0f;
    [SerializeField] private float turnForwardPower = 1.2f;

    [Header("フォロワー設定")]
    [SerializeField] private float followDistance = 10f;
    [SerializeField] private float scanRadius = 150f;

    [Header("車間距離設定")]
    [SerializeField] private float maxPersonalSpace = 8f;
    [SerializeField] private float minPersonalSpace = 3.5f;
    [SerializeField] private float separationWeight = 0.8f;
    [SerializeField] private LayerMask wallLayer;

    private float _currentPersonalSpace;
    private float _stuckTimer = 0f;
    private float _backTimer = 0f;
    private bool _isBacking = false;
    private Vector3 _lastPosition;
    private float _positionCheckTimer = 0f;
    private Transform _attackTarget;
    private float _scanTimer = 0f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _transform = transform;
    }

    private void Start()
    {
        _rigidbody.mass = 32000f;
        _rigidbody.linearDamping = 1.0f;
        _rigidbody.angularDamping = 4.0f;
        _lastPosition = _transform.position;
        _currentPersonalSpace = maxPersonalSpace;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerTransform = player.transform;
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        _scanTimer += Time.deltaTime;
        if (_scanTimer >= 0.5f)
        {
            _scanTimer = 0f;
            FindNearestEnemy();
        }

        CalculateSpaceByWallDistance();

        float distanceToPlayer = Vector3.Distance(_transform.position, _playerTransform.position);

        if (!_isBacking)
        {
            bool shouldMove = (_attackTarget != null) || (distanceToPlayer > followDistance);
            bool isTooFar = distanceToPlayer > (followDistance + 6f);

            _positionCheckTimer += Time.deltaTime;
            if (_positionCheckTimer >= 0.2f)
            {
                _positionCheckTimer = 0f;
                float checkDist = Vector3.Distance(_transform.position, _lastPosition);

                if (shouldMove && !isTooFar && checkDist < 0.1f)
                {
                    _stuckTimer += 0.2f;
                    if (_stuckTimer > 1.2f)
                    {
                        _isBacking = true;
                        _backTimer = 1.2f;
                        _stuckTimer = 0f;
                    }
                }
                else
                {
                    _stuckTimer = 0f;
                }
                _lastPosition = _transform.position;
            }
        }
        else
        {
            _backTimer -= Time.deltaTime;
            if (_backTimer <= 0f)
            {
                _isBacking = false;
                _stuckTimer = 0f;
                _positionCheckTimer = 0f;
                _lastPosition = _transform.position;
            }
        }
    }

    private void FixedUpdate()
    {
        if (_playerTransform == null) return;

        if (_isBacking)
        {
            ExecuteMovement(0f, -1f);
            return;
        }

        float distanceToPlayer = Vector3.Distance(_transform.position, _playerTransform.position);

        if (_attackTarget == null && distanceToPlayer <= followDistance)
        {
            ApplyStopBrake();
            return;
        }

        Vector3 targetPosition = _attackTarget != null ? _attackTarget.position : _playerTransform.position;
        Vector3 baseDirection = (targetPosition - _transform.position).normalized;
        Vector3 separationDirection = ComputeSeparationForce();
        Vector3 finalDirection = (baseDirection + separationDirection * separationWeight).normalized;

        Vector3 localDir = _transform.InverseTransformDirection(finalDirection);
        float angleToTarget = Mathf.Atan2(localDir.x, -localDir.z) * Mathf.Rad2Deg * -1f;

        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (Mathf.Abs(angleToTarget) > 6f)
        {
            horizontalInput = angleToTarget > 0f ? 1f : -1f;
        }

        if (Mathf.Abs(angleToTarget) < 90f)
        {
            verticalInput = 1f;
        }

        if (Mathf.Abs(horizontalInput) > 0.05f && Mathf.Abs(verticalInput) <= 0.05f)
        {
            verticalInput = turnForwardPower;
        }

        ExecuteMovement(horizontalInput, verticalInput);
    }

    private void CalculateSpaceByWallDistance()
    {
        float minWallDistance = 100f;
        Vector3 leftDir = -_transform.right;
        if (Physics.Raycast(_transform.position, leftDir, out RaycastHit leftHit, 30f, wallLayer))
        {
            minWallDistance = Mathf.Min(minWallDistance, leftHit.distance);
        }

        Vector3 rightDir = _transform.right;
        if (Physics.Raycast(_transform.position, rightDir, out RaycastHit rightHit, 30f, wallLayer))
        {
            minWallDistance = Mathf.Min(minWallDistance, rightHit.distance);
        }

        float calculatedSpace = minWallDistance * 0.25f;
        _currentPersonalSpace = Mathf.Clamp(calculatedSpace, minPersonalSpace, maxPersonalSpace);
    }

    private Vector3 ComputeSeparationForce()
    {
        Vector3 steering = Vector3.zero;
        int neighborCount = 0;

        CompanionTankAI[] allCompanions = FindObjectsByType<CompanionTankAI>(FindObjectsSortMode.None);

        foreach (CompanionTankAI companion in allCompanions)
        {
            if (companion.gameObject == this.gameObject) continue;

            float distance = Vector3.Distance(_transform.position, companion.transform.position);

            if (distance < _currentPersonalSpace)
            {
                if (distance < 0.1f)
                {
                    steering += _transform.right;
                    neighborCount++;
                    continue;
                }

                Vector3 diff = (_transform.position - companion.transform.position).normalized;
                diff /= distance;
                steering += diff;
                neighborCount++;
            }
        }

        if (neighborCount > 0) steering /= neighborCount;
        return steering.normalized;
    }

    private void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float closestDistance = scanRadius;
        Transform closestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(_transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }
        _attackTarget = closestEnemy;
    }

    private void ExecuteMovement(float horizontal, float vertical)
    {
        float currentForwardSpeed = -_transform.InverseTransformDirection(_rigidbody.linearVelocity).z;
        float totalForce = horsepower * 1200f;

        if (Mathf.Abs(horizontal) > 0.05f && Mathf.Abs(vertical) > 0.05f)
        {
            Quaternion turnRotation = Quaternion.Euler(0f, horizontal * rotateSpeed * Time.fixedDeltaTime, 0f);
            _rigidbody.MoveRotation(_rigidbody.rotation * turnRotation);
        }

        if (vertical > 0.05f)
        {
            if (currentForwardSpeed < maxSpeed)
            {
                _rigidbody.AddRelativeForce(Vector3.back * (totalForce * vertical), ForceMode.Force);

                if (currentForwardSpeed > maxSpeed * 0.6f)
                {
                    Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
                    localVelocity.z = Mathf.Lerp(localVelocity.z, -maxSpeed, Time.fixedDeltaTime * 1.5f);
                    _rigidbody.linearVelocity = _transform.TransformDirection(localVelocity);
                }
            }
        }
        else if (vertical < -0.05f)
        {
            if (currentForwardSpeed > -maxBackSpeed)
            {
                _rigidbody.AddRelativeForce(Vector3.forward * (totalForce * Mathf.Abs(vertical) * 0.8f), ForceMode.Force);

                if (currentForwardSpeed < -(maxBackSpeed * 0.5f))
                {
                    Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
                    localVelocity.z = Mathf.Lerp(localVelocity.z, maxBackSpeed, Time.fixedDeltaTime * 2.0f);
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
        float currentForwardSpeed = -_transform.InverseTransformDirection(_rigidbody.linearVelocity).z;
        if (Mathf.Abs(currentForwardSpeed) > 0.1f)
        {
            Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
            _rigidbody.AddRelativeForce(Vector3.forward * (localVelocity.z * 25000f), ForceMode.Force);
        }
        else
        {
            Vector3 localVel = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
            localVel.z = 0f;
            _rigidbody.linearVelocity = _transform.TransformDirection(localVel);
        }
    }
}
