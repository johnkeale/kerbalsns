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

        public enum RepLevel
        {
            // TODO research reputation values based on the bar
            Any,
            VeryLow,
            Low,
            Medium,
            High,
            VeryHigh,
        }

        public enum ShoutPoster
        {
            Any,
            LayKerbal,
            VesselCrew,
            KSCEmployee, // FIXME is this really needed??
            KSC,
            Specific,
        }

        public enum ShoutType
        {
            Random,
            RepLevel,
            KSCNews,
        }

        public String name { get; set; }
        public RepLevel repLevel { get; set; }
        public ShoutPoster poster { get; set; }
        public Acct specificPoster { get; set; }
        public ShoutType type { get; set; }
        public String text { get; set; }
        public String[] progressReqtArray { get; set; }
        public bool isRepeatable { get; set; }

        public virtual void LoadFromConfigNode(ConfigNode node)
        {
            this.name = "shout" + Guid.NewGuid();
            if (node.HasValue("name"))
            {
                this.name = node.GetValue("name");
            }

            this.repLevel = RepLevel.Any;
            if (node.HasValue("repLevel"))
            {
	            String repLevel = node.GetValue("repLevel");
	            if ("veryLow".Equals(repLevel))
	            {
	                this.repLevel = RepLevel.VeryLow;
	            }
                else if ("low".Equals(repLevel))
	            {
	                this.repLevel = RepLevel.Low;
	            }
                else if ("medium".Equals(repLevel))
	            {
	                this.repLevel = RepLevel.Medium;
	            }
                else if ("high".Equals(repLevel))
	            {
	                this.repLevel = RepLevel.High;
	            }
                else if ("veryHigh".Equals(repLevel))
	            {
	                this.repLevel = RepLevel.VeryHigh;
	            }
            }

            // required
            this.poster = ShoutPoster.Any;
            String poster = node.GetValue("poster");
            if ("layKerbal".Equals(poster))
            {
                this.poster = ShoutPoster.LayKerbal;
            }
            else if ("vesselCrew".Equals(poster))
            {
                this.poster = ShoutPoster.VesselCrew;
            }
            else if ("kscEmployee".Equals(poster))
            {
                this.poster = ShoutPoster.KSCEmployee;
            }
            else if ("ksc".Equals(poster))
            {
                this.poster = ShoutPoster.KSC;
            }
            else if ("specific".Equals(poster))
            {
                this.poster = ShoutPoster.Specific;

                // required
                this.specificPoster = new Acct();
                this.specificPoster.LoadFromConfigNode(node.GetNode(Acct.NODE_NAME));
            }

            this.type = ShoutType.Random;
            if (node.HasValue("type"))
            {
                String type = node.GetValue("type");
                if ("repLevel".Equals(type))
                {
                    this.type = ShoutType.RepLevel;
                }
                if ("kscNews".Equals(type))
                {
                    this.type = ShoutType.KSCNews;
                }
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
        }

        public virtual ConfigNode SaveToConfigNode()
        {
            ConfigNode node = new ConfigNode(NODE_NAME);

            node.SetValue("name", this.name, true);

            switch (this.repLevel)
            {
                case RepLevel.VeryLow:
                    node.SetValue("repLevel", "veryLow", true);
                    break;
                case RepLevel.Low:
                    node.SetValue("repLevel", "low", true);
                    break;
                case RepLevel.Medium:
                    node.SetValue("repLevel", "medium", true);
                    break;
                case RepLevel.High:
                    node.SetValue("repLevel", "high", true);
                    break;
                case RepLevel.VeryHigh:
                    node.SetValue("repLevel", "veryHigh", true);
                    break;
                case RepLevel.Any:
                default:
                    node.SetValue("type", "random", true);
                    break;
            }

            switch (this.poster)
            {
                case ShoutPoster.LayKerbal:
                    node.SetValue("poster", "layKerbal", true);
                    break;
                case ShoutPoster.VesselCrew:
                    node.SetValue("poster", "vesselCrew", true);
                    break;
                case ShoutPoster.KSCEmployee:
                    node.SetValue("poster", "kscEmployee", true);
                    break;
                case ShoutPoster.KSC:
                    node.SetValue("poster", "ksc", true);
                    break;
                case ShoutPoster.Specific:
                    {
                        node.SetValue("poster", "specific", true);

                        node.AddNode(this.specificPoster.SaveToConfigNode());
                    }
                    break;
                case ShoutPoster.Any:
                default:
                    node.SetValue("poster", "any", true);
                    break;
            }

            switch (this.type)
            {
                case ShoutType.RepLevel:
                    node.SetValue("type", "repLevel", true);
                    break;
                case ShoutType.KSCNews:
                    node.SetValue("type", "kscNews", true);
                    break;
                case ShoutType.Random:
                default:
                    node.SetValue("type", "random", true);
                    break;
            }
            
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

            if (!(obj is KerbBaseStory))
            {
                return false;
            }

            KerbBaseStory other = (KerbBaseStory)obj;
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
