////////////////////////////////////DECLARATII/////////////////////////////////////////////////////

using Common.Logging;
using Common.Logging.Simple;
using Makaretu.Dns;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using CircularProgressBar;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.ComponentModel;

namespace Interfata
{

    public delegate void Delegat(int i, string value);

    public partial class Form1 : Form
    {
        ////////////////////////////////////DECLARATII/////////////////////////////////////////////////////

        public static BindingList<String> myList = new BindingList<String>();  //Initial list of all IP's connected to the server(both MDNS and WiFi)
        public static BindingList<String> newlist = new BindingList<String>(); //List to help clear up IP list
        static public BindingList<String> distinc = new BindingList<String>(); //List that contains distinc IP address of ESP's
        public static BindingList<String> bytes12 = new BindingList<String>(); 
        public static BindingList<String> atins = new BindingList<String>();  //List that contains if button on ESP8266 is pressed
        public static BindingList<String> yes = new BindingList<String>();    //List that contains confirmation that ESP8266 is alive
        public static BindingList<String> timp = new BindingList<String>();   //List that contains time sent from ESP8266
        BackgroundWorker worker = new BackgroundWorker();                     //Background worker that works on a different Thread
        static public DataTable ips = new DataTable();                        //Datatable for DataGridView
        Random rnd = new Random();   //Random variable to select a different ESP8266 each time with same Hash map
        private DataSet dtset;
        string nl = "\r\n";   //Tool to select only the left side of ip until :yyyy,(i.e-- 192.1608.0.1)
        Thread asculta; //New Thread to make sure i can update GUI in real time.


        ////////////////////////////////////SERVER/CLIENT WIFI)/////////////////////////////////////////////
        public class UDPSocket
        {
            Thread asculta1;

            public Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            public const int bufSize = 8 * 1024;
            public State state = new State();
            public EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
            public AsyncCallback recv = null;         /// Create a new UDPSocket 
          
            

            public class State
            {
                public byte[] buffer = new byte[bufSize];
            }

            public void Server(string address, int port)
            {
                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
                _socket.Bind(new IPEndPoint(IPAddress.Parse(address), port));   //Creates a new UDPSocket server that Clients can log in
                Receive();   //Recieve incoming messages from Clients
            }
            public void Client(string address, int port)
            {

                _socket.Connect(IPAddress.Parse(address), port);   //Create a new UDPSocket that i can send message to Clients connected
                Thread.Sleep(100);  //Sleep for making sure data is recieved corectly

            }
            public void Send(string text)    //Function to send message to Clients
            {
                byte[] data = Encoding.ASCII.GetBytes(text);
                _socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
                {
                    State so = (State)ar.AsyncState;
                    int bytes = _socket.EndSend(ar);
                    Console.WriteLine("SEND: {0}", text);
                }, state);
                Receive();
            }


