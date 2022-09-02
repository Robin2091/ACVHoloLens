using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeDialog : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Assign DialogSmall_192x96.prefab")]
    private GameObject dialogPrefabSmall;
    public GameObject DialogPrefabSmall
    {
        get => dialogPrefabSmall;
        set => dialogPrefabSmall = value;
    }
    private Dialog addressDialog;
    private bool dialogAccepted;
    private bool alreadyRunning;

    // Start is called before the first frame update
    void Start()
    {
        GazeSocket.OnGazeDataStopped += ShowDialog;
    }

    public void ShowDialog()
    {
        if (!alreadyRunning)
        {
            StartCoroutine(ShowDialogBox());
        }
        
    }

    public IEnumerator ShowDialogBox()
    {
        alreadyRunning = true;
        addressDialog = Dialog.Open(DialogPrefabSmall, DialogButtonType.OK, "EYE GAZE DATA NOT BEING SENT",
            "EYE GAZE DATA IS NOT BEING SENT. PLEASE EXIT APP AND RESTART THE SERVER.", true);

        if (addressDialog != null)
        {
            addressDialog.OnClosed += OnClosedDialogEvent;
        }
        yield return StartCoroutine(WaitForDialogToBeAccepted());
    }

    public IEnumerator WaitForDialogToBeAccepted()
    {
        yield return new WaitUntil(() => dialogAccepted);
        dialogAccepted = false;
        alreadyRunning = false;
    }

    public void OnClosedDialogEvent(DialogResult obj)
    {
        dialogAccepted = true;
    }
}
