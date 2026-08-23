using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class move : MonoBehaviour
{

    private Rigidbody _rigidbody;
    private Transform _transform;

    private float _forwardSpeed = -10.0f;
    private float _backSpeed = -3.0f;
    private float _rotateSpeed = 1.5f; // 追記する
    private Vector3 _velocity;
    public AudioSource audioSource;
    private bool isAccelerating = false;


    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _transform = GetComponent<Transform>();
        audioSource = gameObject.GetComponent<AudioSource>();

    }
    void Update()
    {
        Vector3 front = transform.forward;
        Vector3 right = transform.right;
        if (isAccelerating == true)
        {
            audioSource.pitch = Mathf.Lerp(audioSource.pitch, 3.0f, Time.deltaTime * 1);
        }
        else
        {
            // ピッチを下げてエンジン音をアイドリングに戻す
            audioSource.pitch = Mathf.Lerp(audioSource.pitch, 1.0f, Time.deltaTime * 1);
        }
        if (Input.GetKey(KeyCode.W) | Input.GetKey(KeyCode.S) | Input.GetKey(KeyCode.A) | Input.GetKey(KeyCode.D))
        {
        isAccelerating = true;
        }
        else
        {
            isAccelerating = false;
        }

    }


    private void FixedUpdate()
    {

        float _horizontal = Input.GetAxis("Horizontal");
        float _vertical = Input.GetAxis("Vertical");
        _rigidbody.useGravity = true;

        _velocity = new Vector3(0, 0, -_vertical);
        _velocity = _transform.TransformDirection(_velocity);

        if (_vertical > 0.1)
        {
            _velocity *= _forwardSpeed;
        }
        else if (_vertical < -0.1)
        {
            _velocity *= _backSpeed;
        }

        _transform.Rotate(0, _horizontal * _rotateSpeed, 0); // 追記する

        _transform.localPosition += _velocity * Time.deltaTime;

    }


}
