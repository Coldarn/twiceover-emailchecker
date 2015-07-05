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

            SearchResultCollection results = GetUser(String.Join(" ", args));

            if (results == null)
                return 0;

            foreach (SearchResult result in results)
            {
                // Get the email property from AD
                if (result.Properties["mail"].Count > 0)
                {
                    foreach (string email in result.Properties["mail"])
                    {
                        if (!String.IsNullOrWhiteSpace(email))
                            Console.WriteLine(email);
                    }
                }

                // Also check for emails in proxyaddresses
                foreach (string proxyAddr in result.Properties["proxyaddresses"])
                {
                    // Make it 'case-insensative'
                    if (!String.IsNullOrWhiteSpace(proxyAddr) && proxyAddr.ToLower().StartsWith("smtp:"))
                        Console.WriteLine(proxyAddr.Substring(5));
                }
            }

            return 0;
        }

        static SearchResultCollection GetUser(string query)
        {
            string domainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            string splitName = String.Join(",", domainName.Split('.').Select(part => "DC=" + part));

            //Set the correct format for the AD query and filter
            string rootQuery = String.Format(@"LDAP://{0}/{1}", domainName, splitName);
            string userQuery = String.Format(@"(&(objectCategory=person)(objectClass=user)(|(cn={0}*)(samAccountName={0}*)))", query);

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
