using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSNS
{
    public class KerbShout : KerbBaseShout
    {
        public String postedId { get; set; }
        public String postedBy { get; set; }
        public double postedTime { get; set; }
        public String postedShout { get; set; }

        public KerbShout() { }

        public KerbShout(KerbBaseShout baseShout)
        {
            this.name = baseShout.name;
            this.repLevel = baseShout.repLevel;
            this.poster = baseShout.poster;
            this.type = baseShout.type;
            this.text = baseShout.text;
        }

        public override void LoadFromConfigNode(ConfigNode node)
        {
            base.LoadFromConfigNode(node);

            this.postedId = node.GetValue("postedId");
            this.postedBy = node.GetValue("postedBy");
            if (node.HasValue("postedTime"))
            {
                this.postedTime = Double.Parse(node.GetValue("postedTime"));
            }
            this.postedShout = node.GetValue("postedShout");
        }

        public override ConfigNode SaveToConfigNode()
        {
            ConfigNode node = base.SaveToConfigNode();
            
            node.SetValue("postedId", this.postedId, true);
            node.SetValue("postedBy", this.postedBy, true);
            node.SetValue("postedTime", this.postedTime, true);
            node.SetValue("postedShout", this.postedShout, true);

            return node;
        }
    }
}
