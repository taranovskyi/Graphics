using System;
using System.Reflection;
using System.Linq.Expressions;
using UnityEditor;

//using EditorGUIUtility=UnityEditor.EditorGUIUtility;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(HDRenderPipeline))]
    public class HDRenderPipelineInspector : Editor
    {
        private class Styles
        {
            // Global debug parameters
            public readonly GUIContent debugging = new GUIContent("Debugging");
            public readonly GUIContent debugOverlayRatio = new GUIContent("Overlay Ratio");

            public readonly GUIContent debugParameters = new GUIContent("Debug Parameters");
            public readonly GUIContent debugViewMaterial = new GUIContent("DebugView Material", "Display various properties of Materials.");

            public readonly GUIContent displayOpaqueObjects = new GUIContent("Display Opaque Objects", "Toggle opaque objects rendering on and off.");
            public readonly GUIContent displayTransparentObjects = new GUIContent("Display Transparent Objects", "Toggle transparent objects rendering on and off.");

            public readonly GUIContent useForwardRenderingOnly = new GUIContent("Use Forward Rendering Only");
            public readonly GUIContent useDepthPrepass = new GUIContent("Use Depth Prepass");
            public readonly GUIContent useDistortion = new GUIContent("Use Distortion");        

            public bool isDebugViewMaterialInit = false;
            public GUIContent[] debugViewMaterialStrings = null;
            public int[] debugViewMaterialValues = null;

            public readonly GUIContent skyParams = new GUIContent("Sky Settings");
            public readonly GUIContent sssSettings = new GUIContent("Subsurface Scattering Settings");

            // Shadow Debug
            public readonly GUIContent shadowDebugParameters = new GUIContent("Shadow Debug");
            public readonly GUIContent shadowDebugEnable = new GUIContent("Enable Shadows");
            public readonly GUIContent shadowDebugVisualizationMode = new GUIContent("Visualize");
            public readonly GUIContent shadowDebugVisualizeShadowIndex = new GUIContent("Visualize Shadow Index");

            public readonly GUIContent shadowSettings = new GUIContent("Shadow Settings");
            public readonly GUIContent shadowsAtlasWidth = new GUIContent("Atlas width");
            public readonly GUIContent shadowsAtlasHeight = new GUIContent("Atlas height");

            public readonly GUIContent tileLightLoopSettings = new GUIContent("Tile Light Loop Settings");
            public readonly string[] tileLightLoopDebugTileFlagStrings = new string[] { "Punctual Light", "Area Light", "Env Light"};
            public readonly GUIContent splitLightEvaluation = new GUIContent("Split light and reflection evaluation", "Toggle");
            public readonly GUIContent bigTilePrepass = new GUIContent("Enable big tile prepass", "Toggle");
            public readonly GUIContent clustered = new GUIContent("Enable clustered", "Toggle");
            public readonly GUIContent disableTileAndCluster = new GUIContent("Disable Tile/clustered", "Toggle");
            public readonly GUIContent disableDeferredShadingInCompute = new GUIContent("Disable deferred shading in compute", "Toggle");

            public readonly GUIContent textureSettings = new GUIContent("Texture Settings");

            public readonly GUIContent spotCookieSize = new GUIContent("Spot cookie size");
            public readonly GUIContent pointCookieSize = new GUIContent("Point cookie size");
            public readonly GUIContent reflectionCubemapSize = new GUIContent("Reflection cubemap size");              
        }

        private static Styles s_Styles = null;

        private static Styles styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new Styles();
                return s_Styles;
            }
        }

        SerializedProperty m_ShowDebug = null;
        SerializedProperty m_ShowDebugShadow = null;
        SerializedProperty m_DebugOverlayRatio = null;

        SerializedProperty m_DebugShadowEnabled = null;
        SerializedProperty m_DebugShadowVisualizationMode = null;
        SerializedProperty m_DebugShadowVisualizeShadowIndex = null;

        private void InitializeProperties()
        {
            m_DebugOverlayRatio = FindProperty(x => x.globalDebugParameters.debugOverlayRatio);
            m_ShowDebugShadow = FindProperty(x => x.globalDebugParameters.displayShadowDebug);
            m_ShowDebug = FindProperty(x => x.globalDebugParameters.displayDebug);

            m_DebugShadowEnabled = FindProperty(x => x.globalDebugParameters.shadowDebugParameters.enableShadows);
            m_DebugShadowVisualizationMode = FindProperty(x => x.globalDebugParameters.shadowDebugParameters.visualizationMode);
            m_DebugShadowVisualizeShadowIndex = FindProperty(x => x.globalDebugParameters.shadowDebugParameters.visualizeShadowMapIndex);
        }

        SerializedProperty FindProperty<TValue>(Expression<Func<HDRenderPipeline, TValue>> expr)
        {
            var path = Utilities.GetFieldPath(expr);
            return serializedObject.FindProperty(path);
        }

        string GetSubNameSpaceName(Type type)
        {
            return type.Namespace.Substring(type.Namespace.LastIndexOf((".")) + 1) + "/";
        }

        void FillWithProperties(Type type, GUIContent[] debugViewMaterialStrings, int[] debugViewMaterialValues, bool isBSDFData, string strSubNameSpace, ref int index)
        {
            var attributes = type.GetCustomAttributes(true);
            // Get attribute to get the start number of the value for the enum
            var attr = attributes[0] as GenerateHLSL;

            if (!attr.needParamDefines)
            {
                return ;
            }

            var fields = type.GetFields();

            var localIndex = 0;
            foreach (var field in fields)
            {
                var fieldName = field.Name;

                // Check if the display name have been override by the users
                if (Attribute.IsDefined(field, typeof(SurfaceDataAttributes)))
                {
                    var propertyAttr = (SurfaceDataAttributes[])field.GetCustomAttributes(typeof(SurfaceDataAttributes), false);
                    if (propertyAttr[0].displayName != "")
                    {
                        fieldName = propertyAttr[0].displayName;
                    }
                }

                fieldName = (isBSDFData ? "Engine/" : "") + strSubNameSpace + fieldName;

                debugViewMaterialStrings[index] = new GUIContent(fieldName);
                debugViewMaterialValues[index] = attr.paramDefinesStart + (int)localIndex;
                index++;
                localIndex++;
            }
        }

        void FillWithPropertiesEnum(Type type, GUIContent[] debugViewMaterialStrings, int[] debugViewMaterialValues, string prefix, bool isBSDFData, ref int index)
        {
            var names = Enum.GetNames(type);

            var localIndex = 0;
            foreach (var value in Enum.GetValues(type))
            {
                var valueName = (isBSDFData ? "Engine/" : "" + prefix) + names[localIndex];

                debugViewMaterialStrings[index] = new GUIContent(valueName);
                debugViewMaterialValues[index] = (int)value;
                index++;
                localIndex++;
            }
        }

        static void HackSetDirty(RenderPipelineAsset asset)
        {
            EditorUtility.SetDirty(asset);
            var method = typeof(RenderPipelineAsset).GetMethod("OnValidate", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
                method.Invoke(asset, new object[0]);
        }

        private void DebuggingUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.LabelField(styles.debugging);

            // Global debug parameters
            EditorGUI.indentLevel++;
            m_DebugOverlayRatio.floatValue = EditorGUILayout.Slider(styles.debugOverlayRatio, m_DebugOverlayRatio.floatValue, 0.1f, 1.0f);
            EditorGUILayout.Space();

            DebugParametersUI(renderContext);
            EditorGUILayout.Space();
            ShadowDebugParametersUI(renderContext);

            EditorGUI.indentLevel--;
        }


        private void DebugParametersUI(HDRenderPipeline renderContext)
        {
            m_ShowDebug.boolValue = EditorGUILayout.Foldout(m_ShowDebug.boolValue, styles.debugParameters);
            if (!m_ShowDebug.boolValue)
                return;

            var debugParameters = renderContext.debugParameters;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            if (!styles.isDebugViewMaterialInit)
            {
                var varyingNames = Enum.GetNames(typeof(Attributes.DebugViewVarying));
                var gbufferNames = Enum.GetNames(typeof(Attributes.DebugViewGbuffer));

                // +1 for the zero case
                var num = 1 + varyingNames.Length
                          + gbufferNames.Length
                          + typeof(Builtin.BuiltinData).GetFields().Length * 2 // BuildtinData are duplicated for each material
                          + typeof(Lit.SurfaceData).GetFields().Length
                          + typeof(Lit.BSDFData).GetFields().Length
                          + typeof(Unlit.SurfaceData).GetFields().Length
                          + typeof(Unlit.BSDFData).GetFields().Length;

                styles.debugViewMaterialStrings = new GUIContent[num];
                styles.debugViewMaterialValues = new int[num];

                var index = 0;

                // 0 is a reserved number
                styles.debugViewMaterialStrings[0] = new GUIContent("None");
                styles.debugViewMaterialValues[0] = 0;
                index++;

                FillWithPropertiesEnum(typeof(Attributes.DebugViewVarying), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, GetSubNameSpaceName(typeof(Attributes.DebugViewVarying)), false, ref index);
                FillWithProperties(typeof(Builtin.BuiltinData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, GetSubNameSpaceName(typeof(Lit.SurfaceData)), ref index);
                FillWithProperties(typeof(Lit.SurfaceData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, GetSubNameSpaceName(typeof(Lit.SurfaceData)), ref index);
                FillWithProperties(typeof(Builtin.BuiltinData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, GetSubNameSpaceName(typeof(Unlit.SurfaceData)), ref index);
                FillWithProperties(typeof(Unlit.SurfaceData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, GetSubNameSpaceName(typeof(Unlit.SurfaceData)), ref index);

                // Engine
                FillWithPropertiesEnum(typeof(Attributes.DebugViewGbuffer), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, "", true, ref index);
                FillWithProperties(typeof(Lit.BSDFData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, true, "", ref index);
                FillWithProperties(typeof(Unlit.BSDFData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, true, "", ref index);

                styles.isDebugViewMaterialInit = true;
            }

            debugParameters.debugViewMaterial = EditorGUILayout.IntPopup(styles.debugViewMaterial, (int)debugParameters.debugViewMaterial, styles.debugViewMaterialStrings, styles.debugViewMaterialValues);

            EditorGUILayout.Space();
            debugParameters.displayOpaqueObjects = EditorGUILayout.Toggle(styles.displayOpaqueObjects, debugParameters.displayOpaqueObjects);
            debugParameters.displayTransparentObjects = EditorGUILayout.Toggle(styles.displayTransparentObjects, debugParameters.displayTransparentObjects);
            debugParameters.useForwardRenderingOnly = EditorGUILayout.Toggle(styles.useForwardRenderingOnly, debugParameters.useForwardRenderingOnly);
            debugParameters.useDepthPrepass = EditorGUILayout.Toggle(styles.useDepthPrepass, debugParameters.useDepthPrepass);
            debugParameters.useDistortion = EditorGUILayout.Toggle(styles.useDistortion, debugParameters.useDistortion);

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        private void SkySettingsUI(HDRenderPipeline pipe)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(styles.skyParams);
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;
            pipe.skyParameters = (SkyParameters) EditorGUILayout.ObjectField(new GUIContent("Sky Settings"), pipe.skyParameters, typeof(SkyParameters), false);
            pipe.lightLoopProducer = (LightLoopProducer) EditorGUILayout.ObjectField(new GUIContent("Light Loop"), pipe.lightLoopProducer, typeof(LightLoopProducer), false);
            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(pipe); // Repaint
            }
        }

        private void SssSettingsUI(HDRenderPipeline pipe)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(styles.sssSettings);
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;
            pipe.sssParameters = (SubsurfaceScatteringParameters) EditorGUILayout.ObjectField(new GUIContent("Subsurface Scattering Parameters"), pipe.sssParameters, typeof(SubsurfaceScatteringParameters), false);
            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(pipe); // Repaint
            }
        }

        private void ShadowDebugParametersUI(HDRenderPipeline renderContext)
        {
            m_ShowDebugShadow.boolValue = EditorGUILayout.Foldout(m_ShowDebugShadow.boolValue, styles.shadowDebugParameters);
            if (!m_ShowDebugShadow.boolValue)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DebugShadowEnabled, styles.shadowDebugEnable);
            EditorGUILayout.PropertyField(m_DebugShadowVisualizationMode, styles.shadowDebugVisualizationMode);
            if (!m_DebugShadowVisualizationMode.hasMultipleDifferentValues)
            {
                if ((ShadowDebugMode)m_DebugShadowVisualizationMode.intValue == ShadowDebugMode.VisualizeShadowMap)
                {
                    EditorGUILayout.IntSlider(m_DebugShadowVisualizeShadowIndex, 0, 5/*renderContext.GetCurrentShadowCount() - 1*/, styles.shadowDebugVisualizeShadowIndex);
                }
            }
            EditorGUI.indentLevel--;
        }

        private void ShadowParametersUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();
            var shadowParameters = renderContext.shadowSettings;

            EditorGUILayout.LabelField(styles.shadowSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            shadowParameters.shadowAtlasWidth = Mathf.Max(0, EditorGUILayout.IntField(styles.shadowsAtlasWidth, shadowParameters.shadowAtlasWidth));
            shadowParameters.shadowAtlasHeight = Mathf.Max(0, EditorGUILayout.IntField(styles.shadowsAtlasHeight, shadowParameters.shadowAtlasHeight));

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        private void TextureParametersUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();
            var textureParameters = renderContext.textureSettings;

            EditorGUILayout.LabelField(styles.textureSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            textureParameters.spotCookieSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.spotCookieSize, textureParameters.spotCookieSize), 16, 1024));
            textureParameters.pointCookieSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.pointCookieSize, textureParameters.pointCookieSize), 16, 1024));
            textureParameters.reflectionCubemapSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.reflectionCubemapSize, textureParameters.reflectionCubemapSize), 64, 1024));

            if (EditorGUI.EndChangeCheck())
            {
                renderContext.textureSettings = textureParameters;
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        /*  private void TilePassUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();

            // TODO: we should call a virtual method or something similar to setup the UI, inspector should not know about it
            var tilePass = renderContext.tileSettings;
            if (tilePass != null)
            {
                EditorGUILayout.LabelField(styles.tileLightLoopSettings);
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                tilePass.enableBigTilePrepass = EditorGUILayout.Toggle(styles.bigTilePrepass, tilePass.enableBigTilePrepass);
                tilePass.enableClustered = EditorGUILayout.Toggle(styles.clustered, tilePass.enableClustered);

                if (EditorGUI.EndChangeCheck())
                {
                   HackSetDirty(renderContext); // Repaint

                    // SetAssetDirty will tell renderloop to rebuild
                    renderContext.DestroyCreatedInstances();
                }

                EditorGUI.BeginChangeCheck();

                tilePass.debugViewTilesFlags = EditorGUILayout.MaskField("DebugView Tiles", tilePass.debugViewTilesFlags, styles.tileLightLoopDebugTileFlagStrings);
                tilePass.enableSplitLightEvaluation = EditorGUILayout.Toggle(styles.splitLightEvaluation, tilePass.enableSplitLightEvaluation);
                tilePass.disableTileAndCluster = EditorGUILayout.Toggle(styles.disableTileAndCluster, tilePass.disableTileAndCluster);
                tilePass.disableDeferredShadingInCompute = EditorGUILayout.Toggle(styles.disableDeferredShadingInCompute, tilePass.disableDeferredShadingInCompute);

                if (EditorGUI.EndChangeCheck())
                {
                   HackSetDirty(renderContext); // Repaint
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
                EditorGUI.indentLevel--;
            }
        }*/

        public void OnEnable()
        {
            InitializeProperties();
        }

        public override void OnInspectorGUI()
        {
            var renderContext = target as HDRenderPipeline;

            if (!renderContext)
                return;

            serializedObject.Update();

            DebuggingUI(renderContext);
            SkySettingsUI(renderContext);
            SssSettingsUI(renderContext);
            ShadowParametersUI(renderContext);
            TextureParametersUI(renderContext);
            //TilePassUI(renderContext);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
