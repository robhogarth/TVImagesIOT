using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using Windows.Storage;

namespace TVImagesIot
{
    public class AppSettings
    {
        public int ImageRotateTimer { get; set; } = 3;
        public string XMLWebAddress { get; set; } = @"http://10.10.0.26/data2.xml";
        public string LocalXMLFileName { get; set; } = "data.xml";
        public string WebUsername { get; set; } = @"newgold\peakitstats";
        public string WebPassword { get; set; } = @"WhiteRussian2";
        public string Location { get; set; } = "ITOffice";
        public int ResolutionHeight { get; set; } = 768;
        public int ResolutionWidth { get; set; } = 1366;
        public bool IgnoreImageInterval { get; set; } = false;
       

        public static async Task<AppSettings> ReadObjectFromXmlFileAsync(string filename)
        {

            LogEventSource.Log.Debug("App Settings ReadObjectFromXmlFileAsync Starting");
            // this reads XML content from a file ("filename") and returns an object  from the XML

            AppSettings objectFromXml = new AppSettings();
            

            try
            {

                var serializer = new XmlSerializer(typeof(AppSettings));
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                if ((await folder.TryGetItemAsync(filename)) != null)
                {

                    StorageFile file = await folder.GetFileAsync(filename);
                    Stream stream = await file.OpenStreamForReadAsync();

                    objectFromXml = (AppSettings)serializer.Deserialize(stream);
                    stream.Dispose();
                }
                else
                {
                    LogEventSource.Log.Debug("App Settings File doesn't exist, attempting to create from class object defaults");
                    AppSettings defaultSettings = new AppSettings();
                    await AppSettings.WriteObjectToXmlFileAsync<AppSettings>(defaultSettings, filename);                                          
                }
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error(ex.Source.ToString() + ": " + ex.Message);               
            }

            LogEventSource.Log.Debug("App Settings ReadObjectFromXmlFileAsync Completed");
            return objectFromXml;
        }

        public async void ReadObjectFromXmlFile(string filename)
        {

            LogEventSource.Log.Debug("App Settings ReadObjectFromXmlFileAsync Starting");
            // this reads XML content from a file ("filename") and returns an object  from the XML

            AppSettings objectFromXml = new AppSettings();
            
            try
            {

                var serializer = new XmlSerializer(typeof(AppSettings));
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                if ((await folder.TryGetItemAsync(filename)) != null)
                {

                    StorageFile file = await folder.GetFileAsync(filename);
                    Stream stream = await file.OpenStreamForReadAsync();

                    objectFromXml = (AppSettings)serializer.Deserialize(stream);
                    this.IgnoreImageInterval = objectFromXml.IgnoreImageInterval;
                    this.ImageRotateTimer = objectFromXml.ImageRotateTimer;
                    this.LocalXMLFileName = objectFromXml.LocalXMLFileName;
                    this.Location = objectFromXml.Location;
                    this.ResolutionHeight = this.ResolutionHeight;
                    this.ResolutionWidth = this.ResolutionWidth;
                    this.WebPassword = this.WebPassword;
                    this.WebUsername = this.WebUsername;
                        
                    stream.Dispose();
                }
                else
                {
                    LogEventSource.Log.Debug("App Settings File doesn't exist, attempting to create from class object defaults");
                    AppSettings defaultSettings = new AppSettings();
                    await AppSettings.WriteObjectToXmlFileAsync<AppSettings>(defaultSettings, filename);
                }
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error(ex.Source.ToString() + ": " + ex.Message);
            }

            LogEventSource.Log.Debug("App Settings ReadObjectFromXmlFileAsync Completed");
            //return objectFromXml;
        }

        public async void WriteObjectToXmlFile(string filename)
        {
            LogEventSource.Log.Debug("SaveObjectToXml Starting");

            try
            {
                // stores an object in XML format in file called 'filename'
                var serializer = new XmlSerializer(typeof(AppSettings));
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile file = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                Stream stream = await file.OpenStreamForWriteAsync();

                using (stream)
                {
                    serializer.Serialize(stream, this);
                }

            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error(ex.Source.ToString() + ": " + ex.Message);
            }
            LogEventSource.Log.Debug("SaveObjectToXml Completed");
        }



        public static async Task WriteObjectToXmlFileAsync<T>(T objectToSave, string filename)
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
    }
}
