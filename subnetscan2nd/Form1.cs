﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Threading;
using System.Net;
using System.Management;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;


namespace subnetscan2nd
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            lblStatus.ForeColor = System.Drawing.Color.Red;
            lblStatus.Text = "Idle";
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        Thread myThread = null;
        //-------------------tim dich vu---------------------//

        public void scan(string subnet) 
        {
            Ping myPing;
            PingReply reply;
            IPAddress addr;
            IPHostEntry host;

            progressBar1.Maximum = 254;
            progressBar1.Value = 0;
            listVAddr.Items.Clear();

            for (int i = 1; i < 255; i++)
            {
                string subnetn = "." + i.ToString();
                myPing = new Ping();
                reply = myPing.Send(subnet+subnetn, 900);

                lblStatus.ForeColor = System.Drawing.Color.Green;
                lblStatus.Text = "Scanning: " + subnet + subnetn;

                if (reply.Status == IPStatus.Success)
                {
                    try 
                     {
                         addr = IPAddress.Parse(subnet + subnetn);
                         host = Dns.GetHostEntry(addr);

                         listVAddr.Items.Add(new ListViewItem(new String[] { subnet + subnetn, host.HostName, "Up" }));
                     }
                     catch { MessageBox.Show("Couldnt retrieve hostname for "+subnet+subnetn, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                 }
                 progressBar1.Value += 1;
            }                    
            cmdScan.Enabled = true;
            cmdStop.Enabled = false;
            txtIP.Enabled = true;
            lblStatus.Text = "Done!";
            int count = listVAddr.Items.Count;
            MessageBox.Show("Scanning done!\nFound " + count.ToString() + " hosts.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information); 
        }

        public void query(string host) 
        {
            //string acc;
            //string os;
            //string board;
            //string biosVersion;
            string temp = null;
            string[] _searchClass = {"Win32_ComputerSystem", "Win32_OperatingSystem", "Win32_BaseBoard", "Win32_BIOS" };
            string[] param = { "UserName", "Caption", "Product", "Description"};

            lblStatus.ForeColor = System.Drawing.Color.Green;

            for (int i = 0; i <= _searchClass.Length-1; i++)
            {
                lblStatus.Text = "Getting information.";
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("\\\\" + host 
                        + "\\root\\CIMV2", "SELECT *FROM "+_searchClass[i]);
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        lblStatus.Text = "Getting information. .";
                    
                        temp += obj.GetPropertyValue(param[i]).ToString() + "\n";
                        if (i == _searchClass.Length - 1)
                        {
                            lblStatus.Text = "Done!";
                            MessageBox.Show(temp, "Hostinfo: " + host, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;
                        }
                        lblStatus.Text = "Getting information. . .";
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error in WMI query.\n\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); break; } 
            }
        }
        public void service(string host)
        {
            ManagementObjectSearcher s =
            new ManagementObjectSearcher(
            "\\\\" + host + "\\root\\CIMV2",
            "SELECT * FROM Win32_Service" +
            " WHERE State='Running'");

            foreach (ManagementObject service in s.Get())
            {
                // show the instance
                lVService.Items.Add(new ListViewItem(service.ToString()));
            }
        }
        public static string DnsToIp(string strHostName)
        {
            IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);
            IPAddress[] addr = ipEntry.AddressList;
            return addr[addr.Length - 1].ToString();
        }
        public string getPort(string host)
        {
            return "";
        }
        public void controlSys(string host, int flags)
        {
            #region 
            /*
             *Flags:
             *  0 (0x0)Log Off
             *  4 (0x4)Forced Log Off (0 + 4)
             *  1 (0x1)Shutdown
             *  5 (0x5)Forced Shutdown (1 + 4)
             *  2 (0x2)Reboot
             *  6 (0x6)Forced Reboot (2 + 4)
             *  8 (0x8)Power Off
             *  12 (0xC)Forced Power Off (8 + 4)
             */
            #endregion

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("\\\\" + host + "\\root\\CIMV2", "SELECT *FROM Win32_OperatingSystem");

                foreach (ManagementObject obj in searcher.Get())
                {
                    ManagementBaseObject inParams = obj.GetMethodParameters("Win32Shutdown");

                    inParams["Flags"] = flags;

                    ManagementBaseObject outParams = obj.InvokeMethod("Win32Shutdown", inParams, null);
                }
            }
            catch (ManagementException manex) { MessageBox.Show("Error:\n\n"+manex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            catch (UnauthorizedAccessException unaccex) { MessageBox.Show("Authorized?\n\n"+unaccex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        //-------------------giao dien---------------------------//

        private void cmdScan_Click(object sender, EventArgs e)
        {
            if (txtIP.Text == string.Empty)
            {
                MessageBox.Show("No IP address entered.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                myThread = new Thread(() => scan(txtIP.Text));
                myThread.Start();

                if (myThread.IsAlive == true)
                {
                    cmdStop.Enabled = true;
                    cmdScan.Enabled = false;
                    txtIP.Enabled = false;
                }
            }      
        }

        private void cmdStop_Click(object sender, EventArgs e)
        {
            myThread.Suspend();
            cmdScan.Enabled = true;
            cmdStop.Enabled = false;
            txtIP.Enabled = true;
            lblStatus.ForeColor = System.Drawing.Color.Red;
            lblStatus.Text = "Idle";
        }

        private void listVAddr_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listVAddr.FocusedItem.Bounds.Contains(e.Location) == true)
                {
                    conMenuStripIP.Show(Cursor.Position);
                }
            }
            //else if(e.Button == MouseButtons.Left)
            //{
            //    if (listVAddr.FocusedItem.Bounds.Contains(e.Location) == true)
            //    {
            //        if (listVAddr.SelectedItems.Count > 0)
            //        {
            //            string host = listVAddr.SelectedItems[0].Text.ToString();
            //            Thread qThread = new Thread(() => query(host));
            //            qThread.Start();
            //        }
            //    }
            //}
        }
        
        private void showInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string host = listVAddr.SelectedItems[0].Text.ToString();
            Thread qThread = new Thread(() => query(host));
            qThread.Start();
        }

        private void shutdownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string host = listVAddr.SelectedItems[0].Text.ToString();
            controlSys(host, 5);
        }

        private void rebootToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string host = listVAddr.SelectedItems[0].Text.ToString();
            controlSys(host, 6);
        }

        private void powerOffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string host = listVAddr.SelectedItems[0].Text.ToString();
            controlSys(host, 12);
        }
        private void showServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string host = listVAddr.SelectedItems[0].Text.ToString();
            Thread sThread = new Thread(() => service(host));
            sThread.Start();
        }



    }
}
