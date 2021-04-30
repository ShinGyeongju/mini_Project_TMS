using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;          // for UART communication
using System.Data.SqlClient;    // for MySQL communication
using MySql.Data.MySqlClient;   // for MySQL communication


namespace _002_TMS
{
    public partial class Form1 : Form
    {
        String receivedData;
        int room1_curTemper, room2_curTemper, room3_curTemper = 0;
        int room1_dsrTemper, room2_dsrTemper, room3_dsrTemper = 0;
        bool room1_state, room2_state, room3_state = false;
        Thread th1;
        MySqlConnection mscn;
        MySqlCommand mscm;
        MySqlDataReader msdr;
        String dbServer = "127.0.0.1";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            serialPort1.Open();

            mscn = new MySqlConnection("Server=" + dbServer + ";Database=temper_db;Uid=winuser;Pwd=p@ssw0rd;Charset=UTF8");
            mscn.Open();
            mscm = new MySqlCommand("", mscn);

            String[] ports = SerialPort.GetPortNames();
            comboBox1_Port.Items.AddRange(ports);
            comboBox1_Port.Text = "COM4";
            comboBox2_BPS.Text = "115200";
            textBox1_Max.Text = "30";
            textBox2_Min.Text = "16";

            chart_State();
            CheckForIllegalCrossThreadCalls = false;

            th1 = new Thread(new ThreadStart(mariaDB));
            th1.Start();

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            th1.Abort();
        }

        public void mariaDB()
        {
            while (true)
            {
                Thread.Sleep(1000);

                mscm.CommandText = "INSERT INTO room1_tb VALUES (now(), " + room1_curTemper + ", " + room1_dsrTemper + ", " + room1_state + ");";
                mscm.ExecuteNonQuery();

                mscm.CommandText = "INSERT INTO room2_tb VALUES (now(), " + room2_curTemper + ", " + room2_dsrTemper + ", " + room2_state + ");";
                mscm.ExecuteNonQuery();

                mscm.CommandText = "INSERT INTO room3_tb VALUES (now(), " + room3_curTemper + ", " + room3_dsrTemper + ", " + room3_state + ");";
                mscm.ExecuteNonQuery();

                chart1_Temper.Series[0].Points.Add(room1_curTemper);
                chart1_Temper.Series[1].Points.Add(room2_curTemper);
                chart1_Temper.Series[2].Points.Add(room3_curTemper);

                if (chart1_Temper.Series[0].Points.Count > 30)
                {
                    chart1_Temper.Series[0].Points.RemoveAt(0);
                    chart1_Temper.Series[1].Points.RemoveAt(0);
                    chart1_Temper.Series[2].Points.RemoveAt(0);
                }
            }
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            receivedData = serialPort1.ReadLine();        // STM32로 부터 데이터를 읽어들임
                                                    // 115200 BPS의 경우 1 char당 약 0.1ms가 소요됨
            this.Invoke(new EventHandler(ShowData));
        }

