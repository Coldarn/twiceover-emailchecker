using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices;

namespace EmailChecker
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Expects at least one argument of partial a username or display name to resolve into an email address!");
                return -1;
            }

            bool getCurrentUser = args[0] == "--getCurrentUser";
            SearchResultCollection results = FindUsers(getCurrentUser ? Environment.UserName : String.Join(" ", args), getCurrentUser);

            if (results == null)
                return 0;

            HashSet<string> emails = new HashSet<string>();

            foreach (SearchResult result in results)
            {
                if (getCurrentUser)
                {
                    Console.WriteLine(result.Properties["samAccountName"][0]);
                    Console.WriteLine(result.Properties["cn"][0]);
                    Console.WriteLine(result.Properties["mail"].Count > 0 ? result.Properties["mail"][0] : "");
                    return 0;
                }
                else
                {
                    // Get the email property from AD
                    if (result.Properties["mail"].Count > 0)
                    {
                        foreach (string email in result.Properties["mail"])
                        {
                            if (!String.IsNullOrWhiteSpace(email) && emails.Add(email))
                                Console.WriteLine(email);
                        }
                    }

                    // Also check for emails in proxyaddresses
                    foreach (string proxyAddr in result.Properties["proxyaddresses"])
                    {
                        // Make it 'case-insensative'
                        if (!String.IsNullOrWhiteSpace(proxyAddr) && proxyAddr.ToLower().StartsWith("smtp:"))
                        {
                            string email = proxyAddr.Substring(5);
                            if (emails.Add(email))
                                Console.WriteLine(email);
                        }
                    }
                }
            }

            return 0;
        }

        static SearchResultCollection FindUsers(string query, bool exactUsernameMatch)
        {
            string domainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            string splitName = String.Join(",", domainName.Split('.').Select(part => "DC=" + part));

            //Set the correct format for the AD query and filter
            string rootQuery = String.Format(@"LDAP://{0}/{1}", domainName, splitName);
            string userQuery = String.Format(exactUsernameMatch
                ? @"(&(objectCategory=person)(objectClass=user)(samAccountName={0}))"
                : @"(&(objectCategory=person)(objectClass=user)(|(cn={0}*)(mail={0}*)(samAccountName={0}*)))", query);

            using (DirectoryEntry root = new DirectoryEntry(rootQuery))
            {
                using (DirectorySearcher searcher = new DirectorySearcher(root))
                {
                    searcher.Filter = userQuery;
                    return searcher.FindAll();
                }
            }
        } 
    }
}
