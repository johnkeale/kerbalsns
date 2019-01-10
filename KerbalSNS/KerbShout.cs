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
            this.posterType = baseShout.posterType;
            this.specificPoster = baseShout.specificPoster;
            this.type = baseShout.type;
            this.text = baseShout.text;
            this.progressReqtArray = baseShout.progressReqtArray;
            this.isRepeatable = baseShout.isRepeatable;
            this.vesselType = baseShout.vesselType;
            this.vesselSituation = baseShout.vesselSituation;
        }

        public override void LoadFromConfigNode(ConfigNode node)
        {
            base.LoadFromConfigNode(node);

            this.postedId = node.GetValue("postedId");

            this.postedBy = new Acct();
            if (this.posterType != KerbBaseShout.PosterType.Specific)
            {
                this.postedBy.LoadFromConfigNode(node.GetNode(Acct.NODE_NAME));
            }
            else
            {
                this.postedBy.LoadFromConfigNode(node.GetNodes(Acct.NODE_NAME)[1]);
            }

            this.postedTime = Double.Parse(node.GetValue("postedTime"));
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

            if (!(obj is KerbShout))
            {
                return false;
            }

            KerbShout other = (KerbShout)obj;
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
