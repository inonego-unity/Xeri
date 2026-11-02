/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AssemblyInfo.cs
수정일 : 2026-10-29

# 설명
Xeri Editor의 내부 직렬화 추적 계약을 EditMode 테스트에 노출한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("inonego.Xeri.Editor.TEST.EDIT")]
