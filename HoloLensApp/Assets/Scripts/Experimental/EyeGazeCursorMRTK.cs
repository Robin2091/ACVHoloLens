using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit;
using System.Runtime.InteropServices.WindowsRuntime;

public class EyeGazeCursorMRTK : MonoBehaviour
{
    //private GameObject grabPointer;
    private GameObject visualsRoot;
    private GameObject tetherLine;
    private SimpleLineDataProvider dataProvider;
    // Start is called before the first frame update
    void Start()
    {
        //grabPointer = gameObject.transform.GetChild(0).gameObject;
        visualsRoot = gameObject.transform.GetChild(0).gameObject;
        tetherLine = visualsRoot.transform.GetChild(0).gameObject;
        dataProvider = tetherLine.GetComponent<SimpleLineDataProvider>();
    }

    // Update is called once per frame
    void Update()
    {
        var eyeGazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
        System.Diagnostics.Debug.WriteLine(eyeGazeProvider.GazeDirection.normalized);
        if (eyeGazeProvider != null)
        {
            tetherLine.transform.position = eyeGazeProvider.GazeOrigin;
            Vector3 endPoint = eyeGazeProvider.GazeOrigin + eyeGazeProvider.GazeDirection.normalized;
            dataProvider.EndPoint = new MixedRealityPose(endPoint, Quaternion.identity);
        }
    }
}
