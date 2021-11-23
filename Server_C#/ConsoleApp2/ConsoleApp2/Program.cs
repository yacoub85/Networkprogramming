/*
 * Projectwork by Yacoub Gerges, IOT19
 * Course: NÃĪtvÃĪrksprogrammering 20 YHP
 * Teacher: Aksel Forsberg
 * Program: Visual Studio CE 2019 and MySQL CE 8.0 including Workbench
 * Language: Server: C#
 *           Application-client: C#
 *           Arduino-client (simulation): C++
 *           
 * This is the serverside in a solution where I recive data via a socket from an arduino (currently simulated with a C++ client) to store in a database.
 * The solution also have another client wich acts like a application written in C#, 
 * the application sends requests to the server to show posts from database, can also add or delete a post from the application 
 * (as for now, I send full MySQL-query directly from the application wich meansI dont have any error handeling of the input to the databas,
 * I did it this way due to the timeframe. Ideally I wouldent like to showcase any MySQL-code/queries in the application).
 * I have also edited some "port-forwarding" within my routersettings to make the communication work over internet.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using MySql.Data.MySqlClient;
using System.Threading;

namespace ConsoleApp2
{
    class Program
    {
        //Class object for socket-connection
        private static Socket serverSocket;
        private static readonly List<Socket> ClientSockets = new List<Socket>();
        private const int BufferSize = 4096;
        private const int Port = 55555;
        private const string ipString = "192.168.10.221";
        private static readonly byte[] Buffer = new byte[BufferSize];
        private static bool _closing;

        static void Main(string[] args)
        {
            //Sets the title for consolwindow to "Server"
            Console.Title = "Server";

            SetupServer();
            Console.ReadLine();
            _closing = true;

            CloseAllSockets();
            Thread.Sleep(2000);

        }
        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ipString), Port));
            serverSocket.Listen(5);
            Console.WriteLine("Waiting for connection...");
            Console.WriteLine(IPAddress.Parse(ipString));

            serverSocket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server setup complete");
        }

        private static void CloseAllSockets()
        {
            foreach (Socket socket in ClientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSocket.Close();
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            if (_closing)
                return;

            Socket socket = serverSocket.EndAccept(ar);
            ClientSockets.Add(socket);
            socket.BeginReceive(Buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client connected, waiting for request...");
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        //Main-function: receiveing data via socket, MySQL-commands, menu-input from client-app, input from client-"arduino"
        private static void ReceiveCallback(IAsyncResult ar)
        {
            if (_closing)
                return;

            Socket current = (Socket)ar.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(ar);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client disconnected");
                current.Close();
                ClientSockets.Remove(current);

                return;
            }

            //Storing incoming data in the string "text", prints it in console
            string text = Encoding.UTF8.GetString(Buffer, 0, received);
            Console.WriteLine("Received Text: " + text);

            //Statement for incoming "arduino-client OR application-client". If recived string starts with "", call for function to send data to database
            if (text.StartsWith("INSERT INTO networkprogramming . clienproject") == true)
            {
                Console.WriteLine("Recived INSERT-query");
                SendToMySql(text);
                text = "";
            }
            else

            //Statement for incoming "application-client". If recived string starts with "", call for function to delete data from database
            if (text.StartsWith("DELETE FROM `networkprogramming`.`clienproject`") == true)
            {
                Console.WriteLine("Recived DELETE-query");
                DeleteFromMySQL(text);
                text = String.Empty;
            }
            else

                //Switch-Case menu to execute different tasks depending on clientrequest
                switch (text.ToLower())
                {
                    //Show highest value
                    case "1":
                        Console.WriteLine("Connecting to database to find highest value of temperature");
                        GetHighestFromMySql();
                        Console.WriteLine("Highest temp sent to client");
                        break;

                    //Show lowest value
                    case "2":
                        Console.WriteLine("Connecting to database to find lowest value of temperature");
                        GetLowestFromMySql();
                        Console.WriteLine("Lowest temp sent client");
                        break;

                    //Show average value
                    case "3":
                        Console.WriteLine("Connecting to database to find average value of temperature");
                        GetAverageFromMySql();
                        Console.WriteLine("Average temp sent to client");
                        break;

                    //Show all posts
                    case "4":
                        Console.WriteLine("Connecting to database get all posts");
                        ShowAllPosts();
                        Console.WriteLine("All posts sent to client");
                        break;

                    //Add a post
                    case "5":
                        current.Send(Encoding.UTF8.GetBytes("To add a post you need to enter the following: Location and Temperature value "));
                        
                        break;

                    //Delete post
                    case "6":
                        Console.WriteLine("Replying to client for input uid");
                        current.Send(Encoding.UTF8.GetBytes("To delete a post you need to enter a uid: "));
                        Console.WriteLine(" ");
                        break;

                    //Exit
                    case "0":
                        current.Close();
                        ClientSockets.Remove(current);
                        Console.WriteLine("Client left");
                        
                        return;
                        break;

                    //Handles the bad requests made from client
                    default:
                        Console.WriteLine("Invalid request");
                        current.Send(Encoding.UTF8.GetBytes("Bad request! \n"));
                        Console.WriteLine("Warning Sent");
                        break;
                }

            current.BeginReceive(Buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, current);

            //Declared functions for database, each one sets upp the connection (Could have been done more efficient by creating a global "struct/variable")
            //When connected -> "Try" to execute query if fail -> catch the exception, if "ok" -> run the query
            void GetHighestFromMySql()
            {
                //Sets up the authorization to the database by storing server,database,user
                //and password in a string and declareing connection and reader-objet to null
                String str = @"server=192.168.0.225;database=networkprogramming;uid=Yacoub;password=password;";
                MySqlConnection con = null;
                MySqlCommand cmd = new MySqlCommand();
                Console.WriteLine("Loggar in till databas");

                try
                {
                    con = new MySqlConnection(str);
                    String highestQuery = "SELECT MAX(sesorvalue) FROM clienproject;";
                    cmd.CommandText = highestQuery;
                    con.Open();
                    cmd.Connection = con;
                    cmd.CommandType = System.Data.CommandType.Text;

                    double highest = (Convert.ToDouble(cmd.ExecuteScalar()));
                    Console.WriteLine("Here is the highest temp: " + highest.ToString());

                    current.Send(Encoding.UTF8.GetBytes("Highest temp in database: " + highest.ToString() + "\n"));
                    Console.WriteLine("Data has sent successfully..!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Nothing");
                }
                con.Close();
            }

            void GetLowestFromMySql()
            {
                //Sets up the authorization to the database by storing server,database,user and password in a string
                //and declareing connection and reader-objet to null
                String str = @"server=192.168.0.225;database=networkprogramming;uid=Yacoub;password=password;";
                MySqlConnection con = null;
                MySqlCommand cmd = new MySqlCommand();
                Console.WriteLine("Loggar in till databas");

                try
                {
                    con = new MySqlConnection(str);
                    String lowestQuery = "SELECT MIN(sesorvalue) FROM clienproject;";
                    cmd.CommandText = lowestQuery;
                    con.Open();
                    cmd.Connection = con;
                    cmd.CommandType = System.Data.CommandType.Text;

                    double lowest = (Convert.ToDouble(cmd.ExecuteScalar()));
                    Console.WriteLine("Here is the lowest temp: " + lowest.ToString());

                    current.Send(Encoding.UTF8.GetBytes("Lowest temp in database: " + lowest.ToString() + "\n"));
                    Console.WriteLine("Data has sent successfully..!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Nothing");
                }
                con.Close();
            }

            void GetAverageFromMySql()
            {
                //Sets up the authorization to the database by storing server,database,user and password in a string and
                //declareing connection and reader-objet to null
                String str = @"server=192.168.0.225;database=networkprogramming;uid=Yacoub;password=password;";
                MySqlConnection con = null;
                MySqlCommand cmd = new MySqlCommand();
                Console.WriteLine("Loggar in till databas");

                try
                {
                    con = new MySqlConnection(str);
                    String averageQuery = "SELECT AVG(sesorvalue) AS AverageValue FROM clienproject WHERE sensorType ='Temperatur'; ";
                    cmd.CommandText = averageQuery;
                    averageQuery = "";
                    con.Open();
                    cmd.Connection = con;
                    cmd.CommandType = System.Data.CommandType.Text;
                    decimal average = (decimal)cmd.ExecuteScalar();
                    Console.WriteLine("Here is the average temp: " + average.ToString());

                    current.Send(Encoding.UTF8.GetBytes("Average temp from database: " + average.ToString() + "\n"));
                    Console.WriteLine("Data has sent successfully..!");

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Nothing");
                }
                con.Close();
            }

            void ShowAllPosts()
            {
                //Sets up the authorization to the database by storing server,database,user and password in a string
                //and declareing connection and reader-objet to null
                String str = @"server=192.168.0.225;database=networkprogramming;uid=Yacoub;password=password;";
                MySqlConnection con = null;
                MySqlDataReader reader = null;
                Console.WriteLine("Loggar in till databas");

                try
                {
                    con = new MySqlConnection(str);
                    con.Open();
                    Console.WriteLine("Connected to the MySql");
                    String query = "SELECT * FROM clienproject";

                    MySqlCommand cmd = new MySqlCommand(query, con);
                    reader = cmd.ExecuteReader();

                    string dataRetrieve = "";
                    Console.WriteLine("test " + dataRetrieve);
                    while (reader.Read())
                    {
                        dataRetrieve = dataRetrieve + (reader.GetString("uid") + " " + reader.GetString("id") + " " + reader.GetString("location") +
                            "  " + reader.GetString("sensorType") + "  " + reader.GetString("sesorvalue") + "  " + reader.GetString("unit") + "  " +
                            reader.GetString("timeOfMeasure") + "\n");
                        Console.WriteLine(reader.GetString("uid") + " " + reader.GetString("id") + " " + reader.GetString("location") +
                            "  " + reader.GetString("sensorType") + "  " + reader.GetString("sesorvalue") + "  " + reader.GetString("unit") + "  " +
                            reader.GetString("timeOfMeasure"));

                    }
                    //Sends the retieved data to the client and emptying the string
                    current.Send(Encoding.UTF8.GetBytes(dataRetrieve + "\n"));
                    dataRetrieve = String.Empty;
                    Console.WriteLine("Data has sent successfully..!");
                    reader.Close();

                }

                //Catch the exception when MySQL returns an error
                catch (MySqlException err)
                {
                    Console.Write(err);
                }

                con.Close();
            }

            void SendToMySql(String query)
            {
                String  str = @"server=192.168.0.225;database=networkprogramming;uid=Yacoub;password=password;";
                MySqlConnection con = null;
                MySqlDataReader reader = null;
                try
                {
                    con = new MySqlConnection(str);
                    Console.WriteLine("Connecting to MySql server....");
                    con.Open();
                    Console.WriteLine("Connected successfully!");

                    MySqlCommand cmd = new MySqlCommand(query, con);
                    Console.WriteLine("Sending data to MySql...");
                    reader = cmd.ExecuteReader();
                    reader.Close();
                    Console.WriteLine("Data sended successfully!");
                    query = String.Empty;
                    //current.Send(Encoding.UTF8.GetBytes("The post added successfully"));
                }

                catch (MySqlException err)
                {
                    Console.Write(err);
                }
                con.Close();
            }

            void DeleteFromMySQL(String query)
            {
                String str = @"server=192.168.0.225;database=networkprogramming;uid=Yacoub;password=password;";
                MySqlConnection con = null;
                MySqlDataReader reader = null;
                try
                {
                    con = new MySqlConnection(str);
                    con.Open();
                    Console.WriteLine("Connected to the MySql");
                    MySqlCommand cmd = new MySqlCommand(query, con);
                    reader = cmd.ExecuteReader();
                    reader.Close();
                    Console.WriteLine("Data deleted from MySQL Send");
                    
                    current.Send(Encoding.UTF8.GetBytes("The post has deleted"));
                }
                catch (MySqlException err)
                {
                    Console.Write(err);
                }
                con.Close();
            }
        }
    }
}
