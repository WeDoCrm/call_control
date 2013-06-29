using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using CallControl;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        CommCtl commctl = new CommCtl();
        delegate void setTextCallback(string text);

        private void setText(string text)
        {
            if (this.richTextBox1.InvokeRequired)
            {
                setTextCallback cb = new setTextCallback(setText);
                this.Invoke(cb, new object[] { text });
            }
            else
            {
                this.richTextBox1.Text = this.richTextBox1.Text + text;
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Add(new ItemKeyValue("LG", "LG"));
            comboBox1.Items.Add(new ItemKeyValue("SS", "삼성"));
            comboBox1.Items.Add(new ItemKeyValue("SIP", "SIP"));
            comboBox1.Items.Add(new ItemKeyValue("CI1", "CID 1P"));
            comboBox1.Items.Add(new ItemKeyValue("CI2", "CID 2P"));
            comboBox1.Items.Add(new ItemKeyValue("CI4", "CID 4P"));

            commctl.OnEvent += new CommCtl.CommCtl_MessageDelegate(RecvMessage);
        }

        public void RecvMessage(string sEvent, string sInfo)
        {
            //MessageBox.Show("Event : " + sEvent + "  sInfo : " + sInfo);
            //richTextBox1.Text = richTextBox1.Text + "Event : " + sEvent + "  sInfo : " + sInfo + "\\n";
            setText("Event : " + sEvent + "  sInfo : " + sInfo + "\n");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                ItemKeyValue mList = null;
                mList = (ItemKeyValue)comboBox1.SelectedItem;
                string sel1 = mList.Key;
                string device_port = "";
                if (!sel1.Equals(null))
                {
                    if (sel1.Equals("CI1"))
                    {
                        button2.Enabled = true;
                        device_port = "COM3";
                    } else if (sel1.Equals("CI2")) {
                        button2.Enabled = true;
                        device_port = "COM3";
                    } else if (sel1.Equals("CI4")) {
                        button2.Enabled = true;
                        device_port = "COM3";
                    } else if (sel1.Equals("SIP"))
                    {
                        button2.Enabled = false;
                        device_port = "SIP";
                        //"Network adapter 'Intel(R) PRO/1000 PL Network Connection' on local host";
                    }
                    else if (sel1.Equals("LG"))
                    {
                        button2.Enabled = false;
                        device_port = "LG";
                    }
                    else if (sel1.Equals("SS"))
                    {
                        button2.Enabled = false;
                        device_port = "SS";
                    }
                    commctl.Select_Type(sel1);
                    //commctl.Connect("Network adapter 'Intel(R) PRO/1000 PL Network Connection' on local host");
                    commctl.Connect(device_port);
                }
            }
            catch (NullReferenceException ex)
            {
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            commctl.disConnect();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string sPhoneNum = "";
            sPhoneNum = "9" + textBox1.Text;
            commctl.Dial(sPhoneNum);
        }
    }

    public class ItemKeyValue
    {
        private string strKey; //키
        private string strValue; //값
     
        public ItemKeyValue(string _key, string _value)
        {
            strKey = _key;
            strValue = _value;
        }
     
        public string Value
        {
            get
            {
                return strValue;
            }
            set
            {
                strValue = value;
            }
        }
     
        public string Key
        {
            get
            {
                return strKey;
            }
            set
            {
                strKey = value;
            }
        }
        public override string ToString()
        {
            return strValue;
        }
    }
}
