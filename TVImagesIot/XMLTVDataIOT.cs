using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Net;
using Windows.Storage;
using TVImagesIotSolution.Base;
using Microsoft.ApplicationInsights;


namespace TVImagesIot
{
    [XmlRoot("XMLTVData")]
    public class XMLTVDataIot : XMLTVData
    {      

        public static async Task<T> ReadObjectFromXmlFileAsync<T>(string filename)
        {
            TelemetryClient tc = new TelemetryClient();
            tc.TrackEvent("ReadObjectFromXmlFileAsync");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            LogEventSource.Log.Debug("ReadObjectFromXmlFileAsync Starting");
            // this reads XML content from a file ("filename") and returns an object  from the XML
            T objectFromXml = default(T);

            try
            {

                var serializer = new XmlSerializer(typeof(T));
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                if ((await folder.TryGetItemAsync(filename)) != null)
                {

                    StorageFile file = await folder.GetFileAsync(filename);
                    Stream stream = await file.OpenStreamForReadAsync();
                    
                    objectFromXml = (T)serializer.Deserialize(stream);
                    stream.Dispose();
                }               
            }           
            catch (Exception ex)
            {
                LogEventSource.Log.Error("ReadObjectFromXMLFileAsync exception: " + ex.Source.ToString() + ": " + ex.Message);
                tc.TrackEvent("ReadObjectFromXmlFileAsync Exception");
                tc.TrackException(ex);
            }
            finally
            {
                LogEventSource.Log.Debug("ReadObjectFromXmlFileAsync Completed");
            }

            stopwatch.Stop();
            tc.TrackRequest("ReadObjectFromXmlFileAsync", DateTime.Now, stopwatch.Elapsed, "Success", true);

            return objectFromXml;

        }
        public async Task<bool> ReadObjectFromXmlFile(string filename)
        {
            bool retval = false;

            LogEventSource.Log.Debug("ReadObjectFromXmlFile Starting");
            // this reads XML content from a file ("filename") and saves it to object then copies that object properties to "this"

            XMLTVDataIot objectFromXml = default(XMLTVDataIot);

            try
            {

                var serializer = new XmlSerializer(typeof(XMLTVDataIot));
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                if ((await folder.TryGetItemAsync(filename)) != null)
                { 
                    StorageFile file = await folder.GetFileAsync(filename);
                    Stream stream = await file.OpenStreamForReadAsync();

                    objectFromXml = (XMLTVDataIot)serializer.Deserialize(stream);
                    CopyProperties(objectFromXml);
                                        
                    stream.Dispose();
                    retval = true;
                }
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error("ReadObjectFromXMLFile exception: " + ex.Source.ToString() + ": " + ex.Message);
            }
            finally
            {
                LogEventSource.Log.Debug("ReadObjectFromXmlFile Completed");
            }
            return retval;

        }

        public static async Task<T> ReadObjectFromXMLWebAddressAsync<T>(string webaddress)
        {
            
            LogEventSource.Log.Debug("ReadObjectFromXMLWebAddressAsync Starting");
            T objectFromXml = default(T);
            var serializer = new XmlSerializer(typeof(T));

            try
            {
                HttpWebRequest xmlWebRequest = HttpWebRequest.CreateHttp(webaddress);
                WebResponse xmlWebResponse = await xmlWebRequest.GetResponseAsync();

                if (xmlWebResponse.ContentLength > 0)
                {

                    Stream stream = xmlWebResponse.GetResponseStream();
                    objectFromXml = (T)serializer.Deserialize(stream);
                    stream.Dispose();
                }
                else
                {
                    throw new InvalidDataException("xmlWebResponse = 0");
                }
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error(ex.Source.ToString() + ": " + ex.Message);
                
            }

            LogEventSource.Log.Debug("ReadObjectFromXMLWebAddressAsync Completed");

            return objectFromXml;
          
        }

