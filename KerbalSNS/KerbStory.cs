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
        }

        public override void LoadFromConfigNode(ConfigNode node)
        {
            base.LoadFromConfigNode(node);

            this.postedId = node.GetValue("postedId");
            this.postedOnVessel = node.GetValue("postedOnVessel");
            if (node.HasValue("postedTime"))
            {
                this.postedTime = Double.Parse(node.GetValue("postedTime"));
            }
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
    }
}
