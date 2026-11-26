/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ValueRegistry.cs
수정일 : 2026-11-26

# 설명
이름 붙은 string/int/float/flag 값을 타입별 런타임 컬렉션에 보관하고 변경을 알린다.
Unity 직렬화 콜백에서 런타임 값과 저장용 목록을 동기화한다.
조회는 IValueLookup, 조회와 변경 관찰은 IReadOnlyValueRegistry로 노출할 수 있다.

# 제약사항
하나의 Key는 하나의 값 타입에만 속하며 구조화된 도메인 State를 대신하지 않는다.
타입별 컬렉션은 그 타입의 값이 처음 들어올 때 만든다. 아직 없는 타입의 조회 프로퍼티는 공유 빈 컬렉션을 반환한다.
Unity 역직렬화는 빈 컬렉션도 만들어 채우므로 복원된 Registry는 네 컬렉션을 모두 가진다.
CloneFrom은 상태 복제 과정에서 변경 이벤트를 발행하지 않는다.
Rename은 이름 교환에도 값이 섞이지 않고 검사에 실패하면 아무것도 바꾸지 않으며, 바뀌면 하나의 변경으로 알린다.
IsSameAs는 모든 타입의 값 구성이 같은지 비교하며 변경 이벤트와 무관하다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ======================================================================
    /// <summary>
    /// <br/> 이름 붙은 primitive 값과 Flag를 타입별 런타임 컬렉션에 보관한다.
    /// <br/> Unity 직렬화 과정에서 저장 목록과 런타임 상태를 동기화한다.
    /// </summary>
    // ======================================================================
    [Serializable]
    public class ValueRegistry : ISerializationCallbackReceiver, IReadOnlyValueRegistry, IDeepCloneable<ValueRegistry>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// string 값 목록을 읽기 전용으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyDictionary<string, string> ValSs => valSs ?? emptySs;
        [SerializeField]
        private XDictionary_VV<string, string> valSs = null;

        // ------------------------------------------------------------
        /// <summary>
        /// int 값 목록을 읽기 전용으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyDictionary<string, int> ValIs => valIs ?? emptyIs;
        [SerializeField]
        private XDictionary_VV<string, int> valIs = null;

        // ------------------------------------------------------------
        /// <summary>
        /// float 값 목록을 읽기 전용으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyDictionary<string, float> ValFs => valFs ?? emptyFs;
        [SerializeField]
        private XDictionary_VV<string, float> valFs = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 등록된 Flag 키 목록을 읽기 전용으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyCollection<string> ValFlags => valFlags ?? emptyFlags;
        [SerializeField]
        private XHashSet_V<string> valFlags = null;

        // 아직 값이 없는 타입의 조회 프로퍼티가 돌려주는 공유 빈 컬렉션. Core 읽기 전용 View와 같은 방식이다.
        private static readonly Dictionary<string, string> emptySs = new();
        private static readonly Dictionary<string, int> emptyIs = new();
        private static readonly Dictionary<string, float> emptyFs = new();
        private static readonly HashSet<string> emptyFlags = new();

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Registry의 실제 값 구성이 변경된 뒤 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action OnChange = null;

    #endregion

    #region 직렬화와 런타임 투영

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Unity 저장 전에 네 컬렉션의 런타임 값을 직렬화 목록에 반영한다.
        /// <br/> 개별 컬렉션의 명시적 ApplyRuntime 계약을 이 Registry에서 수행한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void OnBeforeSerialize()
        {
            // 아직 만들지 않은 컬렉션은 반영할 값이 없다.
            ((ISerializedRuntimeCollection)valSs)?.ApplyRuntime();
            ((ISerializedRuntimeCollection)valIs)?.ApplyRuntime();
            ((ISerializedRuntimeCollection)valFs)?.ApplyRuntime();
            ((ISerializedRuntimeCollection)valFlags)?.ApplyRuntime();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 역직렬화 후 저장 목록을 타입별 런타임 컬렉션으로 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void OnAfterDeserialize()
        {
            ((ISerializedRuntimeCollection)valSs)?.UpdateRuntime();
            ((ISerializedRuntimeCollection)valIs)?.UpdateRuntime();
            ((ISerializedRuntimeCollection)valFs)?.UpdateRuntime();
            ((ISerializedRuntimeCollection)valFlags)?.UpdateRuntime();
        }

    #endregion

    #region 공통 연산

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키의 값이 존재하는지 확인합니다.
        /// </summary>
        // ------------------------------------------------------------
        private bool Has<T>(XDictionary_VV<string, T> dictionary, string key)
        {
            return dictionary != null && dictionary.ContainsKey(key);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키가 존재하는지 확인합니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Has(string key) => HasS(key) || HasI(key) || HasF(key) || HasFlag(key);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값을 설정합니다.
        /// </summary>
        // ------------------------------------------------------------
        private T Set<T>(ref XDictionary_VV<string, T> dictionary, string key, T value)
        {
            if (Has(dictionary, key))
            {
                // 동일 값 재설정은 Registry 상태 변화가 아니므로 알림을 생략한다.
                if (EqualityComparer<T>.Default.Equals(dictionary[key], value)) return value;

                dictionary[key] = value;
                OnChange?.Invoke();
                return value;
            }

            if (Has(key))
            {
                throw new InvalidOperationException($"키({key})에 해당하는 값이 다른 타입으로 존재합니다.");
            }

            // 그 타입의 첫 값이면 이때 컬렉션을 만든다.
            dictionary ??= new();

            // 신규 Key 등록은 fallback 값과 같더라도 Registry 구성 변화로 취급한다.
            dictionary.Add(key, value);
            OnChange?.Invoke();

            return value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키를 제거합니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Remove(string key)
        {
            var removed =  (valSs?.Remove(key) ?? false)
                        || (valIs?.Remove(key) ?? false)
                        || (valFs?.Remove(key) ?? false)
                        || (valFlags?.Remove(key) ?? false);
            if (!removed) return false;

            // 실제 Key 제거가 완료된 뒤 Registry 변경을 외부에 알린다.
            OnChange?.Invoke();
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>키 이름을 한 번에 바꿉니다. map은 이전 키 → 새 키이며 새 키가 null이면 그 키를 제거합니다.
        /// <br/>없는 키는 건너뛰고, 이름 교환(a↔b)도 값이 섞이지 않습니다.
        /// <br/>둘 이상이 같은 새 키로 가거나 새 키가 바뀌지 않는 기존 키와 겹치면 아무것도 바꾸지 않고 예외를 던집니다.
        /// </summary>
        // ------------------------------------------------------------
        public void Rename(IReadOnlyDictionary<string, string> map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            var moves = new List<(string From, string To)>();
            foreach (var pair in map)
            {
                if (pair.Key != null && Has(pair.Key))
                {
                    moves.Add((pair.Key, pair.Value));
                }
            }

            if (moves.Count == 0) return;

            // 바꾸기 전에 모두 검사한다. 떠나는 키는 새 키와 겹쳐도 비는 자리가 된다.
            var leaving = new HashSet<string>();
            foreach (var (oldKey, _) in moves)
            {
                leaving.Add(oldKey);
            }

            var targets = new HashSet<string>();
            foreach (var (_, newKey) in moves)
            {
                if (newKey == null) continue;

                if (!targets.Add(newKey))
                {
                    throw new InvalidOperationException($"둘 이상의 키가 같은 키({newKey})로 바뀝니다.");
                }

                if (Has(newKey) && !leaving.Contains(newKey))
                {
                    throw new InvalidOperationException($"바꿀 키({newKey})가 이미 존재합니다.");
                }
            }

            // 값을 먼저 모두 꺼낸 뒤 옛 키를 지우고 새 키로 넣는다. 종류는 0:string 1:int 2:float 3:flag다.
            var held = new List<(string NewKey, int Kind, string Text, int Number, float Real)>();
            foreach (var (oldKey, newKey) in moves)
            {
                if (HasS(oldKey)) held.Add((newKey, 0, valSs[oldKey], 0, 0f));
                else if (HasI(oldKey)) held.Add((newKey, 1, null, valIs[oldKey], 0f));
                else if (HasF(oldKey)) held.Add((newKey, 2, null, 0, valFs[oldKey]));
                else held.Add((newKey, 3, null, 0, 0f));
            }

            foreach (var (oldKey, _) in moves)
            {
                valSs?.Remove(oldKey);
                valIs?.Remove(oldKey);
                valFs?.Remove(oldKey);
                valFlags?.Remove(oldKey);
            }

            // 옮겨 온 값의 타입 컬렉션은 꺼낼 때 있었으므로 그대로 있다.
            foreach (var (newKey, kind, text, number, real) in held)
            {
                if (newKey == null) continue;

                switch (kind)
                {
                    case 0: valSs.Add(newKey, text); break;
                    case 1: valIs.Add(newKey, number); break;
                    case 2: valFs.Add(newKey, real); break;
                    default: valFlags.Add(newKey); break;
                }
            }

            // 이름 변경은 키 수와 무관하게 하나의 변경으로 알린다.
            OnChange?.Invoke();
        }

    #endregion

    #region 전체 연산

        // ------------------------------------------------------------
        /// <summary>
        /// 어떤 타입이든 값이 하나라도 있는지 확인합니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasAny => (valSs?.Count ?? 0) > 0 || (valIs?.Count ?? 0) > 0 || (valFs?.Count ?? 0) > 0 || (valFlags?.Count ?? 0) > 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 타입의 값을 제거합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            // 이미 비어 있는 Registry는 관찰 가능한 상태 변화가 없다.
            if (!HasAny) return;

            valSs?.Clear();
            valIs?.Clear();
            valFs?.Clear();
            valFlags?.Clear();

            // 일괄 정리는 개별 Key 수와 무관하게 하나의 변경으로 알린다.
            OnChange?.Invoke();
        }

    #endregion

    #region ValS Operator

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키의 값(string)이 존재하는지 확인합니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasS(string key) => Has(valSs, key);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키의 값(string)을 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public string GetS(string key, string fallbackValue = "") => HasS(key) ? valSs[key] : fallbackValue;

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(string)을 설정합니다.
        /// </summary>
        // ------------------------------------------------------------
        public string SetS(string key, string value) => Set(ref valSs, key, value);

    #endregion

    #region ValI Operator

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키의 값(int)이 존재하는지 확인합니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasI(string key) => Has(valIs, key);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키의 값(int)을 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public int GetI(string key, int fallbackValue = 0) => HasI(key) ? valIs[key] : fallbackValue;

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(int)을 설정합니다.
        /// </summary>
        // ------------------------------------------------------------
        public int SetI(string key, int value) => Set(ref valIs, key, value);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(int)을 증가시킵니다.
        /// </summary>
        // ------------------------------------------------------------
        public int AddI(string key, int value, int fallbackValue = 0) => Set(ref valIs, key, GetI(key, fallbackValue) + value);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(int)을 감소시킵니다.
        /// </summary>
        // ------------------------------------------------------------
        public int SubI(string key, int value, int fallbackValue = 0) => Set(ref valIs, key, GetI(key, fallbackValue) - value);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(int)을 곱셈시킵니다.
        /// </summary>
        // ------------------------------------------------------------
        public int MulI(string key, int value, int fallbackValue = 0) => Set(ref valIs, key, GetI(key, fallbackValue) * value);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(int)을 나눗셈시킵니다.
        /// </summary>
        // ------------------------------------------------------------
        public int DivI(string key, int value, int fallbackValue = 0) => Set(ref valIs, key, GetI(key, fallbackValue) / value);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(int)을 최대값으로 설정합니다.
        /// </summary>
        // ------------------------------------------------------------
        public int MaxI(string key, int value, int fallbackValue = 0) => Set(ref valIs, key, Math.Max(GetI(key, fallbackValue), value));

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(int)을 최소값으로 설정합니다.
        /// </summary>
        // ------------------------------------------------------------
        public int MinI(string key, int value, int fallbackValue = 0) => Set(ref valIs, key, Math.Min(GetI(key, fallbackValue), value));

    #endregion

    #region ValF Operator

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키의 값(float)이 존재하는지 확인합니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasF(string key) => Has(valFs, key);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키의 값(float)을 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public float GetF(string key, float fallbackValue = 0f) => HasF(key) ? valFs[key] : fallbackValue;

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(float)을 설정합니다.
        /// </summary>
        // ------------------------------------------------------------
        public float SetF(string key, float value) => Set(ref valFs, key, value);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(float)을 증가시킵니다.
        /// </summary>
        // ------------------------------------------------------------
        public float AddF(string key, float value, float fallbackValue = 0f) => Set(ref valFs, key, GetF(key, fallbackValue) + value);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(float)을 감소시킵니다.
        /// </summary>
        // ------------------------------------------------------------
        public float SubF(string key, float value, float fallbackValue = 0f) => Set(ref valFs, key, GetF(key, fallbackValue) - value);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(float)을 곱셈시킵니다.
        /// </summary>
        // ------------------------------------------------------------
        public float MulF(string key, float value, float fallbackValue = 0f) => Set(ref valFs, key, GetF(key, fallbackValue) * value);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(float)을 나눗셈시킵니다.
        /// </summary>
        // ------------------------------------------------------------
        public float DivF(string key, float value, float fallbackValue = 0f) => Set(ref valFs, key, GetF(key, fallbackValue) / value);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(float)을 최대값으로 설정합니다.
        /// </summary>
        // ------------------------------------------------------------
        public float MaxF(string key, float value, float fallbackValue = 0f) => Set(ref valFs, key, Mathf.Max(GetF(key, fallbackValue), value));

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(float)을 최소값으로 설정합니다.
        /// </summary>
        // ------------------------------------------------------------
        public float MinF(string key, float value, float fallbackValue = 0f) => Set(ref valFs, key, Mathf.Min(GetF(key, fallbackValue), value));

    #endregion

    #region ValFlag Operator

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키의 값(bool)을 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasFlag(string key) => valFlags != null && valFlags.Contains(key);

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 키에 값(bool)을 설정합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetFlag(string key)
        {
            if (HasFlag(key))
            {
                return;
            }

            if (Has(key))
            {
                throw new InvalidOperationException($"키({key})에 해당하는 값이 다른 타입으로 존재합니다.");
            }

            // 신규 Flag 등록이 완료된 뒤 Registry 변경을 외부에 알린다. 첫 Flag면 이때 컬렉션을 만든다.
            valFlags ??= new();
            
            valFlags.Add(key);
            OnChange?.Invoke();
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 ValueRegistry 인스턴스를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public ValueRegistry @new() => new ValueRegistry();

        // ------------------------------------------------------------
        /// <summary>
        /// source의 모든 값을 변경 알림 없이 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(ValueRegistry source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // 자기 자신을 비운 뒤 복사하면 값이 사라지므로 복제할 내용이 없다.
            if (ReferenceEquals(source, this))
            {
                return;
            }

            // 복제는 runtime mutation이 아니므로 공개 Clear 경로를 거치지 않고 각 컬렉션이 직접 복사한다.
            CloneCollection(ref valSs, source.valSs);
            CloneCollection(ref valIs, source.valIs);
            CloneCollection(ref valFs, source.valFs);
            CloneCollection(ref valFlags, source.valFlags);
        }

        // 원본이 비었으면 대상을 비우기만 하고 새로 만들지 않는다. 값이 있으면 필요할 때 만든 뒤 복사한다.
        private static void CloneCollection<T>(ref XDictionary_VV<string, T> target, XDictionary_VV<string, T> source)
        {
            if (source == null || source.Count == 0)
            {
                target?.Clear();
                return;
            }

            target ??= new();
            target.CloneFrom(source);
        }

        private static void CloneCollection(ref XHashSet_V<string> target, XHashSet_V<string> source)
        {
            if (source == null || source.Count == 0)
            {
                target?.Clear();
                return;
            }

            target ??= new();
            target.CloneFrom(source);
        }

    #endregion

    #region 비교

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>모든 타입의 값 구성이 같은지 비교합니다. ignoredKey를 주면 그 키는 어떤 타입이든 비교에서 제외합니다.
        /// <br/>변경 이벤트와 무관하며, null이면 같지 않고 같은 객체이면 같습니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsSameAs(ValueRegistry other, string ignoredKey = null)
        {
            if (other == null) return false;
            if (ReferenceEquals(other, this)) return true;

            return Same(valSs, other.valSs, ignoredKey)
                && Same(valIs, other.valIs, ignoredKey)
                && Same(valFs, other.valFs, ignoredKey)
                && Same(valFlags, other.valFlags, ignoredKey);
        }

        // 구체 컬렉션으로 받아 열거자를 박싱하지 않는다. 만들지 않은 컬렉션은 빈 컬렉션으로 본다.
        private static bool Same<T>
        (
            XDictionary_VV<string, T> a, XDictionary_VV<string, T> b, string ignoredKey
        )
        {
            int aCount = a == null ? 0 : a.Count - (ignoredKey != null && a.ContainsKey(ignoredKey) ? 1 : 0);
            int bCount = b == null ? 0 : b.Count - (ignoredKey != null && b.ContainsKey(ignoredKey) ? 1 : 0);
            if (aCount != bCount) return false;
            if (aCount == 0) return true;

            foreach (var pair in a)
            {
                if (pair.Key == ignoredKey) continue;

                if (!b.TryGetValue(pair.Key, out var value) || !EqualityComparer<T>.Default.Equals(pair.Value, value))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Same(XHashSet_V<string> a, XHashSet_V<string> b, string ignoredKey)
        {
            int aCount = a == null ? 0 : a.Count - (ignoredKey != null && a.Contains(ignoredKey) ? 1 : 0);
            int bCount = b == null ? 0 : b.Count - (ignoredKey != null && b.Contains(ignoredKey) ? 1 : 0);
            if (aCount != bCount) return false;
            if (aCount == 0) return true;

            foreach (var key in a)
            {
                if (key != ignoredKey && !b.Contains(key)) return false;
            }

            return true;
        }

    #endregion

    }
}
