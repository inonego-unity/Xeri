/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameObjectProviderDrawer.cs
수정일 : 2026-05-02

# 설명
IGameObjectProvider 인터페이스 필드용 UI Toolkit PropertyDrawer.
[SerializeReference] 필드에 대해 PrefabGameObjectProvider / AddressableGameObjectProvider
구체 타입 간 동적 전환 버튼과 자식 필드를 표시한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// IGameObjectProvider 전용 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(IGameObjectProvider), useForChildren: false)]
    public class GameObjectProviderDrawer : PropertyDrawer
    {

    #region 상수

        private const float BUTTON_SPACING = 4f;

    #endregion

    #region 초기화

        // ------------------------------------------------------------
        /// <summary>
        /// 인터페이스 + ManagedReference 조건을 만족하면 동적 UI를 반환하고,
        /// 그 외에는 기본 PropertyField를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // 인터페이스 필드가 아니거나 ManagedReference가 아니면 기본 처리
            if (!IsInterfaceField() || property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return new PropertyField(property);
            }

            var root = LoadRoot();

            if (root == null) return new Label($"[{GetType().Name}] UXML을 찾을 수 없습니다.");

            var foldout         = root.Q<Foldout>("foldout");
            var buttonArea      = root.Q<VisualElement>("button-area");
            var fieldsContainer = root.Q<VisualElement>("fields");

            foldout.text  = ObjectNames.NicifyVariableName(property.name);
            foldout.value = property.isExpanded;
            foldout.RegisterValueChangedCallback(e => property.isExpanded = e.newValue);

            // managedReferenceValue가 변경될 때마다 버튼 영역과 자식 필드를 재구성
            void Rebuild() => RebuildContents(property, buttonArea, fieldsContainer);

            Rebuild();

            // 외부 변경(undo, 다른 UI 등)에 의한 SerializedProperty 변경 추적
            root.TrackPropertyValue(property, _ => Rebuild());

            return root;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UXML과 USS를 로드해 루트 VisualElement를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private VisualElement LoadRoot()
        {
            var dir  = EditorAssetHelper.GetScriptDirectory(GetType());
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dir}/GameObjectProviderDrawer.uxml");
            var uss  = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dir}/GameObjectProviderDrawer.uss");

            if (uxml == null) return null;

            var root = uxml.CloneTree();

            if (uss != null) root.styleSheets.Add(uss);

            return root;
        }

    #endregion

    #region 갱신

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 managedReferenceValue 타입에 따라 버튼 영역과 자식 필드를 재구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void RebuildContents(SerializedProperty property, VisualElement buttonArea, VisualElement fieldsContainer)
        {
            buttonArea.Clear();
            fieldsContainer.Clear();

            var current       = property.managedReferenceValue;
            var isPrefab      = current is PrefabGameObjectProvider;
            var isAddressable = current is AddressableGameObjectProvider;

            // null이거나 인식 불가 타입이면 두 선택 버튼 나란히
            if (current == null || !(isPrefab || isAddressable))
            {
                var prefabButton      = MakeProviderButton("Prefab",      () => SetProvider(property, new PrefabGameObjectProvider()));
                var addressableButton = MakeProviderButton("Addressable", () => SetProvider(property, new AddressableGameObjectProvider()));

                addressableButton.style.marginLeft = BUTTON_SPACING;

                buttonArea.Add(prefabButton);
                buttonArea.Add(addressableButton);
                return;
            }

            // 전환 버튼 — 현재 타입에 따라 텍스트와 대상 타입이 달라짐
            if (isPrefab)
            {
                buttonArea.Add(MakeProviderButton("→ Addressable", () => SetProvider(property, new AddressableGameObjectProvider())));
            }
            else
            {
                buttonArea.Add(MakeProviderButton("→ Prefab",      () => SetProvider(property, new PrefabGameObjectProvider())));
            }

            // Provider의 직계 자식 필드를 PropertyField로 렌더링
            AppendChildFields(property, fieldsContainer);

            fieldsContainer.Bind(property.serializedObject);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// SerializedProperty의 직계 자식 필드를 컨테이너에 PropertyField로 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void AppendChildFields(SerializedProperty property, VisualElement container)
        {
            int depth      = property.depth;
            var iterator   = property.Copy();

            if (!iterator.NextVisible(enterChildren: true)) return;

            do
            {
                // 자식 영역을 벗어나면 종료
                if (iterator.depth <= depth) break;

                container.Add(new PropertyField(iterator.Copy()));
            }
            while (iterator.NextVisible(enterChildren: false));
        }

    #endregion

    #region 유틸리티

        // ------------------------------------------------------------
        /// <summary>
        /// Provider 선택/전환용 버튼을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static Button MakeProviderButton(string text, System.Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("provider-button");
            return button;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// managedReferenceValue를 변경하고 시리얼라이즈를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SetProvider(SerializedProperty property, object provider)
        {
            property.managedReferenceValue = provider;
            property.serializedObject.ApplyModifiedProperties();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필드가 IGameObjectProvider 인터페이스 타입으로 선언되었는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsInterfaceField()
        {
            if (fieldInfo == null) return false;

            return fieldInfo.FieldType.IsInterface && fieldInfo.FieldType == typeof(IGameObjectProvider);
        }

    #endregion

    }
}
