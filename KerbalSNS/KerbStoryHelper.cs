using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalSNS
{
    class KerbStoryHelper
    {
        #region properties
        private System.Random mizer = new System.Random();

        private List<KerbBaseStory> baseStoryList;
        private static KerbStoryHelper instance;
        #endregion

        public static KerbStoryHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new KerbStoryHelper();
                }
                return instance;
            }
        }

        private KerbStoryHelper()
        {
        }

        public void LoadBaseStoryList()
        {
            baseStoryList = new List<KerbBaseStory>();

            ConfigNode rootStoryNode = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/KerbalSNS/baseStoriesList.cfg");
            ConfigNode storyListNode = rootStoryNode.GetNode(KerbBaseStory.NODE_NAME_PLURAL);

            ConfigNode[] storyArray = storyListNode.GetNodes();
            foreach (ConfigNode storyNode in storyArray)
            {
                KerbBaseStory story = new KerbBaseStory();
                story.LoadFromConfigNode(storyNode);
                baseStoryList.Add(story);
            }
        }

        public List<KerbStory> GetPostedStories()
        {
            List<KerbStory> postedStoriesList = KerbalSNSScenario.Instance.GetStoryList; // TODO fix bad name
            postedStoriesList = postedStoriesList.OrderByDescending(s => s.postedTime).ToList();
            return postedStoriesList;
        }

        public void PostStory()
        {
            List<KerbBaseStory> filteredBaseStoryList =
                baseStoryList.Where(x => KerbalSNSUtils.HasAchievedAllProgressReqt(x.progressReqtArray)).ToList();
            KerbBaseStory baseStory = filteredBaseStoryList[mizer.Next(filteredBaseStoryList.Count)];

            List<Vessel> vesselList =
                FlightGlobals.Vessels.Where(x => isVesselViable(baseStory, x)).ToList();

            if (vesselList.Count == 0)
            {
                Debug.Log("No kerbals viable for this story");
                return;
            }

            Vessel vessel = vesselList[mizer.Next(vesselList.Count)];

            List<ProtoCrewMember> kerbalList = getViableKerbals(baseStory, vessel);

            KerbStory story = createStory(baseStory, vessel, kerbalList);
            KerbalSNSScenario.Instance.RegisterStory(story);

            Debug.Log("Random story has happened.");

            ScreenMessages.PostScreenMessage("A random story happened at " + vessel.GetDisplayName() + "!");

            MessageSystem.Message message = new MessageSystem.Message(
                "A random story happened at " + vessel.GetDisplayName() + "!",
                story.postedText,
                MessageSystemButton.MessageButtonColor.BLUE,
                MessageSystemButton.ButtonIcons.MESSAGE
            );

            MessageSystem.Instance.AddMessage(message);
        }

        private bool isVesselViable(KerbBaseStory baseStory, Vessel vessel)
        {
            if (FlightGlobals.ActiveVessel != null
                && FlightGlobals.ActiveVessel.Equals(vessel))
            {
                return false;
            }

            bool isCrewEnough = vessel.GetCrewCount() >= baseStory.kerbalCount;
            bool isVesselValid =
                (vessel.vesselType == VesselType.Base
                    && (vessel.situation == Vessel.Situations.LANDED
                        || vessel.situation == Vessel.Situations.SPLASHED))
                || (vessel.vesselType == VesselType.Station
                    && (vessel.situation == Vessel.Situations.ORBITING
                        /*|| vessel.situation == Vessel.Situations.ESCAPING
                        || vessel.situation == Vessel.Situations.FLYING*/));
            bool isBodyValid = baseStory.bodyName == null
                || vessel.mainBody.name.Equals(baseStory.bodyName);

            return isCrewEnough && isVesselValid && isBodyValid;
        }

        private List<ProtoCrewMember> getViableKerbals(KerbBaseStory story, Vessel vessel)
        {
            List<ProtoCrewMember> vesselCrewList = vessel.GetVesselCrew();

            List<ProtoCrewMember> viableKerbalList = new List<ProtoCrewMember>();
            for (int i = 0; i < story.kerbalCount; i++)
            {
                ProtoCrewMember kerbal = vesselCrewList[mizer.Next(vesselCrewList.Count)];
                while (viableKerbalList.Contains(kerbal)) // TODO find a better way
                {
                    kerbal = vesselCrewList[mizer.Next(vesselCrewList.Count)];
                }

                viableKerbalList.Add(kerbal);
            }

            return viableKerbalList;
        }

        private KerbStory createStory(KerbBaseStory baseStory, Vessel vessel, List<ProtoCrewMember> kerbalList)
        {
            KerbStory story = new KerbStory(baseStory);

            story.postedId = "TODO";

            story.postedOnVessel = vessel.GetDisplayName();
            story.postedTime = Planetarium.GetUniversalTime();

            story.postedText = baseStory.text;

            String vesselType = (vessel.vesselType == VesselType.Base) ?
                "base" : "station";
            story.postedText = story.postedText.Replace("%v", vesselType);

            int kerbalIndex = 1;
            foreach (ProtoCrewMember kerbal in kerbalList)
            {
                story.postedText =
                    story.postedText.Replace("%k" + kerbalIndex, CrewGenerator.RemoveLastName(kerbal.name));
                kerbalIndex++;
            }

            return story;
        }
    }
}