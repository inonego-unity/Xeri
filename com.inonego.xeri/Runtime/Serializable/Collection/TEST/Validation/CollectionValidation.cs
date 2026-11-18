/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionValidation.cs
수정일 : 2026-11-18

# 설명
Collection Editor 수동 검증용 컴포넌트. 컬렉션 종류와 원소 종류를 조합별로 나열해
하나의 Inspector에서 모두 열어 보며 추가·삭제·편집·형식 선택·정렬·검색·Undo를 확인한다.

# 특이사항, 제약사항
UNITY_EDITOR에서만 컴파일되는 검증 전용 어셈블리에 있으며 빌드에는 포함되지 않는다.
시작 데이터는 컴포넌트가 활성화될 때마다 코드로 새로 채운다. 씬이나 Inspector 조작으로 저장된 값은 다음 활성화에서 덮어쓰므로 저장 상태가 검증에 영향을 주지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

using inonego.Xeri;

namespace inonego.Xeri.Serializable.Validation
{
    // ============================================================
    /// <summary>
    /// 컬렉션 종류별 Collection Editor 수동 검증 컴포넌트.
    /// </summary>
    // ============================================================
    [ExecuteAlways]
    public sealed class CollectionValidation : MonoBehaviour
    {

    #region A. XDictionary

        [Header("A-1. XDictionary Key V / Value V")]
        [SerializeField] private XDictionary_VV<string, int> dictionaryStringInt = new();
        [SerializeField] private XDictionary_VV<int, string> dictionaryIntString = new();
        [SerializeField] private XDictionary_VV<ValidationKind, string> dictionaryEnumString = new();

        [Header("A-2. XDictionary Key V / Value R (SerializeReference)")]
        [SerializeField] private XDictionary_VR<string, IValidationItem> dictionaryStringReference = new();
        [SerializeField] private XDictionary_VR<int, IValidationItem> dictionaryIntReference = new();

        [Header("A-3. XDictionary Key R / Value V")]
        [SerializeField] private XDictionary_RV<IValidationItem, int> dictionaryReferenceInt = new();

        [Header("A-4. XDictionary Key R / Value R")]
        [SerializeField] private XDictionary_RR<IValidationItem, IValidationItem> dictionaryReferenceReference = new();

        [Header("A-5. XDictionary 비어 있음 / ReadOnly")]
        [SerializeField] private XDictionary_VV<string, int> dictionaryEmpty = new();

        [ReadOnly]
        [SerializeField] private XDictionary_VV<string, int> dictionaryReadOnly = new();

    #endregion

    #region B. XHashSet / XQueue / XStack

        [Header("B-1. XHashSet")]
        [SerializeField] private XHashSet_V<int> hashSetInt = new();
        [SerializeField] private XHashSet_V<string> hashSetString = new();
        [SerializeField] private XHashSet_R<IValidationItem> hashSetReference = new();

        [Header("B-2. XQueue")]
        [SerializeField] private XQueue_V<int> queueInt = new();
        [SerializeField] private XQueue_R<IValidationItem> queueReference = new();

        [Header("B-3. XStack")]
        [SerializeField] private XStack_V<int> stackInt = new();
        [SerializeField] private XStack_R<IValidationItem> stackReference = new();

    #endregion

    #region C. XOrdered / XPriorityQueue

        [Header("C-1. XOrdered (순서 Key)")]
        [SerializeField] private XOrdered<int, IValidationItem> orderedInt = new();
        [SerializeField] private XOrdered<float, IValidationItem> orderedFloat = new();

        [Header("C-2. XOrdered (순서 + 고유 Key)")]
        [SerializeField] private XOrdered<int, string, IValidationItem> orderedKeyed = new();

        [Header("C-3. XPriorityQueue")]
        [SerializeField] private XPriorityQueue<IValidationItem, int> priorityQueue = new();

    #endregion

    #region D. [CollectionEditor] List — 값 원소

        [Header("D-1. 숫자 / 문자열 / 논리")]
        [CollectionEditor]
        [SerializeField] private List<int> listInt = new();

        [CollectionEditor]
        [SerializeField] private List<float> listFloat = new();

        [CollectionEditor(Unique = true)]
        [SerializeField] private List<string> listUniqueString = new();

        [CollectionEditor]
        [SerializeField] private List<bool> listBool = new();

        [Header("D-2. 열거형 / 색 / 벡터")]
        [CollectionEditor]
        [SerializeField] private List<ValidationKind> listEnum = new();

        [CollectionEditor]
        [SerializeField] private List<Color> listColor = new();

        [CollectionEditor]
        [SerializeField] private List<Vector3> listVector3 = new();

