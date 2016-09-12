using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;

[System.Serializable]
public class NetworkProcessor
{
	class AsyncData
	{
		public Socket clientSocket = null;
		public const int messageMaxLength = 1024;
		public byte[] message = new byte[messageMaxLength];
		public int messageLength;
	}
	// event fleld
	public delegate void OnAcceptedEvent(Socket socket);

	public delegate void OnReceivedEvent(Socket socket,byte[] msg,int size);

	public delegate void DisconnectClient(Socket socket);

	public event OnAcceptedEvent OnAccepted;
	public event OnReceivedEvent OnReceived;
	public event DisconnectClient Disconnected;

	// added client socket list
	public List<Socket> clientSockets;

	// field - use connect client (default information)
	Socket listenSocket = null;
	[SerializeField] string serverIP;
	[SerializeField] int port = 9800;

	//constructor - default
	public NetworkProcessor()
	{
		listenSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
		clientSockets = new List<Socket>();
	}


	// private method
	// create packet include header
	private byte[] CreatePacketStream<T, U>( Packet<T,U> packet )
	{
		// data iniialize  
		byte[] packetData = packet.GetPacketData();

		PacketHeader header = new PacketHeader();
		HeaderSerializer serializer = new HeaderSerializer();

		// set header data
		header.length = (short) packetData.Length;
		header.id = (byte) packet.GetPacketID();

		byte[] headerData = null;
	
		try
		{
			serializer.Serialize( header );
		}
		catch
		{

		}

		headerData = serializer.GetSerializeData();

		// header / packet data combine
		byte[] data = new byte[headerData.Length + packetData.Length];

		int headerSize = Marshal.SizeOf( header.id ) + Marshal.SizeOf( header.length );
		Buffer.BlockCopy( headerData, 0, data, 0, headerSize );
		Buffer.BlockCopy( packetData, 0, data, headerSize, packetData.Length );

		return data;
	}

	// public method
	// start tcp server
	// set socket
	// set async call back
	public void ServerStart()
	{
		if( listenSocket.Connected )
			return;

		serverIP = GetLocalIPAddress();

		// listen socket bind
		listenSocket.Bind( new IPEndPoint( IPAddress.Parse( serverIP ), port ) );

		// listen socket listen
		listenSocket.Listen( 10 );

		// add socket accept callback method
		AsyncCallback acceptCallback = new AsyncCallback( AcceptAsyncCallback );
		AsyncData asyncData = new AsyncData();
		object asyncLinker = asyncData;
		asyncLinker = listenSocket;
		listenSocket.BeginAccept( acceptCallback, asyncLinker );

	}

	// close tcp server
	public void ServerClose()
	{
		if( listenSocket == null )
			return;

		foreach ( Socket clientSocket in clientSockets )
		{
			try
			{
				clientSocket.Close();
			}
			catch ( SocketException e )
			{
				Debug.Log( e.ErrorCode );
				Debug.Log( "Server : Socket Exception - On Server Close " );
			}
			catch ( NullReferenceException e )
			{
				Debug.Log( e.Message );
				Debug.Log( "Server : Null Reference Exception - On Server Close " );
			}
		}

		clientSockets.Clear();

		listenSocket.Close();
	}

	// find local ip address
	public static string GetLocalIPAddress()
	{
		var data = Dns.GetHostEntry( Dns.GetHostName() );

		foreach ( var ip in data.AddressList )
		{
			if( ip.AddressFamily == AddressFamily.InterNetwork )
				return ip.ToString();
		}
		throw new Exception( "Server : Local ip address not found" );       
	}

	// set port
	public void SetPort( int _port )
	{
		if( listenSocket.Connected )
			return;

		port = _port;
	}

