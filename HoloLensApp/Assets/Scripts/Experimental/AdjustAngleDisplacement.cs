using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AdjustAngleDisplacement : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Assign DialogSmall_192x96.prefab")]
    private GameObject dialogPrefabSmall;
    public GameObject DialogPrefabSmall
    {
        get => dialogPrefabSmall;
        set => dialogPrefabSmall = value;
    }

    [SerializeField]
    public GameObject countDownTextBox;
    private TextMeshPro startingText;

    private Dialog startDialog;
    private bool startDialogAccepted;

    private Dialog endDialog;
    private bool endDialogAccepted;

    [SerializeField]
    private GameObject gazeSender;
    public GameObject GazeSender
    {
        get => gazeSender;
        set => gazeSender = value;
    }

    [SerializeField]
    private GameObject videoSender;
    public GameObject VideoSender
    {
        get => videoSender;
        set => videoSender = value;
    }

    [SerializeField]
    private GameObject audioSender;
    public GameObject AudioSender
    {
        get => audioSender;
        set => audioSender = value;
    }

    private GazeSocketCalibration gazeSocketCalibration;
    private VideoSocket videoSocket;
    private AudioSocket audioSocket;

    private float gazeVectorAngleAdjustment;
    private float end = 3.0f;
    private float start = 0.0f;
    private float step = 0.1f;
    private float timeBetweenSteps = 0.33f;

    // Start is called before the first frame update
    void Start()
    {
        gazeVectorAngleAdjustment = start;
        startDialogAccepted = false;

        gazeSocketCalibration = GazeSender.GetComponent<GazeSocketCalibration>();
        videoSocket = VideoSender.GetComponent<VideoSocket>();
        audioSocket = AudioSender.GetComponent<AudioSocket>();

        startingText = countDownTextBox.transform.Find("Text").GetComponent<TextMeshPro>();
        StartCoroutine(DisplayStartUpDialog());
    }

    private void StartCalibrationRoutine()
    {
        StartCoroutine(RunCalibration(timeBetweenSteps));
    }

    private IEnumerator DisplayStartUpDialog()
    {
        System.Diagnostics.Debug.WriteLine("DIALOG OPENED");
        string text = "Press OK to start calibrating. Look at a specific point on a small object that is 3 to 4 m away." +
            "Countdown will begin, calibration lasts for about 10 seconds. It is done 3 times.";

        startDialog = Dialog.Open(DialogPrefabSmall, DialogButtonType.OK, "START CALIBRATION", text, true);

        if (startDialog != null)
        {
            startDialog.OnClosed += StartDialogAccepted;
        }
        yield return StartCoroutine(WaitForStartDialogToBeAccepted());
    }

    private IEnumerator DisplayFinishDialog()
    {
        string text = "Calibration is done. Check server.";

        endDialog = Dialog.Open(DialogPrefabSmall, DialogButtonType.OK, "END CALIBRATION", text, true);

        if (endDialog != null)
        {
            endDialog.OnClosed += EndDialogAccepted;
        }
        yield return StartCoroutine(WaitForFinishDialogToBeAccepted());
    }

    private void StartDialogAccepted(DialogResult obj)
    {

        StartCalibrationRoutine();
    }

    private void EndDialogAccepted(DialogResult obj)
    {
        Application.Quit();
    }

    private IEnumerator RunCalibration(float waitSeconds)
    { 
        yield return CountDown(5);
        System.Diagnostics.Debug.WriteLine("SETTING ACTIVE");
        VideoSender.SetActive(true);
        GazeSender.SetActive(true);
        AudioSender.SetActive(true);

        System.Diagnostics.Debug.WriteLine(VideoSender.activeSelf);
        while (gazeVectorAngleAdjustment <= end)
        {
            gazeVectorAngleAdjustment += step;
            gazeSocketCalibration.gazeVectorAngleAdjustment = gazeVectorAngleAdjustment;
            yield return new WaitForSeconds(waitSeconds);
        }
        gazeVectorAngleAdjustment = start;

        gazeSocketCalibration.StopSendingData();
        videoSocket.StopSendingData();
        audioSocket.StopSendingData();

        System.Diagnostics.Debug.WriteLine("BEFORE COUNTDOWN");
        yield return CountDown(5);
        System.Diagnostics.Debug.WriteLine("AFTER COUNTDOWN");

        gazeSocketCalibration.ResumeSendingData();
        videoSocket.ResumeSendingData();
        audioSocket.ResumeSendingData();

        while (gazeVectorAngleAdjustment <= end)
        {
            gazeVectorAngleAdjustment += step;
            gazeSocketCalibration.gazeVectorAngleAdjustment = gazeVectorAngleAdjustment;
            yield return new WaitForSeconds(waitSeconds);
        }
        gazeSocketCalibration.StopSendingData();
        videoSocket.StopSendingData();
        audioSocket.StopSendingData();
        gazeVectorAngleAdjustment = start;

        yield return CountDown(5);

        gazeSocketCalibration.ResumeSendingData();
        videoSocket.ResumeSendingData();
        audioSocket.ResumeSendingData();

        while (gazeVectorAngleAdjustment <= end)
        {
            gazeVectorAngleAdjustment += step;
            gazeSocketCalibration.gazeVectorAngleAdjustment = gazeVectorAngleAdjustment;
            yield return new WaitForSeconds(waitSeconds);
        }
        gazeVectorAngleAdjustment = start;
        gazeSocketCalibration.StopSendingData();
        videoSocket.StopSendingData();
        audioSocket.StopSendingData();

        StartCoroutine(DisplayFinishDialog());
    }

    public IEnumerator WaitForStartDialogToBeAccepted()
    {
        yield return new WaitUntil(() => startDialogAccepted);
        startDialogAccepted = false;
    }

    public IEnumerator WaitForFinishDialogToBeAccepted()
    {
        yield return new WaitUntil(() => endDialogAccepted);
        endDialogAccepted = false;
    }

    private IEnumerator CountDown(int seconds)
    {
        countDownTextBox.SetActive(true);
        yield return RunTimer(startingText, seconds);
    }

    private IEnumerator RunTimer(TextMeshPro displayText, int seconds)
    {
        Debug.Log("COUNTING COROUTINE");
        while (seconds >= 1)
        {
            Debug.Log("IN LOOP");
            displayText.text = string.Format("Starting in {0} seconds...", seconds);
            yield return new WaitForSeconds(1);
            seconds--;
        }
        countDownTextBox.SetActive(false);
    }
}
