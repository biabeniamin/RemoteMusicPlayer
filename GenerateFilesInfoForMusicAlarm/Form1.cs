using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GenerateFilesInfoForMusicAlarm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog ofd = new FolderBrowserDialog();
            ofd.SelectedPath = @"D:\OneDrive\Music";
            if (ofd.ShowDialog()==DialogResult.OK)
            {
                List<string> files = new List<string>();
                GetFiles(ref files, ofd.SelectedPath);
                RemoveNotMp3Files(ref files);
                foreach(string file in files)
                {
                    
                    richTextBox1.Text += string.Format("{0}{1};-!-;",System.IO.Path.GetDirectoryName(file).Replace(System.IO.Directory.GetParent(ofd.SelectedPath).FullName+"\\",""), file.Replace(ofd.SelectedPath,""));
                }
            }
        }
        private void RemoveNotMp3Files(ref List<string> files)
        {
            for(int i=0;i<files.Count;++i)
            {
                if(System.IO.Path.GetExtension(files[i])!=".mp3")
                {
                    files.Remove(files[i]);
                    --i;
                }
            }
        }
        private void GetFiles(ref List<string> files,string path)
        {
            string[] directories = System.IO.Directory.GetDirectories(path);
            foreach (string folder in directories)
                GetFiles(ref files, folder);
            files.AddRange(System.IO.Directory.GetFiles(path));
        }
    }
}
