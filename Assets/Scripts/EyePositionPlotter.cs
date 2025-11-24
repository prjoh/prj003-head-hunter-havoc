using System.Collections.Generic;
using UnityEngine;

public class EyePositionPlotter : MonoBehaviour
{
  [Tooltip("RenderTexture to draw the graph on.")]
  [SerializeField] private RenderTexture m_PlotTexture;

  [Tooltip("OffAxisProjection component providing NoFilter, Lerp, and Kalman eye positions.")]
  [SerializeField] private OffAxisProjection m_OffAxisProjection;

  [Tooltip("Enable plotting of X coordinate.")]
  [SerializeField] private bool m_PlotX = true;

  [Tooltip("Enable plotting of Y coordinate.")]
  [SerializeField] private bool m_PlotY = true;

  [Tooltip("Enable plotting of Z coordinate.")]
  [SerializeField] private bool m_PlotZ = true;

  [Header("Series Toggles")]
  [Tooltip("Plot eye position with no filtering (OffAxisProjection.EyePositionNoFilter).")]
  [SerializeField] private bool m_PlotNoFilter = true;

  [Tooltip("Plot eye position with Lerp filtering (OffAxisProjection.EyePositionLerp).")]
  [SerializeField] private bool m_PlotLerp = true;

  [Tooltip("Plot eye position with Kalman filtering (OffAxisProjection.EyePositionKalman).")]
  [SerializeField] private bool m_PlotKalman = true;

  [Header("NoFilter Colors")]
  [Tooltip("Color for X axis plot (NoFilter).")]
  [SerializeField] private Color m_XNoFilterColor = new Color(1.0f, 0.6f, 0.0f, 0.9f);

  [Tooltip("Color for Y axis plot (NoFilter).")]
  [SerializeField] private Color m_YNoFilterColor = new Color(0.0f, 1.0f, 1.0f, 0.9f);

  [Tooltip("Color for Z axis plot (NoFilter).")]
  [SerializeField] private Color m_ZNoFilterColor = new Color(1.0f, 0.0f, 1.0f, 0.9f);

  [Header("Lerp Colors")]
  [Tooltip("Color for X axis plot (Lerp).")]
  [SerializeField] private Color m_XLerpColor = new Color(0.8f, 0.8f, 0.0f, 0.9f);

  [Tooltip("Color for Y axis plot (Lerp).")]
  [SerializeField] private Color m_YLerpColor = new Color(0.6f, 0.2f, 1.0f, 0.9f);

  [Tooltip("Color for Z axis plot (Lerp).")]
  [SerializeField] private Color m_ZLerpColor = new Color(0.2f, 1.0f, 0.6f, 0.9f);

  [Header("Kalman Colors")]
  [Tooltip("Color for X axis plot (Kalman).")]
  [SerializeField] private Color m_XKalmanColor = Color.red;

  [Tooltip("Color for Y axis plot (Kalman).")]
  [SerializeField] private Color m_YKalmanColor = Color.green;

  [Tooltip("Color for Z axis plot (Kalman).")]
  [SerializeField] private Color m_ZKalmanColor = Color.blue;

