using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using Microsoft.MixedReality.Toolkit.Input;

public class AudioSocket : MonoBehaviour
{
    private bool collectAndSendData = true;
    private ClientSocket socket;
#if !UNITY_EDITOR
    private AudioClip audioClip;
    private readonly int clipLength = 20;
    private int audioFrequency;
    private int lastSample;
    
    // Start is called before the first frame update
    async void Start()
    {
        socket = new ClientSocket(SocketData.Address, SocketData.AudioPort, SocketData.BufferSize);
        await socket.ConnectToServer();
        System.Diagnostics.Debug.WriteLine("IN START");
        lastSample = 0;
        audioFrequency = 44100;
        startRecording();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        sendAudioData();
    }

    public void startRecording()
    {
        audioClip = Microphone.Start(null, true, clipLength, audioFrequency);
        byte[] frameTimeStamp = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss.fff").PadLeft(25));
        socket.SendData(frameTimeStamp);
    }


    public void sendAudioData()
    {
        if (collectAndSendData)
        {
            if (audioClip != null)
            {
                int pos = Microphone.GetPosition(null);
                int diff = pos - lastSample;

                if (diff > 0)
                {
                    float[] samples = new float[diff * audioClip.channels];
                    audioClip.GetData(samples, lastSample);
                    byte[] audioData = new byte[samples.Length * sizeof(float)];
                    Buffer.BlockCopy(samples, 0, audioData, 0, audioData.Length);
                    socket.SendData(audioData);
                }
                lastSample = pos;
            }
        }
    }
#endif
    public void StopSendingData()
    {
        collectAndSendData = false;
    }

    public void ResumeSendingData()
    {
        collectAndSendData = true;
    }

    public bool IsSendingData()
    {
        return collectAndSendData;
    }

    public void CloseConnection()
    {
#if !UNITY_EDITOR
        Microphone.End(null);
        socket.CloseConnection();
#endif
    }
}
