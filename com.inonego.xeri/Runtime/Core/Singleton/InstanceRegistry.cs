/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : InstanceRegistry.cs
수정일 : 2026-04-29

# 설명
범용 슬롯 레지스트리. 싱글톤 전용이 아니며 독립 인스턴스를 여러 개 생성할 수 있다.
InstanceRegistry   : AsyncLocal 기반 컨텍스트 전환(Scope)과 키 검증(Normalize) 담당.
InstanceRegistry<T>: 슬롯 이름으로 인스턴스를 저장·조회(Register, Unregister, Current, Named, Clear) 담당.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace inonego.Xeri
{
    // ======================================================================
    /// <summary>
    /// <br/> 슬롯 키 컨텍스트 관리 기반 클래스.
    /// <br/> DEFAULT_SLOT, Normalize, Scope API를 제공한다.
    /// </summary>
    // ======================================================================
    public abstract class InstanceRegistry
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// Dispose 시 이전 슬롯으로 복원하는 스코프 핸들.
        /// </summary>
        // ============================================================
        private class ScopeHandle : IDisposable
        {
            private readonly InstanceRegistry owner;
            private readonly string           previous;

            public ScopeHandle(InstanceRegistry owner, string previous)
            {
                this.owner    = owner;
                this.previous = previous;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 이전 슬롯으로 복원한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Dispose() => owner.current.Value = previous;
        }

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 슬롯 키.
        /// </summary>
        // ------------------------------------------------------------
        public const string DEFAULT_SLOT = "";

        protected readonly AsyncLocal<string> current = new();

    #endregion

    #region 생성자

        protected InstanceRegistry()
        {
            current.Value = DEFAULT_SLOT;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// null 키 사용 시 예외를 발생시킨다.
        /// </summary>
        // ------------------------------------------------------------
        internal static string Normalize(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), $"슬롯 키로 null을 사용할 수 없습니다. 기본 슬롯을 사용하려면 DEFAULT_SLOT을 사용하세요.");
            }

            return key;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 슬롯을 현재 컨텍스트로 설정하는 스코프를 반환한다.
        /// <br/> Dispose 시 이전 슬롯으로 자동 복원된다.
        /// </summary>
        // ------------------------------------------------------------
        public IDisposable Scope(string key)
        {
            var normalized = Normalize(key);
            var previous   = current.Value;
            current.Value  = normalized;

            return new ScopeHandle(this, previous);
        }

    #endregion

    }

    // ======================================================================
    /// <summary>
    /// <br/> 이름 기반 인스턴스 레지스트리.
    /// <br/> Register / Current / Named / Scope / Clear API를 제공한다.
    /// </summary>
    // ======================================================================
    public class InstanceRegistry<T> : InstanceRegistry
    where T : class
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 이름으로 슬롯 인스턴스에 직접 접근하는 헬퍼.
        /// </summary>
        // ============================================================
        public class NamedAccessor
        {
            private readonly InstanceRegistry<T> owner;

            internal NamedAccessor(InstanceRegistry<T> owner)
            {
                this.owner = owner;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정한 이름의 슬롯 인스턴스를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public T this[string key]
            {
                get
                {
                    var normalized = Normalize(key);
                    return owner.instances[normalized];
                }
            }
        }

    #endregion

    #region 필드

        internal readonly Dictionary<string, T> instances    = new();
        private  readonly List<string>          removeBuffer = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 컨텍스트의 슬롯 인스턴스. 미등록 슬롯 접근 시 예외.
        /// </summary>
        // ------------------------------------------------------------
        public T Current
        {
            get
            {
                var key = Normalize(current.Value);

                if (!instances.TryGetValue(key, out var inst))
                {
                    throw new InvalidOperationException($"슬롯 '{key}'가 등록되지 않았습니다.");
                }

                return inst;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이름으로 슬롯에 직접 접근한다.
        /// </summary>
        // ------------------------------------------------------------
        public NamedAccessor Named { get; }

    #endregion

    #region 생성자

        public InstanceRegistry()
        {
            Named = new NamedAccessor(this);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// DEFAULT_SLOT에 인스턴스를 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Register(T instance)
        {
            Register(DEFAULT_SLOT, instance);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 슬롯에 인스턴스를 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Register(string key, T instance)
        {
            var normalized = Normalize(key);
            instances[normalized] = instance;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 슬롯 등록을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Unregister(string key)
        {
            var normalized = Normalize(key);
            instances.Remove(normalized);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 인스턴스가 등록된 모든 슬롯을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Unregister(T instance)
        {
            foreach (var (key, value) in instances)
            {
                if (ReferenceEquals(value, instance))
                {
                    removeBuffer.Add(key);
                }
            }

            foreach (var key in removeBuffer)
            {
                instances.Remove(key);
            }

            removeBuffer.Clear();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 슬롯을 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            instances.Clear();
        }

    #endregion

    }
}
