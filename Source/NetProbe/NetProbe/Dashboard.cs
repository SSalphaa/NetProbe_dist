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
using System.Threading;

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

        private bool recall = false;            //A flag to check if variables are being loaded or not
        private bool enableConn = true;
        private string ipAddr;
        private Dictionary<string, string> dic;
        VariableController vc;
        private bool connectionOK;
        private string _infoMsg;
        private ObservableCollection<DataObserver> _listOfDataObserver;
        private const string INFORMATION_MSG = "InformationMessage";
        //public int  incr=0;
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
                if (enableConn)
                {
                    //Start capturing the packets...


                    //WITHOUT SOCKETS, USING VS_LIBRARY
                    connectUtest();
                    if (connectionOK)
                    {
                        btnRefresh.Enabled = true;
                    }
                }
                else
                {
                    
                    //To stop capturing the packets close the socket
                    
                    //mainSocket.Close(); 
                    control.disconnect();
                    if (!control.isConnected()) {
                        btnConnect.Text = "Connect";
                        enableConn = true;
                        btnRefresh.Enabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "NetProbe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void connectUtest()
        {
            try
            {
                ipAddr = nodeSelect.Text;
                control.connect(ipAddr, 9090);

                connectionOK = control.isConnected();
                InformationMessage = null;
            }
            catch (Exception ex)
            {
                //Connexion impossible
                connectionOK = false;
                InformationMessage = "Connection to RTC server isn't possible !";
                MessageBox.Show(InformationMessage + "\n" + ex.Message, "NetProbe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            loadVariableList();
        }

        public void loadVariableList() {

            //On crée un dictionnaire qui va contenir le chemin + nom (clé unique) et le mapping associé
            dic = new Dictionary<string, string>();

            vc = Vs.getVariableController();
            
            _listOfDataObserver = new ObservableCollection<DataObserver>();

            connectionOK = control.isConnected();

            if (connectionOK)
            {
                btnConnect.Text = "Disconnect";
                enableConn = false;
                          
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
                   
                   refreshValues();
                }
                catch (Exception e)
                {
                    InformationMessage = "Impossible to get the list of variables !\n" + e.ToString();
                    MessageBox.Show(InformationMessage + "\n" + e.Message, "NetProbe", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        public void refreshValues()
        {
            ObservableCollection<DataObserver> oldList = _listOfDataObserver;

            foreach (DataObserver Obs in oldList)
            {
                if (Obs == oldList.Last())
                    recall = true;
                else
                    recall = false;

                readValue3(Obs);
                
            }
        }

        public string createDateTime(long timeStamp)
        {
            string dateFormat = "MM/dd/yyyy HH:mm:ss:fff";

            return getDateTimeWithLong(timeStamp).ToString(dateFormat).Remove(0,11);
        }

        public DateTime getDateTimeWithLong(long timeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Local);
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
            vc.getType(completeVariable, out typeVS);
            
           
            
            if (importOk != 0 /*&& !oldDataObs.IsChanging*/)
            {
                //vc.waitForConnection(completeVariable,500);
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
                            intr.setBlocking(1 * 20);
                            //intr.waitForEventConnection();
                            VariableState t = intr.waitForFirstValue();

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
                            dblr.setBlocking(1* 20);
                            //dblr.waitForEventConnection();
                            VariableState t = dblr.waitForFirstValue();

                            if (t == VariableState.Ok)
                            {
                                dblr.get(out valVarDbl, out timeStamp);
                                value = valVarDbl.ToString();
                            }
                        }
                        break;
                    ///=================================================================================================
                    case 3:
                        break;
                    ///=================================================================================================
                    /// Si le type est égal à 4 alors c'est un Vector Integer (Tableau d'entier)
                    ///=================================================================================================
                    /*
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
                        break;
                    */
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
            }
            displayValues(dObs);
            return dObs;
        }


        private void displayValues(DataObserver dObs)
        {
            bool RowExists = false;

            if (dObs.PathName.StartsWith("Group1/")){

                if (dObs.Type != VS_Type.INVALID)
                {

                    for (int j = 0; j < dataGridView1.Rows.Count; j++)
                    {

                        if (Convert.ToString(dataGridView1.Rows[j].Cells[0].Value) == dObs.Variable)
                        {
                            dataGridView1.Rows[j].Cells[1].Value = dObs.Value;
                            dataGridView1.Rows[j].Cells[3].Value = createDateTime(dObs.Timestamp);
                            RowExists = true;
                            break;
                        }
                    };
                    if (!RowExists)
                    {
                        string[] row = new string[] { dObs.Variable, dObs.Value, Convert.ToString(dObs.Type), createDateTime(dObs.Timestamp) };
                        dataGridView1.Rows.Add(row);
                    }
                }
                else
                    readValue3(dObs);
             }
            /*if (recall) { 
                System.Threading.Thread.Sleep(2000);
                refreshValues();
            }*/
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
                if (!enableConn)
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

        private void Dashboard_Load(object sender, EventArgs e)
        {
            dataGridView1.ColumnCount = 4;
            dataGridView1.Columns[0].Name = "Variable Name";
            dataGridView1.Columns[1].Name = "Value";
            dataGridView1.Columns[2].Name = "Type";
            dataGridView1.Columns[3].Name = "Timestamp";
            btnRefresh.Enabled = false;
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            refreshValues();
        }

    }
}
