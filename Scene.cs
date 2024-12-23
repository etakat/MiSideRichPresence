using System.Collections.Generic;

namespace MiSideRichPresence;

public class Scene
{
    public const string DefaultIcon = "game_logo";
    public string IconId { get; }
    public string DisplayName { get; }
    public List<string> Aliases { get; }

    private Scene(string iconId, string displayName, params string[] aliases)
    {
        IconId = iconId;
        DisplayName = displayName;
        Aliases = new List<string>(aliases);
    }

    public static readonly Scene Loading = new Scene("game_logo", "Loading...", "SceneLoading");
    public static readonly Scene Aihasto = new Scene("game_logo", "Loading...", "SceneAihasto");
    public static readonly Scene MainMenu = new Scene("game_logo", "Main Menu", "SceneMenu");
    public static readonly Scene Prologue = new Scene("0_prologue", "Prologue", "Scene 1 - RealRoom");
    public static readonly Scene Chapter1 = new Scene("1_im_inside_a_game", "I'm Inside a Game?", "Scene 2 - InGame");
    public static readonly Scene Chapter2 = new Scene("2_together_at_last", "Together at Last", "Scene 3 - WeTogether", "Scene 4 - StartSecret");
    public static readonly Scene Chapter3 = new Scene("3_things_get_weird", "Things Get Weird", "Scene 5 - StartHorror");
    public static readonly Scene Chapter4 = new Scene("4_the_basement", "The Basement", "Scene 6 - BasementFirst");
    public static readonly Scene Chapter5 = new Scene("5_beyond_the_world", "Beyond the World", "Scene 7 - Backrooms");
    public static readonly Scene Chapter6 = new Scene("6_the_loop", "The Loop", "Scene 8 - ReRooms");
    public static readonly Scene Chapter7 = new Scene("7_mini_mita", "Mini Mita", "Scene 9 - ChibiMita");
    public static readonly Scene Chapter8 = new Scene("8_dummies_and_forgotten_puzzles", "Dummies and Forgotten Puzzles", "Scene 10 - ManekenWorld", "Scene 11 - Backrooms");
    public static readonly Scene Chapter9 = new Scene("9_she_just_wants_to_sleep", "She Just Wants to Sleep", "Scene 17 - Dreamer");
    public static readonly Scene Chapter10 = new Scene("10_novels", "Novels", "Scene 18 - 2D");
    public static readonly Scene Chapter11 = new Scene("11_reading_books_destroying_glass", "Reading Books, Destroying Glitches", "Scene 19 - Glasses");
    public static readonly Scene Chapter12 = new Scene("12_run_and_hide", "Run and Hide!", "Scene 20 - FightMita");
    public static readonly Scene Chapter13 = new Scene("13_old_version", "Old Version", "Scene 13 - HelloCore", "Scene 12 - Freak");
    public static readonly Scene Chapter14 = new Scene("14_the_real_world", "The Real World?", "Scene 14 - MobilePlayer");
    public static readonly Scene Chapter15 = new Scene("15_reboot", "Reboot", "Scene 15 - BasementAndDeath");
    public static readonly Scene Unknown = new Scene("game_logo", "Exploring the unknown...");

    private static readonly List<Scene> Scenes = new()
    {
        Aihasto,
        Loading,
        MainMenu,
        Prologue,
        Chapter1,
        Chapter2,
        Chapter3,
        Chapter4,
        Chapter5,
        Chapter6,
        Chapter7,
        Chapter8,
        Chapter9,
        Chapter10,
        Chapter11,
        Chapter12,
        Chapter13,
        Chapter14,
        Chapter15,
        Unknown
    };

    public static Scene GetSceneByName(string sceneName)
    {
        foreach (var scene in Scenes)
        {
            if (scene.Aliases.Contains(sceneName))
            {
                return scene;
            }
        }
        return Unknown;
    }
}
