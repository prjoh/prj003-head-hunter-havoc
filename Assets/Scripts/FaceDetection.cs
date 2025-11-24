using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityUtils;
using UnityEngine;

using PredictorNamePreset = DlibFaceLandmarkDetectorExample.DlibFaceLandmarkDetectorExample.DlibShapePredictorNamePreset;

public class FaceDetection : MonoBehaviour
{
  [SerializeField] private PredictorNamePreset m_DlibShapePredictorName = PredictorNamePreset.sp_human_face_68;

  [Tooltip("Set the width of WebCamTexture. Allowed formats: 1920,1080:1280,720:640,480.")]
  [SerializeField] private int m_RequestedWidth = 320;

  [Tooltip("Set the height of WebCamTexture.")]
  [SerializeField] private int m_RequestedHeight = 240;

  [Tooltip("Set FPS of WebCamTexture.")]
  [SerializeField] private int m_RequestedFPS = 30;
  [SerializeField] private float m_IpdCM = 6.4f;
  [SerializeField] private int m_FocalDistancePx = 711;

  [Tooltip("Enable debug mode to render camera stream to texture.")]
  [SerializeField] private bool m_DebugMode = false;

  [Tooltip("RenderTexture to output the camera stream to when debug mode is enabled. If null, will be created automatically.")]
  [SerializeField] private RenderTexture m_DebugRenderTexture;

  private FaceLandmarkDetector m_FaceLandmarkDetector;
  private WebCamTexture m_WebCamTexture;
  private WebCamDevice m_WebCamDevice;

  private Color32[] m_Colors;
  private int m_Width;
  private int m_Height;

  private bool m_InitDone = false;

  private Vector2 m_LeftEyePx;
  private Vector2 m_RightEyePx;

  private Vector3 m_LeftEyeCMUpdate;
  public Vector3 LeftEyeCMUpdate => m_LeftEyeCMUpdate;

  private Vector3 m_RightEyeCMUpdate;
  public Vector3 RightEyeCMUpdate => m_RightEyeCMUpdate;

  // Raw (unfiltered) eye positions in CM for plotting/diagnostics
  private Vector3 m_LeftEyeCMRaw;
  public Vector3 LeftEyeCMRaw => m_LeftEyeCMRaw;
  private Vector3 m_RightEyeCMRaw;
  public Vector3 RightEyeCMRaw => m_RightEyeCMRaw;

  private List<Vector2> m_DetectLandmarkResult;
  private List<Vector2> m_DetectLandmarkResultForDrawing; // Copy for drawing in debug mode

  private bool m_DebugRenderTextureCreated = false; // Track if we created the RenderTexture ourselves
  private Texture2D m_DebugTexture2D; // Temporary Texture2D for drawing operations

  public RenderTexture DebugRenderTexture => m_DebugRenderTexture;

  private readonly object m_ColorsLock = new object(); // lock object for _colors array
  private readonly object m_DetectLandmarkResultLock = new object(); // lock object for _detectLandmarkResult list

  private bool m_IsEnabled = false;
  public void SetEnabled(bool isEnabled)
  {
    m_IsEnabled = isEnabled;
  }

  public delegate void OnWebcamInit();
  public static event OnWebcamInit WebcamInit;

  public delegate void OnWebcamFailed();
  public static event OnWebcamFailed WebcamFailed;

  private void Awake()
  {
    m_DetectLandmarkResult = new List<Vector2>();
    m_DetectLandmarkResultForDrawing = new List<Vector2>();
  }

  private void OnDestroy()
  {
    // Clean up auto-created RenderTexture
    if (m_DebugRenderTextureCreated && m_DebugRenderTexture != null)
    {
      m_DebugRenderTexture.Release();
      Destroy(m_DebugRenderTexture);
      m_DebugRenderTexture = null;
    }

    // Clean up debug Texture2D
    if (m_DebugTexture2D != null)
    {
      Destroy(m_DebugTexture2D);
      m_DebugTexture2D = null;
    }
  }


  private void Start()
  {
    var path = Utils.getFilePath("DlibFaceLandmarkDetector/" + m_DlibShapePredictorName + ".dat");
    m_FaceLandmarkDetector = new FaceLandmarkDetector(path);

    StartCoroutine(Initialize());
  }

