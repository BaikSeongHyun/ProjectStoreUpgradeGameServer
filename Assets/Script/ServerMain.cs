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
	// test data
	[SerializeField]string[] socket;

	// server socket
	[SerializeField] TcpServer networkProcessor;
	[SerializeField] DataProcessor dataProcessor;

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

		// network process active 
		networkProcessor = new TcpServer();
		networkProcessor.OnReceived += OnReceivedPacketFromClient;
	
		// data process active
		dataProcessor = new DataProcessor( "PlayerData.data", System.IO.FileMode.OpenOrCreate );

		// set receive notifier
		RegisterServerReceivePacket((int) ClientToServerPacket.JoinRequest, ReceiveJoinRequest);
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
		socket = new string[networkProcessor.clientSockets.Count];
		for(int i =0; i < socket.Length; i++)
			socket[i] = networkProcessor.clientSockets[i].ToString();
	}

	// server system off
	void OnApplicationQuit()
	{
		networkProcessor.ServerClose();
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
		networkProcessor.ServerStart();
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


	// receive section
	// join request
	public void ReceiveJoinRequest( Socket clientSocket, byte[] data )
	{
		// receive packet serialize 
		JoinRequestPacket receivePacket = new JoinRequestPacket( data );
		JoinRequestData joinRequestData = receivePacket.GetData();
	
		// process - player join
		bool result;
		string resultString;
		result = dataProcessor.JoinPlayer( joinRequestData.id, joinRequestData.password, out resultString );

		// make result data
		JoinResultData sendData = new JoinResultData();
		sendData.joinResult = result;
		sendData.message = resultString;

		// make result packet
		JoinResultPacket sendPacket = new JoinResultPacket( sendData );

		// send result packet
		networkProcessor.Send( clientSocket, sendPacket.GetPacketData(), sendPacket.GetPacketData().Length );

	}

	// login request
	public void ReceiveLoginRequest( Socket clientSocket, byte[] data )
	{
		// packet serialize 
		LoginRequestPacket packet = new LoginRequestPacket( data );
		LoginRequestData joinRequestData = packet.GetData();

		// process




		// send result packet
	}

}
