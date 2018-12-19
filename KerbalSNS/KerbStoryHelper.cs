using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

            UrlDir.UrlConfig[] allConfigArray = GameDatabase.Instance.root.AllConfigs.ToArray();
            foreach (UrlDir.UrlConfig urlConfig in allConfigArray)
            {
                if (urlConfig.name.Equals(KerbBaseStory.NODE_NAME_PLURAL))
                {
                    ConfigNode[] storyArray = urlConfig.config.GetNodes();
                    foreach (ConfigNode storyNode in storyArray)
                    {
                        KerbBaseStory story = new KerbBaseStory();
                        story.LoadFromConfigNode(storyNode);
                        baseStoryList.Add(story);
                    }
                }
            }

        }

        public List<KerbStory> GetPostedStories()
        {
            List<KerbStory> storyList = KerbalSNSScenario.Instance.GetStoryList; // TODO fix bad name
            storyList = storyList.OrderByDescending(s => s.postedTime).ToList();
            return storyList;
        }

        public KerbStory GenerateRandomStory()
        {
            List<KerbBaseStory> filteredBaseStoryList =
                baseStoryList.Where(x => KerbalSNSUtils.HasAchievedAllProgressReqt(x.progressReqtArray)).ToList();
            if (filteredBaseStoryList.Count <= 0)
            {
                return null;
            }

            KerbBaseStory baseStory = filteredBaseStoryList[mizer.Next(filteredBaseStoryList.Count)];
            filteredBaseStoryList.Remove(baseStory);

            List<Vessel> vesselList =
                FlightGlobals.Vessels.Where(x => isVesselViable(baseStory, x)).ToList();

            while (vesselList.Count == 0 && filteredBaseStoryList.Count > 0)
            {
                baseStory = filteredBaseStoryList[mizer.Next(filteredBaseStoryList.Count)];
                filteredBaseStoryList.Remove(baseStory);
                vesselList =
                    FlightGlobals.Vessels.Where(x => isVesselViable(baseStory, x)).ToList();
            }

            if (vesselList.Count == 0)
            {
                Debug.Log("No kerbals viable for any story");
                return null;
            }
            else
            {
                Debug.Log("There are Kerbals viable for story: " + baseStory.name);

                Vessel vessel = vesselList[mizer.Next(vesselList.Count)];

                List<ProtoCrewMember> kerbalList = getViableKerbals(baseStory, vessel);

                KerbStory story = createStory(baseStory, vessel, kerbalList);
                KerbalSNSScenario.Instance.RegisterStory(story);

                return story;
            }
        }

        private bool isVesselViable(KerbBaseStory baseStory, Vessel vessel)
        {
            if (FlightGlobals.ActiveVessel != null
                && FlightGlobals.ActiveVessel.Equals(vessel))
            {
                return false;
            }

            int kerbalCount = Regex.Matches(baseStory.text, "%k").Count;

            return KerbalSNSUtils.HasEnoughCrew(vessel, kerbalCount) 
                && KerbalSNSUtils.IsVesselTypeCorrect(vessel, baseStory.vesselType) 
                && KerbalSNSUtils.DoesVesselSituationMatch(vessel, baseStory.vesselSituation);
        }

        private List<ProtoCrewMember> getViableKerbals(KerbBaseStory baseStory, Vessel vessel)
        {
            List<ProtoCrewMember> vesselCrewList = vessel.GetVesselCrew();

            int kerbalCount = Regex.Matches(baseStory.text, "%k").Count;

            List<ProtoCrewMember> viableKerbalList = new List<ProtoCrewMember>();
            for (int i = 0; i < kerbalCount; i++)
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

            String vesselType = "";
            switch (vessel.vesselType)
            {
                case VesselType.Probe:
                    vesselType = "probe";
                    break;
                case VesselType.Relay:
                    vesselType = "relay";
                    break;
                case VesselType.Rover:
                    vesselType = "rover";
                    break;
                case VesselType.Lander:
                    vesselType = "lander";
                    break;
                case VesselType.Ship:
                    vesselType = "ship";
                    break;
                case VesselType.Plane:
                    vesselType = "plane";
                    break;
                case VesselType.Station:
                    vesselType = "station";
                    break;
                case VesselType.Base:
                    vesselType = "base";
                    break;
                case VesselType.EVA:
                    vesselType = "eva";
                    break;
                case VesselType.Flag:
                    vesselType = "flag";
                    break;
            }
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