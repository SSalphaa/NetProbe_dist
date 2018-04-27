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
        private bool bContinueCapturing = false;            //A flag to check if packets are being captured or not

        private delegate void AddTreeNode(TreeNode node);

        public Dashboard()
        {
            InitializeComponent();
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if(nodeSelect.Text == "")
            {    //Error Message if the user attempts to connect without choosing the interface's adress
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

                    //To sniff the packets, the socket has to be a raw socket, with the
                    //address family being of type internetwork, and protocol being IP
                    mainSocket = new Socket(AddressFamily.InterNetwork,
                        SocketType.Raw, ProtocolType.IP);

                    //Bind the socket to the selected IP address in the combobox
                    mainSocket.Bind(new IPEndPoint(IPAddress.Parse(nodeSelect.Text), 0));

                    //Set the socket  options
                    mainSocket.SetSocketOption(SocketOptionLevel.IP,            //Applies only to IP packets
                                               SocketOptionName.HeaderIncluded, //Set the include the header
                                               true);                           //option to true

                    byte[] byTrue = new byte[4] { 1, 0, 0, 0 };
                    byte[] byOut = new byte[4] { 1, 0, 0, 0 }; //Capture outgoing packets

                    //Activation of the reception of all packets on the selected network, the user has to be an administrator on
                    mainSocket.IOControl(IOControlCode.ReceiveAll,              //the local machine
                                                                                
                                         byTrue,
                                         byOut);

                    //Start receiving the packets asynchronously
                    mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                        new AsyncCallback(OnReceive), null);
                }
                else
                {
                    btnConnect.Text = "Connect";
                    //To stop capturing the packets close the socket
                    bContinueCapturing = false;
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
                int nReceived = mainSocket.EndReceive(ar); //The number of bytes received in the socket

                //Analyze the received packet...
                ParseData(byteData, nReceived);
                
                if (bContinueCapturing)
                {
                    byteData = new byte[4096];

                    //Another call to BeginReceive to pursur receiving the incoming packets while the 
                    //received one is being parsed
                    mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                        new AsyncCallback(OnReceive), null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "NetProbe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ParseData(byte[] byteData, int nReceived)
        {
            TreeNode rootNode = new TreeNode();

            //Since all protocol packets are encapsulated in the IP datagram,
            //we start parsing the IP header and see what protocol data is being carried by it.
            IPHeader ipHeader = new IPHeader(byteData, nReceived);

            TreeNode ipNode = MakeIPTreeNode(ipHeader);
            rootNode.Nodes.Add(ipNode);

            //Now according to the protocol being carried by the IP datagram we parse 
            //the data field of the datagram.
            //For instance we will focus on TCP Protocol as it is the one used by VS in U-Test
            switch (ipHeader.ProtocolType)
            {
                case Protocol.TCP:

                    TCPHeader tcpHeader = new TCPHeader(ipHeader.Data,              //IPHeader.Data stores the data being 
                                                                                    //carried by the IP datagram
                                                        ipHeader.MessageLength); //Length of the data field                    

                    TreeNode tcpNode = MakeTCPTreeNode(tcpHeader);

                    rootNode.Nodes.Add(tcpNode);

                    break;
                case Protocol.Unknown:
                    break;
            }

            AddTreeNode addTreeNode = new AddTreeNode(OnAddTreeNode);

            rootNode.Text = ipHeader.SourceAddress.ToString() + "-" + //Setting the Text for the rootnode (datagram node)
            ipHeader.DestinationAddress.ToString();

            //Thread safe adding of the nodes
            treeView1.Invoke(addTreeNode, new object[] { rootNode });
        }

        //Function which returns the information contained in the IP header as a
        //tree node
        private TreeNode MakeIPTreeNode(IPHeader ipHeader)
        {
            TreeNode ipNode = new TreeNode();
            //For readability purposes, some fields are masked at execution
            ipNode.Text = "IP";
            ipNode.Nodes.Add("Ver: " + ipHeader.Version);
            ipNode.Nodes.Add("Header Length: " + ipHeader.HeaderLength);
            //ipNode.Nodes.Add ("Differntiated Services: " + ipHeader.DifferentiatedServices);
            ipNode.Nodes.Add("Total Length: " + ipHeader.TotalLength);
            //ipNode.Nodes.Add("Identification: " + ipHeader.Identification);
            ipNode.Nodes.Add("Flags: " + ipHeader.Flags);
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
        //Function which returns the information contained in the TCP header as a
        //tree node
        private TreeNode MakeTCPTreeNode(TCPHeader tcpHeader)
        {
            TreeNode tcpNode = new TreeNode();

            tcpNode.Text = "TCP";
            //For readability purposes, some fields are masked at execution
            tcpNode.Nodes.Add("Source Port: " + tcpHeader.SourcePort);
            tcpNode.Nodes.Add("Destination Port: " + tcpHeader.DestinationPort);
            //tcpNode.Nodes.Add("Sequence Number: " + tcpHeader.SequenceNumber);

            if (tcpHeader.AcknowledgementNumber != "")
                tcpNode.Nodes.Add("Acknowledgement Number: " + tcpHeader.AcknowledgementNumber);

            tcpNode.Nodes.Add("Header Length: " + tcpHeader.HeaderLength);
            tcpNode.Nodes.Add("Flags: " + tcpHeader.Flags);
            tcpNode.Nodes.Add("Window Size: " + tcpHeader.WindowSize);
            //tcpNode.Nodes.Add("Checksum: " + tcpHeader.Checksum);

            //if (tcpHeader.UrgentPointer != "")
            //    tcpNode.Nodes.Add("Urgent Pointer: " + tcpHeader.UrgentPointer);
            tcpNode.Nodes.Add("Data: " + extractTCPData(tcpHeader));

            return tcpNode;
        }

        //Function to extract the data from the IP Data bytes Array and converting them into String
        private string extractIPData(IPHeader ipHeader)
        {
            String Data = null;
            for (int i = 0; i < ipHeader.MessageLength; i += 1)
            {
                Data += ipHeader.Data[i];
            }
            return Data;
        }
        //Function to the data from the TCP Data bytes Array and converting them into String
        private string extractTCPData(TCPHeader tcpHeader)
        {
            String Data = null;
            for (int i = 0; i < tcpHeader.MessageLength; i += 1)
            {
                Data += tcpHeader.Data[i];
            }
            return Data;
        }

        private void OnAddTreeNode(TreeNode node)
        {
            treeView1.Nodes.Add(node);
        }
        //As long as the Dashboard is active, we look after active IP nodes and insert them in the combo box
        private void Dashboard_Activate(object sender, EventArgs e)
        { 
            string strIP = null;

            IPHostEntry HosyEntry = Dns.GetHostEntry((Dns.GetHostName()));//Getting localhost info
            if (HosyEntry.AddressList.Length > 0) //Getting IP address list from the localhost server
            {
                foreach (IPAddress ip in HosyEntry.AddressList)
                {
                    strIP = ip.ToString();
                    if (!nodeSelect.Items.Contains(strIP)){ 
                         nodeSelect.Items.Add(strIP);
                    }
                }
            }
            //As the list from the local host doesn't contain it's address, we give it manually
            //that's the one U-test uses
            nodeSelect.Items.Add("127.0.0.1");
        }

        private void Dashboard_FormClosing(object sender, CancelEventArgs e)
        {
            if(MessageBox.Show("You are about to close the Dashboard...Do you Confirm?", "NetProbe :: Dashboard",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (bContinueCapturing)
                {
                    //Close the socket if not closed before shutting off the execution window
                    mainSocket.Close();
                }
                //Search for active form (Parent Form : Mainview)
                Form m = Form.ActiveForm;
                //Set Parent Form's controls visible and enabled
                foreach (Control c in m.Controls)
                {
                    c.Visible = true;
                    c.Enabled = true;
                }
            }
            else
                e.Cancel = true;
        }
    }
}
