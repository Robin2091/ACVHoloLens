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

public class EyeGazeCylinderCursor : MonoBehaviour
{
    public GameObject cylinder;
    private Vector3 defaultOrientation = new Vector3(0, 1, 0);
    public GazeSocket gazeSender;
    private GazeData gazeData;
    private bool showGazeCursor = false;
#if !UNITY_EDITOR
    SpatialCoordinateSystem worldCoordinateSystem = PerceptionInterop.GetSceneCoordinateSystem(UnityEngine.Pose.identity) as SpatialCoordinateSystem;
#endif

    // Start is called before the first frame update
    void Start()
    {
        cylinder = GameObject.Find("Cursor");
        gazeSender = GameObject.Find("GazeSender").GetComponent<GazeSocket>();
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
                //PlaceCylinder(cylinder, startPoint, endPoint, 0.0025f);
                //PlaceCylinder(cylinder, startPoint , endPoint);
            }
        }
#endif
    }

    private void PlaceCylinder(GameObject cylinder, Vector3 start, Vector3 end)
    {
        Vector3 rotation = Vector3.Normalize(end - start);
        Vector3 rotAxisV = rotation + defaultOrientation;
        rotAxisV = Vector3.Normalize(rotAxisV);
        float dist = Vector3.Distance(end, start);
        cylinder.transform.rotation = new Quaternion(rotAxisV.x, rotAxisV.y, rotAxisV.z, 0);
        cylinder.transform.position = (start + end) / 2.0F;
        cylinder.transform.localScale = new Vector3(0.0025f, dist / 2, 0.0025f);
    }

    private void PlaceCylinder(GameObject cylinder, Vector3 start, Vector3 end, float width)
    {
        var offset = end - start;
        var scale = new Vector3(width, offset.magnitude / 2.0f, width);
        var position = start + (offset / 2.0f);
        cylinder.transform.up = offset;
        cylinder.transform.localScale = scale;
    }

    public void ToogleGazeCursor()
    {
        showGazeCursor = !showGazeCursor;
    }
}
