﻿/*
The MIT License (MIT)

Copyright (c) 2016 Boris-Barboris

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in this Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using KSP.IO;
using ClickThroughFix;


namespace ResonantOrbitCalculator
{
    public class GraphWindow
    {
        public const int wnd_width = 650;
        public const int wnd_height = 500;

        static public Rect wnd_rect = new Rect(100.0f, 100.0f, wnd_width, wnd_height);
        public bool shown = false;
        static PluginConfiguration conf;

        static bool init_gui = false;

        GUIStyle winStyle;
        static GUIStyle normalLabel;
        static GUIStyle warningLabel;
        static GUIStyle headerLabel;

        static GUIStyle toggleMinLOSWarning;
        static GUIStyle toggleMinLOSNormal;
        static GUIStyle labelResonantOrbit;


        //public bool autoUpdate;
        public PlanetSelection planetSelection = null;

        internal bool saveScreen = false;
        public void Start()
        {
            if (ResonantOrbitCalculator.Instance.testlastSelectedPlanet == ResonantOrbitCalculator.unSelected)
                ResonantOrbitCalculator.Instance.testlastSelectedPlanet = FlightGlobals.GetHomeBody().name;
            PlanetSelection.setSelectedBody(ResonantOrbitCalculator.Instance.testlastSelectedPlanet);
            GUI.color = new Color(0.85f, 0.85f, 0.85f, 1);

            winStyle = new GUIStyle(HighLogic.Skin.window);
            winStyle.active.background = winStyle.normal.background;
            Texture2D tex = winStyle.normal.background; //.CreateReadable();

            var pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i].a = 255;

            tex.SetPixels32(pixels);
            tex.Apply();

            winStyle.active.background = tex;
            winStyle.focused.background = tex;
            winStyle.normal.background = tex;            
        }


        string tooltip = "";
        bool drawTooltip = true;
        // Vector2 mousePosition;
        Vector2 tooltipSize;
        float tooltipX, tooltipY;
        Rect tooltipRect;
        void SetupTooltip()
        {
            Vector2 mousePosition;
            mousePosition.x = Input.mousePosition.x;
            mousePosition.y = Screen.height - Input.mousePosition.y;
            if (tooltip != null && tooltip.Trim().Length > 0)
            {
                tooltipSize = HighLogic.Skin.label.CalcSize(new GUIContent(tooltip));
                tooltipX = (mousePosition.x + tooltipSize.x > Screen.width) ? (Screen.width - tooltipSize.x) : mousePosition.x;
                tooltipY = mousePosition.y;
                if (tooltipX < 0) tooltipX = 0;
                if (tooltipY < 0) tooltipY = 0;
                tooltipRect = new Rect(tooltipX - 1, tooltipY - tooltipSize.y, tooltipSize.x + 4, tooltipSize.y);
            }
        }

        void TooltipWindow(int id)
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<CCOLParams>().tooltips)
                GUI.Label(new Rect(2, 0, tooltipRect.width - 2, tooltipRect.height), tooltip, HighLogic.Skin.label);
        }


        public void OnGUI()
        {
            if (!init_gui)
            {
                init_gui = true;

                normalLabel = new GUIStyle(GUI.skin.label);

                warningLabel = new GUIStyle(GUI.skin.label);
                warningLabel.normal.textColor = Color.red;
                warningLabel.normal.background = new Texture2D(2, 2);

                headerLabel = new GUIStyle(GUI.skin.label);
                headerLabel.normal.textColor = Color.white;

                int size = 4; ;
                Color[] pix = new Color[size];
                for (int i = 0; i < size; i++)
                    pix[i] = Color.yellow;
                warningLabel.normal.background.SetPixels(pix);
                warningLabel.normal.background.Apply();

                toggleMinLOSWarning = new GUIStyle(GUI.skin.toggle);
                toggleMinLOSWarning.normal.textColor = Color.red;

                toggleMinLOSNormal = new GUIStyle(GUI.skin.toggle);
                toggleMinLOSNormal.normal.textColor = Color.cyan;

                labelResonantOrbit = new GUIStyle(GUI.skin.label);
                labelResonantOrbit.normal.textColor = Color.green;
                labelResonantOrbit.fontStyle = FontStyle.Bold;
            }
            EditorLogic editorlogic = EditorLogic.fetch;
            if (shown)
            {
                if (drawTooltip /* && HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().buttonTooltip*/ && tooltip != null && tooltip.Trim().Length > 0)
                {
                    SetupTooltip();
                    ClickThruBlocker.GUIWindow(1234, tooltipRect, TooltipWindow, "");
                }

                wnd_rect = ClickThruBlocker.GUILayoutWindow(54665949, wnd_rect, _drawGUI, "Resonant Orbit Calculator", winStyle);
            }

        }

        public string sNumSats = "3";
        public string sOrbitAltitude = "";
        static public bool synchronousOrbit = false;
        static public bool minLOSorbit = false;
        static public bool showLOSlines = false;
        static public bool occlusionModifiers = false;
        public string sAtmOcclusion = "0.75";
        public string sVacOcclusion = "0.9";
        static public double orbitAltitude;
        static public int numSats = 3;
        static public double atmOcclusion = 0.75f;
        static public double vacOcclusion = 0.9f;

        static public bool bValidNumSats = true;
        static public bool bValidOrbitAltitude = true;
        static public bool bValidAtmOcclusion = true;
        static public bool bValidVacOcclusion = true;

        static public bool flipOrbit = false;

        double dTmp;
        int iTmp;
        bool firstTime = true;

        void _drawGUI(int id)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(wnd_width));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(GUILayout.Width(wnd_width));
            GUILayout.BeginVertical(GUILayout.Width(GRAPH_WIDTH + 10));
            // draw graph box
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(OrbitCalc.header[0]);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(OrbitCalc.header[1]);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Box(graph_texture);
            GUILayout.EndVertical();


            // draw side text
            GUILayout.BeginVertical(GUILayout.Width(wnd_width - GRAPH_WIDTH - 30));
            bool draw = false;

            if (firstTime)
            {
                firstTime = false;
                draw = true;
            }
            // if (!PlanetSelection.isActive)
            {
                if (GUILayout.Button("Select Planet"))
                {
                    if (!PlanetSelection.isActive)
                        planetSelection = new GameObject().AddComponent<PlanetSelection>();
                    else
                        planetSelection.DestroyThis();
                }
            }
            //autoUpdate = GUILayout.Toggle(autoUpdate, new GUIContent("Auto-update", "Update the graph after any change"));

            GUILayout.BeginHorizontal();

            GUILayout.Label(new GUIContent("Number of satellites:", "Total number of satellites to arrange"));

            sNumSats = GUILayout.TextField(sNumSats);
            int butW = 19;
            if (GUILayout.Button("^", GUILayout.Width(butW)))
            {
                numSats++;
                sNumSats = numSats.ToString();
                draw = true;
            }
            if (GUILayout.Button("v", GUILayout.Width(butW)))
            {
                if (numSats > 1)
                    numSats--;
                sNumSats = numSats.ToString();
                draw = true;
            }

            bValidNumSats = int.TryParse(sNumSats, out iTmp);
            if (!bValidNumSats)
                sNumSats = numSats.ToString();
            else
                numSats = iTmp;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Altitude:", "Orbital altitude"));
            string newsOrbitAltitude = GUILayout.TextField(sOrbitAltitude);
            if (newsOrbitAltitude != sOrbitAltitude)
            {
                bValidOrbitAltitude = double.TryParse(newsOrbitAltitude, out dTmp);
                if (bValidOrbitAltitude)
                    orbitAltitude = dTmp;
                sOrbitAltitude = orbitAltitude.ToString("F0");
                draw = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Orbital Period: ");
            GUILayout.Label(OrbitCalc.period);
            GUILayout.EndHorizontal();
            if (OrbitCalc.synchrorbit == "" || OrbitCalc.synchrorbit == "n/a")
                GUI.enabled = false;
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(synchronousOrbit, new GUIContent("Synchronous orbit (" + OrbitCalc.synchrorbit + ")", "Set the altitude to have the satellites be in a geosynchronous orbit")))
            {
                orbitAltitude = OrbitCalc.body.geoAlt;
                sOrbitAltitude = orbitAltitude.ToString();
                draw = true;
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (OrbitCalc.losorbit == "" || OrbitCalc.losorbit == "n/a")
                GUI.enabled = false;

            if (GUILayout.Toggle(minLOSorbit, new GUIContent("Minimum LOS orbit (" + OrbitCalc.losorbit + ")", "Set the altitude to the minimum altitude possible to maintain a Line of Sight"),
                OrbitCalc.losOrbitWarning ? toggleMinLOSWarning : toggleMinLOSNormal))
            {

                orbitAltitude = OrbitCalc.minLOS;
                sOrbitAltitude = orbitAltitude.ToString();
                draw = true;
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            bool newshowLOSlines = GUILayout.Toggle(showLOSlines, new GUIContent("Show LOS lines", "Show the Line Of Sight lines"));
            if (newshowLOSlines != showLOSlines)
            {
                draw = true;
                showLOSlines = newshowLOSlines;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            bool newocclusionModifiers = GUILayout.Toggle(occlusionModifiers, new GUIContent("Occlusion modifiers", "Enable occlusion modifiers for vacuum and atmospheres"));
            GUILayout.EndHorizontal();
            if (occlusionModifiers)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("   Atm:", "Occlusion atmospheric modifier"));
                var newsAtmOcclusion = GUILayout.TextField(sAtmOcclusion);
                if (GUILayout.Button("^", GUILayout.Width(butW)))
                {
                    if (atmOcclusion < 1.1)
                        atmOcclusion += 0.01f;
                    sAtmOcclusion = atmOcclusion.ToString("F2");
                    newsAtmOcclusion = sAtmOcclusion;
                    draw = true;
                }
                if (GUILayout.Button("v", GUILayout.Width(butW)))
                {
                    if (atmOcclusion > 0)
                        atmOcclusion -= 0.01f;
                    sAtmOcclusion = atmOcclusion.ToString("F2");
                    newsAtmOcclusion = sAtmOcclusion;
                    draw = true;
                }
                if (newsAtmOcclusion != sAtmOcclusion)
                {
                    bValidAtmOcclusion = double.TryParse(newsAtmOcclusion, out dTmp);
                    if (!bValidAtmOcclusion)
                        sAtmOcclusion = atmOcclusion.ToString("F2");
                    else
                    {
                        atmOcclusion = dTmp;
                        sAtmOcclusion = newsAtmOcclusion;
                        draw = true;
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("   Vac:", "Occlusion vacuum modifier"));
                var newsVacOcclusion = GUILayout.TextField(sVacOcclusion);
                if (GUILayout.Button("^", GUILayout.Width(butW)))
                {
                    if (vacOcclusion < 1.1)
                        vacOcclusion += 0.01f;
                    sVacOcclusion = vacOcclusion.ToString("F2");
                    newsVacOcclusion = sVacOcclusion;
                    draw = true;
                }
                if (GUILayout.Button("v", GUILayout.Width(butW)))
                {
                    if (vacOcclusion > 0)
                        vacOcclusion -= 0.01f;
                    sVacOcclusion = vacOcclusion.ToString("F2");
                    newsVacOcclusion = sVacOcclusion;
                    draw = true;
                }
                if (newsVacOcclusion != sVacOcclusion)
                {
                    bValidVacOcclusion = Double.TryParse(newsVacOcclusion, out dTmp);
                    if (!bValidVacOcclusion)
                        sVacOcclusion = vacOcclusion.ToString("F2");
                    else
                    {
                        vacOcclusion = dTmp;
                        sVacOcclusion = newsVacOcclusion;
                        draw = true;
                    }
                }
                GUILayout.EndHorizontal();
            }
            if (occlusionModifiers != newocclusionModifiers)
                draw = true;
            occlusionModifiers = newocclusionModifiers;

            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Resonant Orbit", labelResonantOrbit);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool newflipOrbit = GUILayout.Toggle(flipOrbit, new GUIContent("Dive orbit", "Set Carrier orbit to be lower than target orbit"));
            if (newflipOrbit != flipOrbit)
            {
                draw = true;
                flipOrbit = newflipOrbit;
            }
            GUILayout.EndHorizontal();

            if (draw)
            {
                UpdateGraph();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Orbital Period: ");
            GUILayout.Label(OrbitCalc.carrierT);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Apoapsis: ");
            GUILayout.Label(OrbitCalc.carrierAp);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Periapsis: ");

            if (OrbitCalc.carrierPe != "")
                GUILayout.Label(OrbitCalc.carrierPe, OrbitCalc.carrierPeWarning ? warningLabel : normalLabel);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Injection Δv: ");
            GUILayout.Label(OrbitCalc.burnDV);
            GUILayout.EndHorizontal();


            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Save Window", "Saves an image of the window to the Screenshots directory") ))
            {
                saveScreen = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint && GUI.tooltip != tooltip)
                tooltip = GUI.tooltip;

            GUI.DragWindow();

        }


        internal void UpdateGraph()
        {

            init_textures();
            OrbitCalc.Update();
            chart.drawchart(OrbitCalc.satelliteorbit, OrbitCalc.carrierorbit, OrbitCalc.body);

            graph_texture.Apply();

        }

        public void save_settings()
        {
            if (conf == null)
                conf = PluginConfiguration.CreateForType<ResonantOrbitCalculator>();
            Debug.Log("[ResonantOrbitCalculator]: serializing");
            if (wnd_rect != null)
            {
                conf.SetValue("x", wnd_rect.x.ToString());
                conf.SetValue("y", wnd_rect.y.ToString());
            }

            conf.save();

        }

        public void load_settings()
        {
            if (conf == null)
                conf = PluginConfiguration.CreateForType<ResonantOrbitCalculator>();
            try
            {
                conf.load();
                Debug.Log("[ResonantOrbitCalculator]: deserializing");
                wnd_rect.x = float.Parse(conf.GetValue<string>("x"));
                wnd_rect.y = float.Parse(conf.GetValue<string>("y"));
            }
            catch (Exception) { }
        }

        public const int GRAPH_WIDTH = 500;
        public const int GRAPH_HEIGHT = 500;
        public const int HALF = GRAPH_WIDTH / 2;
        public static int MAX_DIST = 354; // (int)Math.Sqrt(2f * HALF * HALF);

        static internal Texture2D graph_texture = new Texture2D(GRAPH_WIDTH, GRAPH_HEIGHT, TextureFormat.RGB24, false, true);

        Color fillcolor = new Color(238f / 255f, 238f / 255f, 238f / 255f);
        static Color[] arr = null;
        public void init_textures(bool apply = false)
        {
            MonoBehaviour.Destroy(graph_texture);
            graph_texture = new Texture2D(GRAPH_WIDTH, GRAPH_HEIGHT, TextureFormat.RGB24, false, true);

            if (arr == null)
            {
                arr = new Color[GRAPH_HEIGHT * GRAPH_WIDTH];
                for (int i = 0; i < arr.Length; i++)
                    arr[i] = fillcolor;
            }
            graph_texture.SetPixels(arr);

            if (apply)
            {
                graph_texture.Apply();
            }
        }
    }
}