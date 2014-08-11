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

using KSPAPIExtensions;
using System;
using System.Linq;
using UnityEngine;

namespace GlowStrips
{
    /// <summary>
    /// An extension of the GlowStrip class, adding the ability to bind to the throttle, and to dynamically resize.
    /// </summary>
    public class GlowStripAdaptive : GlowStrip
    {
        #region "KSPFields"
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
            base.OnStart(state);
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
        public override void OnGUI()
        {
            base.OnGUI();
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

        #region "Protected Methods"
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
            GlowStripAdaptive mod;
            while (loop)
            {
                if (i > part.symmetryCounterparts.Count - 1)
                    break;
                if (part.symmetryCounterparts[i])
                {
                    sPart = part.symmetryCounterparts[i];
                    mod = sPart.Modules.OfType<GlowStripAdaptive>().FirstOrDefault();

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
