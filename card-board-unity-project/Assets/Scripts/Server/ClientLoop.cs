
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

                clients.Add(newClientQueue[0]);
                newClientQueue.RemoveAt(0);
            }

        } finally { mutex.ReleaseMutex(); }
    }

    private static List<TcpClient> clients = new List<TcpClient>();

    private static List<TcpClient> clientsToDisconnect = new List<TcpClient>();

    private static List<byte[]> sendQueue = new List<byte[]>();

    public static void Tick () {

        foreach (var client in clients) {

            if (client.Available > 0) { //if data to recv

                //recv packet size
                byte[] psizebuf = new byte[2];
                client.GetStream().Read(psizebuf, 0, 2);
                ushort psize = BitConverter.ToUInt16(psizebuf, 0);

                //recv packet
                byte[] pbuf = new byte[psize];
                client.GetStream().Read(pbuf, 0, psize);

                //process the packet
                byte[] rdata = Process(client, pbuf);
                if (rdata.Length != 0) sendQueue.Add(rdata);
            }
        }

        //disconnect all clients needed to disconnect
        if (clientsToDisconnect.Count != 0) {

            foreach (var client in clientsToDisconnect) {

                clients.Remove(client);

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
        }

        //send msg if needed
        if (sendQueue.Count != 0) {

            foreach (var sbuf in sendQueue) {

                foreach (var client in clients) {

                    client.GetStream().Write(sbuf, 0, sbuf.Length);
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

    private static byte[] Process (TcpClient client, byte[] packet) {

        switch (packet[0]) {

            case 0: { //create card

                //read card id
                int cardID = BitConverter.ToInt32(packet, 1);

                //make buffer create res packet
                byte[] rbuf = new byte[7];
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(1)), 0, rbuf, 0, 2);
                rbuf[2] = 0;
                Buffer.BlockCopy(BitConverter.GetBytes(cardID), 0, rbuf, 0, 4);

                //card manmager create

                Console.Log(client.Client.RemoteEndPoint.ToString() + ": Created Card: " + cardID.ToString());

                //return it
                return rbuf;
            }

            case 1: { //delete card

                //read card id
                int cardID = BitConverter.ToInt32(packet, 1);

                //make buffer delete res packet
                byte[] rbuf = new byte[7];
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(1)), 0, rbuf, 0, 2);
                rbuf[2] = 1;
                Buffer.BlockCopy(BitConverter.GetBytes(cardID), 0, rbuf, 0, 4);

                //card manmager delete

                Console.Log(client.Client.RemoteEndPoint.ToString() + ": Deleted Card: " + cardID.ToString());

                //return it
                return rbuf;
            }
        }

        return new byte[0];
    }
}
