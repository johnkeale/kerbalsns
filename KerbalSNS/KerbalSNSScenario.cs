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
        protected List<KerbShoutout> shoutoutList = new List<KerbShoutout>();
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

            ConfigNode[] shoutoutArray = node.GetNodes("KERBSHOUTOUT");
            foreach (ConfigNode shoutoutNode in shoutoutArray)
            {
                KerbShoutout shoutout = new KerbShoutout();
                shoutout.LoadFromConfigNode(shoutoutNode);
                shoutoutList.Add(shoutout);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            
            foreach(KerbStory story in this.storyList)
            {
                node.AddNode("KERBSTORY", story.SaveToConfigNode());
            }

            foreach (KerbShoutout shoutout in this.shoutoutList)
            {
                node.AddNode("KERBSHOUTOUT", shoutout.SaveToConfigNode());
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

        public List<KerbShoutout> GetShoutoutList
        {
            get
            {
                return this.shoutoutList.ToList();
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

        public void RegisterShoutout(KerbShoutout shoutout)
        {
            this.shoutoutList.Add(shoutout);
        }

        public void DeleteShoutout(KerbShoutout shoutout)
        {
            this.shoutoutList.Remove(shoutout);
        }
        #endregion

    }
}
