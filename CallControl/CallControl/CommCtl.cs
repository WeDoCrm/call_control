using System;
using System.Collections.Generic;
using System.Text;

namespace CallControl
{
    public class CommCtl
    {
        KeyPhone_LG commType_LG;
        KeyPhone_SS commType_SS;
        SIPPhone commType_SIP;
        CIDBox commType_CID;

        public delegate void CommCtl_MessageDelegate(string sEvent, string sInfo);  //Client에 정보 전달을 위한 Delegate
        public event CommCtl_MessageDelegate OnEvent;

        //public enum COMM_TYPE : string {
        //LG_KeyPhone = "LG",
        //SS_KeyPhone = "SS",
        //    SIP_Phone = "SIP",
        //    CIP_1Port = "CI1",
        //    CID_2Port = "CI2"
        //}


        private string keys = "";
        /**
         * 교환기의 Type을 설정한다.
         * param selkeys
         */
        public void Select_Type(string selkeys)
        {
            keys = selkeys;
            if (keys.Equals("LG"))
            {
                commType_LG = new KeyPhone_LG();
                commType_LG.OnEvent += new KeyPhone_LG.KeyPhone_LG_MessageDelegate(RecvMessage);
            }
            if (keys.Equals("SS"))
            {
                commType_SS = new KeyPhone_SS();
                commType_SS.OnEvent += new KeyPhone_SS.KeyPhone_SS_MessageDelegate(RecvMessage);
            }
            if (keys.Equals("SIP"))
            {
                commType_SIP = new SIPPhone();
                commType_SIP.OnEvent += new SIPPhone.SIPPhone_MessageDelegate(RecvMessage);
            }
            if (keys.Equals("CI1") || keys.Equals("CI2"))
            {
                commType_CID = new CIDBox(keys);
                commType_CID.OnEvent += new CIDBox.CIDBox_MessageDelegate(RecvMessage);
            }
        }

        //Network adapter 'Intel(R) PRO/1000 PL Network Connection' on local hostPacket capture Start!!

        /**
         * 교환기 접속 시도
         * 성공 : Success, 실패 : Fail
         */
        public void Connect(string arg)
        {
            string result = "";
            if (keys.Equals("LG"))
            {
                result = commType_LG.Connect(arg);
            }
            if (keys.Equals("SS"))
            {
                result = commType_SS.Connect(arg);
            }
            if (keys.Equals("SIP"))
            {
                result = commType_SIP.Connect(arg);
            }
            if (keys.Equals("CI1")||keys.Equals("CI2"))
            {
                result = commType_CID.Connect(arg);
            }
            OnEvent("Connect", result);
        }

        /**
         * 교환기 접속 해제
         * 성공 : Success, 실패 : Fail
         */
        public void disConnect()
        {
            string result = "";
            if (keys.Equals("LG"))
            {
                result = commType_LG.disConnect();
            }
            if (keys.Equals("SS"))
            {
                result = commType_SS.disConnect();
            }
            if (keys.Equals("SIP"))
            {
                result = commType_SIP.disConnect();
            }
            if (keys.Equals("CI1") || keys.Equals("CI2"))
            {
                result = commType_CID.disConnect();
            }
            OnEvent("disConnect", result);
        }

        /**
         * 각 교환기에서 발생되는 이벤트 수신
         */
        public void RecvMessage(string sEvent, string sInfo)
        {
            OnEvent(sEvent, sInfo);
        }

        /**
         * 전화걸기 요청
         * CID장치일때만 가능함
         */
        public void Dial(string sBuf)
        {
            if (keys.Equals("CI1") || keys.Equals("CI2"))
            {
                commType_CID.MakeOpcode("1", "O", sBuf);
            }
        }
    }
}
