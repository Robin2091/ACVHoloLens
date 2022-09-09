using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_EDITOR
using Windows.Perception;
using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;
using Windows.Perception.People;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.OpenXR;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

public class EyeGazeCursor2 : MonoBehaviour
{
    public GazeSocket gazeSocket;

    private bool showGazeCursor = false;

    public Transform TetherEndPoint => tetherEndPoint;

    [SerializeField]
    private Transform visualsRoot = null;

    [SerializeField]
    private Transform tetherEndPoint = null;

    [SerializeField]
    private BaseMixedRealityLineDataProvider tetherLine = null;


#if !UNITY_EDITOR
    private SpatialCoordinateSystem worldCoordinateSystem = PerceptionInterop.GetSceneCoordinateSystem(UnityEngine.Pose.identity) as SpatialCoordinateSystem;
#endif

    // Start is called before the first frame update
    void Start()
    {
        MixedRealityPlayspace.AddChild(visualsRoot.transform);
        visualsRoot.gameObject.name = $"{gameObject.name}_NearTetherVisualsRoot";
        visualsRoot.gameObject.SetActive(showGazeCursor);
    }

    // Update is called once per frame
    private System.Numerics.Vector3 prevDirection;
    private System.Numerics.Vector3 prevOrigin;
    void Update()
    {
#if !UNITY_EDITOR
        if (showGazeCursor)
        {
            SpatialPointerPose pointerPose = SpatialPointerPose.TryGetAtTimestamp(worldCoordinateSystem, 
                                                                                          PerceptionTimestampHelper.FromHistoricalTargetTime(DateTimeOffset.Now));
            var eyes = pointerPose.Eyes;

            if (eyes != null)
            {
                var eyeGaze = eyes.Gaze;

                if (eyeGaze != null)
                {
                    var eyeGazeDirection = eyeGaze.Value.Direction;
                    var eyeGazeOrigin = eyeGaze.Value.Origin;

                    var adjustedEyeGazeDirectionRay = ChangeVectorAngle(eyeGazeDirection, gazeSocket.gazeVectorAngleAdjustment);

                    UnityEngine.Vector3 gazeOrigin = new UnityEngine.Vector3(eyeGazeOrigin.X, eyeGazeOrigin.Y, -eyeGazeOrigin.Z);
                    UnityEngine.Vector3 gazeDirection = new UnityEngine.Vector3(adjustedEyeGazeDirectionRay.X, adjustedEyeGazeDirectionRay.Y, -adjustedEyeGazeDirectionRay.Z);

                    UnityEngine.Vector3 endPoint = gazeOrigin + 2.0f * gazeDirection;
                    UnityEngine.Vector3 startPoint = gazeOrigin;

                    tetherLine.FirstPoint = startPoint; 
                    tetherLine.LastPoint = endPoint;
                    
                    tetherEndPoint.gameObject.SetActive(true);
                    tetherEndPoint.position = endPoint;

                    prevDirection = eyeGazeDirection;
                    prevOrigin = eyeGazeOrigin;
                }
                else 
                {
                    var adjustedEyeGazeDirectionRay = ChangeVectorAngle(prevDirection, gazeSocket.gazeVectorAngleAdjustment);

                    UnityEngine.Vector3 gazeOrigin = new UnityEngine.Vector3(prevOrigin.X, prevOrigin.Y, -prevOrigin.Z);
                    UnityEngine.Vector3 gazeDirection = new UnityEngine.Vector3(adjustedEyeGazeDirectionRay.X, adjustedEyeGazeDirectionRay.Y, -adjustedEyeGazeDirectionRay.Z);

                    UnityEngine.Vector3 endPoint = gazeOrigin + 2.0f * gazeDirection;
                    UnityEngine.Vector3 startPoint = gazeOrigin;

                    tetherLine.FirstPoint = startPoint; 
                    tetherLine.LastPoint = endPoint;
                    
                    tetherEndPoint.gameObject.SetActive(true);
                    tetherEndPoint.position = endPoint;
                }
            }
            else
            {
                var adjustedEyeGazeDirectionRay = ChangeVectorAngle(prevDirection, gazeSocket.gazeVectorAngleAdjustment);

                UnityEngine.Vector3 gazeOrigin = new UnityEngine.Vector3(prevOrigin.X, prevOrigin.Y, -prevOrigin.Z);
                UnityEngine.Vector3 gazeDirection = new UnityEngine.Vector3(adjustedEyeGazeDirectionRay.X, adjustedEyeGazeDirectionRay.Y, -adjustedEyeGazeDirectionRay.Z);

                UnityEngine.Vector3 endPoint = gazeOrigin + 2.0f * gazeDirection;
                UnityEngine.Vector3 startPoint = gazeOrigin;

                tetherLine.FirstPoint = startPoint; 
                tetherLine.LastPoint = endPoint;
                    
                tetherEndPoint.gameObject.SetActive(true);
                tetherEndPoint.position = endPoint;
            }
        }
#endif
    }

    public void ToggleGazeCursor()
    {
        showGazeCursor = !showGazeCursor;
        visualsRoot.gameObject.SetActive(showGazeCursor);
        tetherLine.enabled = showGazeCursor;
    }

    private System.Numerics.Vector3 ChangeVectorAngle(System.Numerics.Vector3 vector, double angleDegrees)
    {
        if (angleDegrees == 0.0)
        {
            return vector;
        }

        var normal = System.Numerics.Vector3.Cross(vector, new System.Numerics.Vector3(0f, 1f, 0f));
        var unitNormal = normal / normal.Length();
        var ax = System.Numerics.Vector3.Cross(unitNormal, vector);
        var angleRadians = angleDegrees * (Math.PI / 180.0);
        return System.Numerics.Vector3.Multiply(vector, (float)Math.Cos(angleRadians)) + System.Numerics.Vector3.Multiply(ax, (float)Math.Sin(angleRadians));
    }
}
