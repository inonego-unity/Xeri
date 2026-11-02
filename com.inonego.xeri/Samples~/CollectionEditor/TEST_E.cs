/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_E.cs
수정일 : 2026-11-01

# 설명
Xeri PropertyDrawer와 Collection Editor의 Inspector 사용 예제를 한 컴포넌트에 제공하는 샘플 MonoBehaviour.
primitive, object reference, enum 및 managed-reference 컬렉션의 필드 선언과 편집 방식을 보여준다.

# 특이사항, 제약사항
현재 Xeri의 Range<T>와 Collection Editor의 직렬화 타입을 샘플에서 함께 표시한다.
Xeri collection 계열과 opt-in List/Array는 새 Collection Editor 적용 상태를 확인한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Primitive;
using inonego.Xeri.Serializable;
using inonego.Xeri.Utility;

namespace inonego.Xeri.Samples
{
    // ============================================================
    /// <summary>
    /// Xeri 에디터 확장 표시를 한 Inspector에서 확인하는 샘플 컴포넌트.
    /// </summary>
    // ============================================================
    public sealed class TEST_E : MonoBehaviour
    {

    #region Range / Vector

        [Header("1. Range<T>")]
        [SerializeField] private Range<int> intRange = new(1, 10);
        [SerializeField] private Range<float> floatRange = new(0.25f, 1.0f);

        [Header("2. XVec2B / XVec3B / XVec4B")]
        [SerializeField] private XVec2B vec2B = new(true, false);
        [SerializeField] private XVec3B vec3B = new(true, false, true);
        [SerializeField] private XVec4B vec4B = new(true, false, true, false);

        [Header("3. XVec2I / XVec3I")]
        [SerializeField] private XVec2I vec2I = new(1, 2);
        [SerializeField] private XVec3I vec3I = new(1, 2, 3);

        [Header("4. XVec2F / XVec3F / XVec4F")]
        [SerializeField] private XVec2F vec2F = new(1.25f, 2.5f);
        [SerializeField] private XVec3F vec3F = new(1.0f, 2.0f, 3.0f);
        [SerializeField] private XVec4F vec4F = new(1.0f, 2.0f, 3.0f, 4.0f);

    #endregion

    #region Nullable / Value

        [Header("5. XNullable<T>")]
        [SerializeField] private XNullable<int> nullableInt = new(7);
        [SerializeField] private XNullable<float> nullableFloat = new((float?)null);

        [Header("6. Value<T>")]
        [SerializeField] private Value<int> intValue = new(42);
        [SerializeField] private Value<float> floatValue = new(3.14f);

        [Header("7. RangeValue<T>")]
        [SerializeField] private RangeValue<int> intRangeValue = new(5, new Range<int>(0, 10));
        [SerializeField] private RangeValue<float> floatRangeValue = new(0.5f, new Range<float>(0.0f, 1.0f));

        [Header("8. MValue<T>")]
        [SerializeField] private MValue<int> intMValue = new(100);
        [SerializeField] private MValue<float> floatMValue = new(12.5f);

    #endregion

    #region Provider / Attribute / Timer

        [Header("9. IGameObjectProvider")]
        [SerializeReference] private IGameObjectProvider gameObjectProvider = new PrefabGameObjectProvider();

        [Header("10. HelpBoxAttribute")]
        [HelpBox("HelpBoxAttribute 표시 확인용 메시지입니다.", HelpBoxMessageType.Info)]
        [SerializeField] private string helpBoxTarget = "HelpBox 아래의 일반 필드";

        [Header("11. ReadOnlyAttribute")]
        [ReadOnly]
        [SerializeField] private string readOnlyText = "Inspector에서 수정할 수 없어야 합니다.";

        [Header("12. ITimer / IReadOnlyTimer")]
        [SerializeReference] private ITimer timer = new Timer();
        [SerializeReference] private IReadOnlyTimer readOnlyTimer = new Timer();

    #endregion

    #region Collection Editor / 기타 직렬화 타입

