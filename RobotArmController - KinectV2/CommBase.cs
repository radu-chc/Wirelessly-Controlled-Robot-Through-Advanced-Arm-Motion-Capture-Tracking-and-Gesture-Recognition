namespace ThirdYearProject.RobotArmController.ArmInterface
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Xml.Serialization;

    public abstract class CommBase : IDisposable
    {
        private bool auto = false;
        private bool checkSends = true;
        private bool dataQueued = false;
        private bool[] empty = new bool[1];
        private IntPtr hPort;
        private bool online = false;
        private IntPtr ptrUWO = IntPtr.Zero;
        private Exception rxException = null;
        private bool rxExceptionReported = false;
        private Thread rxThread = null;
        private ManualResetEvent startEvent = new ManualResetEvent(false);
        private int stateBRK = 2;
        private int stateDTR = 2;
        private int stateRTS = 2;
        private int writeCount = 0;
        private ManualResetEvent writeEvent = new ManualResetEvent(false);

        protected CommBase()
        {
        }

        protected virtual bool AfterOpen()
        {
            return true;
        }

        private string AltName(string s)
        {
            string str = s.Trim();
            if (s.EndsWith(":"))
            {
                s = s.Substring(0, s.Length - 1);
            }
            if (s.StartsWith(@"\"))
            {
                return s;
            }
            return (@"\\.\" + s);
        }

        protected virtual void BeforeClose(bool error)
        {
        }

        private bool CheckOnline()
        {
            if (!((this.rxException == null) || this.rxExceptionReported))
            {
                this.rxExceptionReported = true;
                this.ThrowException("rx");
            }
            if (this.online)
            {
                if (this.hPort != ((IntPtr)(-1)))
                {
                    return true;
                }
                this.ThrowException("Offline");
                return false;
            }
            if (this.auto && this.Open())
            {
                return true;
            }
            this.ThrowException("Offline");
            return false;
        }

        private void CheckResult()
        {
            uint nNumberOfBytesTransferred = 0;
            if (this.writeCount > 0)
            {
                if (Win32Com.GetOverlappedResult(this.hPort, this.ptrUWO, out nNumberOfBytesTransferred, this.checkSends))
                {
                    if (this.checkSends)
                    {
                        this.writeCount -= (int)nNumberOfBytesTransferred;
                        if (this.writeCount != 0)
                        {
                            this.ThrowException("Send Timeout");
                        }
                        this.writeCount = 0;
                    }
                }
                else if (Marshal.GetLastWin32Error() != 0x3e4L)
                {
                    this.ThrowException("Write Error");
                }
            }
        }

        public void Close()
        {
            if (this.online)
            {
                this.auto = false;
                this.BeforeClose(false);
                this.InternalClose();
                this.rxException = null;
            }
        }

        protected virtual CommBaseSettings CommSettings()
        {
            return new CommBaseSettings();
        }

        public void Dispose()
        {
            this.Close();
        }

        ~CommBase()
        {
            this.Close();
        }

        public void Flush()
        {
            this.CheckOnline();
            this.CheckResult();
        }

        protected ModemStatus GetModemStatus()
        {
            uint num;
            this.CheckOnline();
            if (!Win32Com.GetCommModemStatus(this.hPort, out num))
            {
                this.ThrowException("Unexpected failure");
            }
            return new ModemStatus(num);
        }

        protected QueueStatus GetQueueStatus()
        {
            Win32Com.COMSTAT comstat;
            Win32Com.COMMPROP commprop;
            uint num;
            this.CheckOnline();
            if (!Win32Com.ClearCommError(this.hPort, out num, out comstat))
            {
                this.ThrowException("Unexpected failure");
            }
            if (!Win32Com.GetCommProperties(this.hPort, out commprop))
            {
                this.ThrowException("Unexpected failure");
            }
            return new QueueStatus(comstat.Flags, comstat.cbInQue, comstat.cbOutQue, commprop.dwCurrentRxQueue, commprop.dwCurrentTxQueue);
        }

        private void InternalClose()
        {
            Win32Com.CancelIo(this.hPort);
            if (this.rxThread != null)
            {
                this.rxThread.Abort();
                this.rxThread.Join(100);
                this.rxThread = null;
            }
            Win32Com.CloseHandle(this.hPort);
            if (this.ptrUWO != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this.ptrUWO);
            }
            this.stateRTS = 2;
            this.stateDTR = 2;
            this.stateBRK = 2;
            this.online = false;
        }

        protected bool IsCongested()
        {
            bool flag;
            if (!this.dataQueued)
            {
                return false;
            }
            lock (this.empty)
            {
                flag = this.empty[0];
                this.empty[0] = false;
            }
            this.dataQueued = false;
            return !flag;
        }

        public PortStatus IsPortAvailable(string s)
        {
            IntPtr hObject = Win32Com.CreateFile(s, 0xc0000000, 0, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
            if (hObject == ((IntPtr)(-1)))
            {
                if (Marshal.GetLastWin32Error() == 5L)
                {
                    return PortStatus.unavailable;
                }
                hObject = Win32Com.CreateFile(this.AltName(s), 0xc0000000, 0, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
                if (hObject == ((IntPtr)(-1)))
                {
                    if (Marshal.GetLastWin32Error() == 5L)
                    {
                        return PortStatus.unavailable;
                    }
                    return PortStatus.absent;
                }
            }
            Win32Com.CloseHandle(hObject);
            return PortStatus.available;
        }

        protected virtual void OnBreak()
        {
        }

        protected virtual void OnRxChar(byte ch)
        {
        }

        protected virtual void OnRxException(Exception e)
        {
        }

        protected virtual void OnStatusChange(ModemStatus mask, ModemStatus state)
        {
        }

        protected virtual void OnTxDone()
        {
        }

        public bool Open()
        {
            Win32Com.DCB lpDCB = new Win32Com.DCB();
            Win32Com.COMMTIMEOUTS lpCommTimeouts = new Win32Com.COMMTIMEOUTS();
            Win32Com.OVERLAPPED structure = new Win32Com.OVERLAPPED();
            if (!this.online)
            {
                CommBaseSettings settings = this.CommSettings();
                this.hPort = Win32Com.CreateFile(settings.port, 0xc0000000, 0, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
                if (this.hPort == ((IntPtr)(-1)))
                {
                    if (Marshal.GetLastWin32Error() == 5L)
                    {
                        return false;
                    }
                    this.hPort = Win32Com.CreateFile(this.AltName(settings.port), 0xc0000000, 0, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
                    if (this.hPort == ((IntPtr)(-1)))
                    {
                        if (Marshal.GetLastWin32Error() != 5L)
                        {
                            throw new CommPortException("Port Open Failure");
                        }
                        return false;
                    }
                }
                this.online = true;
                lpCommTimeouts.ReadIntervalTimeout = uint.MaxValue;
                lpCommTimeouts.ReadTotalTimeoutConstant = 0;
                lpCommTimeouts.ReadTotalTimeoutMultiplier = 0;
                if (settings.sendTimeoutMultiplier == 0)
                {
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        lpCommTimeouts.WriteTotalTimeoutMultiplier = 0;
                    }
                    else
                    {
                        lpCommTimeouts.WriteTotalTimeoutMultiplier = 0x2710;
                    }
                }
                else
                {
                    lpCommTimeouts.WriteTotalTimeoutMultiplier = settings.sendTimeoutMultiplier;
                }
                lpCommTimeouts.WriteTotalTimeoutConstant = settings.sendTimeoutConstant;
                lpDCB.init((settings.parity == Parity.odd) || (settings.parity == Parity.even), settings.txFlowCTS, settings.txFlowDSR, (int)settings.useDTR, settings.rxGateDSR, !settings.txWhenRxXoff, settings.txFlowX, settings.rxFlowX, (int)settings.useRTS);
                lpDCB.BaudRate = settings.baudRate;
                lpDCB.ByteSize = (byte)settings.dataBits;
                lpDCB.Parity = (byte)settings.parity;
                lpDCB.StopBits = (byte)settings.stopBits;
                lpDCB.XoffChar = (byte)settings.XoffChar;
                lpDCB.XonChar = (byte)settings.XonChar;
                if (((settings.rxQueue != 0) || (settings.txQueue != 0)) && !Win32Com.SetupComm(this.hPort, (uint)settings.rxQueue, (uint)settings.txQueue))
                {
                    this.ThrowException("Bad queue settings");
                }
                if ((settings.rxLowWater == 0) || (settings.rxHighWater == 0))
                {
                    Win32Com.COMMPROP commprop;
                    if (!Win32Com.GetCommProperties(this.hPort, out commprop))
                    {
                        commprop.dwCurrentRxQueue = 0;
                    }
                    if (commprop.dwCurrentRxQueue > 0)
                    {
                        lpDCB.XoffLim = lpDCB.XonLim = (short)(((int)commprop.dwCurrentRxQueue) / 10);
                    }
                    else
                    {
                        lpDCB.XoffLim = (short)(lpDCB.XonLim = 8);
                    }
                }
                else
                {
                    lpDCB.XoffLim = (short)settings.rxHighWater;
                    lpDCB.XonLim = (short)settings.rxLowWater;
                }
                if (!Win32Com.SetCommState(this.hPort, ref lpDCB))
                {
                    this.ThrowException("Bad com settings");
                }
                if (!Win32Com.SetCommTimeouts(this.hPort, ref lpCommTimeouts))
                {
                    this.ThrowException("Bad timeout settings");
                }
                this.stateBRK = 0;
                if (settings.useDTR == HSOutput.none)
                {
                    this.stateDTR = 0;
                }
                if (settings.useDTR == HSOutput.online)
                {
                    this.stateDTR = 1;
                }
                if (settings.useRTS == HSOutput.none)
                {
                    this.stateRTS = 0;
                }
                if (settings.useRTS == HSOutput.online)
                {
                    this.stateRTS = 1;
                }
                this.checkSends = settings.checkAllSends;
                structure.Offset = 0;
                structure.OffsetHigh = 0;
                if (this.checkSends)
                {
                    structure.hEvent = this.writeEvent.Handle;
                }
                else
                {
                    structure.hEvent = IntPtr.Zero;
                }
                this.ptrUWO = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
                Marshal.StructureToPtr(structure, this.ptrUWO, true);
                this.writeCount = 0;
                this.empty[0] = true;
                this.dataQueued = false;
                this.rxException = null;
                this.rxExceptionReported = false;
                this.rxThread = new Thread(new ThreadStart(this.ReceiveThread));
                this.rxThread.Name = "CommBaseRx";
                this.rxThread.Priority = ThreadPriority.AboveNormal;
                this.rxThread.Start();
                this.startEvent.WaitOne(500, false);
                this.auto = false;
                if (this.AfterOpen())
                {
                    this.auto = settings.autoReopen;
                    return true;
                }
                this.Close();
            }
            return false;
        }

        private void ReceiveThread()
        {
            byte[] lpBuffer = new byte[1];
            bool flag = true;
            AutoResetEvent event2 = new AutoResetEvent(false);
            Win32Com.OVERLAPPED structure = new Win32Com.OVERLAPPED();
            uint num2 = 0;
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
            IntPtr ptr2 = Marshal.AllocHGlobal(Marshal.SizeOf(num2));
            structure.Offset = 0;
            structure.OffsetHigh = 0;
            structure.hEvent = event2.Handle;
            Marshal.StructureToPtr(structure, ptr, true);
            try
            {
                bool flag2;
                goto Label_03D8;
            Label_0075:
                if (!Win32Com.SetCommMask(this.hPort, 0x1fd))
                {
                    throw new CommPortException("IO Error [001]");
                }
                Marshal.WriteInt32(ptr2, 0);
                if (flag)
                {
                    this.startEvent.Set();
                    flag = false;
                }
                if (!Win32Com.WaitCommEvent(this.hPort, ptr2, ptr))
                {
                    if (Marshal.GetLastWin32Error() != 0x3e5L)
                    {
                        throw new CommPortException("IO Error [002]");
                    }
                    event2.WaitOne();
                }
                num2 = (uint)Marshal.ReadInt32(ptr2);
                if ((num2 & 0x80) != 0)
                {
                    uint num3;
                    if (!Win32Com.ClearCommError(this.hPort, out num3, IntPtr.Zero))
                    {
                        throw new CommPortException("IO Error [003]");
                    }
                    int num4 = 0;
                    StringBuilder builder = new StringBuilder("UART Error: ", 40);
                    if ((num3 & 8) != 0)
                    {
                        builder = builder.Append("Framing,");
                        num4++;
                    }
                    if ((num3 & 0x400) != 0)
                    {
                        builder = builder.Append("IO,");
                        num4++;
                    }
                    if ((num3 & 2) != 0)
                    {
                        builder = builder.Append("Overrun,");
                        num4++;
                    }
                    if ((num3 & 1) != 0)
                    {
                        builder = builder.Append("Receive Cverflow,");
                        num4++;
                    }
                    if ((num3 & 4) != 0)
                    {
                        builder = builder.Append("Parity,");
                        num4++;
                    }
                    if ((num3 & 0x100) != 0)
                    {
                        builder = builder.Append("Transmit Overflow,");
                        num4++;
                    }
                    if (num4 > 0)
                    {
                        builder.Length--;
                        throw new CommPortException(builder.ToString());
                    }
                    if (num3 != 0x10)
                    {
                        throw new CommPortException("IO Error [003]");
                    }
                    num2 |= 0x40;
                }
                if ((num2 & 1) != 0)
                {
                    uint num;
                    do
                    {
                        num = 0;
                        if (!Win32Com.ReadFile(this.hPort, lpBuffer, 1, out num, ptr))
                        {
                            int num5 = Marshal.GetLastWin32Error();
                            throw new CommPortException("IO Error [004]");
                        }
                        if (num == 1)
                        {
                            this.OnRxChar(lpBuffer[0]);
                        }
                    }
                    while (num > 0);
                }
                if ((num2 & 4) != 0)
                {
                    lock (this.empty)
                    {
                        this.empty[0] = true;
                    }
                    this.OnTxDone();
                }
                if ((num2 & 0x40) != 0)
                {
                    this.OnBreak();
                }
                uint val = 0;
                if ((num2 & 8) != 0)
                {
                    val |= 0x10;
                }
                if ((num2 & 0x10) != 0)
                {
                    val |= 0x20;
                }
                if ((num2 & 0x20) != 0)
                {
                    val |= 0x80;
                }
                if ((num2 & 0x100) != 0)
                {
                    val |= 0x40;
                }
                if (val != 0)
                {
                    uint num7;
                    if (!Win32Com.GetCommModemStatus(this.hPort, out num7))
                    {
                        throw new CommPortException("IO Error [005]");
                    }
                    this.OnStatusChange(new ModemStatus(val), new ModemStatus(num7));
                }
            Label_03D8:
                flag2 = true;
                goto Label_0075;
            }
            catch (Exception exception)
            {
                Win32Com.CancelIo(this.hPort);
                if (ptr2 != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr2);
                }
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                }
                if (!(exception is ThreadAbortException))
                {
                    this.rxException = exception;
                    this.OnRxException(exception);
                }
            }
        }

        protected void Send(byte[] tosend)
        {
            uint lpNumberOfBytesWritten = 0;
            this.CheckOnline();
            this.CheckResult();
            this.writeCount = tosend.GetLength(0);
            if (Win32Com.WriteFile(this.hPort, tosend, (uint)this.writeCount, out lpNumberOfBytesWritten, this.ptrUWO))
            {
                this.writeCount -= (int)lpNumberOfBytesWritten;
            }
            else
            {
                if (Marshal.GetLastWin32Error() != 0x3e5L)
                {
                    this.ThrowException("Send failed");
                }
                this.dataQueued = true;
            }
        }

        protected void Send(byte tosend)
        {
            byte[] buffer = new byte[] { tosend };
            this.Send(buffer);
        }

        protected void SendImmediate(byte tosend)
        {
            this.CheckOnline();
            if (!Win32Com.TransmitCommChar(this.hPort, tosend))
            {
                this.ThrowException("Transmission failure");
            }
        }

        protected void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }

        protected void ThrowException(string reason)
        {
            if (Thread.CurrentThread == this.rxThread)
            {
                throw new CommPortException(reason);
            }
            if (this.online)
            {
                this.BeforeClose(true);
                this.InternalClose();
            }
            if (this.rxException == null)
            {
                throw new CommPortException(reason);
            }
            throw new CommPortException(this.rxException);
        }

        protected bool Break
        {
            get
            {
                return (this.stateBRK == 1);
            }
            set
            {
                if (this.stateBRK <= 1)
                {
                    this.CheckOnline();
                    if (value)
                    {
                        if (Win32Com.EscapeCommFunction(this.hPort, 8))
                        {
                            this.stateBRK = 0;
                        }
                        else
                        {
                            this.ThrowException("Unexpected Failure");
                        }
                    }
                    else if (Win32Com.EscapeCommFunction(this.hPort, 9))
                    {
                        this.stateBRK = 0;
                    }
                    else
                    {
                        this.ThrowException("Unexpected Failure");
                    }
                }
            }
        }

        protected bool DTR
        {
            get
            {
                return (this.stateDTR == 1);
            }
            set
            {
                if (this.stateDTR <= 1)
                {
                    this.CheckOnline();
                    if (value)
                    {
                        if (Win32Com.EscapeCommFunction(this.hPort, 5))
                        {
                            this.stateDTR = 1;
                        }
                        else
                        {
                            this.ThrowException("Unexpected Failure");
                        }
                    }
                    else if (Win32Com.EscapeCommFunction(this.hPort, 6))
                    {
                        this.stateDTR = 0;
                    }
                    else
                    {
                        this.ThrowException("Unexpected Failure");
                    }
                }
            }
        }

        protected bool DTRavailable
        {
            get
            {
                return (this.stateDTR < 2);
            }
        }

        public bool Online
        {
            get
            {
                if (!this.online)
                {
                    return false;
                }
                return this.CheckOnline();
            }
        }

        protected bool RTS
        {
            get
            {
                return (this.stateRTS == 1);
            }
            set
            {
                if (this.stateRTS <= 1)
                {
                    this.CheckOnline();
                    if (value)
                    {
                        if (Win32Com.EscapeCommFunction(this.hPort, 3))
                        {
                            this.stateRTS = 1;
                        }
                        else
                        {
                            this.ThrowException("Unexpected Failure");
                        }
                    }
                    else if (Win32Com.EscapeCommFunction(this.hPort, 4))
                    {
                        this.stateRTS = 0;
                    }
                    else
                    {
                        this.ThrowException("Unexpected Failure");
                    }
                }
            }
        }

        protected bool RTSavailable
        {
            get
            {
                return (this.stateRTS < 2);
            }
        }

        public enum ASCII : byte
        {
            ACK = 6,
            BELL = 7,
            BS = 8,
            CAN = 0x18,
            CR = 13,
            DC1 = 0x11,
            DC2 = 0x12,
            DC3 = 0x13,
            DC4 = 20,
            DEL = 0x7f,
            EM = 0x19,
            ENQ = 5,
            EOT = 4,
            ESC = 0x1b,
            ETB = 0x17,
            ETX = 3,
            FF = 12,
            FS = 0x1c,
            GS = 0x1d,
            HT = 9,
            LF = 10,
            NAK = 0x15,
            NULL = 0,
            RS = 30,
            SI = 15,
            SO = 14,
            SOH = 1,
            SP = 0x20,
            STX = 2,
            SUB = 0x1a,
            SYN = 0x16,
            US = 0x1f,
            VT = 11
        }

        public class CommBaseSettings
        {
            public bool autoReopen = false;
            public int baudRate = 0x960;
            public bool checkAllSends = true;
            public int dataBits = 8;
            public CommBase.Parity parity = CommBase.Parity.none;
            public string port = "COM1:";
            public bool rxFlowX = false;
            public bool rxGateDSR = false;
            public int rxHighWater = 0;
            public int rxLowWater = 0;
            public int rxQueue = 0;
            public uint sendTimeoutConstant = 0;
            public uint sendTimeoutMultiplier = 0;
            public CommBase.StopBits stopBits = CommBase.StopBits.one;
            public bool txFlowCTS = false;
            public bool txFlowDSR = false;
            public bool txFlowX = false;
            public int txQueue = 0;
            public bool txWhenRxXoff = true;
            public CommBase.HSOutput useDTR = CommBase.HSOutput.none;
            public CommBase.HSOutput useRTS = CommBase.HSOutput.none;
            public CommBase.ASCII XoffChar = CommBase.ASCII.DC3;
            public CommBase.ASCII XonChar = CommBase.ASCII.DC1;

            public static CommBase.CommBaseSettings LoadFromXML(Stream s)
            {
                return LoadFromXML(s, typeof(CommBase.CommBaseSettings));
            }

            protected static CommBase.CommBaseSettings LoadFromXML(Stream s, Type t)
            {
                XmlSerializer serializer = new XmlSerializer(t);
                try
                {
                    return (CommBase.CommBaseSettings)serializer.Deserialize(s);
                }
                catch
                {
                    return null;
                }
            }

            public void SaveAsXML(Stream s)
            {
                new XmlSerializer(base.GetType()).Serialize(s, this);
            }

            public void SetStandard(string Port, int Baud, CommBase.Handshake Hs)
            {
                this.dataBits = 8;
                this.stopBits = CommBase.StopBits.one;
                this.parity = CommBase.Parity.none;
                this.port = Port;
                this.baudRate = Baud;
                switch (Hs)
                {
                    case CommBase.Handshake.none:
                        this.txFlowCTS = false;
                        this.txFlowDSR = false;
                        this.txFlowX = false;
                        this.rxFlowX = false;
                        this.useRTS = CommBase.HSOutput.online;
                        this.useDTR = CommBase.HSOutput.online;
                        this.txWhenRxXoff = true;
                        this.rxGateDSR = false;
                        break;

                    case CommBase.Handshake.XonXoff:
                        this.txFlowCTS = false;
                        this.txFlowDSR = false;
                        this.txFlowX = true;
                        this.rxFlowX = true;
                        this.useRTS = CommBase.HSOutput.online;
                        this.useDTR = CommBase.HSOutput.online;
                        this.txWhenRxXoff = true;
                        this.rxGateDSR = false;
                        this.XonChar = CommBase.ASCII.DC1;
                        this.XoffChar = CommBase.ASCII.DC3;
                        break;

                    case CommBase.Handshake.CtsRts:
                        this.txFlowCTS = true;
                        this.txFlowDSR = false;
                        this.txFlowX = false;
                        this.rxFlowX = false;
                        this.useRTS = CommBase.HSOutput.handshake;
                        this.useDTR = CommBase.HSOutput.online;
                        this.txWhenRxXoff = true;
                        this.rxGateDSR = false;
                        break;

                    case CommBase.Handshake.DsrDtr:
                        this.txFlowCTS = false;
                        this.txFlowDSR = true;
                        this.txFlowX = false;
                        this.rxFlowX = false;
                        this.useRTS = CommBase.HSOutput.online;
                        this.useDTR = CommBase.HSOutput.handshake;
                        this.txWhenRxXoff = true;
                        this.rxGateDSR = false;
                        break;
                }
            }
        }

        public enum Handshake
        {
            none,
            XonXoff,
            CtsRts,
            DsrDtr
        }

        public enum HSOutput
        {
            none,
            online,
            handshake,
            gate
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ModemStatus
        {
            private uint status;
            internal ModemStatus(uint val)
            {
                this.status = val;
            }

            public bool cts
            {
                get
                {
                    return ((this.status & 0x10) != 0);
                }
            }
            public bool dsr
            {
                get
                {
                    return ((this.status & 0x20) != 0);
                }
            }
            public bool rlsd
            {
                get
                {
                    return ((this.status & 0x80) != 0);
                }
            }
            public bool ring
            {
                get
                {
                    return ((this.status & 0x40) != 0);
                }
            }
        }

        public enum Parity
        {
            none,
            odd,
            even,
            mark,
            space
        }

        public enum PortStatus
        {
            absent = -1,
            available = 1,
            unavailable = 0
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct QueueStatus
        {
            private uint status;
            private uint inQueue;
            private uint outQueue;
            private uint inQueueSize;
            private uint outQueueSize;
            internal QueueStatus(uint stat, uint inQ, uint outQ, uint inQs, uint outQs)
            {
                this.status = stat;
                this.inQueue = inQ;
                this.outQueue = outQ;
                this.inQueueSize = inQs;
                this.outQueueSize = outQs;
            }

            public bool ctsHold
            {
                get
                {
                    return ((this.status & 1) != 0);
                }
            }
            public bool dsrHold
            {
                get
                {
                    return ((this.status & 2) != 0);
                }
            }
            public bool rlsdHold
            {
                get
                {
                    return ((this.status & 4) != 0);
                }
            }
            public bool xoffHold
            {
                get
                {
                    return ((this.status & 8) != 0);
                }
            }
            public bool xoffSent
            {
                get
                {
                    return ((this.status & 0x10) != 0);
                }
            }
            public bool immediateWaiting
            {
                get
                {
                    return ((this.status & 0x40) != 0);
                }
            }
            public long InQueue
            {
                get
                {
                    return (long)this.inQueue;
                }
            }
            public long OutQueue
            {
                get
                {
                    return (long)this.outQueue;
                }
            }
            public long InQueueSize
            {
                get
                {
                    return (long)this.inQueueSize;
                }
            }
            public long OutQueueSize
            {
                get
                {
                    return (long)this.outQueueSize;
                }
            }
            public override string ToString()
            {
                StringBuilder builder = new StringBuilder("The reception queue is ", 60);
                if (this.inQueueSize == 0)
                {
                    builder.Append("of unknown size and ");
                }
                else
                {
                    builder.Append(this.inQueueSize.ToString() + " bytes long and ");
                }
                if (this.inQueue == 0)
                {
                    builder.Append("is empty.");
                }
                else if (this.inQueue == 1)
                {
                    builder.Append("contains 1 byte.");
                }
                else
                {
                    builder.Append("contains ");
                    builder.Append(this.inQueue.ToString());
                    builder.Append(" bytes.");
                }
                builder.Append(" The transmission queue is ");
                if (this.outQueueSize == 0)
                {
                    builder.Append("of unknown size and ");
                }
                else
                {
                    builder.Append(this.outQueueSize.ToString() + " bytes long and ");
                }
                if (this.outQueue == 0)
                {
                    builder.Append("is empty");
                }
                else if (this.outQueue == 1)
                {
                    builder.Append("contains 1 byte. It is ");
                }
                else
                {
                    builder.Append("contains ");
                    builder.Append(this.outQueue.ToString());
                    builder.Append(" bytes. It is ");
                }
                if (this.outQueue > 0)
                {
                    if (((this.ctsHold || this.dsrHold) || (this.rlsdHold || this.xoffHold)) || this.xoffSent)
                    {
                        builder.Append("holding on");
                        if (this.ctsHold)
                        {
                            builder.Append(" CTS");
                        }
                        if (this.dsrHold)
                        {
                            builder.Append(" DSR");
                        }
                        if (this.rlsdHold)
                        {
                            builder.Append(" RLSD");
                        }
                        if (this.xoffHold)
                        {
                            builder.Append(" Rx XOff");
                        }
                        if (this.xoffSent)
                        {
                            builder.Append(" Tx XOff");
                        }
                    }
                    else
                    {
                        builder.Append("pumping data");
                    }
                }
                builder.Append(". The immediate buffer is ");
                if (this.immediateWaiting)
                {
                    builder.Append("full.");
                }
                else
                {
                    builder.Append("empty.");
                }
                return builder.ToString();
            }
        }

        public enum StopBits
        {
            one,
            onePointFive,
            two
        }
    }
}