        private void ShowData(object sender, EventArgs e)
        {
            if (receivedData.Contains("mode") == true) 
            {
                int tmpMode = 1;
                try
                {
                    tmpMode = int.Parse(receivedData.Split(' ')[1]);
                }
                catch { }

                if (tmpMode == 1)
                    label19_Mode.Text = "OPERATING";
                else if (tmpMode == 2) 
                    label19_Mode.Text = "SETTING";
            }
            else if (receivedData.Contains("cur") == true)
            {
                String tmpSplit = receivedData.Split('m')[1];
                int tmpRoom = int.Parse(tmpSplit.Split(' ')[0]);

                if (tmpRoom == 1)
                {
                    room1_curTemper = int.Parse(tmpSplit.Split(' ')[1]);
                    textBox1_Room1_Cur.Text = room1_curTemper.ToString();
                }
                else if (tmpRoom == 2)
                {
                    room2_curTemper = int.Parse(tmpSplit.Split(' ')[1]);
                    textBox4_Room2_Cur.Text = room2_curTemper.ToString();
                }
                else if (tmpRoom == 3)
                {
                    room3_curTemper = int.Parse(tmpSplit.Split(' ')[1]);
                    textBox6_Room3_Cur.Text = room3_curTemper.ToString();
                }
            }
            else if ((receivedData.Contains("dsr") == true) && (room1_dsrTemper == 0 || room2_dsrTemper == 0 || room3_dsrTemper == 0))
            {
                String tmpSplit = receivedData.Split('m')[1];
                int tmpRoom = int.Parse(tmpSplit.Split(' ')[0]);

                if (tmpRoom == 1)
                {
                    room1_dsrTemper = int.Parse(tmpSplit.Split(' ')[1]);
                    textBox2_Room1_Dsr.Text = room1_dsrTemper.ToString();
                    trackBar1_Room1.Value = room1_dsrTemper;
                }
                else if (tmpRoom == 2)
                {
                    room2_dsrTemper = int.Parse(tmpSplit.Split(' ')[1]);
                    textBox3_Room2_Dsr.Text = room2_dsrTemper.ToString();
                    trackBar2_Room2.Value = room2_dsrTemper;
                }
                else if (tmpRoom == 3)
                {
                    room3_dsrTemper = int.Parse(tmpSplit.Split(' ')[1]);
                    textBox5_Room3_Dsr.Text = room3_dsrTemper.ToString();
                    trackBar3_Room3.Value = room3_dsrTemper;
                }
            }
            else if (receivedData.Contains("state") == true)
            {
                String tmpSplit = receivedData.Split('m')[1];
                int tmpRoom = int.Parse(tmpSplit.Split(' ')[0]);
                int tmpState = int.Parse(tmpSplit.Split(' ')[1]);

                if (tmpRoom == 1)
                {
                    if (tmpState == 0)
                    {
                        label4_Room1_State.Text = "OFF";
                        label4_Room1_State.ForeColor = Color.Red;
                        room1_state = false;
                    }
                    else if (tmpState == 1)
                    {
                        label4_Room1_State.Text = "ON";
                        label4_Room1_State.ForeColor = Color.Green;
                        room1_state = true;
                    }
                }
                else if (tmpRoom == 2)
                {
                    if (tmpState == 0)
                    {
                        label7_Room2_State.Text = "OFF";
                        label7_Room2_State.ForeColor = Color.Red;
                        room2_state = false;
                    }
                    else if (tmpState == 1)
                    {
                        label7_Room2_State.Text = "ON";
                        label7_Room2_State.ForeColor = Color.Green;
                        room2_state = true;
                    }
                }
                else if (tmpRoom == 3)
                {
                    if (tmpState == 0)
                    {
                        label13_Room3_State.Text = "OFF";
                        label13_Room3_State.ForeColor = Color.Red;
                        room3_state = false;
                    }
                    else if (tmpState == 1)
                    {
                        label13_Room3_State.Text = "ON";
                        label13_Room3_State.ForeColor = Color.Green;
                        room3_state = true;
                    }
                }
            }
            else if (receivedData.Contains("max") == true)
            {
                int tmpMax = int.Parse(receivedData.Split(' ')[1]);

                trackBar1_Room1.Maximum = tmpMax;
                trackBar2_Room2.Maximum = tmpMax;
                trackBar3_Room3.Maximum = tmpMax;
            }
            else if (receivedData.Contains("min") == true)
            {
                int tmpMin = int.Parse(receivedData.Split(' ')[1]);

                trackBar1_Room1.Minimum = tmpMin;
                trackBar2_Room2.Minimum = tmpMin;
                trackBar3_Room3.Minimum = tmpMin;
            }
        }
        private void trackBar1_Room1_Scroll_1(object sender, EventArgs e)
        {
            textBox2_Room1_Dsr.Text = trackBar1_Room1.Value.ToString();
        }

        private void trackBar2_Room2_Scroll(object sender, EventArgs e)
        {
            textBox3_Room2_Dsr.Text = trackBar2_Room2.Value.ToString();
        }

        private void trackBar3_Room3_Scroll(object sender, EventArgs e)
        {
            textBox5_Room3_Dsr.Text = trackBar3_Room3.Value.ToString();
        }

        private void trackBar1_Room1_MouseUp(object sender, MouseEventArgs e)
        {
            serialPort1.WriteLine("SETdsr_room1-" + trackBar1_Room1.Value.ToString());
        }

        private void trackBar2_Room2_MouseUp(object sender, MouseEventArgs e)
        {
            serialPort1.WriteLine("SETdsr_room2-" + trackBar2_Room2.Value.ToString());
        }

        private void trackBar3_Room3_MouseUp(object sender, MouseEventArgs e)
        {
            serialPort1.WriteLine("SETdsr_room3-" + trackBar3_Room3.Value.ToString());
        }

        public void chart_State()
        {
            for (int i = 1; i < 4; i++)
            {
                for (int k = 0; k < 5; k++)
                {
                    mscm.CommandText = "SELECT COUNT(room" + i + "_state) FROM room" + i + "_tb WHERE DAY(room" + i + "_date) BETWEEN DAY(NOW())-" + k + " AND DAY(NOW())-" + k + " AND room" + i + "_state = 1;";
                    msdr = mscm.ExecuteReader();
                    msdr.Read();
                    chart2_State.Series[i - 1].Points.Add((long)msdr["COUNT(room" + i + "_state)"] / 60);
                    msdr.Close();
                }
            }
        }

        private void button6_Control_Click(object sender, EventArgs e)
        {
            serialPort1.WriteLine("SETmax_temp-" + textBox1_Max.Text);
            serialPort1.WriteLine("SETmin_temp-" + textBox2_Min.Text);
            MessageBox.Show("적용되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

    }
}