        [Header("D-3. Unity 오브젝트 참조")]
        [CollectionEditor]
        [SerializeField] private List<GameObject> listGameObject = new();

        [CollectionEditor]
        [SerializeField] private List<Texture2D> listTexture = new();

        [Header("D-4. 대량 항목 (검색·페이지 이동)")]
        [CollectionEditor(VisibleRows = 8)]
        [SerializeField] private List<int> listLarge = new();

        [Header("D-5. 비어 있음 / ReadOnly")]
        [CollectionEditor]
        [SerializeField] private List<int> listEmpty = new();

        [ReadOnly]
        [CollectionEditor]
        [SerializeField] private List<int> listReadOnly = new();

    #endregion

    #region E. [CollectionEditor] List / 배열 — 복합 원소

        [Header("E-1. 값 타입 원소 (Columns 지정 / 미지정)")]
        [CollectionEditor(Columns = new[] { "Label", "Amount", "Enabled", "Kind" })]
        [SerializeField] private List<ValidationRow> listRowColumns = new();

        [CollectionEditor]
        [SerializeField] private ValidationRow[] arrayRow = Array.Empty<ValidationRow>();

        [Header("E-2. 클래스 원소 (다형성 없음)")]
        [CollectionEditor]
        [SerializeField] private List<ValidationGroupItem> listGroup = new();

    #endregion

    #region F. [CollectionEditor] — SerializeReference 원소

        [Header("F-1. 인터페이스 선언 (형식 선택 필요)")]
        [CollectionEditor]
        [SerializeReference] private List<IValidationItem> listReference = new();

        [CollectionEditor]
        [SerializeReference] private IValidationItem[] arrayReference = Array.Empty<IValidationItem>();

        [Header("F-2. 구체 클래스 선언 (바로 생성)")]
        [CollectionEditor]
        [SerializeReference] private List<ValidationNumberItem> listConcreteReference = new();

        [Header("F-3. 비어 있음")]
        [CollectionEditor]
        [SerializeReference] private List<IValidationItem> listReferenceEmpty = new();

    #endregion

    #region 시작 데이터

