using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSNS
{
    public class KerbShoutout
    {
        public enum RepLevel
        {
            Unknown = -1,
            Any,
            Low,
            Medium,
            High,
            VeryHigh,
        }

        public enum ShoutoutPoster
        {
            Unknown = -1,
            Any,
            Citizen,
            VesselCrew,
            KSCEmployee,
            KSC,
            Specific, // TODO
        }

        public enum ShoutoutType
        {
            Unknown = -1,
            RepLevel,
            LameJoke,
            Crew,
            KSCNews,
			NewsReponse, // TODO
			Random, // TODO
			Nonsense, // TODO
        }

        public String name { get; set; }
        public RepLevel repLevel { get; set; }
        public ShoutoutPoster poster { get; set; }
		public String specificPoster; // TODO
        public ShoutoutType type { get; set; }
        public String shoutout { get; set; }
		public String reqdMilestone; // TODO

        public String postedId { get; set; }
        public String postedBy { get; set; }
        public double postedTime { get; set; }
        public String postedShoutout { get; set; }

        public void LoadFromConfigNode(ConfigNode node)
        {
            this.name = node.GetValue("name");

            this.repLevel = RepLevel.Unknown;
            String repLevel = node.GetValue("repLevel");
            if ("any".Equals(repLevel))
            {
                this.repLevel = RepLevel.Any;
            }
            if ("low".Equals(repLevel))
            {
                this.repLevel = RepLevel.Low;
            }
            if ("medium".Equals(repLevel))
            {
                this.repLevel = RepLevel.Medium;
            }
            if ("high".Equals(repLevel))
            {
                this.repLevel = RepLevel.High;
            }
            if ("veryHigh".Equals(repLevel))
            {
                this.repLevel = RepLevel.VeryHigh;
            }

            String poster = node.GetValue("poster");
            if ("any".Equals(poster))
            {
                this.poster = ShoutoutPoster.Any;
            }
            if ("citizen".Equals(poster))
            {
                this.poster = ShoutoutPoster.Citizen;
            }
            if ("vesselCrew".Equals(poster))
            {
                this.poster = ShoutoutPoster.VesselCrew;
            }
            if ("kscEmployee".Equals(poster))
            {
                this.poster = ShoutoutPoster.KSCEmployee;
            }
            if ("ksc".Equals(poster))
            {
                this.poster = ShoutoutPoster.KSC;
            }

            this.type = ShoutoutType.Unknown;
            String type = node.GetValue("type");
            if ("repLevel".Equals(type))
            {
                this.type = ShoutoutType.RepLevel;
            }
            if ("lameJoke".Equals(type))
            {
                this.type = ShoutoutType.LameJoke;
            }
            if ("crew".Equals(type))
            {
                this.type = ShoutoutType.Crew;
            }
            if ("kscNews".Equals(type))
            {
                this.type = ShoutoutType.KSCNews;
            }

            this.shoutout = node.GetValue("shoutout");

            this.postedId = node.GetValue("postedId");
            this.postedBy = node.GetValue("postedBy");
            if (node.GetValue("postedTime") != null)
            {
                this.postedTime = Double.Parse(node.GetValue("postedTime"));
            }
            this.postedShoutout = node.GetValue("postedShoutout");
        }

        public ConfigNode SaveToConfigNode()
        {
            ConfigNode node = new ConfigNode("KERBSHOUTOUT");

            node.SetValue("name", this.name, true);

            if (this.repLevel == RepLevel.Unknown)
            {
                node.SetValue("repLevel", "unknown", true);
            }
            if (this.repLevel == RepLevel.Any)
            {
                node.SetValue("repLevel", "any", true);
            }
            if (this.repLevel == RepLevel.Low)
            {
                node.SetValue("repLevel", "low", true);
            }
            if (this.repLevel == RepLevel.Medium)
            {
                node.SetValue("repLevel", "medium", true);
            }
            if (this.repLevel == RepLevel.High)
            {
                node.SetValue("repLevel", "high", true);
            }
            if (this.repLevel == RepLevel.VeryHigh)
            {
                node.SetValue("repLevel", "veryHigh", true);
            }

            if (this.poster == ShoutoutPoster.Unknown)
            {
                node.SetValue("poster", "unknown", true);
            }
            if (this.poster == ShoutoutPoster.Any)
            {
                node.SetValue("poster", "any", true);
            }
            if (this.poster == ShoutoutPoster.Citizen)
            {
                node.SetValue("poster", "citizen", true);
            }
            if (this.poster == ShoutoutPoster.VesselCrew)
            {
                node.SetValue("poster", "vesselCrew", true);
            }
            if (this.poster == ShoutoutPoster.KSCEmployee)
            {
                node.SetValue("poster", "kscEmployee", true);
            }
            if (this.poster == ShoutoutPoster.KSC)
            {
                node.SetValue("poster", "ksc", true);
            }

            if (this.type == ShoutoutType.Unknown)
            {
                node.SetValue("type", "unknown", true);
            }
            if (this.type == ShoutoutType.RepLevel)
            {
                node.SetValue("type", "repLevel", true);
            }
            if (this.type == ShoutoutType.LameJoke)
            {
                node.SetValue("type", "lameJoke", true);
            }
            if (this.type == ShoutoutType.Crew)
            {
                node.SetValue("type", "crew", true);
            }
            if (this.type == ShoutoutType.KSCNews)
            {
                node.SetValue("type", "kscNews", true);
            }

            node.SetValue("shoutout", this.shoutout, true);
            
            node.SetValue("postedId", this.postedId, true);
            node.SetValue("postedBy", this.postedBy, true);
            node.SetValue("postedTime", this.postedTime, true);
            node.SetValue("postedShoutout", this.postedShoutout, true);

            return node;
        }
    }
}
