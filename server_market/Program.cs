using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class SupermarketServer
{
    private readonly int port;
    private readonly Queue<string> queue;
    private readonly Thread[] cashierThreads;

    public SupermarketServer(int port)
    {
        this.port = port;
        this.queue = new Queue<string>();
        this.cashierThreads = new Thread[2];
    }

    public int GetQueueSize()
    {
        lock (queue)
        {
            return queue.Count;
        }
    }

    public void Start()
    {
        for (int i = 0; i < 2; i++)
        {
            cashierThreads[i] = new Thread(CashierThread);
            cashierThreads[i].Start();
        }
        Console.WriteLine("Выделено кассиров" + " " + cashierThreads.Length);

        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine("Supermarket server started on port " + port);

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Thread clientThread = new Thread(ClientThread);
            clientThread.Start(client);
        }
    }

    public void Stop()
    {
        foreach (var thread in cashierThreads)
        {
            thread.Abort();
        }
    }

    private void ClientThread(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();

        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        Console.WriteLine("Received request: " + request);

        lock (queue)
        {
            queue.Enqueue(request);
            Monitor.PulseAll(queue);
        }

        string response = "OK";
        buffer = Encoding.UTF8.GetBytes(response);
        stream.Write(buffer, 0, buffer.Length);

        client.Close();
        Console.WriteLine("Number of customers in queue: " + GetQueueSize());
    }

    private void CashierThread()
    {

        Random random = new Random();
        while (true)
        {
            string? customer = null;

            lock (queue)
            {
                while (queue.Count == 0)
                {
                    Monitor.Wait(queue);
                }

                customer = queue.Dequeue();
            }

            Console.WriteLine("Cashier " + Environment.CurrentManagedThreadId + " is serving customer: " + customer);
            Thread.Sleep(random.Next(300, 800));
            Console.WriteLine("Cashier " + Environment.CurrentManagedThreadId + " finished serving customer: " + customer);
            
        }
    }

    static void Main(string[] args)
    {
        // Создание и запуск сервера
        int port = 8888;
        SupermarketServer server = new SupermarketServer(port);
        server.Start();

        // Ожидание ввода команды для остановки сервера
        Console.WriteLine("Server started. Press any key to stop.");
        Console.ReadKey();

        // Остановка сервера
        server.Stop();
        Console.WriteLine("Server stopped.");
    }

}
