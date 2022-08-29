using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml;

namespace FC3LockStringGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        byte[] UnlockedData = {0x0f, 0x0f, 0x07, 0x05, 0x07, 0x07, 0x07, 0x07, 0x1f, 0x1f, 0x1b, 0x1a, 0x1b, 0x1b, 0x1b, 0x1b,
                               0x2f, 0x2f, 0x2d, 0x25, 0x2d, 0x2d, 0x2d, 0x2d, 0x3f, 0x3f, 0x3e, 0x3a, 0x3e, 0x3e, 0x3e, 0x3e
        };
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox.Text))
                return;
            string key = textBox.Text;
            key = key.Length > 56 ? key.Substring(0, 56) : key;
            BlowFishCS.BlowFish blowFish = new BlowFishCS.BlowFish(Encoding.UTF8.GetBytes(key));
            byte[] encryptedData =blowFish.Encrypt_ECB(UnlockedData);
            textBox1.Text = System.Convert.ToBase64String(encryptedData);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
                return;
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Far Cry 3");
            try
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "GamerProfile.xml");
                XmlDocument xd = new XmlDocument();

                if (File.Exists(file))
                {
                    xd.Load(file);
                    
                }
                XmlElement gamerProfile = xd.DocumentElement;
                if (gamerProfile == null || gamerProfile.Name != "GamerProfile")
                {
                    gamerProfile = xd.CreateElement("GamerProfile");
                    xd.RemoveAll();
                    xd.AppendChild(gamerProfile);
                }
                XmlElement uplayProfile = gamerProfile.SelectSingleNode("UplayProfile") as XmlElement;
                if (uplayProfile == null)
                {
                    uplayProfile = xd.CreateElement("UplayProfile");
                    gamerProfile.AppendChild(uplayProfile);
                }
                
                uplayProfile.SetAttribute("LockString", textBox1.Text);
                    
                xd.Save(file);
                MessageBox.Show($"Successfully wrote \"{file}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }
    }
}