        public async Task<bool> ReadObjectFromXMLWebAddress(string webaddress)
        {
            bool retval = false;

            LogEventSource.Log.Debug("ReadObjectFromXMLWebAddress Starting");

            XMLTVDataIot objectFromXml = default(XMLTVDataIot);
            var serializer = new XmlSerializer(typeof(XMLTVDataIot));

            try
            {
                HttpWebRequest xmlWebRequest = HttpWebRequest.CreateHttp(webaddress);
                WebResponse xmlWebResponse = await xmlWebRequest.GetResponseAsync();

                if (xmlWebResponse.ContentLength > 0)
                {

                    Stream stream = xmlWebResponse.GetResponseStream();
                    objectFromXml = (XMLTVDataIot)serializer.Deserialize(stream);

                    CopyProperties(objectFromXml);
                                                            
                    stream.Dispose();
                    retval = true;
                }
                else
                {                  
                    LogEventSource.Log.Error("ReadObjectFromXMLWebAddress error - No Content length ");
                }
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error("ReadObjectFromXMLWebAddress error - " + ex.Source.ToString() + ": " + ex.Message);
           }
            
            LogEventSource.Log.Debug("ReadObjectFromXMLWebAddress Completed");
            return retval;
        }

        public static async Task SaveObjectToXml<T>(T objectToSave, string filename)
        {
            LogEventSource.Log.Debug("SaveObjectToXml Starting");

            try
            {
                // stores an object in XML format in file called 'filename'
                var serializer = new XmlSerializer(typeof(T));
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile file = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                Stream stream = await file.OpenStreamForWriteAsync();

                using (stream)
                {
                    serializer.Serialize(stream, objectToSave);
                }

            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error(ex.Source.ToString() + ": " + ex.Message);
            }
            LogEventSource.Log.Debug("SaveObjectToXml Completed");
        }

        public bool isFileinImages(string file)
        {
            bool retval = false;

            foreach (XMLImages image in this.Images)
            {
                if (image.Title == file)
                    retval = true;
            }

            return retval;
        }
        
    }

    [XmlRoot(ElementName = "XMLImages")]
    public class XMLImagesIot : XMLImages
    {
        
        public static async Task<string> DownloadHTTPImage(string URL, string Title, string username, string password)
        {
            
            LogEventSource.Log.Debug("DownloadHTTPImage Starting: " + URL);

            try
            {
                HttpWebRequest xmlWebRequest = HttpWebRequest.CreateHttp(URL);
                xmlWebRequest.Credentials = new NetworkCredential(username, password);

                WebResponse xmlWebResponse = await xmlWebRequest.GetResponseAsync();
                
                Stream stream = xmlWebResponse.GetResponseStream();

                LogEventSource.Log.Debug("DownloadHTTPImage downloaded file - " + Title);

                if (stream.CanRead)
                {
                    StorageFolder folder = ApplicationData.Current.LocalFolder;
                    StorageFolder imagefolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("images", CreationCollisionOption.OpenIfExists);
                    StorageFile file = await imagefolder.CreateFileAsync(Title, CreationCollisionOption.ReplaceExisting);
                    Stream filestream = await file.OpenStreamForWriteAsync();

                    try
                    {
                        await stream.CopyToAsync(filestream);
                    }
                    catch (Exception ex)
                    {
                        LogEventSource.Log.Error("Exception in DownloadHTTPImage trying to copy to local file.  Affected file is - " + Title);
                        LogEventSource.Log.Error(ex.Source.ToString() + ": " + ex.Message);
                    }

                    filestream.Dispose();
                    stream.Dispose();

                    LogEventSource.Log.Debug("DownloadHTTPImage Completed: " + URL);

                    return @"ms-appdata:///local/images/" + Title;
                }
                else
                {
                    LogEventSource.Log.Error(string.Format("Error Downloading {0}: Stream Size 0", Title));
                    return null;
                }

            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error("Exception in DownloadHTTPImage.  Affected file is - " + Title);
                LogEventSource.Log.Error(ex.Source.ToString() + ": " + ex.Message);
                return null;
            }
            
            

        }


    }
}
