using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System.Net;
using System.Linq;
using System;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class KeyBoard : MonoBehaviour
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
    private MixedRealityKeyboardPreview mixedRealityKeyboardPreview = null;

    [SerializeField]
    public GameObject TextBoxStarting;

    private MixedRealityKeyboard keyboard;
    private Dialog failDialog;
    private bool textEntered;
    private bool dialogAccepted;

    // Start is called before the first frame update
    void Start()
    {
        keyboard = gameObject.GetComponent<MixedRealityKeyboard>();
        // Initially hide the preview.
        if (mixedRealityKeyboardPreview != null)
        {
            mixedRealityKeyboardPreview.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (keyboard.Visible)
        {
            if (mixedRealityKeyboardPreview != null)
            {
                mixedRealityKeyboardPreview.Text = keyboard.Text;
                mixedRealityKeyboardPreview.CaretIndex = keyboard.CaretIndex;
            }
        }
        else
        {
            if (mixedRealityKeyboardPreview != null)
            {
                mixedRealityKeyboardPreview.Text = string.Empty;
                mixedRealityKeyboardPreview.CaretIndex = 0;
            }
        }
    }

    public IEnumerator GetValidIPAddress()
    {
        bool isValidAddressEntered = false;
        while (!isValidAddressEntered)
        {
            if (!keyboard.Visible)
            {
                OpenSystemKeyboard();
            }
            yield return StartCoroutine(WaitForTextToBeEntered());

            if (IsValidIPAddress(keyboard.Text))
            {
                System.Diagnostics.Debug.WriteLine(keyboard.Text);
                SocketData.Address = keyboard.Text;
                System.Diagnostics.Debug.WriteLine(SocketData.Address);
                TextBoxStarting.SetActive(true);
                TextMeshPro startingText = TextBoxStarting.transform.Find("Text").GetComponent<TextMeshPro>();
                isValidAddressEntered = true;
                StartCoroutine(RunTimer(startingText));
            }
            else
            {
                failDialog = Dialog.Open(DialogPrefabSmall, DialogButtonType.OK, "Invalid IP Address",
                                        string.Format("The IP Address you entered is {0}. This is an invalid address, please try again.", keyboard.Text), true);

                if (failDialog != null)
                {
                    failDialog.OnClosed += DialogAccepted;
                }
                yield return StartCoroutine(WaitForDialogToBeAccepted());
            }
        }
    }

    public void ShowPreviewText()
    {
        if (mixedRealityKeyboardPreview != null)
        {
            mixedRealityKeyboardPreview.gameObject.SetActive(true);
        }
    }

    public void HidePreviewText()
    {
        if (mixedRealityKeyboardPreview != null)
        {
            mixedRealityKeyboardPreview.gameObject.SetActive(false);
        }
    }

    public void StartKeyBoard()
    {
        StartCoroutine(GetValidIPAddress());
    }

    public void TextEntered()
    {
        textEntered = true;
    }

    public void DialogAccepted(DialogResult obj)
    {
        dialogAccepted = true;
    }

    public IEnumerator WaitForTextToBeEntered()
    {
        yield return new WaitUntil(() => textEntered);
        textEntered = false;
    }
    
    public void OpenSystemKeyboard()
    {
        keyboard.ShowKeyboard("", false);
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

    public IEnumerator WaitForDialogToBeAccepted()
    {
        yield return new WaitUntil(() => dialogAccepted);
        dialogAccepted = false;
    }

    public static bool IsValidIPAddress(string IpAddress)
    {
        try
        {
            IPAddress IP;
            if (IpAddress.Count(c => c == '.') == 3)
            {
                bool flag = IPAddress.TryParse(IpAddress, out IP);
                if (flag)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        catch (Exception) { return false; }
    }
}
