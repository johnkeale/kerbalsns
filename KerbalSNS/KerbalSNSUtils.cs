using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSNS
{
    class KerbalSNSUtils
    {
        public const int VesselTypeNone = -2;
        public const int VesselTypeAny = -1;

        private static System.Random mizer = new System.Random();

        public static bool HasAchievedAllProgressReqt(String[] progressReqtArray)
        {
            if (progressReqtArray == null)
            {
                return true; // no requirements, so technically, has achieved them all
            }
            else
            {
                bool hasAchievedProgressReqt = true;
                foreach (String progressReqt in progressReqtArray)
                {
                    String progressName = progressReqt;
                    bool isNegated = progressName.StartsWith("!");
                    if (isNegated)
                    {
                        progressName = progressName.Substring(1, progressName.Length - 1);
                    }

                    ProgressNode progressNode = ProgressTracking.Instance.FindNode(progressName);
                    if (progressNode == null)
                    {
                        CelestialBody body = FlightGlobals.Bodies.FirstOrDefault(b => progressName.StartsWith(b.name));
                        if (body != null)
                        {
                            progressName = progressName.Substring(body.name.Length, progressName.Length - body.name.Length);
                            progressNode = ProgressTracking.Instance.FindNode(body.name, progressName);
                        }
                    }

                    if (progressNode != null)
                    {
                        hasAchievedProgressReqt = hasAchievedProgressReqt && (isNegated ? !progressNode.IsComplete : progressNode.IsComplete);
                    }
                    else
                    {
                        hasAchievedProgressReqt = hasAchievedProgressReqt && isNegated;
                    }
                }

                return hasAchievedProgressReqt;
            }
        }

        public static bool HasEnoughCrew(Vessel vessel, int crewCount)
        {
            return vessel.GetCrewCount() >= crewCount;
        }

        public static bool IsVesselTypeCorrect(Vessel vessel, int vesselType)
        {
            return (
                    vesselType == VesselTypeAny
                    && vessel.vesselType != VesselType.Debris
                    && vessel.vesselType != VesselType.SpaceObject
                    && vessel.vesselType != VesselType.Unknown
                    && vessel.vesselType != VesselType.EVA
                    && vessel.vesselType != VesselType.Flag
                )
                || (vessel.vesselType == (VesselType)vesselType);
        }

        public static bool DoesVesselSituationMatch(Vessel vessel, CelestialBody body, String vesselSituation)
        {
            if (vesselSituation == null)
            {
                return true;
            }
            
            if (vesselSituation.StartsWith(body.name))
            {
                vesselSituation =
                    vesselSituation.Substring(body.name.Length, vesselSituation.Length - body.name.Length);

                return vessel.mainBody.Equals(body) && vessel.situation == StringToSituation(vesselSituation);
            }
            else
            {
                return false;
            }
        }
        
        public static bool DoesVesselSituationMatch(Vessel vessel, String vesselSituation)
        {
            if (vesselSituation == null)
            {
                return true;
            }

            CelestialBody body =
                FlightGlobals.Bodies.FirstOrDefault(b => vesselSituation.StartsWith(b.name));
            if (body != null)
            {
                vesselSituation =
                    vesselSituation.Substring(body.name.Length, vesselSituation.Length - body.name.Length);
                
                return vessel.mainBody.Equals(body) && vessel.situation == StringToSituation(vesselSituation);
            }
            else
            {
                return false;
            }
        }

        public static Vessel.Situations StringToSituation(String situation)
        {
            if (situation.Equals("Landed"))
            {
                return Vessel.Situations.LANDED;
            }
            else if (situation.Equals("Splashed"))
            {
                return Vessel.Situations.SPLASHED;
            }
            else if (situation.Equals("Prelaunch"))
            {
                return Vessel.Situations.PRELAUNCH;
            }
            else if (situation.Equals("Flying"))
            {
                return Vessel.Situations.FLYING;
            }
            else if (situation.Equals("SubOrbital"))
            {
                return Vessel.Situations.SUB_ORBITAL;
            }
            else if (situation.Equals("Orbiting"))
            {
                return Vessel.Situations.ORBITING;
            }
            else if (situation.Equals("Escaping"))
            {
                return Vessel.Situations.ESCAPING;
            }
            else if (situation.Equals("Docked"))
            {
                return Vessel.Situations.DOCKED;
            }
            return Vessel.Situations.PRELAUNCH;
        }

        public static String RandomKerbalName()
        {
            return CrewGenerator.GetRandomName((ProtoCrewMember.Gender)mizer.Next(2), mizer);
        }

        public static String RandomLayKerbalName()
        {
            String randomName = RandomKerbalName();
            while (HighLogic.CurrentGame.CrewRoster.Exists(randomName))
            {
                randomName = RandomKerbalName();
            }
            return randomName;
        }

        public static String RandomCrewKerbalName()
        {
            // TODO add sanity checks e.g. all crew is kia or missing :(
            ProtoCrewMember kerbal =
                HighLogic.CurrentGame.CrewRoster[mizer.Next(HighLogic.CurrentGame.CrewRoster.Count)];
            return kerbal.name;
        }

        public static String RandomActiveCrewKerbalName()
        {
            // TODO add sanity checks e.g. all crew is kia or missing :(
            ProtoCrewMember kerbal =
                HighLogic.CurrentGame.CrewRoster[mizer.Next(HighLogic.CurrentGame.CrewRoster.Count)];
            while (kerbal.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
            {
                kerbal =
                    HighLogic.CurrentGame.CrewRoster[mizer.Next(HighLogic.CurrentGame.CrewRoster.Count)];
            }
            return kerbal.name;
        }

        public static String RandomVesselCrewKerbalName(Vessel vessel)
        {
            List<ProtoCrewMember> vesselCrewList = vessel.GetVesselCrew();
            if (vesselCrewList.Count == 0)
            {
                return null;
            }

            ProtoCrewMember kerbal = vesselCrewList[mizer.Next(vesselCrewList.Count)];
            return kerbal.name;
        }

        public static String RandomApplicantKerbalName()
        {
            // TODO add sanity checks e.g. all crew is kia or missing :(
            ProtoCrewMember kerbal =
                HighLogic.CurrentGame.CrewRoster[mizer.Next(HighLogic.CurrentGame.CrewRoster.Count)];
            while (kerbal.rosterStatus != ProtoCrewMember.RosterStatus.Available)
            {
                kerbal =
                    HighLogic.CurrentGame.CrewRoster[mizer.Next(HighLogic.CurrentGame.CrewRoster.Count)];
            }
            return kerbal.name;
        }

        public static String GetRelativeTime(double time)
        {
            double now = Planetarium.GetUniversalTime();
            double delta = now - time;

            // TODO test on other planets, maybe the year/day/hour/minute might be different
            int years = (((int)delta) / KSPUtil.dateTimeFormatter.Year) + 1;

            int remainder = ((int)delta) % KSPUtil.dateTimeFormatter.Year;
            int days = (remainder / KSPUtil.dateTimeFormatter.Day) + 1;

            remainder = ((int)delta) % KSPUtil.dateTimeFormatter.Day;
            int hours = (remainder / KSPUtil.dateTimeFormatter.Hour) + 1;

            remainder = ((int)delta) % KSPUtil.dateTimeFormatter.Hour;
            int minutes = remainder / KSPUtil.dateTimeFormatter.Minute;

            int seconds = remainder % KSPUtil.dateTimeFormatter.Minute;

            if (delta < 1 * KSPUtil.dateTimeFormatter.Minute)
                return seconds <= 1 ? "one second ago" : seconds + " seconds ago";

            if (delta < 2 * KSPUtil.dateTimeFormatter.Minute)
                return "a minute ago";

            if (delta < 45 * KSPUtil.dateTimeFormatter.Minute)
                return minutes + " minutes ago";

            if (delta < 90 * KSPUtil.dateTimeFormatter.Minute)
                return "an hour ago";

            if (delta < 6 * KSPUtil.dateTimeFormatter.Hour)
                return hours + " hours ago";

            if (delta < 12 * KSPUtil.dateTimeFormatter.Hour)
                return "yesterday";

            if (delta < 424 * KSPUtil.dateTimeFormatter.Day)
                return days + " days ago";

            return years <= 1 ? "one year ago" : years + " years ago";
        }

        public static String TransactionReasonToString(TransactionReasons reason)
        {
            if (reason == TransactionReasons.Any)
            {
                return "Any";
            }
            else if (reason == TransactionReasons.None)
            {
                return "None";
            }
            else if (reason == TransactionReasons.ContractAdvance)
            {
                return "ContractAdvance";
            }
            else if (reason == TransactionReasons.ContractReward)
            {
                return "ContractReward";
            }
            else if (reason == TransactionReasons.ContractPenalty)
            {
                return "ContractPenalty";
            }
            else if (reason == TransactionReasons.VesselRollout)
            {
                return "VesselRollout";
            }
            else if (reason == TransactionReasons.VesselRecovery)
            {
                return "VesselRecovery";
            }
            else if (reason == TransactionReasons.VesselLoss)
            {
                return "VesselLoss";
            }
            else if (reason == TransactionReasons.Vessels)
            {
                return "Vessels";
            }
            else if (reason == TransactionReasons.StrategyInput)
            {
                return "StrategyInput";
            }
            else if (reason == TransactionReasons.StrategyOutput)
            {
                return "StrategyOutput";
            }
            else if (reason == TransactionReasons.StrategySetup)
            {
                return "StrategySetup";
            }
            else if (reason == TransactionReasons.Strategies)
            {
                return "Strategies";
            }
            else if (reason == TransactionReasons.ScienceTransmission)
            {
                return "ScienceTransmission";
            }
            else if (reason == TransactionReasons.StructureRepair)
            {
                return "StructureRepair";
            }
            else if (reason == TransactionReasons.StructureCollapse)
            {
                return "StructureCollapse";
            }
            else if (reason == TransactionReasons.StructureConstruction)
            {
                return "StructureConstruction";
            }
            else if (reason == TransactionReasons.Structures)
            {
                return "Structures";
            }
            else if (reason == TransactionReasons.RnDTechResearch)
            {
                return "RnDTechResearch";
            }
            else if (reason == TransactionReasons.RnDPartPurchase)
            {
                return "RnDPartPurchase";
            }
            else if (reason == TransactionReasons.RnDs)
            {
                return "RnDs";
            }
            else if (reason == TransactionReasons.Cheating)
            {
                return "Cheating";
            }
            else if (reason == TransactionReasons.CrewRecruited)
            {
                return "CrewRecruited";
            }
            else if (reason == TransactionReasons.ContractDecline)
            {
                return "ContractDecline";
            }
            else if (reason == TransactionReasons.Contracts)
            {
                return "Contracts";
            }
            else if (reason == TransactionReasons.Progression)
            {
                return "Progression";
            }
            else
            {
                return null;
            }
        }
    }
}
