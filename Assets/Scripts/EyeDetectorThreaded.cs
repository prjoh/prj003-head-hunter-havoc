using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityUtils;
using UnityEngine;

using PredictorNamePreset = DlibFaceLandmarkDetectorExample.DlibFaceLandmarkDetectorExample.DlibShapePredictorNamePreset;

public class EyeDetectorThreaded : MonoBehaviour
{
    public PredictorNamePreset dlibShapePredictorName = PredictorNamePreset.sp_human_face_68;

    // Only allowed formats: 1920,1080:1280,720:640,480
    [SerializeField, TooltipAttribute("Set the width of WebCamTexture.")]
    public int requestedWidth = 320;
    [SerializeField, TooltipAttribute("Set the height of WebCamTexture.")]
    public int requestedHeight = 240;
    [SerializeField, TooltipAttribute("Set FPS of WebCamTexture.")]
    public int requestedFPS = 30;

    // [Range(0, 63)]
    // public int index = 0;

    public float ipdCM = 6.4f;
    public int focalDistancePx = 711;

    private FaceLandmarkDetector _faceLandmarkDetector;
    private WebCamTexture _webCamTexture;
    private WebCamDevice _webCamDevice;

    private Color32[] _colors;
    // private Texture2D _texture;
    private int width;
    private int height;

    private bool _initDone = false;

    private Vector2 _leftEyePx;
    private Vector2 _rightEyePx;

    [HideInInspector] public Vector3 leftEyeCMUpdate;
    [HideInInspector] public Vector3 rightEyeCMUpdate;

    // [Range(0, 255)]
    // public int opacity = 255;
    //
    // public bool drawRect = true;

    private List<Vector2> _detectLandmarkResult;

    private readonly object _colorsLock = new object(); // lock object for _colors array
    private readonly object _detectLandmarkResultLock = new object(); // lock object for _detectLandmarkResult list
    private readonly object _detectLock = new object(); // lock object for _detectLandmarkResult list

    private void Awake()
    {
        _detectLandmarkResult = new List<Vector2>();
    }

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
        
                var texture = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32, false);
                width = texture.width;
                height = texture.height;

                // gameObject.GetComponent<Renderer>().material.mainTexture = texture;

                _initDone = true;

                break;
            }

            yield return 0;
        }
        
        // Start the thread that reads from _colors and writes to _detectLandmarkResult
        var thread = new Thread(EyeDetectWorker);
        thread.Start();
    }

    private void Update()
    {
        if (!_initDone || !_webCamTexture.isPlaying || !_webCamTexture.didUpdateThisFrame)
            return;

        // var colors = GetColors();
        lock (_colorsLock)
        {
            _webCamTexture.GetPixels32(_colors); // Writes to SHARED _colors array
            if (_colors == null)
                return;
        }

        lock(_detectLandmarkResultLock) {
            if (_detectLandmarkResult.Count <= 0)
                return;

            // Vector2 landmarkResult = _detectLandmarkResult[0];
            // _detectLandmarkResult.RemoveAt(0);

            // Do something with landmarkResult...
            UpdateEyePositions(_detectLandmarkResult);  // Reads from SHARED _detectLandmarkResult List
            _detectLandmarkResult.Clear();

            var ipdPx = (int)(_leftEyePx - _rightEyePx).magnitude;
            var eyeDistanceCM = (focalDistancePx * ipdCM) / ipdPx;

            var centerPx = new Vector2Int(width / 2, height / 2);
            var leftToCenter = _leftEyePx - centerPx;
            var rightToCenter = _rightEyePx - centerPx;

            rightEyeCMUpdate.x = -_pixelToMeter(eyeDistanceCM, (int)rightToCenter.x);
            rightEyeCMUpdate.y = -_pixelToMeter(eyeDistanceCM, (int)rightToCenter.y);
            rightEyeCMUpdate.z = -eyeDistanceCM;

            leftEyeCMUpdate.x = -_pixelToMeter(eyeDistanceCM, (int)leftToCenter.x);
            leftEyeCMUpdate.y = -_pixelToMeter(eyeDistanceCM, (int)leftToCenter.y);
            leftEyeCMUpdate.z = -eyeDistanceCM;
        }
        // DrawEyePositions(colors, new Color32(0, 255, 0, 255), 3);

        //draw landmark points
        // DrawLandmarkDetectionResult(detectLandmarkResult, colors, new Color32(255, 0, 0, 255), 3);



        // //draw face rect
        // if (drawRect)
        //     _faceLandmarkDetector.DrawDetectResult(colors, _texture.width, _texture.height, 4, true, 255, 0, 0, 255, 2);
        //
        // if (opacity == 0)
        //     return;
        //
        // for (var index = 0; index < colors.Length; index++)
        // {
        //     colors[index].a = (byte)opacity;
        // }
        // _texture.SetPixels32(colors);
        // _texture.Apply(false);
    }

    private void EyeDetectWorker()
    {
        while (true)
        {
            // Lock the _colors array while reading from it in the separate thread
            lock (_colorsLock)
            {
                _faceLandmarkDetector.SetImage(_colors, width, height, 4, true); // Reads from SHARED _colors array
                // UnityMainThreadDispatcher.Instance().Enqueue(() =>
                // {
            }

            lock (_detectLandmarkResultLock)
            {
                var detectRectResult = _faceLandmarkDetector.DetectRectDetection();

                if (detectRectResult.Count == 0)
                    continue;

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
                _detectLandmarkResult = _faceLandmarkDetector.DetectLandmark(detectRect.rect); // Writes to SHARED _detectLandmarkResult List
                // });
            }
        }
    }

    private float _pixelToMeter(float distanceM, int widthPx)
    {
        return (distanceM * widthPx) / focalDistancePx;
    }
    
    // // This function returns a flattened 2D array where the data appears row by row.
    // // Unity lays out the array's pixels left to right, bottom to top.
    // private Color32[] GetColors()
    // {
    //     _webCamTexture.GetPixels32(_colors);
    //     return _colors;
    // }

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

    // private void DrawLandmarkDetectionResult(List<Vector2> result, Color32[] colors, Color32 color, int radius)
    // {
    //     for (var index = 0; index < result.Count; index++)
    //     {
    //         var r = result[index];
    //         var x = (int)r.x;
    //         var y = (int)(_texture.height - r.y);
    //
    //         color.b = (byte)(0 + index * 50);
    //         DrawPosition(new Vector2(x, y), colors, color, radius);
    //     }
    // }
    //
    // private void DrawEyePositions(Color32[] colors, Color32 color, int radius)
    // {
    //     var rightEyePosition = new Vector2(_rightEyePx.x, _texture.height - _rightEyePx.y);
    //     var leftEyePosition = new Vector2(_leftEyePx.x, _texture.height - _leftEyePx.y);
    //
    //     DrawPosition(rightEyePosition, colors, color, radius);
    //     DrawPosition(leftEyePosition, colors, color, radius);
    // }
    //
    // private void DrawPosition(Vector2 position, Color32[] colors, Color32 color, int radius)
    // {
    //     var from = new Vector2Int((int)position.x - radius, (int)position.y - radius);
    //     var to = new Vector2Int((int)position.x + radius, (int)position.y + radius);
    //
    //     for (var i = from.x; i < to.x; ++i)
    //     {
    //         for (var j = from.y; j < to.y; ++j)
    //         {
    //             colors[i + _texture.width * j].r = color.r;
    //             colors[i + _texture.width * j].g = color.g;
    //             colors[i + _texture.width * j].b = color.b;
    //         }
    //     }
    // }
}
