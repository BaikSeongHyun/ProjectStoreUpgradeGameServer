using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;

//server main stream
public class ServerMain : MonoBehaviour
{
	// server socket
	[SerializeField] TcpServer networkProcesser;

	// queue -> input / output check point
	[SerializeField] PacketQueue receiveQueue;
	[SerializeField] PacketQueue sendQueue;

	// delegate -> for packtet receive check
	public delegate void ReceiveNotifier(Socket socket,byte[] data);

	// server notifier set -> socket library
	Dictionary <int, ReceiveNotifier> notifierForServer = new Dictionary<int, ReceiveNotifier>();

	// byte array data
	byte[] receiveBuffer;
	byte[] sendBuffer;
	const int bufferSize = 2048;

	// data initialize
	void Awake()
	{
		// allocate queue
		receiveQueue = new PacketQueue();
		sendQueue = new PacketQueue();

		// allocate buffer
		receiveBuffer = new byte[bufferSize];
		sendBuffer = new byte[bufferSize];


		//server process active 
		networkProcesser = new TcpServer();
	}

	// start main server
	void Start()
	{
		StartServer();
	}

	// server process -> receive from client
	void Update()
	{

	}

	// server system off
	void OnApplicationQuit()
	{
		networkProcesser.ServerClose();
	}

	// public method

	// start main server
	public void StartServer()
	{
		networkProcesser.ServerStart();
	}

	// receive
	public void Receive( Socket socket )
	{
		int count = receiveQueue.Count;

		for ( int i = 0; i < count; i++ )
		{
			int receiveSize = 0;
			receiveSize = receiveQueue.Dequeue( ref receiveBuffer, receiveBuffer.Length );

			if( receiveSize > 0 )
			{
				byte[] message = new byte[receiveSize];
				
				Array.Copy( receiveBuffer, message, receiveSize );

				ReceivePacket( notifierForServer, socket, message );
			}
		}
	}

	// receive packet
	public void ReceivePacket( Dictionary<int, ReceiveNotifier> notifier, Socket socket, byte[] data )
	{

	}


	// server receive notifier register
	public void RegisterServerReceivePacket( int packetID, ReceiveNotifier notifier )
	{
		notifierForServer.Add( packetID, notifier );
	}

	// server receive notifier unregister
	public void UnregisterServerReceivePackter( int packetID )
	{
		notifierForServer.Remove( packetID );
	}


}
