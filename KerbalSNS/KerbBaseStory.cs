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
        public int kerbalCount { get; set; }
        public StoryType type { get; set; }
        public String text { get; set; }
        public String[] progressReqtArray { get; set; }
        public bool isRepeatable { get; set; }
        public String bodyName { get; set; }

        public virtual void LoadFromConfigNode(ConfigNode node)
        {
            this.name = "story" + Guid.NewGuid();
            if (node.HasValue("name"))
            {
                this.name = node.GetValue("name");
            }

            // required
            this.kerbalCount = int.Parse(node.GetValue("kerbalCount"));

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

            this.bodyName = null;
            if (node.HasValue("bodyName"))
            {
                this.bodyName = node.GetValue("bodyName");
            }
        }

        public virtual ConfigNode SaveToConfigNode()
        {
            ConfigNode node = new ConfigNode(NODE_NAME);

            node.SetValue("name", this.name, true);
            node.SetValue("kerbalCount", this.kerbalCount, true);

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

            if (this.progressReqtArray != null)
            {
                node.SetValue("bodyName", this.bodyName, true);
            }
            else
            {
                node.SetValue("bodyName", "", true);
            }

            return node;
        }
    }
}
