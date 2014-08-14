﻿/* Name:    KerbalGraph (Graph GUI Plugin)
 * Version: 1.3   (KSP 0.22+)
 * Copyright 2014, Michael Ferrara, a.k.a. Ferram4 and Zachary Jordan, a.k.a. Zarenor Darkstalker.
 * 
 * This file is part of KerbalGraph.    
 * KerbalGraph is free software: you can redistribute it and/or modify    
 * it under the terms of the GNU General Public License as published by    
 * the Free Software Foundation, either version 3 of the License, or    
 * (at your option) any later version.
 *     
 * KerbalGraph is distributed in the hope that it will be useful,    
 * but WITHOUT ANY WARRANTY; without even the implied warranty of    
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the    
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License    
 * along with KerbalGraph.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * KerbalGraph is derived from FerramGraph, also released under the GPLv3, and written by Michael Ferrara, a.k.a. Ferram4.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GlowStrips
{
    public class KerbalGraph
    {
        class KerbalGraphLine
        {
            private Texture2D lineDisplay;
            private Texture2D lineLegend;
            public bool displayInLegend;
            private double[] rawDataX = new double[1];
            private double[] rawDataY = new double[1];
            private int[] pixelDataX = new int[1];
            private int[] pixelDataY = new int[1];
            private Vector4d bounds;
            public int lineThickness;
            public Color lineColor = new Color();
            public Color backgroundColor = new Color();
            private double verticalScaling;
            private double horizontalScaling;

            #region Constructor

            public KerbalGraphLine(int width, int height)
            {
                lineDisplay = new Texture2D(width, height, TextureFormat.ARGB32, false);
                SetBoundaries(new Vector4(0, 1, 0, 1));
                lineThickness = 1;
                lineColor = Color.red;
                verticalScaling = 1;
                horizontalScaling = 1;
            }

            #endregion

            #region InputData


            public void InputData(double[] xValues, double[] yValues)
            {
                int elements = xValues.Length;
                rawDataX = new double[elements];
                rawDataY = new double[elements];

                for (int i = 0; i < elements; i++)
                {
                    if (double.IsNaN(xValues[i]))
                    {
                        xValues[i] = 0;
                        MonoBehaviour.print("Warning: NaN in xValues array; value set to zero");
                    }
                    if (double.IsNaN(yValues[i]))
                    {
                        yValues[i] = 0;
                        MonoBehaviour.print("Warning: NaN in yValues array; value set to zero");
                    }
                }

                rawDataX = xValues;
                rawDataY = yValues;
                ConvertRawToPixels();
            }
            #endregion

            #region ConvertRawToPixels

            private void ConvertRawToPixels()
            {
                pixelDataX = new int[rawDataX.Length];
                pixelDataY = new int[rawDataY.Length];

                double xScaling = lineDisplay.width / (bounds.y - bounds.x);
                double yScaling = lineDisplay.height / (bounds.w - bounds.z);
                double tmpx, tmpy;

                for (int i = 0; i < rawDataX.Length; i++)
                {
                    tmpx = rawDataX[i] * horizontalScaling;
                    tmpy = rawDataY[i] * verticalScaling;

                    tmpx -= bounds.x;
                    tmpx *= xScaling;

                    tmpy -= bounds.z;
                    tmpy *= yScaling;

                    tmpx = Math.Round(tmpx);
                    tmpy = Math.Round(tmpy);

                    //                    MonoBehaviour.print("x: " + tmpx.ToString() + " y: " + tmpy.ToString());
                    pixelDataX[i] = (int)tmpx;
                    pixelDataY[i] = (int)tmpy;
                }
                Update();
            }

            #endregion

            public void SetBoundaries(Vector4 boundaries)
            {
                bounds = boundaries;
                if (rawDataX.Length > 0)
                {
                    ConvertRawToPixels();
                }
            }

            public void Update()
            {
                ClearLine();
                int lastx = -1;
                int lasty = -1;
                if (lineThickness < 1)
                    lineThickness = 1;

                for (int k = 0; k < pixelDataX.Length; k++)
                {
                    int tmpx = pixelDataX[k];
                    int tmpy = pixelDataY[k];
                    if (lastx >= 0)
                    {
                        int tmpThick = lineThickness - 1;
                        int xstart = Math.Min(tmpx, lastx);
                        int xend = Math.Max(tmpx, lastx);
                        int ystart;
                        int yend;

                        if (xstart == tmpx)
                        {
                            ystart = tmpy;
                            yend = lasty;
                        }
                        else
                        {
                            ystart = lasty;
                            yend = tmpy;
                        }

                        double m = ((double)yend - (double)ystart) / ((double)xend - (double)xstart);
                        if (Math.Abs(m) <= 1 && (xstart != xend))
                        {
                            for (int i = xstart; i < xend; i++)
                                for (int j = -tmpThick; j <= tmpThick; j++)
                                {
                                    int linear = (int)Math.Round(m * (i - xend) + yend);
                                    if ((i >= 0 && i <= lineDisplay.width) && (linear + j >= 0 && linear + j <= lineDisplay.height))
                                        lineDisplay.SetPixel(i, linear + j, lineColor);
                                }
                        }
                        else
                        {
                            ystart = Math.Min(tmpy, lasty);
                            yend = Math.Max(tmpy, lasty);

                            if (ystart == tmpy)
                            {
                                xstart = tmpx;
                                xend = lastx;
                            }
                            else
                            {
                                xstart = lastx;
                                xend = tmpx;
                            }

                            m = 1 / m;

                            for (int i = ystart; i < yend; i++)
                                for (int j = -tmpThick; j <= tmpThick; j++)
                                {
                                    int linear = (int)Math.Round(m * (i - yend) + xend);
                                    if ((linear + j >= 0 && linear + j <= lineDisplay.width) && (i >= 0 && i <= lineDisplay.height))
                                        lineDisplay.SetPixel(linear + j, i, lineColor);
                                }

                        }
                    }
                    lastx = tmpx;
                    lasty = tmpy;
                }
                lineDisplay.Apply();
                UpdateLineLegend();

            }

            private void UpdateLineLegend()
            {
                lineLegend = new Texture2D(25, 15, TextureFormat.ARGB32, false);
                for (int i = 0; i < lineLegend.width; i++)
                    for (int j = 0; j < lineLegend.height; j++)
                    {
                        if (Mathf.Abs((int)(j - (lineLegend.height / 2f))) < lineThickness)
                            lineLegend.SetPixel(i, j, lineColor);
                        else
                            lineLegend.SetPixel(i, j, backgroundColor);
                    }
                lineLegend.Apply();
            }

            private void ClearLine()
            {
                for (int i = 0; i < lineDisplay.width; i++)
                    for (int j = 0; j < lineDisplay.height; j++)
                        lineDisplay.SetPixel(i, j, new Color(0, 0, 0, 0));
                lineDisplay.Apply();
            }

            /// <summary>
            /// XMin, XMax, YMin, YMax
            /// </summary>
            public Vector4d GetExtremeData()
            {
                Vector4d extremes = Vector4d.zero;
                extremes.x = rawDataX.Min();
                extremes.y = rawDataX.Max();
                extremes.z = rawDataY.Min();
                extremes.w = rawDataY.Max();

                return extremes;
            }

            public Texture2D Line()
            {
                return lineDisplay;
            }

            public Texture2D LegendImage()
            {
                return lineLegend;
            }

            public void UpdateVerticalScaling(double scaling)
            {
                verticalScaling = scaling;
                ConvertRawToPixels();
            }
            public void UpdateHorizontalScaling(double scaling)
            {
                horizontalScaling = scaling;
                ConvertRawToPixels();
            }

            public void ClearTextures()
            {
                GameObject.Destroy(lineLegend);
                GameObject.Destroy(lineDisplay);
                lineDisplay = null;
                lineLegend = null;
            }
        }



        protected Texture2D graph;

        /// <summary>
        /// The rectangle the graph is calculated ('drawn') onto. NOT necessarily the same size as displayed.
        /// </summary>
        protected Rect drawRect = new Rect(0, 0, 0, 0);

        /// <summary>
        /// The rectangle used for displaying the graph. NOT necessarily the same as the one the graph was drawn to.
        /// </summary>
        private Rect displayRect = new Rect(0, 0, 0, 0);

        private bool refreshFlag = true;


        private Dictionary<string, KerbalGraphLine> allLines = new Dictionary<string, KerbalGraphLine>();

        private Vector4d bounds;
        public bool autoscale = false;

        public Color backgroundColor = Color.black;
        public Color gridColor = new Color(0.42f, 0.35f, 0.11f, 1);
        public Color axisColor = Color.white;

        private string leftBound;
        private string rightBound;
        private string topBound;
        private string bottomBound;
        public string horizontalLabel = "Axis Label Here";
        public string verticalLabel = "Axis Label Here";
        private Vector2 ScrollView = Vector2.zero;

        #region Constructors
        public KerbalGraph(int width, int height)
        {
            graph = new Texture2D(width, height, TextureFormat.ARGB32, false);
            SetBoundaries(0, 1, 0, 1);
            drawRect = new Rect(1, 1, graph.width, graph.height);
            GridInit();
        }

        public KerbalGraph(int width, int height, double minx, double maxx, double miny, double maxy)
        {
            graph = new Texture2D(width, height, TextureFormat.ARGB32, false);
            SetBoundaries(minx, maxx, miny, maxy);
            drawRect = new Rect(1, 1, graph.width, graph.height);
            GridInit();
        }
        #endregion

        #region Scaling Functions
        public void SetBoundaries(double minx, double maxx, double miny, double maxy)
        {
            bounds.x = minx;
            bounds.y = maxx;
            bounds.z = miny;
            bounds.w = maxy;
            SetBoundaries(bounds);
        }

        public void SetBoundaries(Vector4d boundaries)
        {
            bounds = boundaries;
            leftBound = bounds.x.ToString();
            rightBound = bounds.y.ToString();
            topBound = bounds.w.ToString();
            bottomBound = bounds.z.ToString();
            foreach (KeyValuePair<string, KerbalGraphLine> pair in allLines)
                pair.Value.SetBoundaries(bounds);
        }


        public void SetGridScaleUsingPixels(int gridWidth, int gridHeight)
        {
            GridInit(gridWidth, gridHeight);
            Update();
        }

        public void SetGridScaleUsingValues(double gridWidth, double gridHeight)
        {
            int pixelWidth, pixelHeight;

            pixelWidth = (int)Math.Round(((gridWidth * drawRect.width) / (bounds.y - bounds.x)));
            pixelHeight = (int)Math.Round(((gridHeight * drawRect.height) / (bounds.w - bounds.z)));

            if (pixelWidth <= 1)
            {
                pixelWidth = 5;
                Debug.Log("Warning! Grid width scale too fine for scaling; picking safe alternative");
            }
            if (pixelHeight <= 1)
            {
                pixelHeight = 5;
                Debug.Log("Warning! Grid height scale too fine for scaling; picking safe alternative");
            }

            SetGridScaleUsingPixels(pixelWidth, pixelHeight);


        }

        public void SetLineVerticalScaling(string lineName, double scaling)
        {
            if (!allLines.ContainsKey(lineName))
            {
                MonoBehaviour.print("Error: No line with that name exists");
                return;
            }
            KerbalGraphLine line;

            allLines.TryGetValue(lineName, out line);

            line.UpdateVerticalScaling(scaling);
        }


        public void SetLineHorizontalScaling(string lineName, double scaling)
        {
            if (!allLines.ContainsKey(lineName))
            {
                MonoBehaviour.print("Error: No line with that name exists");
                return;
            }
            KerbalGraphLine line;

            allLines.TryGetValue(lineName, out line);

            line.UpdateHorizontalScaling(scaling);
        }

        #endregion

        #region GridInit

        private void GridInit()
        {
            int squareSize = 25;
            GridInit(squareSize, squareSize);
        }


        private void GridInit(int widthSize, int heightSize)
        {

            int horizontalAxis, verticalAxis;

            horizontalAxis = (int)Math.Round(-bounds.x * drawRect.width / (bounds.y - bounds.x));
            verticalAxis = (int)Math.Round(-bounds.z * drawRect.height / (bounds.w - bounds.z));

            for (int i = 0; i < graph.width; i++)
            {
                for (int j = 0; j < graph.height; j++)
                {

                    Color grid = new Color(0.42f, 0.35f, 0.11f, 1);
                    if (i - horizontalAxis == 0 || j - verticalAxis == 0)
                        graph.SetPixel(i, j, axisColor);
                    else if ((i - horizontalAxis) % widthSize == 0 || (j - verticalAxis) % heightSize == 0)
                        graph.SetPixel(i, j, gridColor);
                    else
                        graph.SetPixel(i, j, backgroundColor);
                }
            }

            graph.Apply();
        }
        #endregion

        #region Add / Remove Line Functions

        public void AddLine(string lineName)
        {
            if (allLines.ContainsKey(lineName))
            {
                MonoBehaviour.print("Error: A Line with that name already exists");
                return;
            }
            KerbalGraphLine newLine = new KerbalGraphLine((int)drawRect.width, (int)drawRect.height);
            newLine.SetBoundaries(bounds);
            allLines.Add(lineName, newLine);
            Update();
        }

        public void AddLine(string lineName, double[] xValues, double[] yValues)
        {
            int lineThickness = 1;
            AddLine(lineName, xValues, yValues, lineThickness);
        }

        public void AddLine(string lineName, double[] xValues, double[] yValues, Color lineColor)
        {
            int lineThickness = 1;
            AddLine(lineName, xValues, yValues, lineColor, lineThickness);
        }

        public void AddLine(string lineName, double[] xValues, double[] yValues, int lineThickness)
        {
            Color lineColor = Color.red;
            AddLine(lineName, xValues, yValues, lineColor, lineThickness);
        }

        public void AddLine(string lineName, double[] xValues, double[] yValues, Color lineColor, int lineThickness)
        {
            AddLine(lineName, xValues, yValues, lineColor, lineThickness, true);

        }

        public void AddLine(string lineName, double[] xValues, double[] yValues, Color lineColor, int lineThickness, bool display)
        {
            if (allLines.ContainsKey(lineName))
            {
                MonoBehaviour.print("Error: A Line with that name already exists");
                return;
            }
            if (xValues.Length != yValues.Length)
            {
                MonoBehaviour.print("Error: X and Y value arrays are different lengths");
                return;
            }

            KerbalGraphLine newLine = new KerbalGraphLine((int)drawRect.width, (int)drawRect.height);
            newLine.InputData(xValues, yValues);
            newLine.SetBoundaries(bounds);
            newLine.lineColor = lineColor;
            newLine.lineThickness = lineThickness;
            newLine.backgroundColor = backgroundColor;
            newLine.displayInLegend = display;

            allLines.Add(lineName, newLine);
            Update();
        }

        public void RemoveLine(string lineName)
        {
            if (!allLines.ContainsKey(lineName))
            {
                MonoBehaviour.print("Error: No line with that name exists");
                return;
            }

            KerbalGraphLine line = allLines[lineName];
            allLines.Remove(lineName);

            line.ClearTextures();
            Update();

        }

        public void Clear()
        {
            foreach (KeyValuePair<string, KerbalGraphLine> line in allLines)
            {
                line.Value.ClearTextures();
            }
            allLines.Clear();
            Update();
        }

        #endregion

        #region Update Data Functions

        public void UpdateLineData(string lineName, double[] xValues, double[] yValues)
        {
            if (xValues.Length != yValues.Length)
            {
                MonoBehaviour.print("Error: X and Y value arrays are different lengths");
                return;
            }

            KerbalGraphLine line;

            if (allLines.TryGetValue(lineName, out line))
            {

                line.InputData(xValues, yValues);

                allLines.Remove(lineName);
                allLines.Add(lineName, line);
                Update();
            }
            else
                MonoBehaviour.print("Error: No line with this name exists");

        }

        #endregion


        #region Update Visual Functions
        /// <summary>
        /// Use this to update the graph display
        /// </summary>
        public void Update()
        {
            #region Autoscaling
            if (autoscale)
            {
                Vector4d extremes = Vector4.zero;
                bool init = false;
                foreach (KeyValuePair<string, KerbalGraphLine> pair in allLines)
                {
                    Vector4d tmp = pair.Value.GetExtremeData();

                    if (!init)
                    {
                        extremes.x = tmp.x;
                        extremes.y = tmp.y;
                        extremes.z = tmp.z;
                        extremes.w = tmp.w;
                        init = true;
                    }
                    else
                    {
                        extremes.x = Math.Min(extremes.x, tmp.x);
                        extremes.y = Math.Max(extremes.y, tmp.y);
                        extremes.z = Math.Min(extremes.z, tmp.z);
                        extremes.w = Math.Max(extremes.w, tmp.w);

                    }

                    extremes.x = Math.Floor(extremes.x);
                    extremes.y = Math.Ceiling(extremes.y);
                    extremes.z = Math.Floor(extremes.z);
                    extremes.w = Math.Ceiling(extremes.w);
                }
                SetBoundaries(extremes);
            }
            #endregion
            foreach (KeyValuePair<string, KerbalGraphLine> pair in allLines)
            {
                pair.Value.backgroundColor = backgroundColor;
                pair.Value.Update();
            }

        }

        public void LineColor(string lineName, Color newColor)
        {
            KerbalGraphLine line;
            if (allLines.TryGetValue(lineName, out line))
            {
                line.lineColor = newColor;

                allLines.Remove(lineName);
                allLines.Add(lineName, line);

            }
        }

        public void LineThickness(string lineName, int thickness)
        {
            KerbalGraphLine line;
            if (allLines.TryGetValue(lineName, out line))
            {
                line.lineThickness = Mathf.Clamp(thickness, 1, 6);

                allLines.Remove(lineName);
                allLines.Add(lineName, line);

            }
        }

        //
        private void modifyDisplayRect(Rect newDisplayRect)
        {
            if (newDisplayRect != displayRect && newDisplayRect.width > 1 && newDisplayRect.height > 1 && refreshFlag)
            {
                displayRect = newDisplayRect;
                Debug.Log("Modified displayRect: " + displayRect);
                refreshFlag = false;
            }



        }

        #endregion



        /// <summary>
        /// This displays the graph.
        /// </summary>
        public void Display(params GUILayoutOption[] options)
        {
            const int axisDisplaySize = 30;
            //const int prespaceX = 20;
            //const int prespaceY = 15;
            const int legendSpacing = 20;

            GUIStyle BackgroundStyle = new GUIStyle(GUI.skin.box);
            BackgroundStyle.hover = BackgroundStyle.active = BackgroundStyle.normal;
            GUIStyle LabelStyle = new GUIStyle(GUI.skin.label);
            LabelStyle.alignment = TextAnchor.UpperCenter;

            Rect r = GUILayoutUtility.GetRect(10, 9999, 10, 9999, options);
            Rect dr = new Rect(0, 0, r.width - 90, r.height - 45);
            modifyDisplayRect(dr);

            int pixelspaceX = (int)displayRect.width / 2 - 102;
            int pixelspaceY = (int)displayRect.height / 2 - 72;

            ScrollView = GUILayout.BeginScrollView(ScrollView, false, false);
            {
                //GUILayout.Space(verticalBorder);
                GUILayout.BeginHorizontal(options);
                {
                    //Vertical axis and labels
                    //GUILayout.Space(prespaceX);
                    GUILayout.BeginVertical(GUILayout.Width(axisDisplaySize), GUILayout.Height(displayRect.height));
                    //GUILayout.BeginArea(new Rect(prespaceX, prespaceY, axisDisplaySize, displayRect.height));
                    {
                        GUILayout.Label(topBound, LabelStyle, GUILayout.Height(20), GUILayout.ExpandWidth(true));
                        GUILayout.Space(pixelspaceY);
                        GUILayout.Label(verticalLabel, LabelStyle, GUILayout.Height(100), GUILayout.ExpandWidth(true));
                        GUILayout.Space(pixelspaceY);
                        GUILayout.Label(bottomBound, LabelStyle, GUILayout.Height(20), GUILayout.ExpandWidth(true));
                    }
                    // GUILayout.EndArea();
                    GUILayout.EndVertical();


                    //Graph itself

                    GUILayout.BeginVertical(GUILayout.Width(displayRect.width), GUILayout.Height(displayRect.height + axisDisplaySize));

                    {
                        //GUILayout.BeginArea(new Rect(prespaceX + axisDisplaySize, prespaceY, displayRect.width, displayRect.height));
                        {
                            r = GUILayoutUtility.GetRect(displayRect.width, displayRect.height);
                            GUI.DrawTexture(r, graph);
                            foreach (KeyValuePair<string, KerbalGraphLine> pair in allLines)
                                GUI.DrawTexture(r, pair.Value.Line());
                        }
                       // GUILayout.EndArea();
                        //Horizontal Axis and Labels

                        GUILayout.BeginHorizontal(GUILayout.Width(displayRect.width));
                        //GUILayout.BeginArea(new Rect(prespaceX + axisDisplaySize, prespaceY + displayRect.height, displayRect.width, axisDisplaySize));
                        {

                            GUILayout.Label(leftBound, LabelStyle, GUILayout.Width(20), GUILayout.ExpandWidth(true));
                            GUILayout.Space(pixelspaceX);
                            GUILayout.Label(horizontalLabel, LabelStyle, GUILayout.Width(160));
                            GUILayout.Space(pixelspaceX);
                            GUILayout.Label(rightBound, LabelStyle, GUILayout.Width(20), GUILayout.ExpandWidth(true));

                        }
                       // GUILayout.EndArea();
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(10);
                    GUILayout.BeginVertical();
                    {
                        //Legend Area

                        int startingSpace = ((int)displayRect.height - allLines.Count * legendSpacing) / 2;
                        GUILayout.Space(startingSpace);
                        foreach (KeyValuePair<string, KerbalGraphLine> pair in allLines)
                        {
                            if (!pair.Value.displayInLegend)
                                continue;
                            GUILayout.BeginHorizontal(GUILayout.Height(15));
                            GUI.DrawTexture(new Rect(1, 1, 25, 15), pair.Value.LegendImage());
                            GUILayout.Label(pair.Key, LabelStyle, GUILayout.Width(35));
                            GUILayout.EndHorizontal();
                            GUILayout.Space(5);
                        }
                    }
                    GUILayout.EndVertical();

                    int rightofarea = (int)displayRect.width + 30;
                    int bottomofarea = (int)displayRect.height + 30;

                    //GUILayout.Space(bottomofarea);
                }
                GUILayout.EndHorizontal();
            }
            //GUILayout.EndArea();
            GUILayout.EndScrollView();

        }

    }
}
