using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;
using UnityEngine.UI;
using System.Linq;

namespace KerbalSNS
{
    [KSPAddon(KSPAddon.Startup.Flight, false)] // TODO make it available everywhere?
    class KerbalSNSMain : MonoBehaviour
    {
        #region properties
        private ApplicationLauncherButton appLauncherButton;
        private bool isGamePaused;
        private bool isUIHidden;
        private bool shouldSpawnBrowserDialog;

        private System.Random mizer;

        private double lastStoryPostedTime = 0; // TODO rename
        private List<KerbStory> baseStoryList;
        private List<KerbShoutout> baseShoutoutList;
        #endregion

        #region inherited methods
        public void Awake()
        {
            Debug.Log("Awake()");
        }

        public void Start()
        {
            Debug.Log("Start()");

            this.isGamePaused = false;
            this.isUIHidden = false;
            this.shouldSpawnBrowserDialog = false;

            this.appLauncherButton = null;
            this.shouldSpawnBrowserDialog = false;

            GameEvents.onGUIApplicationLauncherReady.Add(onGUIApplicationLauncherReady);
            GameEvents.onGameSceneSwitchRequested.Add(onGameSceneSwitchRequested);
            GameEvents.onLevelWasLoaded.Add(onLevelWasLoaded);
            GameEvents.onVesselChange.Add(onVesselChange);
            GameEvents.onHideUI.Add(onHideUI);
            GameEvents.onShowUI.Add(onShowUI);
            GameEvents.onGamePause.Add(onGamePause);
            GameEvents.onGameUnpause.Add(onGameUnpause);

            mizer = new System.Random();
            this.lastStoryPostedTime = Planetarium.GetUniversalTime();

            baseStoryList = new List<KerbStory>();
            baseShoutoutList = new List<KerbShoutout>();

            ConfigNode rootStoryNode = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/KerbalSNS/baseStoriesList.cfg");
            ConfigNode storyListNode = rootStoryNode.GetNode("KERBSTORIES");

            ConfigNode[] storyArray = storyListNode.GetNodes();
            foreach (ConfigNode storyNode in storyArray)
            {
                KerbStory story = new KerbStory();
                story.LoadFromConfigNode(storyNode);
                baseStoryList.Add(story);
            }

            ConfigNode rootShoutoutNode = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/KerbalSNS/baseShoutoutsList.cfg");
            ConfigNode shoutoutListNode = rootShoutoutNode.GetNode("KERBSHOUTOUTS");

            ConfigNode[] shoutoutArray = shoutoutListNode.GetNodes();
            foreach (ConfigNode shoutoutNode in shoutoutArray)
            {
                KerbShoutout shoutout = new KerbShoutout();
                shoutout.LoadFromConfigNode(shoutoutNode);
                baseShoutoutList.Add(shoutout);
            }
            /*
            double time = Planetarium.GetUniversalTime();

            int years = ((int)time) / KSPUtil.dateTimeFormatter.Year;

            int remainder = ((int)time) % KSPUtil.dateTimeFormatter.Year;
            int days = remainder / KSPUtil.dateTimeFormatter.Day;

            remainder = ((int)time) % KSPUtil.dateTimeFormatter.Day;
            int hours = remainder / KSPUtil.dateTimeFormatter.Hour;

            remainder = ((int)time) % KSPUtil.dateTimeFormatter.Hour;
            int minutes = remainder / KSPUtil.dateTimeFormatter.Minute;

            int seconds = remainder % KSPUtil.dateTimeFormatter.Minute;
            
            Debug.Log("time: " + time);
            Debug.Log("years: " + years);
            Debug.Log("days: " + days);
            Debug.Log("hours: " + hours);
            Debug.Log("minutes: " + minutes);
            Debug.Log("seconds: " + seconds);

            Debug.Log("PrintDate: " + KSPUtil.PrintDate(time, true));
            Debug.Log("PrintDateCompact: " + KSPUtil.PrintDateCompact(time, true));
            Debug.Log("PrintDateDelta: " + KSPUtil.PrintDateDelta(time, true));
            Debug.Log("PrintDateDeltaCompact: " + KSPUtil.PrintDateDeltaCompact(time, true, false));
            Debug.Log("PrintDateNew: " + KSPUtil.PrintDateNew(time, true));
            
            Debug.Log("Year: " + KSPUtil.dateTimeFormatter.Year);
            Debug.Log("Day: " + KSPUtil.dateTimeFormatter.Day);
            Debug.Log("Hour: " + KSPUtil.dateTimeFormatter.Hour);
            Debug.Log("Minute: " + KSPUtil.dateTimeFormatter.Minute);
            */
        }

