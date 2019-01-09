using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Thea2ModCompiler.Command;
using Thea2ModCompiler.Model;

namespace Thea2ModCompiler.ViewModel
{
    class MainWindowVM : INotifyPropertyChanged
    {
        //main data storage
        private DataContainer DB { get; set; }

        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };

        public ObservableCollection<string> FileList { get; set; }

        //open path select dialog
        public CommandProvider BrowseClick { get; set; }
        //read files in location
        public CommandProvider LoadFileClick { get; set; }
        //Compile to main xml tree
        public CommandProvider CompileClick { get; set; }

        //parser toggles
        public CommandProvider ToggleParamFollowClick { get; set; }
        public CommandProvider UsePrototypeInferenceClick { get; set; }
        public CommandProvider AttemptMergeClick { get; set; }

        //path to root of streamables
        private string _selectedFilePath = "";
        public string selectedFilePath
        {
            get { return _selectedFilePath; }
            set
            {
                _selectedFilePath = value;
                if (null != this.PropertyChanged)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(selectedFilePath)));
                }
            }
        }

        /*parameter VM handlers*/
        private string _strictParameterFollow = "Lax paramteter parsing";
        public string _usePrototypeInference = "Do not infer from prototype";
        public string _attemptMerge = "Default to merge";

        public string StrictParameterFollow
        {
            get { return _strictParameterFollow; }
            set
            {
                _strictParameterFollow = value;
                if (null != this.PropertyChanged)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(StrictParameterFollow)));
                }
            }
        }
        public string UsePrototypeInference
        {
            get { return _usePrototypeInference; }
            set
            {
                _usePrototypeInference = value;
                if (null != this.PropertyChanged)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(UsePrototypeInference)));
                }
            }
        }
        public string AttemptMerge
        {
            get { return _attemptMerge; }
            set
            {
                _attemptMerge = value;
                if (null != this.PropertyChanged)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(AttemptMerge)));
                }
            }
        }

        public void ToggleParamFollow()
        {
            if(StrictParameterFollow == "Lax paramteter parsing")
            {
                StrictParameterFollow = "Strict paramteter parsing";
                return;
            }
            else
            {
                StrictParameterFollow = "Lax paramteter parsing";
                return;
            }
        }

        public void TogglePrototypeInference()
        {
            if (UsePrototypeInference == "Do not infer from prototype")
            {
                UsePrototypeInference = "Infer from prototype";
                return;
            }
            else
            {
                UsePrototypeInference = "Do not infer from prototype";
                return;
            }
        }

        public void ToggleAttemptMerge()
        {
            if (AttemptMerge == "Default to merge")
            {
                AttemptMerge = "Default to add";
                return;
            }
            if (AttemptMerge == "Default to add")
            {
                AttemptMerge = "Default to replace";
                return;
            }
            if (AttemptMerge == "Default to replace")
            {
                AttemptMerge = "Default to merge";
                return;
            }
        }

        private int rootFileLength;

        public MainWindowVM()
        {
            rootFileLength = 12; // "database.xml"
            DB = new DataContainer();
            //input tutorial
            List<string> _tutorial = new List<string>(new string[] { "THEA 2 MOD COMPILER",
                "", "Please point program to StreamingAssets\\database.xml", "mod folders should start with \"@\" and mirror game files",
                "eg:",
                "StreamingAssets/",
                "               @mod/",
                "                   eventModules.xml",
                "                   Modules/",
                "                           Alphaclan.xml",
                "Files compiled are eventmodules.xml, database.xml, and terrains.xml",
                "Backup of default game files saved in StreamingAssets/Backup",
                "",
                "Toggles:",
                "Strict/lax parameter parsing: Abort on parameter problem/revert to merge.",
                "Infer from prototype: when no parameter present, try and guess action from DataBase/Prototype.",
                "Defrault to: Default action if no params or prediction.",
                "",
                "Mod params:",
                "MOD_PARAM=\"OVERWRITE\":  (q,w,e) + (e,r,t) => (e,r,t)",
                "MOD_PARAM=\"MERGE\":  (q,w,e) + (e,r,t) => (q,w,e,r,t)",
                "MOD_PARAM=\"ADD\":  (q,w,e) + (e,r,t) => (q,w,e,e,r,t)"
            });
            FileList = new ObservableCollection<string>(_tutorial);
            BrowseClick = new CommandProvider((x) => Browse());
            LoadFileClick = new CommandProvider((x) => Crawl());
            CompileClick = new CommandProvider((x) => Compile());
            ToggleParamFollowClick = new CommandProvider((x) => ToggleParamFollow());
            UsePrototypeInferenceClick = new CommandProvider((x) => TogglePrototypeInference());
            AttemptMergeClick = new CommandProvider((x) => ToggleAttemptMerge());
        }

        private void Browse()
        {   //set location of database.xml
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog();
            openFileDialog1.InitialDirectory = "D:\\Steam\\steamapps\\common\\Thea 2 The Shattering\\Thea2_Data\\StreamingAssets\\database.xml";
            openFileDialog1.Filter = "database.xml (database.xml)|database.xml|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 0;

            bool? result = openFileDialog1.ShowDialog();

            if (result == true)
            {
                this.selectedFilePath = openFileDialog1.FileName;
            };
        }

        private void Crawl()
        {   //pick relevant files to display

            //if properly set
            if (this.selectedFilePath.Length > rootFileLength && this.selectedFilePath.Substring(this.selectedFilePath.Length - rootFileLength) == "database.xml")
            {
                List<string> result = new List<string>();

                //crawl through "StreamingAssets/", get all file paths (caveat, does not return files in root folder)
                DirCrawl(this.selectedFilePath.Substring(0, this.selectedFilePath.Length - rootFileLength), result);

                /* operating assumption: root database.xml is list of DB.xml files to compile to RAM during game start
                 * therefore, relevant default xmls are those included there, and relevant non-default xmls must be
                 * added there. Same applies to eventModules.xml and terrains.xml. I'm going to assume alternativeDatabase
                 * is for temporary EA compatibility and ignore it (to my peril).
                 
                 * terrains points to filenames in "Terrain Sources/", eventModules contains actual paths to event files
                 * (eg. "Modules/Death.xml") along with some XDEBUGs pulled from IDK where. database.xml has DATA nodes
                 * with 'name' tags of files in "DataBase/", and PROTOTYPE nodes with "name" tags in "DataBase/(Prototype/)".
                 * I'm going to assume we're only modding actual xmls, and ignore protos for now
                 */

                FileList.Clear();
                foreach (string x in result)
                {   //actually, dir list, but W/E
                    FileList.Add(x.Remove(0, this.selectedFilePath.Length - rootFileLength)); //add paths truncated to root
                }
            }
            else
            {
                MessageBox.Show("Please select database.xml");
            }
        }

        private List<string> DirCrawl(string dir, List<string> result) //position of crawl start, return value
        {
            try
            { //recurse through dirs
                foreach (string d in Directory.GetDirectories(dir))
                {
                    /*
                    foreach (string f in Directory.GetFiles(d))
                    {
                        result.Add(f);
                    }*/
                    if (d.Contains("@")) //borrowing mod folder convention from ArmA: mod folders start with @
                    {
                        result.Add(d); //add dirs, we don't need files
                    }
                    else
                    {   //get mod root, else recurse
                        DirCrawl(d, result);
                    }
                }
            }
            catch
            {
                MessageBox.Show("error in DirCrawl");
            }
            return result;
        }

        private void Compile()
        {
            try
            {
                if (FileList.Count > 0 && FileList.First() == "THEA 2 MOD COMPILER")
                {
                    MessageBox.Show("Scan for mods before compiling");
                }
                else
                {
                    List<string> modDirs = FileList.ToList();
                    FileList.Clear();
                    FileList.Add("reading files");
                    List<string> Log = DB.ParseData(modDirs, selectedFilePath.Remove(selectedFilePath.Length - rootFileLength), StrictParameterFollow, UsePrototypeInference, AttemptMerge);
                    Log.ForEach(FileList.Add);
                }
            }
            catch
            {
                MessageBox.Show("error in Compile");
            }
        }

    }
}
