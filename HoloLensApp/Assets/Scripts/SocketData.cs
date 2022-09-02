using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SocketData 
{
    /*
    private string _address;
    private string _gazePort;
    private string _audioPort;
    private string _videoPort;
    private int _headerSize;
    private int _bufferSize;

    public SocketData(string address, string gazePort, string audioPort, string videoPort, int headerSize, int bufferSize)
    {
        _address = address;
        _gazePort = gazePort;
        _audioPort = audioPort;
        _videoPort = videoPort;
        _headerSize = headerSize;
        _bufferSize = bufferSize;
    }

    public string Address { get => _address; } 
    public string GazePort { get => _gazePort; }
    public string AudioPort { get => _audioPort; }
    public string VideoPort { get => _videoPort; }
    public int HeaderSize { get => _headerSize; }
    public int BufferSize { get => _bufferSize; }
    */
    public static string Address = "169.254.50.239";
    public static string VideoPort = "12345";
    public static string AudioPort = "12346";
    public static string GazePort = "12347";
    public static int HeaderSize = 10;
    public static int BufferSize = 32768;

}
