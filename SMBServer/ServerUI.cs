/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SMBLibrary;
using SMBLibrary.Authentication.GSSAPI;
using SMBLibrary.Authentication.NTLM;
using SMBLibrary.Server;
using SMBLibrary.Win32.Security;
using Utilities;

namespace SMBServer
{
    public partial class ServerUI : Form
    {
        public const string SettingsFileName = "Settings.xml";
        private SMBLibrary.Server.SMBServer m_server;
        private SMBLibrary.Server.NameServer m_nameServer;
        private LogWriter m_logWriter;

        public ServerUI()
        {
            InitializeComponent();
        }

        private void ServerUI_Load(object sender, EventArgs e)
        {
            List<IPAddress> localIPs = NetworkInterfaceHelper.GetHostIPAddresses();
            KeyValuePairList<string, IPAddress> list = new KeyValuePairList<string, IPAddress>();
            list.Add("Any", IPAddress.Any);
            foreach (IPAddress address in localIPs)
            {
                list.Add(address.ToString(), address);
            }
            comboIPAddress.DataSource = list;
            comboIPAddress.DisplayMember = "Key";
            comboIPAddress.ValueMember = "Value";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            IPAddress serverAddress = (IPAddress)comboIPAddress.SelectedValue;
            SMBTransportType transportType;
            if (rbtNetBiosOverTCP.Checked)
            {
                transportType = SMBTransportType.NetBiosOverTCP;
            }
            else
            {
                transportType = SMBTransportType.DirectTCPTransport;
            }

            NTLMAuthenticationProviderBase authenticationMechanism;
            if (chkIntegratedWindowsAuthentication.Checked)
            {
                authenticationMechanism = new IntegratedNTLMAuthenticationProvider();
            }
            else
            {
                UserCollection users;
                try
                {
                    users = ReadUserSettings();
                }
                catch
                {
                    MessageBox.Show("Cannot read " + SettingsFileName, "Error");
                    return;
                }

                authenticationMechanism = new IndependentNTLMAuthenticationProvider(users.GetUserPassword);
            }

            SMBShareCollection shares;
            try
            {
                shares = ReadShareSettings();
            }
            catch (Exception)
            {
                MessageBox.Show("Cannot read " + SettingsFileName, "Error");
                return;
            }

            GSSProvider securityProvider = new GSSProvider(authenticationMechanism);
            m_server = new SMBLibrary.Server.SMBServer(shares, securityProvider);
            m_logWriter = new LogWriter();
            m_server.OnLogEntry += new EventHandler<LogEntry>(m_logWriter.OnLogEntry);

            try
            {
                m_server.Start(serverAddress, transportType, chkSMB1.Checked, chkSMB2.Checked);
                if (transportType == SMBTransportType.NetBiosOverTCP)
                {
                    if (serverAddress.AddressFamily == AddressFamily.InterNetwork && !IPAddress.Equals(serverAddress, IPAddress.Any))
                    {
                        IPAddress subnetMask = NetworkInterfaceHelper.GetSubnetMask(serverAddress);
                        m_nameServer = new NameServer(serverAddress, subnetMask);
                        m_nameServer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            comboIPAddress.Enabled = false;
            rbtDirectTCPTransport.Enabled = false;
            rbtNetBiosOverTCP.Enabled = false;
            chkSMB1.Enabled = false;
            chkSMB2.Enabled = false;
            chkIntegratedWindowsAuthentication.Enabled = false;
        }

        private XmlDocument GetSettingsXML()
        {
            string executableDirectory = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
            XmlDocument document = GetXmlDocument(executableDirectory + SettingsFileName);
            return document;
        }

        private UserCollection ReadUserSettings()
        {
            UserCollection users = new UserCollection();
            XmlDocument document = GetSettingsXML();
            XmlNode usersNode = document.SelectSingleNode("Settings/Users");

            foreach (XmlNode userNode in usersNode.ChildNodes)
            {
                string accountName = userNode.Attributes["AccountName"].Value;
                string password = userNode.Attributes["Password"].Value;
                users.Add(accountName, password);
            }
            return users;
        }

        private SMBShareCollection ReadShareSettings()
        {
            SMBShareCollection shares = new SMBShareCollection();
            XmlDocument document = GetSettingsXML();
            XmlNode sharesNode = document.SelectSingleNode("Settings/Shares");

            foreach (XmlNode shareNode in sharesNode.ChildNodes)
            {
                string shareName = shareNode.Attributes["Name"].Value;
                string sharePath = shareNode.Attributes["Path"].Value;

                XmlNode readAccessNode = shareNode.SelectSingleNode("ReadAccess");
                List<string> readAccess = ReadAccessList(readAccessNode);
                XmlNode writeAccessNode = shareNode.SelectSingleNode("WriteAccess");
                List<string> writeAccess = ReadAccessList(writeAccessNode);
                FileSystemShare share = new FileSystemShare(shareName, new DirectoryFileSystem(sharePath));
                share.AccessRequested += delegate(object sender, AccessRequestArgs args)
                {
                    bool hasReadAccess = Contains(readAccess, "Users") || Contains(readAccess, args.UserName);
                    bool hasWriteAccess = Contains(writeAccess, "Users") || Contains(writeAccess, args.UserName);
                    if (args.RequestedAccess == FileAccess.Read)
                    {
                        args.Allow = hasReadAccess;
                    }
                    else if (args.RequestedAccess == FileAccess.Write)
                    {
                        args.Allow = hasWriteAccess;
                    }
                    else // FileAccess.ReadWrite
                    {
                        args.Allow = hasReadAccess && hasWriteAccess;
                    }
                };
                shares.Add(share);
            }
            return shares;
        }

        private List<string> ReadAccessList(XmlNode node)
        {
            List<string> result = new List<string>();
            if (node != null)
            {
                string accounts = node.Attributes["Accounts"].Value;
                if (accounts == "*")
                {
                    result.Add("Users");
                }
                else
                {
                    string[] splitted = accounts.Split(',');
                    result.AddRange(splitted);
                }
            }
            return result;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            m_server.Stop();
            m_logWriter.CloseLogFile();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            comboIPAddress.Enabled = true;
            rbtDirectTCPTransport.Enabled = true;
            rbtNetBiosOverTCP.Enabled = true;
            chkSMB1.Enabled = true;
            chkSMB2.Enabled = true;
            chkIntegratedWindowsAuthentication.Enabled = true;

            if (m_nameServer != null)
            {
                m_nameServer.Stop();
            }
        }

        private void chkSMB1_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkSMB1.Checked)
            {
                chkSMB2.Checked = true;
            }
        }

        private void chkSMB2_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkSMB2.Checked)
            {
                chkSMB1.Checked = true;
            }
        }

        public static XmlDocument GetXmlDocument(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            return doc;
        }

        public static bool Contains(List<string> list, string value)
        {
            return (IndexOf(list, value) >= 0);
        }

        public static int IndexOf(List<string> list, string value)
        {
            for (int index = 0; index < list.Count; index++)
            {
                if (string.Equals(list[index], value, StringComparison.InvariantCultureIgnoreCase))
                {
                    return index;
                }
            }
            return -1;
        }
    }
}