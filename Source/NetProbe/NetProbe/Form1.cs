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

                //Analyze the bytes received...
                //ParseData(byteData, nReceived);
                showData(byteData);
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
        private string showData(byte[] frame)
        {
            TreeNode rootNode = new TreeNode();
            String Data = null;
            AddTreeNode addTreeNode = new AddTreeNode(OnAddTreeNode);

            rootNode.Text = Convert.ToString(j++);
            
            for (int i = 0; i < frame.Length; i += 1)
            {
                Data += frame[i];
            }
            rootNode.Nodes.Add(Data);

            //Thread safe adding of the nodes
            treeView1.Invoke(addTreeNode, new object[] { rootNode });
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
