
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

public static class ClientLoop {

    private static Mutex mutex = new Mutex();

    private static List<TcpClient> newClientQueue = new List<TcpClient>();
    public static void QueueNewClient (TcpClient client) {

        mutex.WaitOne(); try {

            newClientQueue.Add(client);

        } finally { mutex.ReleaseMutex(); }
    }
    public static void PopCheckNewClients () {

        mutex.WaitOne(); try {

            while (newClientQueue.Count != 0) {

                TcpClient newClient = newClientQueue[0];
                newClientQueue.RemoveAt(0);

                clients.Add(newClient);

                ChatSend("<color=green>User joined!</color>");

                //send card data
                byte[] cardData = ServerCards.GetSave();
                newClient.GetStream().Write(BitConverter.GetBytes(cardData.Length), 0, 4);
                if (cardData.Length != 0) newClient.GetStream().Write(cardData, 0, cardData.Length);
            }

        } finally { mutex.ReleaseMutex(); }
    }

    private static List<TcpClient> clients = new List<TcpClient>();

    private static List<TcpClient> clientsToDisconnect = new List<TcpClient>();

    private static List<SendNode> sendQueue = new List<SendNode>();

    public static void Decache () {

        ServerCards.Save();

        clients.Clear();
        clientsToDisconnect.Clear();
        sendQueue.Clear();
    }

    public static void Tick () {

        PopCheckNewClients();

        foreach (var client in clients) {

            if (client.Available > 0) { //if data to recv

                //recv packet size
                byte[] psizebuf = new byte[4];
                client.GetStream().Read(psizebuf, 0, 4);
                int psize = BitConverter.ToInt32(psizebuf, 0);

                //recv packet
                byte[] pbuf = new byte[psize];
                client.GetStream().Read(pbuf, 0, psize);

                //process the packet
                SendNode rdata = Process(client, pbuf);
                if (rdata != null) sendQueue.Add(rdata);
            }
        }

        //disconnect all clients needed to disconnect
        if (clientsToDisconnect.Count != 0) {

            foreach (var client in clientsToDisconnect) {

                clients.Remove(client);

                ChatSend("<color=orange>User left.</color>");

                try {

                    client.Close();

                } catch (Exception ex) {

                    Console.Log(LogType.WARN, "Failed to boot client: " + client.Client.RemoteEndPoint.ToString());
                    Console.Log(LogType.ERROR, ex.Message);
                }
            }
            clientsToDisconnect.Clear();

            if (clients.Count == 0) { //all clients offline, clear cache

                sendQueue.Clear();

                Console.Log("Zero clients online, cache cleared.");
            }

            ServerCards.Save();
        }

        //send msg if needed
        if (sendQueue.Count != 0) {

            foreach (var sn in sendQueue) {

                foreach (var client in clients) {

                    if (client == sn.clientToIgnore) continue;

                    client.GetStream().Write(sn.buffer, 0, sn.buffer.Length);
                }
            }
            sendQueue.Clear();
        }
    }

    public static void Send (NetworkStream stream, byte pid, byte[] data) {

        stream.Write(BitConverter.GetBytes(data.Length + 1), 0, 2);
        stream.Write(new byte[1]{pid}, 0, 1);
        if (data.Length != 0) stream.Write(data, 0, data.Length);
    }

    public static void ChatSend (string msg) {

        byte[] msgBuf = System.Text.Encoding.ASCII.GetBytes(msg);

        byte[] rbuf = new byte[5 + msgBuf.Length];
        Buffer.BlockCopy(BitConverter.GetBytes(1 + msgBuf.Length), 0, rbuf, 0, 4);
        rbuf[4] = 3;
        Buffer.BlockCopy(msgBuf, 0, rbuf, 5, msgBuf.Length);

        sendQueue.Add(new SendNode(rbuf));
    }

    private static SendNode Process (TcpClient client, byte[] packet) {

        switch (packet[0]) {

            case 0: { //create card

                //read card id
                int cardID = BitConverter.ToInt32(packet, 1);

                //make buffer create res packet
                byte[] rbuf = new byte[9];
                Buffer.BlockCopy(BitConverter.GetBytes(5), 0, rbuf, 0, 4);
                rbuf[4] = 0;
                Buffer.BlockCopy(BitConverter.GetBytes(cardID), 0, rbuf, 5, 4);

                ServerCards.CreateNewCard(cardID);

                Console.Log(client.Client.RemoteEndPoint.ToString() + ": Created Card: " + cardID.ToString());

                //return it
                return new SendNode(rbuf);
            }

            case 1: { //delete card

                //read card id
                int cardID = BitConverter.ToInt32(packet, 1);

                //make buffer delete res packet
                byte[] rbuf = new byte[9];
                Buffer.BlockCopy(BitConverter.GetBytes(5), 0, rbuf, 0, 4);
                rbuf[4] = 1;
                Buffer.BlockCopy(BitConverter.GetBytes(cardID), 0, rbuf, 5, 4);

                ServerCards.DeleteCard(cardID);

                Console.Log(client.Client.RemoteEndPoint.ToString() + ": Deleted Card: " + cardID.ToString());

                //return it
                return new SendNode(rbuf);
            }

            case 2: { //update card

                //read card id
                int cardID = BitConverter.ToInt32(packet, 1);

                //read card buffer data
                byte[] cardBuf = new byte[packet.Length - 5];
                Buffer.BlockCopy(packet, 5, cardBuf, 0, cardBuf.Length);

                //make buffer update packet
                byte[] rbuf = new byte[9 + cardBuf.Length];
                Buffer.BlockCopy(BitConverter.GetBytes(5 + cardBuf.Length), 0, rbuf, 0, 4);
                rbuf[4] = 2;
                Buffer.BlockCopy(BitConverter.GetBytes(cardID), 0, rbuf, 5, 4);
                Buffer.BlockCopy(cardBuf, 0, rbuf, 9, cardBuf.Length);

                ServerCards.UpdateCard(cardID, cardBuf);

                // Console.Log(client.Client.RemoteEndPoint.ToString() + ": Updated Card: " + cardID.ToString());

                //return it
                return new SendNode(rbuf, client);
            }

            case 3: { //disconnect

                //log
                Console.Log(client.Client.RemoteEndPoint.ToString() + ": Disconnected");

                //disconect em
                clientsToDisconnect.Add(client);

            break; }

            case 4: { //chat message

                byte[] msgBuf = new byte[packet.Length - 1];
                Buffer.BlockCopy(packet, 1, msgBuf, 0, msgBuf.Length);
                string msg = System.Text.Encoding.ASCII.GetString(msgBuf);

                //log
                Console.Log(client.Client.RemoteEndPoint.ToString() + ": Sent: <color=white>'" + msg + "'</color>");

                //make return packet
                byte[] rbuf = new byte[5 + msgBuf.Length];
                Buffer.BlockCopy(BitConverter.GetBytes(1 + msgBuf.Length), 0, rbuf, 0, 4);
                rbuf[4] = 3;
                Buffer.BlockCopy(msgBuf, 0, rbuf, 5, msgBuf.Length);

                //return it
                return new SendNode(rbuf);
            }
        }

        return null;
    }
}

public class SendNode {

    public byte[] buffer;
    public TcpClient clientToIgnore;

    public SendNode (byte[] a) { buffer = a; }
    public SendNode (byte[] a, TcpClient b) { buffer = a; clientToIgnore = b; }
}
