/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Singleton.cs
수정일 : 2026-08-03

# 설명
Singleton<T> 정적 API 래퍼 스모크 테스트.
실제 슬롯 로직은 InstanceRegistry<T> 가 보유하며 별도 테스트가 존재한다.
본 파일은 정적 래퍼가 동일 레지스트리에 올바르게 위임되는지만 확인한다.

# 테스트 구성
 E: 정적 API 위임 (Register/TryRegister/Current/TryCurrent/Named/Scope/Clear)

# 특이사항
Singleton<T> 의 static 레지스트리는 T 별로 영속 상태이다.
테스트 간 격리를 위해 [SetUp] 에서 Clear() 를 호출한다.
========================================================================= BLOCK_HEADER_END */

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Core._Singleton
{

    // ============================================================
    /// <summary>
    /// <br/> Singleton&lt;T&gt; 정적 API 위임 동작 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_Singleton
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 테스트 전용 Singleton 파생 더미 타입.
        /// </summary>
        // ============================================================
        private class SingletonItem : Singleton<SingletonItem>
        {
            public string Name;
            public SingletonItem(string name) { Name = name; }
        }

    #endregion

    #region 픽스처

        // ------------------------------------------------------------
        /// <summary>
        /// 각 테스트 전 정적 레지스트리를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        [SetUp]
        public void SetUp()
        {
            Singleton<SingletonItem>.Clear();
        }

    #endregion

    #region E-1: 정적 API 위임 스모크

        [Test]
        public void TEST_Singleton_정적_위임_스모크()
        {
            var main = new SingletonItem("Main");
            var sub  = new SingletonItem("Sub");
            var contender = new SingletonItem("Contender");

            Assert.IsFalse(Singleton<SingletonItem>.TryCurrent(out _), "등록 전에는 false이어야 합니다");

            Assert.IsTrue(Singleton<SingletonItem>.TryRegister(main));
            Assert.IsFalse(Singleton<SingletonItem>.TryRegister(contender));
            Singleton<SingletonItem>.Register("SUB", sub);

            Assert.AreSame(main, Singleton<SingletonItem>.Current);

            Assert.IsTrue(Singleton<SingletonItem>.TryCurrent(out var current));
            Assert.AreSame(main, current);

            Assert.AreSame(sub, Singleton<SingletonItem>.Named["SUB"]);
            Assert.IsTrue(Singleton<SingletonItem>.Named.Has("SUB"));

            using (Singleton<SingletonItem>.Scope("SUB"))
            {
                Assert.AreSame(sub, Singleton<SingletonItem>.Current);
            }

            Assert.AreSame(main, Singleton<SingletonItem>.Current);
        }

    #endregion

    #region E-2: Clear 위임

        [Test]
        public void TEST_Singleton_Clear_모든_슬롯_제거()
        {
            Singleton<SingletonItem>.Register(new SingletonItem("Main"));
            Singleton<SingletonItem>.Register("SLOT", new SingletonItem("Slot"));

            Singleton<SingletonItem>.Clear();

            Assert.IsFalse(Singleton<SingletonItem>.TryCurrent(out _));
            Assert.IsFalse(Singleton<SingletonItem>.Named.Has("SLOT"));
        }

    #endregion

    }

}
