using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class move2 : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Transform _transform;

    [Header("戦車のスペック設定")]
    [SerializeField] private float horsepower = 500f;     // 500馬力固定
    [SerializeField] private float maxSpeed = 14f;         // 最高時速 約50km/h固定
    [SerializeField] private float maxBackSpeed = 3f;      // 後退時の最高速度
    [SerializeField] private float rotateSpeed = 65.0f;    // 旋回速度

    [Tooltip("旋回時に自動で加える前進の強さ（信地旋回のふくらみ具合）")]
    [SerializeField] private float turnForwardPower = 1.0f;

    [Header("オーディオ設定")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource shiftSoundSource;
    [SerializeField] private AudioClip gearShiftClip;
    private bool isAccelerating = false;

    // 内部管理用
    private string previousGear = "1速";
    private bool isFrozen = false; // UIを開いている間などに移動を止めるフラグ

    // 他のスクリプトから車速やギアを取得するための公開プロパティ（UI用）
    public float CurrentForwardSpeed { get; private set; }
    public string CurrentGear { get; private set; } = "1速";

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _transform = transform;
    }

    private void Start()
    {
        _rigidbody.mass = 32000f; // 32トン固定
        _rigidbody.linearDamping = 1.0f;
        _rigidbody.angularDamping = 4.0f;
    }

    private void Update()
    {
        if (isFrozen)
        {
            isAccelerating = false;
            if (audioSource != null) audioSource.pitch = Mathf.Lerp(audioSource.pitch, 1.0f, Time.deltaTime * 2.0f);
            return;
        }

        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");

        isAccelerating = Mathf.Abs(vertical) > 0.1f || Mathf.Abs(horizontal) > 0.1f;

        if (audioSource != null)
        {
            float targetPitch = isAccelerating ? 3.0f : 1.0f;
            audioSource.pitch = Mathf.Lerp(audioSource.pitch, targetPitch, Time.deltaTime * 2.0f);
        }
    }

    private void FixedUpdate()
    {
        CurrentForwardSpeed = _transform.InverseTransformDirection(_rigidbody.linearVelocity).z;

        // ギアの判定処理
        UpdateGearLogic();

        if (isFrozen)
        {
            ApplyStopBrake();
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 旋回時（左右入力あり）かつ前後入力がない時、自動で前進パワーを代入
        if (Mathf.Abs(horizontal) > 0.05f && Mathf.Abs(vertical) <= 0.05f)
        {
            vertical = turnForwardPower;
        }

        float pitchAngle = _transform.localEulerAngles.x;
        if (pitchAngle > 180f) pitchAngle -= 360f;

        // 1. 旋回（回転）処理
        if (Mathf.Abs(horizontal) > 0.05f && Mathf.Abs(vertical) > 0.05f)
        {
            // ★【ここを修正】前回入れた「-1f」を削除し、通常の入力方向に回転させる（これで左右が直ります）
            Quaternion turnRotation = Quaternion.Euler(0f, horizontal * rotateSpeed * Time.fixedDeltaTime, 0f);
            _rigidbody.MoveRotation(_rigidbody.rotation * turnRotation);
        }

        // 2. 移動処理
        float totalForce = horsepower * 1200f;

        if (vertical > 0.05f)
        {
            if (CurrentForwardSpeed < maxSpeed)
            {
                _rigidbody.AddRelativeForce(Vector3.forward * (totalForce * vertical), ForceMode.Force);

                float startBrakeSpeed = 6.0f - (pitchAngle * 0.2f);
                startBrakeSpeed = Mathf.Clamp(startBrakeSpeed, 2.0f, maxSpeed - 1f);

                if (CurrentForwardSpeed > startBrakeSpeed)
                {
                    Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
                    float slopeWeight = 1.5f + (pitchAngle * 0.1f);
                    slopeWeight = Mathf.Clamp(slopeWeight, 0.2f, 5.0f);

                    localVelocity.z = Mathf.Lerp(localVelocity.z, maxSpeed, Time.fixedDeltaTime * slopeWeight);
                    _rigidbody.linearVelocity = _transform.TransformDirection(localVelocity);
                }
            }
        }
        else if (vertical < -0.05f)
        {
            if (CurrentForwardSpeed > -maxBackSpeed)
            {
                _rigidbody.AddRelativeForce(Vector3.back * (totalForce * Mathf.Abs(vertical) * 0.8f), ForceMode.Force);

                if (CurrentForwardSpeed < -(maxBackSpeed * 0.5f))
                {
                    Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
                    localVelocity.z = Mathf.Lerp(localVelocity.z, -maxBackSpeed, Time.fixedDeltaTime * 2.0f);
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
        if (Mathf.Abs(CurrentForwardSpeed) > 0.1f)
        {
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

    private void UpdateGearLogic()
    {
        float kmh = Mathf.Abs(CurrentForwardSpeed) * 3.6f;
        int displaySpeed = Mathf.RoundToInt(kmh);

        if (CurrentForwardSpeed < -0.1f)
        {
            CurrentGear = "後進一速";
        }
        else
        {
            if (displaySpeed <= 6) CurrentGear = "1速";
            else if (displaySpeed <= 15) CurrentGear = "2速";
            else if (displaySpeed <= 25) CurrentGear = "3速";
            else if (displaySpeed <= 35) CurrentGear = "4速";
            else CurrentGear = "5速";
        }

        if (CurrentGear != previousGear)
        {
            if (shiftSoundSource != null && gearShiftClip != null)
            {
                shiftSoundSource.PlayOneShot(gearShiftClip);
            }
            previousGear = CurrentGear;
        }
    }

    public void SetFreeze(bool freeze)
    {
        isFrozen = freeze;
    }
}
