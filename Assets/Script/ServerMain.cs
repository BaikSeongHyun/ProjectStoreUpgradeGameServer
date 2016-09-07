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
	PacketQueue receiveQueue;
	PacketQueue sendQueue;
	Queue<Socket> indexClientQueue;
	object lockReceiveQueue = new object();

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
		indexClientQueue = new Queue<Socket>();

		// allocate buffer
		receiveBuffer = new byte[bufferSize];
		sendBuffer = new byte[bufferSize];

		// server process active 
		networkProcesser = new TcpServer();
		networkProcesser.OnReceived += OnReceivedPacketFromClient;
	}

	// start main server
	void Start()
	{
		StartServer();
	}

	// server process -> receive from client
	void Update()
	{
		ReceiveFromClient();
	}

	// server system off
	void OnApplicationQuit()
	{
		networkProcesser.ServerClose();
	}

	// private method
	// packet seperate id / data
	private bool SeperatePacket( byte[] originalData, out int packetID, out byte[] seperatedData )
	{
		PacketHeader header = new PacketHeader();
		HeaderSerializer serializer = new HeaderSerializer();

		serializer.SetDeserializedData( originalData );
		serializer.Deserialize( ref header );

		seperatedData = null;

		packetID = 0;
		return true;
	}

	// enqueue - receive queue
	private void OnReceivedPacketFromClient( Socket socket, byte[] message, int size )
	{
		receiveQueue.Enqueue( message, size );

		lock ( lockReceiveQueue )
		{
			indexClientQueue.Enqueue( socket );
		}
	}

	// public method
	// start main server
	public void StartServer()
	{
		networkProcesser.ServerStart();
	}

	// receive
	public void ReceiveFromClient()
	{
		int count = receiveQueue.Count;

		for ( int i = 0; i < count; i++ )
		{
			int receiveSize = 0;
			receiveSize = receiveQueue.Dequeue( ref receiveBuffer, receiveBuffer.Length );

			Socket clientSocket;
			lock ( lockReceiveQueue )
			{
				clientSocket = indexClientQueue.Dequeue();
			}

			// packet precess 
			if( receiveSize > 0 )
			{
				byte[] message = new byte[receiveSize];
				
				Array.Copy( receiveBuffer, message, receiveSize );

				int packetID;
				byte[] packetData;

				// packet seperate -> header / data
				SeperatePacket( message, out packetID, out packetData );

				ReceiveNotifier notifier;

				// use notifier
				try
				{
					notifierForServer.TryGetValue( packetID, out notifier );
					notifier( clientSocket, packetData );			
				}
				catch ( NullReferenceException e )
				{
					Debug.Log( e.Message );
					Debug.Log( "Server : Null Reference Exception - On Receive (use notifier)" );
				}
			}
		}
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
