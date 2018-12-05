using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSNS
{
    public class KerbStory
    {
        public enum StoryType
        {
            Unknown,
        }

        public String name { get; set; }
        public int kerbalCount { get; set; }
        public StoryType type { get; set; }
        public String storyText { get; set; }
        public bool isRepeatable { get; set; }

        public String postedId { get; set; }
        public String postedOnVessel { get; set; }
        public double postedTime { get; set; }
        public String postedStoryText { get; set; }

        public void LoadFromConfigNode(ConfigNode node)
        {
            this.name = node.GetValue("name");

            this.kerbalCount = int.Parse(node.GetValue("kerbalCount"));

            this.type = StoryType.Unknown;
            String type = node.GetValue("type");
            if ("type".Equals(type))
            {
                this.type = StoryType.Unknown;
            }

            this.storyText = node.GetValue("storyText");

            this.postedId = node.GetValue("postedId");
            this.postedOnVessel = node.GetValue("postedOnVessel");
            if (node.HasValue("postedTime"))
            {
                this.postedTime = Double.Parse(node.GetValue("postedTime"));
            }
            this.postedStoryText = node.GetValue("postedStoryText");
        }

        public ConfigNode SaveToConfigNode()
        {
            ConfigNode node = new ConfigNode("KERBSTORY");

            node.SetValue("name", this.name, true);
            node.SetValue("kerbalCount", this.kerbalCount, true);

            // TODO type

            node.SetValue("storyText", this.storyText, true);

            node.SetValue("postedId", this.postedId, true);
            node.SetValue("postedOnVessel", this.postedOnVessel, true);
            node.SetValue("postedTime", this.postedTime, true);
            node.SetValue("postedStoryText", this.postedStoryText, true);

            return node;
        }
    }
}
