using System;
using KSP.Localization;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace PlanetaryDiversity
{
    /// <summary>
    /// Gives the user an opportunity to set the seed of their savegame
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class SeedParams : MonoBehaviour
    {
        /// <summary>
        /// The seed for the new game
        /// </summary>
        internal static bool activeSet = false;
        public static bool Active { get; set; } = false;

        /// <summary>
        /// The seed for the new game
        /// </summary>
        public String Seed { get; set; }

        /// <summary>
        /// The main menu Instance
        /// </summary>
        private MainMenu menu { get; set; }

        /// <summary>
        /// Gets called when the mono behaviour is created and registers a callback for changing
        /// the game seed
        /// </summary>
        void Awake()
        {
            GameEvents.onGameStateCreated.Add(OnGameStateCreated);
            DontDestroyOnLoad(this);
        }

        /// <summary>
        /// Adds a hook into the MainMenu UI for editing the New Game Dialog
        /// </summary>
        void Start()
        {
            menu = FindObjectsOfType<MainMenu>().FirstOrDefault();
            menu.newGameBtn.onTap += OnNewGameBtnTap;
        }

        /// <summary>
        /// This function gets called when the user clicks the "New Game" button in the main menu
        /// </summary>
        void OnNewGameBtnTap()
        {

            activeSet = false;
            FieldInfo createGameDialog = typeof(MainMenu).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(f => f.FieldType == typeof(PopupDialog));
            if (createGameDialog == null)
                return;
            PopupDialog dialog = createGameDialog.GetValue(menu) as PopupDialog;
            if (dialog == null)
                return;
            if (dialog.dialogToDisplay == null)
                return;
            DialogGUIHorizontalLayout d1 = dialog.dialogToDisplay.Options[0] as DialogGUIHorizontalLayout;
            if (d1 == null)
                return;
            DialogGUIVerticalLayout d2 = d1.children[0] as DialogGUIVerticalLayout;
            if (d2 == null)
                return;

            Active = true;

            // Create the new layout
            DialogGUIHorizontalLayout layout = new DialogGUIHorizontalLayout(new DialogGUIBase[]
            {
                
                new DialogGUIToggle(Active, Localizer.Format("#LOC_PlanetaryDiversity_SeedParams_Seed"),(b) => Active = b, 200f),

                //new DialogGUILabel(Localizer.Format("#LOC_PlanetaryDiversity_SeedParams_Seed"), true, false),
                new DialogGUIFlexibleSpace(),
                new DialogGUITextInput(Seed ?? "", Localizer.Format("#LOC_PlanetaryDiversity_SeedParams_Placeholder"), false, 32, (s) => Seed = s, 200f, 30f)
            });
            d2.children.Insert(1, layout);
            d1.children[0] = d2;
            dialog.dialogToDisplay.Options[0] = d1;
            PopupDialog newDialog = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dialog.dialogToDisplay, false, menu.guiSkinDef.SkinDef, true, "");
            dialog.Dismiss();
            createGameDialog.SetValue(menu, newDialog);
        }

        void OnGameStateCreated(Game game)
        {
            if (Active)
            {
                Seed = Seed?.Trim();
                if (String.IsNullOrEmpty(Seed))
                    return;
                if (Int32.TryParse(Seed, out Int32 iSeed))
                    game.Seed = iSeed;
                else
                    game.Seed = Seed.GetHashCode();
                Seed = null;

                activeSet = true;
            }
            
        }
    }
}
