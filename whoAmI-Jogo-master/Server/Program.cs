using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MultiServer
{
    class Program
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 2048;
        private const int PORT = 5000;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];

        private static string palavra = "";
        private static int vez = 1;
        private static bool acerto = false;

        static void Main()
        {
            Console.Title = "Servidor";
            SetupServer();
            Console.ReadLine(); // When we press enter close everything
            CloseAllSockets();
        }

        private static void SetupServer()
        {
            Console.WriteLine("Configurando o servidor...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            clientSockets.Add(socket);

            if (clientSockets.Count > 3)
            {
                SendString(socket, "Sala cheia, volte mais tarde");
                clientSockets.RemoveAt(clientSockets.Count);
                socket.Close();
            }

            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Cliente {0} conectado", clientSockets.Count);
            serverSocket.BeginAccept(AcceptCallback, null);

            if (socket.Equals(clientSockets[0]))
            {
                SendString(socket, "Voce e o mestre, Aguarde....");
                PartidaMestre(socket);
            }

            else
            {
                SendString(socket, "Voce e um jogador, Aguarde....\n");
                PartidaJogador(socket);
            }
        }

        private static void PartidaMestre(Socket socket)
        {
            SendString(socket, "Partida iniciada....");
            SendString(socket, "Escolha a palavra");

            int received = socket.Receive(buffer, SocketFlags.None);
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            palavra = Encoding.ASCII.GetString(data);
            Console.WriteLine("Palavra da vez: " + palavra);

        }

        private static void PartidaJogador(Socket socket)
        {   
            Socket mestre = clientSockets[0];

            SendString(socket, "Partida iniciada....");

            while (palavra.Equals(""))
            {
                System.Threading.Thread.Sleep(1000);
            }

            while (acerto == false)
            {
                string pergunta;

                if (!socket.Equals(mestre) && socket.Equals(clientSockets[vez]))
                {
                    SendString(socket, "Faca uma pergunta: ");

                    int received;

                    try
                    {
                        received = socket.Receive(buffer, SocketFlags.None);
                    }

                    catch
                    {
                        Console.WriteLine("Erro");
                        socket.Close();
                        clientSockets.Remove(socket);
                        return;
                    }

                    var data = new byte[received];
                    Array.Copy(buffer, data, received);
                    pergunta = Encoding.ASCII.GetString(data);

                    Console.WriteLine("Pergunta feita: " + pergunta);
                }
                else
                {
                    SendString(socket, "Aguarde....");
                }
                vez++;
            }
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;

            int received;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Erro");
                current.Close(); 
                clientSockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);

            //if (text.ToLower() == "get time") // Client requested time
            //{
            //    Console.WriteLine("Text is a get time request");
            //    byte[] data = Encoding.ASCII.GetBytes("Oi");
            //    current.Send(data);
            //    Console.WriteLine("Time sent to client");
            //}
            //else if (text.ToLower() == "exit") // Client wants to exit gracefully
            //{
            //    current.Shutdown(SocketShutdown.Both);
            //    current.Close();
            //    clientSockets.Remove(current);
            //    Console.WriteLine("Client disconnected");
            //    return;
            //}
            //else
            //{
            //    Console.WriteLine("Text is an invalid request");
            //    byte[] data = Encoding.ASCII.GetBytes("Invalid request");
            //    current.Send(data);
            //    Console.WriteLine("Warning Sent");
            //}
        }

        private static void SendString(Socket socket, string message)
        {
            byte[] mensagem = Encoding.ASCII.GetBytes(message);
            socket.Send(mensagem);
        }

        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            serverSocket.Close();
        }
    }
}