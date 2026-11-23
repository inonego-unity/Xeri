/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridViewStatePrefs.cs
수정일 : 2026-11-21

# 설명
호스트가 Editor인 TableGrid의 보기 상태를 EditorPrefs에 저장·복원하는 도우미.
런타임 소비처와 같은 직렬화 경로(TableGrid.CaptureViewState → TableGridViewState.ToJson)를 쓰고 저장 위치만 EditorPrefs다.

# 특이사항, 제약사항
EditorPrefs는 기기 전체에서 공유되므로 키에 프로젝트 구분(PlayerSettings.productGUID)을 붙인다.
tableKey는 호스트가 준다(창 형식 + 표 ID, Collection은 대상 형식 + 속성 경로 등). 에디터를 다시 열면 바뀌는 값은 쓰지 않는다.
Bind는 SetColumns 뒤에 부른다. 저장된 상태가 없거나 읽지 못하면 지금 상태를 그대로 둔다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.UI.TableGrid.Editor
{
    // ============================================================
    /// <summary>
    /// TableGrid 보기 상태를 EditorPrefs에 저장·복원한다.
    /// </summary>
    // ============================================================
    public static class TableGridViewStatePrefs
    {

    #region 상수

        private const string KEY_PREFIX = "Xeri.TableGrid.";

    #endregion

    #region 저장과 복원

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 저장된 보기 상태가 있으면 표에 적용하고,
        /// <br/> 이후 ViewStateChanged마다 저장한다.
        /// <br/> includeQuery이면 정렬·필터·그룹(원래 값)도 함께 저장한다.
        /// <br/> 세션(스크롤·선택)은 저장하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Bind(TableGrid table, string tableKey, bool includeQuery = true)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            var key = GetKey(tableKey);
            if (EditorPrefs.HasKey(key) && TableGridViewState.TryFromJson(EditorPrefs.GetString(key), out var state))
            {
                table.ApplyViewState(state);
            }

            table.ViewStateChanged += (_, _) => EditorPrefs.SetString(key, table.CaptureViewState(includeQuery).ToJson());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 저장된 보기 상태를 지운다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Clear(string tableKey)
        {
            EditorPrefs.DeleteKey(GetKey(tableKey));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 프로젝트 구분을 붙인 EditorPrefs 키를 돌려준다.
        /// </summary>
        // ------------------------------------------------------------
        public static string GetKey(string tableKey)
        {
            if (string.IsNullOrWhiteSpace(tableKey))
            {
                throw new ArgumentException("표 키가 필요합니다.", nameof(tableKey));
            }

            return KEY_PREFIX + PlayerSettings.productGUID + "." + tableKey;
        }

    #endregion

    }
}
