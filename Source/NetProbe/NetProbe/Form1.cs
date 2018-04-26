using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;

namespace NetProbe
{
    public enum Protocol
    {
        TCP = 6,
        UDP = 17,
        Unknown = -1
    }

    public partial class Dashboard : Form
    {
        private Socket mainSocket;                          //The socket which captures all incoming packets
        private byte[] byteData = new byte[4096];
        private bool bContinueCapturing = false;            //A flag to check if packets are to be captured or not
        private int j = 0;

        private delegate void AddTreeNode(TreeNode node);

        public Dashboard()
        {
            InitializeComponent();
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if(nodeSelect.Text == "")
            {
                MessageBox.Show("Select an Interface to capture the packets.", "NetProbe",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                if (!bContinueCapturing)
                {
                    //Start capturing the packets...

                    btnConnect.Text = "Disconnect";

                    bContinueCapturing = true;

                    //For sniffing the socket to capture the packets has to be a raw socket, with the
                    //address family being of type internetwork, and protocol being IP
                    mainSocket = new Socket(AddressFamily.InterNetwork,
                        SocketType.Raw, ProtocolType.IP);

                    //Bind the socket to the selected IP address
                    mainSocket.Bind(new IPEndPoint(IPAddress.Parse(nodeSelect.Text), 0));

                    //Set the socket  options
                    mainSocket.SetSocketOption(SocketOptionLevel.IP,            //Applies only to IP packets
                                               SocketOptionName.HeaderIncluded, //Set the include the header
                                               true);                           //option to true

                    byte[] byTrue = new byte[4] { 1, 0, 0, 0 };
                    byte[] byOut = new byte[4] { 1, 0, 0, 0 }; //Capture outgoing packets

                    //Socket.IOControl is analogous to the WSAIoctl method of Winsock 2
                    mainSocket.IOControl(IOControlCode.ReceiveAll,              //Equivalent to SIO_RCVALL constant
                                                                                //of Winsock 2
                                         byTrue,
                                         byOut);

                    //Start receiving the packets asynchronously
                    mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                        new AsyncCallback(OnReceive), null);
                }
                else
                {
                    btnConnect.Text = "Connect";
                    bContinueCapturing = false;
                    //To stop capturing the packets close the socket
                    mainSocket.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "NetProbe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                int nReceived = mainSocket.EndReceive(ar);

                //showData(byteData); Function used to show brut frames now parsing data to extract IP Header info

                //Analyze the bytes received...
                ParseData(byteData, nReceived);
                
                if (bContinueCapturing)
                {
                    byteData = new byte[4096];

                    //Another call to BeginReceive so that we continue to receive the incoming
                    //packets
                    mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                        new AsyncCallback(OnReceive), null);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "NetProbe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ParseData(byte[] byteData, int nReceived)
        {
            TreeNode rootNode = new TreeNode();

            //Since all protocol packets are encapsulated in the IP datagram
            //so we start by parsing the IP header and see what protocol data
            //is being carried by it
            IPHeader ipHeader = new IPHeader(byteData, nReceived);

            TreeNode ipNode = MakeIPTreeNode(ipHeader);
            rootNode.Nodes.Add(ipNode);

            AddTreeNode addTreeNode = new AddTreeNode(OnAddTreeNode);

            rootNode.Text = ipHeader.SourceAddress.ToString() + "-" +
            ipHeader.DestinationAddress.ToString();

            //Thread safe adding of the nodes
            treeView1.Invoke(addTreeNode, new object[] { rootNode });
        }

        //Helper function which returns the information contained in the IP header as a
        //tree node
        private TreeNode MakeIPTreeNode(IPHeader ipHeader)
        {
            TreeNode ipNode = new TreeNode();

            ipNode.Text = "IP";
            ipNode.Nodes.Add("Ver: " + ipHeader.Version);
            ipNode.Nodes.Add("Header Length: " + ipHeader.HeaderLength);
            //ipNode.Nodes.Add ("Differntiated Services: " + ipHeader.DifferentiatedServices);
            ipNode.Nodes.Add("Total Length: " + ipHeader.TotalLength);
            //ipNode.Nodes.Add("Identification: " + ipHeader.Identification);
            //ipNode.Nodes.Add("Flags: " + ipHeader.Flags);
            // ipNode.Nodes.Add("Fragmentation Offset: " + ipHeader.FragmentationOffset);
            //ipNode.Nodes.Add("Time to live: " + ipHeader.TTL);
            switch (ipHeader.ProtocolType)
            {
                case Protocol.TCP:
                    ipNode.Nodes.Add("Protocol: " + "TCP");
                    break;
                case Protocol.UDP:
                    ipNode.Nodes.Add("Protocol: " + "UDP");
                    break;
                case Protocol.Unknown:
                    ipNode.Nodes.Add("Protocol: " + "Unknown");
                    break;
            }
            //ipNode.Nodes.Add("Checksum: " + ipHeader.Checksum);
            ipNode.Nodes.Add("Source: " + ipHeader.SourceAddress.ToString());
            ipNode.Nodes.Add("Destination: " + ipHeader.DestinationAddress.ToString());
            ipNode.Nodes.Add("Data: " + extractIPData(ipHeader));

            return ipNode;
        }
        //Extracting the data from the IP Data bytes Array and converting them into String
        private string extractIPData(IPHeader ipHeader)
        {
            String Data = null;
            for (int i = 0; i < ipHeader.MessageLength; i += 1)
            {
                Data += ipHeader.Data[i];
            }
            return Data;
        }
        private void OnAddTreeNode(TreeNode node)
        {
            treeView1.Nodes.Add(node);
        }

        private void Dashboard_Activate(object sender, EventArgs e)
        {
            string strIP = null;

            IPHostEntry HosyEntry = Dns.GetHostEntry((Dns.GetHostName()));
            if (HosyEntry.AddressList.Length > 0)
            {
                foreach (IPAddress ip in HosyEntry.AddressList)
                {
                    strIP = ip.ToString();
                    if (!nodeSelect.Items.Contains(strIP)){ 
                         nodeSelect.Items.Add(strIP);
                    }
                }
            }
            nodeSelect.Items.Add("127.0.0.1");
        }

        private void Dashboard_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bContinueCapturing)
            {
                mainSocket.Close();
            }
        }

    }
}
