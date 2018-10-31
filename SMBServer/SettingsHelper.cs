/* Copyright (C) 2014-2018 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace SMBServer
{
    public class SettingsHelper
    {
        public const string SettingsFileName = "Settings.xml";

        public static XmlDocument ReadXmlDocument(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            return doc;
        }

        public static XmlDocument ReadSettingsXML()
        {
            string executableDirectory = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
            XmlDocument document = ReadXmlDocument(executableDirectory + SettingsFileName);
            return document;
        }

        public static UserCollection ReadUserSettings()
        {
            UserCollection users = new UserCollection();
            XmlDocument document = ReadSettingsXML();
            XmlNode usersNode = document.SelectSingleNode("Settings/Users");

            foreach (XmlNode userNode in usersNode.ChildNodes)
            {
                string accountName = userNode.Attributes["AccountName"].Value;
                string password = userNode.Attributes["Password"].Value;
                users.Add(accountName, password);
            }
            return users;
        }

        public static List<ShareSettings> ReadSharesSettings()
        {
            List<ShareSettings> shares = new List<ShareSettings>();
            XmlDocument document = ReadSettingsXML();
            XmlNode sharesNode = document.SelectSingleNode("Settings/Shares");

            foreach (XmlNode shareNode in sharesNode.ChildNodes)
            {
                string shareName = shareNode.Attributes["Name"].Value;
                string sharePath = shareNode.Attributes["Path"].Value;

                XmlNode readAccessNode = shareNode.SelectSingleNode("ReadAccess");
                List<string> readAccess = ReadAccessList(readAccessNode);
                XmlNode writeAccessNode = shareNode.SelectSingleNode("WriteAccess");
                List<string> writeAccess = ReadAccessList(writeAccessNode);
                ShareSettings share = new ShareSettings(shareName, sharePath, readAccess, writeAccess);
                shares.Add(share);
            }
            return shares;
        }

        private static List<string> ReadAccessList(XmlNode node)
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
    }
}
