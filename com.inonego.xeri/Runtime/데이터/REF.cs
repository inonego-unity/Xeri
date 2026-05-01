/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : REF.cs
수정일 : 2026-05-01

# 설명
DataPackage에서 값을 참조하는 구조체.
string 키를 저장하고 ToValue() 호출 시 현재 슬롯의 DataPackage에서 값을 조회한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

using UnityEngine;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// <br/> DataPackage에서 값을 참조하는 구조체.
    /// <br/> ToValue()로 현재 슬롯의 DataPackage에서 값을 조회한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct REF<T> : IKeyable<string>
    where T : class, ITableValue
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 참조할 데이터의 키.
        /// </summary>
        // ------------------------------------------------------------
        [XmlAttribute("Key")]
        public string Key
        {
            get => key;
            set => key = value;
        }

        [SerializeField]
        private string key;

        // ------------------------------------------------------------
        /// <summary>
        /// 키가 설정되어 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        [XmlIgnore]
        public bool HasKey => !string.IsNullOrEmpty(key);

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 키로 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public REF(string key)
        {
            this.key = key;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 슬롯의 DataPackage에서 값을 조회한다.
        /// <br/> 키가 없거나 DataPackage가 미등록이면 null을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public T ToValue()
        {
            if (!HasKey) return null;
            if (!DataPackage.TryCurrent(out var package)) return null;
            return package.TryRead<T>(key);
        }

        public static implicit operator REF<T>(string key) => new(key);
        public static implicit operator string(REF<T> reference) => reference.key;

    #endregion

    }
}