        public void OnDestroy()
        {
            Debug.Log("OnDestroy()");

            GameEvents.onGUIApplicationLauncherReady.Remove(onGUIApplicationLauncherReady);
            GameEvents.onGameSceneSwitchRequested.Remove(onGameSceneSwitchRequested);
            GameEvents.onLevelWasLoaded.Remove(onLevelWasLoaded);
            GameEvents.onVesselChange.Remove(onVesselChange);
            GameEvents.onHideUI.Remove(onHideUI);
            GameEvents.onShowUI.Remove(onShowUI);
            GameEvents.onGamePause.Remove(onGamePause);
            GameEvents.onGameUnpause.Remove(onGameUnpause);

            destroyLauncher();
        }

        public void FixedUpdate()
        {
            //Debug.Log("FixedUpdate()");

            int minTimeBetweenStories =
                (KerbalSNSSettings.MinStoryIntervalHours * 60 * 60)
                + (KerbalSNSSettings.MinStoryIntervalMinutes * 60)
                + KerbalSNSSettings.MinStoryIntervalSeconds;
            if (minTimeBetweenStories <= 0)
            {
                minTimeBetweenStories = 10; // 600
            }

            if (this.lastStoryPostedTime + minTimeBetweenStories < Planetarium.GetUniversalTime())
            {
                this.lastStoryPostedTime = Planetarium.GetUniversalTime();
				
                double postStoryChance = mizer.Next(100) + 1;
                if (postStoryChance <= KerbalSNSSettings.StoryChance)
                {
                    postStory(); // TODO rename
                }
            }
        }
        #endregion

        #region GUI methods
        public void OnGUI()
        {
            if (this.isGamePaused || isUIHidden)
            {
                return;
            }
            if (!this.shouldSpawnBrowserDialog)
            {
                return;
            }
            // XXX
            if (this.shouldSpawnBrowserDialog)
            {
                spawnBrowserDialog(BrowserType.Stories);
            }
        }
        
        enum BrowserType { Stories, Shoutouts };

