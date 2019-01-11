using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;
using UnityEngine.UI;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;

namespace KerbalSNS
{
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)] // XXX what's the diff to every scene
    class KerbalSNSMain : MonoBehaviour
    {
        #region properties
        private ApplicationLauncherButton appLauncherButton;
        private bool isGamePaused;
        private bool isUIHidden;
        private bool shouldSpawnBrowserDialog;

        private const int STORY_PER_PAGE = 10;
        private int numOfStoryPages;

        private System.Random mizer;

        private double lastStoryPostedTime = 0; // TODO rename

        private DialogGUITextInput shoutTextInput;
        private PopupDialog browserDialog;

        private Vector2 lastBrowserPosition;
        private const float BROWSER_WIDTH = 340;
        private const float BROWSER_HEIGHT = 640;
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
            this.numOfStoryPages = 1;

            this.appLauncherButton = null;
            this.lastBrowserPosition = new Vector2(0.5f, 0.5f);

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

            KerbStoryHelper.Instance.LoadBaseStoryList();
            KerbShoutHelper.Instance.LoadBaseShoutList();
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

            if (this.lastStoryPostedTime + minTimeBetweenStories < Planetarium.GetUniversalTime())
            {
                this.lastStoryPostedTime = Planetarium.GetUniversalTime();
                
                double postStoryChance = mizer.Next(100) + 1;
                if (postStoryChance <= KerbalSNSSettings.StoryChance)
                {
                    KerbStory story = KerbStoryHelper.Instance.GenerateRandomStory();
                    KerbalSNSScenario.Instance.RegisterStory(story);

                    if (story != null)
                    {
		                Debug.Log("Random story has happened.");
		
                        ScreenMessages.PostScreenMessage("A random story happened at " + story.postedOnVessel + "!");
		
		                MessageSystem.Message message = new MessageSystem.Message(
                            "A random story happened at " + story.postedOnVessel + "!",
		                    story.postedText,
		                    MessageSystemButton.MessageButtonColor.BLUE,
		                    MessageSystemButton.ButtonIcons.MESSAGE
		                );
		
		                MessageSystem.Instance.AddMessage(message);
                    }
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

            // FIXME scroll bar is on the middle at the start
            spawnBrowserDialog(BrowserType.Stories);
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
                            saveLastBrowserDialogPosition();
                            spawnBrowserDialog(BrowserType.Stories);
                        },
                        () => (browserType == BrowserType.Shouts),
                        true
                    ),
                    new DialogGUIButton(
                        "Kerbshouts!",
                        delegate {
                            saveLastBrowserDialogPosition();
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
                            saveLastBrowserDialogPosition();
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

            // FIXME scrolled at the middle

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

            this.browserDialog =
                PopupDialog.SpawnPopupDialog(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog(
                        dialogName,
                        "",
                        "",
                        UISkinManager.defaultSkin,
                        new Rect(this.lastBrowserPosition, new Vector2(BROWSER_WIDTH, BROWSER_HEIGHT)),
                        dialogElementsList.ToArray()
                    ),
                    false,
                    UISkinManager.defaultSkin
                );
            addShoutTextInputLocking();
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

            List<KerbStory> storyList = KerbStoryHelper.Instance.GetPostedStories();

            if (storyList.Count > 0) {
                int numOfStories = 0;
                foreach (KerbStory story in storyList)
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
                                + " " + KerbalSNSUtils.GetRelativeTime(story.postedTime),
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
                            new DialogGUILabel(story.postedText, true, true)
                        }
                    ));

                    if (numOfStories == (STORY_PER_PAGE * this.numOfStoryPages))
                    {
                        scrollElementsList.Add(new DialogGUIHorizontalLayout(
                            true,
                            true,
                            4,
                            new RectOffset(),
                            TextAnchor.MiddleCenter,
                            new DialogGUIBase[] {
                                new DialogGUIButton(
                                    "Load more stories...",
                                    delegate {
                                        this.numOfStoryPages++;
                                        saveLastBrowserDialogPosition();
                                        spawnBrowserDialog(BrowserType.Stories);
                                    },
                                    true
                                ),
                            }
                        ));
                        break;
                    }
                    else
                    {
                        numOfStories++;
                    }
                }


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
            this.shoutTextInput = 
                new DialogGUITextInput(
                    enteredShout,
                    "<color=#8B907D>What are you thinking?</color>",
                    false,
                    200,
                    delegate (String s)
                    {
                        enteredShout = s;
                        return s;
                    },
                    25
                );

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
                    this.shoutTextInput,
                    new DialogGUIButton(
                        "Shout!",
                        delegate {
                            KerbShout shout = KerbShoutHelper.Instance.GenerateShout(enteredShout);
                            KerbalSNSScenario.Instance.RegisterShout(shout);

                            saveLastBrowserDialogPosition();
                            spawnBrowserDialog(BrowserType.Shouts);
                        },
                        true
                    ),
                }
            ));

            List<KerbShout> shoutList = KerbShoutHelper.Instance.GetPostedShouts();

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
                        // where the shout is
                        new DialogGUIVerticalLayout(
                            10,
                            25,
                            4,
                            new RectOffset(6, 24, 10, 10),
                            TextAnchor.MiddleCenter,
                            new DialogGUIBase[] {
                                new DialogGUILabel(
                                    shout.postedBy.fullname 
                                    + " <color=#CBF856><u>" + shout.postedBy.username + "</u></color>"
                                    + " " + KerbalSNSUtils.GetRelativeTime(shout.postedTime),
                                    true,
                                    true),
                                new DialogGUILabel(shout.postedText, true, true)
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

        public void onGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> fromToAction)
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

            saveLastBrowserDialogPosition();
            PopupDialog.DismissPopup("browseStoriesDialog");
            PopupDialog.DismissPopup("browseShoutsDialog");

            this.numOfStoryPages = 1;
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
                    ApplicationLauncher.AppScenes.SPACECENTER
                    | ApplicationLauncher.AppScenes.TRACKSTATION
                    | ApplicationLauncher.AppScenes.FLIGHT
                    | ApplicationLauncher.AppScenes.MAPVIEW,
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

        private void saveLastBrowserDialogPosition()
        {
            if (this.browserDialog != null)
            {
                Vector3 position = this.browserDialog.RTrf.position;
                this.lastBrowserPosition =
                    new Vector2(position.x / Screen.width + 0.5f, position.y / Screen.height + 0.5f);
            }
        }

        // https://forum.kerbalspaceprogram.com/index.php?/topic/149324-popupdialog-and-the-dialoggui-classes/&do=findComment&comment=3213159
        private void addShoutTextInputLocking()
        {
            if (this.shoutTextInput == null)
            {
                return;
            }

            TMP_InputField tmp_input = this.shoutTextInput.uiItem.GetComponent<TMP_InputField>();
            if (tmp_input != null)
            {
                tmp_input.onSelect.AddListener(new UnityEngine.Events.UnityAction<String>(OnShoutTextInputSelect));
                tmp_input.onDeselect.AddListener(new UnityEngine.Events.UnityAction<String>(OnShoutTextInputDeselect));
            }
        }

        // https://forum.kerbalspaceprogram.com/index.php?/topic/151312-preventing-keystroke-fallthrough-on-text-field-usage-between-different-modsinputlockmanager/
        private void OnShoutTextInputSelect(String s)
        {
            InputLockManager.SetControlLock(ControlTypes.KEYBOARDINPUT, "KerbalSNS");
        }

        private void OnShoutTextInputDeselect(String s)
        {
            InputLockManager.RemoveControlLock("KerbalSNS");
        }

        #endregion

    }
}
