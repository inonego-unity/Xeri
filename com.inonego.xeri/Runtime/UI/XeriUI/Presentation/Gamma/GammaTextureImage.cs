/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GammaTextureImage.cs
수정일 : 2026-09-01

# 설명
Gamma Premultiplied Texture를 Xeri Linear PMA Overlay Surface에 직접 합성하는 UGUI RawImage다.
Screen UITK Gamma Composite와 동일한 Xeri Gamma→Linear 색공간 계약을 Texture 입력에도 제공한다.

# 특이사항, 제약사항
입력 Texture는 Gamma numeric RGB와 Premultiplied Alpha를 보존해야 하며 Material 수명은 이 Component가 소유한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UI;

namespace inonego.Xeri.UI
{
    // ================================================================================
    /// <summary>
    /// Gamma PMA Texture를 Linear PMA로 변환하면서 Xeri UGUI Layer에 표시하는 RawImage.
    /// </summary>
    // ================================================================================
    [DisallowMultipleComponent]
    public sealed class GammaTextureImage : RawImage
    {

    #region 상수

        private const string SHADER_NAME = "Hidden/XeriUI/GammaTextureComposite";

    #endregion

    #region 필드

        private Material gammaCompositeMaterial = null;

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Canvas 등록 뒤 Gamma Texture 합성 Material을 보장한다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureMaterial();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Component가 생성한 runtime Material을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void OnDestroy()
        {
            ReleaseMaterial();
            base.OnDestroy();
        }

    #endregion

    #region Gamma 합성 Material

        // ------------------------------------------------------------
        /// <summary>
        /// Xeri Gamma Texture Composite Shader의 runtime Material을 생성하고 Graphic에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureMaterial()
        {
            if (gammaCompositeMaterial != null)
            {
                if (material != gammaCompositeMaterial)
                {
                    material = gammaCompositeMaterial;
                }

                return;
            }

            var shader = Shader.Find(SHADER_NAME);

            if (shader == null)
            {
                throw new MissingReferenceException
                (
                    $"Gamma Texture Composite Shader를 찾을 수 없습니다: {SHADER_NAME}"
                );
            }

            gammaCompositeMaterial = new Material(shader)
            {
                name = $"{name} Gamma Texture Composite Material",
                hideFlags = HideFlags.HideAndDontSave,
            };
            material = gammaCompositeMaterial;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 생성한 Gamma Texture Composite Material을 파괴하고 Graphic 참조를 비운다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseMaterial()
        {
            if (gammaCompositeMaterial == null) return;

            material = null;

            if (Application.isPlaying)
            {
                Destroy(gammaCompositeMaterial);
            }
            else
            {
                DestroyImmediate(gammaCompositeMaterial);
            }

            gammaCompositeMaterial = null;
        }

    #endregion

    }
}
