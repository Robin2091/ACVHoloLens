using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_EDITOR
using System;
using Windows.Perception;
using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;
using Windows.Perception.People;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.OpenXR;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

public class EyeGazeCursor : MonoBehaviour
{
    public GazeSocket gazeSender;
    private GazeData gazeData;
    //private GameObject visualsRoot;
    //private BaseMixedRealityLineDataProvider tetherLine;
    //private SimpleLineDataProvider dataProvider;
    private bool showGazeCursor = false;
    
    public Transform TetherEndPoint => tetherEndPoint;

    [SerializeField]
    private Transform visualsRoot = null;

    [SerializeField]
    private Transform tetherEndPoint = null;

    [SerializeField]
    private BaseMixedRealityLineDataProvider tetherLine = null;
    

#if !UNITY_EDITOR
    SpatialCoordinateSystem worldCoordinateSystem = PerceptionInterop.GetSceneCoordinateSystem(UnityEngine.Pose.identity) as SpatialCoordinateSystem;
#endif
    void Start()
    {
        MixedRealityPlayspace.AddChild(visualsRoot.transform);
        visualsRoot.gameObject.name = $"{gameObject.name}_NearTetherVisualsRoot";
        //gazeSender = GameObject.Find("GazeSender").GetComponent<GazeSocket>();
        gazeData = null;
        //visualsRoot = gameObject.transform.GetChild(0).gameObject;
        //tetherLine = visualsRoot.transform.GetChild(0).gameObject.GetComponent<BaseMixedRealityLineDataProvider>();
        //dataProvider = tetherLine.GetComponent<SimpleLineDataProvider>();
        //visualsRoot.SetActive(false);
    }

    private void Update()
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
                UnityEngine.Vector3 startPoint = adjustedGazeOrigin;
                System.Diagnostics.Debug.WriteLine(startPoint);
                System.Diagnostics.Debug.WriteLine(endPoint);
                
                tetherLine.FirstPoint = startPoint; //new MixedRealityPose(startPoint);
                tetherLine.LastPoint = endPoint; //new MixedRealityPose(endPoint);
                tetherEndPoint.gameObject.SetActive(true);
                tetherEndPoint.position = endPoint;
                
                //tetherLine.transform.position = gazeOrigin;
                //dataProvider.EndPoint = new MixedRealityPose(endPoint);
            }
        }
#endif
    }

    public void ToogleGazeCursor()
    {
        System.Diagnostics.Debug.WriteLine("TOGGLING GAZE"); 
        showGazeCursor = !showGazeCursor;
        System.Diagnostics.Debug.WriteLine(showGazeCursor);
        gazeSender.SendToQueue = showGazeCursor;
        //visualsRoot.SetActive(showGazeCursor);
        visualsRoot.gameObject.SetActive(showGazeCursor);
    }
}
