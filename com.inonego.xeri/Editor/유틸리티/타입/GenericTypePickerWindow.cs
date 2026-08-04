/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenericTypePickerWindow.cs
수정일 : 2026-08-05

# 설명
중첩 generic argument를 트리로 구성하고 닫힌 CLR 타입을 확정하는 UI Toolkit EditorWindow.

# 특이사항
후보 제공과 결과 검증은 호출자가 소유한다.
이 창은 TreeNode 계층, 제네릭 타입 결합, 표시와 확정만 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using inonego.Xeri.Editor.Picker;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 중첩 generic argument를 한 화면에서 구성하는 EditorWindow.
    /// </summary>
    // ============================================================
    internal sealed class GenericTypePickerWindow : EditorWindow
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 제네릭 parameter 하나의 선택 상태.
        /// </summary>
        // ============================================================
        private sealed class GenericTypePickerArgument
        {

        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 행이 구성하는 제네릭 parameter.
            /// </summary>
            // ------------------------------------------------------------
            public Type Parameter => parameter;
            private readonly Type parameter = null;

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 선택한 일반 타입 또는 열린 제네릭 타입 정의.
            /// </summary>
            // ------------------------------------------------------------
            public Type SelectedType => selectedType;
            private Type selectedType = null;

            // ------------------------------------------------------------
            /// <summary>
            /// 완성된 현재 타입을 배열로 감쌀지 여부.
            /// </summary>
            // ------------------------------------------------------------
            public bool IsArray
            {
                get => isArray;
                set => isArray = value;
            }
            private bool isArray = false;

            // ------------------------------------------------------------
            /// <summary>
            /// 상위 계약으로 확정되어 사용자가 변경할 수 없는지 여부.
            /// </summary>
            // ------------------------------------------------------------
            public bool IsFixed => isFixed;
            private bool isFixed = false;

        #endregion

        #region 생성자

            // ------------------------------------------------------------
            /// <summary>
            /// 지정한 제네릭 parameter의 선택 상태를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public GenericTypePickerArgument(Type parameter) : base()
            {
                this.parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            }

        #endregion

        #region 메서드

            // ------------------------------------------------------------
            /// <summary>
            /// 사용자가 선택한 타입으로 현재 argument 상태를 교체한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Select(Type type)
            {
                // 배열 여부는 현재 parameter의 최종 형태이므로 후보 타입을 바꿔도 유지한다.
                selectedType = type;
                isFixed      = false;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 상위 선언 계약으로 확정된 타입을 설정하고 편집을 잠근다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetFixed(Type type)
            {
                selectedType = type;
                isArray      = false;
                isFixed      = true;
            }

        #endregion

        }

    #endregion

    #region 상수

        private const string UxmlFileName       = "GenericTypePickerWindow.uxml";
        private const string UssFileName        = "GenericTypePickerWindow.uss";
        private const string UiDirectoryName    = "UI";
        private const float BaseWindowWidth     = 460f;
        private const float InitialWindowHeight = 240f;
        private static readonly Vector2 unrestrictedWindowSize = new(float.MaxValue, float.MaxValue);

    #endregion

    #region 필드

        private Type genericTypeDefinition = null;
        private IReadOnlyList<TreeNode<GenericTypePickerArgument>> rootNodes = null;
        private Func<Type, bool, IReadOnlyList<Type>> candidateProvider = null;
        private Func<Type, string> displayNameProvider = null;
        private Func<Type, string> validationMessageProvider = null;
        private Action<Type> onSelected = null;

        private VisualElement windowRoot = null;
        private VisualElement argumentsContainer = null;
        private Label definitionLabel = null;
        private Label resultLabel = null;
        private Label statusLabel = null;
        private Button selectButton = null;
        private TreeView<GenericTypePickerArgument> argumentTree = null;
        private Vector2 initialWindowCenter = default;
        private bool hasFittedContentSize = false;

    #endregion

    #region 생성

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 열린 제네릭 타입 정의의 닫힌 타입을 구성할 EditorWindow를 표시한다.
        /// <br/> 고정 argument는 초기 상태로 적용하고 사용자가 변경하지 못하도록 잠근다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static void Open
        (
            Type genericTypeDefinition,
            Func<Type, bool, IReadOnlyList<Type>> candidateProvider,
            Func<Type, string> displayNameProvider,
            Action<Type> onSelected,
            IReadOnlyDictionary<Type, Type> fixedArguments,
            Func<Type, string> validationMessageProvider
        )
        {
            if (genericTypeDefinition == null || !genericTypeDefinition.IsGenericTypeDefinition)
            {
                throw new ArgumentException("열린 제네릭 타입 정의가 필요합니다.", nameof(genericTypeDefinition));
            }

            var window = CreateInstance<GenericTypePickerWindow>();
            window.genericTypeDefinition      = genericTypeDefinition;
            window.candidateProvider          = candidateProvider ?? throw new ArgumentNullException(nameof(candidateProvider));
            window.displayNameProvider        = displayNameProvider ?? throw new ArgumentNullException(nameof(displayNameProvider));
            window.validationMessageProvider  = validationMessageProvider;
            window.onSelected                 = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
            window.rootNodes                  = CreateRootNodes(genericTypeDefinition, fixedArguments);
            window.titleContent               = new GUIContent("제네릭 타입 선택");
            window.minSize                    = new Vector2(BaseWindowWidth, 0f);
            window.maxSize                    = unrestrictedWindowSize;

            var windowSize = new Vector2(BaseWindowWidth, InitialWindowHeight);
            var mainWindow = EditorGUIUtility.GetMainWindowPosition();
            window.initialWindowCenter = mainWindow.center;
            window.position = new Rect(mainWindow.center - windowSize * 0.5f, windowSize);
            window.ShowUtility();
            window.Focus();
        }

    #endregion

    #region 트리 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 최상위 제네릭 parameter와 고정 argument를 포함한 트리 루트를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static IReadOnlyList<TreeNode<GenericTypePickerArgument>> CreateRootNodes
        (
            Type genericTypeDefinition,
            IReadOnlyDictionary<Type, Type> fixedArguments
        )
        {
            var roots = new List<TreeNode<GenericTypePickerArgument>>();

            foreach (var parameter in genericTypeDefinition.GetGenericArguments())
            {
                var argumentNode = CreateArgumentNode(parameter);
                if (fixedArguments != null && fixedArguments.TryGetValue(parameter, out var fixedType))
                {
                    argumentNode.Value.SetFixed(fixedType);
                }

                roots.Add(argumentNode);
            }

            return roots;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 parameter를 표시할 빈 argument 노드를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static TreeNode<GenericTypePickerArgument> CreateArgumentNode(Type parameter)
        {
            return new TreeNode<GenericTypePickerArgument>(new GenericTypePickerArgument(parameter));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 열린 generic definition의 parameter를 현재 노드 자식으로 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void RebuildChildArguments
        (
            TreeNode<GenericTypePickerArgument> argumentNode,
            Type selectedType
        )
        {
            argumentNode.Clear();
            if (selectedType == null || !selectedType.IsGenericTypeDefinition)
            {
                return;
            }

            // CLR generic parameter 순서를 보존해야 MakeGenericType의 인자 순서와 일치한다.
            foreach (var parameter in selectedType.GetGenericArguments())
            {
                argumentNode.Add(CreateArgumentNode(parameter));
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 argument 트리를 TreeView로 다시 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RebuildArgumentTree()
        {
            ReleaseContentSize();
            argumentsContainer.Clear();

            argumentTree = new TreeView<GenericTypePickerArgument>(CreateArgumentRow);
            argumentsContainer.Add(argumentTree);
            argumentTree.SetRoots(rootNodes);

            RefreshResult();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// argument 노드 하나를 편집할 행 VisualElement로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private VisualElement CreateArgumentRow(TreeNode<GenericTypePickerArgument> argumentNode)
        {
            var argument = argumentNode.Value;
            var row = new VisualElement();
            row.AddToClassList("generic-type-argument-row");

            var parameterLabel = new Label(argument.Parameter.Name);
            parameterLabel.AddToClassList("generic-type-argument-name");
            row.Add(parameterLabel);

            var displayName = argument.SelectedType == null
                ? "타입 선택"
                : displayNameProvider.Invoke(argument.SelectedType);
            var typeButton = new Button(() => ShowTypePicker(argumentNode));
            typeButton.text = displayName;
            typeButton.tooltip = displayName;
            typeButton.AddToClassList("generic-type-argument-button");
            typeButton.SetEnabled(!argument.IsFixed);
            row.Add(typeButton);

            var arrayToggle = new Toggle("배열");
            arrayToggle.AddToClassList("generic-type-array-toggle");
            arrayToggle.SetValueWithoutNotify(argument.IsArray);
            arrayToggle.SetEnabled(!argument.IsFixed);
            arrayToggle.RegisterValueChangedCallback(changeEvent =>
            {
                // 배열 여부도 CLR generic constraint에 영향을 주므로 결과를 즉시 다시 계산한다.
                argument.IsArray = changeEvent.newValue;
                RefreshResult();
            });
            row.Add(arrayToggle);

            return row;
        }

    #endregion

    #region 선택

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 generic parameter의 타입을 Xeri Picker에서 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ShowTypePicker(TreeNode<GenericTypePickerArgument> argumentNode)
        {
            var argument = argumentNode.Value;
            var candidates = candidateProvider.Invoke(argument.Parameter, argument.IsArray) ?? Array.Empty<Type>();
            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog
                (
                    "제네릭 타입 선택",
                    $"{argument.Parameter.Name}에 사용할 수 있는 타입 후보가 없습니다.",
                    "확인"
                );
                return;
            }

            var currentType = candidates.Contains(argument.SelectedType) ? argument.SelectedType : null;
            var spec = PickerSpec<Type, Type>.Create($"{argument.Parameter.Name} 선택")
                .Value(type => type)
                .Preview(false)
                .Label(displayNameProvider)
                .Column
                (
                    "타입",
                    displayNameProvider,
                    PickerColumnOptions.Flexible(width: 320f, minWidth: 180f, stretchWeight: 1f)
                )
                .Build();

            Picker.Show
            (
                spec,
                candidates,
                currentType,
                selectedType =>
                {
                    // 타입 교체 시 이전 nested argument는 새 definition과 관계가 없으므로 함께 버린다.
                    argument.Select(selectedType);
                    RebuildChildArguments(argumentNode, selectedType);
                    ReleaseContentSize();
                    argumentTree.Refresh();
                    RefreshResult();
                }
            );
        }

    #endregion

    #region 결과

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 argument 트리를 닫힌 CLR generic type으로 결합한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool TryBuildResult(out Type result, out string error)
        {
            result = null;
            error  = string.Empty;

            var argumentTypes = new Type[rootNodes.Count];
            for (var i = 0; i < rootNodes.Count; i++)
            {
                if (!TryBuildArgument(rootNodes[i], out var argumentType, out error))
                {
                    return false;
                }

                argumentTypes[i] = argumentType;
            }

            try
            {
                // 모든 인자가 닫힌 뒤 CLR에 결합을 위임해 parameter 간 관계형 constraint도 함께 검증한다.
                result = genericTypeDefinition.MakeGenericType(argumentTypes);
                return true;
            }
            catch (ArgumentException)
            {
                error = "선택한 타입이 제네릭 제약 조건을 만족하지 않습니다.";
                return false;
            }
            catch (TypeLoadException)
            {
                error = "구성한 제네릭 타입을 불러올 수 없습니다.";
                return false;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 하나의 argument 노드와 모든 nested child를 닫힌 CLR 타입으로 결합한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool TryBuildArgument
        (
            TreeNode<GenericTypePickerArgument> argumentNode,
            out Type result,
            out string error
        )
        {
            result = null;
            error  = string.Empty;

            var argument = argumentNode.Value;
            if (argument.SelectedType == null)
            {
                error = "모든 제네릭 인자 타입을 선택하세요.";
                return false;
            }

            if (!argument.SelectedType.IsGenericTypeDefinition)
            {
                if (argument.SelectedType.ContainsGenericParameters)
                {
                    error = "선택한 타입에 아직 열린 제네릭 인자가 남아 있습니다.";
                    return false;
                }

                result = argument.SelectedType;
                return TryApplyArray(argument.IsArray, ref result, out error);
            }

            var parameters = argument.SelectedType.GetGenericArguments();
            if (parameters.Length != argumentNode.Children.Count)
            {
                error = "제네릭 인자 구성이 타입 정의와 일치하지 않습니다.";
                return false;
            }

            var argumentTypes = new Type[argumentNode.Children.Count];
            for (var i = 0; i < argumentNode.Children.Count; i++)
            {
                if (!TryBuildArgument(argumentNode.Children[i], out var argumentType, out error))
                {
                    return false;
                }

                argumentTypes[i] = argumentType;
            }

            try
            {
                // nested definition도 안쪽부터 닫아야 현재 node가 하나의 유효한 CLR 타입이 된다.
                result = argument.SelectedType.MakeGenericType(argumentTypes);
            }
            catch (ArgumentException)
            {
                error = "선택한 타입이 제네릭 제약 조건을 만족하지 않습니다.";
                return false;
            }
            catch (TypeLoadException)
            {
                error = "구성한 제네릭 타입을 불러올 수 없습니다.";
                return false;
            }

            return TryApplyArray(argument.IsArray, ref result, out error);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// node의 배열 설정을 완성된 타입 바깥에 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool TryApplyArray(bool isArray, ref Type result, out string error)
        {
            error = string.Empty;
            if (!isArray)
            {
                return true;
            }

            try
            {
                result = result.MakeArrayType();
                return true;
            }
            catch (TypeLoadException)
            {
                error = "선택한 타입은 배열 요소로 사용할 수 없습니다.";
                return false;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 타입 구성 결과와 소비자 검증 상태를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshResult()
        {
            if (!TryBuildResult(out var result, out var error))
            {
                resultLabel.text = "미완성";
                statusLabel.text = error;
                selectButton.SetEnabled(false);
                return;
            }

            resultLabel.text = GetResultDisplayName(result);

            var validationMessage = validationMessageProvider?.Invoke(result);
            if (!string.IsNullOrEmpty(validationMessage))
            {
                statusLabel.text = validationMessage;
                selectButton.SetEnabled(false);
                return;
            }

            statusLabel.text = string.Empty;
            selectButton.SetEnabled(true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 긴 generic 결과를 의미 단위에서 줄바꿈할 수 있는 표시 문자열으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private string GetResultDisplayName(Type type)
        {
            return displayNameProvider.Invoke(type)
                .Replace(".", ".\u200B")
                .Replace(",", ",\u200B")
                .Replace("<", "<\u200B")
                .Replace(">", ">\u200B");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 닫힌 타입을 호출자에게 전달하고 창을 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        private void SelectType()
        {
            if (!TryBuildResult(out var result, out _))
            {
                return;
            }

            if (!string.IsNullOrEmpty(validationMessageProvider?.Invoke(result)))
            {
                return;
            }

            onSelected.Invoke(result);
            Close();
        }

    #endregion

    #region UI

        // ------------------------------------------------------------
        /// <summary>
        /// UXML과 USS를 로드해 window root를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement LoadRoot()
        {
            var dir         = EditorAssetHelper.GetScriptDirectory(typeof(GenericTypePickerWindow));
            var uiDirectory = $"{dir}/{UiDirectoryName}";
            var template    = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{uiDirectory}/{UxmlFileName}");
            var style       = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{uiDirectory}/{UssFileName}");

            if (template == null)
            {
                return null;
            }

            var root = template.CloneTree();
            if (style != null)
            {
                root.styleSheets.Add(style);
            }

            return root;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// argument 또는 상태 영역의 높이가 바뀔 때 form이 요구하는 창 크기를 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleContentGeometryChanged(GeometryChangedEvent _)
        {
            FitToContent(GetContentSize());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 form의 기본 폭과 가장 깊은 argument 행의 실제 들여쓰기를 합산한다.
        /// </summary>
        // ------------------------------------------------------------
        private float GetContentWidth()
        {
            var maximumIndent = 0f;
            var baseX = argumentsContainer.worldBound.x;
            foreach (var row in argumentsContainer.Query<VisualElement>(className: "generic-type-argument-row").ToList())
            {
                maximumIndent = Mathf.Max(maximumIndent, row.worldBound.x - baseX);
            }

            return BaseWindowWidth + Mathf.Ceil(maximumIndent);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 직접 자식의 최하단을 기준으로 현재 form이 요구하는 높이를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private float GetContentHeight()
        {
            var contentBottom = windowRoot.resolvedStyle.paddingTop;
            foreach (var child in windowRoot.Children())
            {
                contentBottom = Mathf.Max(contentBottom, child.layout.yMax);
            }

            return contentBottom + windowRoot.resolvedStyle.paddingBottom;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 form이 요구하는 창의 폭과 높이를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private Vector2 GetContentSize()
        {
            return new Vector2(GetContentWidth(), GetContentHeight());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// argument 트리가 확장되기 전에 Utility 창의 크기 고정을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseContentSize()
        {
            minSize = new Vector2(BaseWindowWidth, 0f);
            maxSize = unrestrictedWindowSize;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 측정된 content 크기로 Utility 창을 고정 크기 상태로 전환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void FitToContent(Vector2 contentSize)
        {
            if (float.IsNaN(contentSize.x)
                || float.IsInfinity(contentSize.x)
                || float.IsNaN(contentSize.y)
                || float.IsInfinity(contentSize.y)
                || contentSize.x <= 0f
                || contentSize.y <= 0f)
            {
                return;
            }

            var size = new Vector2(Mathf.Ceil(contentSize.x), Mathf.Ceil(contentSize.y));
            if (Mathf.Approximately(position.width, size.x) && Mathf.Approximately(position.height, size.y))
            {
                return;
            }

            // 첫 layout은 Unity가 Utility rect를 확정하기 전이므로 Open에서 기록한 메인 창 중심을 사용한다.
            var center = hasFittedContentSize ? position.center : initialWindowCenter;
            ReleaseContentSize();
            position = new Rect(center - size * 0.5f, size);
            minSize = size;
            maxSize = size;
            hasFittedContentSize = true;
        }

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// UXML view를 만들고 현재 generic 구성 상태를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CreateGUI()
        {
            rootVisualElement.Clear();

            var root = LoadRoot();
            if (root == null || rootNodes == null)
            {
                rootVisualElement.Add(new Label("제네릭 타입 선택기 리소스를 불러올 수 없습니다."));
                return;
            }

            windowRoot         = root.Q<VisualElement>("generic-type-picker");
            definitionLabel    = root.Q<Label>("definition-label");
            argumentsContainer = root.Q<VisualElement>("arguments");
            resultLabel        = root.Q<Label>("result-label");
            statusLabel        = root.Q<Label>("status-label");
            selectButton       = root.Q<Button>("select-button");
            var cancelButton   = root.Q<Button>("cancel-button");

            if (windowRoot == null
                || definitionLabel == null
                || argumentsContainer == null
                || resultLabel == null
                || statusLabel == null
                || selectButton == null
                || cancelButton == null)
            {
                rootVisualElement.Add(new Label("제네릭 타입 선택기 레이아웃이 완전하지 않습니다."));
                return;
            }

            argumentsContainer.RegisterCallback<GeometryChangedEvent>(HandleContentGeometryChanged);
            resultLabel.RegisterCallback<GeometryChangedEvent>(HandleContentGeometryChanged);
            statusLabel.RegisterCallback<GeometryChangedEvent>(HandleContentGeometryChanged);
            definitionLabel.text = displayNameProvider.Invoke(genericTypeDefinition);
            selectButton.clicked += SelectType;
            cancelButton.clicked += Close;
            rootVisualElement.Add(root);
            RebuildArgumentTree();
        }

    #endregion

    }
}
