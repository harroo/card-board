
using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class MainServer : MonoBehaviour {

    private Thread clientCatchThread;

    private void Start () {

        Console.Log("Starting server...");

        try {

            TcpListener listener = new TcpListener(IPAddress.Any, 2486);
            listener.Start();

            clientCatchThread = new Thread(()=>ClientCatchLoop(listener));
            clientCatchThread.Start();

            Console.Log(LogType.OK, "Server started successfully!");

        } catch (Exception ex) {

            Console.Log(LogType.ERROR, "Failed to start server: <color=yellow>" + ex.Message + "</color>");
        }
    }

    private void Update () {

        ClientLoop.Tick();
    }

    private void OnDestroy () {

        clientCatchThread.Abort();
    }

    private void ClientCatchLoop (TcpListener listener) {

        while (true) {

            TcpClient client = listener.AcceptTcpClient();

            Console.Log("Caught client: '<color=white>" + client.Client.RemoteEndPoint.ToString() + "</color>'");

            ClientLoop.QueueNewClient(client);
        }
    }
}
