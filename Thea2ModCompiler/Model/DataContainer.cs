using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace Thea2ModCompiler.Model
{
    class DataContainer
    {
        //file arrays, default data loaded first, then have mods added
        public List<XMLStore> Terrains { get; private set; }
        public List<XMLStore> Modules { get; private set; }
        public List<XMLStore> DB { get; private set; }
        public List<XElement> ProtoDB { get; private set; }

        //file lists, default data loaded first, then have mods added
        public XDocument TerrainsXML { get; private set; }
        public XDocument ModulesXML { get; private set; }
        public XDocument DBXML { get; private set; }

        private string RootPath { get; set; }
        private List<string> debugLog = new List<string>();

        public DataContainer()
        { //empty init
            Terrains = new List<XMLStore>();
            Modules = new List<XMLStore>();
            DB = new List<XMLStore>();
        }

        public List<string> ParseData(List<string> mods, string path, string strictParam, string inferFromProto, string defaultTo)
        {
            //clear values
            TerrainsXML = null;
            ModulesXML = null;
            DBXML = null;
            Terrains = new List<XMLStore>();
            Modules = new List<XMLStore>();
            DB = new List<XMLStore>();
            ProtoDB = new List<XElement>();

            //set behavior
            bool StrictParamParse = strictParam == "Strict paramteter parsing";
            bool InferFromProto = inferFromProto == "Infer from prototype";

            RootPath = path;
            debugLog.Clear();
            //load default data
            ReadXML(RootPath);

            //load mod data
            foreach(string dir in mods)
            {
                ReadXML(Path.Combine(path, dir));
            }

            //compile
            if (debugLog.Any(p => p.Contains("##error")))
            {
                debugLog.Add("Compile aborted");
                return debugLog;
            }
            else
            {
                string[] lines;

                /*When saving, check if the XML is just a node (most data files have no header), then
                 * save, then load as string and resave to strip them of BOM, so the game doesn't
                 * throw it's toys out of the pram.
                 */
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    Indent = true,
                    IndentChars = "    "
                };

                if (DBXML.Declaration != null)
                {
                    DBXML.Save(Path.Combine(path, "database.xml"));
                }
                else
                {
                    DBXML.Root.Save(Path.Combine(path, "database.xml"));
                }
                if (TerrainsXML.Declaration != null)
                {
                    TerrainsXML.Save(Path.Combine(path, "terrains.xml"));
                }
                else
                {
                    TerrainsXML.Root.Save(Path.Combine(path, "terrains.xml"));
                }
                if (ModulesXML.Declaration != null)
                {
                    ModulesXML.Save(Path.Combine(path, "eventModules.xml"));
                }
                else
                {
                    ModulesXML.Root.Save(Path.Combine(path, "eventModules.xml"));
                }


                lines = File.ReadAllLines(Path.Combine(path, "terrains.xml"));
                File.WriteAllLines(Path.Combine(path, "terrains.xml"), lines);
                lines = File.ReadAllLines(Path.Combine(path, "eventModules.xml"));
                File.WriteAllLines(Path.Combine(path, "eventModules.xml"), lines);
                lines = File.ReadAllLines(Path.Combine(path, "database.xml"));
                File.WriteAllLines(Path.Combine(path, "database.xml"), lines);
                
                foreach (XMLStore x in Terrains)
                {
                    if (x.data.Declaration != null)
                    {
                        x.data.Save(Path.Combine(path, "Terrain Sources/" + x.fileName));
                        lines = File.ReadAllLines(Path.Combine(path, "Terrain Sources/" + x.fileName));
                        File.WriteAllLines(Path.Combine(path, "Terrain Sources/" + x.fileName), lines);
                    }
                    else
                    {
                        using (XmlWriter xw = XmlWriter.Create(Path.Combine(path, "Terrain Sources/" + x.fileName), settings))
                        {
                            x.data.Save(xw);
                        }
                        lines = File.ReadAllLines(Path.Combine(path, "Terrain Sources/" + x.fileName));
                        File.WriteAllLines(Path.Combine(path, "Terrain Sources/" + x.fileName), lines);
                    }
                }
                foreach (XMLStore x in Modules)
                {
                    if (x.data.Declaration != null)
                    {
                        x.data.Save(Path.Combine(path, x.fileName));
                        lines = File.ReadAllLines(Path.Combine(path, x.fileName));
                        File.WriteAllLines(Path.Combine(path, x.fileName), lines);
                    }
                    else
                    {
                        using (XmlWriter xw = XmlWriter.Create(Path.Combine(path, x.fileName), settings))
                        {
                            x.data.Save(xw);
                        }
                        lines = File.ReadAllLines(Path.Combine(path, x.fileName));
                        File.WriteAllLines(Path.Combine(path, x.fileName), lines);
                    }
                }
                foreach (XMLStore x in DB)
                {
                    if(x.data.Declaration != null)
                    {
                        var foo = x.data.Declaration;
                        x.data.Save(Path.Combine(path, "DataBase/" + x.fileName));
                        lines = File.ReadAllLines(Path.Combine(path, "DataBase/" + x.fileName));
                        File.WriteAllLines(Path.Combine(path, "DataBase/" + x.fileName), lines);
                    }
                    else
                    {
                        using (XmlWriter xw = XmlWriter.Create(Path.Combine(path, "DataBase/" + x.fileName), settings))
                        {
                            x.data.Save(xw);
                        }
                        lines = File.ReadAllLines(Path.Combine(path, "DataBase/" + x.fileName));
                        File.WriteAllLines(Path.Combine(path, "DataBase/" + x.fileName), lines);
                    }
                }
                debugLog.Add("Files compiled");


                debugLog.Add("Mod compile complete");
            }


            return debugLog;
        }

        private void ReadXML(string dataPath, bool strictParamParse = false, bool inferFromProto = false, string defaultTo = "Default to merge")//path is root folder, either of game or of mod
        {
            //set paths for current dir (StreamingAssets, or @mod
            string TerrainsPath = Path.Combine(dataPath, "Terrain Sources");//for some reason,terrains 
            string ModulesPath = Path.Combine(dataPath, "");                //and DB are listed as filenames
            string DBPath = Path.Combine(dataPath, "DataBase");             //but modules as full paths.

            if (TerrainsXML == null && ModulesXML == null && DBXML == null)
            { 
                /*LOAD DEFAULT ARRAYS*/
                try
                {
                    TerrainsXML = XDocument.Load($"{Path.Combine(dataPath, "terrains.xml")}");
                    ModulesXML = XDocument.Load($"{Path.Combine(dataPath, "eventModules.xml")}");
                    DBXML = XDocument.Load($"{Path.Combine(dataPath, "database.xml")}");
                }
                catch (FileNotFoundException e)
                {   //if files not found, notify user, and abort
                    MessageBox.Show(e.Message);
                    return;
                }

                /*LOAD DEFAULT FILES*/
                foreach (XElement x in TerrainsXML.Element("TERRAINS").Elements())
                {
                    try
                    {
                        string name = x.Attribute("name").Value;
                        Terrains.Add(new XMLStore(name, XDocument.Load($"{Path.Combine(TerrainsPath, name)}")));
                        debugLog.Add("load " + name);
                    }
                    catch (FileNotFoundException e)
                    {
                        debugLog.Add("##error: " + e.Message);
                    }
                    catch
                    {
                        debugLog.Add(TerrainsPath);
                        debugLog.Add("##error in terrains.xml : element read error");
                    }
                }
                foreach (XElement x in ModulesXML.Element("ROOT").Elements())
                {
                    try
                    {
                        string name = x.Attribute("path").Value;
                        if (!name.Contains("DEBUG"))
                        {
                            Modules.Add(new XMLStore(name, XDocument.Load($"{Path.Combine(ModulesPath, name)}")));
                            debugLog.Add("load " + name);
                        }
                        else
                        {
                            debugLog.Add("skip " + name);
                        }
                    }
                    catch (FileNotFoundException e)
                    {
                        debugLog.Add("##error: " + e.Message);
                    }
                    catch
                    {
                        debugLog.Add(TerrainsPath);
                        debugLog.Add("##error in eventModules.xml : element read error");
                    }
                }
                foreach (XElement x in DBXML.Element("DATA_BASE").Elements())
                {
                    try
                    {
                        string name = x.Attribute("name").Value;
                        if (!name.Contains("Prototype"))
                        {
                            DB.Add(new XMLStore(name, XDocument.Load($"{Path.Combine(DBPath, name)}")));
                            debugLog.Add("load " + name);
                        }
                        else
                        {
                            //crawl through /DataBase/Proto for potential arrays
                            XDocument newFile = XDocument.Load($"{Path.Combine(DBPath, name)}");
                            foreach(XElement y in newFile.Root.Elements())
                            {
                                getArrays(y);
                            }
                        }
                    }
                    catch (FileNotFoundException e)
                    {
                        debugLog.Add("##error: " + e.Message);
                    }
                    catch
                    {
                        debugLog.Add(DBPath);
                        debugLog.Add("##error in database.xml : name = null");
                    }
                }

                //back up files before altering
                Backup(dataPath);
                return;
            }
            else
            {
                /*LOAD MOD DATA*/
                /*First load the index files from the mod folders, (if they're missing, log and continue).
                 * Then read the indexes, then start reading new files, merging or adding to XMLStore
                 * and main index as you go.
                 */
                XDocument newTerrainsXML = null;
                XDocument newModulesXML = null;
                XDocument newDBXML = null;
                debugLog.Add("Loading " + dataPath);
                try
                {
                    newTerrainsXML = XDocument.Load($"{Path.Combine(dataPath, "terrains.xml")}");
                    debugLog.Add("terrains.xml loaded");
                    foreach (XElement x in newTerrainsXML.Root.Elements())
                    {
                        string fileName;
                        XDocument newFile;
                        try
                        {//get file, if it exists, else log and skip
                            fileName = x.Attribute("name").Value;
                            newFile = XDocument.Load($"{Path.Combine(TerrainsPath, fileName)}");
                            debugLog.Add("load " + fileName);
                        }
                        catch (FileNotFoundException e)
                        {
                            debugLog.Add("##error: " + e.Message + ", skipping");
                            continue;
                        }
                        catch
                        {
                            debugLog.Add(TerrainsPath);
                            debugLog.Add("##error in terrains.xml : element read error");
                            continue;
                        }

                        if (!TerrainsXML.Root.Elements().Any(p => p.Attribute("name").Value != fileName))
                        {//if data files are not already listed, add them to the index and load file to file list
                            XElement newEntry = new XElement("DEFINITION");
                            newEntry.SetAttributeValue("name", x.Attribute("name").Value);
                            TerrainsXML.Root.Add(newEntry);
                            Terrains.Add(new XMLStore(fileName, newFile));
                        }
                        else
                        {//else, update existing
                            XMLStore oldFile = Terrains.Where(p => p.fileName == fileName).FirstOrDefault();
                            //note, oldfile and newfile are disparate object types
                            List<string> output = oldFile.Update(newFile, strictParamParse, inferFromProto, defaultTo, ProtoDB);
                            debugLog.AddRange(output);
                        }
                    }
                }
                catch (FileNotFoundException)
                {//not an error, per se, just not used
                    debugLog.Add("terrains.xml not found");
                }
                try
                {
                    newModulesXML = XDocument.Load($"{Path.Combine(dataPath, "eventModules.xml")}");
                    debugLog.Add("eventModules.xml loaded");
                    foreach (XElement x in newModulesXML.Root.Elements())
                    {
                        string fileName;
                        XDocument newFile;
                        try
                        {//get file, if it exists, else log and skip
                            fileName = x.Attribute("path").Value;
                            newFile = XDocument.Load($"{Path.Combine(ModulesPath, fileName)}");
                            debugLog.Add("load " + fileName);
                        }
                        catch (FileNotFoundException e)
                        {
                            debugLog.Add("##error: " + e.Message + ", skipping");
                            continue;
                        }
                        catch
                        {
                            debugLog.Add(ModulesPath);
                            debugLog.Add("##error in eventModules.xml : element read error");
                            continue;
                        }

                        if (!ModulesXML.Root.Elements().Any(p => p.Attribute("path").Value == fileName))
                        {//if data files are not already listed, add them to the index and load file to file list
                            XElement newEntry = new XElement("FILE");
                            newEntry.SetAttributeValue("path", x.Attribute("path").Value);
                            ModulesXML.Root.Add(newEntry);
                            Modules.Add(new XMLStore(fileName, newFile));
                        }
                        else
                        {//else, update existing
                            XMLStore oldFile = Modules.Where(p => p.fileName == fileName).FirstOrDefault();
                            //note, oldfile and newfile are disparate object types
                            List<string> output = oldFile.Update(newFile, strictParamParse, inferFromProto, defaultTo, ProtoDB);
                            debugLog.AddRange(output);
                        }
                    }
                }
                catch (FileNotFoundException)
                {//not an error, per se, just not used
                    debugLog.Add("eventModules.xml not found");
                }
                try
                {
                    newDBXML = XDocument.Load($"{Path.Combine(dataPath, "database.xml")}");
                    debugLog.Add("database.xml loaded");
                    foreach (XElement x in newDBXML.Root.Elements())
                    {
                        string fileName;
                        XDocument newFile;
                        try
                        {//get file, if it exists, else log and skip
                            fileName = x.Attribute("name").Value;
                            newFile = XDocument.Load($"{Path.Combine(DBPath, fileName)}");
                            debugLog.Add("load " + fileName);
                        }
                        catch (FileNotFoundException e)
                        {
                            debugLog.Add("##error: " + e.Message + ", skipping");
                            continue;
                        }
                        catch
                        {
                            debugLog.Add(ModulesPath);
                            debugLog.Add("##error in database.xml : element read error");
                            continue;
                        }

                        if (!DBXML.Root.Elements().Any(p => p.Attribute("name").Value != fileName))
                        {//if data files are not already listed, add them to the index and load file to file list
                            XElement newEntry = new XElement("DATA");
                            newEntry.SetAttributeValue("name", x.Attribute("name").Value);
                            ModulesXML.Root.Add(newEntry);
                            Modules.Add(new XMLStore(fileName, newFile));
                        }
                        else
                        {//else, update existing
                            XMLStore oldFile = DB.Where(p => p.fileName == fileName).FirstOrDefault();
                            //note, oldfile and newfile are disparate object types
                            List<string> output = oldFile.Update(newFile, strictParamParse, inferFromProto, defaultTo, ProtoDB);
                            debugLog.AddRange(output);
                        }
                    }
                }
                catch (FileNotFoundException)
                {//not an error, per se, just not used
                    debugLog.Add("database.xml not found");
                }
            }
        }

        private void getArrays(XElement node)
        {
            if(node.Attributes().Any(p => p.Name == "Type"))
            {
                if (node.Attribute("Type").Value.Contains("Array"))
                {
                    ProtoDB.Add(node);
                    debugLog.Add("Add proto node: " + node.Name);
                }
            }
            else
            {
                foreach (XElement x in node.Elements())
                {
                    getArrays(x);
                }
            }
        }

        private void Backup(string rootPath)
        {

            System.IO.Directory.CreateDirectory(Path.Combine(rootPath, "Backup"));
            System.IO.Directory.CreateDirectory(Path.Combine(rootPath, "Backup/DataBase"));
            System.IO.Directory.CreateDirectory(Path.Combine(rootPath, "Backup/Modules"));
            System.IO.Directory.CreateDirectory(Path.Combine(rootPath, "Backup/Terrain Sources"));
            if(File.Exists(Path.Combine(rootPath, "Backup/terrains.xml")))
            {
                /*if backups are already there, the game's already been modded to some extent
                 * backing up now would back up mods, meaning you can break the backup by running
                 * the compiler twice)
                 */
                debugLog.Add("Skipping backup");
                return;
            }
            debugLog.Add("Create debug dirs");

            TerrainsXML.Save(Path.Combine(rootPath, "Backup/terrains.xml"));
            ModulesXML.Save(Path.Combine(rootPath, "Backup/eventModules.xml"));
            DBXML.Save(Path.Combine(rootPath, "Backup/database.xml"));
            debugLog.Add("Backup XML collections");

            foreach(XMLStore x in Terrains)
            {
                x.data.Save(Path.Combine(rootPath, "Backup/Terrain Sources/" + x.fileName));
            }
            foreach (XMLStore x in Modules)
            {
                x.data.Save(Path.Combine(rootPath, "Backup/" + x.fileName));
            }
            foreach (XMLStore x in DB)
            {
                x.data.Save(Path.Combine(rootPath, "Backup/DataBase/" + x.fileName));
            }
            debugLog.Add("Backup files");
        }
    }

    class XMLStore
    {
        public string fileName { get; set; }
        public XDocument data { get; set; }

        public XMLStore(string name, XDocument elements)
        {
            fileName = name;
            data = elements;
        }

        public List<string> Update(XDocument newData, bool strictParamParse, bool inferFromProto, string defaultTo, List<XElement> ProtoDB)
        {//param switches are a backup in case this gets split later on.
            List<string> debugLog = new List<string>();

            //update declarations
            if (newData.Declaration != null && data.Declaration.Encoding != newData.Declaration.Encoding)
            {
                data.Declaration.Encoding = newData.Declaration.Encoding;
                debugLog.Add("set Encoding: " + newData.Declaration.Encoding);
            }
            if (newData.Declaration != null && data.Declaration.Standalone != newData.Declaration.Standalone)
            {
                data.Declaration.Standalone = newData.Declaration.Standalone;
                debugLog.Add("set Standalone: " + newData.Declaration.Standalone);
            }
            if (newData.Declaration != null && data.Declaration.Version != newData.Declaration.Version)
            {
                data.Declaration.Version = newData.Declaration.Version;
                debugLog.Add("set Version: " + newData.Declaration.Version);
            }

            //Update elements
            List<string> output = updateNode(data.Root, newData.Root, strictParamParse, inferFromProto, defaultTo, ProtoDB); //recursive scan, see implementation
            debugLog.AddRange(output); 
            return debugLog;
        }

        private List<string> updateNode(XElement oldNode, XElement newNode, bool strictParamParse, bool inferFromProto, string defaultTo, List<XElement> ProtoDB)
        {
            List<string> debugLog = new List<string>();

            //update root attribs
            foreach (XAttribute x in newNode.Attributes())
            {//overwrite attribs
                if (oldNode.Attribute(x.Name) == null || oldNode.Attribute(x.Name).Value != x.Value)
                {
                    oldNode.Attribute(x.Name).Value = x.Value;
                    debugLog.Add("set attrib" + x.Name + " to " + x.Value);
                }
            }

            /*checking if node is tagged with MOD_PARAM attribute
             * the tag is not game-related, and instead tells the
             * mod compiler how to handle the nodes below it
             */
            if (newNode.Attribute("MOD_PARAM") == new XAttribute("MOD_PARAM", "OVERWRITE"))
            {//overwrite
                oldNode.ReplaceWith(newNode);
                debugLog.Add("Replaced node " + oldNode.Name + " in " + fileName);
                return debugLog;
            }

            if (newNode.Attribute("MOD_PARAM") == new XAttribute("MOD_PARAM", "MERGE"))
            {//loop through children, judge case by case
                foreach (XElement x in newNode.Elements())
                {
                    IEnumerable<XElement> Matches = oldNode.Elements().Where(p=>p.Name == x.Name);
                    if(Matches.Count() == 0)
                    {//no matching nodes, add new one
                        if (strictParamParse)
                        {
                            debugLog.Add("##error, no matching nodes for," + x.Name + " in " + fileName);
                        }
                        else
                        {
                            oldNode.Add(x);
                            debugLog.Add("##Warning, no matching nodes," + x.Name + " added to " + oldNode.Name + " in " + fileName);
                        }
                    }
                    else if (Matches.Count() == 1)
                    {//match found, continue recursion
                        List<string> output = updateNode(Matches.First(), x, strictParamParse, inferFromProto, defaultTo, ProtoDB);
                        debugLog.Add("Merged " + x.Name + "to " + oldNode.Name + " in " + fileName);
                        debugLog.AddRange(output);
                    }
                    else
                    {//multiple matches, refine
                        foreach(XAttribute y in x.Attributes())
                        {//loop through attribs, until you find the relevant node
                            if(Matches.Count() > 1)
                            {
                                Matches = Matches.Where(p => p.Attribute(y.Name) != null && p.Attribute(y.Name).Value == x.Attribute(y.Name).Value);
                            }
                        }
                        if (Matches.Count() == 0)
                        {//no matching nodes, add new one
                            if (strictParamParse)
                            {
                                debugLog.Add("##error, no matching nodes for," + x.Name + " in " + fileName);
                            }
                            else
                            {
                                oldNode.Add(x);
                                debugLog.Add("##Warning, no matching nodes," + x.Name + " added to " + oldNode.Name + " in " + fileName);
                            }
                        }
                        else if (Matches.Count() == 1)
                        {//match found, continue
                            List<string> output = updateNode(Matches.First(), x, strictParamParse, inferFromProto, defaultTo, ProtoDB);
                            debugLog.Add("Merged " + x.Name + "to " + oldNode.Name + " in " + fileName);
                            debugLog.AddRange(output);
                        }
                        else
                        {//multiple matches, assume array, add, but log
                            if (strictParamParse)
                            {
                                debugLog.Add("##error, multiple matching nodes for," + x.Name + " in " + fileName);
                            }
                            else
                            {
                                oldNode.Add(x);
                                debugLog.Add("##Warning: " + x.Name + "listed under " + oldNode.Name + "as array, in " + fileName + ", possible error");
                            }
                        }
                    }
                }
                return debugLog;
            }

            if (newNode.Attribute("MOD_PARAM") == new XAttribute("MOD_PARAM", "ADD"))
            {//is an array, just add
                foreach (XElement x in newNode.Elements())
                {
                    oldNode.Add(x);
                    debugLog.Add("Added " + x.Name + "to "+ oldNode.Name + " in " + fileName);
                }
                return debugLog;
            }

            /*Here, I call upon the protoype files, in hopes of finding if things are
             * supposed to be arrays or just values. Arrays are added to the file, no
             * questions asked. Values attempt merging.
             * 
             * In other words, I basically turn to divination*/

            if (inferFromProto && ProtoDB.Any(p=>p.Name == newNode.Name))
            {//fuzzy match says it's an array, add it.
                foreach (XElement x in newNode.Elements())
                {
                    oldNode.Add(x);
                    debugLog.Add("Added " + x.Name + "to " + oldNode.Name + " in " + fileName);
                }
                return debugLog;
            }

            if (defaultTo == "Default to add")
            {//add
                foreach (XElement x in newNode.Elements())
                {
                    oldNode.Add(x);
                    debugLog.Add("Added " + x.Name + "to " + oldNode.Name + " in " + fileName);
                }
                return debugLog;
            }
            if (defaultTo == "Default to replace")
            {//overwrite
                oldNode.ReplaceWith(newNode);
                debugLog.Add("Replaced node " + oldNode.Name + " in " + fileName);
                return debugLog;
            }
            if (defaultTo == "Default to merge")
            {//loop through children, judge case by case
                foreach (XElement x in newNode.Elements())
                {
                    IEnumerable<XElement> Matches = oldNode.Elements().Where(p => p.Name == x.Name);
                    if (Matches.Count() == 0)
                    {//no matching nodes, add new one
                        oldNode.Add(x);
                        debugLog.Add("##Warning, no matching nodes," + x.Name + " added to " + oldNode.Name + " in " + fileName);
                    }
                    else if (Matches.Count() == 1)
                    {//match found, continue recursion
                        List<string> output = updateNode(Matches.First(), x, strictParamParse, inferFromProto, defaultTo, ProtoDB);
                        debugLog.Add("Merged " + x.Name + "to " + oldNode.Name + " in " + fileName);
                        debugLog.AddRange(output);
                    }
                    else
                    {//multiple matches, refine
                        foreach (XAttribute y in x.Attributes())
                        {//loop through attribs, until you find the relevant node
                            if (Matches.Count() > 1)
                            {
                                Matches = Matches.Where(p => p.Attribute(y.Name) !=null && p.Attribute(y.Name).Value == x.Attribute(y.Name).Value);
                            }
                        }
                        if (Matches.Count() == 0)
                        {//no matching nodes, add new one
                            oldNode.Add(x);
                            debugLog.Add("##Warning, no matching nodes," + x.Name + " added to " + oldNode.Name + " in " + fileName);
                        }
                        else if (Matches.Count() == 1)
                        {//match found, continue
                            List<string> output = updateNode(Matches.First(), x, strictParamParse, inferFromProto, defaultTo, ProtoDB);
                            debugLog.Add("Merged " + x.Name + "to " + oldNode.Name + " in " + fileName);
                            debugLog.AddRange(output);
                        }
                        else
                        {//multiple matches, assume array, add, but log
                            oldNode.Add(x);
                            debugLog.Add("##Warning: " + x.Name + "listed under " + oldNode.Name + " as array, in " + fileName + ", possible error");
                        }
                    }
                }
                return debugLog;
            }
            debugLog.Add("##error, default param error: " + defaultTo);
            return debugLog;
        }
    }
}

