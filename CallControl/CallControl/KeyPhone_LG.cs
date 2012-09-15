using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Ports;

namespace CallControl
{
    class KeyPhone_LG
    {
        public delegate void KeyPhone_LG_MessageDelegate(string sEvent, string sInfo);
        public event KeyPhone_LG_MessageDelegate OnEvent;
        StreamWriter sw;
        private SerialPort comport = new SerialPort();

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
            //comport.
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
            string data = comport.ReadLine();
            //data = comport.ReadExisting();

            // Display the text to the user in the terminal
            //MessageBox.Show(data);
            Process_code(data);

            log(data);
        }

        protected void Process_code(string data)
        {
            string rtnval = "";
            //\a001 : 01033786876\r
            data = data.Substring(1, data.Length - 2);
            if (data.Length > 3)
            {
                string aa = data.Replace(" ", "");
                aa = aa.Replace("//a", "");
                aa = aa.Replace("\r\r", "");
                aa = aa.Replace("r", "");
                aa = aa.Replace("-", "");

                
                string[] bb = aa.Split(':');
                string[] cc = aa.Split('>');


                string Opcode = bb[0];
                Opcode = Opcode.Substring(0, 1);
                switch (Opcode)
                {
                    case "0":
                        if(cc.Length == 1)
                        {
                            rtnval = bb[1];
                            OnEvent("Ringing", rtnval);
                        }
                        else if (cc.Length > 1)
                        {
                            rtnval = bb[1];
                            OnEvent("Answer", rtnval);
                        }
                        else
                        {
                        }
                        
                        break;
                    case "":
                        if (cc.Length == 1)
                        {
                            rtnval = bb[1];
                            OnEvent("Ringing", rtnval);
                        }
                        else if (cc.Length > 1)
                        {
                            rtnval = bb[1];
                            OnEvent("Answer", rtnval);
                        }
                        else
                        {
                        }

                        break;
                    case "P":
                        break;
                    case "K":
                        OnEvent("Dialing", "|"+ data);
                        break;
                    case "S":
                        break;
                    case "E":
                        break;

                    default :

                        OnEvent("EventNoConfig", data);
                        break;
                }
            }
        }

        private void log(string log)
        {
            try
            {
                sw = new StreamWriter("CallControl.log", true);
                sw.WriteLine(log);
                sw.Flush();
                sw.Close();
            }
            catch (Exception ex)
            {
                
            }
        }
    }
}
