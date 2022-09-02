using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices.WindowsRuntime;
#if !UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using Windows.Perception;
using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;
using Windows.Perception.People;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.OpenXR;
#endif

public class EyeGazeLineCursor : MonoBehaviour
{
    private GameObject line;
    private LineRenderer renderer;
    public GazeSocket gazeSender;
    private GazeData gazeData;
    public float lineWidth = 0.005f;
    private bool showGazeCursor = false;
#if !UNITY_EDITOR
    SpatialCoordinateSystem worldCoordinateSystem = PerceptionInterop.GetSceneCoordinateSystem(UnityEngine.Pose.identity) as SpatialCoordinateSystem;
#endif

    // Start is called before the first frame update
    void Start()
    {
        gazeSender = GameObject.Find("GazeSender").GetComponent<GazeSocket>();
        line = new GameObject();
        renderer = line.AddComponent<LineRenderer>();
        gazeData = null;
    }

    // Update is called once per frame
    void Update()
    {
#if !UNITY_EDITOR
        if (showGazeCursor)
        {
            bool isSuccess = gazeSender.getQueue().TryDequeue(out gazeData);
            if (gazeData != null)
            {
                var cameraCoordinateSystem = gazeData.cameraCoordinateSystem;
                System.Numerics.Vector3 origin = gazeData.gazeOrigin;
                System.Numerics.Vector3 adjustedOrigin = new System.Numerics.Vector3(0,-0.05f,0); //origin + new System.Numerics.Vector3(0.05f, 0.0f, 0.25f);
                System.Numerics.Vector3 direction = gazeData.gazeDirection;

                var transformMatrix = cameraCoordinateSystem.TryGetTransformTo(worldCoordinateSystem).Value;
                origin = System.Numerics.Vector3.Transform(origin, transformMatrix);
                direction = System.Numerics.Vector3.Transform(direction, transformMatrix);
                adjustedOrigin = System.Numerics.Vector3.Transform(adjustedOrigin, transformMatrix);

                UnityEngine.Vector3 gazeOrigin = new UnityEngine.Vector3(origin.X, origin.Y, -origin.Z);
                UnityEngine.Vector3 gazeDirection = new UnityEngine.Vector3(direction.X, direction.Y, -direction.Z);
                UnityEngine.Vector3 adjustedGazeOrigin = new UnityEngine.Vector3(adjustedOrigin.X, adjustedOrigin.Y, -adjustedOrigin.Z);

                UnityEngine.Vector3 endPoint = gazeOrigin + 2.0f * gazeDirection;
                UnityEngine.Vector3 startPoint = gazeOrigin;

                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.startWidth = lineWidth;
                renderer.endWidth = lineWidth;
                renderer.startColor = Color.red;
                renderer.endColor = Color.red;
                renderer.positionCount = 2;
                renderer.SetPositions(new Vector3[] { startPoint, endPoint });
            }
        }
#endif
    }

    public void ToogleGazeCursor()
    {
        showGazeCursor = !showGazeCursor;
    }
}
