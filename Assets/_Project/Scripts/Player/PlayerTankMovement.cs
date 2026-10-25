using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerTankMovement : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Transform _transform;
    private TankDrive _drive;

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
        _drive = new TankDrive(_rigidbody, 1f);
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
            if (audioSource != null)
            {
                audioSource.pitch = Mathf.Lerp(audioSource.pitch, 1.0f, Time.deltaTime * 2.0f);
            }
            return;
        }

        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");

        isAccelerating = Mathf.Abs(vertical) > 0.1f || Mathf.Abs(horizontal) > 0.1f;

        if (audioSource != null)
        {
            float targetPitch = 1.0f;
            if (isAccelerating)
            {
                targetPitch = 3.0f;
            }
            audioSource.pitch = Mathf.Lerp(audioSource.pitch, targetPitch, Time.deltaTime * 2.0f);
        }
    }

    private void FixedUpdate()
    {
        CurrentForwardSpeed = _drive.ForwardSpeed;

        // ギアの判定処理
        UpdateGearLogic();

        if (isFrozen)
        {
            _drive.Brake();
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
        if (pitchAngle > 180f)
        {
            pitchAngle -= 360f;
        }

        // 1. 旋回（回転）処理
        if (Mathf.Abs(horizontal) > 0.05f && Mathf.Abs(vertical) > 0.05f)
        {
            _drive.Turn(horizontal, rotateSpeed);
        }

        // 2. 移動処理
        float totalForce = horsepower * 1200f;

        if (vertical > 0.05f)
        {
            if (CurrentForwardSpeed < maxSpeed)
            {
                _drive.Push(totalForce * vertical);

                float startBrakeSpeed = 6.0f - (pitchAngle * 0.2f);
                startBrakeSpeed = Mathf.Clamp(startBrakeSpeed, 2.0f, maxSpeed - 1f);

                if (CurrentForwardSpeed > startBrakeSpeed)
                {
                    float slopeWeight = 1.5f + (pitchAngle * 0.1f);
                    slopeWeight = Mathf.Clamp(slopeWeight, 0.2f, 5.0f);

                    _drive.ApproachSpeed(maxSpeed, Time.fixedDeltaTime * slopeWeight);
                }
            }
        }
        else if (vertical < -0.05f)
        {
            if (CurrentForwardSpeed > -maxBackSpeed)
            {
                _drive.Push(-(totalForce * Mathf.Abs(vertical) * 0.8f));

                if (CurrentForwardSpeed < -(maxBackSpeed * 0.5f))
                {
                    _drive.ApproachSpeed(-maxBackSpeed, Time.fixedDeltaTime * 2.0f);
                }
            }
        }
        else
        {
            _drive.Brake();
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
            if (displaySpeed <= 6)
            {
                CurrentGear = "1速";
            }
            else if (displaySpeed <= 15)
            {
                CurrentGear = "2速";
            }
            else if (displaySpeed <= 25)
            {
                CurrentGear = "3速";
            }
            else if (displaySpeed <= 35)
            {
                CurrentGear = "4速";
            }
            else
            {
                CurrentGear = "5速";
            }
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