            string n;
            //string ip;
            //int cont = 0;
            public void Receive()    //Function to recieve Messages from Clients
            {
                _ = _socket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
                 {
                     State so = (State)ar.AsyncState;
                     int bytes = _socket.EndReceiveFrom(ar, ref epFrom);
                     _socket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                     Console.WriteLine("RECV: {0}: {1}, {2}", epFrom.ToString(), bytes, Encoding.ASCII.GetString(so.buffer, 0, bytes));
                     n = Encoding.ASCII.GetString(so.buffer, 0, bytes);  // n is message recieved from client
                     string pat = ":\\d{1,5}";  //Tool to separate port from rest of IP
                     string output = Regex.Replace(epFrom.ToString(), pat, ""); //Output is the IP without port


                     if (n == "da") { yes.Add(n); }  //If Client sent "da" ,add to yes list as confirmation
                     if (n.Contains("baterie"))   //If message starts with "baterie" but has an aditional int that means the percentage of battery of the Client
                     {
                          //string baterie = Regex.Replace(n, pat, "");
                          for (int i = 0; i < distinc.Count; i++)  //search in DataRow the ip it has been sent from
                         {

                             if (ips.Rows[i][1].ToString() == output) //If ip == DataRow[x][y] 
                             {

                                 ips.Rows[i][5] = n; // Adds int sent from Message meaning the percentage of battery

                             }
                         }
                     }

                     if (n == "atins")    //If message == "atins"  adds atins to list ,so that the game can progress to the next Requirement
                     {
                         atins.Add(n); bytes12.Add(Encoding.ASCII.GetString(so.buffer, 0, bytes));


                     }

                     if (int.TryParse(n, out int value))  //If message is only a int that means it's the time sent from ESP8266
                     {
                         timp.Add(n);  //Add time to timp list
                         try
                         {
                             Console.WriteLine(output);  //Write the value of int in console
                             for (int i = 0; i < distinc.Count; i++)   //Goes trough how many clients are connected
                             {
                                 if (ips.Rows[i][1].ToString() == output)   //Search the Row on which the IP of sender is
                                 {
                                     ips.Rows[i][2] = n; //Replace Current time with time sent

                                     if (Int64.Parse(ips.Rows[i][4].ToString()) == 0) //For first time use only.Set the Maximum value to current value so that the next values can be replaced.
                                     {
                                         ips.Rows[i][3] = n;

                                     }

                                     if (Int64.Parse(n) > Int64.Parse(ips.Rows[i][4].ToString())) //If time sent is bigger than current Maximum time, replace value
                                     {
                                         ips.Rows[i][4] = n;
                                     }

                                     if (Int64.Parse(n) < Int64.Parse(ips.Rows[i][3].ToString())) //If time sent is lower than current Minimum time ,replace value
                                     {
                                         ips.Rows[i][3] = n;
                                     }
                                 }
                             }
                         }
                         catch { }

                         Console.WriteLine("=======================");
                     }
                 }, state);
            }
        }




        public void AppendTextBox3(string value)  //Function to Update TextBox3 from another Thread in real time
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendTextBox3), new object[] { value });
                return;
            }
            textBox3.Text += value + nl;
        }


        public void AppendTextBox2(string value)  //Function to Update TextBox2 from another Thread in real time
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendTextBox2), new object[] { value });
                return;
            }
            textBox2.Text += value + nl;

        }

        public void AppendTextBox(string value)  //Function to Update TextBox from another Thread in real time
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendTextBox), new object[] { value });
                return;
            }
            textBox5.Text += value + nl;
        }
        public void Appendlist(string value) //Function to Update ListBox1 from another Thread in real time
        {

            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(Appendlist), new object[] { value });
                return;
            }
            listBox1.Items.Add(value + nl);
        }




        private void empty(object sender, EventArgs e)  // Function to clear up ListBox1 items
        {
            listBox1.Items.Clear();
        }

        //////////////////////////////////////////INCEPUT DE FORM////////////////////////////////////////////////////////////////
        public Form1()
        {
            InitializeComponent();

            int ok = 0;           
            string nl = "\r\n";
            UDPSocket server = new UDPSocket(); //Create server Socket
            ips.Columns.Add("ID", typeof(int));  //Create ID Column for DataGridView 
            ips.Columns.Add("Adresa ip", typeof(string)); //Create IP Adress Column for DataGridView
            ips.Columns.Add("Timp Current", typeof(string)); //Create Current Time Column for DataGridView
            ips.Columns.Add("Minim", typeof(string)); //Create Minimum Column for DataGridView
            ips.Columns.Add("Maxim", typeof(string)); //Create Maximum Column for DataGridView
            ips.Columns.Add("Baterie", typeof(string)); //Create Battery percentage Column for DataGridView
            dataGridView1.DataSource = ips; //Link DataGridView to DataSource


            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    AppendTextBox(ip.ToString());
                    server.Server(ip.ToString(), 27000);

                }
            }

            /////////////////////////// MDNS DISCOVERY ///////////////////////////
            var properties = new Common.Logging.Configuration.NameValueCollection
            {
                ["level"] = "TRACE",
                ["showLogName"] = "true",
                ["showDateTime"] = "true",
                ["dateTimeFormat"] = "HH:mm:ss.fff"

            };
            var mdns = new MulticastService();
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(properties);
            var sd = new ServiceDiscovery();
            mdns.UseIpv6 = false;

            foreach (var a in MulticastService.GetIPAddresses())
            {
                AppendTextBox($"IP address {a}");

            }

            var service = new ServiceProfile("ponolulu", "_simplu1._udp", 27000); //Create new MDNS service simplu1
            var service2 = (new ServiceProfile("ponolulu", "_simplu2._udp", 27000)); //Create new MDNS service simplu2
            sd.Advertise(service); //Advertise service 
            sd.Advertise(service2); //Advertise service


            mdns.QueryReceived += (s, e) =>  //On query Recieved(response to service Advertise)

            {
                var msg = e.Message; //Save message recieved in "msg"
                var names = e.Message.Questions
                .Select(q => q.Name + " " + q.Type);


                if (String.Join(" ", names).Contains("simplu1") || String.Join(" ", names).Contains("simplu2")) //Test to check if service discovered is the one we need
                {
                    // AppendTextBox("Clients connected with IP:" + e.RemoteEndPoint.ToString());
                    int contor = 0;
                    myList.Add(e.RemoteEndPoint.ToString());  //Adds Clients connected to mylist

                    foreach (String v in myList)  //Function to remove port from ip of client
                    {
                        int pos = v.IndexOf(":");
                        if (pos < 0) continue;
                        newlist.Add(v.Substring(0, pos)); //Add Client ip without port in a new list

                    }

                    foreach (String v in newlist)

                        if (distinc.Contains(v) == false)  //Function to check if IP we try to add to distinc list is already there
                        {
                            // distinc.Clear();
                            distinc.Add(v);  //If not in newlist,add to distinc list
                            try
                            {
                                listBox1.Items.Clear(); //Clear ListBox to have only the new ones showing
                            }
                            catch { }

                        }
                    try
                    {

                        ips.Clear();   //Clear DataGridView database to show only the connected clients
                        dataGridView1.DataSource = null; //Remove database

                        dataGridView1.DataSource = ips; //Add datasource again to be sure there are only connected and distinc Clients

                    }
                    catch { }

                    int h = 0;
                    int g = 0;

                    asculta = new Thread(() =>

                    {

                        g = 0;
                        int x = 1;
                        int y = 0;

                        for (int j = 0; j < distinc.Count(); j++)  //Function to add Clients to GUI
                        {

                        //AppendTextBox(distinc.Count().ToString());
                        //Appendlist(distinc[j].ToString());

                        Invoke((MethodInvoker)delegate
                        {

                                    try
                                    {
                                        ips.Rows.Add(h++, distinc[j].ToString(), 0.ToString(), 0.ToString(), 0.ToString());

                                        if (!panel2.Visible || (panel2.Visible && label4.Text.Contains("Disconnected")))
                                        {
                                            panel2.Visible = true;
                                            label2.Text = "Client: " + g++;
                                            label3.Text = "IP:" + distinc[j].ToString();
                                            label4.Text = "Status:" + "Connected";
                                        }
                                        if (((panel4.Visible && panel2.Visible) && (!panel3.Visible)) && (!label3.Text.Contains(distinc[j].ToString()) && !label6.Text.Contains(distinc[j].ToString())) || (panel3.Visible && label9.Text.Contains("Disconnected")))
                                        {
                                            if (y % 2 == 0)
                                            {
                                                panel3.Visible = true;
                                                label8.Text = "Client: " + g++;
                                                label9.Text = "IP:" + distinc[j].ToString();
                                                label10.Text = "Status:" + "Connected";
                                            }
                                        }

                                        if (panel2.Visible && !panel4.Visible || (panel4.Visible && label7.Text.Contains("Disconnected")))
                                        {
                                            if (x % 2 == 0)
                                            {
                                                panel4.Visible = true;
                                                label5.Text = "Client: " + g++;
                                                label6.Text = "IP:" + distinc[j].ToString();
                                                label7.Text = "Status:" + "Connected";
                                            }
                                        }

                                        x++;
                                        y++;


                                    }
                                    catch { }
                                });
                        }
                    });
                    asculta.Start();
                }
            };

            mdns.AnswerReceived += (s, e) =>
            {
                var names = e.Message.Answers

                    .Select(q => q.Name + " " + q.Type)
                    .Distinct();
                // AppendTextBox($"got answer for {String.Join(", ", names)}");
            };
            mdns.NetworkInterfaceDiscovered += (s, e) =>
            {
                foreach (var nic in e.NetworkInterfaces)
                {
                    // textBox1.AppendText($"discovered NIC '{nic.Name}'");

                }

            };

            mdns.Start();

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            label1.Visible = false;
            label14.Visible = false;
            panel2.Visible = false;
            panel4.Visible = false;
            panel3.Visible = false;
            panel5.Visible = false;
            textBox2.Visible = false;
            textBox3.Visible = false;
            dataGridView1.Visible = false;
            listBox1.Visible = false;
            textBox1.Visible = false;
            textBox5.Visible = false;
            textBox4.Visible = false;
        }

        int kontor = 0;
        private async void button1_Click(object sender, EventArgs e) //If button1 Pressed Show Settings elements 
        {
            kontor++;
            if (kontor % 2 != 0)
            {
                label1.Visible = true;
                label14.Visible = true;
                textBox2.Visible = true;
                textBox3.Visible = true;
                dataGridView1.Visible = true;
                textBox5.Visible = true;


            }
            else                              //If Button pressed again,Hide settings elements
            {
                label1.Visible = false;
                label14.Visible = false;
                textBox2.Visible = false;
                textBox3.Visible = false;
                dataGridView1.Visible = false;
                textBox5.Visible = false;
            }



        }
        int ok = 0;
        int okk = 0;
        int cont = 0;
        int stop = 0;
        long TimpRamas = 0;
        int y = 0;
        public void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

            int contorPierdut = 0;
            ok = 0;
            int timpReactie = 0;
            Int32.TryParse(textBox3.Text, out timpReactie);
            int repetari = 0;
            Int32.TryParse(textBox2.Text, out repetari);


            while (ok < repetari)
            {

                UDPSocket c = new UDPSocket();
                int a = rnd.Next(distinc.Count);
                c.Client(distinc[a], 27000);
                Console.WriteLine(distinc[a]);

                var waitwatch = Stopwatch.StartNew();
                //TimpRamas !=
                Thread.Sleep(40);
                while (waitwatch.ElapsedMilliseconds-40<timpReactie)
                {

                }

                c.Send("HIGH");
                var watch = Stopwatch.StartNew();
                atins.Clear();
                while (!atins.Contains("atins"))
                {


                    if (watch.ElapsedMilliseconds > timpReactie)
                    {
                        Console.WriteLine(watch.ElapsedMilliseconds);

                        c.Send("TimpPierdut");
                        Thread.Sleep(40);

                        ok++;
                        AppendTextBox("am atins" + y++);
                        contorPierdut = 1;
                        Thread.Sleep(40);
                        break;

                    }

                    else if (watch.ElapsedMilliseconds < timpReactie)
                    {
                        try
                        {

                            Thread.Sleep(40);
                            if (atins.Contains("atins"))
                            {

                                ok++;
                                AppendTextBox("am atins" + y++);
                                TimpRamas = timpReactie - watch.ElapsedMilliseconds;
                            }
                        }
                        catch { }
                    }


                }

                atins.Clear();

            }

            stop = 1;
            /*
            if (stop == 1)
            {
                for (int i = 0; i < distinc.Count; i++)
                {
                    UDPSocket c = new UDPSocket();
                    c.Client(distinc[i], 27000);
                    c.Send("stop");
                }
            }
            backgroundWorker1.Dispose();
            */

        }




        private async void button2_Click(object sender, EventArgs e)
        {
            /*
            for (int i = 0; i < distinc.Count; i++)
            {
                yes.Clear();
                UDPSocket c = new UDPSocket();
                c.Client(distinc[i], 27000);
                c.Send("estiaici?");
                await Task.Delay(5000);
                if (!yes.Contains("da"))
                {
                    await Task.Delay(500);
                    try
                    {
                        for (int j = 0; j < distinc.Count; j++)
                        {
                            if (yes.Contains("da") == false && ips.Rows[j][1].ToString() == distinc[i])
                            {
                                AppendTextBox("am trimis mesaj la" + distinc[i]);
                                try
                                {
                                    if (panel2.Visible && label3.Text.Contains(distinc[i]))
                                    {
                                        panel2.Visible = true;
                                        label2.Text = "Client: ";
                                        label3.Text = "IP:";
                                        label4.Text = "Status:" + "Disconnected";
                                    }
                                    if (panel4.Visible && label6.Text.Contains(distinc[i]))
                                    {
                                        panel4.Visible = true;
                                        label5.Text = "Client: ";
                                        label6.Text = "IP:";
                                        label7.Text = "Status:" + "Disconnected";

                                    }
                                    if(panel3.Visible  && label9.Text.Contains(distinc[i]))
                                    {
                                        panel3.Visible = true;
                                        label5.Text = "Client: ";
                                        label6.Text = "IP:";
                                        label7.Text = "Status:" + "Disconnected";
                                    }

                                    ips.Rows.RemoveAt(j);

                                }
                                catch { }
                                distinc.Remove(distinc[i]);
                            }
                        }
                    }
                    catch { }
                }
                }

          */
            if (!String.IsNullOrEmpty(textBox2.Text) && !String.IsNullOrEmpty(textBox3.Text))  //If TextBox of settings are different than Empty or Null -Start can be pressed
                for (int i = 0; i < distinc.Count; i++)
                {
                    stop = 0;
                    UDPSocket c = new UDPSocket();
                    c.Client(distinc[i], 27000);
                    c.Send("start");

                }
            backgroundWorker1.RunWorkerAsync();
        }

    }
}
