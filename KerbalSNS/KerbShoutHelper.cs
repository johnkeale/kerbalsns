using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Upgradeables;
using static GameEvents;

namespace KerbalSNS
{
    partial class KerbShoutHelper
    {
        #region properties
        private System.Random mizer = new System.Random();

        private List<KerbBaseShout> baseShoutList;
        private static KerbShoutHelper instance;
        #endregion

        public static KerbShoutHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new KerbShoutHelper();
                }
                return instance;
            }
        }

        private KerbShoutHelper()
        {
        }

        public void LoadBaseShoutList()
        {
            baseShoutList = new List<KerbBaseShout>();

            UrlDir.UrlConfig[] allConfigArray = GameDatabase.Instance.root.AllConfigs.ToArray();
            foreach (UrlDir.UrlConfig urlConfig in allConfigArray)
            {
                if (urlConfig.name.Equals(KerbBaseShout.NODE_NAME_PLURAL))
                {
                    ConfigNode[] shoutArray = urlConfig.config.GetNodes();
                    foreach (ConfigNode shoutNode in shoutArray)
                    {
                        KerbBaseShout shout = new KerbBaseShout();
                        shout.LoadFromConfigNode(shoutNode);
                        baseShoutList.Add(shout);
                    }
                }
            }

        }

        public List<KerbShout> GetPostedShouts()
        {
            List<KerbShout> shoutList = KerbalSNSScenario.Instance.GetShoutList; // TODO fix bad name
            shoutList = updateShoutsIfNeeded(shoutList);
            shoutList = shoutList.OrderByDescending(s => s.postedTime).ToList();
            return shoutList;
        }

        public KerbShout GenerateKSCShout(String text)
        {
            String posterType = KerbBaseShout.PosterType_KSC;
            KerbShout.Acct postedBy = KerbShout.Acct.KSC_OFFICIAL;

            if (FlightGlobals.ActiveVessel != null)
            {
                String fullname = KerbalSNSUtils.RandomVesselCrewKerbalName(FlightGlobals.ActiveVessel);
                if (fullname != null)
                {
                    posterType = KerbBaseShout.PosterType_VesselCrew;
                    postedBy = ensureKSCShoutAcctExists(fullname);
                }
            }

            return generateShout(KerbBaseShout.RepLevel_Any, KerbBaseShout.ShoutType_Random, text, posterType, postedBy);
        }

        private KerbShout generateShout(String repLevel, String shoutType, String text, String posterType, KerbShout.Acct postedBy)
        {
            KerbBaseShout baseShout = new KerbBaseShout();

            baseShout.name = "TODO";
            baseShout.repLevel = repLevel;
            baseShout.type = shoutType;
            baseShout.text = text;
            baseShout.posterType = posterType;

            KerbShout shout = createShout(baseShout, postedBy);
            formatShoutTags(shout);
            formatShoutMentions(shout);

            return shout;
        }

        private List<KerbShout> updateShoutsIfNeeded(List<KerbShout> shoutList)
        {
            double now = Planetarium.GetUniversalTime();
            List<KerbShout> updatedShoutList =
                purgeOldShouts(shoutList, now, KSPUtil.dateTimeFormatter.Hour);
            
            if (updatedShoutList.Count == 0 || updatedShoutList.Count < KerbalSNSSettings.NumOfShouts)
            {
                int neededShoutCount = KerbalSNSSettings.NumOfShouts - updatedShoutList.Count; // FIXME this creates repLevel shouts based only on neededShouts, so the percentage will be off
                int repLevelShoutCount = (int)Math.Ceiling(neededShoutCount * (KerbalSNSSettings.RepLevelShoutPercentage / 100.0f));

                int outlierRepLevelShoutCount = 0;
                if (repLevelShoutCount > 5)
                {
                    outlierRepLevelShoutCount = mizer.Next(2) + 1;
                    repLevelShoutCount -= outlierRepLevelShoutCount;
                }

                List<KerbBaseShout> filteredBaseShoutList = baseShoutList.ToList();
                filteredBaseShoutList =
                    filteredBaseShoutList.Where(
                        x => KerbalSNSUtils.HasAchievedAllProgressReqt(x.progressReqtArray)
                    ).ToList();
                filteredBaseShoutList =
                    filteredBaseShoutList.Where(
                        x => hasVesselViable(x)
                    ).ToList();
                filteredBaseShoutList =
                    filteredBaseShoutList.Where(
                        x => x.gameEvent == null
                    ).ToList();

                List<KerbShout> repLevelShoutList =
                    generateRandomShouts(
                        filteredBaseShoutList,
                        x => (
                            x.type.Equals(KerbBaseShout.ShoutType_RepLevel)
                            && x.repLevel.Equals(getCurrentRepLevel())
                        ),
                        repLevelShoutCount);
                foreach (KerbShout shout in repLevelShoutList)
                {
                    shout.postedTime = now - mizer.Next(KSPUtil.dateTimeFormatter.Hour) + 1; // set time to random time in most recent hour XXX
                    updatedShoutList.Add(shout);  // XXX this list seems unnecessary
                    KerbalSNSScenario.Instance.RegisterShout(shout);
                }

                List<KerbShout> outlierRepLevelShoutList =
                    generateRandomShouts(
                        filteredBaseShoutList,
                        x => (
                            x.type.Equals(KerbBaseShout.ShoutType_RepLevel)
                            && !x.repLevel.Equals(getCurrentRepLevel())
                        ),
                        outlierRepLevelShoutCount);
                foreach (KerbShout shout in outlierRepLevelShoutList)
                {
                    shout.postedTime = now - mizer.Next(KSPUtil.dateTimeFormatter.Hour) + 1;
                    updatedShoutList.Add(shout);
                    KerbalSNSScenario.Instance.RegisterShout(shout);
                }

                List<KerbShout> otherShoutList =
                    generateRandomShouts(
                        filteredBaseShoutList,
                        x => (
                            !x.type.Equals(KerbBaseShout.ShoutType_RepLevel)
                        ),
                        neededShoutCount - repLevelShoutCount);
                foreach (KerbShout shout in otherShoutList)
                {
                    shout.postedTime = now - mizer.Next(KSPUtil.dateTimeFormatter.Hour) + 1;
                    updatedShoutList.Add(shout);
                    KerbalSNSScenario.Instance.RegisterShout(shout);
                }
            }
            
            return updatedShoutList;
        }

        private List<KerbShout> generateRandomShouts(List<KerbBaseShout> referenceBaseShoutList, Func<KerbBaseShout, bool> predicate, int count)
        {
            List<KerbBaseShout> filteredBaseShoutList = referenceBaseShoutList.Where(predicate).ToList();

            int neededShouts = count == -1 ? filteredBaseShoutList.Count : count;

            List <KerbShout> shoutList = new List<KerbShout>();

            if (filteredBaseShoutList.Count > 0)
            {
                // FIXME will get stuck on an infinite loop if no shouts generated
                for (int i = 0; i < neededShouts; i++)
                {
                    KerbBaseShout baseShout =
                        filteredBaseShoutList[mizer.Next(filteredBaseShoutList.Count)];

                    KerbShout.Acct postedBy = generateShoutAcctFromBaseShout(baseShout);

                    KerbShout shout = createShout(baseShout, postedBy);

                    formatShoutTags(shout);

                    if (baseShout.text.Contains("%v") || baseShout.text.Contains("%k"))
                    {
                        Vessel vessel = getRandomViableVessel(baseShout);
                        if (vessel == null)
                        {
                            continue;
                        }

                        shout.postedText = shout.postedText.Replace("%v", vessel.GetDisplayName());

                        int kerbalCount = Regex.Matches(baseShout.text, "%k").Count;

                        int kerbalIndex = 1;
                        List<ProtoCrewMember> crewList = vessel.GetVesselCrew().ToList();
                        for (int k = 0; k < kerbalCount; k++)
                        {
                            ProtoCrewMember randomKerbal = crewList[mizer.Next(crewList.Count)];
                            crewList.Remove(randomKerbal);

                            KerbShout.Acct shoutAcct = ensureKSCShoutAcctExists(randomKerbal.name);

                            shout.postedText = shout.postedText.Replace("%k" + kerbalIndex, shoutAcct.username);
                            kerbalIndex++;
                        }
                    }

                    formatShoutMentions(shout);

                    shoutList.Add(shout);
                }

            }

            return shoutList;
        }

        private List<KerbShout> purgeOldShouts(List<KerbShout> shoutList, double baseTime, double deltaTime)
        {
            List<KerbShout> freshShoutsList = new List<KerbShout>();

            foreach (KerbShout shout in shoutList)
            {
                // shout still new
                if (baseTime - shout.postedTime <= deltaTime)
                {
                    freshShoutsList.Add(shout);
                }
                else
                {
                    KerbalSNSScenario.Instance.DeleteShout(shout);
                }
            }

            return freshShoutsList;
        }

        public KerbShout.Acct generateShoutAcctFromBaseShout(KerbBaseShout baseShout)
        {
            if (baseShout.posterType.Equals(KerbBaseShout.PosterType_Specific))
            {
                KerbalSNSScenario.Instance.SaveShoutAcct(baseShout.specificPoster);
                return baseShout.specificPoster;
            }
            else
            {
                if (baseShout.posterType.Equals(KerbBaseShout.PosterType_Any)
                    || baseShout.posterType.Equals(KerbBaseShout.PosterType_LayKerbal))
                {
                    KerbShout.Acct shoutAcct = new KerbShout.Acct();
                    shoutAcct.name = "TODO";

                    shoutAcct.fullname = KerbalSNSUtils.RandomLayKerbalName();
                    shoutAcct.username = "@" + makeLikeUsername(shoutAcct.fullname);

                    return shoutAcct;
                }
                else if (baseShout.posterType.Equals(KerbBaseShout.PosterType_VesselCrew)
                    || baseShout.posterType.Equals(KerbBaseShout.PosterType_KSCEmployee))
                {
                    String fullname = baseShout.posterType.Equals(KerbBaseShout.PosterType_VesselCrew) ?
                        KerbalSNSUtils.RandomActiveCrewKerbalName() :
                        KerbalSNSUtils.RandomLayKerbalName();

                    return ensureKSCShoutAcctExists(fullname);
                }
                else if (baseShout.posterType.Equals(KerbBaseShout.PosterType_KSC))
                {
                    return KerbShout.Acct.KSC_OFFICIAL;
                }
            }

            return null;
        }

        private KerbShout createShout(KerbBaseShout baseShout, KerbShout.Acct postedBy)
        {
            KerbShout shout = new KerbShout(baseShout);

            shout.postedId = "TODO";

            shout.postedBy = postedBy;
            shout.postedTime = Planetarium.GetUniversalTime();

            shout.postedText = baseShout.text;

            return shout;
        }

        private void formatShoutTags(KerbShout shout)
        {
            shout.postedText =
                Regex.Replace(shout.postedText, "#([\\w]+)", "<color=#29E667><u>#$1</u></color>", RegexOptions.IgnoreCase);
        }

        private void formatShoutMentions(KerbShout shout)
        {
            shout.postedText =
                Regex.Replace(shout.postedText, "@([\\w]+)", "<color=#6F8E2F><u>@$1</u></color>", RegexOptions.IgnoreCase);
        }

        private bool hasVesselViable(KerbBaseShout baseShout)
        {
            return baseShout.vesselType == KerbalSNSUtils.VesselTypeAny
                || baseShout.vesselSituation == null
                || getRandomViableVessel(baseShout) != null;
        }

        private Vessel getRandomViableVessel(KerbBaseShout baseShout)
        {
            List<Vessel> vesselList =
                FlightGlobals.Vessels.Where(x => isVesselViable(baseShout, x)).ToList();
            if (vesselList.Count == 0)
            {
                return null;
            }
            else
            {
                return vesselList[mizer.Next(vesselList.Count)];
            }
        }

        private bool isVesselViable(KerbBaseShout baseShout, Vessel vessel)
        {
            int kerbalCount = Regex.Matches(baseShout.text, "%k").Count;

            return KerbalSNSUtils.HasEnoughCrew(vessel, kerbalCount)
                && KerbalSNSUtils.IsVesselTypeCorrect(vessel, baseShout.vesselType)
                && KerbalSNSUtils.DoesVesselSituationMatch(vessel, baseShout.vesselSituation);
        }

        private String makeLikeUsername(String name)
        {
            String username = name;

            int r = mizer.Next(4);
            if (r == 0)
            {
                username = Regex.Replace(username, " ", "", RegexOptions.IgnoreCase);
            }
            else if (r == 1)
            {
                username = Regex.Replace(username, " ", "_", RegexOptions.IgnoreCase);
            }
            else
            {
                username = CrewGenerator.RemoveLastName(name);
            }

            r = mizer.Next(13);
            if (r < 3)
            {
                username = Regex.Replace(username, "o", "0", RegexOptions.IgnoreCase);
            }
            r = mizer.Next(13);
            if (r < 3)
            {
                username = Regex.Replace(username, "i", "1", RegexOptions.IgnoreCase);
            }
            r = mizer.Next(13);
            if (r < 3)
            {
                username = Regex.Replace(username, "l", "2", RegexOptions.IgnoreCase);
            }
            r = mizer.Next(13);
            if (r < 3)
            {
                username = Regex.Replace(username, "e", "3", RegexOptions.IgnoreCase);
            }

            r = mizer.Next(13);
            if (r < 1)
            {
                username = username + mizer.Next(1000);
            }

            return username;
        }

        private String getCurrentRepLevel()
        {
            if (-1000f <= Reputation.CurrentRep && Reputation.CurrentRep < -600f)
            {
                return KerbBaseShout.RepLevel_VeryLow;
            }
            else if (-600f <= Reputation.CurrentRep && Reputation.CurrentRep < -200f)
            {
                return KerbBaseShout.RepLevel_Low;
            }
            else if (-200f <= Reputation.CurrentRep && Reputation.CurrentRep < 200f)
            {
                return KerbBaseShout.RepLevel_Medium;
            }
            else if (200f <= Reputation.CurrentRep && Reputation.CurrentRep < 600f)
            {
                return KerbBaseShout.RepLevel_High;
            }
            else if (600f <= Reputation.CurrentRep && Reputation.CurrentRep < 1000f)
            {
                return KerbBaseShout.RepLevel_VeryHigh;
            }
            return KerbBaseShout.RepLevel_Any;
        }

        private KerbShout.Acct ensureKSCShoutAcctExists(String fullname)
        {
            KerbShout.Acct shoutAcct = KerbalSNSScenario.Instance.FindShoutAcct(fullname);
            if (shoutAcct == null)
            {
                shoutAcct = new KerbShout.Acct();

                shoutAcct.name = "TODO";
                shoutAcct.fullname = fullname;
                shoutAcct.username = "@KSC_" + makeLikeUsername(shoutAcct.fullname);

                KerbalSNSScenario.Instance.SaveShoutAcct(shoutAcct);
            }
            return shoutAcct;
        }

        public void RegenerateRandomShouts()
        {
            List<KerbShout> shoutList = KerbalSNSScenario.Instance.GetShoutList;
            foreach (KerbShout shout in shoutList) KerbalSNSScenario.Instance.DeleteShout(shout);
            updateShoutsIfNeeded(shoutList);
        }
    }
}