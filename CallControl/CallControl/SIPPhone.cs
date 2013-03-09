using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Ports;
using PacketDotNet;
using SharpPcap;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CallControl
{
    class SIPPhone
    {
        public delegate void SIPPhone_MessageDelegate(string sEvent, string info);
        public event SIPPhone_MessageDelegate OnEvent;

        CaptureDeviceList deviceList = null; //검색된 NIC 장치 목록
        ICaptureDevice dev = null;  //각 NIC 장치
        SIPMessage SIPM = null;  //SIP 정보 저장 클래스
        //PopForm popform = null;
        //InfoForm infoform = null;
        //SetNICForm nicform = null;
        delegate void stringDele(string log);
        delegate void RingingDele(string cname, string ani);
        delegate void AbandonDele();

        bool DEBUG = false;

        enum PhoneType { None, LG00, KTCallBox };//LG10, LG50
        PhoneType CurPhoneType = PhoneType.LG00;//PhoneType.None;  엘지로 고정2013/03/06

        const string LG_URL = "lgdacom.net";
        const string KT_URL = "kt070.co.kr";
        const string SS_URL = "samsung070.com";

        Hashtable socketTable = new Hashtable();

        public string Connect(string Device_Name, bool debug)
        {
            DEBUG = debug;
            return Connect(Device_Name);

        }

        public string Connect(string Device_Name)
        {
            int failCount = 0;
            string result = "";

            deviceList = CaptureDeviceList.Instance;
            foreach (ICaptureDevice item in deviceList)
            {
                if (item.Description.Equals(Device_Name))
                {
                    dev = item;
                    break;
                }
            }

            try
            {
                if (dev != null)
                {
                    dev.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);
                    dev.Open(DeviceMode.Promiscuous, 500);
                    dev.Filter = "udp src port 5060";

                    try
                    {
                        dev.StartCapture();
                        //log("Packet capture Start!!");
                    }
                    catch (Exception ex1)
                    {
                        //log("capture fail");
                        failCount++;

                    }
                }
            }
            catch (Exception ex)
            {
                failCount++;
                //Logwriter(ex.ToString());
            }
            if (failCount == 0)
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
                dev.Close();
            }
            catch (Exception e)
            {
                result = "Fail";
            }
            return result;
        }


        /// <summary>
        /// 설정된 NIC 디바이스의 패킷 수신 이벤트 처리
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            //log("Packet 수신!");
            Packet p = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            UdpPacket udpPacket = UdpPacket.GetEncapsulated(p);
            string data = Encoding.ASCII.GetString(udpPacket.PayloadData);
            
            SIPM = makeSIPConstructor(data);

            ParseLG10();  //엘지로 고정 2013/03/06

            //if (CurPhoneType == PhoneType.None) return;

            //if (CurPhoneType == PhoneType.LG00)
            //{
            //    ParseLG10();
            //}
            //else
            //{
            //    ParseKTCallBox();
            //}


        }

        private void ParseLG10()
        {
            if (SIPM.method.Equals("INVITE"))
            {
                if (SIPM.sName.Equals("session")) //Ringing
                {
                    logWriter("VALID_SIPM: "+SIPM.toString());
                    OnEvent("Ringing", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                }
                else if (SIPM.sName.Equals("SIP Call")) //Dial
                {
                    logWriter("VALID_SIPM: " + SIPM.toString());
                    OnEvent("Dialing", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                }
            }
            else if (SIPM.code.Equals("200")) //Answer
            {
                if (SIPM.sName.Equals("SIP Call"))
                {
                    logWriter("VALID_SIPM: " + SIPM.toString());
                    OnEvent("Answer", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                }
                else if (SIPM.sName.Equals("session")) //발신 후 연결 
                {
                    logWriter("VALID_SIPM: " + SIPM.toString());
                    OnEvent("CallConnect", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                }
            }
            else if (SIPM.method.Equals("CANCEL")) //Abandon
            {
                logWriter("VALID_SIPM: " + SIPM.toString());
                OnEvent("Abandon", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
            }
            else if (SIPM.method.Equals("BYE")) //Abandon
            {
                logWriter("VALID_SIPM: " + SIPM.toString());
                OnEvent("HangUp", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
            }
        }

        private void ParseKTCallBox()
        {
            //Ringing
            if (SIPM.code.Equals("180") && SIPM.method.Equals("Ringing") && SIPM.cseq.Contains("INVITE"))
            {
                logWriter("VALID_SIPM: " + SIPM.toString());
                OnEvent("Ringing", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                return;
            }

            if (SIPM.method.Equals("INVITE") && SIPM.sName.Equals("SIP Call")) //Dial
            {
                logWriter("VALID_SIPM: " + SIPM.toString());
                OnEvent("Dialing", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                return;
            }

            if (SIPM.code.Equals("200")) //Answer
            {
                if (SIPM.sName.Equals("SIP Call"))
                {
                    logWriter("VALID_SIPM: " + SIPM.toString());
                    OnEvent("Answer", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                }
                else if (SIPM.sName.Equals("session")) //발신 후 연결 
                {
                    OnEvent("CallConnect", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                }
            }
            
            if (SIPM.method.Equals("CANCEL")) //Abandon
            {
                logWriter("VALID_SIPM: " + SIPM.toString());
                OnEvent("Abandon", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
            }
            else if (SIPM.method.Equals("BYE")) //Abandon
            {
                logWriter("VALID_SIPM: " + SIPM.toString());
                OnEvent("HangUp", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
            }
        }


        private SIPMessage makeSIPConstructor(string data)
        {
            SIPM = new SIPMessage();
            //StreamWriter sw = new StreamWriter("PacketDump_" + DateTime.Now.ToShortDateString() + ".txt", true, Encoding.Default);
            try
            {
                string code = "Unknown";
                string method = "Unknown";
                string callid = "Unknown";
                string cseq = "Unknown";
                string from = "Unknown";
                string to = "Unknown";
                string agent = "Unknown";
                string sName = "Unknown";
                string url = "Unknown";

                if (this.DEBUG) logWriter("SIP_PACKET_DATA:" + data);

                StringReader sr = new StringReader(data);

                while (sr.Peek() != -1)
                {
                    string line = sr.ReadLine();
                    string Gubun = "";

                    if (line.Length > 2)
                    {
                        Gubun = line.Substring(0, 3);
                    }

                    //REGISTER sip:ippbx.lgdacom.net:5060;transport=udp SIP/2.0
                    //REGISTER sip:cta.kt070.co.kr;transport=udp SIP/2.0
                    if (Gubun.Equals("REG"))
                    {
                        //if (CurPhoneType != PhoneType.None)
                        //{
                        //    break;
                        //}
                        if (line.Contains(LG_URL) || line.Contains(SS_URL))
                        {
                            CurPhoneType = PhoneType.LG00;
                            logWriter("SIP_PACKET_DATA: LG Selected.:"+line);
                        }
                        else if (line.Contains(KT_URL))
                        {
                            CurPhoneType = PhoneType.KTCallBox;
                            logWriter("SIP_PACKET_DATA: KT Selected.:"+line);
                        }
                        break;
                    }
                    else
                    {
                        //폰 타입이 결정되지 않았으면 결정될때까지 처리하지 않음
                        if (CurPhoneType == PhoneType.None)
                        {
                            break;
                        }

                        if (Gubun.Equals("SIP"))  //Status Line
                        {
                            string[] sipArr = line.Split(' ');
                            if (sipArr.Length > 0)
                            {
                                code = sipArr[1].Trim();
                                method = sipArr[2].Trim();
                                if (this.DEBUG) logWriter("STATUS:"+code + " " + method);
                            }
                        }
                        else if (Gubun.Equals("INV")) //Request Line
                        {
                            method = "INVITE";
                            if (this.DEBUG) logWriter("METHOD:" + method);
                        }
                        else if (Gubun.Equals("CAN"))
                        {
                            method = "CANCEL";
                            if (this.DEBUG) logWriter("METHOD:" + method);
                        }
                        else if (Gubun.Equals("BYE"))
                        {
                            method = "BYE";
                            if (this.DEBUG) logWriter("METHOD:" + method);
                        }
                        else
                        {
                            string[] sipArr = line.Split(':');
                            if (sipArr.Length < 2)
                            {
                                sipArr = line.Split('=');
                                if (sipArr.Length > 1)
                                {
                                    if (this.DEBUG) logWriter(sipArr[0] + " = " + sipArr[1]);
                                    if (sipArr[0].Equals("s")) sName = sipArr[1];
                                }
                            }
                            else
                            {
                                string key = sipArr[0];

                                switch (key)
                                {
                                    case "From":
                                        from = sipArr[2].Split('@')[0];
                                        if (this.DEBUG) logWriter("From = " + from);
                                        break;

                                    case "To":
                                        to = sipArr[2].Split('@')[0];
                                        if (this.DEBUG) logWriter("To = " + to);
                                        break;

                                    case "Call-ID":
                                        callid = sipArr[1].Split('@')[0];
                                        if (this.DEBUG) logWriter("Call-ID = " + callid);
                                        break;

                                    case "CSeq":
                                        cseq = sipArr[1].Split('@')[0];
                                        if (this.DEBUG) logWriter("CSeq = " + cseq);
                                        break;

                                    case "User-Agent":
                                        agent = sipArr[1].Split('@')[0];
                                        if (this.DEBUG) logWriter("User-Agent = " + cseq);
                                        break;

                                    default:

                                        string value = "";
                                        for (int i = 1; i < sipArr.Length; i++)
                                        {
                                            value += sipArr[i];
                                        }
                                        if (this.DEBUG) logWriter(key + " = " + value);

                                        break;
                                }
                            }
                        }
                    }
                }
                SIPM.setSIPMessage(code, method, callid, cseq, from, to, agent, sName, url);
                if (this.DEBUG & !SIPM.code.Equals("Unknown")) logWriter(SIPM.toString());
                if (this.DEBUG) logWriter("\r\n");
                if (this.DEBUG) logWriter("###########");
                

            }
            catch (Exception ex)
            {
                //log(ex.ToString());
                //sw.Close();
            }

            return SIPM;
        }

        static StreamWriter sw;
        private void logWriter(string log)
        {
            try
            {
                sw = new StreamWriter("log\\PacketDump_" + DateTime.Now.ToShortDateString() + ".txt", true, Encoding.Default);
                //sw.WriteLine("\r\n");
                sw.WriteLine("["+DateTime.Now.ToLongTimeString() + "]:"+log);
                sw.Flush();
            }
            catch (Exception) { }
            finally { if (sw != null) { sw.Close(); } }
        }

    }

}
