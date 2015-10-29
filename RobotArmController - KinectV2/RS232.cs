namespace ThirdYearProject.RobotArmController.ArmInterface
{
    using System;   

    public class RS232 : CommBase
    {
        private byte[] buffer = new byte[0x1000];
        private int bufferIndex = 0;
        private CommBase.CommBaseSettings Settings = new CommBase.CommBaseSettings();

        public RS232(string ComPort)
        {
            this.ComPort = ComPort;
            this.Settings.baudRate = 0x2580;
        }

        public int BytesToRead()
        {
            return this.BufferIndex;
        }

        protected override CommBase.CommBaseSettings CommSettings()
        {
            return this.Settings;
        }
        /*
        public void FillBauds(ComboBox cb)
        {
            cb.Items.Clear();
            cb.Text = this.Settings.baudRate.ToString();
            cb.Items.Add("110");
            cb.Items.Add("300");
            cb.Items.Add("600");
            cb.Items.Add("1200");
            cb.Items.Add("2400");
            cb.Items.Add("4800");
            cb.Items.Add("9600");
            cb.Items.Add("19200");
            cb.Items.Add("38400");
            cb.Items.Add("57600");
            cb.Items.Add("115200");
        }

        public void FillPorts(ComboBox cb)
        {
            string str2 = "";
            for (int i = 0; i < 0x63; i++)
            {
                string s = "COM" + i.ToString() + ":";
                if (base.IsPortAvailable(s) == CommBase.PortStatus.available)
                {
                    cb.Items.Add(s);
                    if (str2 == "")
                    {
                        str2 = s;
                    }
                }
            }
            cb.Text = str2;
        }
        */
        protected override void OnRxChar(byte c)
        {
            if (this.bufferIndex >= 0x1000)
            {
                throw new Exception("OnRxChar...Buffer Overflow");
            }
            this.buffer[this.bufferIndex] = c;
            this.bufferIndex++;
        }

        public bool Read(ref string temp)
        {
            temp = "";
            if (this.BufferIndex > 0)
            {
                for (int i = 0; i < this.BufferIndex; i++)
                {
                    temp = temp + Convert.ToString(Convert.ToChar(this.buffer[i]));
                }
                this.bufferIndex = 0;
                return true;
            }
            return false;
        }

        public bool Read(ref byte[] temp)
        {
            if (this.BufferIndex > 0)
            {
                temp = new byte[this.BufferIndex];
                for (int i = 0; i < this.BufferIndex; i++)
                {
                    temp[i] = this.Buffer[i];
                }
                this.bufferIndex = 0;
                return true;
            }
            return false;
        }

        public bool Write(byte zeichen)
        {
            if (base.Online)
            {
                base.Send(zeichen);
                return true;
            }
            return false;
        }

        public bool Write(char zeichen)
        {
            if (base.Online)
            {
                base.Send(Convert.ToByte(zeichen));
                return true;
            }
            return false;
        }

        public bool Write(string zeichenkette)
        {
            if (zeichenkette.Length > 0)
            {
                for (int i = 0; i < zeichenkette.Length; i++)
                {
                    this.Write(zeichenkette[i]);
                }
                return true;
            }
            return false;
        }

        public bool Write(byte[] zeichenarray)
        {
            if (base.Online)
            {
                base.Send(zeichenarray);
                return true;
            }
            return false;
        }

        public int Baudrate
        {
            get
            {
                return this.Settings.baudRate;
            }
            set
            {
                if (!base.Online)
                {
                    switch (value)
                    {
                        case 600:
                            this.Settings.baudRate = value;
                            return;

                        case 0x4b0:
                            this.Settings.baudRate = value;
                            return;

                        case 0x960:
                            this.Settings.baudRate = value;
                            return;

                        case 110:
                            this.Settings.baudRate = value;
                            return;

                        case 300:
                            this.Settings.baudRate = value;
                            return;

                        case 0x12c0:
                            this.Settings.baudRate = value;
                            return;

                        case 0x2580:
                            this.Settings.baudRate = value;
                            return;

                        case 0x4b00:
                            this.Settings.baudRate = value;
                            return;

                        case 0x9600:
                            this.Settings.baudRate = value;
                            return;

                        case 0xe100:
                            this.Settings.baudRate = value;
                            return;

                        case 0x1c200:
                            this.Settings.baudRate = value;
                            return;
                    }
                    throw new Exception("Illegal Baudrate: " + value.ToString());
                }
                //MessageBox.Show("NO CHANGE WHILE PORT IS OPEN");
            }
        }

        public byte[] Buffer
        {
            get
            {
                this.bufferIndex = 0;
                return this.buffer;
            }
        }

        public int BufferIndex
        {
            get
            {
                return this.bufferIndex;
            }
        }

        public string ComPort
        {
            get
            {
                return this.Settings.port;
            }
            set
            {
                if (!base.Online)
                {
                    this.Settings.port = value;
                }
                else
                {
                    //MessageBox.Show("NO CHANGE WHILE PORT IS OPEN");
                }
            }
        }

        public bool O_BREAK
        {
            get
            {
                return base.Break;
            }
            set
            {
                if (base.Online)
                {
                    base.Break = value;
                }
            }
        }

        public bool O_DTR
        {
            get
            {
                return base.DTR;
            }
            set
            {
                if (base.DTRavailable)
                {
                    base.DTR = value;
                }
            }
        }

        public bool O_RTS
        {
            get
            {
                return base.RTS;
            }
            set
            {
                if (base.RTSavailable)
                {
                    base.RTS = value;
                }
            }
        }
    }
}
