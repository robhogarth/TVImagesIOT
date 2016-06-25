using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace GenerateXML
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            XMLTVDataWin shareXML = new XMLTVDataWin();

            shareXML.createDefinition();

            shareXML.Serialize(@"c:\temp\testfromsharepoint.xml");                   

        }

        private void button2_Click(object sender, EventArgs e)
        {
            

        }
    }
}
