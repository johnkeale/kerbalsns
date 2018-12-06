using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalSNS
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    public class KerbalSNSScenario : ScenarioModule
    {
        public static KerbalSNSScenario Instance;

        #region properties
        protected List<KerbStory> storyList = new List<KerbStory>();
        protected List<KerbShout> shoutList = new List<KerbShout>();
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

            ConfigNode[] storyArray = node.GetNodes("KERBSTORY");
            foreach (ConfigNode storyNode in storyArray)
            {
                KerbStory story = new KerbStory();
                story.LoadFromConfigNode(storyNode);
                storyList.Add(story);
            }

            ConfigNode[] shoutArray = node.GetNodes("KERBSHOUT");
            foreach (ConfigNode shoutNode in shoutArray)
            {
                KerbShout shout = new KerbShout();
                shout.LoadFromConfigNode(shoutNode);
                shoutList.Add(shout);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            
            foreach(KerbStory story in this.storyList)
            {
                node.AddNode("KERBSTORY", story.SaveToConfigNode());
            }

            foreach (KerbShout shout in this.shoutList)
            {
                node.AddNode("KERBSHOUT", shout.SaveToConfigNode());
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
        #endregion

    }
}
