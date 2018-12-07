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
            Unknown,
        }

        public String name { get; set; }
        public int kerbalCount { get; set; }
        public StoryType type { get; set; }
        public String text { get; set; }
        public String reqdMilestone; // TODO
        public bool isRepeatable { get; set; }

        public virtual void LoadFromConfigNode(ConfigNode node)
        {
            this.name = node.GetValue("name");

            this.kerbalCount = int.Parse(node.GetValue("kerbalCount"));

            this.type = StoryType.Unknown;
            String type = node.GetValue("type");
            if ("type".Equals(type))
            {
                this.type = StoryType.Unknown;
            }

            this.text = node.GetValue("text");
        }

        public virtual ConfigNode SaveToConfigNode()
        {
            ConfigNode node = new ConfigNode(NODE_NAME);

            node.SetValue("name", this.name, true);
            node.SetValue("kerbalCount", this.kerbalCount, true);

            // TODO type

            node.SetValue("text", this.text, true);

            return node;
        }
    }
}
