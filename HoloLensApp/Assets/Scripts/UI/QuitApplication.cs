using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitApplication : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Assign DialogSmall_192x96.prefab")]
    private GameObject dialogPrefabSmall;
    public GameObject DialogPrefabSmall
    {
        get => dialogPrefabSmall;
        set => dialogPrefabSmall = value;
    }
    public GazeSocket gazeSender;
    public VideoSocket videoSender;
    public AudioSocket audioSender;

    private Dialog exitDialog;
    private bool dialogAccepted;

    public void ExitApplication()
    {
        gazeSender.StopSendingData();
        videoSender.StopSendingData();
        audioSender.StopSendingData();

        System.Diagnostics.Debug.WriteLine("APPLICATION EXIT COROUTINE STARTED");
        StartCoroutine(Confirm());
    }

    public IEnumerator Confirm()
    {
        exitDialog = Dialog.Open(DialogPrefabSmall, DialogButtonType.Yes | DialogButtonType.No, 
            "EXIT APPLICATION?","Are you sure you want to exit?", true);

        if (exitDialog != null)
        {
            exitDialog.OnClosed += DialogAccepted;
        }
        yield return StartCoroutine(WaitForDialogToBeAccepted());

    }

    public void DialogAccepted(DialogResult obj)
    {
        dialogAccepted = true;
        if (obj.Result == DialogButtonType.Yes)
        {
            //gazeSender.CloseConnection();
            //audioSender.CloseConnection();
            //videoSender.CloseConnection();
            Application.Quit();
        }
        else
        {
            GameObject.Find("VideoSender").GetComponent<VideoSocket>().ResumeSendingData();
            GameObject.Find("AudioSender").GetComponent<AudioSocket>().ResumeSendingData();
            GameObject.Find("GazeSender").GetComponent<GazeSocket>().ResumeSendingData();
        }
    }

    public IEnumerator WaitForDialogToBeAccepted()
    {
        yield return new WaitUntil(() => dialogAccepted);
        dialogAccepted = false;
    }
}
