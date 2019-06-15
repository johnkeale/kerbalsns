using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSNS
{
    public class KerbBaseShout
    {
        public const String NODE_NAME = "KERBSHOUT";
        public const String NODE_NAME_PLURAL = "KERBSHOUTS";

        // TODO research reputation values based on the bar
        public const String RepLevel_Any = "any";
        public const String RepLevel_VeryLow = "veryLow";
        public const String RepLevel_Low = "low";
        public const String RepLevel_Medium = "medium";
        public const String RepLevel_High = "high";
        public const String RepLevel_VeryHigh = "veryHigh";
		
        public const String PosterType_Any = "any";
        public const String PosterType_LayKerbal = "layKerbal";
        public const String PosterType_VesselCrew = "vesselCrew";
        public const String PosterType_KSCEmployee = "kscEmployee"; // FIXME is this really needed??
        public const String PosterType_KSC = "ksc";
        public const String PosterType_Specific = "specific";
		
        public const String ShoutType_Random = "random";
        public const String ShoutType_RepLevel = "repLevel";
        public const String ShoutType_GameEvent = "gameEvent";
        public const String ShoutType_KSCNews = "kscNews";
        public const String ShoutType_NewsReaction = "newsReaction";

        public String name { get; set; }
        public String author { get; set; }
        public String repLevel { get; set; }
        public String posterType { get; set; }
        public Acct specificPoster { get; set; }
        public String type { get; set; }
        public String text { get; set; }
        public String[] progressReqtArray { get; set; }
        public bool isRepeatable { get; set; }
        public int vesselType { get; set; }
        public String vesselSituation { get; set; }
        public String gameEvent { get; set; }
        public ConfigNode gameEventSpecifics { get; set; }

        public virtual void LoadFromConfigNode(ConfigNode node)
        {
            this.name = "shout" + Guid.NewGuid();
            if (node.HasValue("name"))
            {
                this.name = node.GetValue("name");
            }

            this.author = null;
            if (node.HasValue("author"))
            {
                this.author = node.GetValue("author");
            }

            this.repLevel = RepLevel_Any;
            if (node.HasValue("repLevel"))
            {
                this.repLevel = node.GetValue("repLevel");
            }

            // required
            this.posterType = node.GetValue("posterType");
            if ("specific".Equals(this.posterType))
            {
                // required
                this.specificPoster = new Acct();
                this.specificPoster.LoadFromConfigNode(node.GetNode(Acct.NODE_NAME));
            }

            this.type = ShoutType_Random;
            if (node.HasValue("type"))
            {
                this.type = node.GetValue("type");
            }
            
            // required
            this.text = node.GetValue("text");
            
            this.progressReqtArray = null;
            if (node.HasValue("progressReqt"))
            {
                this.progressReqtArray = node.GetValue("progressReqt").
                    Split(new String[] { "," }, StringSplitOptions.None).Select(x => x.Trim()).ToArray();
            }

            this.isRepeatable = true;
            if (node.HasValue("isRepeatable"))
            {
                this.isRepeatable = "True".Equals(node.GetValue("isRepeatable"));
            }

            this.vesselType = KerbalSNSUtils.VesselTypeNone;
            if (node.HasValue("vesselType"))
            {
                String vesselType = node.GetValue("vesselType");
                if (vesselType.Equals("any"))
                {
                    this.vesselType = KerbalSNSUtils.VesselTypeAny;
                }
                else if (vesselType.Equals("probe"))
                {
                    this.vesselType = (int)VesselType.Probe;
                }
                else if (vesselType.Equals("relay"))
                {
                    this.vesselType = (int)VesselType.Relay;
                }
                else if (vesselType.Equals("rover"))
                {
                    this.vesselType = (int)VesselType.Rover;
                }
                else if (vesselType.Equals("lander"))
                {
                    this.vesselType = (int)VesselType.Lander;
                }
                else if (vesselType.Equals("ship"))
                {
                    this.vesselType = (int)VesselType.Ship;
                }
                else if (vesselType.Equals("plane"))
                {
                    this.vesselType = (int)VesselType.Plane;
                }
                else if (vesselType.Equals("station"))
                {
                    this.vesselType = (int)VesselType.Station;
                }
                else if (vesselType.Equals("base"))
                {
                    this.vesselType = (int)VesselType.Base;
                }
                else if (vesselType.Equals("eva"))
                {
                    this.vesselType = (int)VesselType.EVA;
                }
                else if (vesselType.Equals("flag"))
                {
                    this.vesselType = (int)VesselType.Flag;
                }
            }

            this.vesselSituation = null;
            if (node.HasValue("vesselSituation"))
            {
                this.vesselSituation = node.GetValue("vesselSituation");
            }

            this.gameEvent = null;
            this.gameEventSpecifics = null;
            if (node.HasValue("gameEvent"))
            {
                this.gameEvent = node.GetValue("gameEvent");
                if (node.HasNode("GAMEEVENTSPECIFICS"))
                {
                    this.gameEventSpecifics = node.GetNode("GAMEEVENTSPECIFICS");
                }
            }
        }

        public virtual ConfigNode SaveToConfigNode()
        {
            ConfigNode node = new ConfigNode(NODE_NAME);

            node.SetValue("name", this.name, true);

            if (this.author != null)
            {
                node.SetValue("author", this.author, true);
            }
            else
            {
                node.SetValue("author", "");
            }

            node.SetValue("repLevel", this.repLevel, true);
            node.SetValue("posterType", this.posterType, true);
            if ("specific".Equals(this.posterType))
			{
				node.AddNode(this.specificPoster.SaveToConfigNode());
			}
			node.SetValue("type", this.type, true);
            
            node.SetValue("text", this.text, true);

            if (this.progressReqtArray != null)
            {
                node.SetValue("progressReqt", String.Join(",", this.progressReqtArray), true);
            }
            else
            {
                node.SetValue("progressReqt", "");
            }

            node.SetValue("isRepeatable", this.isRepeatable, true);

            switch (this.vesselType)
            {
                case (int)VesselType.Probe:
                    node.SetValue("vesselType", "probe", true);
                    break;
                case (int)VesselType.Relay:
                    node.SetValue("vesselType", "relay", true);
                    break;
                case (int)VesselType.Rover:
                    node.SetValue("vesselType", "rover", true);
                    break;
                case (int)VesselType.Lander:
                    node.SetValue("vesselType", "lander", true);
                    break;
                case (int)VesselType.Ship:
                    node.SetValue("vesselType", "ship", true);
                    break;
                case (int)VesselType.Plane:
                    node.SetValue("vesselType", "plane", true);
                    break;
                case (int)VesselType.Station:
                    node.SetValue("vesselType", "station", true);
                    break;
                case (int)VesselType.Base:
                    node.SetValue("vesselType", "base", true);
                    break;
                case (int)VesselType.EVA:
                    node.SetValue("vesselType", "eva", true);
                    break;
                case (int)VesselType.Flag:
                    node.SetValue("vesselType", "flag", true);
                    break;
                case KerbalSNSUtils.VesselTypeAny:
                    node.SetValue("vesselType", "any", true);
                    break;
                case KerbalSNSUtils.VesselTypeNone:
                default:
                    node.SetValue("vesselType", "none", true);
                    break;
            }

            if (this.vesselSituation != null)
            {
                node.SetValue("vesselSituation", this.vesselSituation, true);
            }
            else
            {
                node.SetValue("vesselSituation", "", true);
            }

            if (this.gameEvent != null)
            {
                node.SetValue("gameEvent", this.gameEvent, true);
                if (this.gameEventSpecifics != null)
                {
                    node.AddNode("GAMEEVENTSPECIFICS", this.gameEventSpecifics);
                }
            }
            else
            {
                node.SetValue("gameEvent", "", true);
            }

            return node;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is KerbBaseShout))
            {
                return false;
            }

            KerbBaseShout other = (KerbBaseShout)obj;
            if (this.name == null && other.name != null)
            {
                return false;
            }
            else
            {
                return this.name.Equals(other.name);
            }
        }

        // debug
        public override string ToString()
        {
            var properties = GetType().GetProperties();
            var propertiesString = new StringBuilder();
            foreach (var property in properties)
            {
                propertiesString.AppendLine(property.Name + ": " + property.GetValue(this, null));
            }
            return propertiesString.ToString();
        }

        public class Acct
        {
            public const String NODE_NAME = "KERBSHOUTACCT";

            public static Acct KSC_OFFICIAL = new KerbShout.Acct("kscOfficial", "KSC_Official", "@KSC_Official");

            public String name { get; set; }
            public String fullname { get; set; }
            public String username { get; set; }

            public Acct() { }

            public Acct(String name, String fullname, String username)
            {
                this.name = name;
                this.fullname = fullname;
                this.username = username;
            }

            public void LoadFromConfigNode(ConfigNode node)
            {
                this.name = node.GetValue("name");
                this.fullname = node.GetValue("fullname");
                this.username = node.GetValue("username");
            }

            public ConfigNode SaveToConfigNode()
            {
                ConfigNode node = new ConfigNode(NODE_NAME);

                node.SetValue("name", this.name, true);
                node.SetValue("fullname", this.fullname, true);
                node.SetValue("username", this.username, true);

                return node;
            }
        }
    }
}
