using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Ports;


namespace CallControl
{
    class KeyPhone_SS
    {
        public delegate void KeyPhone_SS_MessageDelegate(string sEvent, string sInfo);
        public event KeyPhone_SS_MessageDelegate OnEvent;

        private SerialPort comport = new SerialPort();

        private bool DEBUG = false;

        public string Connect(string COM_Name, bool debug)
        {
            DEBUG = debug;
            return Connect(COM_Name);
        }

        public string Connect(string COM_Name)
        {
            string result = "";
            comport.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            bool error = false;

            // If the port is open, close it.
            if (comport.IsOpen) comport.Close();

            // Set the port's settings
            comport.BaudRate = 19200;
            comport.DataBits = 8;
            comport.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "1");
            comport.Parity = (Parity)Enum.Parse(typeof(Parity), "None");
            comport.PortName = COM_Name;

            try
            {
                // Open the port
                comport.Open();
            }
            catch (UnauthorizedAccessException) { error = true; }
            catch (IOException) { error = true; }
            catch (ArgumentException) { error = true; }

            if (error)
            {
                result = "Fail";
                //MessageBox.Show(this, "CID 장치의 인식을 실패했습니다. 장치와의 연결을 확인해주시고 만약 연결되어있다면 다른프로그램에서 사용되고 있는지 확인바랍니다.", "CID장치 인식Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else
            {
                result = "Success";
            }

            // If the port is open, send focus to the send data box
            if (comport.IsOpen)
            {
                result = "Success";
            }
            else
            {
                result = "Fail";
            }
            return result;
        }

        public string disConnect()
        {
            string result = "Success";
            try
            {
                comport.Close();
            }
            catch (Exception e)
            {
                result = "Fail";
            }
            return result;
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!comport.IsOpen) return;

            // This method will be called when there is data waiting in the port's buffer

            // Read all the data waiting in the buffer
            string data = comport.ReadExisting();

            // Display the text to the user in the terminal
            //MessageBox.Show(data);
            Process_code(data);
        }

        protected void Process_code(string data)
        {
            string rtnval = "";

            if (data.Length > 3)
            {
                string Opcode = data.Substring(2, 1);
                switch (Opcode)
                {
                    case "I":
                        rtnval = data.Substring(3, 1);
                        if (rtnval.Equals("P"))
                        {
                            rtnval = "발신번호표시금지";
                        }
                        else if (rtnval.Equals("C"))
                        {
                            rtnval = "공중전화";
                        }
                        else if (rtnval.Equals("O"))  //영문 대문자 "O"
                        {
                            rtnval = "발신번호수집불가";
                        }
                        else if (rtnval.Equals("0"))    //숫자 0
                        {
                            rtnval = data.Replace(" ", "");
                            rtnval = rtnval.Substring(3, rtnval.Length - 4);
                        }
                        else
                        {
                            rtnval = "";
                        }
                        OnEvent("Ringing", rtnval);
                        break;
                    case "P":
                        break;
                    case "K":
                        OnEvent("Dialing", "");
                        break;
                    case "S":
                        break;
                    case "E":
                        break;
                }
            }
        }

        private void MakeOpcode(string sPort, string sCode, string sValue)
        {
            string trBuffer = "";
            string trTemp = "";

            trBuffer = "" + sPort + sCode + sValue;
            for (int i = 1; i < 21 - trBuffer.Length; i++)
            {
                trBuffer = trBuffer + " ";
            }
            trBuffer = trBuffer + "";

            for (int i = 1; i < 22; i++)
            {
                trTemp = trBuffer.Substring(i - 1, 1);
            }
        }
    }
}
