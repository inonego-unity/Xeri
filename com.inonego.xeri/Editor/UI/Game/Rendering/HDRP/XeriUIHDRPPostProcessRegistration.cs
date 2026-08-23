/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIHDRPPostProcessRegistration.cs
수정일 : 2026-08-24

# 설명
Xeri HDRP UI Composite Post Process를 현재 HDRP Global Settings의 AfterPostProcess 목록에 자동 등록한다.

# 특이사항, 제약사항
HDRP internal 설정 형식을 직접 참조하지 않고 RenderPipelineGlobalSettings의 직렬화 경로만 사용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace inonego.Xeri.UI.Game
{
    // ================================================================================
    /// <summary>
    /// Editor 로드와 Player Build 전에 HDRP Custom Post Process 등록 상태를 보장한다.
    /// </summary>
    // ================================================================================
    [InitializeOnLoad]
    internal static class XeriUIHDRPPostProcessRegistration
    {

    #region 설정 경로

        private const string SETTINGS_LIST_PATH = "m_Settings.m_SettingsList.m_List";
        private const string ORDERS_TYPE_NAME = "CustomPostProcessOrdersSettings";
        private const string AFTER_POST_PROCESS_FIELD = "m_AfterPostProcessCustomPostProcesses";
        private const string TYPE_LIST_FIELD = "m_CustomPostProcessTypesAsString";

    #endregion

    #region Editor 등록 수명

        // ------------------------------------------------------------
        /// <summary>
        /// Assembly reload 뒤 Global Settings가 사용 가능해지는 Editor update에서 등록을 보장한다.
        /// </summary>
        // ------------------------------------------------------------
        static XeriUIHDRPPostProcessRegistration()
        {
            QueueEnsureRegistered();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 중복 callback 없이 다음 Editor update에 등록 검사를 예약한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void QueueEnsureRegistered()
        {
            EditorApplication.delayCall -= EnsureRegisteredWhenReady;
            EditorApplication.delayCall += EnsureRegisteredWhenReady;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Asset import와 Script compile이 끝난 시점에 실제 등록을 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void EnsureRegisteredWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueEnsureRegistered();
                return;
            }

            EnsureRegistered();
        }

    #endregion

    #region Global Settings 등록

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 HDRP Global Settings의 AfterPostProcess 타입 목록에 Xeri Composite를 한 번 추가한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static void EnsureRegistered()
        {
            var settings = GraphicsSettings.GetSettingsForRenderPipeline
            (
                typeof(HDRenderPipeline)
            );

            if (settings == null) return;

            var serializedSettings = new SerializedObject(settings);
            serializedSettings.UpdateIfRequiredOrScript();

            var types = FindAfterPostProcessTypes(serializedSettings);
            if (types == null) return;

            var typeName = typeof(XeriUIHDRPCompositePostProcess).AssemblyQualifiedName;
            if (string.IsNullOrEmpty(typeName)) return;

            for (var i = 0; i < types.arraySize; i++)
            {
                if (types.GetArrayElementAtIndex(i).stringValue == typeName)
                {
                    return;
                }
            }

            // 자동 인프라 등록은 Undo 대상이 아니며 Asset에 영구 기록해 Player Build에서도 같은 목록을 사용한다.
            var index = types.arraySize;
            types.InsertArrayElementAtIndex(index);
            types.GetArrayElementAtIndex(index).stringValue = typeName;

            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 HDRP Graphics Settings managed-reference 목록에서 AfterPostProcess 타입 배열을 찾는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static SerializedProperty FindAfterPostProcessTypes
        (
            SerializedObject serializedSettings
        )
        {
            var settingsList = serializedSettings.FindProperty(SETTINGS_LIST_PATH);
            if (settingsList == null) return null;

            for (var i = 0; i < settingsList.arraySize; i++)
            {
                var setting = settingsList.GetArrayElementAtIndex(i);
                if (!setting.managedReferenceFullTypename.Contains(ORDERS_TYPE_NAME)) continue;

                var afterPostProcess = setting.FindPropertyRelative(AFTER_POST_PROCESS_FIELD);
                return afterPostProcess?.FindPropertyRelative(TYPE_LIST_FIELD);
            }

            return null;
        }

    #endregion

    }

}
