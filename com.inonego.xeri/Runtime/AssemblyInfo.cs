/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AssemblyInfo.cs
수정일 : 2026-08-23

# 설명
Xeri Runtime 내부 계약을 Editor, 선택형 Render Pipeline Adapter와 검증 Assembly에 제한 공개한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("inonego.Xeri.Editor")]
[assembly: InternalsVisibleTo("inonego.Xeri.UI.HDRP")]
[assembly: InternalsVisibleTo("inonego.Xeri.TEST.EDIT")]
[assembly: InternalsVisibleTo("inonego.Xeri.TEST.PLAY")]