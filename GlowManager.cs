/* COPYRIGHT NOTICE   
 * GlowStrips, a Kerbal Space Program Mod   
 * Copyright (C) 2014 Zachary Jordan a.k.a. Zarenor Darkstalker
 *    
 * This program is free software: you can redistribute it and/or modify   
 * it under the terms of the GNU General Public License as published by   
 * the Free Software Foundation, either version 3 of the License, or   
 * (at your option) any later version.
 * 
 *    
 * This program is distributed in the hope that it will be useful,   
 * but WITHOUT ANY WARRANTY; without even the implied warranty of    
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the  
 * GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * This mod references KSPAPIExtensions (KAE), originally by swamp-ig,
 * released under the CC-BY-SA 3.0 with some modifications.
 * Details on KAE are available at <http://forum.kerbalspaceprogram.com/threads/81496>
 * 
 * This mod includes parts (modified and/or unmodified) of ferramGraph, 
 * by Michael Ferrara a.k.a. ferram4, also released under the GPLv3 (or any later version).
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GlowStrips
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class GlowManager : MonoBehaviour
    {
        public static GlowManager instance;

        [KSPField(isPersistant = true)]
        private Rect mainUIRect = new Rect(300, 100, 700, 400);

        [KSPField(isPersistant = true)]
        private Rect groupUIRect = new Rect(270, 130, 300, 500);

        private string save = "";

        private Game game;

        private bool showGUI = false;

        private bool showGroupGUI = false;

        private ApplicationLauncherButton StockButton;

        private KerbalGraph animGraph = new KerbalGraph(400, 150);

        private Vector2 edGroupScroll = Vector2.zero;

        private int selectedGroup = 0;

        private bool playing = false;

        private List<string> groupList = new List<string>();

        private List<GlowStripX> stripList = new List<GlowStripX>();
        /// <summary>
        /// This is called just before the first Update() is called. 
        /// It's possible for something to happen before this, and it's possible that the ship may not be loadedf.
        /// </summary>
        public void Start()
        {
            save = HighLogic.SaveFolder;
            game = HighLogic.CurrentGame;

            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            Debug.Log("[GS] Added Event");
            /*
            var ssl = EditorLogic.SortedShipList;
            var sl = FindObjectsOfType<GlowStripX>();

            if (sl != null)
            {
                stripList.AddRange(sl.Where(gsx => ssl.Contains(gsx.part)));
                if (stripList.Count > 0)
                    stripList.ForEach(gsx => { if (!groupList.Contains(gsx.groupName)) groupList.Add(gsx.groupName); });
            }
             */
            Debug.Log("[GS] Manager finished Start()");
        }

        /// <summary>
        /// This method is called when the manager is about to be destroyed. Do any necessary cleanup here.
        /// </summary>
        public void OnDestroy()
        {
            if (instance == this)
                instance = null;
            ApplicationLauncher.Instance.RemoveModApplication(StockButton);
            Debug.Log("[GS] Manager finished OnDestroy()");
        }

        /// <summary>
        /// Called for GUI updates
        /// </summary>
        public void OnGUI()
        {
            if (showGUI)
            {
                mainUIRect = GUILayout.Window(9791, mainUIRect, editorGUI, "GlowStrips");
                if (showGroupGUI)
                {

                }
            }
        }


        /// <summary>
        /// Fires when we're told the AppLauncher is ready to accept commands.
        /// </summary>
        public void OnGUIAppLauncherReady()
        {
            StockButton = ApplicationLauncher.Instance.AddModApplication(
                 onToggleOn,
                 onToggleOff,
                 null,
                 null,
                 null,
                 null,
                 ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                 (Texture)GameDatabase.Instance.GetTexture("GlowStrips/Textures/GSButton", false));
        }


        /// <summary>
        /// Fires when the GUI has been toggled on by the AppLauncher.
        /// </summary>
        private void onToggleOn()
        {
            //Any necessary preparation
            showGUI = true;
        }

        /// <summary>
        /// Fires when the GUI has been toggled off by the AppLauncher.
        /// </summary>
        private void onToggleOff()
        {
            //Any necessary shutdown
            showGUI = false;
        }

        /// <summary>
        /// The method drawing the main editor GUI window.
        /// </summary>
        /// <param name="id">The id of the windown being drawn.</param>
        private void editorGUI(int id)
        {
            // Store frequently used formatting options
            var smallButton = new[] { GUILayout.Width(30), GUILayout.Height(30) };
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            //Overall layout
            GUILayout.BeginHorizontal();
            {
                //Group list
                GUILayout.BeginVertical(GUILayout.Width(175));
                {
                    GUILayout.Label("Groups", labelStyle);
                    edGroupScroll = GUILayout.BeginScrollView(edGroupScroll, false, true);
                    selectedGroup = GUILayout.SelectionGrid(selectedGroup, groupList.ToArray(), 1);
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
                GUILayout.Space(10);
                //Graph & Buttons
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                {
                    //Graph (And indicator, maybe?)
                    animGraph.Display(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                    //Buttons, etc. for graph editing
                    GUILayout.BeginHorizontal(GUILayout.Height(40));
                    {
                        bool wasPlaying = playing;
                        if (GUILayout.Button("stop"/*use stop texture here*/, smallButton))
                            stopButton();
                        playing = GUILayout.Toggle(playing, "play"/*use play texture here*/, GUI.skin.button, smallButton);
                        if (playing != wasPlaying)
                            playButton();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        private void playButton()
        {
            throw new NotImplementedException();
        }

        private void stopButton()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The method drawing the group GUI window.
        /// </summary>
        /// <param name="id">The id of the window being drawn.</param>
        private void groupGUI(int id)
        {

        }
    }
}
