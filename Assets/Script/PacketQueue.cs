﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// check point Queue
[System.Serializable]
public class PacketQueue
{
	// inner class packet information
	[System.Serializable]
	class PacketInfo
	{
		public int offset;
		public int size;
	}

	// write memory
	MemoryStream streamBuffer;
	[SerializeField] int memoryOffset;

	// input data & allocate infomation
	[SerializeField] byte[] tempData;
	[SerializeField] List<PacketInfo> packetList;

	// object - use lock
	object lockObject;

	// constructor - default
	public PacketQueue()
	{
		tempData = new byte[2048];
		streamBuffer = new MemoryStream();
		packetList = new List<PacketInfo>();
		lockObject = new object();
	}

	// stream size
	public int Count { get { return memoryOffset; } }

	// en queue
	public int Enqueue( byte[] data, int size )
	{
		// create new packet information
		PacketInfo info = new PacketInfo();

		// allocate packet information
		info.offset = memoryOffset;
		info.size = size;

		// lock & write buffer in stream buffer
		lock ( lockObject )
		{
			// add packet list
			packetList.Add( info );

			// write buffer
			streamBuffer.Position = memoryOffset;
			streamBuffer.Write( data, 0, size );
			streamBuffer.Flush();
			memoryOffset += size;
		}

		return size;
	}

	// de queue
	public int Dequeue( ref byte[] buffer, int size )
	{
		// check empty 
		if( packetList.Count <= 0 )
			return -1;

		int receiveSize = 0;

		lock ( lockObject )
		{
			// set first element
			PacketInfo info = packetList[0];

			// copy data
			int dataSize = Math.Min( size, info.size );
			streamBuffer.Position = info.offset;
			receiveSize = streamBuffer.Read( buffer, 0, dataSize );

			// delete first element
			if( receiveSize > 0 )
			{
				packetList.RemoveAt( 0 );
			}

			// clean stream
			if( packetList.Count == 0 )
			{
				Clear();
				memoryOffset = 0;
			}
		}

		return receiveSize;
	}

	// clear queue
	public void Clear()
	{
		// clean stream buffer
		byte[] buffer = streamBuffer.GetBuffer();
		Array.Clear( buffer, 0, buffer.Length );

		// set stream buffer information
		streamBuffer.Position = 0;
		streamBuffer.SetLength( 0 );
	}
}