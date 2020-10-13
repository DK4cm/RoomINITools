using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;

namespace RoomINITools
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string homeDIR;
        string backupDIR; //Folder for backup 
        string tmpDIR;  
        string tmpFile; //Tmp file for utf-8 no bom ini file (since winapi cannt process a bom utf-8 ini)
#if DEBUG
        string serverPrefix = @"C:\Users\dk4cm\source\repos\RoomINITools\RoomINITools\bin\Debug\room - {0}.ini";
#else
        string serverPrefix = @"\\192.168.1.{0}\VertrigoServ\www\VodBox\room.ini";
#endif
        string[] serverNo;
        Dictionary<int, string> ipRoom; //ip-room key-value pair
        Dictionary<string, string> RoomServer; //room-server key-value pair

        private void Form1_Load(object sender, EventArgs e)
        {
            homeDIR = Directory.GetCurrentDirectory();

            backupDIR = Path.Combine(homeDIR, "backup");
            if (!Directory.Exists(backupDIR)) //if backup directory not found, just create one
            {
                Directory.CreateDirectory(backupDIR);
            }

            tmpDIR = Path.Combine(homeDIR, "tmp");
            if (!Directory.Exists(tmpDIR)) //if tmp directory not found, just create one
            {
                Directory.CreateDirectory(tmpDIR);
            }
            tmpFile = Path.Combine(tmpDIR, "tmp.ini");

            string confFile = Path.Combine(homeDIR, "conf.ini");
            if (!File.Exists(confFile)) 
            {
                MessageBox.Show("缺少Conf.ini", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            using (IniTools ini = new IniTools(confFile)) 
            {
                serverNo = ini.ReadValue("Servers", "list").Split(new Char[] {','}); //get the server list seperate by ','
            }
            LoadRoomServerState();  // first page is Room Server
        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            switch (e.TabPageIndex)
            {
                case 0: //Change room Server
                    LoadRoomServerState();
                    break;
                case 1: //Change IP
                    LoadIPState();
                    break;
                case 2: //Change Server States
                    LoadServerState();
                    break;
                default:
                    break;
            }

        }

        private void LoadIPState()
        {
            //update tmp file to server 2
            UpdateTmpFile("2");
            UpdateIpRoomDict();
            comboBox2.DataSource = new BindingSource(ipRoom, null);
            comboBox2.DisplayMember = "Value";
            comboBox2.ValueMember = "Key";
        }

        private void UpdateIpRoomDict()
        {
            ipRoom = new Dictionary<int, string>();
            using (IniTools ini = new IniTools(tmpFile))
            {
                string room;
                for (int i = 1; i <= 255; i++)
                {
                    room = ini.ReadValue("Room", "192.168.1." + i.ToString());
                    if (room != "")
                    {
                        ipRoom.Add(i, room);
                    }
                }
            }
        }

        private void LoadRoomServerState()
        {
            //get server list and bind it
            comboBox5.DataSource = new BindingSource(serverNo, null);
            //get room list and bind it
            UpdateTmpFile("2");
            //get room server dictionary for further use
            UpdateRoomServerDict();
            comboBox4.DataSource = new BindingSource(RoomServer, null);
            comboBox4.DisplayMember = "Key";
            
        }

        private void UpdateRoomServerDict()
        {
            //get room data from ip, so need to update it first
            UpdateIpRoomDict();
            RoomServer = new Dictionary<string, string>();
            using (IniTools ini = new IniTools(tmpFile))
            {
                string server;
                foreach (string room in ipRoom.Values) 
                {
                    server = ini.ReadValue("TotalSelect", room);
                    if ((server != "") && (!RoomServer.ContainsKey(room))) 
                    {
                        RoomServer.Add(room, server);
                    }
                }
            }
        }

        private void LoadServerState()
        {
            UpdateTmpFile("2"); //update server2 is enough

            using (IniTools ini = new IniTools(tmpFile))
            {
                //use Normal ini tool to read it
                string state = ini.ReadValue("status", "state").Trim();
                switch (state)
                {
                    case "1":
                        comboBox1.SelectedIndex = 0;
                        break;
                    case "3":
                        comboBox1.SelectedIndex = 1;
                        break;
                    case "6":
                        comboBox1.SelectedIndex = 2;
                        break;
                    case "9":
                        comboBox1.SelectedIndex = 3;
                        break;
                    case "10":
                        comboBox1.SelectedIndex = 4;
                        break;
                    default:
                        break;
                }
            }
        }

        private void UpdateTmpFile(String ServerName)
        {
            string OrginalIniFileName = serverPrefix.Replace("{0}", ServerName); 
            //copy target to tmp file first
            File.Copy(OrginalIniFileName, tmpFile, true);
            //convert to ANSI file
            ConvertIniFile(tmpFile);
        }

        /// <summary>
        /// Convert ini File Format to ANSI
        /// </summary>
        /// <param name="FileName">input file name</param>
        private void ConvertIniFile(string FileName) 
        {
            string data; //ini data save
            using (StreamReader sr = new StreamReader(FileName, true)) 
            {
                data = sr.ReadToEnd();
                sr.Close();
                sr.Dispose();
            }
            using (StreamWriter sw = new StreamWriter(FileName, false, new UTF8Encoding(false)))
            {
                sw.Write(data);
                sw.Flush();
                sw.Close();
                sw.Dispose();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Get the select Option
            string selected = comboBox1.SelectedItem.ToString();
            selected = selected.Substring(0, selected.IndexOf('-'));
            string serverName = "2"; //only server2 need to change
            string section = "status";
            string key = "state";
            string value = selected;
            ChangeIniValue(serverName, section, key, value);

            MessageBox.Show("檔案已儲存", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ChangeIniValue(string serverName, string section, string key, string value)
        {
            //copy one copy to backup folder
            BackupFile(serverName);
            //update the temp file
            UpdateTmpFile(serverName);
            //Change the tmpFile using Normal ini function
            using (IniTools ini = new IniTools(tmpFile))
            {
                ini.WriteValue(section, key, value);
            }
            //Update the Original File
            string data;
            string originalFile = serverPrefix.Replace("{0}", serverName);
            using (StreamReader sr = new StreamReader(tmpFile, true))
            {
                data = sr.ReadToEnd();
                sr.Close();
                sr.Dispose();
            }
            using (StreamWriter sw = new StreamWriter(originalFile, false, new UTF8Encoding(true)))
            {
                sw.Write(data);
                sw.Flush();
                sw.Close();
            }
        }

        private void BackupFile(string ServerName) 
        {
            string BackupFile = Path.Combine(backupDIR, NameByDateAndServer(ServerName));
            string OriginalFile = serverPrefix.Replace("{0}", ServerName);
            File.Copy(OriginalFile, BackupFile);
        }

        private string NameByDateAndServer(string ServerName) 
        {
            string fileName = "S" + ServerName + ".ini";
            DateTime dt = DateTime.Now;
            string dateString = string.Format("{0:yyyyMMddHHmmssffff}", dt);
            return dateString + "-" +fileName;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedValue == null)
            {
                return;
            }
            string[] ip = comboBox2.SelectedItem.ToString().Substring(1, comboBox2.SelectedItem.ToString().Length - 2).Split(new char[]{ ',' });
            textBox1.Text = "192.168.1." + ip[0];
        }

        
        private void button2_Click(object sender, EventArgs e)
        {
            //check if valid
            string ipAddress = textBox1.Text.Trim();
            string checkValue = ipAddress.Replace("192.168.1.", "");
            int ipValue;
            if ((!ipAddress.StartsWith("192.168.1.")) || (ipAddress.Length > 13)) 
            {
                MessageBox.Show("IP格式錯誤", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!int.TryParse(checkValue,out ipValue)) 
            {
                MessageBox.Show("IP格式錯誤(不合法字元)", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if ((ipValue <= 10) || (ipValue >= 254)) 
            {
                MessageBox.Show("IP格式錯誤(IP範圍錯誤)", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            foreach (int ip in ipRoom.Keys) 
            {
                if (ip == ipValue) 
                {
                    MessageBox.Show("IP已使用", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            //for delete items since ip is the key, if we need to change ip, we need to delete first
            string originalIp = "192.168.1." + ipRoom.First(q => q.Value == comboBox2.Text).Key.ToString(); 

            //save to all server
            foreach (string serverName in serverNo) 
            {
                //刪除原本值
                ChangeIniValue(serverName, "Room", originalIp, null);
                //增加新值
                ChangeIniValue(serverName, "Room", ipAddress, comboBox2.Text);
            }
            MessageBox.Show("檔案已儲存到所有Server", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadIPState();
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox4.SelectedValue == null)
            {
                return;
            }
            string[] tmp = comboBox4.SelectedItem.ToString().Substring(1, comboBox4.SelectedItem.ToString().Length - 2).Split(new char[] { ',' });
            string server = RoomServer[tmp[0]];
            for (int i = 0; i < serverNo.Length; i++) 
            {
                if (server == serverNo[i])
                {
                    comboBox5.SelectedIndex = i;
                    return;
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string room = comboBox4.Text;
            string server = comboBox5.Text;

            //save to all server
            foreach (string serverName in serverNo)
            {
                //修改新值
                ChangeIniValue(serverName, "TotalSelect", room, server);
            }
            MessageBox.Show("檔案已儲存到所有Server", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadRoomServerState();
        }
    }
}
