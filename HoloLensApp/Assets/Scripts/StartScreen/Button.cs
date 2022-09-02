using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;


public class Button : MonoBehaviour
{
    [SerializeField]
    public GameObject StartButton;

    [SerializeField]
    [Tooltip("Assign DialogSmall_192x96.prefab")]
    private GameObject dialogPrefabSmall;
    public GameObject DialogPrefabSmall
    {
        get => dialogPrefabSmall;
        set => dialogPrefabSmall = value;
    }

    [SerializeField]
    public GameObject TextBoxStarting;

    private Dialog addressDialog;
    public void Button_Pressed()
    {
        StartButton.SetActive(false);
        OpenChoiceDialogSmall();
    }

    public void OpenChoiceDialogSmall()
    {
         addressDialog = Dialog.Open(DialogPrefabSmall, DialogButtonType.Yes | DialogButtonType.No, "Change IP Address",
            string.Format("The current IP Address is {0}. Would you like to change this?", SocketData.Address), true);

        if (addressDialog != null)
        {
            addressDialog.OnClosed += OnClosedDialogEvent;
        }
    }

    private void OnClosedDialogEvent(DialogResult obj)
    {
        if (obj.Result == DialogButtonType.Yes)
        {
            gameObject.GetComponent<KeyBoard>().StartKeyBoard();
            
        } else
        {
            TextBoxStarting.SetActive(true);
            TextMeshPro startingText = TextBoxStarting.transform.Find("Text").GetComponent <TextMeshPro>();
            StartCoroutine(RunTimer(startingText));
        }

    }

    private IEnumerator RunTimer(TextMeshPro displayText)
    {
        int seconds = 3;
        while (seconds >= 1)
        {
            displayText.text = string.Format("Starting in {0} seconds...", seconds);
            yield return new WaitForSeconds(1);
            seconds--;
        }
        TextBoxStarting.SetActive(false);
        SceneManager.LoadScene("HoloLens2");
    }

}
