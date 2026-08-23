using UnityEngine;


namespace ChobiAssets.KTP
{

    public class Camera_Zoom_CS : MonoBehaviour
    {
        /*
		 * This script is attached to the main camera in "Camera_Manager" object in the scene..
		 * This script controls the FOV (Field Of View) of the main camera.
		*/

        [Header("Main Camera settings")]
        [Tooltip("Set the main camera.")] public Camera mainCamera;
        [Tooltip("Set the minimum Field Of View.")] public float Min_FOV = 30.0f;
        [Tooltip("Set the maximum Field Of View.")] public float Max_FOV = 80.0f;


        // Set by "Camera_Zoom_Input_##_###_CS".
        [HideInInspector] public float zoomInput;

        float targetFOV;
        float currentFOV;
        float currentnowFOV;

        Camera_Zoom_Input_00_Base_CS inputScript;


        void Start()
        {
            Initialize();
        }


        void Initialize()
        {
            // Get the camera.
            if (mainCamera == null)
            {
                mainCamera = GetComponent<Camera>();
            }
            currentFOV = mainCamera.fieldOfView;
            targetFOV = currentFOV;

            // Set the "inputScript".
            Set_Input_Script();

            // Prepare the input script.
            inputScript.Prepare(this);
        }


        protected virtual void Set_Input_Script()
        {
#if !UNITY_ANDROID && !UNITY_IPHONE
            inputScript = gameObject.AddComponent<Camera_Zoom_Input_01_Desktop_CS>();
#else
            inputScript = gameObject.AddComponent<Camera_Zoom_Input_02_Mobile_CS>();
#endif
        }


        void Update()
        {
            // Check the camera.
            //if (mainCamera.enabled == false)
            //{ // It should be aiming now.
            //    return;
            //}

            // Get the input.
            inputScript.Get_Input();
            // ★ここから追加★：右クリックが押されていたら、自動で限界までズームする
            // Input.GetMouseButton(1) はマウスの右クリックのことです
            if (Input.GetMouseButton(1))
            {
                // 強制的に一番ズームした状態（Min_FOV = 30）を目標にする
                targetFOV = Min_FOV+1;
            }
            // ★ここまで追加★

            // Zoom the main camera.
            Zoom();
        }


        float currentZoomVelocity;
        void Zoom()
        {   
            if (Input.GetMouseButtonDown(1))
            {
                currentnowFOV = currentFOV;
            }
            
            float currentMinFOV = 15.0f;//koko
            //koko
            if (Input.GetMouseButton(1) == false)
            {   
                
                targetFOV *= 1.0f + zoomInput;
                // ★普段は「50 〜 Max_FOV(80)」の間だけでしか動かせないように制限する！
                targetFOV = Mathf.Clamp(targetFOV, currentMinFOV, Max_FOV);
                
            }
            else
            {
                // ★右クリックしている時は、アセット本来の限界「Min_FOV(30)」まで一気に突っ走る！
                targetFOV = Mathf.Clamp(targetFOV, Min_FOV, Max_FOV);
            }
            //koko
            //targetFOV *= 1.0f + zoomInput;//moto
            targetFOV = Mathf.Clamp(targetFOV, Min_FOV, Max_FOV);
            if (Input.GetMouseButtonUp(1))
            {
                Debug.Log("右クリックを離しました");
                targetFOV = currentnowFOV;
                //mainCamera.fieldOfView = currentFOV;
            }
            if (currentFOV != targetFOV)
            {
                currentFOV = Mathf.SmoothDamp(currentFOV, targetFOV, ref currentZoomVelocity, 2.0f * Time.deltaTime);
                mainCamera.fieldOfView = currentFOV;
            }
        }


        void Pause(bool isPaused)
        { // Called from "Game_Controller_CS".
            this.enabled = !isPaused;
        }

    }

}
