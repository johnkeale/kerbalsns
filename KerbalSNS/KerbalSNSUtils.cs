using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSNS
{
    class KerbalSNSUtils
    {
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
            return vesselType == VesselTypeAny
                || (vessel.vesselType == (VesselType)vesselType);
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

                Vessel.Situations situation = Vessel.Situations.PRELAUNCH;
                if (vesselSituation.Equals("Landed"))
                {
                    situation = Vessel.Situations.LANDED;
                }
                else if (vesselSituation.Equals("Splashed"))
                {
                    situation = Vessel.Situations.SPLASHED;
                }
                else if (vesselSituation.Equals("Prelaunch"))
                {
                    situation = Vessel.Situations.PRELAUNCH;
                }
                else if (vesselSituation.Equals("Flying"))
                {
                    situation = Vessel.Situations.FLYING;
                }
                else if (vesselSituation.Equals("SubOrbital"))
                {
                    situation = Vessel.Situations.SUB_ORBITAL;
                }
                else if (vesselSituation.Equals("Orbiting"))
                {
                    situation = Vessel.Situations.ORBITING;
                }
                else if (vesselSituation.Equals("Escaping"))
                {
                    situation = Vessel.Situations.ESCAPING;
                }
                else if (vesselSituation.Equals("Docked"))
                {
                    situation = Vessel.Situations.DOCKED;
                }

                return vessel.mainBody.Equals(body) && vessel.situation == situation;
            }
            else
            {
                return false;
            }
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
    }
}
