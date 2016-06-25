using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Client;
using System.Xml;
using System.Xml.Serialization;
using TVImagesIotSolution.Base;
using System.Net;

[Serializable]

[XmlRoot("XMLTVData")]
public class XMLTVDataWin : XMLTVData
{
    public bool createDefinition(string Website, string SubSite, string sList)
    {
        try
        {
            string fqdn = Website;
            if (SubSite.Length > 0)
            {
                if (fqdn.EndsWith(@"/"))
                {
                    fqdn = Website + SubSite;
                }
                else
                {
                    fqdn = Website + "/" + SubSite;
                }
            }

            if (fqdn.EndsWith(@"/"))
            {
                fqdn = fqdn.Substring(0, fqdn.Length - 1);
            }

            ClientContext clientContext = new ClientContext(fqdn);
            Web oWebsite = clientContext.Web;
            ListCollection collList = oWebsite.Lists;

            List oList = collList.GetByTitle(sList);

            CamlQuery cQuery = new CamlQuery();

            ListItemCollection collListItem = oList.GetItems(cQuery);

            clientContext.Load(collListItem);

            clientContext.ExecuteQuery();

            DateTime tempExpiryDate;
            DateTime tempStartDate;
            Boolean tempEnabled;
            string[] tempDisplayTarget;
            int tempInterval;
            string tempURL;

            if (Website.EndsWith(@"/"))
            {
                Website = Website.Substring(0, Website.Length - 1);
            }

            foreach (ListItem oListItem in collListItem)
            {
                tempDisplayTarget = null;

                try
                {
                    try
                    {
                        tempStartDate = (DateTime)oListItem["StartDate"];
                    }
                    catch
                    {
                        tempStartDate = DateTime.Today.AddDays(-1);
                    }

                    try
                    {
                        tempExpiryDate = (DateTime)oListItem["Expiry_x0020_Date"];
                    }
                    catch
                    {
                        tempExpiryDate = DateTime.Today.AddDays(1);
                    }


                    try
                    {
                        tempDisplayTarget = (string[])oListItem["DisplayTarget"];
                    }
                    catch
                    {
                        tempDisplayTarget = new string[] { "All" };
                    }
                    finally
                    {
                        if (tempDisplayTarget == null)
                            tempDisplayTarget = new string[] { "All" };
                    }



                    try
                    {
                        tempInterval = int.Parse(oListItem["Interval"].ToString());
                    }
                    catch
                    {
                        tempInterval = 0;
                    }
                    
                    tempEnabled = (Boolean)oListItem["Enabled"];

                    if (tempEnabled) // if enabled
                    {
                        if ((DateTime.Compare(tempExpiryDate, DateTime.Now) > 0) && (DateTime.Compare(tempStartDate, DateTime.Now) < 0)) // if not expired
                        {
                            
                            XMLImages tempImages;
                            tempImages = new XMLImages();

                            tempImages.Title = oListItem["FileLeafRef"].ToString();
                            tempURL = oListItem["FileRef"].ToString();
                            tempImages.URL = Website + tempURL.Replace(" ", "%20");
                            tempImages.Interval = tempInterval;

                            foreach (string target in tempDisplayTarget)
                                tempImages.Target.Add(target);

                            this.Images.Add(tempImages);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            this.Timestamp = DateTime.Now;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Serialize(Stream iStream)
    {
        try
        {
            XmlSerializer xml = new XmlSerializer(typeof(XMLTVData));
            xml.Serialize(iStream, this);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Serialize(string outfile)
    {
        bool retval;

        try
        {          
            TextWriter xmlout = new StreamWriter(outfile);
            XmlSerializer xml = new XmlSerializer(this.GetType());
            xml.Serialize(xmlout, this);
            xmlout.Close();

            retval = true;
        }
        catch (Exception ex)
        {
            retval = false;
            throw new Exception("Error Serializing object - " + ex.Message, ex);           
        }     

        return retval;
    }

    public XMLTVData Deserialize(Stream iStream)
    {
        XmlSerializer test = new XmlSerializer(typeof(XMLTVData));
        XMLTVData importedClass = new XMLTVData();
        return (XMLTVData)test.Deserialize(iStream);                      
    }

    public XMLTVData Deserialize(string infile)
    {
        TextReader xmlin = new StreamReader(infile);
        XmlSerializer test = new XmlSerializer(typeof(XMLTVData));
        XMLTVData importedClass = new XMLTVData();
        return (XMLTVData)test.Deserialize(xmlin);
    }
    
}
