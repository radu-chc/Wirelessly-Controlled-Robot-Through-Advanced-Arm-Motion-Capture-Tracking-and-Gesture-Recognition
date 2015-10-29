namespace ThirdYearProject.RobotArmController.ArmInterface
{
    
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;

    public abstract class CommLine : CommBase
    {
        private byte[] RxBuffer;
        private uint RxBufferP = 0;
        private CommBase.ASCII[] RxFilter;
        private string RxString = "";
        private CommBase.ASCII RxTerm;
        private ManualResetEvent TransFlag = new ManualResetEvent(true);
        private uint TransTimeout;
        private CommBase.ASCII[] TxTerm;

        protected CommLine()
        {
        }

        protected override void OnRxChar(byte ch)
        {
            CommBase.ASCII ascii = (CommBase.ASCII) ch;
            if ((ascii == this.RxTerm) || (this.RxBufferP > this.RxBuffer.GetUpperBound(0)))
            {
                lock (this.RxString)
                {
                    this.RxString = Encoding.ASCII.GetString(this.RxBuffer, 0, (int) this.RxBufferP);
                }
                this.RxBufferP = 0;
                if (this.TransFlag.WaitOne(0, false))
                {
                    this.OnRxLine(this.RxString);
                }
                else
                {
                    this.TransFlag.Set();
                }
            }
            else
            {
                bool flag = true;
                if (this.RxFilter != null)
                {
                    for (int i = 0; i <= this.RxFilter.GetUpperBound(0); i++)
                    {
                        if (this.RxFilter[i] == ascii)
                        {
                            flag = false;
                        }
                    }
                }
                if (flag)
                {
                    this.RxBuffer[this.RxBufferP] = ch;
                    this.RxBufferP++;
                }
            }
        }

        protected virtual void OnRxLine(string s)
        {
        }

        protected void Send(string toSend)
        {
            uint byteCount = (uint) Encoding.ASCII.GetByteCount(toSend);
            if (this.TxTerm != null)
            {
                byteCount += (uint) this.TxTerm.GetLength(0);
            }
            byte[] tosend = new byte[byteCount];
            byte[] bytes = Encoding.ASCII.GetBytes(toSend);
            int index = 0;
            while (index <= bytes.GetUpperBound(0))
            {
                tosend[index] = bytes[index];
                index++;
            }
            if (this.TxTerm != null)
            {
                int num3 = 0;
                while (num3 <= this.TxTerm.GetUpperBound(0))
                {
                    tosend[index] = (byte) this.TxTerm[num3];
                    num3++;
                    index++;
                }
            }
            base.Send(tosend);
        }

        protected void Setup(CommLineSettings s)
        {
            this.RxBuffer = new byte[s.rxStringBufferSize];
            this.RxTerm = s.rxTerminator;
            this.RxFilter = s.rxFilter;
            this.TransTimeout = (uint) s.transactTimeout;
            this.TxTerm = s.txTerminator;
        }

        protected string Transact(string toSend)
        {
            this.Send(toSend);
            this.TransFlag.Reset();
            if (!this.TransFlag.WaitOne((int) this.TransTimeout, false))
            {
                base.ThrowException("Timeout");
            }
            lock (this.RxString)
            {
                return this.RxString;
            }
        }

        public class CommLineSettings : CommBase.CommBaseSettings
        {
            public CommBase.ASCII[] rxFilter;
            public int rxStringBufferSize = 0x100;
            public CommBase.ASCII rxTerminator = CommBase.ASCII.CR;
            public int transactTimeout = 500;
            public CommBase.ASCII[] txTerminator;

            public static CommLine.CommLineSettings LoadFromXML(Stream s)
            {
                return (CommLine.CommLineSettings) CommBase.CommBaseSettings.LoadFromXML(s, typeof(CommLine.CommLineSettings));
            }
        }
    }
}
