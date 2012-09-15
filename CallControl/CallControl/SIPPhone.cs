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
using System.Windows.Forms;

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
        StreamWriter sw;
        delegate void stringDele(string log);
        delegate void RingingDele(string cname, string ani);
        delegate void AbandonDele();

        Hashtable socketTable = new Hashtable();

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
                    dev.Filter = "udp port 5060";
                    dev.OnCaptureStopped += new CaptureStoppedEventHandler(dev_OnCaptureStopped);

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

        private void dev_OnCaptureStopped(object sender, CaptureStoppedEventStatus status)
        {
            try
            {
                logWriter("Capture Stopped  Cause : " + status.ToString());
            }
            catch (Exception ex)
            {
                logWriter(ex.ToString());
            }
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
            logWriter("Packet 수신!");
            Packet p = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            UdpPacket udpPacket = UdpPacket.GetEncapsulated(p);
            string data = Encoding.ASCII.GetString(udpPacket.PayloadData);
            SIPM = makeSIPConstructor(data);

            //logWriter("raw packet : " + data);

            if (SIPM.method.Equals("INVITE"))
            {
                if (SIPM.sName.Equals("session")) //Ringing
                {
                    OnEvent("Ringing", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                }
            }
            else if (SIPM.code.Equals("180"))
            {
                if (SIPM.cseq.Contains("102") && SIPM.cseq.Contains("INVITE")) //Ringing
                {
                    OnEvent("Ringing", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                }
                else if (SIPM.method.Equals("Ringing"))
                {
                    OnEvent("Ringing", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                }

            }
            else if (SIPM.code.Equals("200")) 
            {
                if (SIPM.cseq.Contains("BYE")) //통화완료
                {
                    OnEvent("HangUp", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                }

            }
            else if (SIPM.method.Equals("CANCEL")) //Abandon
            {
                OnEvent("Abandon", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                
            }
            else if (SIPM.method.Equals("BYE")) //Release
            {
                OnEvent("HangUp", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
            
            }
            
            else if (SIPM.code.Equals("183")) 
            {
                if (SIPM.sName.Equals("session")) //Dialing
                {
                    OnEvent("Dialing", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                }
            }
            else if (SIPM.cseq.Contains("ACK"))
            {
                if (!SIPM.from.Equals(SIPM.to)) //Answer
                {
                    OnEvent("Answer", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                }
            }
            else
            {
                if (!SIPM.from.Equals(SIPM.to) && SIPM.sName.Equals("session"))
                {
                    OnEvent("Ringing", SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);
                }
            }
           
        }

        private SIPMessage makeSIPConstructor(string data)
        {
            SIPM = new SIPMessage();
            
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


                StringReader sr = new StringReader(data);

                while (sr.Peek() != -1)
                {
                    string line = sr.ReadLine();
                    string Gubun = "";

                    if (line.Length > 2)
                    {
                        Gubun = line.Substring(0, 3);
                    }


                    if (Gubun.Equals("REG"))
                    {
                        break;
                    }
                    else
                    {

                        if (Gubun.Equals("SIP"))  //Status Line
                        {
                            string[] sipArr = line.Split(' ');
                            if (sipArr.Length > 0)
                            {
                                code = sipArr[1].Trim();
                                method = sipArr[2].Trim();
                                logWriter(code + " " + method);
                            }
                        }
                        else if (Gubun.Equals("INV")) //Request Line
                        {
                            method = "INVITE";
                            logWriter("method = " + method);
                        }
                        else if (Gubun.Equals("CAN"))
                        {
                            method = "CANCEL";
                            logWriter("method = " + method);
                        }
                        else if (Gubun.Equals("BYE"))
                        {
                            method = "BYE";
                            logWriter("method = " + method);
                        }
                        else if (Gubun.Equals("ACK"))
                        {
                            method = "ACK";
                            logWriter("method = " + method);
                        }
                        else
                        {
                            string[] sipArr = line.Split(':');
                            if (sipArr.Length < 2)
                            {
                                sipArr = line.Split('=');
                                if (sipArr.Length > 1)
                                {
                                    logWriter(sipArr[0] + " = " + sipArr[1]);
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
                                        logWriter("From = " + from);
                                        break;

                                    case "To":
                                        to = sipArr[2].Split('@')[0];
                                        logWriter("To = " + to);
                                        break;

                                    case "Call-ID":
                                        callid = sipArr[1].Split('@')[0];
                                        logWriter("Call-ID = " + callid);
                                        break;

                                    case "CSeq":
                                        cseq = sipArr[1].Split('@')[0];
                                        logWriter("CSeq = " + cseq);
                                        break;

                                    case "User-Agent":
                                        agent = sipArr[1].Split('@')[0];
                                        logWriter("User-Agent = " + cseq);
                                        break;

                                    default:

                                        string value = "";
                                        for (int i = 1; i < sipArr.Length; i++)
                                        {
                                            value += sipArr[i];
                                        }
                                        logWriter(key + " = " + value);

                                        break;
                                }
                            }
                        }
                    }
                }
                if (!from.Equals(to))
                {
                    logWriter("raw packet : " + data);
                }
               
                SIPM.setSIPMessage(code, method, callid, cseq, from, to, agent, sName);

            }
            catch (Exception ex)
            {
                logWriter(ex.ToString());
                
            }

            return SIPM;
        }

        private void logWriter(string log)
        {
            sw = new StreamWriter("PacketDump_" + DateTime.Now.ToShortDateString() + ".txt", true, Encoding.Default);
            sw.WriteLine("\r\n");
            sw.WriteLine(log + "  (" + DateTime.Now.ToShortTimeString() + ")");
            sw.Flush();
            sw.Close();
        }
    }
}
