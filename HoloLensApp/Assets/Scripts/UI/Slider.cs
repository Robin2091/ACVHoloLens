using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slider : MonoBehaviour
{
    public GameObject slider;
    private PinchSlider sliderScript;
    public GazeSocket gazeSocket;
    private Camera mainCamera; //Main camera represents the position of the headset
    // Start is called before the first frame update
    void Start()
    {
        //slider = GameObject.Find("PinchSliderColorRed");
        slider.SetActive(false);
        sliderScript = slider.GetComponent<PinchSlider>();
        sliderScript.SliderValue = gazeSocket.gazeVectorAngleAdjustment;
        mainCamera = Camera.main;
    }

    public void setValue()
    {
        int oldRange = 1;
        int newRange = 6;
        float value = (((sliderScript.SliderValue - 0) * newRange) / oldRange) + -3.0f;
        gazeSocket.gazeVectorAngleAdjustment = value;
    }

    public void ToggleSlider()
    {
        slider.SetActive(!slider.activeSelf);
        if (slider.activeSelf)
        {
            slider.transform.position = mainCamera.transform.position + mainCamera.transform.forward * 0.75f;
            var n = mainCamera.transform.position - slider.transform.position;
            slider.transform.LookAt(mainCamera.transform);
            slider.transform.RotateAround(slider.transform.position, transform.up, 180f);
        }
        
    }

    public void HideSlider()
    {
        slider.SetActive(false);
    }
}