        // ------------------------------------------------------------
        /// <summary>
        /// 활성화될 때마다 시작 데이터를 코드로 새로 채운다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnEnable()
        {
            Seed();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 컬렉션을 비우고 시작 데이터를 다시 채운다.
        /// </summary>
        // ------------------------------------------------------------
        private void Seed()
        {
            Clear();

            dictionaryStringInt.Add("ONE", 1);
            dictionaryStringInt.Add("TWO", 2);
            dictionaryStringInt.Add("THREE", 3);
            dictionaryIntString.Add(10, "TEN");
            dictionaryIntString.Add(20, "TWENTY");
            dictionaryEnumString.Add(ValidationKind.Alpha, "FIRST");
            dictionaryEnumString.Add(ValidationKind.Beta, "SECOND");

            dictionaryStringReference.Add("NUMBER", new ValidationNumberItem(7));
            dictionaryStringReference.Add("TEXT", new ValidationTextItem("HELLO"));
            dictionaryStringReference.Add("GROUP", new ValidationGroupItem("G", 1, 2));
            dictionaryIntReference.Add(1, new ValidationNumberItem(1));
            dictionaryIntReference.Add(2, new ValidationTextItem("TWO", ValidationKind.Beta));

            dictionaryReferenceInt.Add(new ValidationNumberItem(1), 100);
            dictionaryReferenceInt.Add(new ValidationTextItem("KEY"), 200);

            dictionaryReferenceReference.Add(new ValidationNumberItem(1), new ValidationTextItem("VALUE"));
            dictionaryReferenceReference.Add(new ValidationTextItem("K"), new ValidationGroupItem("V", 4, 5));

            dictionaryReadOnly.Add("FIXED", 1);
            dictionaryReadOnly.Add("READONLY", 2);

            hashSetInt.Add(1);
            hashSetInt.Add(2);
            hashSetInt.Add(3);
            hashSetString.Add("A");
            hashSetString.Add("B");
            hashSetReference.Add(new ValidationNumberItem(1));
            hashSetReference.Add(new ValidationTextItem("SET"));

            queueInt.Enqueue(1);
            queueInt.Enqueue(2);
            queueInt.Enqueue(3);
            queueReference.Enqueue(new ValidationNumberItem(1));
            queueReference.Enqueue(new ValidationTextItem("QUEUE"));

            stackInt.Push(1);
            stackInt.Push(2);
            stackInt.Push(3);
            stackReference.Push(new ValidationNumberItem(1));
            stackReference.Push(new ValidationTextItem("STACK"));

            orderedInt.Add(10, new ValidationNumberItem(10));
            orderedInt.Add(20, new ValidationTextItem("TWENTY"));
            orderedInt.Add(5, new ValidationGroupItem("FIVE", 5));
            orderedFloat.Add(0.5f, new ValidationNumberItem(1));
            orderedFloat.Add(1.5f, new ValidationTextItem("FLOAT"));
            orderedKeyed.Add(1, "ALPHA", new ValidationNumberItem(1));
            orderedKeyed.Add(2, "BETA", new ValidationTextItem("BETA"));

            priorityQueue.Enqueue(new ValidationNumberItem(1), 5);
            priorityQueue.Enqueue(new ValidationTextItem("HIGH"), 1);
            priorityQueue.Enqueue(new ValidationGroupItem("LOW", 9), 9);

            listInt.AddRange(new[] { 10, 20, 30 });
            listFloat.AddRange(new[] { 0.5f, 1.25f, 2f });
            listUniqueString.AddRange(new[] { "PLAYER", "ENEMY", "INTERACTABLE" });
            listBool.AddRange(new[] { true, false });
            listEnum.AddRange(new[] { ValidationKind.Alpha, ValidationKind.Gamma });
            listColor.AddRange(new[] { Color.red, Color.green, Color.blue });
            listVector3.AddRange(new[] { Vector3.zero, Vector3.one });

            for (var index = 0; index < 120; index++)
            {
                listLarge.Add(index * 3);
            }

            listReadOnly.AddRange(new[] { 1, 2, 3 });

            listRowColumns.Add(new ValidationRow("SWORD", 3, 0.5f, true, ValidationKind.Alpha));
            listRowColumns.Add(new ValidationRow("SHIELD", 1, 1f, false, ValidationKind.Beta));
            arrayRow = new[]
            {
                new ValidationRow("A", 1, 0.1f, true, ValidationKind.Gamma),
                new ValidationRow("B", 2, 0.2f, false, ValidationKind.Delta),
            };
            listGroup.Add(new ValidationGroupItem("FIRST", 1, 2, 3));
            listGroup.Add(new ValidationGroupItem("SECOND", 4, 5));

            listReference.Add(new ValidationNumberItem(1));
            listReference.Add(new ValidationTextItem("TWO"));
            listReference.Add(new ValidationGroupItem("THREE", 3));
            arrayReference = new IValidationItem[]
            {
                new ValidationNumberItem(10),
                new ValidationTextItem("ARRAY"),
            };
            listConcreteReference.Add(new ValidationNumberItem(1));
            listConcreteReference.Add(new ValidationNumberItem(2));

            ApplyRuntime();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 컬렉션을 비운다.
        /// </summary>
        // ------------------------------------------------------------
        private void Clear()
        {
            dictionaryStringInt.Clear();
            dictionaryIntString.Clear();
            dictionaryEnumString.Clear();
            dictionaryStringReference.Clear();
            dictionaryIntReference.Clear();
            dictionaryReferenceInt.Clear();
            dictionaryReferenceReference.Clear();
            dictionaryEmpty.Clear();
            dictionaryReadOnly.Clear();

            hashSetInt.Clear();
            hashSetString.Clear();
            hashSetReference.Clear();
            queueInt.Clear();
            queueReference.Clear();
            stackInt.Clear();
            stackReference.Clear();

            orderedInt.Clear();
            orderedFloat.Clear();
            orderedKeyed.Clear();
            priorityQueue.Clear();

            listInt.Clear();
            listFloat.Clear();
            listUniqueString.Clear();
            listBool.Clear();
            listEnum.Clear();
            listColor.Clear();
            listVector3.Clear();
            listGameObject.Clear();
            listTexture.Clear();
            listLarge.Clear();
            listEmpty.Clear();
            listReadOnly.Clear();

            listRowColumns.Clear();
            arrayRow = Array.Empty<ValidationRow>();
            listGroup.Clear();

            listReference.Clear();
            arrayReference = Array.Empty<IValidationItem>();
            listConcreteReference.Clear();
            listReferenceEmpty.Clear();

            ApplyRuntime();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime 컬렉션의 변경을 직렬화 목록에 명시적으로 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyRuntime()
        {
            var collections = new ISerializedRuntimeCollection[]
            {
                dictionaryStringInt, dictionaryIntString, dictionaryEnumString,
                dictionaryStringReference, dictionaryIntReference,
                dictionaryReferenceInt, dictionaryReferenceReference,
                dictionaryEmpty, dictionaryReadOnly,
                hashSetInt, hashSetString, hashSetReference,
                queueInt, queueReference,
                stackInt, stackReference,
            };

            foreach (var collection in collections)
            {
                collection.ApplyRuntime();
            }
        }

    #endregion

    }
}
