using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalSNS
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class KerbalSNSScenario : ScenarioModule
    {
        public static KerbalSNSScenario Instance;

        #region properties
        protected List<KerbStory> storyList = new List<KerbStory>();
        protected List<KerbShout> shoutList = new List<KerbShout>();
        protected List<KerbShout.Acct> shoutAcctList = new List<KerbShout.Acct>();
        #endregion

        #region inherited methods
        public override void OnAwake()
        {
            base.OnAwake();
            Instance = this;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            ConfigNode[] storyArray = node.GetNodes(KerbStory.NODE_NAME);
            foreach (ConfigNode storyNode in storyArray)
            {
                KerbStory story = new KerbStory();
                story.LoadFromConfigNode(storyNode);
                storyList.Add(story);
            }

            ConfigNode[] shoutArray = node.GetNodes(KerbShout.NODE_NAME);
            foreach (ConfigNode shoutNode in shoutArray)
            {
                KerbShout shout = new KerbShout();
                shout.LoadFromConfigNode(shoutNode);
                shoutList.Add(shout);
            }

            ConfigNode[] shoutAcctArray = node.GetNodes(KerbShout.Acct.NODE_NAME);
            foreach (ConfigNode shoutAcctNode in shoutAcctArray)
            {
                KerbShout.Acct shoutAcct = new KerbShout.Acct();
                shoutAcct.LoadFromConfigNode(shoutAcctNode);
                shoutAcctList.Add(shoutAcct);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            
            foreach(KerbStory story in this.storyList)
            {
                node.AddNode(KerbStory.NODE_NAME, story.SaveToConfigNode());
            }

            foreach (KerbShout shout in this.shoutList)
            {
                node.AddNode(KerbShout.NODE_NAME, shout.SaveToConfigNode());
            }
            
            foreach (KerbShout.Acct shoutAcct in this.shoutAcctList)
            {
                node.AddNode(KerbShout.Acct.NODE_NAME, shoutAcct.SaveToConfigNode());
            }

        }
        #endregion

        #region property methods
        public List<KerbStory> GetStoryList
        {
            get
            {
                return this.storyList.ToList();
            }
        }

        public List<KerbShout> GetShoutList
        {
            get
            {
                return this.shoutList.ToList();
            }
        }

        public void RegisterStory(KerbStory story)
        {
            this.storyList.Add(story);
        }

        public void DeleteStory(KerbStory story)
        {
            this.storyList.Remove(story);
        }

        public void RegisterShout(KerbShout shout)
        {
            this.shoutList.Add(shout);
        }

        public void DeleteShout(KerbShout shout)
        {
            this.shoutList.Remove(shout);
        }

        public KerbShout.Acct FindShoutAcct(String fullname)
        {
            return shoutAcctList.FirstOrDefault(a => a.fullname.Equals(fullname));
        }

        public void SaveShoutAcct(KerbShout.Acct shoutAcct)
        {
            if (FindShoutAcct(shoutAcct.fullname) == null)
            {
                this.shoutAcctList.Add(shoutAcct);
            }
        }
        #endregion

    }
}
