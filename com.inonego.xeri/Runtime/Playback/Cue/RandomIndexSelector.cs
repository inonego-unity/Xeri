/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : RandomIndexSelector.cs
수정일 : 2026-09-05

# 설명
지정 개수의 인덱스를 무작위로 선택하고 선택적으로 직전 인덱스 반복을 제외한다.
선택 이력은 Selector 인스턴스의 runtime 상태로만 유지한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 직전 선택 이력을 소유하는 무작위 인덱스 선택기.
    /// </summary>
    // ============================================================
    internal sealed class RandomIndexSelector
    {

    #region 필드

        private readonly bool excludePrevious = false;
        private int previousIndex = -1;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 직전 선택 제외 정책으로 Selector를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal RandomIndexSelector(bool excludePrevious)
        {
            this.excludePrevious = excludePrevious;
        }

    #endregion

    #region 선택

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 개수에서 다음 인덱스를 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        internal int Select(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException
                (
                    nameof(count),
                    "선택할 항목 수는 1 이상이어야 합니다."
                );
            }

            // 단일 항목은 선택 여지가 없으므로 이력만 현재 인덱스로 맞춘다.
            if (count == 1)
            {
                previousIndex = 0;
                return 0;
            }

            int index;

            // 직전 선택을 제외할 때는 하나 줄인 범위에서 뽑아 이전 인덱스를 건너뛴다.
            if
            (
                excludePrevious &&
                previousIndex >= 0 &&
                previousIndex < count
            )
            {
                index = UnityEngine.Random.Range(0, count - 1);

                if (index >= previousIndex)
                {
                    index++;
                }
            }
            else
            {
                index = UnityEngine.Random.Range(0, count);
            }

            // 다음 선택에서 직전 Variant를 판단할 수 있도록 이번 결과를 보관한다.
            previousIndex = index;
            return index;
        }

    #endregion

    }
}
