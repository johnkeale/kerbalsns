using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSNS
{
    public class KerbBaseStory
    {
        public const String NODE_NAME = "KERBSTORY";
        public const String NODE_NAME_PLURAL = "KERBSTORIES";

        public enum StoryType
        {
            Random,
            Situational,
        }

        public String name { get; set; }
        public String author { get; set; }
        public StoryType type { get; set; }
        public String text { get; set; }
        public String[] progressReqtArray { get; set; }
        public bool isRepeatable { get; set; }
        public int vesselType { get; set; }
        public String vesselSituation { get; set; }

        public virtual void LoadFromConfigNode(ConfigNode node)
        {
            this.name = "story" + Guid.NewGuid();
            if (node.HasValue("name"))
            {
                this.name = node.GetValue("name");
            }

            this.author = null;
            if (node.HasValue("author"))
            {
                this.author = node.GetValue("author");
            }

            this.type = StoryType.Random;
            if (node.HasValue("type"))
            {
                String type = node.GetValue("type");
                if ("situational".Equals(type))
                {
                    this.type = StoryType.Situational;
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

            // Unlike shouts, stories are supposed to happen on vessels, so VesselTypeNone is not applicable here
            this.vesselType = KerbalSNSUtils.VesselTypeAny;
            if (node.HasValue("vesselType"))
            {
                String vesselType = node.GetValue("vesselType");
                if (vesselType.Equals("probe"))
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

            switch (this.type)
            {
                case StoryType.Situational:
                    node.SetValue("type", "situational", true);
                    break;
                case StoryType.Random:
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
                default:
                    node.SetValue("vesselType", "any", true);
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

            KerbBaseStory other = (KerbBaseStory) obj;
            if (this.name == null && other.name != null)
            {
                return false;
            } else
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
    }
}
