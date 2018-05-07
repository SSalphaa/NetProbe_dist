using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
//using System.Net.Sockets;
using System.Net;
using U_TEST;
using VS;
using System.Collections;
using System.Linq;
//using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

namespace NetProbe
{
    public enum Protocol
    {
        TCP = 6,
        UDP = 17,
        Unknown = -1
    }

    public partial class Dashboard : Form, INotifyPropertyChanged//, IEqualityComparer<DataObserver>
    {
        //VS.Vs.getVariableController().createVariable("Test",VS.VS_Type.INTEGER);

        private bool bContinueCapturing = false;            //A flag to check if packets are being captured or not

        private string ipAddr;
        private Dictionary<string, string> dic;
        VariableController vc;
        private bool connectionOK;
        private string _infoMsg;
        private ObservableCollection<DataObserver> _listOfDataObserver;
        private const string INFORMATION_MSG = "InformationMessage";
        public int  incr=0;
        private delegate void AddTreeNode(TreeNode node);
        IControl control = IControl.create();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        }


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
                    
                    //WITHOUT SOCKETS, USING VS_LIBRARY

                    loadVariableList();

                }
                else
                {
                    btnConnect.Text = "Connect";
                    bContinueCapturing = false;
                    //To stop capturing the packets close the socket
                    
                    //mainSocket.Close(); 
                    control.disconnect();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "NetProbe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void loadVariableList() {

            //On crée un dictionnaire qui va contenir le chemin + nom (clé unique) et le mapping associé
            dic = new Dictionary<string, string>();

            vc = Vs.getVariableController();
            
            _listOfDataObserver = new ObservableCollection<DataObserver>();

            try
            {
                ipAddr = nodeSelect.Text;
                control.connect(ipAddr, 9090);
                connectionOK = true;
                InformationMessage = null;
            }
            catch (Exception ex)
            {
                //Connexion impossible
                connectionOK = false;
                InformationMessage = "Connection to RTC server isn't possible !";
                MessageBox.Show(InformationMessage+"\n"+ex.Message, "NetProbe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (connectionOK)
            {
                try
                {
                    ///Récupération de toutes les variables U-test
                    NameList listeUT = control.getVariableList();

                    if (listeUT.size() > 0)
                    {
                        for (int i = 0; i < listeUT.size(); i++)
                        {
                            ///Si la clé primaire existe déjà dans le dictionnaire alors on rajoute le mapping
                            ///Si elle n'existe pas on met un mapping vide
                            if (!dic.ContainsKey(listeUT.get(i)))
                            {
                                _listOfDataObserver.Add(createDataObserver(listeUT.get(i), "", VS_Type.INVALID, 0, "", false));
                            }
                            else
                            {
                                _listOfDataObserver.Add(createDataObserver(listeUT.get(i), "", VS_Type.INVALID, 0, dic[listeUT.get(i)].ToString(), false));
                            }
                        }
                    }
                    
                    foreach (DataObserver DaObs in _listOfDataObserver)
                    {
                        readValue3(DaObs);
                    }
                }
                catch (Exception e)
                {
                    InformationMessage = "Impossible to get the list of variables !\n" + e.ToString();
                    MessageBox.Show(InformationMessage + "\n" + e.Message, "NetProbe", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public string createDateTime(long timeStamp)
        {
            return getDateTimeWithLong(timeStamp).ToString();
        }
        public DateTime getDateTimeWithLong(long timeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            long ts = (timeStamp / 1000) + (2 * 360 * 10000);
            dtDateTime = dtDateTime.AddMilliseconds(ts);
            return dtDateTime;
        }

        private DataObserver readValue3(DataObserver oldDataObs)
        {
            DataObserver dObs = oldDataObs;
            string completeVariable = oldDataObs.PathName;
            int importOk = vc.importVariable(completeVariable);
            int typeVS = -1;
            long oldTimeStamp = oldDataObs.Timestamp;
            long timeStamp = 0;
            string value = "";
            //vc = Vs.getVariableController();
            vc.getType(completeVariable, out typeVS);
            


            if (importOk != 0 /*&& !oldDataObs.IsChanging*/)
            {

                if (dObs.PathName.Contains("Int"))
                {
                    typeVS = 1;
                }

                if (dObs.PathName.Contains("Double"))
                {
                    typeVS = 2;
                }
                //MessageBox.Show("readValue : " + completeVariable + " TYPE " + Convert.ToString(typeVS) + " VC " + Convert.ToString(importOk), "NetProbe", MessageBoxButtons.OK, MessageBoxIcon.Information);
                switch (typeVS)
                {
                    ///=================================================================================================
                    /// Si le type est égal à 1 alors c'est un entier
                    ///=================================================================================================
                    case 1:
                        dObs.Type = VS_Type.INTEGER;
                        IntegerReader intr = vc.createIntegerReader(completeVariable);
                        int valVarInt;

                        if (intr != null)
                        {
                            intr.setBlocking(1 * 200);
                            VariableState t = intr.waitForConnection();

                            if (t == VariableState.Ok)
                            {
                                intr.get(out valVarInt, out timeStamp);
                                value = valVarInt.ToString();
                            }
                        }

                        break;
                    ///=================================================================================================
                    ///=================================================================================================
                    /// Si le type est égal à 2 alors c'est un double
                    ///=================================================================================================
                    case 2:
                        dObs.Type = VS_Type.DOUBLE;
                        DoubleReader dblr = vc.createDoubleReader(completeVariable);
                        double valVarDbl;

                        if (dblr != null)
                        {
                            dblr.setBlocking(1 * 200);
                            VariableState t = dblr.waitForConnection();

                            if (t == VariableState.Ok)
                            {
                                dblr.get(out valVarDbl, out timeStamp);
                                value = valVarDbl.ToString();
                            }
                        }
                        break;
                    ///=================================================================================================
                    /*
                    case 3:
                        break;
                    ///=================================================================================================
                    /// Si le type est égal à 4 alors c'est un Vector Integer (Tableau d'entier)
                    ///=================================================================================================
                    case 4:
                        dObs.Type = VS_Type.VECTOR_INTEGER;
                        VectorIntegerReader vecIntReader = vc.createVectorIntegerReader(completeVariable);
                        IntegerVector valVarVecInt = new IntegerVector();

                        if (vecIntReader != null)
                        {
                            vecIntReader.setBlocking(1 * 200);
                            VariableState t = vecIntReader.waitForConnection();

                            if (t == VariableState.Ok)
                            {
                                vecIntReader.get(valVarVecInt, out timeStamp);
                                value = tableToString(valVarVecInt);
                            }
                        }
                        break;*/
                    ///=================================================================================================
                    default:
                        dObs.Type = VS_Type.INVALID;
                        value = "Undefined";
                        break;
                    
                }

                if (!oldDataObs.Value.Equals(value))
                {
                    dObs.Value = value;
                    dObs.ValueHasChanged = true;
                }
                else
                {
                    dObs.ValueHasChanged = false;
                }
                dObs.Timestamp = timeStamp;
                //dObs.WhenUpdated = howManyTime(oldTimeStamp, dObs.Timestamp);
                dObs.WhenUpdated = createDateTime(dObs.Timestamp);
                
                if(dObs.PathName.StartsWith("Group1/")){

                TreeNode rootNode = new TreeNode();

                rootNode.Nodes.Add("Name : " + dObs.Variable);
                rootNode.Nodes.Add("Value : " + dObs.Value);
                rootNode.Nodes.Add("Type : " + dObs.Type);
                rootNode.Nodes.Add("Timestamp : " + dObs.Timestamp);
                    
                AddTreeNode addTreeNode = new AddTreeNode(OnAddTreeNode);
                    
                rootNode.Text = "Variable " + Convert.ToString(incr);

                //Thread safe adding of the nodes
                treeView1.Invoke(addTreeNode, new object[] { rootNode });

                incr++;
                }
                
            }

            return dObs;
        }

        private void OnAddTreeNode(TreeNode node)
        {
            treeView1.Nodes.Add(node);
        }

        public string InformationMessage
        {
            get { return _infoMsg; }
            set { _infoMsg = value; OnPropertyChanged(INFORMATION_MSG); }
        }

        private DataObserver createDataObserver(string path, string value, VS_Type type, long timeStamp, string mapping, bool forced)
        {
            DataObserver dObs = new DataObserver
            {
                PathName = path,
                Path = System.IO.Path.GetDirectoryName(path).Replace("\\", "/"),
                Variable = System.IO.Path.GetFileName(path),
                Value = value,
                Type = type,
                Mapping = mapping,
                IsForced = forced,
                Timestamp = timeStamp
            };

            return dObs;
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
                    control.disconnect();
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
