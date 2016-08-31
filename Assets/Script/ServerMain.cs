using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;

public class ServerMain : MonoBehaviour
{
	[SerializeField] Queue receiveCheckPoint;
	[SerializeField] Queue sendCheckPoint;

	void Awake()
	{
		receiveCheckPoint = new Queue();
		sendCheckPoint = new Queue();


	}
}
