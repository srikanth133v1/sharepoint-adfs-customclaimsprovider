using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.SharePoint;

using NS.Extranet.ADFSClaimsProvider.Common;
using System.Text;


namespace NS.Extranet.ADFSClaimsProvider
{

    public class ADFSHelper
    {
        public static List<ADFSUser> Search(string pattern)
        {
            List<ADFSUser> filterdUsers = new List<ADFSUser>();

            //Run with elevated privileges to get the context of the service account            
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                filterdUsers = SearchUsers(pattern, AllUsers);
            });
            return filterdUsers;

        }
        public static T DeSerialize<T>(string filePath)
        {
            var x = new XmlSerializer(typeof(T));

            //Serilaize would fail if there are comments in the xml document
            var xmlReaderSettings = new XmlReaderSettings { IgnoreComments = true };
            var xmlReader = XmlReader.Create(filePath, xmlReaderSettings);

            return (T)x.Deserialize(xmlReader);
        }
        public static List<ADFSUser> SearchUsers(string pattern, ADFSUsers users)
        {
            ADFSUser[] allUsers = users.Items;
            List<ADFSUser> filteredUsers = (from u in allUsers
                                            where u.SearchableTerms.Contains(pattern)
                                            select u).ToList();
            return filteredUsers;
        }
        public static ADFSUser SearchExact(string pattern, ADFSUsers users)
        {
            ADFSUser[] allUsers = users.Items;
            ADFSUser filteredUser = (from u in allUsers
                                     where u.SearchableTerms.Contains(pattern)
                                     select u).FirstOrDefault();
            return filteredUser;
        }
        public static ADFSUser FindExact(string pattern)
        {
            ADFSUser user = null;

            //Run with elevated privileges to get the context of the service account
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                user = SearchExact(pattern, AllUsers);
            });
            return user;

        }
        private static ADFSUsers GetAllUsers()
        {
            ADFSUsers allUsers = DeSerialize<ADFSUsers>(ADFSXMLFilePath);
            List<PropertyInfo> properties = new List<PropertyInfo>();
            List<string> searchableProperties = SearchableProperties;
            foreach (var prop in searchableProperties)
            {
                properties.Add(typeof(ADFSUser).GetProperty(prop));
            }

            foreach (var user in allUsers.Items)
            {
                StringBuilder sbSearchableString = new StringBuilder();
                foreach (PropertyInfo pi in properties)
                {
                    sbSearchableString.Append(Convert.ToString(pi.GetValue(user, null)).ToLower())
                        .Append(Constants.WHITE_SPACE);
                }
                user.SearchableTerms = sbSearchableString.ToString();
            }
            return allUsers;
        }
        private static string ADFSXMLSearchableFields
        {
            get
            {
                return ConfigurationManager.AppSettings[Constants.ADFS_XML_SEARCHABLE_FIELDS];
            }
        }
        private static string ADFSXMLFilePath
        {
            get
            {
                return ConfigurationManager.AppSettings[Constants.ADFS_XML_FILE_PATH];
            }
        }
        private static List<string> SearchableProperties
        {
            get
            {
                List<string> searchableProperties = new List<string>();
                if (!string.IsNullOrEmpty(ADFSXMLSearchableFields))
                {
                    string[] properties = ADFSXMLSearchableFields.Split(new string[] { Constants.DELIMITER_SEMI_COLON }, StringSplitOptions.RemoveEmptyEntries);
                    searchableProperties = properties.ToList();
                }
                return searchableProperties;
            }
        }
        private static int CacheDuration
        {
            get
            {
                int minutes = 360;
                Int32.TryParse(ConfigurationManager.AppSettings[Constants.ADFS_XML_FILE_CACHE_IN_MINUTES], out minutes);
                return minutes;
            }
        }
        private static ADFSUsers AllUsers
        {
            get
            {
                if (null == HttpRuntime.Cache[Constants.CACHE_KEY_ADFS_USERS])
                {
                    ADFSUsers allUsers = GetAllUsers();
                    HttpRuntime.Cache.Insert(Constants.CACHE_KEY_ADFS_USERS,
                        allUsers, null, DateTime.Now.AddMinutes(CacheDuration), TimeSpan.Zero);
                    return allUsers;
                }
                return HttpRuntime.Cache[Constants.CACHE_KEY_ADFS_USERS] as ADFSUsers;
            }
        }
    }
}
