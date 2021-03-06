﻿/* COPYRIGHT NOTICE   
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
using KSPAPIExtensions;

namespace GlowStrips
{
    /// <summary>
    /// The PartModule class implementing GlowStrip functionality on supported parts.
    /// </summary>
    public class GlowStripX : PartModule
    {
        #region "KSPFields"
        /// <summary>
        /// The value color values are multiplied by before going into the animation. [0,1]
        /// </summary>
        [UI_FloatRange(maxValue = 1, minValue = 0, scene = UI_Scene.Editor, stepIncrement = 0.025f)]
        [KSPField(guiName = "Value", isPersistant = true)]
        public float glowValue = 1.0f;

        /// <summary>
        /// The red value which is multiplied by glowValue and applied to the animation. [0,1]
        /// </summary>
        [UI_FloatRange(maxValue = 1, minValue = 0, scene = UI_Scene.Editor, stepIncrement = 0.025f)]
        [KSPField(guiName = "Red", isPersistant = true)]
        public float glowRed = 1.0f;

        /// <summary>
        /// The green value which is multiplied by glowValue and applied to the animation. [0,1]
        /// </summary>
        [UI_FloatRange(maxValue = 1, minValue = 0, scene = UI_Scene.Editor, stepIncrement = 0.025f)]
        [KSPField(guiName = "Green", isPersistant = true)]
        public float glowGreen = 1.0f;

        /// <summary>
        /// The blue value which is multiplied by glowValue and applied to the animation. [0,1]
        /// </summary>
        [UI_FloatRange(maxValue = 1, minValue = 0, scene = UI_Scene.Editor, stepIncrement = 0.025f)]
        [KSPField(guiName = "Blue", isPersistant = true)]
        public float glowBlue = 1.0f;

        /// <summary>
        /// The array indice used to select and display the number of seconds while in the editor. [0,19]
        /// </summary>
        [UI_ChooseOption(scene = UI_Scene.Editor)]
        [KSPField(guiName = "Time", isPersistant = true)]
        public int time = 1;

        /// <summary>
        /// The array indice used to select and display the timescale while in the editor. [0,5]
        /// </summary>
        [UI_ChooseOption(scene = UI_Scene.Editor)]
        [KSPField(guiName = "Time Scale", isPersistant = true)]
        public int timeScale = 1;

        /// <summary>
        /// Whether the part is glowing or not. Setting this parameter externally will have undesired effects.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool glowing;

        /// <summary>
        /// Whether the part is pulsing or not. Setting this parameter externally will have undesired effects.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool pulsing;

        /// <summary>
        /// The number of seconds an animation lasts, trough to peak, as displayed while in flight.
        /// </summary>
        [KSPField(guiName = "Seconds", isPersistant = false, guiActive = true, guiActiveEditor = false)]
        public float seconds = 1.0f;

        [KSPField(guiName = "Name", isPersistant= true, guiActive = false, guiActiveEditor = true)]
        public string GSName = "";

        [KSPField(isPersistant = true)]
        public string groupName = "";

        /// <summary>
        /// The Vec3 used to scale the GlowStrip. .z is the length parameter, and is equal to length when updated.
        /// </summary>
        [KSPField(isPersistant = true)]
        public Vector3 scale = Vector3.one;

        /// <summary>
        /// The length, in meters, of the GlowStrip
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Length", guiFormat = "S5", guiUnits = "m")]
        [UI_FloatEdit(minValue = .125f, maxValue = 12f, incrementSlide = .0125f, incrementSmall = .25f, incrementLarge = 1.0f)]
        public float length = 1.0f;

        /// <summary>
        /// Whether the GlowStrip is bound to the throttle or not.
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Bound to throttle", guiActive = true)]
        [UI_Toggle(scene = UI_Scene.Editor, enabledText = "Yes", disabledText = "No")]
        public bool throttleBound = false;

        /// <summary>
        /// Whether the GlowStrip was elected to be the 'master' of it's symmetryCounterparts. Determines which counterpart calls updateCounterparts.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool master = true;
        #endregion

        #region "Other Fields"
        /// <summary>
        /// Used to compare if the color values have been altered.
        /// </summary>
        protected Vector4 oldColorValues;

        /// <summary>
        /// Used to compare if the time values have been altered.
        /// </summary>
        protected Vector2 oldTimeValues;

        /// <summary>
        /// A reference to the animation component of the part. Populated in OnStart().
        /// </summary>
        protected Animation partAnim;

        /// <summary>
        /// A reference to the glowAnim animation, attached to the animation component of the part. Populated in OnStart().
        /// </summary>
        protected AnimationState glowAnim;

        /// <summary>
        /// A reference to the animation clip, which is attached to the animation, which is attached to the animation component. Populated in OnStart().
        /// </summary>
        protected AnimationClip glowAnimClip;

        /// <summary>
        /// The string array of timescale options. 
        /// </summary>
        protected string[] timeScaleOptions = { "0.1s", "0.5s", "1s", "5s", "10s", "30s" };

        /// <summary>
        /// The float array of timescale options.
        /// </summary>
        protected float[] timeScales = { 0.1f, 0.5f, 1f, 5f, 10f, 30f };

        /// <summary>
        /// Used to compare if throttleBound has been altered.
        /// </summary>
        protected bool oldTB;

        /// <summary>
        /// Used to compare if length has been altered.
        /// </summary>
        protected float oldLength;

        /// <summary>
        /// Used to save 'time', when the GlowStrip is bound to throttle.
        /// </summary>
        protected int oldTime = 1;

        /// <summary>
        /// Used to save 'timeScale', when the GlowStrip is bound to throttle.
        /// </summary>
        protected int oldTimeScale = 1;

        /// <summary>
        /// Used to save 'pulsing', when the GlowStrip is bound to throttle.
        /// </summary>
        protected bool oldPulsing;

        /// <summary>
        /// Used to save 'glowing', when the GlowStrip is bound to throttle.
        /// </summary>
        protected bool oldGlowing;
        
        #endregion

        #region "PartModule Methods"

        /// <summary>
        /// The method called by KSP when the PartModule is started. Called on each part, every time.
        /// </summary>
        /// <param name="state">The StartState indicating the starting condition of the part (and the vessel it's on)</param>
        public override void OnStart(StartState state)
        {
            if (!(part.FindModelAnimators("glowAnim").FirstOrDefault() == null))
            {
                partAnim = part.FindModelAnimators("glowAnim").FirstOrDefault();
                if (!(partAnim["glowAnim"] == null))
                {
                    glowAnim = partAnim["glowAnim"];
                    if (!(glowAnim.clip == null))
                    {
                        glowAnimClip = glowAnim.clip;
                    }
                }
            }
            UI_ChooseOption tsOptions = (UI_ChooseOption)this.Fields["timeScale"].uiControlEditor;
            tsOptions.options = timeScaleOptions;

            updateSecondsOptions();

            seconds = (time + 1) * timeScales[timeScale];

            checkColorValues();
            checkTimeValues();
            updateCurves();

            if (pulsing)
            {
                partAnim.wrapMode = WrapMode.PingPong;
            }
            else
            {
                partAnim.wrapMode = WrapMode.Once;
            }

            if (glowing)
            {
                Events["toggleGlow"].guiName = "Stop Glowing";
                glowAnim.normalizedTime = 1.0f;
                partAnim.Play("glowAnim");
            }
            else
            {
                Events["toggleGlow"].guiName = "Start Glowing";
                glowAnim.normalizedTime = 0.0f;
                partAnim.Play("glowAnim");
                partAnim.Stop("glowAnim");
            }

            checkLengthValue();
            if (throttleBound)
                this.Fields["seconds"].guiActive = false;
            Invoke("updateScale", 0.01f);
            if (part.symmetryCounterparts != null)
            {
                master = true;
                foreach (Part p in part.symmetryCounterparts)
                {
                    // If any counterpart considers itself the master, this isn't the master.
                    master = p.Modules.OfType<GlowStripAdaptive>().FirstOrDefault().master ? false : master;
                }
                Debug.Log("Counterparts :" + part.symmetryCounterparts.Count);
            }
            else
            {
                master = true;
                Debug.Log("No counterparts found");
            }
        }

        /// <summary>
        /// The method called by KSP at GUI time, to render GUI and react to GUI events.
        /// </summary>
        public virtual void OnGUI()
        {
            float t = glowAnim.normalizedTime;
            bool reverse = false;
            if (glowAnim.normalizedSpeed < 0)
                reverse = true;
            bool playing = partAnim.isPlaying;
            bool update = false;

            if (!checkColorValues())
            {
                update = true;
            }
            if (!checkTimeValues())
            {
                updateSecondsOptions();
                refreshMenu();
                update = true;
            }
            if (update)
            {
                updateCurves();
                glowAnim.normalizedTime = t;
                if (reverse) glowAnim.normalizedSpeed = -glowAnim.normalizedSpeed;
                partAnim.Play("glowAnim");
                if (!pulsing && t == 1)
                {
                    partAnim.Stop();
                }
                if (!playing)
                    partAnim.Stop();
            }
            if (!checkLengthValue())
            {
                updateScale();
            }
            if (!checkTB())
            {
                if (throttleBound)
                {
                    oldTimeScale = timeScale;
                    oldTime = time;
                    timeScale = 1;
                    time = 1;
                    oldPulsing = pulsing;
                    oldGlowing = glowing;
                    pulsing = false;
                    glowing = false;
                    this.Fields["throttleBound"].guiActive = true;

                    this.Fields["timeScale"].guiActiveEditor = false;
                    this.Fields["time"].guiActiveEditor = false;
                    this.Fields["seconds"].guiActive = false;
                    this.Actions["toggleGlow"].active = false;
                    this.Actions["startGlow"].active = false;
                    this.Actions["stopGlow"].active = false;
                    this.Events["toggleGlow"].active = false;
                    this.Events["togglePulse"].active = false;
                }
                else
                {
                    time = oldTime;
                    timeScale = oldTimeScale;
                    pulsing = oldPulsing;
                    glowing = oldGlowing;
                    this.Fields["throttleBound"].guiActive = false;

                    this.Fields["timeScale"].guiActiveEditor = true;
                    this.Fields["time"].guiActiveEditor = true;
                    this.Fields["seconds"].guiActive = true;
                    this.Actions["toggleGlow"].active = true;
                    this.Actions["startGlow"].active = true;
                    this.Actions["stopGlow"].active = true;
                    this.Events["toggleGlow"].active = true;
                    this.Events["togglePulse"].active = true;
                }
                refreshMenu();
                updateCurves();
                glowAnim.normalizedTime = 0.0f;
                glowAnim.speed = 0.0f;
                partAnim.Play("glowAnim");
            }

        }

        /// <summary>
        /// The method called by KSP on each update frame.
        /// </summary>
        public override void OnUpdate()
        {
            if (throttleBound)
                throttleUpdateGlow();
        }
        #endregion

        #region "Actions"

        /// <summary>
        /// Toggles the power state of the GlowStrip.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("Toggle Glow")]
        public void toggleGlow(KSPActionParam param)
        {
            toggleGlow();
        }

        /// <summary>
        /// If the GlowStrip is off, turns it on.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("Start Glowing")]
        public void startGlow(KSPActionParam param)
        {
            if (!glowing)
            {
                toggleGlow();
            }
        }

        /// <summary>
        /// If the GlowStrip is on, turns it off.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("Stop Glowing")]
        public void stopGlow(KSPActionParam param)
        {
            if (glowing)
            {
                toggleGlow();
            }
        }

        #endregion

        #region "Events (UI buttons)"

        /// <summary>
        /// Toggles the power state of the GlowStrip.
        /// </summary>
        [KSPEvent(guiName = "Toggle Glow", name = "toggleGlow", guiActiveUnfocused = true, guiActive = true, guiActiveEditor = true)]
        public void toggleGlow()
        {


            if (glowing)
            {// Stop glowing
                Events["toggleGlow"].guiName = "Start Glowing";
                glowAnim.speed = -1.0f;
                if (pulsing)
                {
                    float t = glowAnim.normalizedTime % (seconds * 2);
                    if (t > seconds) glowAnim.normalizedTime = (seconds * 2) - t;
                    else glowAnim.normalizedTime = t;
                    partAnim.wrapMode = WrapMode.Once;
                }
                else
                {
                    //Ensure animation fades both direction.
                    if (glowAnim.normalizedTime == 0.0f || glowAnim.normalizedTime == 1.0f)
                        glowAnim.normalizedTime = 1.0f;
                }
                partAnim.Play("glowAnim");
            }
            else
            {
                Events["toggleGlow"].guiName = "Stop Glowing";
                if (!checkColorValues())
                {
                    updateCurves();
                }
                if (pulsing)
                {
                    partAnim.wrapMode = WrapMode.PingPong;
                }
                glowAnim.speed = 1.0f;
                partAnim.Play("glowAnim");
            }
            glowing = !glowing;
        }

        /// <summary>
        /// Toggles whether the GlowStrip pulses or not. (Editor only)
        /// </summary>
        [KSPEvent(guiName = "Start Pulsing", name = "togglePulse", guiActive = false, guiActiveEditor = true)]
        public void togglePulse()
        {
            if (pulsing)
            {//Stop pulsing
                Events["togglePulse"].guiName = "Start Pulsing";
                partAnim.wrapMode = WrapMode.Once;
            }
            else
            {
                Events["togglePulse"].guiName = "Stop Pulsing";
                partAnim.wrapMode = WrapMode.PingPong;
                if (glowing)
                {
                    partAnim.Play("glowAnim");
                }
            }
            pulsing = !pulsing;
        }

        #endregion

        #region "Protected Methods"

        /// <summary>
        /// Refreshes the tweakable menu on the attached part.
        /// </summary>
        protected void refreshMenu()
        {
            foreach (var window in GameObject.FindObjectsOfType(typeof(UIPartActionWindow)).OfType<UIPartActionWindow>().Where(w => w.part == part))
            {
                window.displayDirty = true;
            }
        }

        /// <summary>
        /// Ensures the timeScale setting, and the options displayed by time are in agreement.
        /// </summary>
        protected void updateSecondsOptions()
        {
            UI_ChooseOption sOptions = (UI_ChooseOption)this.Fields["time"].uiControlEditor;
            switch (timeScale)
            {
                case 0: // 0.1s Scale
                    sOptions.options = new string[] { "0.1s", "0.2s", "0.3s", "0.4s", "0.5s", "0.6s", "0.7s", "0.8s", "0.9s", "1.0s",
                                                      "1.1s", "1.2s", "1.3s", "1.4s", "1.5s", "1.6s", "1.7s", "1.8s", "1.9s", "2.0s"};
                    break;
                case 1: // 0.5s Scale
                    sOptions.options = new string[] { "0.5s", "1.0s", "1.5s", "2.0s", "2.5s", "3.0s", "3.5s", "4.0s", "4.5s", "5.0s",
                                                      "5.5s", "6.0s", "6.5s", "7.0s", "7.5s", "8.0s", "8.5s", "9.0s", "9.5s", "10s"};
                    break;
                case 2: // 1s Scale
                    sOptions.options = new string[] { "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "10s",
                                                      "11s","12s","13s","14s","15s","16s","17s","18s","19s","20s"};
                    break;
                case 3:// 5s Scale
                    sOptions.options = new string[] { "5s", "10s", "15s", "20s", "25s", "30s", "35s", "40s", "45s", "50s",
                                                      "55s","60s", "65s", "70s", "75s", "80s", "85s", "90s", "95s", "100s"};
                    break;
                case 4:// 10s Scale
                    sOptions.options = new string[] { "10s", "20s", "30s", "40s", "50s", "60s", "70s", "80s", "90s", "100s",
                                                      "110s","120s","130s","140s","150s","160s","170s","180s","190s","200s"};
                    break;
                case 5:// 30s Scale 
                    sOptions.options = new string[] { "30s", "60s", "90s", "120s", "150s", "180s", "210s", "240s", "270s", "300s",
                                                      "330s","360s","390s","420s", "450s", "480s", "510s", "540s", "570s", "600s"};
                    break;
            }
        }

        /// <summary>
        /// Checks whether the color values have changed since the last call.
        /// </summary>
        /// <returns>False if changed, true if unchanged.</returns>
        protected bool checkColorValues()
        {
            Vector4 vec = new Vector4(glowValue, glowRed, glowGreen, glowBlue);
            if (vec != oldColorValues)
            {
                oldColorValues = vec;
                return false;
            }
            else return true;
        }

        /// <summary>
        /// Checks whether the time values have changed since the last call.
        /// </summary>
        /// <returns>False if changed, true if unchanged.</returns>
        protected bool checkTimeValues()
        {
            Vector2 vec = new Vector2(time, timeScale);
            if (vec != oldTimeValues)
            {
                oldTimeValues = vec;
                return false;
            }
            else return true;
        }

        /// <summary>
        /// Updates the animation curves, based on the current set of values.
        /// </summary>
        protected void updateCurves()
        {

            if (!(glowAnimClip == null))
            {
                AnimationCurve redCurve = AnimationCurve.Linear(0.0f, 0.0f, (float)((time + 1) * timeScales[timeScale]), glowValue * glowRed);
                AnimationCurve greenCurve = AnimationCurve.Linear(0.0f, 0.0f, (float)((time + 1) * timeScales[timeScale]), glowValue * glowGreen);
                AnimationCurve blueCurve = AnimationCurve.Linear(0.0f, 0.0f, (float)((time + 1) * timeScales[timeScale]), glowValue * glowBlue);

                glowAnimClip = new AnimationClip();
                glowAnimClip.SetCurve("", typeof(Material), "_EmissiveColor.r", redCurve);
                glowAnimClip.SetCurve("", typeof(Material), "_EmissiveColor.g", greenCurve);
                glowAnimClip.SetCurve("", typeof(Material), "_EmissiveColor.b", blueCurve);

                partAnim.AddClip(glowAnimClip, "glowAnim");
                glowAnim = partAnim["glowAnim"];
            }
        }

        /// <summary>
        /// Used to update the glow, if the GlowStrip is bound to the throttle.
        /// </summary>
        protected void throttleUpdateGlow()
        {
            glowAnim.normalizedTime = (float)Math.Pow(vessel.ctrlState.mainThrottle, 2.2);
            glowAnim.speed = 0.0f;
            partAnim.Play("glowAnim");
        }

        /// <summary>
        /// Checks whether the length value has changed since the last call.
        /// </summary>
        /// <returns>False if changed, true if unchanged.</returns>
        private bool checkLengthValue()
        {
            if (length != oldLength)
            {
                oldLength = length;
                return false;
            }
            else return true;
        }

        /// <summary>
        /// Checks whether the throttleBound has changed since the last call.
        /// </summary>
        /// <returns>False if changed, true if unchanged.</returns>
        private bool checkTB()
        {
            if (throttleBound != oldTB)
            {
                oldTB = throttleBound;
                return false;
            }
            else return true;
        }

        /// <summary>
        /// Updates the scale of the GlowStrip, based on the current length value.
        /// </summary>
        private void updateScale()
        {
            Transform scaleTransform = transform.GetChild(0);
            scale.z = length;
            scaleTransform.localScale = scale;
            scaleTransform.hasChanged = true;
            transform.hasChanged = true;
            if (master && part.symmetryCounterparts.Count != 0)
                updateCounterparts();
        }

        /// <summary>
        /// Ensures the counterparts are updated. Only called if this was elected the master.
        /// </summary>
        private void updateCounterparts()
        {
            int i = 0;
            bool loop = true;
            Part sPart;
            GlowStripX mod;
            while (loop)
            {
                if (i > part.symmetryCounterparts.Count - 1)
                    break;
                if (part.symmetryCounterparts[i])
                {
                    sPart = part.symmetryCounterparts[i];
                    mod = sPart.Modules.OfType<GlowStripX>().FirstOrDefault();

                    if (mod.length != this.length)
                    {
                        mod.length = this.length;
                        mod.throttleBound = this.throttleBound;
                        mod.time = this.time;
                        mod.timeScale = this.timeScale;
                        mod.checkLengthValue();
                        mod.updateScale();
                    }
                    i++;
                }
                else
                {
                    i++;

                    if (part.symmetryCounterparts[i])
                    {
                        Invoke("updateCounterparts", 0.01f);
                    }
                    else
                    {
                        loop = false;
                    }

                }
            }
        }

        #endregion

    }
}
