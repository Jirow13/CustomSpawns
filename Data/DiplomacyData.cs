﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.Core;

namespace CustomSpawns.Data
{
    public class DiplomacyDataManager
    {

        static DiplomacyDataManager _instance;

        public static DiplomacyDataManager Instance
        {
            get
            {
                return _instance ?? new DiplomacyDataManager();
            }
            private set
            {
                _instance = value;

            }
        }

        public static void ClearInstance(Main caller)
        {
            if (caller == null)
                return;
            _instance = null;
        }

        private Dictionary<string, DiplomacyData> data = new Dictionary<string, DiplomacyData>();

        public MBReadOnlyDictionary<string, DiplomacyData> Data
        {
            get
            {
                return data.GetReadOnlyDictionary();
            }
        }

        private DiplomacyDataManager()
        {
            if (!Main.isAPIMode)
            {
                string path = Path.Combine(BasePath.Name, "Modules", "CustomSpawns", "ModuleData", "Data", "Diplomacy.xml");
                ConstructListFromXML(path);
            }
            foreach (var subMod in ModIntegration.SubModManager.dependentModsArray)
            {
                string path = Path.Combine(subMod.CustomSpawnsDirectoryPath, "Diplomacy.xml");
                if (File.Exists(path))
                    ConstructListFromXML(path);
            }
        }

        private void ConstructListFromXML(string path)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);

                foreach (XmlNode node in doc.DocumentElement)
                {
                    if (node.NodeType == XmlNodeType.Comment)
                        continue;
                    DiplomacyData diplomacyData = new DiplomacyData();
                    if(node.Attributes["target"] == null || node.Attributes["target"].InnerText == "")
                    {
                        throw new Exception("Each diplomacy data instance must have a target faction!");
                    }
                    diplomacyData.clanString = node.Attributes["target"].InnerText;
                    if (node["ForceWarPeaceBehaviour"] != null)
                    {
                        //handle forced war peace data.
                        diplomacyData.ForcedWarPeaceDataInstance = new DiplomacyData.ForcedWarPeaceData();
                        XmlElement forceNode = node["ForceWarPeaceBehaviour"];
                        HandleForcedWarPeaceBehaviourData(forceNode, diplomacyData);
                    }
                    if(node["ForceNoKingdom"] != null)
                    {
                        //handle forcing of no parent kingdoms.
                        bool result;
                        if(!bool.TryParse(node["ForceNoKingdom"].InnerText, out result))
                        {
                            throw new Exception("ForceNoKingdom must be a boolean value!");
                        }
                        diplomacyData.ForceNoKingdom = result;
                    }

                    data.Add(diplomacyData.clanString, diplomacyData);
                }

            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(e, "Diplomacy Data Parsing of " + path);
            }
        }

        private void HandleForcedWarPeaceBehaviourData(XmlElement forceNode, DiplomacyData diplomacyData)
        {
            foreach (XmlNode forceNodeChild in forceNode)
            {
                if (forceNodeChild.NodeType == XmlNodeType.Comment)
                    continue;
                if (forceNodeChild.Name == "ForcedWarSpecial")
                {
                    //handle forced war special.
                    if (forceNodeChild.Attributes["flag"] == null)
                    {
                        throw new Exception("Each forced war special data must have a flag.");
                    }
                    string flag = forceNodeChild.Attributes["flag"].InnerText;
                    switch (flag)
                    {
                        case "all": //handle case where All clans except maybe some are designated as enemies.
                            List<string> exceptions = new List<string>();
                            List<string> exceptionKingdoms = new List<string>();
                            int j = 0;
                            string st = "but";
                            while (true)
                            {
                                string s1 = st + "_" + j.ToString();
                                if (forceNodeChild.Attributes[s1] == null || forceNodeChild.Attributes[s1].InnerText == "")
                                {
                                    break;
                                }
                                else
                                {
                                    exceptions.Add(forceNodeChild.Attributes[s1].InnerText);
                                }
                                j++;
                            }
                            j = 0;
                            st = "but_kingdom";
                            while (true)
                            {
                                string s1 = st + "_" + j.ToString();
                                if (forceNodeChild.Attributes[s1] == null || forceNodeChild.Attributes[s1].InnerText == "")
                                {
                                    break;
                                }
                                else
                                {
                                    exceptionKingdoms.Add(forceNodeChild.Attributes[s1].InnerText);
                                }
                                j++;
                            }
                            foreach (Clan c in Clan.All)
                            {
                                string stringID = c.StringId;
                                if (!exceptions.Contains(stringID))
                                    diplomacyData.ForcedWarPeaceDataInstance.atWarClans.Add(c);

                            }
                            foreach(Kingdom k in Kingdom.All)
                            {
                                string stringID = k.StringId;
                                if (exceptionKingdoms.Contains(stringID))
                                    diplomacyData.ForcedWarPeaceDataInstance.exceptionKingdoms.Add(k);
                            }
                            break;
                        default:
                            throw new Exception("Invalid forced war special data flag detected");
                    }
                }
            }
        }

    }

    public class DiplomacyData
    {
        public string clanString;

        public class ForcedWarPeaceData
        {
            public List<Clan> atWarClans = new List<Clan>();
            public List<Kingdom> exceptionKingdoms = new List<Kingdom>();
        }

        public ForcedWarPeaceData ForcedWarPeaceDataInstance;

        public bool ForceNoKingdom { get; set; }

    }
}
