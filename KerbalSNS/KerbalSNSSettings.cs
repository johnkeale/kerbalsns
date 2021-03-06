﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSNS
{
    class KerbalSNSSettings : GameParameters.CustomParameterNode
    {
        #region game parameters
        [GameParameters.CustomIntParameterUI("Story chance", maxValue = 100, minValue = 0, stepSize = 1, toolTip = "Percentage chance of an story happening.", autoPersistance = true)]
        public int storyChance = 80;

        [GameParameters.CustomIntParameterUI("Minimum story interval seconds", maxValue = 60, minValue = 0, stepSize = 5, toolTip = "The minimum interval between stories happening, in seconds.", autoPersistance = true)]
        public int minStoryIntervalSeconds = 0;

        [GameParameters.CustomIntParameterUI("Minimum story interval minutes", maxValue = 60, minValue = 0, stepSize = 1, toolTip = "The minimum interval between stories happening, in minutes.", autoPersistance = true)]
        public int minStoryIntervalMinutes = 25;

        [GameParameters.CustomIntParameterUI("Minimum story interval hours", maxValue = 6, minValue = 0, stepSize = 1, toolTip = "The minimum interval between stories happening, in hours.", autoPersistance = true)]
        public int minStoryIntervalHours = 2;

        // TODO add day?

        [GameParameters.CustomIntParameterUI("Number of shouts", maxValue = 100, minValue = 5, stepSize = 1, toolTip = "The number of shouts to show on the browser.", autoPersistance = true)]
        public int numOfShouts = 24;

        [GameParameters.CustomIntParameterUI("RepLevel shout percentage", maxValue = 100, minValue = 0, stepSize = 1, toolTip = "The number of shouts pertaining to your rep level among all the shouts in the feed. (e.g. 60% of 24 shouts means around 14 shouts will talk about your reputation)", autoPersistance = true)]
        public int repLevelShoutPercentage = 60;

        #endregion

        #region parameter getter methods
        public static int StoryChance
        {
            get
            {
                KerbalSNSSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbalSNSSettings>();
                return settings.storyChance;
            }
        }

        public static int MinStoryIntervalSeconds
        {
            get
            {
                KerbalSNSSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbalSNSSettings>();
                return settings.minStoryIntervalSeconds;
            }
        }

        public static int MinStoryIntervalMinutes
        {
            get
            {
                KerbalSNSSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbalSNSSettings>();
                return settings.minStoryIntervalMinutes;
            }
        }

        public static int MinStoryIntervalHours
        {
            get
            {
                KerbalSNSSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbalSNSSettings>();
                return settings.minStoryIntervalHours;
            }
        }

        public static int NumOfShouts
        {
            get
            {
                KerbalSNSSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbalSNSSettings>();
                return settings.numOfShouts;
            }
        }

        public static int RepLevelShoutPercentage
        {
            get
            {
                KerbalSNSSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbalSNSSettings>();
                return settings.repLevelShoutPercentage;
            }
        }
        #endregion

        #region overriden methods
        public override string Title
        {
            get
            {
                return "General Settings";
            }
        }

        public override string DisplaySection
        {
            get
            {
                return Section;
            }
        }

        public override string Section
        {
            get
            {
                return "Kerbal SNS";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 1;
            }
        }

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }
        #endregion
    }
}
