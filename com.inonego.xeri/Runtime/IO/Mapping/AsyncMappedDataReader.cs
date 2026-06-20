/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AsyncMappedDataReader.cs
수정일 : 2026-06-21

# 설명
기존 async reader가 읽은 response value를 다른 값으로 변환해 반환하는 비동기 IO reader adapter를 정의한다.
Addressables처럼 비동기 입력원이 필요한 흐름에서 source reader와 값 변환을 분리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace inonego.Xeri.IO
{
   // =========================================================================================
   /// <summary>
   /// async source reader가 읽은 response value를 지정한 변환 함수로 변환해 반환하는 reader adapter.
   /// </summary>
   /// <typeparam name="TLocation">읽기 위치 타입.</typeparam>
   /// <typeparam name="TSource">source reader가 읽는 원본 값 타입.</typeparam>
   /// <typeparam name="TValue">adapter가 반환하는 변환 값 타입.</typeparam>
   // =========================================================================================
   [Serializable]
   public sealed class AsyncMappedDataReader<TLocation, TSource, TValue> : IAsyncDataReader<TLocation, TValue>
   {

   #region 필드

      private readonly IAsyncDataReader<TLocation, TSource> sourceReader = null;
      private readonly Func<TSource, TValue> map = null;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// AsyncMappedDataReader를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public AsyncMappedDataReader
      (
         IAsyncDataReader<TLocation, TSource> sourceReader,
         Func<TSource, TValue> map
      ) : base()
      {
         this.sourceReader = sourceReader ?? throw new ArgumentNullException(nameof(sourceReader));
         this.map = map ?? throw new ArgumentNullException(nameof(map));
      }

   #endregion

   #region 메서드

      // --------------------------------------------------------------------------
      /// <summary>
      /// async source reader에서 read response를 받은 뒤 value를 변환해 반환한다.
      /// </summary>
      // --------------------------------------------------------------------------
      public async Task<ReadResponse<TValue>> ReadAsync
      (
         TLocation location,
         CancellationToken cancellationToken = default
      )
      {
         var sourceResponse = await sourceReader.ReadAsync(location, cancellationToken);
         cancellationToken.ThrowIfCancellationRequested();

         if (!sourceResponse.Success)
         {
            return ReadResponse<TValue>.Fail
            (
               sourceResponse.Error,
               sourceResponse.Exception,
               sourceResponse.ReleaseHandle
            );
         }

         try
         {
            var value = map(sourceResponse.Value);

            return ReadResponse<TValue>.Succeed(value, sourceResponse.ReleaseHandle);
         }
         catch (Exception exception)
         {
            return ReadResponse<TValue>.Fail
            (
               exception.Message,
               exception,
               sourceResponse.ReleaseHandle
            );
         }
      }

   #endregion

   }
}
