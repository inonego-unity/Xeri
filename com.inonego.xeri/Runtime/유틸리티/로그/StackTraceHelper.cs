/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : StackTraceHelper.cs
수정일 : 2026-05-07

# 설명
스택 트레이스 문자열을 Unity Console에서 클릭 가능한 <a> 하이퍼링크로 변환한다.
"in ...Assets/...cs:라인" 패턴을 찾아 Unity Console 링크 형식으로 치환한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace inonego.Xeri.Utility
{
    // ============================================================
    /// <summary>
    /// 스택 트레이스 관련 문자열 처리 헬퍼.
    /// </summary>
    // ============================================================
    public static class StackTraceHelper
    {
        // "in ...Assets/...cs:라인" 패턴 — Unity 에셋 경로 + 줄 번호를 찾는다.
        private static readonly Regex regex = new Regex(@"in (.*Assets.*\.cs):(\d+)", RegexOptions.Compiled);

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 스택 트레이스를 Unity Console에서 클릭 가능한 &lt;a&gt; 링크로 변환한다.
        /// <br/> "in ...Assets/...cs:라인" 패턴을 찾아 Unity 링크 형식으로 치환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static string ReplaceWithHyperlink(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return stackTrace;

            // Windows 환경 \r\n 대응을 위해 \n으로 분리 후 \r 제거
            var lines = stackTrace.Split('\n');
            var builder = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                var match = regex.Match(line);

                if (match.Success)
                {
                    string fullPath = match.Groups[1].Value.Replace("\\", "/");
                    int idx = fullPath.IndexOf("Assets");
                    if (idx >= 0) fullPath = fullPath.Substring(idx);

                    string lineNumber = match.Groups[2].Value;

                    string hyperlink = $"<a href=\"{fullPath}\" line=\"{lineNumber}\">{fullPath}:{lineNumber}</a>";
                    builder.Append(line.Replace(match.Value, $"(at {hyperlink})"));
                }
                else
                {
                    builder.Append(line);
                }

                if (i < lines.Length - 1) builder.Append('\n');
            }

            return builder.ToString();
        }
    }
}
