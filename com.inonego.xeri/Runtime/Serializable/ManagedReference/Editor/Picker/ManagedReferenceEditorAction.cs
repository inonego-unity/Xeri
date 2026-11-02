/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ManagedReferenceEditorAction.cs
수정일 : 2026-10-29

# 설명
SerializeReference의 구현 형식 선택·생성·참조 비우기·복사·붙여넣기를 공통 제공한다.
키·값 및 읽기 전용 상태의 변경 허용 여부는 호출자가 지정하고, 생성 Picker는 함께 재사용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Editor;
using inonego.Xeri.Editor.Picker;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Managed-reference Editor 공통 action.
    /// </summary>
    // ============================================================
    internal static class ManagedReferenceEditorAction
    {

    #region 조회

        private static readonly Dictionary<Type, PickerSpec<ManagedReferenceCreationCandidate,
            ManagedReferenceCreationCandidate>> creationPickerSpecs = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 managed-reference의 Inspector 표시 타입 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static string GetDisplayName(SerializedProperty property)
        {
            if (property == null)
            {
                return "None";
            }

            if (property.hasMultipleDifferentValues)
            {
                return "여러 값";
            }

            return ManagedReferenceTypeCatalog.GetDisplayName
            (
                property.managedReferenceValue?.GetType()
            );
        }

    #endregion

    #region 타입 선택

        // ----------------------------------------------------------------------
        /// <summary>
        /// 선언 타입에 대입 가능한 구현 타입 Picker를 anchor 기준으로 표시한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void ShowTypePicker
        (
            SerializedProperty property,
            Type declaredType,
            VisualElement anchor,
            Action onChanged = null,
            Func<bool> canApply = null
        )
        {
            if (property == null || declaredType == null || anchor == null)
            {
                return;
            }

            var targets = property.serializedObject.targetObjects;
            var locators = CaptureTargets(property);
            var currentType = property.managedReferenceValue?.GetType();
            ShowCreationTypePicker
            (
                declaredType,
                anchor,
                currentType,
                type =>
                {
                    // Picker의 anchor가 사라졌으면 종료된 field의 명령을 실행하지 않는다.
                    if (anchor.panel != null && (canApply == null || canApply()))
                    {
                        ApplyNewReference(targets, locators, type, onChanged);
                    }
                }
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 아직 property가 없는 Add 시점에 생성할 managed-reference 타입을 선택한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void ShowCreationTypePicker
        (
            Type declaredType,
            VisualElement anchor,
            Action<Type> onSelected
        )
        {
            if (declaredType == null || anchor == null || onSelected == null)
            {
                return;
            }

            ShowCreationTypePicker
            (
                declaredType,
                anchor,
                currentType: null,
                onSelected
            );
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 선언 타입 후보 Picker를 구성하고 최종 closed type을 선택 callback에 전달한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static void ShowCreationTypePicker
        (
            Type declaredType,
            VisualElement anchor,
            Type currentType,
            Action<Type> onSelected
        )
        {
            const int popupPageSize = 5;

            var candidates = ManagedReferenceTypeCatalog.GetCreationCandidates(declaredType);
            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog
                (
                    "SerializeReference 선택",
                    "생성할 수 있는 managed reference 타입 후보가 없습니다.",
                    "확인"
                );
                return;
            }

            var currentValue = candidates.FirstOrDefault
            (
                candidate => candidate.Type == currentType
            );
            var spec = GetCreationPickerSpec(declaredType);

            Picker.ShowPopup
            (
                spec,
                candidates,
                currentValue,
                selectedCandidate =>
                {
                    if (selectedCandidate.RequiresGenericTypeCreation)
                    {
                        GenericTypePicker.Show
                        (
                            selectedCandidate.Type,
                            ManagedReferenceTypeCatalog.GetGenericArgumentCandidates,
                            ManagedReferenceTypeCatalog.GetDisplayName,
                            onSelected,
                            selectedCandidate.FixedArguments,
                            type => GetGenericTypeValidationMessage
                            (
                                type,
                                declaredType
                            )
                        );
                        return;
                    }

                    onSelected(selectedCandidate.Type);
                },
                anchor,
                pageSize: popupPageSize
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 타입의 managed-reference instance를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryCreateReference
        (
            Type type,
            out object reference,
            out string reason
        )
        {
            reference = null;

            if (!ManagedReferenceTypeCatalog.CanCreate(type))
            {
                reason = "선택한 타입은 public 기본 생성자로 만들 수 없습니다.";
                return false;
            }

            try
            {
                reference = Activator.CreateInstance(type);
                reason = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                reason = exception.GetBaseException().Message;
                return false;
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 새 managed-reference instance를 모든 선택 target의 동일 property에 대입한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static void ApplyNewReference
        (
            UnityEngine.Object[] targets,
            SerializedPropertyLocator[] locators,
            Type type,
            Action onChanged
        )
        {
            // 생성 실패는 아무 target도 변경하기 전에 확인한다.
            var references = new object[targets.Length];
            for (var index = 0; index < references.Length; index++)
            {
                if (!TryCreateReference(type, out references[index], out var reason))
                {
                    EditorUtility.DisplayDialog("SerializeReference", reason, "확인");
                    return;
                }
            }

            var cursor = 0;
            RunTargets(targets, locators, current => current.managedReferenceValue = references[cursor++], true, onChanged);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 구성한 generic managed-reference type의 선언 타입 호환성을 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static string GetGenericTypeValidationMessage
        (
            Type type,
            Type targetType
        )
        {
            if (!targetType.IsAssignableFrom(type))
            {
                return "구성한 타입을 이 필드에 할당할 수 없습니다.";
            }

            return ManagedReferenceTypeCatalog.CanCreate(type)
                ? string.Empty
                : "구성한 타입은 public 기본 생성자로 만들 수 없습니다.";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 선언 타입은 동일한 불변 Picker 설정과 표시 Entry를 재사용한다.
        /// </summary>
        // ------------------------------------------------------------
        private static PickerSpec<ManagedReferenceCreationCandidate,
            ManagedReferenceCreationCandidate> GetCreationPickerSpec(Type declaredType)
        {
            if (creationPickerSpecs.TryGetValue(declaredType, out var spec))
            {
                return spec;
            }

            var columnOptions = PickerColumnOptions.Flexible
            (
                width: 260f,
                minWidth: 160f,
                stretchWeight: 1f
            );
            spec = PickerSpec<ManagedReferenceCreationCandidate, ManagedReferenceCreationCandidate>
                .Create($"{declaredType.Name} 선택")
                .Value(candidate => candidate)
                .Preview(false)
                .Label(candidate => candidate.DisplayName)
                .Column("타입", candidate => candidate.DisplayName, columnOptions)
                .CacheImmutableEntries()
                .Build();

            creationPickerSpecs.Add(declaredType, spec);
            return spec;
        }

    #endregion

    #region 동작 메뉴

        // --------------------------------------------------------------------------------
        /// <summary>
        /// null, Value/Link clipboard 명령을 포함한 managed-reference 메뉴를 표시한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static void ShowActions
        (
            SerializedProperty property,
            Type declaredType,
            Action onChanged = null,
            Action chooseType = null,
            Func<bool> canApply = null,
            VisualElement menuOwner = null,
            bool canChangeReference = true,
            bool canChooseType = true
        )
        {
            if (property == null || declaredType == null)
            {
                return;
            }

            var targets = property.serializedObject.targetObjects;
            var locators = CaptureTargets(property);
            var anchor = menuOwner ?? EditorWindow.focusedWindow?.rootVisualElement;
            if (anchor == null)
            {
                return;
            }

            var menu = new XeriContextMenu(anchor);

            void ClearReference()
            {
                if (!canChangeReference || canApply != null && !canApply())
                {
                    return;
                }

                RunTargets
                (
                    targets, locators,
                    current => current.managedReferenceValue = null,
                    true, onChanged
                );
            }

            void CopyValue()
            {
                var firstTarget = new UnityEngine.Object[1];
                firstTarget[0] = targets[0];
                var firstLocator = new SerializedPropertyLocator[1];
                firstLocator[0] = locators[0];

                RunTargets
                (
                    firstTarget, firstLocator,
                    current =>
                    {
                        if (!SerializedPropertyClipboard.TryCopyValue(current, out var reason))
                        {
                            throw new InvalidOperationException(reason);
                        }
                    },
                    false, null
                );
            }

            void PasteValue()
            {
                if (!canChangeReference || canApply != null && !canApply())
                {
                    return;
                }

                RunTargets
                (
                    targets, locators,
                    current =>
                    {
                        if (!SerializedPropertyClipboard.TryPasteValue(current, out var reason, declaredType))
                        {
                            throw new InvalidOperationException(reason);
                        }
                    },
                    true, onChanged
                );
            }

            void CopyLink()
            {
                RunTargets
                (
                    targets, locators,
                    current =>
                    {
                        if (!SerializedPropertyClipboard.TryCopyLink(current, out var reason))
                        {
                            throw new InvalidOperationException(reason);
                        }
                    },
                    false, null
                );
            }

            void PasteLink()
            {
                if (!canChangeReference || canApply != null && !canApply())
                {
                    return;
                }

                RunTargets
                (
                    targets, locators,
                    current =>
                    {
                        if (!SerializedPropertyClipboard.TryPasteLink(current, declaredType, out var reason))
                        {
                            throw new InvalidOperationException(reason);
                        }
                    },
                    true, onChanged
                );
            }

            if (!property.hasMultipleDifferentValues && property.managedReferenceValue != null)
            {
                menu.AddLabel($"참조 ID: {property.managedReferenceId}");
                menu.AddSeparator();
            }

            if (chooseType != null)
            {
                menu.AddAction("구현 형식 선택...", chooseType, enabled: canChooseType);
            }

            var canClear = canChangeReference && (property.hasMultipleDifferentValues
                || property.managedReferenceValue != null);
            menu.AddAction("참조 비우기 (Null)", ClearReference, enabled: canClear);
            menu.AddSeparator();
            menu.AddAction("값으로 복사", CopyValue);
            menu.AddAction
            (
                "값으로 붙여넣기", PasteValue,
                enabled: canChangeReference && SerializedPropertyClipboard.CanPasteValue(property, out _, declaredType)
            );
            menu.AddAction
            (
                "참조 링크 복사", CopyLink,
                enabled: SerializedPropertyClipboard.CanCopyLink(property, out _)
            );
            menu.AddAction
            (
                "참조 링크 붙여넣기", PasteLink,
                enabled: canChangeReference && SerializedPropertyClipboard.CanPasteLink(property, declaredType, out _)
            );

            menu.OpenBelow();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// menu나 Picker를 열 때 각 host의 property identity를 캡처한다.
        /// </summary>
        // ------------------------------------------------------------
        private static SerializedPropertyLocator[] CaptureTargets(SerializedProperty property)
        {
            var targets = property.serializedObject.targetObjects;
            var result = new SerializedPropertyLocator[targets.Length];
            for (var index = 0; index < targets.Length; index++)
            {
                using var owner = new SerializedObject(targets[index]);
                result[index] = new SerializedPropertyLocator(owner.FindProperty(property.propertyPath));
            }

            return result;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 locator를 먼저 확인한 뒤 현재 property에 명령을 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void RunTargets
        (
            UnityEngine.Object[] targets,
            SerializedPropertyLocator[] locators,
            Action<SerializedProperty> execute,
            bool mutation,
            Action onChanged
        )
        {
            var owners = new SerializedObject[targets.Length];
            var properties = new SerializedProperty[targets.Length];
            try
            {
                for (var index = 0; index < targets.Length; index++)
                {
                    owners[index] = new SerializedObject(targets[index]);
                    if (!locators[index].TryResolve(owners[index], out properties[index], out var reason))
                    {
                        EditorUtility.DisplayDialog("SerializeReference", reason, "확인");
                        return;
                    }
                }

                if (mutation)
                {
                    Undo.RecordObjects(targets, "Managed Reference 편집");
                }

                foreach (var property in properties)
                {
                    execute(property);
                    if (mutation)
                    {
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }

                if (mutation)
                {
                    onChanged?.Invoke();
                }
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("SerializeReference", exception.GetBaseException().Message, "확인");
            }
            finally
            {
                // SerializedProperty보다 owner가 오래 살지 않도록 명령 종료 시 함께 정리한다.
                foreach (var owner in owners)
                {
                    owner?.Dispose();
                }
            }
        }

    #endregion

    }
}
