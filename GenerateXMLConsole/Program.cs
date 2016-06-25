using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateXMLConsole
{
    class Program
    {


        static void Main(string[] args)
        {

            string outputFile = GenerateXMLConsole.Properties.Settings.Default.OutputFile;

            if (args.Count() > 0)
            {
                outputFile = args[0];
            }

            XMLTVDataWin shareXML = new XMLTVDataWin();

            try
            {
                
                shareXML.createDefinition(GenerateXMLConsole.Properties.Settings.Default.Website, GenerateXMLConsole.Properties.Settings.Default.SubSite, GenerateXMLConsole.Properties.Settings.Default.List);
                Console.WriteLine("Created object Definition - {0} images", shareXML.Images.Count());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating object definition from sharepoint - {0}", ex.Message);
            }

            try
            {
                shareXML.Serialize(outputFile);
                Console.WriteLine("Created xml file - {0}", outputFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating xml outputfile - {0}", ex.Message);
            }

#if DEBUG
            Console.ReadKey();
#endif
        }            
    }
}
