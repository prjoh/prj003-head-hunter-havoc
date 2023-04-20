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

    public EyeDetector eyeDetector;
    [Range(1.0f, 3.0f)]
    public float moveFactorX = 1.0f;
    [Range(1.0f, 3.0f)]
    public float moveFactorY = 1.0f;
    [Range(1.0f, 3.0f)]
    public float moveFactorZ = 1.0f;

    private void Start()
    {
        _camera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (enableDebugMode)
        {
            _camera.transform.position = debugPosition;
        }
        else
        {
            var cameraPosition = _camera.transform.position;
            cameraPosition.x = eyeDetector.rightEyeCM.x * moveFactorX;
            cameraPosition.y = eyeDetector.rightEyeCM.y * moveFactorY;
            cameraPosition.z = eyeDetector.rightEyeCM.z * moveFactorZ;
            _camera.transform.position = cameraPosition;
        }

        eyePosition = _camera.transform.position;
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
