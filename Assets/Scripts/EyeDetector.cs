using System.Collections;
using System.Collections.Generic;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityUtils;
using UnityEngine;

using PredictorNamePreset = DlibFaceLandmarkDetectorExample.DlibFaceLandmarkDetectorExample.DlibShapePredictorNamePreset;

public class EyeDetector : MonoBehaviour
{
    public PredictorNamePreset dlibShapePredictorName = PredictorNamePreset.sp_human_face_68;

    // Only allowed formats: 1920,1080:1280,720:640,480
    [SerializeField, TooltipAttribute("Set the width of WebCamTexture.")]
    public int requestedWidth = 320;
    [SerializeField, TooltipAttribute("Set the height of WebCamTexture.")]
    public int requestedHeight = 240;
    [SerializeField, TooltipAttribute("Set FPS of WebCamTexture.")]
    public int requestedFPS = 30;

    [Range(0, 63)]
    public int index = 0;

    public float ipdCM = 6.4f;
    public int focalDistancePx = 711;

    private FaceLandmarkDetector _faceLandmarkDetector;
    private WebCamTexture _webCamTexture;
    private WebCamDevice _webCamDevice;

    private Color32[] _colors;
    private Texture2D _texture;

    private bool _initDone = false;

    private Vector2 _leftEyePx;
    private Vector2 _rightEyePx;

    public Vector3 leftEyeCM;
    public Vector3 rightEyeCM;
    private Vector3 _leftEyeCMUpdate;
    private Vector3 _rightEyeCMUpdate;

    [Range(0.01f, 1.0f)]
    public float filterStrength = 0.5f;

    [Range(0, 255)]
    public int opacity = 255;

    public bool drawRect = true;

    private void Start()
    {
        var path = Utils.getFilePath("DlibFaceLandmarkDetector/" + dlibShapePredictorName + ".dat");
        _faceLandmarkDetector = new FaceLandmarkDetector(path);

        StartCoroutine(_Initialize());
    }

