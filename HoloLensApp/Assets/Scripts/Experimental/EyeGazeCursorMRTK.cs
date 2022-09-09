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
    //private GameObject visualsRoot;
    //private GameObject tetherLine;
    //private SimpleLineDataProvider dataProvider;

    private bool showGazeCursor = false;

    public Transform TetherEndPoint => tetherEndPoint;

    [SerializeField]
    private Transform visualsRoot = null;

    [SerializeField]
    private Transform tetherEndPoint = null;

    [SerializeField]
    private BaseMixedRealityLineDataProvider tetherLine = null;

    private float gazeVectorAngleAdjustment;

    // Start is called before the first frame update
    void Start()
    {
        //grabPointer = gameObject.transform.GetChild(0).gameObject;
        //visualsRoot = gameObject.transform.GetChild(0).gameObject;
        //tetherLine = visualsRoot.transform.GetChild(0).gameObject;
        //dataProvider = tetherLine.GetComponent<SimpleLineDataProvider>();

        MixedRealityPlayspace.AddChild(visualsRoot.transform);
        visualsRoot.gameObject.name = $"{gameObject.name}_NearTetherVisualsRoot";
        gazeVectorAngleAdjustment = 0.0f;
        visualsRoot.gameObject.SetActive(showGazeCursor);
    }

    // Update is called once per frame
    void Update()
    {
        /*
        var eyeGazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
        System.Diagnostics.Debug.WriteLine(eyeGazeProvider.GazeDirection.normalized);
        if (eyeGazeProvider != null)
        {
            tetherLine.transform.position = eyeGazeProvider.GazeOrigin;
            Vector3 endPoint = eyeGazeProvider.GazeOrigin + eyeGazeProvider.GazeDirection.normalized;
            dataProvider.EndPoint = new MixedRealityPose(endPoint, Quaternion.identity);
        }
        */
        if (showGazeCursor)
        {
            var eyeGazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
            Vector3 endPoint = eyeGazeProvider.GazeOrigin + eyeGazeProvider.GazeDirection.normalized;
            Vector3 startPoint = eyeGazeProvider.GazeOrigin;
            tetherLine.FirstPoint = startPoint;
            tetherLine.LastPoint = endPoint;

            tetherEndPoint.gameObject.SetActive(true);
            tetherEndPoint.position = endPoint;
        }
    }

    public void ToggleGazeCursor()
    {
        showGazeCursor = !showGazeCursor;
        visualsRoot.gameObject.SetActive(showGazeCursor);
        tetherLine.enabled = showGazeCursor;
    }
}
