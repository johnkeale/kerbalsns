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

                    ensureKSCShoutAcctExists(fullname);
                    postedBy = KerbalSNSScenario.Instance.FindShoutAcct(fullname);
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

                List<KerbBaseShout> filteredBaseShoutList = filterByVesselAndProgressStatus(baseShoutList);

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
                    updatedShoutList.Add(shout);
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

        private List<KerbBaseShout> filterByVesselAndProgressStatus(List<KerbBaseShout> referenceBaseShoutList)
        {
            List<KerbBaseShout> filteredBaseShoutList = referenceBaseShoutList.ToList();
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

            return filteredBaseShoutList;
        }

        private List<KerbShout> generateRandomShouts(List<KerbBaseShout> referenceBaseShoutList, Func<KerbBaseShout, bool> predicate, int count)
        {
            return generateRandomShouts(referenceBaseShoutList, predicate, count, null);
        }

        private List<KerbShout> generateRandomShouts(List<KerbBaseShout> referenceBaseShoutList, Func<KerbBaseShout, bool> predicate, int count, Vessel vessel)
        {
            List<KerbBaseShout> filteredBaseShoutList = referenceBaseShoutList.Where(predicate).ToList();

            int neededShouts = count == -1 ? filteredBaseShoutList.Count : count;

            List <KerbShout> shoutList = new List<KerbShout>();

            if (filteredBaseShoutList.Count > 0)
            {
                for (int i = 0; i < neededShouts; i++)
                {
                    KerbBaseShout baseShout =
                        filteredBaseShoutList[mizer.Next(filteredBaseShoutList.Count)];

                    KerbShout.Acct postedBy = null;

                    if (baseShout.posterType.Equals(KerbBaseShout.PosterType_Specific))
                    {
                        KerbalSNSScenario.Instance.SaveShoutAcct(baseShout.specificPoster);
                        postedBy = baseShout.specificPoster;
                    }
                    else
                    {
                        if (baseShout.posterType.Equals(KerbBaseShout.PosterType_Any)
                            || baseShout.posterType.Equals(KerbBaseShout.PosterType_LayKerbal))
                        {
                            postedBy = new KerbShout.Acct();
                            postedBy.name = "TODO";

                            postedBy.fullname = KerbalSNSUtils.RandomLayKerbalName();
                            postedBy.username = "@" + makeLikeUsername(postedBy.fullname);
                        }
                        else if (baseShout.posterType.Equals(KerbBaseShout.PosterType_VesselCrew)
                            || baseShout.posterType.Equals(KerbBaseShout.PosterType_KSCEmployee))
                        {
                            String fullname = baseShout.posterType.Equals(KerbBaseShout.PosterType_VesselCrew) ?
                                KerbalSNSUtils.RandomActiveCrewKerbalName() :
                                KerbalSNSUtils.RandomLayKerbalName();

                            ensureKSCShoutAcctExists(fullname);
                            postedBy = KerbalSNSScenario.Instance.FindShoutAcct(fullname);
                        }
                        else if (baseShout.posterType.Equals(KerbBaseShout.PosterType_KSC))
                        {
                            postedBy = KerbShout.Acct.KSC_OFFICIAL;
                        }
                    }

                    KerbShout shout = createShout(baseShout, postedBy, vessel);

                    shoutList.Add(shout);
                }

            }

            return shoutList;
        }

        private List<KerbBaseShout> generateRandomBaseShouts(List<KerbBaseShout> referenceBaseShoutList, Func<KerbBaseShout, bool> predicate, int count)
        {
            List<KerbBaseShout> filteredBaseShoutList = referenceBaseShoutList.Where(predicate).ToList();
            int neededShouts = count == -1 ? filteredBaseShoutList.Count : count;

            List <KerbBaseShout> randomBaseShoutList = new List<KerbBaseShout>();

            if (filteredBaseShoutList.Count > 0)
            {
                for (int i = 0; i < neededShouts; i++)
                {
                    KerbBaseShout baseShout =
                        filteredBaseShoutList[mizer.Next(filteredBaseShoutList.Count)];
					filteredBaseShoutList.Remove(baseShout);

                    randomBaseShoutList.Add(baseShout);
                }
            }

            return randomBaseShoutList;
        }

        private KerbShout generateRandomGameEventShout(Func<KerbBaseShout, bool> predicate)
        {
            return generateRandomGameEventShout(predicate, null);
        }

        private KerbShout generateRandomGameEventShout(Func<KerbBaseShout, bool> predicate, Vessel vessel)
        {
            List<KerbShout> filteredShoutList =
                generateRandomShouts(this.baseShoutList, predicate, 1, vessel);

            if (filteredShoutList.Count() <= 0)
            {
                return null;
            }

            KerbShout shout = filteredShoutList.FirstOrDefault();
            shout.postedTime = Planetarium.GetUniversalTime();
			
            return shout;
        }

        private KerbShout generateRandomGameEventCrewShout(String gameEvent, ProtoCrewMember protoCrewMember, String specialization)
        {
            return generateRandomGameEventCrewShout(gameEvent, protoCrewMember, specialization, -1);
        }

        private KerbShout generateRandomGameEventCrewShout(String gameEvent, ProtoCrewMember protoCrewMember, String specialization, int newLevel)
        {
            List<KerbBaseShout> filteredBaseShoutList =
                generateRandomBaseShouts(
                    this.baseShoutList,
                    x => (
                        x.gameEvent != null && x.gameEvent.Equals(gameEvent)
                        && x.posterType.Equals(KerbBaseShout.PosterType_VesselCrew)
                        && (
                            x.gameEventSpecifics == null || specialization == null
                            || (
                                x.gameEventSpecifics.HasValue("specialization")
                                && x.gameEventSpecifics.GetValue("specialization").Equals(specialization)
                                && !x.gameEventSpecifics.HasValue("newLevel")
                            )
                            || (
                                x.gameEventSpecifics.HasValue("specialization")
                                && x.gameEventSpecifics.GetValue("specialization").Equals(specialization)
                                && x.gameEventSpecifics.HasValue("newLevel") && newLevel != -1
                                && Convert.ToInt32(x.gameEventSpecifics.GetValue("newLevel")) == newLevel
                            )
                        )
                        && x.repLevel == getCurrentRepLevel()
                    ),
                    1);

            if (filteredBaseShoutList.Count() <= 0)
            {
                return null;
            }

            KerbBaseShout baseShout = filteredBaseShoutList.FirstOrDefault();

            ensureKSCShoutAcctExists(protoCrewMember.name);
            KerbShout.Acct postedBy = KerbalSNSScenario.Instance.FindShoutAcct(protoCrewMember.name);

            KerbShout shout = createShout(baseShout, postedBy);
            shout.postedTime = Planetarium.GetUniversalTime();

            return shout;
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
            return createShout(baseShout, postedBy, null);
        }

        private KerbShout createShout(KerbBaseShout baseShout, KerbShout.Acct postedBy, Vessel vessel)
        {
            KerbShout shout = new KerbShout(baseShout);

            shout.postedId = "TODO";

            shout.postedBy = postedBy;
            shout.postedTime = Planetarium.GetUniversalTime();

            shout.postedText =
                Regex.Replace(baseShout.text, "#([\\w]+)", "<color=#29E667><u>#$1</u></color>", RegexOptions.IgnoreCase);

            if (baseShout.text.Contains("%v") || baseShout.text.Contains("%k"))
            {
                if (vessel == null)
                {
                    vessel = getRandomViableVessel(baseShout);
                }

                shout.postedText = shout.postedText.Replace("%v", vessel.GetDisplayName());

                int kerbalCount = Regex.Matches(baseShout.text, "%k").Count;

                int kerbalIndex = 1;
                List<ProtoCrewMember> crewList = vessel.GetVesselCrew().ToList();
                for (int i = 0; i < kerbalCount; i++)
                {
                    ProtoCrewMember randomKerbal = crewList[mizer.Next(crewList.Count)];
                    crewList.Remove(randomKerbal);

                    ensureKSCShoutAcctExists(randomKerbal.name);
                    KerbShout.Acct shoutAcct = KerbalSNSScenario.Instance.FindShoutAcct(randomKerbal.name);

                    shout.postedText = shout.postedText.Replace("%k" + kerbalIndex, shoutAcct.username);
                    kerbalIndex++;
                }
            }

            shout.postedText =
                Regex.Replace(shout.postedText, "@([\\w]+)", "<color=#6F8E2F><u>@$1</u></color>", RegexOptions.IgnoreCase);

            return shout;
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

        private void ensureKSCShoutAcctExists(String fullname)
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
        }

        public void RegenerateRandomShouts()
        {
            List<KerbShout> shoutList = KerbalSNSScenario.Instance.GetShoutList;
            foreach (KerbShout shout in shoutList) KerbalSNSScenario.Instance.DeleteShout(shout);
            updateShoutsIfNeeded(shoutList);
        }

        #region GameEvent methods

        // https://kerbalspaceprogram.com/api/class_game_events.html

        public void AddGameEventsCallbacks()
        {
            GameEvents.OnOrbitalSurveyCompleted.Add(OnOrbitalSurveyCompleted);
            GameEvents.onFlagPlant.Add(onFlagPlant);
            GameEvents.OnExperimentDeployed.Add(OnExperimentDeployed);
            GameEvents.OnScienceRecieved.Add(OnScienceRecieved);
            GameEvents.OnReputationChanged.Add(OnReputationChanged);
            GameEvents.OnScienceChanged.Add(OnScienceChanged);
            GameEvents.OnFundsChanged.Add(OnFundsChanged);
            GameEvents.OnCrewmemberHired.Add(OnCrewmemberHired);
            GameEvents.OnCrewmemberSacked.Add(OnCrewmemberSacked);
            GameEvents.onVesselRecoveryProcessing.Add(onVesselRecoveryProcessing);
            GameEvents.OnKSCStructureCollapsed.Add(OnKSCStructureCollapsed);
            GameEvents.OnKSCStructureRepaired.Add(OnKSCStructureRepaired);
            GameEvents.OnKSCFacilityUpgraded.Add(OnKSCFacilityUpgraded);
            GameEvents.OnTechnologyResearched.Add(OnTechnologyResearched);
            GameEvents.OnPartPurchased.Add(OnPartPurchased);
            GameEvents.OnPartUpgradePurchased.Add(OnPartUpgradePurchased);
            GameEvents.OnVesselRollout.Add(OnVesselRollout);
            GameEvents.OnProgressReached.Add(OnProgressReached);
            GameEvents.onVesselRename.Add(onVesselRename);
            GameEvents.onAsteroidSpawned.Add(onAsteroidSpawned);
            GameEvents.onKnowledgeChanged.Add(onKnowledgeChanged);
            GameEvents.onCrewOnEva.Add(onCrewOnEva);
            GameEvents.onCrewBoardVessel.Add(onCrewBoardVessel);
            GameEvents.onVesselSituationChange.Add(onVesselSituationChange);
            GameEvents.onVesselSOIChanged.Add(onVesselSOIChanged);
            GameEvents.onKerbalLevelUp.Add(onKerbalLevelUp);
            GameEvents.onFlightReady.Add(onFlightReady);
        }

        public void RemoveGameEventsCallbacks()
        {
            GameEvents.OnOrbitalSurveyCompleted.Remove(OnOrbitalSurveyCompleted);
            GameEvents.onFlagPlant.Remove(onFlagPlant);
            GameEvents.OnExperimentDeployed.Remove(OnExperimentDeployed);
            GameEvents.OnScienceRecieved.Remove(OnScienceRecieved);
            GameEvents.OnReputationChanged.Remove(OnReputationChanged);
            GameEvents.OnScienceChanged.Remove(OnScienceChanged);
            GameEvents.OnFundsChanged.Remove(OnFundsChanged);
            GameEvents.OnCrewmemberHired.Remove(OnCrewmemberHired);
            GameEvents.OnCrewmemberSacked.Remove(OnCrewmemberSacked);
            GameEvents.onVesselRecoveryProcessing.Remove(onVesselRecoveryProcessing);
            GameEvents.OnKSCStructureCollapsed.Remove(OnKSCStructureCollapsed);
            GameEvents.OnKSCStructureRepaired.Remove(OnKSCStructureRepaired);
            GameEvents.OnKSCFacilityUpgraded.Remove(OnKSCFacilityUpgraded);
            GameEvents.OnTechnologyResearched.Remove(OnTechnologyResearched);
            GameEvents.OnPartPurchased.Remove(OnPartPurchased);
            GameEvents.OnPartUpgradePurchased.Remove(OnPartUpgradePurchased);
            GameEvents.OnVesselRollout.Remove(OnVesselRollout);
            GameEvents.OnProgressReached.Remove(OnProgressReached);
            GameEvents.onVesselRename.Remove(onVesselRename);
            GameEvents.onAsteroidSpawned.Remove(onAsteroidSpawned);
            GameEvents.onKnowledgeChanged.Remove(onKnowledgeChanged);
            GameEvents.onCrewOnEva.Remove(onCrewOnEva);
            GameEvents.onCrewBoardVessel.Remove(onCrewBoardVessel);
            GameEvents.onVesselSituationChange.Remove(onVesselSituationChange);
            GameEvents.onVesselSOIChanged.Remove(onVesselSOIChanged);
            GameEvents.onKerbalLevelUp.Remove(onKerbalLevelUp);
            GameEvents.onFlightReady.Remove(onFlightReady);
        }

        public void OnOrbitalSurveyCompleted(Vessel vessel, CelestialBody body)
        {
            KerbShout shout = generateRandomGameEventShout(
				x => (
					x.gameEvent != null && x.gameEvent.Equals("OnOrbitalSurveyCompleted")
					&& KerbalSNSUtils.IsVesselTypeCorrect(vessel, x.vesselType)
					&& KerbalSNSUtils.DoesVesselSituationMatch(vessel, body, x.vesselSituation)
					&& x.repLevel == getCurrentRepLevel()
				)
			);

            if (shout != null)
            {
				KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void onFlagPlant(Vessel vessel)
        {
            CelestialBody body = vessel.mainBody;
            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("onFlagPlant")
                    && x.vesselSituation.StartsWith(body.name)
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnExperimentDeployed(ScienceData data)
        {
            String[] dataSubjectID = data.subjectID.Split('@'); 
            String scienceType = dataSubjectID[0];
            String situation = dataSubjectID[1];

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnExperimentDeployed")
                    && (
                        x.gameEventSpecifics == null
                        || (
                            x.gameEventSpecifics.HasValue("scienceType") 
                            && x.gameEventSpecifics.GetValue("scienceType").Equals(scienceType)
                            && x.gameEventSpecifics.HasValue("situation")
                            && x.gameEventSpecifics.GetValue("situation").Equals(situation)
                        )
                    )
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnScienceRecieved(float science, ScienceSubject subj, ProtoVessel protoVessel, bool flag)
        {
            Vessel vessel = protoVessel.vesselRef;

            String[] dataSubjectID = subj.id.Split('@');
            String scienceType = dataSubjectID[0];
            String situation = dataSubjectID[1];

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnScienceRecieved")
                    && (
                        x.gameEventSpecifics == null
                        || (
                            x.gameEventSpecifics.HasValue("scienceType")
                            && x.gameEventSpecifics.GetValue("scienceType").Equals(scienceType)
                            && x.gameEventSpecifics.HasValue("situation")
                            && x.gameEventSpecifics.GetValue("situation").Equals(situation)
                        )
                    )
                    && x.repLevel == getCurrentRepLevel() // XXX maybe check only if there is repLevel?
                ),
                vessel
            );

            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnReputationChanged(float latestReputation, TransactionReasons reason)
        {
            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnReputationChanged")
                    && (
                        x.gameEventSpecifics == null
                        || (
                            x.gameEventSpecifics.HasValue("reason")
                            && x.gameEventSpecifics.GetValue("reason").Equals(KerbalSNSUtils.TransactionReasonToString(reason))
                            // TODO maybe add another condition for latestReputation (e.g. if deltaRep eq certainAmt)
                        )
                    )
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnScienceChanged(float latestScience, TransactionReasons reason)
        {
            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnScienceChanged")
                    && (
                        x.gameEventSpecifics == null
                        || (
                            x.gameEventSpecifics.HasValue("reason")
                            && x.gameEventSpecifics.GetValue("reason").Equals(KerbalSNSUtils.TransactionReasonToString(reason))
                            // TODO maybe add another condition for latestScience (e.g. if deltaScience gte certainAmt)
                        )
                    )
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnFundsChanged(double latestFunds, TransactionReasons reason)
        {
            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnFundsChanged")
                    && (
                        x.gameEventSpecifics == null
                        || (
                            x.gameEventSpecifics.HasValue("reason")
                            && x.gameEventSpecifics.GetValue("reason").Equals(KerbalSNSUtils.TransactionReasonToString(reason))
                            // TODO maybe add another condition for latestFunds (e.g. if deltaFunds lte certainAmt)
                        )
                    )
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnCrewmemberHired(ProtoCrewMember protoCrewMember, int newActiveCrewCount)
        {
            String specialization = protoCrewMember.experienceTrait.TypeName;

            // TODO maybe randomize whether to shout or not
            KerbShout shout = generateRandomGameEventCrewShout("OnCrewmemberHired", protoCrewMember, specialization);
            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }
		
        public void OnCrewmemberSacked(ProtoCrewMember protoCrewMember, int newActiveCrewCount)
        {
            String specialization = protoCrewMember.experienceTrait.TypeName;

            // TODO maybe randomize whether to shout or not
            KerbShout shout = generateRandomGameEventCrewShout("OnCrewmemberSacked", protoCrewMember, specialization);
            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void onVesselRecoveryProcessing(ProtoVessel protoVessel, MissionRecoveryDialog dialog, float recoveredFundsPercentage)
        {
            Vessel vessel = protoVessel.vesselRef;
            if (vessel.vesselType == VesselType.Debris || vessel.vesselType == VesselType.SpaceObject 
                || vessel.vesselType == VesselType.Unknown)
            {
                return; // TODO consider if will also exclude EVA or Flag
            }

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("onVesselRecoveryProcessing")
                    && x.repLevel == getCurrentRepLevel()
                ),
                vessel
            );

            if (shout != null)
            {
                // TODO use recoveredFundsPercentage
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnKSCStructureCollapsed(DestructibleBuilding bldg)
        {
            String[] idComponents = bldg.id.Split('/');
            String facility = idComponents[1];

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnKSCStructureCollapsed")
                    && (
                        x.gameEventSpecifics == null
                        || (
                            x.gameEventSpecifics.HasValue("facility")
                            && x.gameEventSpecifics.GetValue("facility").Equals(facility)
                        )
                    )
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnKSCStructureRepaired(DestructibleBuilding bldg)
        {
            String[] idComponents = bldg.id.Split('/');
            String facility = idComponents[1];

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnKSCStructureRepaired")
                    && (
                        x.gameEventSpecifics == null
                        || (
                            x.gameEventSpecifics.HasValue("facility")
                            && x.gameEventSpecifics.GetValue("facility").Equals(facility)
                        )
                    )
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnKSCFacilityUpgraded(UpgradeableFacility facility, int oldLevel)
        {
            String[] idComponents = facility.id.Split('/');
            String facilityName = idComponents[1];
            
            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnKSCFacilityUpgraded")
                    && (
                        x.gameEventSpecifics == null
                        || (
                            x.gameEventSpecifics.HasValue("facility")
                            && x.gameEventSpecifics.GetValue("facility").Equals(facilityName)
                        )
                    )
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnTechnologyResearched(HostTargetAction<RDTech, RDTech.OperationResult> hostTargetAction)
        {
            RDTech.OperationResult researchResult = hostTargetAction.target;
            if (hostTargetAction.target == RDTech.OperationResult.Successful)
            {
                String techId = hostTargetAction.host.techID;
                String name = hostTargetAction.host.name;
                String description = hostTargetAction.host.description;

                KerbShout shout = generateRandomGameEventShout(
                    x => (
                        x.gameEvent != null && x.gameEvent.Equals("OnTechnologyResearched")
                        && (
                            x.gameEventSpecifics == null
                            || (
                                x.gameEventSpecifics.HasValue("techId")
                                && x.gameEventSpecifics.GetValue("techId").Equals(techId)
                            )
                        )
                        && x.repLevel == getCurrentRepLevel()
                    )
                );

                if (shout != null)
                {
                    // TODO maybe use the name and description somewhere?
                    KerbalSNSScenario.Instance.RegisterShout(shout);
                }
            }
        }

        public void OnPartPurchased(AvailablePart availablePart)
        {
            // TODO make it so that only one shout is done per purchase
            String name = availablePart.name;
            String title = availablePart.title;
            String manufacturer = availablePart.manufacturer;

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnPartPurchased")
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%p", title);
                shout.postedText = shout.postedText.Replace("%m", manufacturer);

                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnPartUpgradePurchased(PartUpgradeHandler.Upgrade upgrade)
        {
            String name = upgrade.name;
            String title = upgrade.title;
            String manufacturer = upgrade.manufacturer;

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnPartUpgradePurchased")
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%p", title);
                shout.postedText = shout.postedText.Replace("%m", manufacturer);

                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnVesselRollout(ShipConstruct shipConstruct)
        {
            String vesselName = shipConstruct.shipName;
            String facility = "";
            if (shipConstruct.shipFacility == EditorFacility.SPH)
            {
                facility = "Spaceplane Hangar";
            }
            else if (shipConstruct.shipFacility == EditorFacility.VAB)
            {
                facility = "Vehicle Assembly Building";
            }

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnVesselRollout")
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%rv", vesselName);
                shout.postedText = shout.postedText.Replace("%f", facility);

                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void OnProgressReached(ProgressNode progressNode)
        {
            // FIXME find a way to get the celestial body and add it as a filter
            String progressNodeId = progressNode.Id;
            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnProgressReached")
                    && (
                        x.gameEventSpecifics == null
                        || (
                            x.gameEventSpecifics.HasValue("progressNodeId")
                            && x.gameEventSpecifics.GetValue("progressNodeId").Equals(progressNodeId)
                        )
                    )
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void onVesselRename(GameEvents.HostedFromToAction<Vessel, String> hostedFromToAction)
        {
            Vessel vessel = hostedFromToAction.host;
            String oldName = hostedFromToAction.from;
            String newName = hostedFromToAction.to;

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("onVesselRename")
                    //&& KerbalSNSUtils.IsVesselTypeCorrect(vessel, x.vesselType)
                    && x.repLevel == getCurrentRepLevel()
                ),
                vessel
            );

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%ov", oldName);
                shout.postedText = shout.postedText.Replace("%nv", newName);

                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        private HashSet<String> shoutedAsteroidsCache;

        public void onAsteroidSpawned(Vessel asteroid)
        {
            if (shoutedAsteroidsCache == null)
            {
                shoutedAsteroidsCache = new HashSet<string>();
            }

            String designatedName = asteroid.GetDisplayName();
            CelestialBody mainBody = asteroid.mainBody;

            if (shoutedAsteroidsCache.Contains(designatedName))
            {
                return; // for some reason, onAsteroidSpawned is getting called multiple times. A check was added to prevent spam shouts
            }

            shoutedAsteroidsCache.Add(designatedName);

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("onAsteroidSpawned")
                    && x.repLevel == getCurrentRepLevel()
                )
            );

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%a", designatedName);
                shout.postedText = shout.postedText.Replace("%b", mainBody.name);
                KerbalSNSScenario.Instance.RegisterShout(shout);

                // TODO add delayed shout if posterType is layKerbal
            }
        }

        public void onKnowledgeChanged(HostedFromToAction<IDiscoverable, DiscoveryLevels> hostedFromToAction)
        {
            // TODO study discoverylevels
            //IDiscoverable discoverable = hostedFromToAction.host;
            //String discoverableName = discoverable.DiscoveryInfo.name.Value;

            //DiscoveryLevels discoveryLevelFrom = hostedFromToAction.from;
            //DiscoveryLevels discoveryLevelTo = hostedFromToAction.to;
        }

        public void onCrewOnEva(GameEvents.FromToAction<Part, Part> fromToAction)
        {
            Part hatch = fromToAction.from;
            Vessel vessel = hatch.vessel;

            Part kerbalEVA = fromToAction.to;
            Vessel kerbal = kerbalEVA.vessel;
            ProtoCrewMember protoCrewMember = kerbal.GetVesselCrew().FirstOrDefault();

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("OnCrewOnEVA")
                    && (
                        x.gameEventSpecifics == null
                        || (
                            x.gameEventSpecifics.HasValue("vesselSituation") // XXX use shout.vesselSituation
                            && KerbalSNSUtils.DoesVesselSituationMatch(vessel, x.gameEventSpecifics.GetValue("vesselSituation"))
                        )
                    )
                    && x.repLevel == getCurrentRepLevel()
                ),
                vessel
            );

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%ek", protoCrewMember.name);

                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void onCrewBoardVessel(GameEvents.FromToAction<Part, Part> fromToAction)
        {
            // TODO check if kerbal is returning or enters the vessel for the first time
            Part hatch = fromToAction.to;
            Vessel vessel = hatch.vessel;

            Part kerbalEVA = fromToAction.from;
            Vessel kerbal = kerbalEVA.vessel;

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("onCrewBoardVessel")
                    && x.repLevel == getCurrentRepLevel()
                ),
                vessel
            );

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%ek", kerbal.GetDisplayName());

                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void onVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> hostedFromToAction)
        {
            Vessel vessel = hostedFromToAction.host;
            CelestialBody body = vessel.mainBody;
            
            Vessel.Situations fromSituation = hostedFromToAction.from;
            Vessel.Situations toSituation = hostedFromToAction.to;

            if (fromSituation != 0)
            {
                KerbShout shout = generateRandomGameEventShout(
                    x => (
                        x.gameEvent != null && x.gameEvent.Equals("onVesselSituationChange")
                        && KerbalSNSUtils.IsVesselTypeCorrect(vessel, x.vesselType)
                        && (
                            x.gameEventSpecifics == null
                            || (
                                (
                                    x.gameEventSpecifics.HasValue("fromSituation")
                                    && fromSituation == KerbalSNSUtils.StringToSituation(x.gameEventSpecifics.GetValue("fromSituation"))
                                    && x.gameEventSpecifics.HasValue("toSituation")
                                    && toSituation == KerbalSNSUtils.StringToSituation(x.gameEventSpecifics.GetValue("toSituation"))
                                    && x.gameEventSpecifics.HasValue("bodyName")
                                    && body.name.Equals(x.gameEventSpecifics.GetValue("bodyName"))
                                )
                                || (
                                    !x.gameEventSpecifics.HasValue("fromSituation")
                                    && x.gameEventSpecifics.HasValue("toSituation")
                                    && toSituation == KerbalSNSUtils.StringToSituation(x.gameEventSpecifics.GetValue("toSituation"))
                                    && x.gameEventSpecifics.HasValue("bodyName")
                                    && body.name.Equals(x.gameEventSpecifics.GetValue("bodyName"))
                                )
                            )
                        )
                        && x.repLevel == getCurrentRepLevel()
                    ),
                    vessel
                );

                if (shout != null)
                {
                    KerbalSNSScenario.Instance.RegisterShout(shout);
                }
            }
        }

        public void onVesselSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> hostedFromToAction)
        {
            Vessel vessel = hostedFromToAction.host;
            CelestialBody fromBody = hostedFromToAction.from;
            CelestialBody toBody = hostedFromToAction.to;

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("onVesselSOIChanged")
                    && KerbalSNSUtils.IsVesselTypeCorrect(vessel, x.vesselType)
                    && (
                        x.gameEventSpecifics == null
                        || (
                            x.gameEventSpecifics.HasValue("fromBodyName")
                            && fromBody.name == x.gameEventSpecifics.GetValue("fromBodyName")
                            && x.gameEventSpecifics.HasValue("toBodyName")
                            && toBody.name == x.gameEventSpecifics.GetValue("toBodyName")
                        )
                    )
                    && x.repLevel == getCurrentRepLevel()
                ),
                vessel
            );

            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void onKerbalLevelUp(ProtoCrewMember protoCrewMember)
        {
            String specialization = protoCrewMember.experienceTrait.TypeName;
            int newLevel = protoCrewMember.experienceLevel;

            // TODO maybe randomize whether to shout or not
            KerbShout shout = generateRandomGameEventCrewShout("onKerbalLevelUp", protoCrewMember, specialization, newLevel);
            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        public void onFlightReady()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("onFlightReady")
                    && KerbalSNSUtils.IsVesselTypeCorrect(vessel, x.vesselType)
                    && KerbalSNSUtils.DoesVesselSituationMatch(vessel, x.vesselSituation)
                    && x.repLevel == getCurrentRepLevel()
                ),
                vessel
            );

            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }

        #endregion
    }
}