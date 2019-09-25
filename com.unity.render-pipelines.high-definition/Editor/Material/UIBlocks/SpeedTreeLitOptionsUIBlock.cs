using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class SpeedTreeLitOptionsUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string SpeedTreeHeader = "SpeedTree Options";

            public static GUIContent typeText = new GUIContent("Geometry Type", "Which type of tree geometry component is the part being shaded");
            public static GUIContent enableWindText = new GUIContent("Enable Wind", "Whether you want the wind effect to be on or not");
            public static GUIContent windQualityText = new GUIContent("Wind Quality", "Detail level of the wind effect");
            public static GUIContent billboardText = new GUIContent("Billboard", "This surface is a billboard");
            public static GUIContent billboardFacingText = new GUIContent("Billboard Camera Facing", "Factor which affects billboard's impact on shadows");
        }

        Expandable m_ExpandableBit;

        MaterialProperty geomType = null;
        const string kGeomType = "_SpeedTreeGeom";

        MaterialProperty windEnable = null;
        const string kWindEnable = "_WindEnabled";
        MaterialProperty windQuality = null;
        const string kWindQuality = "_WindQuality";
        const string kWindQuality8 = "_WindQualityVer8";
        MaterialProperty isBillboard = null;

        const string kIsBillboard = "_Billboard";
        MaterialProperty billboardFacesCam = null;
        const string kBillboardFacing = "_BillboardFacing";

        public SpeedTreeLitOptionsUIBlock(Expandable expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        public override void LoadMaterialProperties()
        {
            geomType = FindProperty(kGeomType);

            windEnable = FindProperty(kWindEnable);
            windQuality = FindProperty(kWindQuality);
            if (windQuality == null)
                windQuality = FindProperty(kWindQuality8);

            isBillboard = FindProperty(kIsBillboard);
            billboardFacesCam = FindProperty(kBillboardFacing);
        }

        void DrawSpeedTreeInputsGUI()
        {
            if (geomType != null)
                materialEditor.ShaderProperty(geomType, Styles.typeText);

            if (windEnable != null && windQuality != null)
            {
                materialEditor.ShaderProperty(windEnable, Styles.enableWindText);
                materialEditor.ShaderProperty(windQuality, Styles.windQualityText);
            }

            if (isBillboard != null)
            {
                materialEditor.ShaderProperty(isBillboard, Styles.billboardText);
                if (billboardFacesCam != null)
                {
                    materialEditor.ShaderProperty(billboardFacesCam, Styles.billboardFacingText);
                }
            }
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.SpeedTreeHeader, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                    DrawSpeedTreeInputsGUI();
            }
        }
    }
}