  [Tooltip("Background color of the graph.")]
  [SerializeField] private Color m_BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);

  [Tooltip("Color for grid lines.")]
  [SerializeField] private Color m_GridColor = new Color(0.3f, 0.3f, 0.3f, 1f);

  [Tooltip("Color for axis lines.")]
  [SerializeField] private Color m_AxisColor = new Color(0.5f, 0.5f, 0.5f, 1f);

  [Tooltip("Maximum number of data points to store in history (per series).")]
  [SerializeField] private int m_MaxDataPoints = 1000;

  [Tooltip("Padding around the graph area in pixels.")]
  [SerializeField] private int m_Padding = 20;

  [Tooltip("Number of horizontal grid lines.")]
  [SerializeField] private int m_GridLinesHorizontal = 5;

  [Tooltip("Number of vertical grid lines.")]
  [SerializeField] private int m_GridLinesVertical = 10;

  [Tooltip("Line thickness for the plot lines.")]
  [SerializeField] private int m_LineThickness = 2;

  private Queue<Vector3> m_NoFilterHistory; // OffAxisProjection.EyePositionNoFilter
  private Queue<Vector3> m_LerpHistory;     // OffAxisProjection.EyePositionLerp
  private Queue<Vector3> m_KalmanHistory;   // OffAxisProjection.EyePositionKalman
  private Texture2D m_PlotTexture2D;
  private Vector2 m_ValueRange = new Vector2(-10f, 10f); // Min and max value range for Y axis
  private int m_WindowSizePixels = 0; // Calculated based on graph width

  private void Awake()
  {
    m_NoFilterHistory = new Queue<Vector3>();
    m_LerpHistory = new Queue<Vector3>();
    m_KalmanHistory = new Queue<Vector3>();
  }

  private void OnDestroy()
  {
    Cleanup();
  }

  private void OnDisable()
  {
    Cleanup();
  }

  private void Cleanup()
  {
    if (m_PlotTexture2D != null)
    {
      Destroy(m_PlotTexture2D);
      m_PlotTexture2D = null;
    }
  }

  private void Update()
  {
    if (m_PlotTexture == null || m_OffAxisProjection == null)
    {
      return;
    }

    // Check if RenderTexture is still valid
    if (m_PlotTexture.width <= 0 || m_PlotTexture.height <= 0)
    {
      return;
    }

    // Get current eye positions for all three filter modes from OffAxisProjection
    Vector3 currentNoFilter = m_OffAxisProjection.EyePositionNoFilter;
    Vector3 currentLerp = m_OffAxisProjection.EyePositionLerp;
    Vector3 currentKalman = m_OffAxisProjection.EyePositionKalman;

    // Add to histories
    if (m_PlotNoFilter)
    {
      m_NoFilterHistory.Enqueue(currentNoFilter);
      while (m_NoFilterHistory.Count > m_MaxDataPoints)
      {
        m_NoFilterHistory.Dequeue();
      }
    }

    if (m_PlotLerp)
    {
      m_LerpHistory.Enqueue(currentLerp);
      while (m_LerpHistory.Count > m_MaxDataPoints)
      {
        m_LerpHistory.Dequeue();
      }
    }

    if (m_PlotKalman)
    {
      m_KalmanHistory.Enqueue(currentKalman);
      while (m_KalmanHistory.Count > m_MaxDataPoints)
      {
        m_KalmanHistory.Dequeue();
      }
    }

    // Update value range based on current data
    UpdateValueRange();

    // Draw the graph
    DrawGraph();
  }

  private void UpdateValueRange()
  {
    if ((!m_PlotNoFilter || m_NoFilterHistory.Count == 0) &&
        (!m_PlotLerp || m_LerpHistory.Count == 0) &&
        (!m_PlotKalman || m_KalmanHistory.Count == 0))
      return;
    if (m_PlotTexture == null)
      return;

    // Calculate window size based on graph width in pixels
    int graphWidth = m_PlotTexture.width - 2 * m_Padding;
    m_WindowSizePixels = Mathf.Max(1, graphWidth);

    float minValue = float.MaxValue;
    float maxValue = float.MinValue;

    // Helper local function to scan a queue
    void ScanQueue(Queue<Vector3> q)
    {
      if (q == null || q.Count == 0) return;
      int visible = Mathf.Min(m_WindowSizePixels, q.Count);
      var arr = q.ToArray();
      int start = q.Count - visible;
      for (int i = start; i < arr.Length; i++)
      {
        var data = arr[i];
        if (m_PlotX)
        {
          minValue = Mathf.Min(minValue, data.x);
          maxValue = Mathf.Max(maxValue, data.x);
        }
        if (m_PlotY)
        {
          minValue = Mathf.Min(minValue, data.y);
          maxValue = Mathf.Max(maxValue, data.y);
        }
        if (m_PlotZ)
        {
          minValue = Mathf.Min(minValue, data.z);
          maxValue = Mathf.Max(maxValue, data.z);
        }
      }
    }

    if (m_PlotNoFilter) ScanQueue(m_NoFilterHistory);
    if (m_PlotLerp) ScanQueue(m_LerpHistory);
    if (m_PlotKalman) ScanQueue(m_KalmanHistory);

    // Add some padding to the range
    float padding = (maxValue - minValue) * 0.1f;
    m_ValueRange.x = minValue - padding;
    m_ValueRange.y = maxValue + padding;

    // Ensure minimum range
    if (m_ValueRange.y - m_ValueRange.x < 0.1f)
    {
      m_ValueRange.x = -0.05f;
      m_ValueRange.y = 0.05f;
    }
  }

  private void DrawGraph()
  {
    if (m_PlotTexture == null)
      return;

    // Check if RenderTexture is still valid
    if (m_PlotTexture.width <= 0 || m_PlotTexture.height <= 0)
      return;

    int width = m_PlotTexture.width;
    int height = m_PlotTexture.height;

    // Create or resize Texture2D if needed
    if (m_PlotTexture2D == null || m_PlotTexture2D.width != width || m_PlotTexture2D.height != height)
    {
      if (m_PlotTexture2D != null)
      {
        Destroy(m_PlotTexture2D);
      }
      m_PlotTexture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
    }

    // Initialize with background color
    Color32[] pixels = new Color32[width * height];
    Color32 bgColor32 = m_BackgroundColor;
    for (int i = 0; i < pixels.Length; i++)
    {
      pixels[i] = bgColor32;
    }

    // Draw grid
    DrawGrid(pixels, width, height);

    // Draw axes
    DrawAxes(pixels, width, height);

    // Draw plots - only show the most recent window of data
    if (m_WindowSizePixels > 0)
    {
      // Helper to draw from a queue with given colors and thickness
      void DrawFromQueue(Queue<Vector3> q, Color cx, Color cy, Color cz, int thickness)
      {
        if (q == null || q.Count < 2) return;
        var dataArray = q.ToArray();
        int visibleCount = Mathf.Min(m_WindowSizePixels, dataArray.Length);
        if (visibleCount < 2) return;
        int startIndex = dataArray.Length - visibleCount;
        Vector3[] windowData = new Vector3[visibleCount];
        System.Array.Copy(dataArray, startIndex, windowData, 0, visibleCount);

        if (m_PlotX) DrawPlot(windowData, width, height, 0, cx, pixels, thickness);
        if (m_PlotY) DrawPlot(windowData, width, height, 1, cy, pixels, thickness);
        if (m_PlotZ) DrawPlot(windowData, width, height, 2, cz, pixels, thickness);
      }

      // Draw NoFilter first (thinnest), then Lerp, then Kalman on top (thickest).
      if (m_PlotNoFilter) DrawFromQueue(m_NoFilterHistory, m_XNoFilterColor, m_YNoFilterColor, m_ZNoFilterColor, Mathf.Max(1, m_LineThickness - 1));
      if (m_PlotLerp) DrawFromQueue(m_LerpHistory, m_XLerpColor, m_YLerpColor, m_ZLerpColor, m_LineThickness);
      if (m_PlotKalman) DrawFromQueue(m_KalmanHistory, m_XKalmanColor, m_YKalmanColor, m_ZKalmanColor, m_LineThickness + 1);
    }

    // Write pixels to texture
    m_PlotTexture2D.SetPixels32(pixels);
    m_PlotTexture2D.Apply();

    // Blit to RenderTexture
    Graphics.Blit(m_PlotTexture2D, m_PlotTexture);
  }

  private void DrawGrid(Color32[] pixels, int width, int height)
  {
    int graphWidth = width - 2 * m_Padding;
    int graphHeight = height - 2 * m_Padding;
    int graphX = m_Padding;
    int graphY = m_Padding;

    Color32 gridColor32 = m_GridColor;

    // Draw horizontal grid lines
    for (int i = 0; i <= m_GridLinesHorizontal; i++)
    {
      float t = (float)i / m_GridLinesHorizontal;
      int y = graphY + Mathf.RoundToInt(graphHeight * t);

      for (int x = graphX; x < graphX + graphWidth; x++)
      {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
          pixels[x + width * y] = gridColor32;
        }
      }
    }

    // Draw vertical grid lines
    for (int i = 0; i <= m_GridLinesVertical; i++)
    {
      float t = (float)i / m_GridLinesVertical;
      int x = graphX + Mathf.RoundToInt(graphWidth * t);

      for (int y = graphY; y < graphY + graphHeight; y++)
      {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
          pixels[x + width * y] = gridColor32;
        }
      }
    }
  }

  private void DrawAxes(Color32[] pixels, int width, int height)
  {
    int graphWidth = width - 2 * m_Padding;
    int graphHeight = height - 2 * m_Padding;
    int graphX = m_Padding;
    int graphY = m_Padding;

    Color32 axisColor32 = m_AxisColor;

    // Draw horizontal axis (X axis, at zero value)
    float zeroY = graphY + graphHeight * 0.5f; // Center of graph
    for (int x = graphX; x < graphX + graphWidth; x++)
    {
      if (x >= 0 && x < width)
      {
        int y = Mathf.RoundToInt(zeroY);
        if (y >= 0 && y < height)
        {
          pixels[x + width * y] = axisColor32;
        }
      }
    }

    // Draw vertical axis (Y axis, at left edge)
    for (int y = graphY; y < graphY + graphHeight; y++)
    {
      if (y >= 0 && y < height)
      {
        int x = graphX;
        if (x >= 0 && x < width)
        {
          pixels[x + width * y] = axisColor32;
        }
      }
    }
  }

  private void DrawPlot(Vector3[] data, int width, int height, int componentIndex, Color color, Color32[] pixels, int thickness)
  {
    if (data.Length < 2)
      return;

    int graphWidth = width - 2 * m_Padding;
    int graphHeight = height - 2 * m_Padding;
    int graphX = m_Padding;
    int graphY = m_Padding;

    Color32 color32 = color;
    float valueRange = m_ValueRange.y - m_ValueRange.x;

    // Draw line segments
    for (int i = 0; i < data.Length - 1; i++)
    {
      float value1 = GetComponentValue(data[i], componentIndex);
      float value2 = GetComponentValue(data[i + 1], componentIndex);

      // Normalize values to 0-1 range
      float normalized1 = (value1 - m_ValueRange.x) / valueRange;
      float normalized2 = (value2 - m_ValueRange.x) / valueRange;

      // Convert to screen coordinates (flip Y for Unity texture coordinates)
      int x1 = graphX + Mathf.RoundToInt((float)i / (data.Length - 1) * graphWidth);
      int y1 = graphY + graphHeight - Mathf.RoundToInt(normalized1 * graphHeight);
      int x2 = graphX + Mathf.RoundToInt((float)(i + 1) / (data.Length - 1) * graphWidth);
      int y2 = graphY + graphHeight - Mathf.RoundToInt(normalized2 * graphHeight);

      // Draw line with thickness
      DrawLine(x1, y1, x2, y2, pixels, width, height, color32, thickness);
    }
  }

  private float GetComponentValue(Vector3 vector, int componentIndex)
  {
    switch (componentIndex)
    {
      case 0: return vector.x;
      case 1: return vector.y;
      case 2: return vector.z;
      default: return 0f;
    }
  }

  private void DrawLine(int x1, int y1, int x2, int y2, Color32[] pixels, int width, int height, Color32 color, int thickness)
  {
    // Bresenham's line algorithm
    int dx = Mathf.Abs(x2 - x1);
    int dy = Mathf.Abs(y2 - y1);
    int sx = x1 < x2 ? 1 : -1;
    int sy = y1 < y2 ? 1 : -1;
    int err = dx - dy;

    int x = x1;
    int y = y1;

    while (true)
    {
      // Draw pixel with thickness
      for (int tx = -thickness / 2; tx <= thickness / 2; tx++)
      {
        for (int ty = -thickness / 2; ty <= thickness / 2; ty++)
        {
          int px = x + tx;
          int py = y + ty;
          if (px >= 0 && px < width && py >= 0 && py < height)
          {
            pixels[px + width * py] = color;
          }
        }
      }

      if (x == x2 && y == y2)
        break;

      int e2 = 2 * err;
      if (e2 > -dy)
      {
        err -= dy;
        x += sx;
      }
      if (e2 < dx)
      {
        err += dx;
        y += sy;
      }
    }
  }
}

