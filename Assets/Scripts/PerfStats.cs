using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

// Measures FPS, draw calls and triangles over a fixed time window.
// Draw calls and triangles need a Development Build (Build Profiles > Development Build).
public class PerfStats : MonoBehaviour
{
    [Header("Test")]
    public float testSeconds = 30f;
    public float warmupSeconds = 2f;
    public bool visible = true;           // untick for the final demo build

    ProfilerRecorder drawCalls, triangles;

    readonly List<float> frameTimes = new List<float>();
    bool testing;
    float testTimer, warmTimer;
    double sumDraw, sumTri;
    int samples;
    long maxDraw, maxTri;

    // Last result
    bool hasResult;
    float avgFps, minFps, lowFps;
    double avgDraw, avgTri;
    long peakDraw, peakTri;
    bool countersAvailable;

    float liveFps = 60f;
    GUIStyle label, button;

    void OnEnable()
    {
        drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
    }

    void OnDisable()
    {
        if (drawCalls.Valid) drawCalls.Dispose();
        if (triangles.Valid) triangles.Dispose();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;
        liveFps = Mathf.Lerp(liveFps, 1f / dt, 0.1f);

        if (!testing) return;

        if (warmTimer > 0f) { warmTimer -= dt; return; }

        frameTimes.Add(dt);
        testTimer += dt;

        if (drawCalls.Valid && triangles.Valid && (drawCalls.LastValue > 0 || triangles.LastValue > 0))
        {
            countersAvailable = true;
            long d = drawCalls.LastValue, t = triangles.LastValue;
            sumDraw += d; sumTri += t; samples++;
            if (d > maxDraw) maxDraw = d;
            if (t > maxTri) maxTri = t;
        }

        if (testTimer >= testSeconds) Finish();
    }

    void StartTest()
    {
        frameTimes.Clear();
        testing = true;
        hasResult = false;
        testTimer = 0f;
        warmTimer = warmupSeconds;
        sumDraw = sumTri = 0; samples = 0; maxDraw = maxTri = 0;
        countersAvailable = false;
    }

    void Finish()
    {
        testing = false;
        hasResult = true;

        int n = frameTimes.Count;
        avgFps = n / testTimer;

        float worst = 0f;
        foreach (var f in frameTimes) if (f > worst) worst = f;
        minFps = 1f / worst;

        // 1% low: average of the slowest 1% of frames
        frameTimes.Sort();
        int k = Mathf.Max(1, n / 100);
        float sum = 0f;
        for (int i = n - k; i < n; i++) sum += frameTimes[i];
        lowFps = 1f / (sum / k);

        avgDraw = samples > 0 ? sumDraw / samples : 0;
        avgTri = samples > 0 ? sumTri / samples : 0;
        peakDraw = maxDraw; peakTri = maxTri;

        Debug.Log("PERF avgFps=" + avgFps.ToString("F1") + " minFps=" + minFps.ToString("F1")
                  + " low1%=" + lowFps.ToString("F1") + " drawAvg=" + avgDraw.ToString("F0")
                  + " triAvg=" + avgTri.ToString("F0"));
    }

    void OnGUI()
    {
        if (!visible) return;

        float u = Mathf.Min(Screen.width, Screen.height) * 0.032f;
        if (label == null) label = new GUIStyle(GUI.skin.label);
        if (button == null) button = new GUIStyle(GUI.skin.button);
        label.fontSize = Mathf.RoundToInt(u);
        label.normal.textColor = Color.yellow;
        button.fontSize = Mathf.RoundToInt(u);

        float x = u * 0.5f, y = u * 0.5f;
        if (GUI.Button(new Rect(x, y, u * 7f, u * 2.2f), testing ? "TESTING..." : "PERF TEST") && !testing)
            StartTest();
        y += u * 2.4f;

        string text = "FPS " + liveFps.ToString("F0")
                      + "\n" + SystemInfo.deviceModel
                      + "\n" + Screen.width + "x" + Screen.height;

        if (testing)
            text += "\nTest: " + testTimer.ToString("F0") + " / " + testSeconds.ToString("F0") + " s";

        if (hasResult)
        {
            text += "\n--- RESULT ---"
                  + "\nAvg FPS " + avgFps.ToString("F1")
                  + "\nMin FPS " + minFps.ToString("F1")
                  + "\n1% low  " + lowFps.ToString("F1");
            text += countersAvailable
                ? "\nDraw calls " + avgDraw.ToString("F0") + " (peak " + peakDraw + ")"
                  + "\nTriangles " + avgTri.ToString("F0") + " (peak " + peakTri + ")"
                : "\nDraw calls / triangles: n/a (use a Development Build)";
        }

        GUI.Label(new Rect(x, y, u * 18f, u * 14f), text, label);
    }
}