  private void Update()
  {
    if (!m_IsEnabled)
    {
      return;
    }

    if (!m_InitDone || !m_WebCamTexture.isPlaying || !m_WebCamTexture.didUpdateThisFrame)
    {
      return;
    }

    lock (m_ColorsLock)
    {
      m_WebCamTexture.GetPixels32(m_Colors); // Writes to SHARED _colors array
      if (m_Colors == null)
      {
        return;
      }
    }

    var deltaTime = Time.deltaTime;

    lock (m_DetectLandmarkResultLock)
    {
      if (m_DetectLandmarkResult.Count <= 0)
      {
        return;
      }

      UpdateEyePositions(m_DetectLandmarkResult);  // Reads from SHARED _detectLandmarkResult List

      // Copy landmark results for drawing (before clearing)
      if (m_DebugMode)
      {
        m_DetectLandmarkResultForDrawing.Clear();
        m_DetectLandmarkResultForDrawing.AddRange(m_DetectLandmarkResult);
      }

      m_DetectLandmarkResult.Clear();

      var ipdPx = (int)(m_LeftEyePx - m_RightEyePx).magnitude;
      var eyeDistanceCM = m_FocalDistancePx * m_IpdCM / ipdPx;

      var centerPx = new Vector2Int(m_Width / 2, m_Height / 2);
      var leftToCenter = m_LeftEyePx - centerPx;
      var rightToCenter = m_RightEyePx - centerPx;

      var rawRightEyeCM = new Vector3(
        -PixelToMeter(eyeDistanceCM, (int)rightToCenter.x),
        -PixelToMeter(eyeDistanceCM, (int)rightToCenter.y),
        -eyeDistanceCM
      );

      var rawLeftEyeCM = new Vector3(
        -PixelToMeter(eyeDistanceCM, (int)leftToCenter.x),
        -PixelToMeter(eyeDistanceCM, (int)leftToCenter.y),
        -eyeDistanceCM
      );

      // Store raw values for plotting/diagnostics
      m_LeftEyeCMRaw = rawLeftEyeCM;
      m_RightEyeCMRaw = rawRightEyeCM;

      m_RightEyeCMUpdate = rawRightEyeCM;
      m_LeftEyeCMUpdate = rawLeftEyeCM;
    }

    // Update debug render texture if debug mode is enabled
    if (m_DebugMode && m_DebugRenderTexture != null)
    {
      UpdateDebugTexture();
    }
  }

  private IEnumerator Initialize()
  {
    yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

    if (Application.HasUserAuthorization(UserAuthorization.WebCam))
    {
      yield return StartCamera();
    }
    else
    {
      Debug.LogError("FaceDetection: Failed to start camera.");
    }
  }

  private IEnumerator StartCamera()
  {
    var devices = WebCamTexture.devices;
    if (devices.Length > 0)
    {
      m_WebCamDevice = devices[0];
      m_WebCamTexture = new WebCamTexture(m_WebCamDevice.name, m_RequestedWidth, m_RequestedHeight, m_RequestedFPS);
    }
    else
    {
      Debug.LogError("No camera device exists!");
      WebcamFailed?.Invoke();
      yield break;
    }

    m_WebCamTexture.Play();

    while (true)
    {
      if (m_WebCamTexture.didUpdateThisFrame)
      {
        if (m_Colors == null || m_Colors.Length != m_WebCamTexture.width * m_WebCamTexture.height)
        {
          m_Colors = new Color32[m_WebCamTexture.width * m_WebCamTexture.height];
        }

        var texture = new Texture2D(m_WebCamTexture.width, m_WebCamTexture.height, TextureFormat.RGBA32, false);
        m_Width = texture.width;
        m_Height = texture.height;

        // Create debug render texture if debug mode is enabled and no texture is assigned
        if (m_DebugMode && m_DebugRenderTexture == null)
        {
          m_DebugRenderTexture = new RenderTexture(m_Width, m_Height, 0, RenderTextureFormat.ARGB32);
          m_DebugRenderTexture.name = "FaceDetection_DebugRT";
          m_DebugRenderTextureCreated = true;
        }

        m_InitDone = true;

        break;
      }

      yield return 0;
    }

    // Start the thread that reads from m_Colors and writes to m_DetectLandmarkResult
    var thread = new Thread(EyeDetectWorker);
    thread.Start();

    WebcamInit?.Invoke();
  }


  private void EyeDetectWorker()
  {
    while (true)
    {
      // Lock the _colors array while reading from it in the separate thread
      lock (m_ColorsLock)
      {
        m_FaceLandmarkDetector.SetImage(m_Colors, m_Width, m_Height, 4, true); // Reads from SHARED m_Colors array
      }

      lock (m_DetectLandmarkResultLock)
      {
        var detectRectResult = m_FaceLandmarkDetector.DetectRectDetection();

        if (detectRectResult.Count == 0)
        {
          continue;
        }

        // Filter for most confident result
        var detectRect = detectRectResult[0];
        if (detectRectResult.Count > 1)
        {
          foreach (var rect in detectRectResult)
          {
            if (rect.detection_confidence > detectRect.detection_confidence)
            {
              detectRect = rect;
            }
          }
        }

        //detect landmark points
        m_DetectLandmarkResult = m_FaceLandmarkDetector.DetectLandmark(detectRect.rect); // Writes to SHARED m_DetectLandmarkResult List
      }
    }
  }

