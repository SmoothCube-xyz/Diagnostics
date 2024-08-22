using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NetworkDiagnostics : MonoBehaviour
{
    private float deltaTime = 0.0f;
    private float updateInterval = 0.5f;
    private float accum = 0.0f;
    private int frames = 0;
    private float timeleft;
    private float fps;

    private Queue<float> fpsHistory = new Queue<float>();
    private int fpsHistoryLength = 60;

    private StringBuilder stringBuilder = new StringBuilder();

    private float dataReceived = 0f;
    private float dataSent = 0f;
    private float dataReceivedPeak = 0f;
    private float dataSentPeak = 0f;

    private float lastCheckTime;
    private bool showDiagnostics = false;
    private Rect windowRect = new Rect(20, 20, 350, 500);
    private Vector2 scrollPosition;

    private long totalAllocatedMemory;
    private long totalReservedMemory;
    private long totalUnusedReservedMemory;

    private int drawCalls;
    private int batches;
    private int triangles;
    private int vertices;

    void Start()
    {
        lastCheckTime = Time.time;
        InvokeRepeating(nameof(ResetPeakValues), 5f, 5f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            showDiagnostics = !showDiagnostics;
        }

        UpdateFPSData();
        UpdateMemoryStats();
        UpdateRenderingStats();
    }

    void UpdateFPSData()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        accum += Time.unscaledDeltaTime;
        ++frames;

        if (timeleft <= 0.0)
        {
            fps = frames / accum;
            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;

            fpsHistory.Enqueue(fps);
            if (fpsHistory.Count > fpsHistoryLength)
                fpsHistory.Dequeue();
        }

        timeleft -= Time.unscaledDeltaTime;
    }

    void UpdateMemoryStats()
    {
        totalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        totalReservedMemory = Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
        totalUnusedReservedMemory = Profiler.GetTotalUnusedReservedMemoryLong() / (1024 * 1024);
    }

    void UpdateRenderingStats()
    {
#if UNITY_EDITOR
        drawCalls = UnityStats.drawCalls;
        batches = UnityStats.batches;
        triangles = UnityStats.triangles;
        vertices = UnityStats.vertices;
#endif
    }

    void ResetPeakValues()
    {
        dataReceivedPeak = 0f;
        dataSentPeak = 0f;
    }

    void OnGUI()
    {
        if (showDiagnostics)
        {
            windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "Diagnostics");
        }
    }

    void DoMyWindow(int windowID)
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        DisplayPerformanceStats();
        DisplayMemoryStats();

#if UNITY_EDITOR
        DisplayRenderingStats();
#endif

        DisplaySystemInfo();

        GUILayout.EndScrollView();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    void DisplayPerformanceStats()
    {
        GUILayout.Label("Performance", GUI.skin.box);
        GUILayout.Label($"FPS: {fps:0.} ({deltaTime * 1000.0f:0.0} ms)");
        GUILayout.Label($"Avg FPS (1min): {CalculateAverageFPS():0.}");
        GUILayout.Label($"Min FPS (1min): {CalculateMinFPS():0.}");
        GUILayout.Label($"Max FPS (1min): {CalculateMaxFPS():0.}");
        GUILayout.Label($"Time Scale: {Time.timeScale:F2}");
    }

    void DisplayMemoryStats()
    {
        GUILayout.Label("Memory", GUI.skin.box);
        GUILayout.Label($"Allocated: {totalAllocatedMemory} MB");
        GUILayout.Label($"Reserved: {totalReservedMemory} MB");
        GUILayout.Label($"Unused Reserved: {totalUnusedReservedMemory} MB");
        GUILayout.Label($"Mono Usage: {GetMonoUsage()} MB");
        GUILayout.Label($"Texture Memory: {Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024 * 1024)} MB");
    }

#if UNITY_EDITOR
    void DisplayRenderingStats()
    {
        GUILayout.Label("Rendering", GUI.skin.box);
        GUILayout.Label($"Draw Calls: {drawCalls}");
        GUILayout.Label($"Batches: {batches}");
        GUILayout.Label($"Triangles: {triangles}");
        GUILayout.Label($"Vertices: {vertices}");
        GUILayout.Label($"Screen Resolution: {Screen.width}x{Screen.height}");
        GUILayout.Label($"Quality Level: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
    }
#endif

    void DisplaySystemInfo()
    {
        GUILayout.Label("System", GUI.skin.box);
        GUILayout.Label($"OS: {SystemInfo.operatingSystem}");
        GUILayout.Label($"Device: {SystemInfo.deviceModel}");
        GUILayout.Label($"CPU: {SystemInfo.processorType}");
        GUILayout.Label($"CPU Cores: {SystemInfo.processorCount}");
        GUILayout.Label($"CPU Frequency: {SystemInfo.processorFrequency} MHz");
        GUILayout.Label($"GPU: {SystemInfo.graphicsDeviceName}");
        GUILayout.Label($"GPU Memory: {SystemInfo.graphicsMemorySize} MB");
        GUILayout.Label($"RAM: {SystemInfo.systemMemorySize} MB");
        GUILayout.Label($"Max Texture Size: {SystemInfo.maxTextureSize}");
        GUILayout.Label($"Supports Instancing: {SystemInfo.supportsInstancing}");
        GUILayout.Label($"Supports Ray Tracing: {SystemInfo.supportsRayTracing}");
        GUILayout.Label($"Graphics API: {SystemInfo.graphicsDeviceType}");
        GUILayout.Label($"Graphics API Version: {SystemInfo.graphicsDeviceVersion}");
    }

    private float CalculateAverageFPS() => fpsHistory.Count == 0 ? 0 : fpsHistory.Average();
    private float CalculateMinFPS() => fpsHistory.Count == 0 ? 0 : fpsHistory.Min();
    private float CalculateMaxFPS() => fpsHistory.Count == 0 ? 0 : fpsHistory.Max();

    private long GetMonoUsage()
    {
        return System.GC.GetTotalMemory(false) / (1024 * 1024);
    }
}
