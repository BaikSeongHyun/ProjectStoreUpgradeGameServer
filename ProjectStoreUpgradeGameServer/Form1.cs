using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;



namespace ProjectStoreUpgradeGameServer
{
    public partial class Form1 : Form
    {
        // packet queue -> Receive / send
        PacketQueue receiveQueue;
        PacketQueue sendQueue;

        // queue -> set socket
        Queue<Socket> indexClientQueue;

        // lock object for index client queue
        object lockIndexQueue = new object();

        // processor
        NetworkProcessor networkProcessor;
        DataProcessor dataProcessor;

        // delegate -> for packet receive check
        public delegate void ReceiveNotifier(Socket socket, byte[] data);

        // server notifier set -> socket library
        Dictionary<int, ReceiveNotifier> notifierForServer = new Dictionary<int, ReceiveNotifier>();
        string activateMethodName;

        // byte array data
        byte[] receiveBuffer;
        byte[] sendBuffer;
        const int bufferSize = 2048;

        // constructor -> start win form
        public Form1()
        {
            InitializeComponent();
            DataInitialize();
            StartServer();

            Thread thread = new Thread(new ThreadStart(ReceiveFromClient));
            thread.Start();
        }


        // private method
        // packet seperater -> header / packet data
        private bool SeperatePacket(byte[] originalData, out int packetID, out byte[] seperatedData)
        {
            PacketHeader header = new PacketHeader();
            HeaderSerializer serializer = new HeaderSerializer();

            serializer.SetDeserializedData(originalData);
            serializer.Deserialize(ref header);

            int headerSize = Marshal.SizeOf(header.id) + Marshal.SizeOf(header.length);
            int packetDataSize = originalData.Length - headerSize;
            byte[] packetData = null;

            if (packetDataSize > 0)
            {
                packetData = new byte[packetDataSize];
                Buffer.BlockCopy(originalData, headerSize, packetData, 0, packetData.Length);
            }
            else
            {
                packetID = header.id;
                seperatedData = null;
                return false;
            }

            packetID = header.id;
            seperatedData = packetData;
            return true;
        }

        // enqueue - receive queue
        private void OnReceivePacketFromClient(Socket socket, byte[] message, int size)
        {
            receiveQueue.Enqueue(message, size);

            lock (lockIndexQueue)
            {
                indexClientQueue.Enqueue(socket);
            }
        }

        // public method
        // data initialize
        public void DataInitialize()
        {
            // allocate queue
            receiveQueue = new PacketQueue();
            sendQueue = new PacketQueue();
            indexClientQueue = new Queue<Socket>();

            // allocate buffer
            receiveBuffer = new byte[bufferSize];
            sendBuffer = new byte[bufferSize];

            // set network processor
            networkProcessor = new NetworkProcessor();
            networkProcessor.OnReceived += OnReceivePacketFromClient;

            // set data processor
            dataProcessor = new DataProcessor(networkProcessor);

            // set receive notifier
            RegisterServerReceivePacket((int)ClientToServerPacket.JoinRequest, ReceiveJoinRequest);
            RegisterServerReceivePacket((int)ClientToServerPacket.LoginRequest, ReceiveLoginRequest);
            RegisterServerReceivePacket((int)ClientToServerPacket.GameDataRequest, ReceiveGameDataRequest);
            RegisterServerReceivePacket((int)ClientToServerPacket.StoreCreateRequest, ReceiveStoreCreateRequest);
            RegisterServerReceivePacket((int)ClientToServerPacket.ItemCreateRequest, ReceiveItemCreateRequest);
            RegisterServerReceivePacket((int)ClientToServerPacket.ItemAcquireRequest, ReceiveItemCreateRequest);
            RegisterServerReceivePacket((int)ClientToServerPacket.ItemSellRequest, ReceiveItemSellRequest);
        }

        // start main server
        public void StartServer()
        {
            networkProcessor.ServerStart();
        }

        // receive
        public void ReceiveFromClient()
        {
            Console.WriteLine("Receive Threading start");

            while (true)
            {
                int count = receiveQueue.Count;

                for (int i = 0; i < count; i++)
                {
                    int receiveSize = 0;
                    receiveSize = receiveQueue.Dequeue(ref receiveBuffer, receiveBuffer.Length);

                    Socket clientSocket;
                    lock (lockIndexQueue)
                    {
                        if (indexClientQueue.Count == 0)
                            continue;
                        clientSocket = indexClientQueue.Dequeue();
                    }

                    // packet precess 
                    if (receiveSize > 0)
                    {
                        byte[] message = new byte[receiveSize];

                        Array.Copy(receiveBuffer, message, receiveSize);

                        int packetID;
                        byte[] packetData;

                        // packet seperate -> header / data
                        SeperatePacket(message, out packetID, out packetData);

                        ReceiveNotifier notifier;

                        try
                        {
                            // use notifier
                            notifierForServer.TryGetValue(packetID, out notifier);
                            activateMethodName = notifier.Method.ToString();
                            notifier(clientSocket, packetData);
                        }
                        catch (NullReferenceException e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine("Server : Null Reference Exception - On Receive (use notifier)");
                        }
                    }
                }
            }
        }

