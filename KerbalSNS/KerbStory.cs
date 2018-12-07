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
        public String postedStoryText { get; set; }

        public KerbStory() { }

        public KerbStory(KerbBaseStory baseStory)
        {
            this.name = baseStory.name;
            this.kerbalCount = baseStory.kerbalCount;
            this.type = baseStory.type;
            this.text = baseStory.text;
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
            this.postedStoryText = node.GetValue("postedStoryText");
        }

        public override ConfigNode SaveToConfigNode()
        {
            ConfigNode node = base.SaveToConfigNode();

            node.SetValue("postedId", this.postedId, true);
            node.SetValue("postedOnVessel", this.postedOnVessel, true);
            node.SetValue("postedTime", this.postedTime, true);
            node.SetValue("postedStoryText", this.postedStoryText, true);

            return node;
        }
    }
}
