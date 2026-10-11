using UnityEngine;

// プレイヤー・敵・味方の戦車で共通の走行処理
// モデルが -z 向きの戦車は facing に -1 を渡す
public class TankDrive
{
    private readonly Rigidbody _rigidbody;
    private readonly Transform _transform;
    private readonly float _facing;

    public TankDrive(Rigidbody rigidbody, float facing)
    {
        _rigidbody = rigidbody;
        _transform = rigidbody.transform;
        _facing = facing;
    }

    public float ForwardSpeed => _facing * _transform.InverseTransformDirection(_rigidbody.linearVelocity).z;

    public void Turn(float horizontal, float rotateSpeed)
    {
        Quaternion turnRotation = Quaternion.Euler(0f, horizontal * rotateSpeed * Time.fixedDeltaTime, 0f);
        _rigidbody.MoveRotation(_rigidbody.rotation * turnRotation);
    }

    // 正の値で前進方向、負の値で後退方向に力をかける
    public void Push(float force)
    {
        _rigidbody.AddRelativeForce(Vector3.forward * (_facing * force), ForceMode.Force);
    }

    public void ApproachSpeed(float targetSpeed, float t)
    {
        Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
        localVelocity.z = Mathf.Lerp(localVelocity.z, _facing * targetSpeed, t);
        _rigidbody.linearVelocity = _transform.TransformDirection(localVelocity);
    }

    public void Brake()
    {
        Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.linearVelocity);
        if (Mathf.Abs(_facing * localVelocity.z) > 0.1f)
        {
            _rigidbody.AddRelativeForce(Vector3.back * (_facing * localVelocity.z * 25000f), ForceMode.Force);
        }
        else
        {
            localVelocity.z = 0f;
            _rigidbody.linearVelocity = _transform.TransformDirection(localVelocity);
        }
    }
}