        private void spawnBrowserDialog(BrowserType browserType)
        {
            String dialogName = null;
            if (browserType == BrowserType.Stories)
            {
                dialogName = "browseStoriesDialog";
            }
            else if (browserType == BrowserType.Shoutouts)
            {
                dialogName = "browseShoutoutsDialog";
            }

            this.shouldSpawnBrowserDialog = false;

            List<DialogGUIBase> dialogElementsList = new List<DialogGUIBase>();

            if (browserType == BrowserType.Stories)
            {
                dialogElementsList.Add(new DialogGUIHorizontalLayout(
                    new DialogGUIBase[] {
                        new DialogGUIButton(
                            "Browse Stories",
                            delegate { },
                            () => false,
                            true
                        ),
                        new DialogGUIButton(
                            "Browse Shoutouts",
                            delegate {
                                spawnBrowserDialog(BrowserType.Shoutouts);
                            },
                            true
                        ),
                        new DialogGUIFlexibleSpace(),
                        new DialogGUIButton(
                            "Close",
                            delegate {
                                this.appLauncherButton.SetFalse();
                            },
                            true
                        )
                    }
                ));
            }
            else if (browserType == BrowserType.Shoutouts)
            {
                dialogElementsList.Add(new DialogGUIHorizontalLayout(
                    new DialogGUIBase[] {
                        new DialogGUIButton(
                            "Browse Stories",
                            delegate {
                                spawnBrowserDialog(BrowserType.Stories);
                            },
                            true
                        ),
                        new DialogGUIButton(
                            "Browse Shoutouts",
                            delegate { },
                            () => false,
                            true
                        ),
                        new DialogGUIFlexibleSpace(),
                        new DialogGUIButton(
                            "Close",
                            delegate {
                                this.appLauncherButton.SetFalse();
                            },
                            true
                        )
                    }
                ));
            }

            String dummyUrl = null;
            if (browserType == BrowserType.Stories)
            {
                dummyUrl = "https://www.ksc.org/stories.php";
            }
            else if (browserType == BrowserType.Shoutouts)
            {
                dummyUrl = "https://kerbshouts.com/feed";
            }

            dialogElementsList.Add(new DialogGUIHorizontalLayout(
                TextAnchor.MiddleLeft,
                new DialogGUIBase[] {
                    new DialogGUIButton(
                        "<-",
                        delegate { },
                        () => false,
                        false
                    ),
                    new DialogGUIButton(
                        "->",
                        delegate { },
                        () => false,
                        false
                    ),
                    new DialogGUIButton(
                        "Refresh",
                        delegate {
                            spawnBrowserDialog(browserType);
                        },
                        true
                    ),
                    new DialogGUILabel(dummyUrl, true, false),
                }
            ));

            List<DialogGUIHorizontalLayout> scrollElementsList = null;
            if (browserType == BrowserType.Stories)
            {
                scrollElementsList = buildKerbStoriesScrollElementsList();
            }
            else if (browserType == BrowserType.Shoutouts)
            {
                scrollElementsList = buildKerbShoutoutsScrollElementsList();
            }

            float scrollElementsHeight = 0;
            for (int i = 0; i < scrollElementsList.Count; i++)
            {
                scrollElementsHeight += scrollElementsList[i].height;
            }

            if (scrollElementsHeight < 450)
            {
                float neededHeight = 450 - scrollElementsHeight;
                DialogGUIHorizontalLayout spacer = new DialogGUIHorizontalLayout(
                    true,
                    false,
                    4,
                    new RectOffset(),
                    TextAnchor.MiddleCenter,
                    new DialogGUIBase[] {
                        new DialogGUILabel("", 320, neededHeight)
                    }
                );
                scrollElementsList.Add(spacer);
            }

            DialogGUIBase[] scrollArray = new DialogGUIBase[scrollElementsList.Count + 1];

            scrollArray[0] = new DialogGUIContentSizer(
                ContentSizeFitter.FitMode.Unconstrained,
                ContentSizeFitter.FitMode.PreferredSize,
                true
            );

            for (int i = 0; i < scrollElementsList.Count; i++)
            {
                scrollArray[i + 1] = scrollElementsList[i];
            }

            dialogElementsList.Add(new DialogGUIScrollList(
                Vector2.one,
                false,
                true,
                new DialogGUIVerticalLayout(
                    10,
                    100,
                    4,
                    new RectOffset(6, 24, 10, 10),
                    TextAnchor.MiddleLeft,
                    scrollArray
                )
            ));

            dialogElementsList.Add(new DialogGUISpace(4));

            PopupDialog.SpawnPopupDialog(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new MultiOptionDialog(
                    dialogName,
                    "",
                    "Browse Stories & Shoutouts",
                    UISkinManager.defaultSkin,
                    new Rect(0.5f, 0.5f, 340, 640),
                    dialogElementsList.ToArray()
                ),
                false,
                UISkinManager.defaultSkin
            );
        }
        
        private List<DialogGUIHorizontalLayout> buildKerbStoriesScrollElementsList()
        {
            List<DialogGUIHorizontalLayout> scrollElementsList = new List<DialogGUIHorizontalLayout>();

            List<KerbStory> postedStoriesList = KerbalSNSScenario.Instance.GetStoryList; // TODO fix bad name
            postedStoriesList = postedStoriesList.OrderByDescending(s => s.postedTime).ToList();

            if (postedStoriesList.Count > 0) {
				foreach (KerbStory story in postedStoriesList)
                {
                    scrollElementsList.Add(new DialogGUIHorizontalLayout(
                        true,
                        false,
                        4,
                        new RectOffset(),
                        TextAnchor.MiddleCenter,
                        new DialogGUIBase[] {
                            new DialogGUILabel(
                            "-------------------------------------------------",
                                320,
                                25)
                        }
                    ));
                    scrollElementsList.Add(new DialogGUIHorizontalLayout(
						true,
						false,
						4,
						new RectOffset(),
						TextAnchor.MiddleCenter,
						new DialogGUIBase[] {
							new DialogGUILabel(
                                "Random story on " + story.postedOnVessel
                                + " " + getRelativeTime(story.postedTime),
                                true,
                                true)
						}
					));
					scrollElementsList.Add(new DialogGUIHorizontalLayout(
						true,
						false,
						4,
						new RectOffset(),
						TextAnchor.MiddleCenter,
						new DialogGUIBase[] {
							new DialogGUILabel(story.postedStoryText, true, true)
						}
					));
				}
			} else {
				scrollElementsList.Add(new DialogGUIHorizontalLayout(
					true,
					false,
					4,
					new RectOffset(),
					TextAnchor.MiddleCenter,
					new DialogGUIBase[] {
						new DialogGUILabel("No stories yet.", 320, 25)
					}
				));
			}

            return scrollElementsList;
        }