        [Header("13. XDictionary_RR / RV / VR / VV")]
        [SerializeField] private XDictionary_RR<BooleanModifier, StringModifier> dictionaryRR = new();
        [SerializeField] private XDictionary_RV<BooleanModifier, int> dictionaryRV = new();
        [SerializeField] private XDictionary_VR<string, StringModifier> dictionaryVR = new();
        [SerializeField] private XDictionary_VV<string, int> dictionaryVV = new();
        [SerializeField] private XDictionary_VV<BooleanOperation, string> dictionaryEnumKeys = new();
        [SerializeField] private XDictionary_VV<int, string> dictionaryIntKeys = new();

        // HashSet/Queue/Stack과 동일한 R=StringModifier, V=int 조합.
        [SerializeField] private XDictionary_RR<StringModifier, StringModifier> dictionaryMatchedRR = new();
        [SerializeField] private XDictionary_RV<StringModifier, int> dictionaryMatchedRV = new();
        [SerializeField] private XDictionary_VR<int, StringModifier> dictionaryMatchedVR = new();
        [SerializeField] private XDictionary_VV<int, int> dictionaryMatchedVV = new();

        [Header("14. XHashSet_R / V")]
        [SerializeField] private XHashSet_R<StringModifier> hashSetR = new();
        [SerializeField] private XHashSet_V<int> hashSetV = new();

        [Header("15. XQueue_R / V")]
        [SerializeField] private XQueue_R<StringModifier> queueR = new();
        [SerializeField] private XQueue_V<int> queueV = new();

        [Header("16. XStack_R / V")]
        [SerializeField] private XStack_R<StringModifier> stackR = new();
        [SerializeField] private XStack_V<int> stackV = new();

        [Header("17. XOrdered")]
        [SerializeField] private XOrdered<int, StringModifier> ordered = new();
        [SerializeField] private XOrdered<int, string, StringModifier> keyedOrdered = new();

        [Header("18. XPriorityQueue")]
        [SerializeField] private XPriorityQueue<StringModifier, int> priorityQueue = new();

        [Header("19. XType / SerializeReferenceWrapper<T>")]
        [SerializeField] private XType xType = new(typeof(TEST_E));
        [SerializeField] private SerializeReferenceWrapper<IGameObjectProvider> referenceWrapper = new(new PrefabGameObjectProvider());

        [Header("20. XNumericF / XNumericI")]
        [SerializeField] private XNumericF numericF = new(1.25f);
        [SerializeField] private XNumericI numericI = new(42);

        [Header("21. Modifiers")]
        [SerializeField] private BooleanModifier booleanModifier = new(BooleanOperation.XOR, true);
        [SerializeField] private NumericIModifier numericIModifier = new(NumericIOperation.ADD, 10);
        [SerializeField] private NumericFModifier numericFModifier = new(NumericFOperation.MUL, 1.5f);
        [SerializeField] private StringModifier stringModifier = new(StringOperation.SET, "Xeri");

        [Header("22. CollectionEditor List / Array")]
        [CollectionEditor]
        [SerializeField] private List<int> collectionInts = new()
        {
            10,
            20,
            30,
        };

        [CollectionEditor(Unique = true)]
        [SerializeField] private List<string> collectionUniqueTags = new()
        {
            "Player",
            "Enemy",
            "Interactable",
        };

        [CollectionEditor]
        [SerializeField] private StringModifier[] collectionModifiers =
        {
            new(StringOperation.SET, "A"),
            new(StringOperation.SET, "B"),
        };

        [Header("23. CollectionEditor Type-aware Add")]
        [CollectionEditor]
        [SerializeField] private List<BooleanOperation> collectionEnums = new();

        [CollectionEditor]
        [SerializeField] private List<GameObject> collectionGameObjects = new();

        [CollectionEditor]
        [SerializeReference] private List<IGameObjectProvider> collectionReferenceProviders = new();

        [CollectionEditor]
        [SerializeReference] private IGameObjectProvider[] collectionReferenceProviderArray =
            Array.Empty<IGameObjectProvider>();

        [CollectionEditor]
        [SerializeReference] private List<PrefabGameObjectProvider> collectionConcreteReferences = new();

    #endregion

    }
}
