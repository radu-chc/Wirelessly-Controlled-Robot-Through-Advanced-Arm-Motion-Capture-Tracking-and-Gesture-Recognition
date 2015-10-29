using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ThirdYearProject.RobotArmController.ArmInterface
{

    using System.Threading;
    class RoboticArm
    {
        RS232 COMPORT = new RS232("COM5:");
        DispatcherTimer timer1;


        public RoboticArm()
        {
            InitializerTimer();
        }

        byte[] ArmPosition = new byte[6];
        private byte status = 0;

        public void InitializerTimer()
        {
            timer1 = new System.Windows.Threading.DispatcherTimer();
            timer1.Tick += new EventHandler(Timer1Tick);
            timer1.Interval = new TimeSpan(0, 0, 0, 0, 300);
        }


        public void SetInitialPosition()
        {
            for (int i = 0; i < 6; i++)
                ArmPosition[i] = 100;

            sendPositions();
        }


        public void SetGripperValue(byte value)
        {
            ArmPosition[5] = value;
        }

        public void SetTopRotatorValue(byte value)
        {
            ArmPosition[4] = value;
        }

        public void SetTopFlexerValue(byte value)
        {
            ArmPosition[3] = value;
        }

        public void SetMiddleFlexerValue(byte value)
        {
            ArmPosition[2] = value;
        }

        public void SetLowerFlexerValue(byte value)
        {
            ArmPosition[1] = value;
        }

        public void SetLowerRotatorValue(byte value)
        {
            ArmPosition[0] = value;
        }


        public void sendPositions()
        {
            if (true)//this.checkBox_port.Checked)
            {
                this.timer1.Stop();
                string temp = "";
                this.COMPORT.Write((byte)20);
                Thread.Sleep(200);
                if (this.COMPORT.Read(ref temp))
                {
                    this.COMPORT.Write(ArmPosition);
                    Thread.Sleep(5);
                }
                this.timer1.Start();
            }
        }

        public void send1Pos(int num, byte value)
        {
            if (true)//this.checkBox_port.Checked)
            {
                this.timer1.Stop();
                this.COMPORT.Write((byte)(20 + num));
                Thread.Sleep(5);
                this.COMPORT.Write(value);
                Thread.Sleep(5);
                this.timer1.Start();
            }
        }

        private void Timer1Tick(object sender, EventArgs e)
        {
            string temp = "";
            this.COMPORT.Write((byte)0);
            Thread.Sleep(200);
            if (this.COMPORT.Read(ref temp))
            {
                this.status = (byte)temp[0];
                if ((this.status & 0x40) > 0)
                {
                    //this.checkBox_servopower.Checked = true;
                }
                else
                {
                    //this.checkBox_servopower.Checked = false;
                }
                if ((this.status & 0x80) > 0)
                {
                    //this.blinkTick();
                }
                else
                {
                    /*
                    this.label1.Visible = true;
                    this.label2.Visible = true;
                    this.label3.Visible = true;
                    this.label4.Visible = true;
                    this.label5.Visible = true;
                    this.label6.Visible = true;*/
                }
                //this.getCurent();
            }
            else
            {
                this.timer1.Stop();
                this.disconnect();/*
                this.listBox_step.Enabled = true;
                this.scrollBarsONOFF(true);
                MessageBox.Show("Connection broken!", "Error");*/
            }
        }

        /*
        private void getCurrent()
        {
            if (this.checkBox_port.Checked)
            {
                this.timer1.Stop();
                string temp = "";
                this.COMPORT.Write((byte)50);
                Thread.Sleep(200);
                if (this.COMPORT.Read(ref temp) && (temp[0] == '2'))
                {
                    this.cur1 = Convert.ToByte(temp[1]);
                    this.cur2 = Convert.ToByte(temp[2]);
                    this.cur3 = Convert.ToByte(temp[3]);
                    this.cur4 = Convert.ToByte(temp[4]);
                    this.cur5 = Convert.ToByte(temp[5]);
                    this.cur6 = Convert.ToByte(temp[6]);
                }
                this.updateCur();
                this.timer1.Start();
            }
        }

        private void updateCur()
        {
            try
            {
                this.pbCur1.Value = Convert.ToInt16(this.cur1);
                this.pbCur2.Value = Convert.ToInt16(this.cur2);
                this.pbCur3.Value = Convert.ToInt16(this.cur3);
                this.pbCur4.Value = Convert.ToInt16(this.cur4);
                this.pbCur5.Value = Convert.ToInt16(this.cur5);
                this.pbCur6.Value = Convert.ToInt16(this.cur6);
            }
            catch
            {
            }
        }
       */


        public void Connect(bool connected, bool wireless)
        {

            if (connected)
            {
                if (wireless)
                {
                    COMPORT.Open();
                    COMPORT.Close();
                    COMPORT.Baudrate = 0x2580;
                    
                    if (COMPORT.Open())
                    {
                        //comboBox_ports.Enabled = false;
                        if (connect_APC())
                        {
                            Thread.Sleep(0x3e8);
                            //this.checkBox_servopower.Enabled = true;
                            //this.updatePos2View(this.pos1, this.pos2, this.pos3, this.pos4, this.pos5, this.pos6);
                            this.timer1.Start();
                        }
                        else
                        {
                            //this.checkBox_servopower.Enabled = false;
                            this.disconnect();
                            //MessageBox.Show("Connection failed! \nPress START button on Robot Arm.", "ERROR:");
                        }
                    }
                    else
                    {
                        //this.checkBox_servopower.Enabled = false;
                        this.disconnect();
                        //MessageBox.Show("Cannot acces selected comport!", "ERROR:");
                    }
                }
                else
                {
                    COMPORT.Open();
                    COMPORT.Close();
                    COMPORT.Baudrate = 0x9600;
                    if (COMPORT.Open())
                    {
                        //comboBox_ports.Enabled = false;
                        if (connect())
                        {
                            Thread.Sleep(0x3e8);
                            //this.checkBox_servopower.Enabled = true;
                            //getPositions(connected);
                            //this.updatePos2View(this.pos1, this.pos2, this.pos3, this.pos4, this.pos5, this.pos6);
                            this.timer1.Start();
                        }
                        else
                        {
                            //this.checkBox_servopower.Enabled = false;
                            disconnect();
                            //MessageBox.Show("Connection failed!", "ERROR:");
                        }
                    }
                    else
                    {
                        //this.checkBox_servopower.Enabled = false;
                        disconnect();
                        //MessageBox.Show("Cannot acces selected comport!", "ERROR:");
                    }
                }
            }
            else
            {
                //this.checkBox_servopower.Enabled = false;
                disconnect();
            }
        }


        private void getPositions(bool connected)
        {
            if (connected)
            {
                this.timer1.Stop();
                string temp = "";
                this.COMPORT.Write((byte)10);
                Thread.Sleep(200);
                if (this.COMPORT.Read(ref temp) && (temp[0] == '\n'))
                {
                    ArmPosition[0] = Convert.ToByte(temp[1]);
                    ArmPosition[1] = Convert.ToByte(temp[2]);
                    ArmPosition[2] = Convert.ToByte(temp[3]);
                    ArmPosition[3] = Convert.ToByte(temp[4]);
                    ArmPosition[4] = Convert.ToByte(temp[5]);
                    ArmPosition[5] = Convert.ToByte(temp[6]);
                }
                this.timer1.Start();
            }
        }

        private bool connect()
        {
            string temp = "";
            string str2 = "\n[RP6BOOT]\n\n[READY]\n";
            COMPORT.O_RTS = true;
            COMPORT.O_RTS = false;
            Thread.Sleep(0x3e8);
            COMPORT.Write("s");
            Thread.Sleep(0x3e8);
            COMPORT.Read(ref temp);
            return (temp == str2);
        }

        private bool connect_APC()
        {
            string temp = "";
            string str2 = "d";
            COMPORT.Write((byte)100);
            Thread.Sleep(0x3e8);
            COMPORT.Read(ref temp);
            return (temp == str2);
        }

        private void disconnect()
        {
            timer1.Stop();
            COMPORT.Close();
            /*this.comboBox_ports.Enabled = true;
            this.checkBox_port.Checked = false;
            this.checkBox_servopower.Enabled = false;*/
        }

        public void PowerServos(bool powerEnabled)
        {
            if (powerEnabled)
            {
                COMPORT.Write((byte)1);
                Thread.Sleep(100);
                timer1.Start();
            }
            else
            {
                COMPORT.Write((byte)2);
                Thread.Sleep(100);
            }
        }
    }
}
