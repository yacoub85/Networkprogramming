/*
 * Projectwork by Yacoub Gerges, IOT19
 * Course: Networkprogramming 20 YHP
 * Teacher: Aksel Forsberg
 * Program: Visual Studio CE 2019 and MySQL CE 8.0 including Workbench
 * Language: Server: C#
 *           Application-client: C#
 *           Arduino-client (simulation): C++
 *           
 * This is the application client in my solution where we connect to the server via socket to make different requests,
 * like getting highest and lowest temp from database, calculate the average value, show all post and add post & delete post
 * (as for now, we send full MySQL-query directly from the application wich means
 * I dont have any error handeling of the datainput to the database, I did it this way due to the timeframe. 
 * Ideally I wouldent like to showcase any MySQL-code/queries in the application).
 * I have also edited some "port-forwarding" within my routersettings to make the communication work over internet.
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client_C_Sharp
{

    class Program
    {
        private static readonly Socket ClientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private const int Port = 5555;
        

        static void Main()
        {
            Console.Title = "ClientApp";
            ConnectToServer();

            RequestLoop("");

            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
        }

        private static void ConnectToServer()
        {
            int attempts = 0;

            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection attempt " + attempts);
                    ClientSocket.Connect(IPAddress.Parse("192.168.0.42"), Port); //Enter the servers public IP-adress
                }
                catch (SocketException)
                {
                   Console.Clear();
                }
            }
            Console.Clear();
            Console.WriteLine("Connected");             
        }

        private static void RequestLoop(string requestSent)
        {
            try
            {
                while (requestSent.ToLower() != "0")
                {
                    print_menuSelections("");
                }                             
        
            }
            catch (Exception)
            {
                Console.WriteLine("Error! - Lost server.");
                Console.WriteLine("Disconnected");
                Console.ReadLine();
            }
        }

        private static void ReceiveResponse()
        {
            var buffer = new byte[2048];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0)
                return;

            else if(Encoding.UTF8.GetString(buffer, 0, received)== "To delete a post you need to enter a uid: ")
            {
                Console.WriteLine("To delete a post you need to enter a uid: ");
                String indexNum = "x";
                Console.WriteLine("Please enter the specific uid for the post you want delete: ");
                indexNum = Console.ReadLine();
                String deleteQuery = "DELETE FROM `networkprogramming`.`clienproject` WHERE(`uid` = '" + indexNum + "')";
                ClientSocket.Send(Encoding.UTF8.GetBytes(deleteQuery), SocketFlags.None);
                ReceiveResponse();
            }

            else

            Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, received));
            Console.WriteLine("Enter to back to the menu");
            Console.ReadLine();
            print_menuSelections("");
        }

        private static void print_menuSelections(string choiceFminu)
        {
            choiceFminu = "";
            while (choiceFminu != "0")
            {
                Console.Clear();
                Console.WriteLine("[ ] Välj:");
                Console.WriteLine("     [1] Show highest value");
                Console.WriteLine("     [2] Show lowest value");
                Console.WriteLine("     [3] Show average value");
                Console.WriteLine("     [4] Show all posts");
                Console.WriteLine("     [5] Add a post");
                Console.WriteLine("     [6] Delete post");            
                Console.WriteLine("     [0] Exit\n");

                choiceFminu = Console.ReadLine();
                Console.Clear();
                Console.WriteLine(choiceFminu);

                switch (choiceFminu)
                {
                    case "1":
                        Console.WriteLine("Show highest value");
                        ClientSocket.Send(Encoding.UTF8.GetBytes(choiceFminu), SocketFlags.None);
                        choiceFminu = String.Empty;
                        ReceiveResponse();
                        break;

                    case "2":
                        Console.WriteLine("Show lowest value");
                        ClientSocket.Send(Encoding.UTF8.GetBytes(choiceFminu), SocketFlags.None);
                        choiceFminu = String.Empty;
                        ReceiveResponse();
                        break;

                    case "3":
                        Console.WriteLine("Show average value");
                        ClientSocket.Send(Encoding.UTF8.GetBytes(choiceFminu), SocketFlags.None);
                        choiceFminu = String.Empty;
                        ReceiveResponse();
                        break;

                    case "4":
                        Console.WriteLine("Show all posts");
                        ClientSocket.Send(Encoding.UTF8.GetBytes(choiceFminu), SocketFlags.None);
                        ReceiveResponse();
                        break;

                    case "5":
                        Console.WriteLine("Add a post:");
                        Console.WriteLine("To add a post you need to enter the following:\n" +
                            " Location and Temperature value\n ");
                        String location, temperatureValue,addQuery = "";

                        Console.Write("Enter your Location: ");
                        location = Console.ReadLine();
                        Console.Write("Enter the emperature: ");
                        temperatureValue = Console.ReadLine();
                        addQuery = "INSERT INTO networkprogramming . clienproject " +
                            "(`id`, `location`, `sensorType`, `sesorvalue`, `unit`, `timeOfMeasure`) " +
                            "VALUES('App', '" + location + "', 'Temperatur ', '" + temperatureValue + "', 'C', '" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "')";

                        Console.Write("Enter 1 to send or 2 to cancel: ");
                        choiceFminu = Console.ReadLine();

                        if (choiceFminu == "1") 
                        {
                            ClientSocket.Send(Encoding.UTF8.GetBytes(addQuery), SocketFlags.None);
                            choiceFminu = String.Empty;                            
                        }
                        else
                            Console.WriteLine(choiceFminu + " : is invalid Choice please enter the number from the menu!");
                        break;

                    case "6":
                        Console.WriteLine("Delete post");
                        ClientSocket.Send(Encoding.UTF8.GetBytes(choiceFminu), SocketFlags.None);
                        ReceiveResponse();
                        break;

                    case "0":
                        ClientSocket.Send(Encoding.UTF8.GetBytes(choiceFminu), SocketFlags.None);
                        ClientSocket.Send(Encoding.UTF8.GetBytes(choiceFminu), SocketFlags.None);
                        ReceiveResponse();
                        break;

                    default:
                        Console.WriteLine(choiceFminu + " : is invalid Choice please enter the number from the menu!");
                        
                        break;
                }
            }           
        }
    }    
}