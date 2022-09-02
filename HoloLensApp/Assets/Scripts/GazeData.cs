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

public class GazeData 
{
#if !UNITY_EDITOR
    public SpatialCoordinateSystem cameraCoordinateSystem;

    public System.Numerics.Vector3 gazeOrigin;

    public System.Numerics.Vector3 gazeDirection;

    public GazeData(SpatialCoordinateSystem camera, System.Numerics.Vector3 origin, System.Numerics.Vector3 direction)
    {
        cameraCoordinateSystem = camera;
        gazeOrigin = origin;
        gazeDirection = direction;
    }
#endif
}