    private IEnumerator _Initialize()
    {
        var devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            _webCamDevice = devices[0];
            _webCamTexture = new WebCamTexture(_webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
        }
        else
        {
            Debug.LogError("No camera device exists!");
            yield break;
        }

        _webCamTexture.Play();

        while (true)
        {
            if (_webCamTexture.didUpdateThisFrame)
            {
                if (_colors == null || _colors.Length != _webCamTexture.width * _webCamTexture.height)
                {
                    _colors = new Color32[_webCamTexture.width * _webCamTexture.height];
                }
        
                _texture = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32, false);
        
                gameObject.GetComponent<Renderer>().material.mainTexture = _texture;

                _initDone = true;

                break;
            }
            else
            {
                yield return 0;
            }
        }
    }

    private void Update()
    {
        if (!_initDone || !_webCamTexture.isPlaying || !_webCamTexture.didUpdateThisFrame)
            return;

        var colors = GetColors();
        if (colors == null) return;

        _faceLandmarkDetector.SetImage(colors, _texture.width, _texture.height, 4, true);
        var detectRectResult = _faceLandmarkDetector.DetectRectDetection();

        if (detectRectResult.Count == 0)
            return;

        // Filter for most confident result
        var detectRect = detectRectResult[0];
        if (detectRectResult.Count > 1)
        {
            foreach (var rect in detectRectResult)
            {
                if (rect.detection_confidence > detectRect.detection_confidence)
                    detectRect = rect;
            }
        }

        //detect landmark points
        var detectLandmarkResult = _faceLandmarkDetector.DetectLandmark(detectRect.rect);

        UpdateEyePositions(detectLandmarkResult);
        DrawEyePositions(colors, new Color32(0, 255, 0, 255), 3);

        //draw landmark points
        DrawLandmarkDetectionResult(detectLandmarkResult, colors, new Color32(255, 0, 0, 255), 3);

        var ipdPx = (int)(_leftEyePx - _rightEyePx).magnitude;
        var eyeDistanceCM = (focalDistancePx * ipdCM) / ipdPx;

        var centerPx = new Vector2Int(_texture.width / 2, _texture.height / 2);
        var leftToCenter = _leftEyePx - centerPx;
        var rightToCenter = _rightEyePx - centerPx;

        _rightEyeCMUpdate.x = -_pixelToMeter(eyeDistanceCM, (int)rightToCenter.x);
        _rightEyeCMUpdate.y = -_pixelToMeter(eyeDistanceCM, (int)rightToCenter.y);
        _rightEyeCMUpdate.z = -eyeDistanceCM;

        _leftEyeCMUpdate.x = -_pixelToMeter(eyeDistanceCM, (int)leftToCenter.x);
        _leftEyeCMUpdate.y = -_pixelToMeter(eyeDistanceCM, (int)leftToCenter.y);
        _leftEyeCMUpdate.z = -eyeDistanceCM;

        rightEyeCM = Vector3.Lerp(rightEyeCM, _rightEyeCMUpdate, filterStrength);
        leftEyeCM = Vector3.Lerp(leftEyeCM, _leftEyeCMUpdate, filterStrength);

        //draw face rect
        if (drawRect)
            _faceLandmarkDetector.DrawDetectResult(colors, _texture.width, _texture.height, 4, true, 255, 0, 0, 255, 2);

        if (opacity == 0)
            return;

        for (var index = 0; index < colors.Length; index++)
        {
            colors[index].a = (byte)opacity;
        }
        _texture.SetPixels32(colors);
        _texture.Apply(false);
    }

    private float _pixelToMeter(float distanceM, int widthPx)
    {
        return (distanceM * widthPx) / focalDistancePx;
    }
    
    // This function returns a flattened 2D array where the data appears row by row.
    // Unity lays out the array's pixels left to right, bottom to top.
    private Color32[] GetColors()
    {
        _webCamTexture.GetPixels32(_colors);
        return _colors;
    }

    private void UpdateEyePositions(List<Vector2> result)
    {
        if (dlibShapePredictorName is PredictorNamePreset.sp_human_face_68 or PredictorNamePreset.sp_human_face_68_for_mobile)
        {
            var rightEye0 = result[36];
            var rightEye1 = result[39];
            var leftEye0 = result[42];
            var leftEye1 = result[45];

            _rightEyePx = (rightEye0 + rightEye1) * 0.5f;
            _leftEyePx = (leftEye0 + leftEye1) * 0.5f;
        }

        else
        {
            var rightEye0 = result[2];
            var rightEye1 = result[3];
            var leftEye0 = result[4];
            var leftEye1 = result[5];

            _rightEyePx = (rightEye0 + rightEye1) * 0.5f;
            _leftEyePx = (leftEye0 + leftEye1) * 0.5f;
        }
    }

    private void DrawLandmarkDetectionResult(List<Vector2> result, Color32[] colors, Color32 color, int radius)
    {
        for (var index = 0; index < result.Count; index++)
        {
            var r = result[index];
            var x = (int)r.x;
            var y = (int)(_texture.height - r.y);

            color.b = (byte)(0 + index * 50);
            DrawPosition(new Vector2(x, y), colors, color, radius);
        }
    }

    private void DrawEyePositions(Color32[] colors, Color32 color, int radius)
    {
        var rightEyePosition = new Vector2(_rightEyePx.x, _texture.height - _rightEyePx.y);
        var leftEyePosition = new Vector2(_leftEyePx.x, _texture.height - _leftEyePx.y);

        DrawPosition(rightEyePosition, colors, color, radius);
        DrawPosition(leftEyePosition, colors, color, radius);
    }
    
    private void DrawPosition(Vector2 position, Color32[] colors, Color32 color, int radius)
    {
        var from = new Vector2Int((int)position.x - radius, (int)position.y - radius);
        var to = new Vector2Int((int)position.x + radius, (int)position.y + radius);

        for (var i = from.x; i < to.x; ++i)
        {
            for (var j = from.y; j < to.y; ++j)
            {
                colors[i + _texture.width * j].r = color.r;
                colors[i + _texture.width * j].g = color.g;
                colors[i + _texture.width * j].b = color.b;
            }
        }
    }
}
