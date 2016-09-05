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
	[SerializeField] TcpServer mainProcesser;

	// queue -> input / output check point
	[SerializeField] Queue receiveCheckPoint;
	[SerializeField] Queue sendCheckPoint;

	// delegate -> for packtet receive check
	public delegate void ReceiveNotifier(Socket socket,byte[] data);

	// server notifier set -> socket library
	Dictionary <int, ReceiveNotifier> notifierForServer = new Dictionary<int, ReceiveNotifier>();

	// data initialize
	void Awake()
	{
		receiveCheckPoint = new Queue();
		sendCheckPoint = new Queue();

		//server process active 
		mainProcesser = new TcpServer();
		mainProcesser.OnReceived += OnReceivedPacketFromClient;
	}

	// start main server
	void Start()
	{
		StartMainServer();
	}

	// server process -> receive from client
	void Update()
	{

	}

	// server down
	void OnApplicationQuit()
	{
		mainProcesser.ServerClose();
	}

	// packet insert
	private void OnReceivedPacketFromClient( Socket socket, byte[] msg, int size )
	{
		//receive & enqueue
		//receiveCheckPoint.Enqueue( msg, size );

		//classify use client socket
		lock ( receiveCheckPoint )
		{
			
		}
	}

	// public method

	// start main server
	public void StartMainServer()
	{
		mainProcesser.ServerStart();
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
