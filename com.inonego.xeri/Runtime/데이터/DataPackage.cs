/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DataPackage.cs
수정일 : 2026-05-01

# 설명
데이터 패키지 중앙 관리자.
InstanceRegistry<DataPackage>를 통한 슬롯·Scope 시스템으로 여러 DataPackage를 이름별로 관리한다.

# 슬롯 시스템
- Register(pkg)           : DEFAULT_SLOT에 등록
- Register(slot, pkg)     : 지정 슬롯에 등록
- Unregister(slot)        : 슬롯 해제
- Current                 : 현재 컨텍스트 슬롯의 DataPackage (IReadOnlyDataPackage)
- Named[slot]             : 슬롯 이름으로 직접 접근
- Scope(slot)             : using 블록 안에서 일시적으로 슬롯 전환, Dispose 시 복원

# 이벤트
- OnChange: Register / Unregister / Clear 시 발생. Scope 전환 시에는 발생하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri
{
    using Serializable;

    // ================================================================
    /// <summary>
    /// 데이터 패키지 읽기 전용 인터페이스.
    /// </summary>
    // ================================================================
    public interface IReadOnlyDataPackage
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 키로 데이터를 읽는다. 없으면 예외.
        /// </summary>
        // ------------------------------------------------------------
        public TTableValue Read<TTableValue>(string key)
        where TTableValue : class, ITableValue;

        // ------------------------------------------------------------
        /// <summary>
        /// 키로 데이터를 읽는다. 테이블이 없거나 키가 없으면 null 반환.
        /// </summary>
        // ------------------------------------------------------------
        public TTableValue TryRead<TTableValue>(string key)
        where TTableValue : class, ITableValue;

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 타입의 테이블을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyTable<TTableValue> Table<TTableValue>()
        where TTableValue : class, ITableValue;

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 타입의 테이블을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyTable Table(Type valueType);
    }

    // ================================================================
    /// <summary>
    /// <br/> 데이터 패키지. 슬롯 기반으로 여러 인스턴스를 관리한다.
    /// <br/> Register로 등록하고 Current로 현재 슬롯을 조회한다.
    /// </summary>
    // ================================================================
    [Serializable]
    public class DataPackage : IReadOnlyDataPackage
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// <br/> Named 슬롯 접근자.
        /// <br/> InstanceRegistry의 NamedAccessor를 상속해 IReadOnlyDataPackage로 노출한다.
        /// </summary>
        // ============================================================
        public class NamedAccessor : InstanceRegistry<DataPackage>.NamedAccessor
        {
            internal NamedAccessor(InstanceRegistry<DataPackage> owner) : base(owner) {}

            // ------------------------------------------------------------
            /// <summary>
            /// 지정 슬롯의 DataPackage를 IReadOnlyDataPackage로 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public new IReadOnlyDataPackage this[string slot] => base[slot];
        }

    #endregion

    #region 정적 필드

        private static readonly InstanceRegistry<DataPackage> registry = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 컨텍스트의 DataPackage. 미등록 슬롯 접근 시 예외.
        /// </summary>
        // ------------------------------------------------------------
        public static IReadOnlyDataPackage Current => registry.Current;

        // ------------------------------------------------------------
        /// <summary>
        /// 슬롯 이름으로 DataPackage에 직접 접근한다.
        /// </summary>
        // ------------------------------------------------------------
        public static readonly NamedAccessor Named = new(registry);

        // ------------------------------------------------------------
        /// <summary>
        /// Register / Unregister / Clear 시 발생하는 이벤트.
        /// </summary>
        // ------------------------------------------------------------
        public static event Action OnChange;

    #endregion

    #region 정적 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// DEFAULT_SLOT에 DataPackage를 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Register(DataPackage package)
        {
            registry.Register(package);
            OnChange?.Invoke();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 슬롯에 DataPackage를 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Register(string slot, DataPackage package)
        {
            registry.Register(slot, package);
            OnChange?.Invoke();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 슬롯의 등록을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Unregister(string slot)
        {
            registry.Unregister(slot);
            OnChange?.Invoke();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 인스턴스가 등록된 모든 슬롯을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Unregister(DataPackage package)
        {
            registry.Unregister(package);
            OnChange?.Invoke();
        }

        // --------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정 슬롯을 현재 컨텍스트로 전환하는 스코프를 반환한다.
        /// <br/> Dispose 시 이전 슬롯으로 자동 복원된다. OnChange를 발생시키지 않는다.
        /// </summary>
        // --------------------------------------------------------------------
        public static IDisposable Scope(string slot) => registry.Scope(slot);

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 슬롯을 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Clear()
        {
            registry.Clear();
            OnChange?.Invoke();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 슬롯의 DataPackage를 안전하게 가져온다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryCurrent(out IReadOnlyDataPackage package)
        {
            var result = registry.TryCurrent(out var pkg);
            package = pkg;
            return result;
        }

    #endregion

    #region 필드

        [SerializeField]
        private XDictionary_VR<XType, ITable> dictionary = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 저장된 타입→테이블 딕셔너리.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyDictionary<XType, ITable> Dictionary => dictionary;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 키로 데이터를 읽는다. 없으면 예외.
        /// </summary>
        // ------------------------------------------------------------
        public TTableValue Read<TTableValue>(string key)
        where TTableValue : class, ITableValue
        {
            return Table<TTableValue>()[key];
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테이블이 없거나 키가 없으면 null을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TTableValue TryRead<TTableValue>(string key)
        where TTableValue : class, ITableValue
        {
            var valueType = typeof(TTableValue);

            if (!dictionary.TryGetValue(valueType, out ITable lTable))
            {
                return null;
            }

            return (lTable as IReadOnlyTable<TTableValue>)?[key];
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 타입의 테이블을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyTable<TTableValue> Table<TTableValue>()
        where TTableValue : class, ITableValue
        {
            return Table(typeof(TTableValue)) as IReadOnlyTable<TTableValue>;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 타입의 테이블을 반환한다. 없으면 예외.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyTable Table(Type valueType)
        {
            if (dictionary.TryGetValue(valueType, out ITable lTable))
            {
                return lTable;
            }

            throw new InvalidOperationException($"데이터 패키지에 {valueType.Name} 타입의 테이블이 존재하지 않습니다.");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테이블을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddTable<TTable, TTableValue>(TTable table)
        where TTable : Table<TTableValue>
        where TTableValue : class, ITableValue
        {
            AddTable(typeof(TTableValue), table);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테이블을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddTable(Type valueType, ITable table)
        {
            if (dictionary.ContainsKey(valueType))
            {
                throw new InvalidOperationException($"데이터 패키지에 {valueType.Name} 타입의 테이블이 이미 존재합니다.");
            }

            dictionary.Add(valueType, table);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테이블을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void RemoveTable<TTableValue>()
        where TTableValue : class, ITableValue
        {
            var valueType = typeof(TTableValue);

            if (!dictionary.ContainsKey(valueType))
            {
                throw new InvalidOperationException($"데이터 패키지에 {valueType.Name} 타입의 테이블이 존재하지 않습니다.");
            }

            dictionary.Remove(valueType);
        }

    #endregion

    }
}
