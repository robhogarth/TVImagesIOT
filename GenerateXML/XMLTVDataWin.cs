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

    [XmlElement()]
    new List<XMLImagesWin> Images { get; set; } = new List<XMLImagesWin>();

    public bool createDefinition()
    {
        try
        {

            ClientContext clientContext = new ClientContext("https://caz.intranet.newgold.com/IT");
            clientContext.Credentials = new NetworkCredential(@"newgold\robert.hogarth", "Alejandro12#");
            Web oWebsite = clientContext.Web;
            ListCollection collList = oWebsite.Lists;

            List oList = collList.GetByTitle("TVImages");

            CamlQuery cQuery = new CamlQuery();

            ListItemCollection collListItem = oList.GetItems(cQuery);

            clientContext.Load(collListItem);

            clientContext.ExecuteQuery();

            DateTime tempDate;
            Boolean tempEnabled;
            string[] tempDisplayTarget;

            foreach (ListItem oListItem in collListItem)
            {
                try
                {
                    try
                    {
                        tempDate = (DateTime)oListItem["Expiry_x0020_Date"];
                    }
                    catch
                    {
                        tempDate = DateTime.Today.AddDays(1);
                    }

                    tempDisplayTarget = (string[])oListItem["DisplayTarget"];

                    tempEnabled = (Boolean)oListItem["Enabled"];

                    if (tempEnabled)
                    {
                        if (DateTime.Compare(tempDate, DateTime.Now) > 0)
                        {
                            XMLImagesWin tempImages;
                            tempImages = new XMLImagesWin();

                            tempImages.Title = oListItem["FileLeafRef"].ToString();
                            tempImages.URL = @"https://caz.intranet.newgold.com" + oListItem["FileRef"].ToString();

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
            XmlSerializer xml = new XmlSerializer(typeof(XMLTVDataWin));
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
            XmlSerializer xml = new XmlSerializer(typeof(XMLTVDataWin));
            xml.Serialize(xmlout, this);
            xmlout.Close();

            retval = true;
        }
        catch (Exception ex)
        {
            retval = false;
            throw new Exception("Error Seializing stuff - " + ex.Message, ex);           
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

[Serializable]
public class XMLImagesWin: XMLImages
{
    
}