	// accept callback method
	public void AcceptAsyncCallback( IAsyncResult asyncResult )
	{
		// add client socket
		Socket listenSocket = (Socket) asyncResult.AsyncState;
		Socket clientSocket = listenSocket.EndAccept( asyncResult );
		clientSockets.Add( clientSocket );
		Debug.Log( "Server : Connect Client -> IP : " + ( (IPEndPoint) clientSocket.RemoteEndPoint ).Address.ToString() + " / port : " + ( (IPEndPoint) clientSocket.RemoteEndPoint ).Port.ToString() );

		if( OnAccepted != null )
			OnAccepted( clientSocket );

		// add socket receive callback method
		AsyncCallback receiveCallback = new AsyncCallback( ReceiveAsyncCallback );
		AsyncData asyncData = new AsyncData();
		asyncData.clientSocket = clientSocket;
		object asyncLinker = asyncData;

		// start begin receive
		try
		{
			clientSocket.BeginReceive( asyncData.message, 0, AsyncData.messageMaxLength, SocketFlags.None, ReceiveAsyncCallback, asyncLinker );
		}
		catch
		{
			Debug.Log( "Error" );
			DownClient( clientSocket );
		}

		// reset listen socket
		AsyncCallback acceptCallback = new AsyncCallback( AcceptAsyncCallback );
		asyncLinker = listenSocket;
		listenSocket.BeginAccept( acceptCallback, asyncLinker );
	}

	public void ReceiveAsyncCallback( IAsyncResult asyncResult )
	{
		// data link
		AsyncData asyncData = (AsyncData) asyncResult.AsyncState;
		Socket clientSocket = asyncData.clientSocket;

		// message receive
		try
		{
			asyncData.messageLength = clientSocket.EndReceive( asyncResult );
		}
		catch ( NullReferenceException e )
		{
			DownClient( clientSocket );
			Debug.Log( e.Message );
			Debug.Log( "Server : Wrong Data structure - On Receive Async Callback (message receive)" );
			return;
		}
		catch ( SocketException e )
		{
			DownClient( clientSocket );
			Debug.Log( e.ErrorCode );
			Debug.Log( "Server : Disconnected client - On Receive Async Callback (message receive)" );
			return;
		}

		// receive process
		try
		{
			OnReceived( clientSocket, asyncData.message, asyncData.messageLength );
		}
		catch ( NullReferenceException e )
		{
			DownClient( clientSocket );
			Debug.Log( e.Message );
			Debug.Log( "Server : Wrong Data structure - On Receive Async Callback (receive process)" );
		}
		catch ( SocketException e )
		{
			DownClient( clientSocket );
			Debug.Log( e.ErrorCode );
			Debug.Log( "Server : Disconnected client - On Receive Async Callback (receive process)" );
		}

		// reset client socket
		AsyncCallback receiveCallback = new AsyncCallback( ReceiveAsyncCallback );
		try
		{
			clientSocket.BeginReceive( asyncData.message, 0, AsyncData.messageMaxLength, SocketFlags.None, ReceiveAsyncCallback, asyncData );
		}
		catch ( SocketException e )
		{
			DownClient( clientSocket );
			Debug.Log( e.ErrorCode );
			Debug.Log( "Server :  Socket Exception - On ReceiveAsyncCallback" );
		}
		catch ( NullReferenceException e )
		{
			DownClient( clientSocket );
			Debug.Log( e.Message );
			Debug.Log( "Server : Wrong Data structure - On ReceiveAsyncCallback" );
		}
	}

	public int Send<T,U>( Socket clientSocket, Packet<T,U> packet )
	{
		byte[] data = CreatePacketStream<T,U>( packet );
		Debug.Log( "Send Data" );
		foreach ( Socket client in clientSockets )
		{
			if( client == clientSocket )
			{
				try
				{
					client.Send( data, data.Length, SocketFlags.None );
				}
				catch ( SocketException e )
				{
					DownClient( client );
					Debug.Log( e.ErrorCode );
					Debug.Log( "Server :  Socket Exception - On Send" );
				}
				catch ( NullReferenceException e )
				{
					DownClient( client );
					Debug.Log( e.Message );
				}
			}
		}

		return -1;
	}

	public void DownClient( Socket clientSocket )
	{
		try
		{
			clientSocket.Close();
		}
		catch ( SocketException e )
		{
			Debug.Log( e.ErrorCode );
			Debug.Log( "Server : Socket Exception - Down Client" );
		}
		catch ( NullReferenceException e )
		{
			Debug.Log( e.Message );
			Debug.Log( "Server : Null Reference Exception - Down Client" );
		}
	}
}
