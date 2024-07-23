using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.AurorasAdditions.Entities;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using static Celeste.TextMenu;
using static Celeste.TextMenuExt;
using System.Text;
using System.Collections;
using MonoMod.Utils;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.AurorasAdditions
{
    public class AurorasAdditionsModule : EverestModule
    {
        public static AurorasAdditionsModule Instance { get; private set; }

        public override Type SettingsType => typeof(AurorasAdditionsModuleSettings);
        public static AurorasAdditionsModuleSettings ModSettings => (AurorasAdditionsModuleSettings)Instance._Settings;

        public override Type SessionType => typeof(AurorasAdditionsModuleSession);
        public static AurorasAdditionsModuleSession Session => (AurorasAdditionsModuleSession)Instance._Session;

        public override Type SaveDataType => typeof(AurorasAdditionsModuleSaveData);
        public AurorasAdditionsModuleSaveData AASaveData => _SaveData as AurorasAdditionsModuleSaveData;

        private static bool isInDrawMode;
        private static DrawModeUI drawModeUI;


        private static bool isMouseHeldDown;
        private static Vector2 lastMousePosition;
        private static int thickness = 3;

        private int renderModNameFrames = 0;
        private string renderModName = "???";

        private static bool ignoreLevelExit;

        private static readonly Type t_OuiChapterPanelOption = typeof(OuiChapterPanel)
            .GetNestedType("Option", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static bool inContinueDecision = false;

        private static readonly string ContinueDecisionContinue = "AuroraAquirWasHere_AurorasAdditions_SessionContinue";
        private static readonly string ContinueDecisionDelete = "AuroraAquirWasHere_AurorasAdditions_SessionDelete";

        public AurorasAdditionsModule()
        {
            Instance = this;
        }

        public override void Load()
        {
            ignoreLevelExit = false;
            isInDrawMode = false;
            On.Celeste.Level.Update += ModLevelUpdate;
            On.Celeste.TextMenu.Update += TextMenu_Update;
            On.Celeste.TextMenu.Render += TextMenu_Render;

            Everest.Events.Input.OnInitialize += () =>
            {
                ModSettings.JumpToPreviousModOption.SetRepeat(0.4f, 0.15f);
                ModSettings.JumpToNextModOption.SetRepeat(0.4f, 0.15f);
            };
            Everest.Events.Level.OnCreatePauseMenuButtons += Level_OnCreatePauseMenuButtons;
            Everest.Events.Level.OnExit += Level_OnExit;
            Everest.Events.Level.OnLoadLevel += Level_OnLoadLevel;
            On.Celeste.OuiChapterPanel.Start += OuiChapterPanel_Start;
            On.Celeste.OuiChapterPanel.Swap += OuiChapterPanel_Swap;
            On.Celeste.OuiChapterPanel.DrawCheckpoint += OuiChapterPanel_DrawCheckpoint;
        }


        public override void Unload()
        {
            On.Celeste.Level.Update -= ModLevelUpdate;
            On.Celeste.TextMenu.Update -= TextMenu_Update;
            On.Celeste.TextMenu.Render -= TextMenu_Render;

            Everest.Events.Input.OnInitialize -= () =>
            {
                ModSettings.JumpToPreviousModOption.SetRepeat(0.4f, 0.1f);
                ModSettings.JumpToNextModOption.SetRepeat(0.4f, 0.1f);
            };
            Everest.Events.Level.OnCreatePauseMenuButtons -= Level_OnCreatePauseMenuButtons;
            Everest.Events.Level.OnExit -= Level_OnExit;
            Everest.Events.Level.OnLoadLevel -= Level_OnLoadLevel;
            On.Celeste.OuiChapterPanel.Start -= OuiChapterPanel_Start;
            On.Celeste.OuiChapterPanel.Swap -= OuiChapterPanel_Swap;
            On.Celeste.OuiChapterPanel.DrawCheckpoint -= OuiChapterPanel_DrawCheckpoint;
        }

        /******
         * START OF Variant Icon code
         * *******/

        private void Level_OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
        {
            if (ModSettings.ShowAssistIcons && level.Entities.FindFirst<AssistIconController>() == null) level.Add(new AssistIconController());
        }

        /******
         * START OF S&Q2M code
         * *******/
        private static string GetMapUniqueID(AreaKey area)
        {
            return area.GetSID() + ", " + area.Mode;
        }

        private void OuiChapterPanel_DrawCheckpoint(On.Celeste.OuiChapterPanel.orig_DrawCheckpoint orig, OuiChapterPanel self, Vector2 center, object option, int checkpointIndex)
        {
            if (inContinueDecision) return;
            orig(self, center, option, checkpointIndex);
        }

        private void OuiChapterPanel_Swap(On.Celeste.OuiChapterPanel.orig_Swap orig, OuiChapterPanel self)
        {
            if (inContinueDecision)
            {

                inContinueDecision = false;
                DynamicData panel = DynamicData.For(self);
                panel.Set("selectingMode", true);
                int currOption = panel.Get<int>("option");
                panel.Invoke("Reset");
                panel.Set("option", currOption);
                panel.Invoke("UpdateStats", false, false, false, false);
                panel.Invoke("SetStatsPosition", false);

                return;
            }
            else if (SessionDataExists(GetMapUniqueID(self.Area)))
            {
                if (SwapToSelectionPanel(self)) return;
            }
            orig(self);
        }

        private void OuiChapterPanel_Start(On.Celeste.OuiChapterPanel.orig_Start orig, OuiChapterPanel self, string checkpoint)
        {
            string mapUID = GetMapUniqueID(self.Area);
            if (inContinueDecision)
            {
                self.Focused = false;
                if (checkpoint == ContinueDecisionDelete)
                {

                    Instance.AASaveData.SessionsPerLevel.Remove(mapUID);
                    Instance.AASaveData.ModSessionsPerLevel.Remove(mapUID);
                    Instance.AASaveData.ModSessionsPerLevelBinary.Remove(mapUID);

                    DynamicData panel = DynamicData.For(self);
                    // Just going back to "Climb" is a better idea than sometimes entering the level and sometimes showing checkpoints
                    panel.Invoke("Swap");
                    /**
                    panel.Set("selectingMode", true);
                    inContinueDecision = false;

                    if (!SaveData.Instance.FoundAnyCheckpoints(self.Area))
                    {
                        self.Start(null);
                    }
                    else
                    {
                        Audio.Play("event:/ui/world_map/chapter/level_select");
                        panel.Invoke("Swap");
                    }
                    **/
                    self.Focused = true;
                    return;
                }
                else if (checkpoint == ContinueDecisionContinue)
                {
                    // TODO
                    // this should start as if we entered using Save & quit
                    Audio.SetMusic(null, true, true);
                    Audio.SetAmbience(null, true);
                    Audio.Play("event:/ui/world_map/chapter/checkpoint_start");
                    new FadeWipe(Engine.Scene, false, () =>
                    {
                        inContinueDecision = false;
                        Session session;
                        if(!Instance.AASaveData.SessionsPerLevel.TryGetValue(mapUID, out string savedSessionXML))
                        {
                            Logger.Log(LogLevel.Warn, "Aurora's Additions", "Could not load saved session");
                        }

                        using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(savedSessionXML)))
                        {
                            session = (Session)new XmlSerializer(typeof(Session)).Deserialize(stream);
                            session.InArea = true;
                            SaveData.Instance.CurrentSession_Safe = session;
                        }

                        loadModSessions(session);

                        Instance.AASaveData.SessionsPerLevel.Remove(mapUID);
                        Instance.AASaveData.ModSessionsPerLevel.Remove(mapUID);
                        Instance.AASaveData.ModSessionsPerLevelBinary.Remove(mapUID);

                        LevelEnter.Go(SaveData.Instance.CurrentSession_Safe, true);
                    });
                    return;
                }
            }
            else
            {
                if (SessionDataExists(mapUID))
                {
                    if (SwapToSelectionPanel(self)) return;
                }
            }
            orig(self, checkpoint);
        }

        /** attempt to instead add a 1st checkpoint.. it just doesn't work at all idk
        private IEnumerator OuiChapterPanel_SwapRoutine(On.Celeste.OuiChapterPanel.orig_SwapRoutine orig, OuiChapterPanel self)
        {
            yield return new SwapImmediately(orig(self));

            DynamicData ppanel = DynamicData.For(self);

            IList checkpoints = ppanel.Get<IList>("checkpoints");
            checkpoints.Insert(0, DynamicData.New(t_OuiChapterPanelOption)(new
            {
                Label = Dialog.Clean("chapterpanel_AurorasAdditions_continueSession"),
                BgColor = Calc.HexToColor("eabe26"),
                Icon = GFX.Gui["areaselect/startpoint"],
                CheckpointLevelName = ContinueDecisionContinue,
                Large = false,
                Siblings = checkpoints.Count+1
            }));
            foreach (Object cp in checkpoints)
            {
                DynamicData ddcp = DynamicData.For(cp);
                ddcp.Set("Siblings", checkpoints.Count);
            }

            yield break;
        }
        **/

        private bool SessionDataExists(string uid)
        {
            return Instance.AASaveData.SessionsPerLevel.ContainsKey(uid);
        }

        private bool SwapToSelectionPanel(OuiChapterPanel panel)
        {
            DynamicData ppanel = DynamicData.For(panel);

            if (!ppanel.Get<bool>("selectingMode")) return false;

            ppanel.Set("selectingMode", false);

            IList checkpoints = ppanel.Get<IList>("checkpoints");
            checkpoints.Clear();
            checkpoints.Add(DynamicData.New(t_OuiChapterPanelOption)(new
            {
                Label = Dialog.Clean("chapterpanel_AurorasAdditions_continueSession"),
                BgColor = Calc.HexToColor("eabe26"),
                Icon = GFX.Gui["areaselect/startpoint"],
                CheckpointLevelName = ContinueDecisionContinue,
                Large = false,
                Siblings = 2
            }));

            checkpoints.Add(DynamicData.New(t_OuiChapterPanelOption)(new
            {
                Label = Dialog.Clean("chapterpanel_AurorasAdditions_deleteSession"),
                Icon = GFX.Gui["areaselect/aurora_aquir/no"],
                CheckpointLevelName = ContinueDecisionDelete,
                Large = false,
                Siblings = 2
            }));
            inContinueDecision = true;
            return true;
        }

        private void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow)
        {
            if(!ignoreLevelExit)
            {
                string mapUID = GetMapUniqueID(level.Session.Area);
                Instance.AASaveData.SessionsPerLevel.Remove(mapUID);
                Instance.AASaveData.ModSessionsPerLevel.Remove(mapUID);
                Instance.AASaveData.ModSessionsPerLevelBinary.Remove(mapUID);
            }
        }

        private void Level_OnCreatePauseMenuButtons(Level level, TextMenu menu, bool minimal)
        {
            if (!ModSettings.ShowSaveAndQuitToMap) return;
            Button button = new(Dialog.Clean("menu_AurorasAdditions_SaveAndQuitToMap"));
            button.Pressed(() =>
            {
                menu.Focused = false;
                SaveSessionsAndQuit();
            });
            EaseInSubHeaderExt descriptionText = new(Dialog.Get("menu_AurorasAdditions_SaveAndQuitToMap_Warning"), false, menu, null)
            {
                TextColor = Calc.HexToColor("a33333"),
                HeightExtra = 0f
            };

            // Add button below Save & Quit
            int saveAndQuitIndex = menu.Items.FindIndex(0, (item) => item is Button button && button.Label == Dialog.Clean("menu_pause_savequit"));
            if (saveAndQuitIndex == -1) saveAndQuitIndex = Math.Min(7, menu.Items.Count-1);
            // Add them normally so it does all the recalculation etc.
            menu.Add(button);
            menu.Add(descriptionText);

            // remove again and insert in right place lol
            menu.Items.Remove(button);
            menu.Items.Remove(descriptionText);

            menu.Items.Insert(saveAndQuitIndex, descriptionText);
            menu.Items.Insert(saveAndQuitIndex, button);

            button.OnEnter += () => descriptionText.FadeVisible = true;
            button.OnLeave += () => descriptionText.FadeVisible = false;
            if (level.SaveQuitDisabled) button.Disabled = true;
        }

        private void SaveSessionsAndQuit()
        {
            Level level = Engine.Scene as Level;
            if (level == null) return;
            // add death like s&q
            level.Session.InArea = true;
            level.Session.Deaths++;
            level.Session.DeathsInCurrentLevel++;
            SaveData.Instance.AddDeath(level.Session.Area);

            // Quit level

            level.DoScreenWipe(false, () => {
                // Save Mod sessions (not ours) to the map id in save data
                // This is taken from CollabUtils2 (and then modified), link: https://github.com/EverestAPI/CelesteCollabUtils2/blob/e8c48ad85f11f40eb6f813d0669ce7af725f2ffd/UI/ReturnToLobbyHelper.cs#L287
                string mapUID = GetMapUniqueID(level.Session.Area);
                Instance.AASaveData.SessionsPerLevel[mapUID] = Encoding.UTF8.GetString(UserIO.Serialize(level.Session));

                // save all mod sessions of mods that have mod sessions.
                Dictionary<string, string> modSessions = new Dictionary<string, string>();
                Dictionary<string, string> modSessionsBinary = new Dictionary<string, string>();
                foreach (EverestModule mod in Everest.Modules)
                {
                    if (mod == Instance)
                    {
                        // we do NOT want to mess with our own session!
                        continue;
                    }

                    if (mod.SaveDataAsync)
                    {
                        // new save data API: session is serialized into a byte array.
                        byte[] sessionBinary = mod.SerializeSession(SaveData.Instance.FileSlot);
                        if (sessionBinary != null)
                        {
                            modSessionsBinary[mod.Metadata.Name] = Convert.ToBase64String(sessionBinary);
                        }
                    }
                    else if (mod._Session != null && !(mod._Session is EverestModuleBinarySession))
                    {
                        // old behavior: serialize save data ourselves, as a string.
                        try
                        {
                            modSessions[mod.Metadata.Name] = YamlHelper.Serializer.Serialize(mod._Session);
                        }
                        catch (Exception e)
                        {
                            // this is the same fallback message as the base EverestModule class if something goes wrong.
                            Logger.Log(LogLevel.Warn, "AuroraAdditions/SaveModSessions", "Failed to save the session of " + mod.Metadata.Name + "!");
                            Logger.LogDetailed(e);
                        }
                    }
                }
                Instance.AASaveData.ModSessionsPerLevel[mapUID] = modSessions;
                Instance.AASaveData.ModSessionsPerLevelBinary[mapUID] = modSessionsBinary;

                // leave
                ignoreLevelExit = true;
                Engine.Scene = new LevelExit(LevelExit.Mode.GiveUp, level.Session, level.HiresSnow);
                ignoreLevelExit = false;
            }, true );
        }

        // This is copied from CollabUtils2, direct link: https://github.com/EverestAPI/CelesteCollabUtils2/blob/e8c48ad85f11f40eb6f813d0669ce7af725f2ffd/UI/ReturnToLobbyHelper.cs#L418C9-L418C9
        private static bool loadModSessions(Session session)
        {
            string mapUID = GetMapUniqueID(session.Area);
            if (Instance.AASaveData.ModSessionsPerLevel.TryGetValue(mapUID, out Dictionary<string, string> sessions))
            {
                Instance.AASaveData.ModSessionsPerLevelBinary.TryGetValue(mapUID, out Dictionary<string, string> sessionsBinary);

                // restore all mod sessions we can restore.
                foreach (EverestModule mod in Everest.Modules)
                {
                    if (mod == Instance)
                    {
                        continue;
                    }

                    if (mod.SaveDataAsync && sessionsBinary != null && sessionsBinary.TryGetValue(mod.Metadata.Name, out string savedSessionBinary))
                    {
                        // new save data API: session is deserialized by passing the byte array as is.
                        mod.DeserializeSession(SaveData.Instance.FileSlot, Convert.FromBase64String(savedSessionBinary));
                    }
                    if (mod._Session != null && sessions.TryGetValue(mod.Metadata.Name, out string savedSession))
                    {
                        // old behavior: deserialize the session ourselves from a string.
                        try
                        {
                            // note: we are deserializing the session rather than just storing the object, because loading the session usually does that,
                            // and a mod could react to a setter on its session being called.
                            YamlHelper.DeserializerUsing(mod._Session).Deserialize(savedSession, mod.SessionType);
                        }
                        catch (Exception e)
                        {
                            // this is the same fallback message as the base EverestModule class if something goes wrong.
                            Logger.Log(LogLevel.Warn, "AuroraAdditions/LoadModSessions", "Failed to load the session of " + mod.Metadata.Name + "!");
                            Logger.LogDetailed(e);
                        }
                    }
                }

                return true;
            }

            return false;
        }
        // --- END OF SESSION SAVE CODE ---
        // --- START OF MOD OPTION SCROLL CODE ---
        private void TextMenu_Render(On.Celeste.TextMenu.orig_Render orig, TextMenu self)
        {
            orig(self);
            if(renderModNameFrames > 0)
            {
                renderModNameFrames--;
                Vector2 position = new Vector2(10f, 1080f-33f);
                Vector2 textMeasure = ActiveFont.Measure(renderModName);
                position.X += textMeasure.X;
                Draw.Rect(new Vector2(0f, 1080f-33f-textMeasure.Y), textMeasure.X, textMeasure.Y, Color.Black * 0.8f);
                ActiveFont.DrawOutline(renderModName, position, Vector2.One, Vector2.One, Color.White, 2f, Color.Black);
            }
        }

        private void TextMenu_Update(On.Celeste.TextMenu.orig_Update orig, TextMenu self)
        {
            orig(self);

            if ((self?.Items?.Any() ?? false) && self.Items[0] is HeaderImage hi && hi.Image == "menu/everest")
            {
                bool isFirstOptionOfMod() => self.Items[self.Selection - 1] is SubHeader subheader && subheader.Title.Contains("| v.");

                if (ModSettings.JumpToPreviousModOption.Pressed && self.Selection != self.FirstPossibleSelection)
                {
                    self.Current.OnLeave?.Invoke();
                    self.MoveSelection(-1, false);
                    while (!isFirstOptionOfMod() && self.Selection > self.FirstPossibleSelection)
                    {
                        self.MoveSelection(-1, false);
                    }
                    Audio.Play("event:/ui/main/rollover_down");
                    self.Current.OnEnter?.Invoke();

                    renderModNameFrames = 30;
                    renderModName = (self.Items[self.Selection - 1] as SubHeader)?.Title.Split('|')[0] ?? "???";
                    return;
                }
                else if (ModSettings.JumpToNextModOption.Pressed && self.Selection != self.LastPossibleSelection)
                {
                    self.Current.OnLeave?.Invoke();
                    self.MoveSelection(1, false);
                    while (!isFirstOptionOfMod() && self.Selection < self.LastPossibleSelection)
                    {
                        self.MoveSelection(1, false);
                    }
                    Audio.Play("event:/ui/main/rollover_down");
                    self.Current.OnEnter?.Invoke();

                    renderModNameFrames = 30;
                    if(self.Selection != self.LastPossibleSelection) renderModName = (self.Items[self.Selection - 1] as SubHeader)?.Title.Split('|')[0] ?? "???";
                    return;
                }
            }
        }

        // start of draw?

        private static void ModLevelUpdate(On.Celeste.Level.orig_Update orig, Level level)
        {
            orig(level);
            handleKeyPresses(level);

            if (isInDrawMode)
            {
                // no :D
                if (ModSettings.DisableDrawModeBackground && level?.HudRenderer != null)
                {
                    level.HudRenderer.BackgroundFade = 0f;
                }
                DrawModeUpdate();
            }
        }

        private static void ToggleDrawMode(Level level)
        {
            // if you triggered this same frame as player dies, always leads to it being untoggled.
            Boolean isPlayerDead = level?.Tracker?.GetEntity<Player>()?.Dead ?? true;
            if (isPlayerDead || isInDrawMode)
            {
                Engine.Instance.IsMouseVisible = false;
                isInDrawMode = false;
                if (drawModeUI != null) drawModeUI.RemoveSelf();
                drawModeUI = null;
            } else
            {
                if (!ModSettings.DrawCursor) Engine.Instance.IsMouseVisible = true;

                drawModeUI = new DrawModeUI();
                if (level == null)
                {
                    Logger.Log(LogLevel.Warn, "Aurora's Helper", "Level is null, cannot add ui to scene");
                    return;
                }
                level.Add(drawModeUI);
                isInDrawMode = true;
            }
        }

        private static void handleKeyPresses(Level level)
        {
            SaveData savedata = SaveData.Instance;
            if (ModSettings.ToggleInvicibility.Pressed)
            {
                ModSettings.ToggleInvicibility.ConsumePress();
                savedata.Assists.Invincible = !savedata.Assists.Invincible;
            }

            if (ModSettings.ToggleInfiniteStamina.Pressed)
            {
                ModSettings.ToggleInfiniteStamina.ConsumePress();
                savedata.Assists.InfiniteStamina = !savedata.Assists.InfiniteStamina;
            }

            if (ModSettings.ToggleAirDashMode.Pressed)
            {
                ModSettings.ToggleAirDashMode.ConsumePress();
                savedata.Assists.DashMode = savedata.Assists.DashMode switch 
                {
                    Assists.DashModes.Normal => Assists.DashModes.Two,
                    Assists.DashModes.Two => Assists.DashModes.Infinite,
                    Assists.DashModes.Infinite => Assists.DashModes.Normal,
                    _ => Assists.DashModes.Normal
                };
            }

            if (ModSettings.ToggleDrawMode.Pressed)
            {
                ModSettings.ToggleDrawMode.ConsumePress();

                ToggleDrawMode(level);
            }
            if(ModSettings.ToggleMusic.Pressed)
            {
                if (Settings.Instance.MusicVolume != 0)
                {
                    Instance.AASaveData.MusicVolumeMemory = Settings.Instance.MusicVolume;
                    Settings.Instance.MusicVolume = 0;
                } else
                {
                    Settings.Instance.MusicVolume = Instance.AASaveData.MusicVolumeMemory;
                }
                Settings.Instance.ApplyMusicVolume();
            }
            // let escape also close the mode
            if(MInput.Keyboard.Pressed(Keys.Escape) && isInDrawMode)
            {
                ToggleDrawMode(level);
            }
        }


        private static void DrawModeUpdate()
        {
            if (drawModeUI == null)
            {
                Engine.Instance.IsMouseVisible = false;
                isInDrawMode = false;
                return;
            }

            Vector2 mousePosition = MInput.Mouse.Position;
            Color color = Calc.HexToColor(ModSettings.DrawModeColor);

            if (ModSettings.HoldForColor2.Check && ModSettings.HoldForColor3.Check)
            {
                color = Calc.HexToColor(ModSettings.DrawModeColor4);
            } else if (ModSettings.HoldForColor2.Check)
            {
                color = Calc.HexToColor(ModSettings.DrawModeColor2);
            } else if (ModSettings.HoldForColor3.Check)
            {
                color = Calc.HexToColor(ModSettings.DrawModeColor3);
            }

            if(MInput.Mouse.CheckLeftButton)
            {
                if(isMouseHeldDown)
                {
                    Vector2[] line = { lastMousePosition, mousePosition };
                    drawModeUI.lines[line] = new DrawModeUI.LineData(color, thickness);
                } else
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        for(int y = -1; y <= 1; y++)
                        {
                            Vector2 point = mousePosition;
                            point.X += x;
                            point.Y += y;
                            drawModeUI.points[point] = new DrawModeUI.LineData(color, thickness);
                        }
                    }
                }

                isMouseHeldDown = true;
            } else
            {
                isMouseHeldDown = false;
            }

            lastMousePosition = mousePosition;

            if(MInput.Mouse.CheckRightButton)
            {
                drawModeUI.drawEraserCircle = true;

                foreach (var item in drawModeUI.points.Where((kvp) => Vector2.Distance(kvp.Key, mousePosition) < 33f).ToList())
                {
                    drawModeUI.points.Remove(item.Key);
                }
                foreach (var item in drawModeUI.lines.Where((kvp) => Vector2.Distance(kvp.Key[0], mousePosition) < 33f || Vector2.Distance(kvp.Key[1], mousePosition) < 33f).ToList())
                {
                    drawModeUI.lines.Remove(item.Key);
                }
            } else
            {
                drawModeUI.drawEraserCircle = false;
            }
            if (ModSettings.DrawCursor)
            {
                drawModeUI.drawCursor = true;
                drawModeUI.currColor = color;
            }
            else
            {
                drawModeUI.drawCursor = false;
            }
        }
    }
}