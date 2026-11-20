/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AssemblyInfo.cs
수정일 : 2026-11-20

# 설명
Xeri Editor 어셈블리(기능 폴더의 Editor.asmref 포함)의 internal 타입을 EditMode 테스트에 노출한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("inonego.Xeri.Editor.TEST.EDIT")]
