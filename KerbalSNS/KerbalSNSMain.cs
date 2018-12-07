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
        private List<KerbBaseStory> baseStoryList;
        private List<KerbBaseShout> baseShoutList;
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

            baseStoryList = new List<KerbBaseStory>();
            baseShoutList = new List<KerbBaseShout>();

            ConfigNode rootStoryNode = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/KerbalSNS/baseStoriesList.cfg");
            ConfigNode storyListNode = rootStoryNode.GetNode(KerbBaseStory.NODE_NAME_PLURAL);

            ConfigNode[] storyArray = storyListNode.GetNodes();
            foreach (ConfigNode storyNode in storyArray)
            {
                KerbBaseStory story = new KerbBaseStory();
                story.LoadFromConfigNode(storyNode);
                baseStoryList.Add(story);
            }

            ConfigNode rootShoutNode = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/KerbalSNS/baseShoutsList.cfg");
            ConfigNode shoutListNode = rootShoutNode.GetNode(KerbBaseShout.NODE_NAME_PLURAL);

            ConfigNode[] shoutArray = shoutListNode.GetNodes();
            foreach (ConfigNode shoutNode in shoutArray)
            {
                KerbBaseShout shout = new KerbBaseShout();
                shout.LoadFromConfigNode(shoutNode);
                baseShoutList.Add(shout);
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
        
        enum BrowserType { Stories, Shouts };

        // TODO learn to use UIStyle
        private void spawnBrowserDialog(BrowserType browserType)
        {
            String dialogName = null;
            if (browserType == BrowserType.Stories)
            {
                dialogName = "browseStoriesDialog";
            }
            else if (browserType == BrowserType.Shouts)
            {
                dialogName = "browseShoutsDialog";
            }

            this.shouldSpawnBrowserDialog = false;

            List<DialogGUIBase> dialogElementsList = new List<DialogGUIBase>();

            dialogElementsList.Add(new DialogGUIHorizontalLayout(
                new DialogGUIBase[] {
                    new DialogGUIButton(
                        "KSC's Random Stories",
                        delegate {
                            spawnBrowserDialog(BrowserType.Stories);
                        },
                        () => (browserType == BrowserType.Shouts),
                        true
                    ),
                    new DialogGUIButton(
                        "Kerbshouts!",
                        delegate {
                            spawnBrowserDialog(BrowserType.Shouts);
                        },
                        () => (browserType == BrowserType.Stories),
                        true
                    ),
                    new DialogGUIButton(
                        "+",
                        delegate { },
                        false
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

            String dummyUrl = null;
            if (browserType == BrowserType.Stories)
            {
                dummyUrl = "https://www.ksc.org/stories.php";
            }
            else if (browserType == BrowserType.Shouts)
            {
                dummyUrl = "https://kerbshouts.com/feed";
            }

            dialogElementsList.Add(new DialogGUIHorizontalLayout(
                TextAnchor.MiddleLeft,
                new DialogGUIBase[] {
                    new DialogGUIButton(
                        "<-",
                        delegate { },
                        false
                    ),
                    new DialogGUIButton(
                        "->",
                        delegate { },
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
                    /*new DialogGUITextInput(
                        dummyUrl,
                        "",
                        false,
                        50,
                        delegate (String s) {
							s = dummyUrl;
                            return dummyUrl;
                        },
                        25
                    ),*/
                    new DialogGUIButton(
                        "Go",
                        delegate {

                        },
                        false
                    ),
                }
            ));

            List<DialogGUIHorizontalLayout> scrollElementsList = null;
            if (browserType == BrowserType.Stories)
            {
                scrollElementsList = buildStoriesScrollElementsList();
            }
            else if (browserType == BrowserType.Shouts)
            {
                scrollElementsList = buildShoutsScrollElementsList();
            }

            float scrollElementsHeight = 0;
            for (int i = 0; i < scrollElementsList.Count; i++)
            {
                scrollElementsHeight += scrollElementsList[i].height;
            }
            while (scrollElementsHeight < 450)
            {
                DialogGUIHorizontalLayout spacer = new DialogGUIHorizontalLayout(
                    TextAnchor.MiddleCenter,
                    new DialogGUIBase[] {
                        new DialogGUILabel("", 320, 25)
                    }
                );
                scrollElementsList.Add(spacer);
                scrollElementsHeight += 25;
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
                    "",
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
                        new Vector2(58, 36),
                        new Vector2(0, 0),
                        Color.white,
                        GameDatabase.Instance.GetTexture(HighLogic.CurrentGame.flagURL, false)
                    ),
                    new DialogGUIHorizontalLayout(
                        TextAnchor.MiddleCenter,
                        new DialogGUIBase[] {
                            new DialogGUIFlexibleSpace(),
                            new DialogGUILabel("<size=28>Kerbal Space Center</size>", true, true),
                            new DialogGUIFlexibleSpace(),
                        }
                    )
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
                    // TODO maybe add some pictures?
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

        private List<DialogGUIHorizontalLayout> buildShoutsScrollElementsList()
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
                    new DialogGUILabel("<size=24>Kerbshouts!</size>", true, true),
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

            List<KerbShout> shoutList = KerbalSNSScenario.Instance.GetShoutList; // TODO fix bad name
            shoutList = updateShoutsIfNeeded(shoutList);
            shoutList = shoutList.OrderByDescending(s => s.postedTime).ToList();

            scrollElementsList.Add(new DialogGUIHorizontalLayout(
                TextAnchor.MiddleCenter,
                new DialogGUIBase[] {
                    new DialogGUILabel(
                        "--------------------------------------------------------------------------------",
                        320,
                        25)
                }
            ));

            String enteredShout = "";
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
                                    "KerbalSNS/kerbal1",
                                    false)
                            )
                        }
                    ),
                    new DialogGUITextInput(
                        enteredShout,
                        "<color=#8B907D>What are you thinking?</color>",
                        false,
                        200,
                        delegate (String s) {
                            enteredShout = s;
                            // TODO block key press
                            return s;
                        },
                        25
                    ),
                    new DialogGUIButton(
                        "Shout!",
                        delegate {
                            KerbBaseShout baseShout = new KerbBaseShout();

                            baseShout.name = "TODO";
                            baseShout.repLevel = KerbBaseShout.RepLevel.Any;
                            baseShout.type = KerbBaseShout.ShoutType.Random;
                            baseShout.text = enteredShout;

                            String postedBy = randomVesselCrewKerbalName(FlightGlobals.ActiveVessel);
                            if (postedBy != null)
                            {
                                baseShout.poster = KerbBaseShout.ShoutPoster.KSCEmployee;

                                postedBy = postedBy + " @KSC_" + makeLikeUsername(postedBy);
                                // TODO check if already posted before so that usernames will be consistent
                            }
                            else
                            {
                                baseShout.poster = KerbBaseShout.ShoutPoster.KSC;
                                postedBy = "KSC_Official @KSC_Official";
                            }

                            KerbShout shout = createShout(baseShout, postedBy);
                            KerbalSNSScenario.Instance.RegisterShout(shout);

                            spawnBrowserDialog(BrowserType.Shouts);
                        },
                        true
                    ),
                }
            ));
            foreach (KerbShout shout in shoutList)
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
                                    shout.postedBy
                                    + " " + getRelativeTime(shout.postedTime),
                                    true,
                                    true),
                                new DialogGUILabel(shout.postedShout, true, true)
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
            PopupDialog.DismissPopup("browseShoutsDialog");
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
            KerbBaseStory baseStory = baseStoryList[mizer.Next(baseStoryList.Count)];

            Vessel vessel = getViableVessel(baseStory);
            if (vessel == null)
            {
                Debug.Log("No kerbals viable for this story");
                return;
            }

            List<ProtoCrewMember> kerbalList = getViableKerbals(baseStory, vessel);

            KerbStory story = createStory(baseStory, vessel, kerbalList);
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

        private Vessel getViableVessel(KerbBaseStory story)
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

        private List<ProtoCrewMember> getViableKerbals(KerbBaseStory story, Vessel vessel)
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

        private KerbStory createStory(KerbBaseStory baseStory, Vessel vessel, List<ProtoCrewMember> kerbalList)
        {
            KerbStory story = new KerbStory(baseStory);

            story.postedId = "TODO";

            story.postedOnVessel = vessel.GetDisplayName();
            story.postedTime = Planetarium.GetUniversalTime();

            story.postedStoryText = baseStory.text;

            String vesselType = (vessel.vesselType == VesselType.Base) ?
                "base" : "station";
            story.postedStoryText = story.postedStoryText.Replace("%v", vesselType);

            int kerbalIndex = 1;
            foreach (ProtoCrewMember kerbal in kerbalList)
            {
                story.postedStoryText = 
                    story.postedStoryText.Replace("%k" + kerbalIndex, CrewGenerator.RemoveLastName(kerbal.name));
                kerbalIndex++;
            }

            return story;
        }
        
        private List<KerbShout> updateShoutsIfNeeded(List<KerbShout> shoutList)
        {
            double now = Planetarium.GetUniversalTime();
            List<KerbShout> updatedShoutList = 
                purgeOldShouts(shoutList, now, KSPUtil.dateTimeFormatter.Hour);

            if (updatedShoutList.Count == 0 || updatedShoutList.Count < KerbalSNSSettings.MaxNumOfShouts)
            {
                int neededShoutCount = KerbalSNSSettings.MaxNumOfShouts - updatedShoutList.Count;
                int repLevelShoutCount = (int)Math.Ceiling(neededShoutCount * 0.6); // 60% of the shouts are repLevel shouts
                
                List<KerbShout> repLevelShoutList = createRepLevelShouts(repLevelShoutCount, now);
                foreach (KerbShout shout in repLevelShoutList)
                {
                    updatedShoutList.Add(shout);
                }

                List<KerbShout> otherShoutList = createNonRepLevelShouts(neededShoutCount - repLevelShoutCount, now);
                foreach (KerbShout shout in otherShoutList)
                {
                    updatedShoutList.Add(shout);
                }
            }

            return updatedShoutList;
        }

        private List<KerbShout> createRepLevelShouts(int count, double baseTime)
        {
            List<KerbBaseShout> repLevelBaseShoutList =
                baseShoutList.Where(
                    x => (
                        x.type == KerbBaseShout.ShoutType.RepLevel 
                        && x.repLevel == getCurrentRepLevel()
                    )
                ).ToList();
            List<KerbShout> shoutList = new List<KerbShout>();

            for (int i = 0; i < count; i++)
            {
                KerbBaseShout baseShout = 
                    repLevelBaseShoutList[mizer.Next(repLevelBaseShoutList.Count)];

                String postedBy = buildShoutPostedBy(baseShout);

                KerbShout shout = createShout(baseShout, postedBy);
                shout.postedTime = baseTime - mizer.Next(KSPUtil.dateTimeFormatter.Hour) + 1; // set time to random time in most recent hour

                shoutList.Add(shout);
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }

            return shoutList;
        }

        private List<KerbShout> createNonRepLevelShouts(int count, double baseTime)
        {
            List<KerbBaseShout> nonRepLevelBaseShoutList =
                baseShoutList.Where(
                    x => (
                        x.type != KerbBaseShout.ShoutType.RepLevel
                    )
                ).ToList();
            List<KerbShout> shoutList = new List<KerbShout>();

            for (int i = 0; i < count; i++)
            {
                KerbBaseShout baseShout =
                    nonRepLevelBaseShoutList[mizer.Next(nonRepLevelBaseShoutList.Count)];

                String postedBy = buildShoutPostedBy(baseShout);

                KerbShout shout = createShout(baseShout, postedBy);
                shout.postedTime = baseTime - mizer.Next(KSPUtil.dateTimeFormatter.Hour) + 1; // set time to random time in most recent hour

                shoutList.Add(shout);
                KerbalSNSScenario.Instance.RegisterShout(shout);
            }

            return shoutList;
        }

        private List<KerbShout> createRandomShouts(int count, double baseTime)
        {
            List<KerbShout> shoutList = new List<KerbShout>();

            for (int i = 0; i < count; i++)
            {
                KerbBaseShout baseShout = baseShoutList[mizer.Next(baseShoutList.Count)];

                String postedBy = buildShoutPostedBy(baseShout);
                
                KerbShout shout = createShout(baseShout, postedBy);
                shout.postedTime = baseTime - mizer.Next(KSPUtil.dateTimeFormatter.Hour) + 1; // set time to random time in most recent hour

                shoutList.Add(shout);
                KerbalSNSScenario.Instance.RegisterShout(shout);
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

        private KerbShout createShout(KerbBaseShout baseShout, String postedBy)
        {
            KerbShout shout = new KerbShout(baseShout);

            shout.postedId = "TODO";

            shout.postedBy = 
                Regex.Replace(postedBy, "@([\\w]+)", "<color=#CBF856><u>@$1</u></color>", RegexOptions.IgnoreCase);
            shout.postedTime = Planetarium.GetUniversalTime();

            shout.postedShout = 
                Regex.Replace(baseShout.text, "#([\\w]+)", "<color=#29E667><u>#$1</u></color>", RegexOptions.IgnoreCase);
            shout.postedShout =
                Regex.Replace(shout.postedShout, "@([\\w]+)", "<color=#6F8E2F><u>@$1</u></color>", RegexOptions.IgnoreCase);

            // TODO add some formatting if needed

            return shout;
        }

        private String buildShoutPostedBy(KerbBaseShout baseShout)
        {
            String postedBy = null;
            if (baseShout.poster == KerbBaseShout.ShoutPoster.Any
                || baseShout.poster == KerbBaseShout.ShoutPoster.Citizen
                || baseShout.poster == KerbBaseShout.ShoutPoster.Unknown)
            {
                postedBy = randomLayKerbalName();
                postedBy = postedBy + " @" + makeLikeUsername(postedBy);
            }
            if (baseShout.poster == KerbBaseShout.ShoutPoster.VesselCrew)
            {
                postedBy = randomActiveCrewKerbalName();
                postedBy = postedBy + " @" + makeLikeUsername(postedBy);
            }
            if (baseShout.poster == KerbBaseShout.ShoutPoster.KSCEmployee)
            {
                postedBy = randomLayKerbalName();
                postedBy = postedBy + " @KSC_" + makeLikeUsername(postedBy);
            }
            if (baseShout.poster == KerbBaseShout.ShoutPoster.KSC)
            {
                postedBy = "KSC_Official @KSC_Official";
            }
            if (baseShout.poster == KerbBaseShout.ShoutPoster.Specific)
            {
                postedBy = baseShout.specificPoster;
            }

            return postedBy;
        }

        private String makeLikeUsername(String name)
        {
            // maybe return an already existing if one was created before

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

        private String randomLayKerbalName()
        {
            String randomName = randomKerbalName();
            while (HighLogic.CurrentGame.CrewRoster.Exists(randomName))
            {
                randomName = randomKerbalName();
            }
            return randomName;
        }

        private String randomCrewKerbalName()
        {
            // TODO add sanity checks e.g. all crew is kia or missing :(
            ProtoCrewMember kerbal = 
                HighLogic.CurrentGame.CrewRoster[mizer.Next(HighLogic.CurrentGame.CrewRoster.Count)];
            return kerbal.name;
        }

        private String randomActiveCrewKerbalName()
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

        private String randomVesselCrewKerbalName(Vessel vessel)
        {
            List<ProtoCrewMember> vesselCrewList = vessel.GetVesselCrew();
            if (vesselCrewList.Count == 0)
            {
                return null;
            }

            ProtoCrewMember kerbal = vesselCrewList[mizer.Next(vesselCrewList.Count)];
            return kerbal.name;
        }

        private String randomApplicantKerbalName()
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
