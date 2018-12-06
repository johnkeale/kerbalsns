using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSNS
{
    public class KerbShout
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

        public enum ShoutPoster
        {
            Unknown = -1,
            Any,
            Citizen,
            VesselCrew,
            KSCEmployee,
            KSC,
            Specific,
        }

        public enum ShoutType
        {
            Unknown = -1,
            RepLevel,
            LameJoke,
            Crew,
            KSCNews,
			NewsReponse,
			Random,
			Nonsense,
        }

        public String name { get; set; }
        public RepLevel repLevel { get; set; }
        public ShoutPoster poster { get; set; }
		public String specificPoster { get; set; }
        public ShoutType type { get; set; }
        public String shout { get; set; }
		public String reqdMilestone; // TODO

        public String postedId { get; set; }
        public String postedBy { get; set; }
        public double postedTime { get; set; }
        public String postedShout { get; set; }

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
                this.poster = ShoutPoster.Any;
            }
            if ("citizen".Equals(poster))
            {
                this.poster = ShoutPoster.Citizen;
            }
            if ("vesselCrew".Equals(poster))
            {
                this.poster = ShoutPoster.VesselCrew;
            }
            if ("kscEmployee".Equals(poster))
            {
                this.poster = ShoutPoster.KSCEmployee;
            }
            if ("ksc".Equals(poster))
            {
                this.poster = ShoutPoster.KSC;
            }
            if ("specific".Equals(poster))
            {
                this.poster = ShoutPoster.Specific;
            }

            if (node.HasValue("specificPoster"))
            {
                this.specificPoster = node.GetValue("specificPoster");
            }

            this.type = ShoutType.Unknown;
            String type = node.GetValue("type");
            if ("repLevel".Equals(type))
            {
                this.type = ShoutType.RepLevel;
            }
            if ("lameJoke".Equals(type))
            {
                this.type = ShoutType.LameJoke;
            }
            if ("crew".Equals(type))
            {
                this.type = ShoutType.Crew;
            }
            if ("kscNews".Equals(type))
            {
                this.type = ShoutType.KSCNews;
            }
            if ("newsReponse".Equals(type))
            {
                this.type = ShoutType.NewsReponse;
            }
            if ("random".Equals(type))
            {
                this.type = ShoutType.Random;
            }
            if ("nonsense".Equals(type))
            {
                this.type = ShoutType.Nonsense;
            }
            
            this.shout = node.GetValue("shout");

            this.postedId = node.GetValue("postedId");
            this.postedBy = node.GetValue("postedBy");
            if (node.HasValue("postedTime"))
            {
                this.postedTime = Double.Parse(node.GetValue("postedTime"));
            }
            this.postedShout = node.GetValue("postedShout");
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

            if (this.poster == ShoutPoster.Unknown)
            {
                node.SetValue("poster", "unknown", true);
            }
            if (this.poster == ShoutPoster.Any)
            {
                node.SetValue("poster", "any", true);
            }
            if (this.poster == ShoutPoster.Citizen)
            {
                node.SetValue("poster", "citizen", true);
            }
            if (this.poster == ShoutPoster.VesselCrew)
            {
                node.SetValue("poster", "vesselCrew", true);
            }
            if (this.poster == ShoutPoster.KSCEmployee)
            {
                node.SetValue("poster", "kscEmployee", true);
            }
            if (this.poster == ShoutPoster.KSC)
            {
                node.SetValue("poster", "ksc", true);
            }
            if (this.poster == ShoutPoster.Specific)
            {
                node.SetValue("poster", "specific", true);
            }

            node.SetValue("specificPoster", this.specificPoster, true);

            if (this.type == ShoutType.Unknown)
            {
                node.SetValue("type", "unknown", true);
            }
            if (this.type == ShoutType.RepLevel)
            {
                node.SetValue("type", "repLevel", true);
            }
            if (this.type == ShoutType.LameJoke)
            {
                node.SetValue("type", "lameJoke", true);
            }
            if (this.type == ShoutType.Crew)
            {
                node.SetValue("type", "crew", true);
            }
            if (this.type == ShoutType.KSCNews)
            {
                node.SetValue("type", "kscNews", true);
            }
            if (this.type == ShoutType.NewsReponse)
            {
                node.SetValue("type", "newsReponse", true);
            }
            if (this.type == ShoutType.Random)
            {
                node.SetValue("type", "random", true);
            }
            if (this.type == ShoutType.Nonsense)
            {
                node.SetValue("type", "nonsense", true);
                ;
            }

            node.SetValue("shout", this.shout, true);
            
            node.SetValue("postedId", this.postedId, true);
            node.SetValue("postedBy", this.postedBy, true);
            node.SetValue("postedTime", this.postedTime, true);
            node.SetValue("postedShout", this.postedShout, true);

            return node;
        }
    }
}
