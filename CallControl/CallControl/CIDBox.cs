using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Collections;
using System.Threading;

namespace CallControl
{
    class CIDBox
    {
        public delegate void CIDBox_MessageDelegate(string sEvent, string sInfo);
        public event CIDBox_MessageDelegate OnEvent;

        private SerialPort comport = new SerialPort();
        private bool hasHookOffMsg = false;
        private bool DEBUG = false;

        public CIDBox(string key)
        {
            logFileWrite("CIDBox key=" + key);

            if (key.Equals("CI1"))
            {
                hasHookOffMsg = true;
            }
            else
            {
                hasHookOffMsg = false;
            }

        }
        public string Connect(string COM_Name, bool debug)
        {
            DEBUG = debug;
            return Connect(COM_Name);
        }

        public string Connect(string COM_Name)
        {
            logFileWrite("Connect hasHookOffMsg=" + hasHookOffMsg + " COM_Name=" + COM_Name);


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

        string mData = "";
        byte[] mBuffer = new byte[1024];
        bool isFullMsg = false;
        bool isMsgStarted = false;
        const byte BYTE_CID_START = 0x02;  //""
        const byte BYTE_CID_END = 0x03;    //""
        const string DUMMY_CID_HOOKOFF = "1S";
        const string DUMMY_CID_HOOKON = "1E";
        
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!comport.IsOpen) return;

            // This method will be called when there is data waiting in the port's buffer

            // Read all the data waiting in the buffer
            //string data = comport.ReadExisting();
            int byteLen = comport.BytesToRead;
            int read = 0;
            int offset = 0;
            byte buff = 0x00;
            //logFileWrite("byteLen=" + byteLen);
            while (true)
            {
                buff = (byte)comport.ReadByte();
                //logFileWrite("read=" + read);
                //logFileWrite("buff=" + buff);
                if (buff == BYTE_CID_START) isMsgStarted = true;

                if (buff == BYTE_CID_START
                    || (buff == BYTE_CID_END && read == (byteLen - 1))
                    || (buff <= 0x7A && buff >= 0x30))
                {
                    mBuffer[offset] = buff;
                    offset++;
                    buff = 0x00;
                } else {
                    if (isMsgStarted)  //means got all message needed. not between 'z' ~ '0'
                    {
                        isFullMsg = true;
                        //logFileWrite("tread=" + read);
                        //logFileWrite("tmBuffer[read]=" + buff);
                        break;
                    }
                }
                read++;
                if (read >= byteLen)
                {
                    if (!isMsgStarted) isFullMsg = true;
                    break;
                }
            }

            //if (offset == 1 && mBuffer[0] == BYTE_CID_END)
            //{
            //    mData = DUMMY_CID_HOOKOFF;
            //}
            //else
            //{
            //    mData += System.Text.Encoding.Default.GetString(mBuffer, 0, offset);
            //}
            mData += System.Text.Encoding.Default.GetString(mBuffer, 0, offset);
            //memset
            for (int i = 0; i < mBuffer.Length; i++)
                mBuffer[i] = 0x00;

            if (isFullMsg)
            {
                // Display the text to the user in the terminal
                //MessageBox.Show(data);
                Process_code(mData);
                isFullMsg = false;
                isMsgStarted = false;
                mData = "";
            }
        }

        protected void Process_code(string data)
        {
            string rtnval = "";
            //if (DEBUG)
            logFileWrite(":"+data+":");
            if (data.Length >= 3)
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
                            rtnval = rtnval.Substring(3, rtnval.Length - 3);
                        }
                        else
                        {
                            rtnval = "";
                        }
                        logFileWrite("Ringing::" + rtnval + "\n");
                        OnEvent("Ringing", rtnval);
                        //2포트의 경우 'S','E'시그널이 없으므로 강제로 OffHook만 발생시킴
                        //CRM에서 자동으로 팝업뜨게 됨
                        if (!hasHookOffMsg)
                        {
                            Thread.Sleep(1000);
                            logFileWrite("Auto OffHook ==> 2 port \n");
                            OnEvent("OffHook", "");
                        }
                        break;
                    case "P":
                        break;
                    case "K":
                        OnEvent("Dialing", "");
                        break;
                    case "S":
                        OnEvent("OffHook", "");
                        break;
                    case "E":
                        OnEvent("OnHook", "");
                        break;
                }
            }
        }
        public void MakeOpcode(string sPort, string sCode, string sValue)
        {
            string trBuffer = "";
            //string trTemp = "";

            trBuffer = "" + sPort + sCode + sValue;
            for (int i = 1; i < 25 - trBuffer.Length; i++)
            {
                trBuffer = trBuffer + " ";
            }
            trBuffer = trBuffer + "";
            comport.WriteLine(trBuffer);
            /*
            for (int i = 1; i < 22; i++)
            {
                trTemp = trBuffer.Substring(i - 1, 1);
                comport.WriteLine
            }
            */
        }


        /// <summary>
        /// 로그파일 생성 및 쓰기
        /// </summary>
        /// <param name="_log"></param>
        public void logFileWrite(string _log)
        {
            try
            {
                try
                {
                    StreamWriter sw = new StreamWriter("CallControlDump_" + DateTime.Now.ToShortDateString() + ".log", true, Encoding.Default);
                    sw.WriteLine(_log + "[" + DateTime.Now.ToLongTimeString()  + "]");
                    sw.Flush();
                    sw.Close();
                }
                catch (Exception e)
                {
                    logFileWrite("logFileWriter() 에러 : " + e.ToString());
                }
            }
            catch (Exception exception)
            {
                logFileWrite(exception.ToString());
            }
        }
    }
}
