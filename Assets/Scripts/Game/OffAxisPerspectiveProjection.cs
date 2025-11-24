using System;
using UnityEngine;


[ExecuteInEditMode]
public class OffAxisPerspectiveProjection : MonoBehaviour
{
    private Camera _camera;

    public float left = -20.0f;
    public float right = 20.0f;
    public float top = 20.0f;
    public float bottom = -20.0f;
    public Vector3 eyePosition = new (0.0f, 0.0f, -30.0f);

    public bool enableDebugMode = false;
    public Vector3 debugPosition = new(0.0f, -3.0f, -40.0f);

    public bool threaded = false;
    public EyeDetector eyeDetector;
    public EyeDetectorThreaded eyeDetectorThreaded;

    [Range(0.0f, 0.99f)]
    public float filterStrength = 0.5f;
    [Range(1.0f, 3.0f)]
    public float moveFactorX = 1.0f;
    [Range(1.0f, 3.0f)]
    public float moveFactorY = 1.0f;
    [Range(1.0f, 3.0f)]
    public float moveFactorZ = 1.0f;

    public Vector3 leftEyeCM;
    public Vector3 rightEyeCM;

    // public float filterStrengthFactor = 0.95f;
    // public float maxFilter = 0.95f;
    // public float minFilter = 0.6f;
    // public float threshold = 0.8f;
    // public float increaseFactor = 0.01f;
    // public float decreaseFactor = 0.025f;
    //
    // public bool enableStats = false;
    // private MovingAverage rightEyeDiff;
    // public float rightEyeDiffAvg;
    // public float rightEyeDiffMin;
    // public float rightEyeDiffMax;

    // private void Awake()
    // {
    //     rightEyeDiff = new MovingAverage(100);
    //     rightEyeDiffMin = float.MaxValue;
    //     rightEyeDiffMax = float.MinValue;
    // }

    private GameManager _gameManager;

    private void Awake()
    {
        eyeDetector.activate = !threaded;
        eyeDetectorThreaded.activate = threaded;

        _gameManager = FindObjectOfType<GameManager>();
    }

    private void Start()
    {
        _camera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (_gameManager.pauseMenu.Paused())
            return;

        if (enableDebugMode)
        {
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, debugPosition, 0.01f);
        }
        else
        {
            // var newValue = Vector3.Distance(rightEyeCM, eyeDetector.rightEyeCMUpdate);
            // rightEyeDiff.Add(newValue);
            // if (enableStats)
            // {
            //     //Moving, Filter 0.6
            //     // [0.09924269, 1.804512], [0.09924269, 2.461033]
            //     //Still, Filter 0.95
            //     // [0.1828557, 0.5087405]
            //     rightEyeDiffAvg = rightEyeDiff.GetAverage;
            //     if (rightEyeDiffAvg < rightEyeDiffMin)  // 0.09924269
            //         rightEyeDiffMin = rightEyeDiffAvg;
            //     if (rightEyeDiffAvg > rightEyeDiffMax)  // 1.604512; 2.461033
            //         rightEyeDiffMax = rightEyeDiffAvg;
            //
            //     float difference = Mathf.Abs(newValue - rightEyeDiffAvg);
            //     if (difference <= threshold)
            //     {
            //         filterStrengthFactor = ExtensionMethods.Lerp(filterStrengthFactor, maxFilter, increaseFactor);
            //         // filterStrengthFactor = Mathf.Min(maxFilter, filterStrengthFactor + increaseFactor);
            //     }
            //     else if (difference > threshold)
            //     {
            //         filterStrengthFactor = Mathf.Max(minFilter, filterStrengthFactor - decreaseFactor);
            //     }
            //
            //     filterStrength = filterStrengthFactor;
            // }

            if (threaded)
            {
                rightEyeCM = Vector3.Lerp(rightEyeCM, eyeDetectorThreaded.rightEyeCMUpdate, 1.0f - filterStrength);
                leftEyeCM = Vector3.Lerp(leftEyeCM, eyeDetectorThreaded.leftEyeCMUpdate, 1.0f - filterStrength);
            }
            else
            {
                rightEyeCM = Vector3.Lerp(rightEyeCM, eyeDetector.rightEyeCMUpdate, 1.0f - filterStrength);
                leftEyeCM = Vector3.Lerp(leftEyeCM, eyeDetector.leftEyeCMUpdate, 1.0f - filterStrength);    
            }

            var cameraPosition = _camera.transform.position;
            cameraPosition.x = rightEyeCM.x * moveFactorX;
            cameraPosition.y = rightEyeCM.y * moveFactorY;
            cameraPosition.z = rightEyeCM.z * moveFactorZ;
            // cameraPosition.z = -40.0f;
            _camera.transform.position = cameraPosition;
        }

        eyePosition = _camera.transform.position;

        if (eyePosition == Vector3.zero)
        {
            _camera.transform.position = debugPosition;
            eyePosition = debugPosition;
        }

        _camera.projectionMatrix = PerspectiveOffCenter(eyePosition, left, right, bottom, top, _camera.nearClipPlane, _camera.farClipPlane);
    }

    private static Matrix4x4 PerspectiveOffCenter(Vector3 eyePosition, float left, float right, float bottom, float top, float near, float far)
    {
        var screenBL = new Vector3(left, bottom, 0.0f);
        var screenBR = new Vector3(right, bottom, 0.0f);
        var screenTL = new Vector3(left, top, 0.0f);

        var screenRight = (screenBR - screenBL).normalized;
        var screenUp = (screenTL - screenBL).normalized;
        var screenNormal = Vector3.Cross(screenRight, screenUp).normalized;

        var eyeToBL = screenBL - eyePosition;
        var eyeToBR = screenBR - eyePosition;
        var eyeToTL = screenTL - eyePosition;
        var distanceEyeScreen = Vector3.Dot(screenNormal, eyeToBL);

        var nD = near / distanceEyeScreen;
        var l = Vector3.Dot(screenRight, eyeToBL) * nD;
        var r = Vector3.Dot(screenRight, eyeToBR) * nD;
        var b = Vector3.Dot(screenUp, eyeToBL) * nD;
        var t = Vector3.Dot(screenUp, eyeToTL) * nD;

        var m00 = 2.0F * near / (r - l);
        var m11 = 2.0F * near / (t - b);
        var m02 = (r + l) / (r - l);
        var m12 = (t + b) / (t - b);
        var m22 = -(far + near) / (far - near);
        var m23 = -(2.0F * far * near) / (far - near);
        var m = new Matrix4x4
        {
            [0, 0] = m00,
            [0, 1] = 0,
            [0, 2] = m02,
            [0, 3] = 0,
            [1, 0] = 0,
            [1, 1] = m11,
            [1, 2] = m12,
            [1, 3] = 0,
            [2, 0] = 0,
            [2, 1] = 0,
            [2, 2] = m22,
            [2, 3] = m23,
            [3, 0] = 0,
            [3, 1] = 0,
            [3, 2] = -1.0f,
            [3, 3] = 0
        };
        return m;
    }
}
