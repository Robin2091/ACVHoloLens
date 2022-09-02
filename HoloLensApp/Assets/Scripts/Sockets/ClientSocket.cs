using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_EDITOR
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif

public class ClientSocket
{
#if !UNITY_EDITOR
	private readonly string ipAddress;
	private readonly string port;
	private readonly int bufferSize;
	private Stream outputStream;
	private Stream inputStream;
	private StreamSocket socket;
	private volatile bool receivedMessageFromServer = false;

	public ClientSocket(string ipAddress, string port, int bufferSize)
	{
		this.ipAddress = ipAddress;
		this.port = port;
		this.bufferSize = bufferSize;
		this.socket = new StreamSocket();
	}

	public async Task ConnectToServer()
	{
		CancellationTokenSource _cts = new CancellationTokenSource();
		_cts.CancelAfter(5000);
		Windows.Networking.HostName hostName = new Windows.Networking.HostName(ipAddress);
		try
        {
            await socket.ConnectAsync(hostName, port).AsTask(_cts.Token);
        }
        catch (TaskCanceledException)
        {
			throw new CannotConnectToServerException(IpAddress, port);
        }
		System.Diagnostics.Debug.WriteLine("CLIENT CONNECTED");
       outputStream = socket.OutputStream.AsStreamForWrite();

	}

	public void SendData(byte[] data)
	{
		try
		{
			outputStream.WriteAsync(data, 0, data.Length);
			outputStream.FlushAsync();
		} 
		catch (Exception e)
		{
			System.Diagnostics.Debug.WriteLine(e.Message);
		}
	}

	public void CloseConnection()
	{
        inputStream.Dispose();
        socket.Dispose();
	}

	public int BufferSize
	{
		get
		{
			return bufferSize;
		}
	}


	public String IpAddress
	{
		get
		{
			return ipAddress;
		}
	}

	public String Port
	{
		get
		{
			return port;
		}
	}

	public bool ReceivedMessageFromServer
	{
		get
		{
			return receivedMessageFromServer;
		}
	}
#endif
}

public class CannotConnectToServerException : System.Exception
{
	public CannotConnectToServerException(string ip, string port) :
		base(string.Format("Timed out connecting to server. Make sure server IP address:{0} and Port number:{1} are correct.",
			ip, port))
	{ }
}

public class DataNotBeingSentException : System.Exception
{
	public DataNotBeingSentException(string serverMessage) :
		base(serverMessage)
	{ }
}


