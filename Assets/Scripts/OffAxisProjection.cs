using System;
using UnityEngine;

[ExecuteInEditMode]
public class OffAxisProjection : MonoBehaviour
{
  public enum CameraMode
  {
    LeftEye,
    RightEye,
    BothEyes,
  }

  public enum EyeFilterMode
  {
    NoFilter,
    Lerp,
    KalmanFilter
  }

  [Header("References")]
  [SerializeField] private Camera m_Camera;
  [SerializeField] private FaceDetection m_FaceDetection;

  [Header("Kalman Filter")]
  [SerializeField] private EyeFilterMode m_FilterMode = EyeFilterMode.KalmanFilter;
  // [Tooltip("Enable detection of when the head/eyes are approximately stationary.")]
  // [SerializeField] private bool m_EnableStationaryDetection = true;
  // [Tooltip("Speed (units/second) below which the eye position is considered stationary.")]
  // [SerializeField] private float m_StationarySpeedThreshold = 0.01f;
  // [Tooltip("Number of consecutive frames below the speed threshold required to enter stationary state.")]
  // [SerializeField] private int m_StationaryFrameCountThreshold = 15;
  [Tooltip("Base process noise used by the Kalman filter (larger = more responsive, less smoothing).")]
  [SerializeField] private float m_BaseProcessNoise = 0.001f;
  [Tooltip("Base measurement noise used by the Kalman filter (larger = more smoothing, less trust in measurement).")]
  [SerializeField] private float m_BaseMeasurementNoise = 0.01f;
  // [Header("Adaptive Noise")]
  // [Tooltip("Enable adaptive process noise based on motion (innovation magnitude).")]
  // [SerializeField] private bool m_EnableAdaptiveNoise = true;
  // [Tooltip("Minimum multiplier applied to the base process noise.")]
  // [SerializeField] private float m_MinProcessNoiseFactor = 0.5f;
  // [Tooltip("Maximum multiplier applied to the base process noise when movement is high.")]
  // [SerializeField] private float m_MaxProcessNoiseFactor = 5.0f;
  // [Tooltip("Innovation magnitude at which the maximum process noise factor is applied.")]
  // [SerializeField] private float m_MaxInnovationForNoise = 0.2f;

  [Header("Settings")]
  [SerializeField] private CameraMode m_CameraMode = CameraMode.BothEyes;
  [SerializeField] private float m_Left = -20.0f;
  [SerializeField] private float m_right = 20.0f;
  [SerializeField] private float m_Top = 20.0f;
  [SerializeField] private float m_Bottom = -20.0f;
  [SerializeField] private Vector3 m_CameraOffset = new(0.0f, 0.0f, 0.0f);
  [SerializeField] private Vector3 m_FallbackPosition = new(0.0f, 0.0f, -40.0f);
  [Range(0.0f, 0.99f)]
  [SerializeField] private float m_FilterStrength = 0.5f;

  [Header("Debug Info")]
  [SerializeField] private Vector3 m_EyePosition = new(0.0f, 0.0f, -40.0f);        // Active position driving the camera
  [SerializeField] private Vector3 m_EyePositionNoFilter = new(0.0f, 0.0f, -40.0f); // Raw (no filter)
  [SerializeField] private Vector3 m_EyePositionLerp = new(0.0f, 0.0f, -40.0f);     // Lerp-filtered
  [SerializeField] private Vector3 m_EyePositionKalman = new(0.0f, 0.0f, -40.0f);   // Kalman-filtered
  [SerializeField] private Vector3 m_LeftEyeCM;
  [SerializeField] private Vector3 m_RightEyeCM;

  public Vector3 EyePosition => m_EyePosition;
  public Vector3 EyePositionNoFilter => m_EyePositionNoFilter;
  public Vector3 EyePositionLerp => m_EyePositionLerp;
  public Vector3 EyePositionKalman => m_EyePositionKalman;

  // Simple 3D Kalman filter for position only.
  // Uses independent 1D filters per axis with optional adaptive process noise and
  // stationary state detection based on estimated velocity.
  [Serializable]
  private class KalmanFilterVector3
  {
    [NonSerialized] public Vector3 State;            // Estimated position
    [NonSerialized] public Vector3 ErrorCovariance;  // P

    [NonSerialized] public Vector3 LastState;

    private float m_BaseProcessNoise;
    private float m_BaseMeasurementNoise;