        private List<DialogGUIHorizontalLayout> buildKerbShoutoutsScrollElementsList()
        {
            List<DialogGUIHorizontalLayout> scrollElementsList = new List<DialogGUIHorizontalLayout>();
            
            List<KerbShoutout> shoutoutList = KerbalSNSScenario.Instance.GetShoutoutList; // TODO fix bad name
            shoutoutList = shoutoutList.OrderByDescending(s => s.postedTime).ToList();

            updateShoutoutsIfNeeded(shoutoutList);
            shoutoutList = shoutoutList.OrderByDescending(s => s.postedTime).ToList();

            foreach (KerbShoutout shoutout in shoutoutList)
			{
                scrollElementsList.Add(new DialogGUIHorizontalLayout(
                    true,
                    false,
                    4,
                    new RectOffset(),
                    TextAnchor.MiddleCenter,
                    new DialogGUIBase[] {
                        new DialogGUILabel(
                            "-------------------------------------------------",
                            320,
                            25)
                    }
                ));
                
                scrollElementsList.Add(new DialogGUIHorizontalLayout(
					true,
					false,
					4,
					new RectOffset(),
					TextAnchor.MiddleCenter,
					new DialogGUIBase[] {
                        new DialogGUIButton(
                            "o",
                            delegate { },
                            () => false,
                            false
                        ),
                        new DialogGUIVerticalLayout(
                            10,
                            25,
                            4,
                            new RectOffset(6, 24, 10, 10),
                            TextAnchor.MiddleCenter,
                            new DialogGUIBase[] {
                                new DialogGUILabel(
                                    shoutout.postedBy
                                    + " @" + shoutout.postedBy
                                    + " " + getRelativeTime(shoutout.postedTime),
                                    true,
                                    true),
                                new DialogGUILabel(shoutout.shoutout, true, true)
                            }
                        )
                    }
				));
            }
            
            return scrollElementsList;
        }
        
        #endregion

        #region GameEvents callback methods
        public void onGUIApplicationLauncherReady()
        {
            createLauncher();
        }

        public void onGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> ev)
        {
            if (this.appLauncherButton != null)
            {
                this.appLauncherButton.SetFalse();
            }
            this.shouldSpawnBrowserDialog = false;
        }

        public void onLevelWasLoaded(GameScenes scene)
        {
            this.isGamePaused = false;
        }

        public void onVesselChange(Vessel vessel)
        {
            // TODO
        }

        public void onHideUI()
        {
            this.isUIHidden = true;
        }

        public void onShowUI()
        {
            this.isUIHidden = false;
        }

        public void onGamePause()
        {
            this.isGamePaused = true;
        }

        public void onGameUnpause()
        {
            this.isGamePaused = false;
        }

        public void onAppTrue()
        {
            this.shouldSpawnBrowserDialog = true;
        }

        public void onAppFalse()
        {
            this.shouldSpawnBrowserDialog = false;
            PopupDialog.DismissPopup("browseStoriesDialog");
            PopupDialog.DismissPopup("browseShoutoutsDialog");
        }

        private void createLauncher()
        {
            if (this.appLauncherButton == null)
            {
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(
                    onAppTrue,
                    onAppFalse,
                    null,
                    null,
                    null,
                    null,
                    //ApplicationLauncher.AppScenes.SPACECENTER |
                    //ApplicationLauncher.AppScenes.TRACKSTATION |
                    //ApplicationLauncher.AppScenes.FLIGHT |
                    //ApplicationLauncher.AppScenes.MAPVIEW,
                    ApplicationLauncher.AppScenes.FLIGHT,
                    GameDatabase.Instance.GetTexture("KerbalSNS/app-launcher-icon", false)
                );
            }
        }

        private void destroyLauncher()
        {
            if (appLauncherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
                appLauncherButton = null;
            }
        }
        #endregion

        #region private methods
        
        private void postStory()
        {
            KerbStory story = baseStoryList[mizer.Next(baseStoryList.Count)];

            Vessel vessel = getViableVessel(story);
            if (vessel == null)
            {
                Debug.Log("No kerbals viable for this story");
                return;
            }

            List<ProtoCrewMember> kerbalList = getViableKerbals(story, vessel);

            story = createStory(story, vessel, kerbalList);
            KerbalSNSScenario.Instance.RegisterStory(story);

            Debug.Log("Random story has happened.");

            ScreenMessages.PostScreenMessage("A random story happened at " + vessel.GetDisplayName() + "!");

            MessageSystem.Message message = new MessageSystem.Message(
                "A random story happened at " + vessel.GetDisplayName() + "!",
                story.postedStoryText,
                MessageSystemButton.MessageButtonColor.BLUE,
                MessageSystemButton.ButtonIcons.MESSAGE
                );

            MessageSystem.Instance.AddMessage(message);
        }

