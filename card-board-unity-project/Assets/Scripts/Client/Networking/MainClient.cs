
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public static class MainClient {

    public static TcpClient client;
    public static NetworkStream stream;

    public static string serverAddress;
    public static bool connected;

    public static void ConnectToServer (string address, int port) {

        if (connected) return;

        serverAddress = address;

        try {

            //connect
            client = new TcpClient(address, port);
            stream = client.GetStream();

            byte[] sizeBuf = new byte[4];
            stream.Read(sizeBuf, 0, 4);
            int size = BitConverter.ToInt32(sizeBuf, 0);

            byte[] packetBuf = new byte[size];
            stream.Read(packetBuf, 0, size);

            CardManager.instance.LoadData(packetBuf);

            connected = true;

        } catch (Exception ex) {

            UnityEngine.Debug.Log(ex.Message);
        }
    }

    public static void DisconnectFromServer () {

        if (!connected) return;

        try {

            //send disconnect
            Send((byte)5, new byte[0]);

        } catch {}
        try {

            client.Close();
            stream.Close();

        } catch {}

        connected = false;
    }

    public static void Send (byte pid, byte[] data) {

        if (!connected) return;

        stream.Write(BitConverter.GetBytes(data.Length + 1), 0, 2);
        stream.Write(new byte[1]{pid}, 0, 1);
        if (data.Length != 0) stream.Write(data, 0, data.Length);
    }
}
