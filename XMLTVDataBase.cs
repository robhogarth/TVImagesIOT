using System;
using System.Collections.Generic;

namespace TVImagesIotSolution.Base
{
    
    public class XMLTVData
    {
        public List<XMLImages> Images { get; set; } = new List<XMLImages>();
        public DateTime Timestamp { get; set; }

        public void CopyProperties(XMLTVData origObj)
        {
            this.Images = origObj.Images;
            this.Timestamp = origObj.Timestamp;           
        }

    }

    public class XMLImages
    {
        public string URL { get; set; }
       
        public string Title { get; set; }
        public int Interval { get; set; }
    
        public List<string> Target { get; set; } = new List<string>();

        public bool LocationMatch(string location)
        {
            bool retval = false;

            if (this.Target.Contains("All"))
            {
                retval = true;
            }
            else
            {
                if (this.Target.Contains(location))
                    retval = true;
            }

            return retval;
        }

        
    }

}