/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationGroup.cs
수정일 : 2026-09-17
# 설명
서로 다른 UI hierarchy와 backend에 있는 Presentation을 비소유 논리 그룹으로 묶는다.
Group operation은 획득 시점의 현재 Member에 동일한 Alpha·Visibility contribution을 적용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.UI
{
    // ======================================================================
    /// <summary>
    /// 여러 Presentation에 같은 표현 상태 operation을 적용하는 비소유 논리 그룹.
    /// </summary>
    // ======================================================================
    [Serializable]
    public sealed class PresentationGroup
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Group에 연결된 Presentation 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<IPresentation> Members => readOnlyMembers;

        private readonly ReadOnlyCollection<IPresentation> readOnlyMembers = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Group Member 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => members.Count;

        private readonly List<IPresentation> members = new List<IPresentation>();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 Presentation Group을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationGroup() : base()
        {
            readOnlyMembers = members.AsReadOnly();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 초기 Member를 가진 Presentation Group을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationGroup(IEnumerable<IPresentation> presentations) : this()
        {
            if (presentations == null)
            {
                throw new ArgumentNullException(nameof(presentations));
            }

            foreach (var presentation in presentations)
            {
                Add(presentation);
            }
        }

    #endregion

    #region Member 구성

        // ------------------------------------------------------------
        /// <summary>
        /// Presentation을 Group에 한 번 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Add(IPresentation presentation)
        {
            if (presentation == null)
            {
                throw new ArgumentNullException(nameof(presentation));
            }

            if (members.Contains(presentation)) return false;

            members.Add(presentation);
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Presentation을 Group에서 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Remove(IPresentation presentation)
        {
            if (presentation == null) return false;

            return members.Remove(presentation);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Group Member 연결을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            members.Clear();
        }

    #endregion

    #region Alpha operation

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 Alpha-capable Member에 같은 Numeric Modifier를 연결한다.
        /// <br/> 반환된 Modifier 자체를 Transition Target으로 사용할 수 있다.
        /// </summary>
        // ----------------------------------------------------------------------
        public PresentationAlphaModifier AcquireAlphaModifier
        (
            string key,
            NumericFOperation operation,
            float initialValue,
            int order = 0
        )
        {
            var modifier = new PresentationAlphaModifier
            (
                key,
                operation,
                initialValue,
                order
            );

            try
            {
                for (var index = 0; index < members.Count; index++)
                {
                    var alpha = members[index].Alpha;
                    if (alpha == null) continue;

                    modifier.Add(alpha);
                }

                if (!modifier.HasTargets)
                {
                    throw new InvalidOperationException
                    (
                        "Presentation Group에 Alpha를 지원하는 Member가 없습니다."
                    );
                }

                return modifier;
            }
            catch
            {
                modifier.Dispose();
                throw;
            }
        }

    #endregion

    #region Visibility operation

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 현재 Visibility-capable Member에 같은 scoped Visibility Modifier를 적용한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public Lease AcquireVisibilityModifier(bool visible)
        {
            var leases = new List<Lease>();

            try
            {
                for (var index = 0; index < members.Count; index++)
                {
                    var visibility = members[index].Visibility;
                    if (visibility == null) continue;

                    leases.Add(visibility.AcquireModifier(visible));
                }

                if (leases.Count == 0)
                {
                    throw new InvalidOperationException
                    (
                        "Presentation Group에 Visibility를 지원하는 Member가 없습니다."
                    );
                }
            }
            catch (Exception exception)
            {
                var errors = ReleaseLeases(leases);

                if (errors.Count == 0)
                {
                    throw;
                }

                errors.Insert(0, exception);
                throw new AggregateException
                (
                    "Presentation Group Visibility 획득과 롤백이 실패했습니다.",
                    errors
                );
            }

            return new Lease(() => ReleaseVisibilityLeases(leases));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Visibility Override Lease를 역순 반환하고 정리 실패를 집계한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ReleaseVisibilityLeases(List<Lease> leases)
        {
            var errors = ReleaseLeases(leases);

            if (errors.Count > 0)
            {
                throw new AggregateException
                (
                    "Presentation Group Visibility 반환이 실패했습니다.",
                    errors
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Lease 목록을 역순으로 한 번 반환하고 발생한 오류를 수집한다.
        /// </summary>
        // ------------------------------------------------------------
        private static List<Exception> ReleaseLeases(List<Lease> leases)
        {
            var errors = new List<Exception>();

            for (var index = leases.Count - 1; index >= 0; index--)
            {
                try
                {
                    leases[index]?.Dispose();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            leases.Clear();
            return errors;
        }

    #endregion

    }
}