        private Vessel getViableVessel(KerbStory story)
        {
            List<Vessel> vesselList = new List<Vessel>();
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (!FlightGlobals.ActiveVessel.Equals(vessel)
                    && vessel.GetCrewCount() >= story.kerbalCount
                    && (vessel.vesselType == VesselType.Base
                        || vessel.vesselType == VesselType.Station))
                {
                    vesselList.Add(vessel);
                }
            }

            if (vesselList.Count == 0)
            {
                return null;
            }

            return vesselList[mizer.Next(vesselList.Count)];
        }

        private List<ProtoCrewMember> getViableKerbals(KerbStory story, Vessel vessel)
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

        // XXX
        private KerbStory createStory(KerbStory baseStory, Vessel vessel, List<ProtoCrewMember> kerbalList)
        {
            KerbStory story = new KerbStory();

            story.name = baseStory.name;
            story.kerbalCount = baseStory.kerbalCount;
            story.type = baseStory.type;
            story.storyText = baseStory.storyText;

            story.postedId = "TODO";

            story.postedOnVessel = vessel.GetDisplayName();
            story.postedTime = Planetarium.GetUniversalTime();

            story.postedStoryText = baseStory.storyText;

            String vesselType = (vessel.vesselType == VesselType.Base) ?
                "base" : "station";
            story.postedStoryText = story.postedStoryText.Replace("%v", vesselType);

            int kerbalIndex = 1;
            foreach (ProtoCrewMember kerbal in kerbalList)
            {
                story.postedStoryText = story.postedStoryText.Replace("%k" + kerbalIndex, CrewGenerator.RemoveLastName(kerbal.name));
                kerbalIndex++;
            }

            return story;
        }
        
        private void updateShoutoutsIfNeeded(List<KerbShoutout> shoutoutList)
        {
            double now = Planetarium.GetUniversalTime();
            purgeOldShoutouts(shoutoutList, now, KSPUtil.dateTimeFormatter.Hour);

            if (shoutoutList.Count == 0 || shoutoutList.Count < 40) // 40 minimum tweets for now
            {
                int max = mizer.Next(16);
                for (int i = 0; i < max; i++)
                {
                    KerbShoutout baseShoutout = baseShoutoutList[mizer.Next(baseShoutoutList.Count)];

                    String randomName = randomKerbalName();
                    double time = now - mizer.Next(KSPUtil.dateTimeFormatter.Hour) + 1;

                    KerbShoutout shoutout = createShoutout(baseShoutout, randomName, time);

                    shoutoutList.Add(shoutout);
                    KerbalSNSScenario.Instance.RegisterShoutout(shoutout);
                }

                //createNewShoutouts()
            }
        }

        private void purgeOldShoutouts(List<KerbShoutout> shoutoutList, double baseTime, double deltaTime)
        {
            List<KerbShoutout> shoutoutsToRemove = new List<KerbShoutout>();

            foreach (KerbShoutout shoutout in shoutoutList)
            {
                if (baseTime - shoutout.postedTime > deltaTime)
                {
                    shoutoutsToRemove.Add(shoutout);
                }
            }

            foreach(KerbShoutout shoutout in shoutoutsToRemove)
            {
                shoutoutList.Remove(shoutout);
                KerbalSNSScenario.Instance.DeleteShoutout(shoutout);
            }
        }

        private KerbShoutout createShoutout(KerbShoutout baseShoutout, String postedBy, double postedTime)
        {
            KerbShoutout shoutout = new KerbShoutout();

            shoutout.name = baseShoutout.name;
            shoutout.repLevel = baseShoutout.repLevel;
            shoutout.poster = baseShoutout.poster;
            shoutout.type = baseShoutout.type;
            shoutout.shoutout = baseShoutout.shoutout;

            shoutout.postedId = "TODO";

            shoutout.postedBy = postedBy;
            shoutout.postedTime = postedTime;
            shoutout.postedShoutout = baseShoutout.shoutout;

            return shoutout;
        }
        
        private String randomKerbalName()
        {
            return CrewGenerator.GetRandomName((ProtoCrewMember.Gender) mizer.Next(2), mizer);
        }

        private String getRelativeTime(double time)
        {
            double now = Planetarium.GetUniversalTime();
            double delta =  now - time;

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

        #endregion

    }
}
