using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Client;
using UploadImageToSharepoint.Properties;

namespace UploadImageToSharepoint
{
    class Program
    {
        
        static void Main(string[] args)
        {
            if (args.Count() > 0)
            {
                if (System.IO.File.Exists(args[0]))
                {
                       string ImageName = "";

                        try
                        {
                            ImageName = UploadDocument(args[0], Settings.Default.sList, Settings.Default.Website, Settings.Default.SubSite);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Upload Document error:" + ex.Message);
                        }

                        bool eplods = false;
                        if (args.Count() > 1)
                        {
                            if (args[1].ToLower() == "true")
                                eplods = true;
                        }
                    string webfqdn = Settings.Default.Website;
                    if (Settings.Default.SubSite.Length > 0)
                    {
                        webfqdn += @"/" + Settings.Default.SubSite;
                    }

                    SetParams(ImageName, Settings.Default.sList, eplods, webfqdn);
                    Console.WriteLine("File {0} has been uploaded to TVImages.  Eplods was set to {1}", ImageName, eplods);
                }
                else
                    Console.WriteLine("Argument is not a valid file");
            }
            else
            {
                Console.WriteLine("No Filename specified");
            }

#if DEBUG
            Console.ReadKey();
#endif

        }

        public static void SetParams(string fileName, string Library, bool eplod, string Website)
        {
            ClientContext clientContext = new ClientContext(Website);
            Web oWebsite = clientContext.Web;
            ListCollection collList = oWebsite.Lists;

            List oList = collList.GetByTitle(Library);

            CamlQuery cQuery = new CamlQuery();

            ListItemCollection collListItem = oList.GetItems(cQuery);

            clientContext.Load(collListItem);

            clientContext.ExecuteQuery();

            foreach (ListItem oListItem in collListItem)
            {
                if ((string)oListItem["FileLeafRef"] == fileName)
                {
                    string[] tempDisplayTarget;
                    if (eplod)
                    {
                        oListItem["Interval"] = 60;
                        tempDisplayTarget = new string[] { "ITOffice", "630ShiftBoss", "NewCobar", "Sharepoint" };                       
                    }
                    else
                    {
                        tempDisplayTarget = new string[] { "All" };
                    }

                    oListItem["Enabled"] = true;
                    oListItem["DisplayTarget"] = tempDisplayTarget;
                    oListItem.Update();

                }

            }

            clientContext.ExecuteQuery();

        }


        public static string UploadDocument(String fileName, String Library, string Website, string SubSite)
        {

            ClientContext ctx = new ClientContext(Website);
            
            Web web = ctx.Web;

            FileCreationInformation newFile = new FileCreationInformation();

            newFile.Content = System.IO.File.ReadAllBytes(fileName);

            newFile.Url = "/" + fileName;

            List docs = web.Lists.GetByTitle(Library);

            System.IO.FileStream fs = System.IO.File.OpenRead(fileName);

            System.IO.FileInfo fileInf = new System.IO.FileInfo(fileName);

            ctx.Load(docs.RootFolder);
            ctx.ExecuteQuery();

            string weburl = Website;
            if (SubSite.Length > 0)
            {
                weburl += @"/" + SubSite;
            }
            //            var fileUrl = String.Format("{0}/{1}", list.RootFolder.ServerRelativeUrl, fi.Name);

            string fileUrl = string.Format("{0}/{1}", docs.RootFolder.ServerRelativeUrl, fileInf.Name);
            //string fileUrl = string.Format("{0}/{1}", Library.Replace(" ","%20"), fileInf.Name);

            Microsoft.SharePoint.Client.File.SaveBinaryDirect(ctx, fileUrl, fs, true);

            return fileInf.Name;

        }
    }
}
