using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Chat
{
    class Servidor
    {
        private static TcpListener serverSocket = default(TcpListener);
        private static Socket clientSocket = default(Socket);
        private static readonly int maxClientsCount = 4;
        private static readonly handleClient[] clients = new handleClient[maxClientsCount];

        static void Main(string[] args)
        {

            Console.Title = "Servidor";

            serverSocket = new TcpListener(IPAddress.Any, 7777);
            clientSocket = default(Socket);
            serverSocket.Start();

            while (true)
            {
                Console.WriteLine("Esperando conexões...");
                clientSocket = serverSocket.AcceptSocket();
                Console.WriteLine("Conectado!");
                int i = 0;
                for (i = 0; i < maxClientsCount; i++)
                {
                    if (clients[i] == null)
                    {
                        (clients[i] = new handleClient()).startClient(clientSocket, clients);
                        break;
                    }
                }

                if (i == maxClientsCount)
                {
                    StreamWriter ots = new StreamWriter(new NetworkStream(clientSocket));
                    ots.AutoFlush = true;
                    ots.WriteLine("*** Servidor Cheio ***");
                    ots.Close();
                    clientSocket.Close();
                }
            }
        }
    }

    public class handleClient
    {
        private Socket clientSocket;
        private handleClient[] clients;
        private int maxClientsCount;
        private String clientName;
        private StreamReader ins;
        private StreamWriter ots;
        private String palavra;
        private int jogadorDaVez = 1;

        public void startClient(Socket inClientSocket, handleClient[] clients)
        {
            this.clientSocket = inClientSocket;
            this.clients = clients;
            this.maxClientsCount = clients.Length;

            ots = new StreamWriter(new NetworkStream(clientSocket));
            ots.AutoFlush = true;

            if (inClientSocket.Equals(clients[0].clientSocket))
            {
                ots.WriteLine("*** Voce e o mestre ***");
            }

            Thread ctThread = new Thread(doChat);
            ctThread.Start();
        }

        private void doChat()
        {
            int maxClientsCount = this.maxClientsCount;
            handleClient[] clients = this.clients;

            try
            {
                ins = new StreamReader(new NetworkStream(clientSocket));
                ots = new StreamWriter(new NetworkStream(clientSocket));
                ots.AutoFlush = true;
                String name;

                ots.WriteLine("*** Informe seu nome ***");
                name = ins.ReadLine().Trim();

                Console.WriteLine("Novo usuario: " + name);
                ots.WriteLine("*** Ola " + name + " ***\n*** Para sair digite /quit ***");

                lock (this)
                {
                    for (int i = 0; i < maxClientsCount; i++)
                    {
                        if (clients[i] != null && clients[i] == this)
                        {
                            clientName = "@" + name;
                            break;
                        }
                    }

                    for (int i = 0; i < maxClientsCount; i++)
                    {
                        if (clients[i] != null && clients[i] != this)
                        {
                            clients[i].ots.WriteLine("*** Novo usuario entrou: " + name + " ***");
                        }
                    }
                }

                if (clientSocket.Equals(clients[0].clientSocket))
                {
                    ots.WriteLine("Informe a palavra do jogo:");
                    palavra = ins.ReadLine();
                    Console.WriteLine("Palavra do jogo: " + palavra);
                }

                while (true)
                {
                    //Console.WriteLine(jogadorDaVez);
                    //Console.WriteLine(clients.Length);
                    if (clientSocket.Equals(clients[jogadorDaVez].clientSocket) || clientSocket.Equals(clients[0].clientSocket))
                    {
                        if (clientSocket.Equals(clients[jogadorDaVez].clientSocket))
                        {
                            ots.WriteLine("Jogador da vez !\nFaca a pergunta: ");
                            String pergunta = ins.ReadLine();

                            if (pergunta.StartsWith("/quit"))
                            {
                                break;
                            }

                            else
                            {
                                lock (this)
                                {
                                    for (int i = 0; i < maxClientsCount; i++)
                                    {
                                        if (clients[i] != null && clients[i] != null)
                                            clients[i].ots.WriteLine("*** O usuario " + name + " perguntou: " + pergunta);

                                    }

                                    clients[0].ots.WriteLine("Digite a resposta:");
                                    String resposta = clients[0].ins.ReadLine();

                                    for (int i = 0; i < maxClientsCount; i++)
                                    {
                                        if (clients[i] != null && clients[i] != null)
                                            clients[i].ots.WriteLine("*** Mestre respondeu: " + resposta);
                                    }

                                    ots.WriteLine("Faca o chute:");
                                    String chute = this.ins.ReadLine();

                                    Console.WriteLine(clients[0].palavra);

                                    if (chute == clients[0].palavra)
                                    {
                                        Console.WriteLine("Palavra adivinhada");
                                        for (int i = 0; i < maxClientsCount; i++)
                                        {
                                            if (clients[i] != null && clients[i] != null)
                                                clients[i].ots.WriteLine("*** O usuario " + name + " acertou a palavra " + palavra + " e o VENCEDOR ***");

                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < maxClientsCount; i++)
                                        {
                                            if (clients[i] != null && clients[i] != null)
                                                clients[i].ots.WriteLine("*** O usuario " + name + " errou a palavra");
                                        }
                                    }
                                }
                            }
                        }

                        jogadorDaVez++;
                        if (jogadorDaVez > maxClientsCount)
                            jogadorDaVez = 1;
                    }

                    else
                    {
                        String line = ins.ReadLine();
                        ots.WriteLine("*** Aguarde sua vez! ***");
                    }
                }

                Console.WriteLine("Usuario " + name + " se desconectou");
                lock (this)
                {
                    for (int i = 0; i < maxClientsCount; i++)
                    {
                        if (clients[i] != null && clients[i] != null)
                        {
                            clients[i].ots.WriteLine("*** O usuario " + name + " saiu ***");
                        }
                    }
                }
                ots.WriteLine("*** Ate logo " + name + " ***");

                lock (this)
                {
                    for (int i = 0; i < maxClientsCount; i++)
                    {
                        if (clients[i] == this)
                        {
                            clients[i] = null;
                        }
                    }
                }
                ins.Close();
                ots.Close();
                clientSocket.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        
        }
    }
}