        // server receive notifier register
        public void RegisterServerReceivePacket(int packetID, ReceiveNotifier notifier)
        {
            notifierForServer.Add(packetID, notifier);
        }

        // server receive notifier unregister
        public void UnregisterServerReceivePackter(int packetID)
        {
            notifierForServer.Remove(packetID);
        }

        // receive section
        // join request
        public void ReceiveJoinRequest(Socket clientSocket, byte[] data)
        {
            // receive packet serialize 
            JoinRequestPacket receivePacket = new JoinRequestPacket(data);
            JoinRequestData joinRequestData = receivePacket.GetData();

            // process - player join
            bool result;
            string resultString;
            result = dataProcessor.JoinPlayer(joinRequestData.id, joinRequestData.password, out resultString);

            // make result data
            JoinResultData sendData = new JoinResultData();
            sendData.joinResult = result;
            sendData.message = resultString;

            // make result packet
            JoinResultPacket sendPacket = new JoinResultPacket(sendData);

            // send result packet
            networkProcessor.Send(clientSocket, sendPacket);
        }

        // login request
        public void ReceiveLoginRequest(Socket clientSocket, byte[] data)
        {
            // packet serialize 
            LoginRequestPacket receivePacket = new LoginRequestPacket(data);
            LoginRequestData loginRequestData = receivePacket.GetData();

            // process
            bool result;
            string resultString;
            result = dataProcessor.LoginPlayer(clientSocket, loginRequestData.id, loginRequestData.password, out resultString);

            // make result data
            LoginResultData sendData = new LoginResultData();
            sendData.loginResult = result;
            sendData.message = resultString;

            //make result packet
            LoginResultPacket sendPacket = new LoginResultPacket(sendData);

            // send result packet
            networkProcessor.Send(clientSocket, sendPacket);
        }

        // game data request
        public void ReceiveGameDataRequest(Socket clientSocket, byte[] data)
        {
            // packet serialize
            GameDataRequestPacket receivePacket = new GameDataRequestPacket(data);
            GameDataRequestData gameDataRequestData = receivePacket.GetData();

            // process 
            List<StoreData> storeDataSet = new List<StoreData>();
            List<ItemData> itemDataSet = new List<ItemData>();
            dataProcessor.GetGameData(gameDataRequestData.playerID, storeDataSet, itemDataSet);

            // send store data
            foreach (StoreData element in storeDataSet)
            {
                StoreDataPacket sendPacket = new StoreDataPacket(element);

                networkProcessor.Send(clientSocket, sendPacket);
            }

            // send item data
            foreach (ItemData element in itemDataSet)
            {
                ItemDataPacket sendPacket = new ItemDataPacket(element);

                networkProcessor.Send(clientSocket, sendPacket);
            }
        }

        // store create request
        public void ReceiveStoreCreateRequest(Socket clientSocket, byte[] data)
        {
            // packet deserialize
            StoreCreateRequestPacket packet = new StoreCreateRequestPacket(data);
            StoreCreateRequestData storeCreateRequestData = packet.GetData();

            // process
            StoreCreateResultData sendData = new StoreCreateResultData();
            StoreData sendStoreData = new StoreData();
            sendData.createResult = dataProcessor.StoreCreate(storeCreateRequestData, sendStoreData, out sendData.message);

            // serialize & send data - result
            StoreCreateResultPacket sendPacket = new StoreCreateResultPacket(sendData);
            networkProcessor.Send(clientSocket, sendPacket);

            if (sendData.createResult)
            {
                // serialize & send data
                StoreDataPacket sendStorePacket = new StoreDataPacket(sendStoreData);
                networkProcessor.Send(clientSocket, sendStorePacket);
            }
           
        }

        // item create request
        public void ReceiveItemCreateRequest(Socket clientSocket, byte[] data)
        {

        }

        // item acquire request
        public void ReceiveItemAcquireRequest(Socket clientSocket, byte[] data)
        {

        }

        // item Sell request
        public void ReceiveItemSellRequest(Socket clientSocket, byte[] data)
        {

        }
    }
}
