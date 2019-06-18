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
        private KerbShout generateRandomGameEventShout(Func<KerbBaseShout, bool> predicate)
        {
            List<KerbBaseShout> filteredBaseShoutList = this.baseShoutList.Where(predicate).ToList();
            if (filteredBaseShoutList.Count == 0)
            {
                return null;
            }

			KerbBaseShout baseShout = // XXX add null check
				filteredBaseShoutList[mizer.Next(filteredBaseShoutList.Count)];

			KerbShout shout = createShout(baseShout, generateShoutAcctFromBaseShout(baseShout));
            formatShoutTags(shout);
            formatShoutMentions(shout);
			
            return shout;
        }

        private KerbShout generateRandomGameEventCrewShout(String gameEvent, ProtoCrewMember protoCrewMember, String specialization, int newLevel)
        {
            List<KerbBaseShout> filteredBaseShoutList =
				this.baseShoutList.Where(
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
                    )).ToList();
            if (filteredBaseShoutList.Count == 0)
            {
                return null;
            }

            KerbBaseShout baseShout =
				filteredBaseShoutList[mizer.Next(filteredBaseShoutList.Count)];

			KerbShout shout = createShout(baseShout, ensureKSCShoutAcctExists(protoCrewMember.name));
            formatShoutTags(shout);
            formatShoutMentions(shout);

            return shout;
        }
		
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
					&& KerbalSNSUtils.DoesVesselSituationMatch(vessel, x.vesselSituation)
                    && (x.vesselSituation != null && x.vesselSituation.StartsWith(body.name))
					&& x.repLevel == getCurrentRepLevel()
				)
			);

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%v", vessel.GetDisplayName());
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
                )
            );

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%v", vessel.GetDisplayName());
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
            KerbShout shout = generateRandomGameEventCrewShout("OnCrewmemberHired", protoCrewMember, specialization, -1);
            if (shout != null)
            {
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }
		
        public void OnCrewmemberSacked(ProtoCrewMember protoCrewMember, int newActiveCrewCount)
        {
            String specialization = protoCrewMember.experienceTrait.TypeName;

            // TODO maybe randomize whether to shout or not
            KerbShout shout = generateRandomGameEventCrewShout("OnCrewmemberSacked", protoCrewMember, specialization, -1);
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
                )
            );

            if (shout != null)
            {
                // TODO use recoveredFundsPercentage
                shout.postedText = shout.postedText.Replace("%v", vessel.GetDisplayName());
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

            // TODO make it possible to also generate shout if only the type was changed
            if (oldName.Equals(newName))
            {
                return;
            }

            KerbShout shout = generateRandomGameEventShout(
                x => (
                    x.gameEvent != null && x.gameEvent.Equals("onVesselRename")
                    //&& KerbalSNSUtils.IsVesselTypeCorrect(vessel, x.vesselType)
                    && x.repLevel == getCurrentRepLevel()
                )
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
                )
            );

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%v", vessel.GetDisplayName());
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
                )
            );

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%v", vessel.GetDisplayName());
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
                    )
                );

                if (shout != null)
                {
                    shout.postedText = shout.postedText.Replace("%v", vessel.GetDisplayName());
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
                )
            );

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%v", vessel.GetDisplayName());
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
                )
            );

            if (shout != null)
            {
                shout.postedText = shout.postedText.Replace("%v", vessel.GetDisplayName());
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }
        }
    }
}