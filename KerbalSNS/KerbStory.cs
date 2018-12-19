using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSNS
{
    public class KerbStory : KerbBaseStory
    {
        public String postedId { get; set; }
        public String postedOnVessel { get; set; }
        public double postedTime { get; set; }
        public String postedText { get; set; }

        public KerbStory() { }

        public KerbStory(KerbBaseStory baseStory)
        {
            this.name = baseStory.name;
            this.kerbalCount = baseStory.kerbalCount;
            this.type = baseStory.type;
            this.text = baseStory.text;
            this.progressReqtArray = baseStory.progressReqtArray;
            this.isRepeatable = baseStory.isRepeatable;
            this.vesselType = baseStory.vesselType;
            this.vesselSituation = baseStory.vesselSituation;
        }

        public override void LoadFromConfigNode(ConfigNode node)
        {
            base.LoadFromConfigNode(node);

            this.postedId = node.GetValue("postedId");
            this.postedOnVessel = node.GetValue("postedOnVessel");
            this.postedTime = Double.Parse(node.GetValue("postedTime"));
            this.postedText = node.GetValue("postedText");
        }

        public override ConfigNode SaveToConfigNode()
        {
            ConfigNode node = base.SaveToConfigNode();

            node.SetValue("postedId", this.postedId, true);
            node.SetValue("postedOnVessel", this.postedOnVessel, true);
            node.SetValue("postedTime", this.postedTime, true);
            node.SetValue("postedText", this.postedText, true);

            return node;
        }

        public override int GetHashCode()
        {
            return postedId.GetHashCode();
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is KerbStory))
            {
                return false;
            }

            KerbStory other = (KerbStory)obj;
            if (this.postedId == null && other.postedId != null)
            {
                return false;
            }
            else
            {
                return this.postedId.Equals(other.postedId);
            }
        }
    }
}