  private float PixelToMeter(float distanceM, int widthPx)
  {
    return distanceM * widthPx / m_FocalDistancePx;
  }

  private void UpdateEyePositions(List<Vector2> result)
  {
    if (m_DlibShapePredictorName is PredictorNamePreset.sp_human_face_68 or PredictorNamePreset.sp_human_face_68_for_mobile)
    {
      var rightEye0 = result[36];
      var rightEye1 = result[39];
      var leftEye0 = result[42];
      var leftEye1 = result[45];

      m_RightEyePx = (rightEye0 + rightEye1) * 0.5f;
      m_LeftEyePx = (leftEye0 + leftEye1) * 0.5f;
    }

    else
    {
      var rightEye0 = result[2];
      var rightEye1 = result[3];
      var leftEye0 = result[4];
      var leftEye1 = result[5];

      m_RightEyePx = (rightEye0 + rightEye1) * 0.5f;
      m_LeftEyePx = (leftEye0 + leftEye1) * 0.5f;
    }
  }

  private void UpdateDebugTexture()
  {
    // Blit WebCamTexture to RenderTexture
    Graphics.Blit(m_WebCamTexture, m_DebugRenderTexture);

    // Create or resize temporary Texture2D if needed
    if (m_DebugTexture2D == null || m_DebugTexture2D.width != m_Width || m_DebugTexture2D.height != m_Height)
    {
      if (m_DebugTexture2D != null)
      {
        Destroy(m_DebugTexture2D);
      }
      m_DebugTexture2D = new Texture2D(m_Width, m_Height, TextureFormat.RGBA32, false);
    }

    // Read pixels from RenderTexture
    RenderTexture.active = m_DebugRenderTexture;
    m_DebugTexture2D.ReadPixels(new Rect(0, 0, m_Width, m_Height), 0, 0);
    m_DebugTexture2D.Apply();
    RenderTexture.active = null;

    // Get pixel array for drawing
    var pixels = m_DebugTexture2D.GetPixels32();

    // Draw detection results
    lock (m_DetectLandmarkResultLock)
    {
      // Draw landmark points
      if (m_DetectLandmarkResultForDrawing.Count > 0)
      {
        DrawLandmarkDetectionResult(m_DetectLandmarkResultForDrawing, pixels, new Color32(255, 0, 0, 255), 2);
      }

      // Draw eye positions
      DrawEyePositions(pixels, new Color32(0, 255, 0, 255), 3);
    }

    // Write pixels back to Texture2D
    m_DebugTexture2D.SetPixels32(pixels);
    m_DebugTexture2D.Apply();

    // Blit back to RenderTexture
    Graphics.Blit(m_DebugTexture2D, m_DebugRenderTexture);
  }

  private void DrawLandmarkDetectionResult(List<Vector2> result, Color32[] colors, Color32 color, int radius)
  {
    for (var index = 0; index < result.Count; index++)
    {
      var r = result[index];
      var x = (int)r.x;
      var y = (int)(m_Height - r.y); // Flip Y coordinate

      var drawColor = color;
      drawColor.b = (byte)(0 + index * 50);
      DrawPosition(new Vector2(x, y), colors, drawColor, radius);
    }
  }

  private void DrawEyePositions(Color32[] colors, Color32 color, int radius)
  {
    var rightEyePosition = new Vector2(m_RightEyePx.x, m_Height - m_RightEyePx.y);
    var leftEyePosition = new Vector2(m_LeftEyePx.x, m_Height - m_LeftEyePx.y);

    DrawPosition(rightEyePosition, colors, color, radius);
    DrawPosition(leftEyePosition, colors, color, radius);
  }

  private void DrawPosition(Vector2 position, Color32[] colors, Color32 color, int radius)
  {
    var from = new Vector2Int(Mathf.Max(0, (int)position.x - radius), Mathf.Max(0, (int)position.y - radius));
    var to = new Vector2Int(Mathf.Min(m_Width - 1, (int)position.x + radius), Mathf.Min(m_Height - 1, (int)position.y + radius));

    for (var i = from.x; i <= to.x; ++i)
    {
      for (var j = from.y; j <= to.y; ++j)
      {
        var dist = Vector2.Distance(new Vector2(i, j), position);
        if (dist <= radius)
        {
          var index = i + m_Width * j;
          if (index >= 0 && index < colors.Length)
          {
            colors[index] = color;
          }
        }
      }
    }
  }
}
