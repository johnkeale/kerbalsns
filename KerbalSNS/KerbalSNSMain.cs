using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;
using UnityEngine.UI;
using System.Linq;
using System.Text.RegularExpressions;

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
                    postStory();
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

        // TODO learn to use UIStyle
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
                            "KSC's Random Stories",
                            delegate { },
                            () => false,
                            true
                        ),
                        new DialogGUIButton(
                            "Kerbshouts!",
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
                            "KSC's Random Stories",
                            delegate {
                                spawnBrowserDialog(BrowserType.Stories);
                            },
                            true
                        ),
                        new DialogGUIButton(
                            "Kerbshouts!",
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
                    new DialogGUILabel("  " + dummyUrl, true, false),
                }
            ));

            List<DialogGUIHorizontalLayout> scrollElementsList = null;
            if (browserType == BrowserType.Stories)
            {
                scrollElementsList = buildStoriesScrollElementsList();
            }
            else if (browserType == BrowserType.Shoutouts)
            {
                scrollElementsList = buildShoutoutsScrollElementsList();
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
                    "Kerbal SNS",
                    UISkinManager.defaultSkin,
                    new Rect(0.5f, 0.5f, 340, 640),
                    dialogElementsList.ToArray()
                ),
                false,
                UISkinManager.defaultSkin
            );
        }
        
        private List<DialogGUIHorizontalLayout> buildStoriesScrollElementsList()
        {
            List<DialogGUIHorizontalLayout> scrollElementsList = new List<DialogGUIHorizontalLayout>();

            DialogGUIHorizontalLayout header = new DialogGUIHorizontalLayout(
                TextAnchor.MiddleLeft,
                new DialogGUIBase[] {
                    new DialogGUIImage(
                        new Vector2(36, 36),
                        new Vector2(0, 0),
                        Color.white,
                        GameDatabase.Instance.GetTexture("KerbalSNS/ksp", false)
                    ),
                    new DialogGUILabel("KSC", true, true),
                    new DialogGUIFlexibleSpace(),
                }
            );
            scrollElementsList.Add(header);
            DialogGUIHorizontalLayout navBar = new DialogGUIHorizontalLayout(
                TextAnchor.MiddleCenter,
                new DialogGUIBase[] {
                    new DialogGUILabel("<color=#44E6FF><u>Missions</u></color>", true, true),
                    new DialogGUILabel("|", true, true),
                    new DialogGUILabel("<color=#44E6FF><u>Galleries</u></color>", true, true),
                    new DialogGUILabel("|", true, true),
                    new DialogGUILabel("<color=#4422EE><u><b>Stories</b></u></color>", true, true),
                    new DialogGUILabel("|", true, true),
                    new DialogGUILabel("<color=#44E6FF><u>Programs</u></color>", true, true),
                    new DialogGUILabel("|", true, true),
                    new DialogGUILabel("<color=#44E6FF><u>About</u></color>", true, true),
                }
            );
            scrollElementsList.Add(navBar);

            List<KerbStory> postedStoriesList = KerbalSNSScenario.Instance.GetStoryList; // TODO fix bad name
            postedStoriesList = postedStoriesList.OrderByDescending(s => s.postedTime).ToList();

            if (postedStoriesList.Count > 0) {
				foreach (KerbStory story in postedStoriesList)
                {
                    scrollElementsList.Add(new DialogGUIHorizontalLayout(
                        TextAnchor.MiddleCenter,
                        new DialogGUIBase[] {
                            new DialogGUILabel(
                                "--------------------------------------------------------------------------------",
                                320,
                                25)
                        }
                    ));
                    scrollElementsList.Add(new DialogGUIHorizontalLayout(
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
                        TextAnchor.MiddleCenter,
                        new DialogGUIBase[] {
                            new DialogGUILabel(
                                "",
                                true,
                                true)
                        }
                    ));
                    scrollElementsList.Add(new DialogGUIHorizontalLayout(
						TextAnchor.MiddleCenter,
						new DialogGUIBase[] {
							new DialogGUILabel(story.postedStoryText, true, true)
						}
					));
				}

                // TODO limit loaded stories

			}
            else
            {
                scrollElementsList.Add(new DialogGUIHorizontalLayout(
                    TextAnchor.MiddleCenter,
                    new DialogGUIBase[] {
                        new DialogGUILabel(
                            "--------------------------------------------------------------------------------",
                            320,
                            25)
                    }
                ));
                scrollElementsList.Add(new DialogGUIHorizontalLayout(
					TextAnchor.MiddleCenter,
					new DialogGUIBase[] {
						new DialogGUILabel("No stories yet.", 320, 25)
					}
				));

			}

            return scrollElementsList;
        }

        private List<DialogGUIHorizontalLayout> buildShoutoutsScrollElementsList()
        {
            List<DialogGUIHorizontalLayout> scrollElementsList = new List<DialogGUIHorizontalLayout>();

            DialogGUIHorizontalLayout header = new DialogGUIHorizontalLayout(
                TextAnchor.MiddleLeft,
                new DialogGUIBase[] {
                    new DialogGUIImage(
                        new Vector2(36, 36),
                        new Vector2(0, 0),
                        Color.white,
                        GameDatabase.Instance.GetTexture("KerbalSNS/shouts", false)
                    ),
                    new DialogGUILabel("Home", true, true),
                    new DialogGUIFlexibleSpace()
                }
            );
            scrollElementsList.Add(header);
            DialogGUIHorizontalLayout navBar = new DialogGUIHorizontalLayout(
                TextAnchor.MiddleCenter,
                new DialogGUIBase[] {
                    new DialogGUIFlexibleSpace(),
                    new DialogGUILabel("<color=#4422EE><u><b>Feed</b></u></color>", true, true),
                    new DialogGUIFlexibleSpace(),
                    new DialogGUILabel("<color=#44E6FF><u>Search</u></color>", true, true),
                    new DialogGUIFlexibleSpace(),
                    new DialogGUILabel("<color=#44E6FF><u>Notifs</u></color>", true, true),
                    new DialogGUIFlexibleSpace(),
                    new DialogGUILabel("<color=#44E6FF><u>Messages</u></color>", true, true),
                    new DialogGUIFlexibleSpace(),
                }
            );
            scrollElementsList.Add(navBar);

            List<KerbShoutout> shoutoutList = KerbalSNSScenario.Instance.GetShoutoutList; // TODO fix bad name
            shoutoutList = updateShoutoutsIfNeeded(shoutoutList);
            shoutoutList = shoutoutList.OrderByDescending(s => s.postedTime).ToList();

            foreach (KerbShoutout shoutout in shoutoutList)
			{
                scrollElementsList.Add(new DialogGUIHorizontalLayout(
                    TextAnchor.MiddleCenter,
                    new DialogGUIBase[] {
                        new DialogGUILabel(
                            "--------------------------------------------------------------------------------",
                            320,
                            25)
                    }
                ));
                
                scrollElementsList.Add(new DialogGUIHorizontalLayout(
					TextAnchor.MiddleCenter,
					new DialogGUIBase[] {
                        // this is supposed to be a profile image
                        new DialogGUIVerticalLayout(
                            10,
                            25,
                            4,
                            new RectOffset(2, 2, 4, 4),
                            TextAnchor.UpperCenter,
                            new DialogGUIBase[] {
                                new DialogGUIImage(
                                    new Vector2(20, 20),
                                    new Vector2(0, 0),
                                    Color.white,
                                    GameDatabase.Instance.GetTexture(
                                        "KerbalSNS/kerbal" + (mizer.Next(2) + 1),
                                        false)
                                )
                            }
                        ),
                        // where the tweet is
                        new DialogGUIVerticalLayout(
                            10,
                            25,
                            4,
                            new RectOffset(6, 24, 10, 10),
                            TextAnchor.MiddleCenter,
                            new DialogGUIBase[] {
                                new DialogGUILabel(
                                    shoutout.postedBy
                                    + " @" + makeLikeUsername(shoutout.postedBy)
                                    + " " + getRelativeTime(shoutout.postedTime),
                                    true,
                                    true),
                                new DialogGUILabel(shoutout.postedShoutout, true, true)
                            }
                        )
                    }
				));
                scrollElementsList.Add(new DialogGUIHorizontalLayout(
                    TextAnchor.MiddleCenter,
                    new DialogGUIBase[] {
                        new DialogGUIFlexibleSpace(),
                        new DialogGUILabel("<color=#44E6FF><u>Reshout</u></color>", true, true),
                        new DialogGUIFlexibleSpace(),
                        new DialogGUILabel("<color=#44E6FF><u>Respond</u></color>", true, true),
                        new DialogGUIFlexibleSpace(),
                        new DialogGUILabel("<color=#44E6FF><u>Heart</u></color>", true, true),
                        new DialogGUIFlexibleSpace(),
                        new DialogGUILabel("<color=#44E6FF><u>Share</u></color>", true, true),
                        new DialogGUIFlexibleSpace(),
                    }
                ));
            }

            // TODO limit loaded tweets (load more button)

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
        
        private List<KerbShoutout> updateShoutoutsIfNeeded(List<KerbShoutout> shoutoutList)
        {
            double now = Planetarium.GetUniversalTime();
            List<KerbShoutout> updatedShoutoutList = 
                purgeOldShoutouts(shoutoutList, now, KSPUtil.dateTimeFormatter.Hour);

            if (updatedShoutoutList.Count == 0 || updatedShoutoutList.Count < KerbalSNSSettings.MaxNumOfShoutouts)
            {
                for (int i = updatedShoutoutList.Count; i < KerbalSNSSettings.MaxNumOfShoutouts; i++)
                {
                    // TODO fetch shoutouts based on current reputation, and get random from there
                    KerbShoutout baseShoutout = baseShoutoutList[mizer.Next(baseShoutoutList.Count)];

                    String postedBy = randomKerbalName();
                    if (baseShoutout.poster == KerbShoutout.ShoutoutPoster.Any 
                        || baseShoutout.poster == KerbShoutout.ShoutoutPoster.Citizen)
                    {
                        postedBy = randomKerbalName(); // TODO add checking to see if not currently in roster
                    }
                    if (baseShoutout.poster == KerbShoutout.ShoutoutPoster.VesselCrew)
                    {
                        postedBy = randomKerbalName(); // TODO get from vesel crews
                    }
                    if (baseShoutout.poster == KerbShoutout.ShoutoutPoster.KSCEmployee)
                    {
                        postedBy = "KSC";
                    }
                    if (baseShoutout.poster == KerbShoutout.ShoutoutPoster.KSC)
                    {
                        postedBy = "KSC";
                    }
                    if (baseShoutout.poster == KerbShoutout.ShoutoutPoster.Specific)
                    {
                        postedBy = baseShoutout.specificPoster;
                    }

                    KerbShoutout shoutout = createShoutout(baseShoutout, postedBy); // TODO check if existing as an applicant or crew
                    shoutout.postedTime = now - mizer.Next(KSPUtil.dateTimeFormatter.Hour) + 1; // set time to random time in most recent hour

                    updatedShoutoutList.Add(shoutout);
                    KerbalSNSScenario.Instance.RegisterShoutout(shoutout);
                }
            }

            return updatedShoutoutList;
        }

        private List<KerbShoutout> purgeOldShoutouts(List<KerbShoutout> shoutoutList, double baseTime, double deltaTime)
        {
            List<KerbShoutout> freshShoutoutsList = new List<KerbShoutout>();

            foreach (KerbShoutout shoutout in shoutoutList)
            {
                // shoutout still new
                if (baseTime - shoutout.postedTime <= deltaTime)
                {
                    freshShoutoutsList.Add(shoutout);
                }
                else
                {
                    KerbalSNSScenario.Instance.DeleteShoutout(shoutout);
                }
            }

            return freshShoutoutsList;
        }

        private KerbShoutout createShoutout(KerbShoutout baseShoutout, String postedBy)
        {
            KerbShoutout shoutout = new KerbShoutout();

            shoutout.name = baseShoutout.name;
            shoutout.repLevel = baseShoutout.repLevel;
            shoutout.poster = baseShoutout.poster;
            shoutout.type = baseShoutout.type;
            shoutout.shoutout = baseShoutout.shoutout;

            shoutout.postedId = "TODO";

            shoutout.postedBy = postedBy;
            shoutout.postedTime = Planetarium.GetUniversalTime();

            shoutout.postedShoutout = 
                Regex.Replace(baseShoutout.shoutout, "#([\\w]+)", "<color=#29E667><u>#$1</u></color>", RegexOptions.IgnoreCase);
            
            // TODO add some formatting if needed

            return shoutout;
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
            
            r = mizer.Next(8);
            if (r < 3)
            {
                username = Regex.Replace(username, "o", "0", RegexOptions.IgnoreCase);
            }
            r = mizer.Next(8);
            if (r < 3)
            {
                username = Regex.Replace(username, "i", "1", RegexOptions.IgnoreCase);
            }
            r = mizer.Next(8);
            if (r < 3)
            {
                username = Regex.Replace(username, "l", "2", RegexOptions.IgnoreCase);
            }
            r = mizer.Next(8);
            if (r < 3)
            {
                username = Regex.Replace(username, "e", "3", RegexOptions.IgnoreCase);
            }

            r = mizer.Next(4);
            if (r < 1)
            {
                username = username + mizer.Next(1000);
            }

            return username;
        }

        private String randomKerbalName()
        {
            return CrewGenerator.GetRandomName((ProtoCrewMember.Gender) mizer.Next(2), mizer);
        }

        private String getRelativeTime(double time)
        {
            double now = Planetarium.GetUniversalTime();
            double delta =  now - time;

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

        #endregion

    }
}