    public void Initialize(Vector3 initialPosition, float baseProcessNoise, float baseMeasurementNoise)
    {
      State = initialPosition;
      LastState = initialPosition;
      ErrorCovariance = Vector3.one * 1.0f;
      m_BaseProcessNoise = Mathf.Max(1e-6f, baseProcessNoise);
      m_BaseMeasurementNoise = Mathf.Max(1e-6f, baseMeasurementNoise);
    }

    public Vector3 Update(
      Vector3 measurement)
    {
      var Q = Vector3.one * m_BaseProcessNoise;
      var R = Vector3.one * m_BaseMeasurementNoise;

      // Prediction step (identity state transition for position-only model).
      var predictedState = State;
      var predictedErrorCovariance = ErrorCovariance + Q;

      // Correction step (per-axis scalar Kalman).
      Vector3 kalmanGain = new(
        predictedErrorCovariance.x / (predictedErrorCovariance.x + R.x),
        predictedErrorCovariance.y / (predictedErrorCovariance.y + R.y),
        predictedErrorCovariance.z / (predictedErrorCovariance.z + R.z)
      );

      State = predictedState + Vector3.Scale(kalmanGain, measurement - predictedState);
      ErrorCovariance = new Vector3(
        (1.0f - kalmanGain.x) * predictedErrorCovariance.x,
        (1.0f - kalmanGain.y) * predictedErrorCovariance.y,
        (1.0f - kalmanGain.z) * predictedErrorCovariance.z
      );

      LastState = State;
      return State;
    }
  }

  [NonSerialized] private KalmanFilterVector3 m_EyeKalmanFilter = new();

  private void Awake()
  {
    m_FaceDetection.SetEnabled(true);

    // Initialize Kalman filter with fallback position so that the first
    // updates are well-defined even if tracking data is not immediately available.
    m_EyeKalmanFilter.Initialize(m_FallbackPosition, m_BaseProcessNoise, m_BaseMeasurementNoise);
  }

  private void Start()
  {
    m_Camera = GetComponent<Camera>();
  }

  private void LateUpdate()
  {
    // Always start from raw eye center-of-mass values from FaceDetection.
    m_RightEyeCM = m_FaceDetection.RightEyeCMUpdate;
    m_LeftEyeCM = m_FaceDetection.LeftEyeCMUpdate;

    // Base (no filter) eye position: average of both eyes + camera offset.
    var basePosition = m_Camera.transform.position;
    switch (m_CameraMode)
    {
      case CameraMode.LeftEye:
        basePosition.x = m_LeftEyeCM.x;
        basePosition.y = m_LeftEyeCM.y;
        basePosition.z = m_LeftEyeCM.z;
        break;
      case CameraMode.RightEye:
        basePosition.x = m_RightEyeCM.x;
        basePosition.y = m_RightEyeCM.y;
        basePosition.z = m_RightEyeCM.z;
        break;
      case CameraMode.BothEyes:
        basePosition.x = (m_LeftEyeCM.x + m_RightEyeCM.x) * 0.5f;
        basePosition.y = (m_LeftEyeCM.y + m_RightEyeCM.y) * 0.5f;
        basePosition.z = (m_LeftEyeCM.z + m_RightEyeCM.z) * 0.5f;
        break;
    }

    if (basePosition == Vector3.zero)
    {
      basePosition = m_FallbackPosition;
    }
    else
    {
      basePosition += m_CameraOffset;
    }

    // 1) NoFilter: direct measurement.
    m_EyePositionNoFilter = basePosition;

    // 2) Lerp filter: simple exponential smoothing towards the measurement.
    if (m_FilterStrength > 0f)
    {
      m_EyePositionLerp = Vector3.Lerp(m_EyePositionLerp, basePosition, 1.0f - m_FilterStrength);
    }
    else
    {
      m_EyePositionLerp = basePosition;
    }

    // 3) Kalman filter: full adaptive filter on the same measurement.
    m_EyePositionKalman = m_EyeKalmanFilter.Update(basePosition);

    // Choose which filtered value actually drives the camera.
    switch (m_FilterMode)
    {
      case EyeFilterMode.NoFilter:
        m_EyePosition = m_EyePositionNoFilter;
        break;
      case EyeFilterMode.Lerp:
        m_EyePosition = m_EyePositionLerp;
        break;
      case EyeFilterMode.KalmanFilter:
        m_EyePosition = m_EyePositionKalman;
        break;
    }

    m_Camera.transform.position = m_EyePosition;
    m_Camera.projectionMatrix = PerspectiveOffCenter(m_EyePosition, m_Left, m_right, m_Bottom, m_Top, m_Camera.nearClipPlane, m_Camera.farClipPlane);
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
