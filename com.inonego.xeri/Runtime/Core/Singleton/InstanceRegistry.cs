/* BLOCK_HEADER_BEGIN ======================================================================================
파일명 : InstanceRegistry.cs
수정일 : 2026-05-03

# 설명
"이름표가 붙은 여러 인스턴스 중 지금 어떤 것을 쓸지"를 관리하는 레지스트리.
- Register("SLOT", instance) 로 이름을 붙여 등록한다.
- Current 로 지금 활성화된 슬롯의 인스턴스를 가져온다.
- Scope("SLOT") / using 블록으로 일시적으로 슬롯을 전환하고, 블록 종료 시 자동 복원한다.
- OpenScope("SLOT") + CloseScope() 는 using 없이 같은 동작을 수동으로 수행한다 (테스트 SetUp/TearDown 용).

# 슬롯 전환 원리
"현재 슬롯"은 스레드/async 컨텍스트별로 독립적으로 관리된다.
OpenScope를 호출할 때마다 이전 슬롯 키를 기억하는 노드를 하나 쌓아 올리고,
CloseScope 시 그 노드를 꺼내 이전 슬롯으로 되돌린다.
Scope() + OpenScope() + CloseScope() 세 API는 모두 같은 스택을 공유하므로 혼용해도 LIFO 순서가 보장된다.
======================================================================================== BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace inonego.Xeri
{
    // ======================================================================
    /// <summary>
    /// <br/> 슬롯 키 컨텍스트 관리 기반 클래스.
    /// <br/> DEFAULT_SLOT, Normalize, OpenScope/CloseScope/Scope API를 제공한다.
    /// </summary>
    // ======================================================================
    public abstract class InstanceRegistry
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// <br/> 슬롯 키 이력을 추적하는 불변 연결 노드.
        /// <br/> 각 OpenScope마다 새 인스턴스를 생성해 재할당하므로
        /// <br/> AsyncLocal의 async 컨텍스트 격리가 보장된다.
        /// </summary>
        // ============================================================
        private sealed class ScopeNode
        {
            public readonly string    Key;
            public readonly ScopeNode Previous;

            public ScopeNode(string key, ScopeNode previous)
            {
                Key      = key;
                Previous = previous;
            }
        }

        // ============================================================
        /// <summary>
        /// Dispose 시 CloseScope를 호출해 이전 슬롯으로 복원하는 스코프 핸들.
        /// </summary>
        // ============================================================
        private sealed class CloseScopeHandle : IDisposable
        {
            private readonly InstanceRegistry owner;
            private          bool             disposed;

            public CloseScopeHandle(InstanceRegistry owner) => this.owner = owner;

            // ------------------------------------------------------------
            /// <summary>
            /// CloseScope를 호출해 이전 슬롯으로 복원한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                owner.CloseScope();
            }
        }

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 슬롯 키.
        /// </summary>
        // ------------------------------------------------------------
        public const string DEFAULT_SLOT = "";

        private readonly AsyncLocal<ScopeNode> scopeStack = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 컨텍스트의 슬롯 키. 열린 스코프가 없으면 DEFAULT_SLOT을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        protected string CurrentKey
        {
            get
            {
                var node = scopeStack.Value;
                return node != null ? node.Key : DEFAULT_SLOT;
            }
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
        /// <br/> 지정한 슬롯을 현재 컨텍스트로 설정한다.
        /// <br/> CloseScope()로 명시적으로 닫을 때까지 유지된다.
        /// <br/> 프로덕션 코드에서는 Scope() + using을 사용할 것.
        /// </summary>
        // ------------------------------------------------------------
        public void OpenScope(string key)
        {
            scopeStack.Value = new ScopeNode(Normalize(key), scopeStack.Value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 가장 최근에 열린 스코프를 닫고 이전 슬롯으로 복원한다.
        /// <br/> 열린 스코프가 없을 시 InvalidOperationException이 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloseScope()
        {
            var node = scopeStack.Value ?? throw new InvalidOperationException("닫을 스코프가 없습니다. OpenScope를 먼저 호출하세요.");

            scopeStack.Value = node.Previous;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 슬롯을 현재 컨텍스트로 설정하는 스코프를 반환한다.
        /// <br/> Dispose 시 이전 슬롯으로 자동 복원된다.
        /// </summary>
        // ------------------------------------------------------------
        public IDisposable Scope(string key)
        {
            OpenScope(key);
            return new CloseScopeHandle(this);
        }

    #endregion

    }

    // ======================================================================
    /// <summary>
    /// <br/> 이름 기반 인스턴스 레지스트리.
    /// <br/> Register / Current / Named / Scope / OpenScope / CloseScope / Clear API를 제공한다.
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

            // ------------------------------------------------------------
            /// <summary>
            /// 지정한 이름의 슬롯 인스턴스를 시도해 반환한다. 없으면 false.
            /// </summary>
            // ------------------------------------------------------------
            public bool TryGet(string key, out T instance)
            {
                var normalized = Normalize(key);
                return owner.instances.TryGetValue(normalized, out instance);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정한 이름의 슬롯이 등록되어 있는지 확인한다.
            /// </summary>
            // ------------------------------------------------------------
            public bool Contains(string key)
            {
                var normalized = Normalize(key);
                return owner.instances.ContainsKey(normalized);
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
                var key = Normalize(CurrentKey);

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
        /// 현재 컨텍스트의 슬롯 인스턴스를 안전하게 가져온다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryCurrent(out T instance)
        {
            return instances.TryGetValue(CurrentKey, out instance);
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
