using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KerbalSNS
{
    class KerbShoutHelper
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

            ConfigNode rootShoutNode = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/KerbalSNS/baseShoutsList.cfg");
            ConfigNode shoutListNode = rootShoutNode.GetNode(KerbBaseShout.NODE_NAME_PLURAL);

            ConfigNode[] shoutArray = shoutListNode.GetNodes();
            foreach (ConfigNode shoutNode in shoutArray)
            {
                KerbBaseShout shout = new KerbBaseShout();
                shout.LoadFromConfigNode(shoutNode);
                baseShoutList.Add(shout);
            }
        }

        public List<KerbShout> GetPostedShouts()
        {
            List<KerbShout> shoutList = KerbalSNSScenario.Instance.GetShoutList; // TODO fix bad name
            shoutList = updateShoutsIfNeeded(shoutList);
            shoutList = shoutList.OrderByDescending(s => s.postedTime).ToList();
            return shoutList;
        }

        public void GenerateShout(String text)
        {
            KerbBaseShout baseShout = new KerbBaseShout();

            baseShout.name = "TODO";
            baseShout.repLevel = KerbBaseShout.RepLevel.Any;
            baseShout.type = KerbBaseShout.ShoutType.Random;
            baseShout.text = text;

            baseShout.poster = KerbBaseShout.ShoutPoster.KSC;
            KerbShout.Acct postedBy = KerbShout.Acct.KSC_OFFICIAL;

            if (FlightGlobals.ActiveVessel != null)
            {
                String fullname = KerbalSNSUtils.RandomVesselCrewKerbalName(FlightGlobals.ActiveVessel);
                if (fullname != null)
                {
                    baseShout.poster = KerbBaseShout.ShoutPoster.VesselCrew;

                    KerbShout.Acct shoutAcct = KerbalSNSScenario.Instance.FindShoutAcct(postedBy.fullname);
                    if (shoutAcct != null)
                    {
                        postedBy = shoutAcct;
                    }
                    else
                    {
                        postedBy.name = "TODO";
                        postedBy.fullname = fullname;
                        postedBy.username = "@KSC_" + makeLikeUsername(postedBy.fullname);

                        KerbalSNSScenario.Instance.SaveShoutAcct(postedBy);
                    }
                }
            }

            KerbShout shout = createShout(baseShout, postedBy);
            KerbalSNSScenario.Instance.RegisterShout(shout);
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

                List<KerbShout> repLevelShoutList =
                    generateShouts(
                        x => (
                            x.type == KerbBaseShout.ShoutType.RepLevel
                            && x.repLevel == getCurrentRepLevel()
                        ),
                        repLevelShoutCount,
                        now);
                foreach (KerbShout shout in repLevelShoutList)
                {
                    updatedShoutList.Add(shout);
                }

                List<KerbShout> outlierRepLevelShoutList =
                    generateShouts(
                        x => (
                            x.type == KerbBaseShout.ShoutType.RepLevel
                            && x.repLevel != getCurrentRepLevel()
                        ),
                        outlierRepLevelShoutCount,
                        now);
                foreach (KerbShout shout in outlierRepLevelShoutList)
                {
                    updatedShoutList.Add(shout);
                }

                List<KerbShout> otherShoutList =
                    generateShouts(
                        x => (
                            x.type != KerbBaseShout.ShoutType.RepLevel
                        ),
                        neededShoutCount - repLevelShoutCount,
                        now);
                foreach (KerbShout shout in otherShoutList)
                {
                    updatedShoutList.Add(shout);
                }
            }

            return updatedShoutList;
        }

        private List<KerbShout> generateShouts(Func<KerbBaseShout, bool> predicate, int count, double baseTime)
        {
            List<KerbBaseShout> filteredBaseShoutList = baseShoutList.Where(predicate).ToList();
            filteredBaseShoutList =
                filteredBaseShoutList.Where(x => KerbalSNSUtils.HasAchievedAllProgressReqt(x.progressReqtArray)).ToList();

            List<KerbShout> shoutList = new List<KerbShout>();

            if (filteredBaseShoutList.Count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    KerbBaseShout baseShout =
                        filteredBaseShoutList[mizer.Next(filteredBaseShoutList.Count)];

                    KerbShout.Acct postedBy = buildShoutPostedBy(baseShout);

                    KerbShout shout = createShout(baseShout, postedBy); // TODO add maximum time? (e.g. don't insert shouts on the list)
                    shout.postedTime = baseTime - mizer.Next(KSPUtil.dateTimeFormatter.Hour) + 1; // set time to random time in most recent hour

                    shoutList.Add(shout);
                    KerbalSNSScenario.Instance.RegisterShout(shout);
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

        private KerbShout createShout(KerbBaseShout baseShout, KerbShout.Acct postedBy)
        {
            KerbShout shout = new KerbShout(baseShout);

            shout.postedId = "TODO";

            shout.postedBy = postedBy;
            shout.postedTime = Planetarium.GetUniversalTime();

            shout.postedText =
                Regex.Replace(baseShout.text, "#([\\w]+)", "<color=#29E667><u>#$1</u></color>", RegexOptions.IgnoreCase);
            shout.postedText =
                Regex.Replace(shout.postedText, "@([\\w]+)", "<color=#6F8E2F><u>@$1</u></color>", RegexOptions.IgnoreCase);

            // TODO add some formatting if needed

            return shout;
        }

        private KerbShout.Acct buildShoutPostedBy(KerbBaseShout baseShout)
        {
            if (baseShout.poster == KerbBaseShout.ShoutPoster.Specific)
            {
                KerbalSNSScenario.Instance.SaveShoutAcct(baseShout.specificPoster);
                return baseShout.specificPoster;
            }

            KerbShout.Acct postedBy = new KerbShout.Acct();
            postedBy.name = "TODO";

            if (baseShout.poster == KerbBaseShout.ShoutPoster.Any
                || baseShout.poster == KerbBaseShout.ShoutPoster.LayKerbal
                || baseShout.poster == KerbBaseShout.ShoutPoster.Unknown)
            {
                postedBy.fullname = KerbalSNSUtils.RandomLayKerbalName();
                postedBy.username = "@" + makeLikeUsername(postedBy.fullname);
            }
            else if (baseShout.poster == KerbBaseShout.ShoutPoster.VesselCrew)
            {
                postedBy.fullname = KerbalSNSUtils.RandomActiveCrewKerbalName();

                KerbShout.Acct shoutAcct = KerbalSNSScenario.Instance.FindShoutAcct(postedBy.fullname);
                if (shoutAcct != null)
                {
                    postedBy = shoutAcct;
                }
                else
                {
                    postedBy.username = "@KSC_" + makeLikeUsername(postedBy.fullname);
                }
                KerbalSNSScenario.Instance.SaveShoutAcct(postedBy);
            }
            else if (baseShout.poster == KerbBaseShout.ShoutPoster.KSCEmployee)
            {
                postedBy.fullname = KerbalSNSUtils.RandomLayKerbalName();

                KerbShout.Acct shoutAcct = KerbalSNSScenario.Instance.FindShoutAcct(postedBy.fullname);
                if (shoutAcct != null)
                {
                    postedBy = shoutAcct;
                }
                else
                {
                    postedBy.username = "@KSC_" + makeLikeUsername(postedBy.fullname);
                }
                KerbalSNSScenario.Instance.SaveShoutAcct(postedBy);
            }
            else if (baseShout.poster == KerbBaseShout.ShoutPoster.KSC)
            {
                postedBy = KerbShout.Acct.KSC_OFFICIAL;
            }

            return postedBy;
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

        private KerbShout.RepLevel getCurrentRepLevel()
        {
            if (-1000f <= Reputation.CurrentRep && Reputation.CurrentRep < -600f)
            {
                return KerbBaseShout.RepLevel.VeryLow;
            }
            else if (-600f <= Reputation.CurrentRep && Reputation.CurrentRep < -200f)
            {
                return KerbBaseShout.RepLevel.Low;
            }
            else if (-200f <= Reputation.CurrentRep && Reputation.CurrentRep < 200f)
            {
                return KerbBaseShout.RepLevel.Medium;
            }
            else if (200f <= Reputation.CurrentRep && Reputation.CurrentRep < 600f)
            {
                return KerbBaseShout.RepLevel.High;
            }
            else if (600f <= Reputation.CurrentRep && Reputation.CurrentRep < 1000f)
            {
                return KerbBaseShout.RepLevel.VeryHigh;
            }
            return KerbBaseShout.RepLevel.Unknown;
        }

        public void RegenerateShouts()
        {
            List<KerbShout> shoutList = KerbalSNSScenario.Instance.GetShoutList;
            foreach (KerbShout shout in shoutList) KerbalSNSScenario.Instance.DeleteShout(shout);
            updateShoutsIfNeeded(shoutList);
        }
    }
}