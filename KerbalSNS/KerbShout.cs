using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSNS
{
    public class KerbShout : KerbBaseShout
    {
        public String postedId { get; set; }
        public Acct postedBy { get; set; }
        public double postedTime { get; set; }
        public String postedText { get; set; }

        public KerbShout() { }

        public KerbShout(KerbBaseShout baseShout)
        {
            this.name = baseShout.name;
            this.repLevel = baseShout.repLevel;
            this.poster = baseShout.poster;
            this.specificPoster = baseShout.specificPoster;
            this.type = baseShout.type;
            this.text = baseShout.text;
            this.progressReqtArray = baseShout.progressReqtArray;
            this.isRepeatable = baseShout.isRepeatable;
        }

        public override void LoadFromConfigNode(ConfigNode node)
        {
            base.LoadFromConfigNode(node);

            this.postedId = node.GetValue("postedId");

            this.postedBy = new Acct();
            if (this.poster != ShoutPoster.Specific)
            {
                this.postedBy.LoadFromConfigNode(node.GetNode(Acct.NODE_NAME));
            }
            else
            {
                this.postedBy.LoadFromConfigNode(node.GetNodes(Acct.NODE_NAME)[1]);
            }

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

            node.AddNode(this.postedBy.SaveToConfigNode());

            node.SetValue("postedTime", this.postedTime, true);
            node.SetValue("postedText", this.postedText, true);

            return node;
        }
    }
}
