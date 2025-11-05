using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Styly.NetSync;
using UnityEngine;
using UnityEngine.Events;
using R3;

namespace Styly.NetSync.Utility
{
    [DefaultExecutionOrder(-100)]
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// NetSyncManagerがReady状態になるまで待機します。
        /// 既にReady状態の場合は即座に完了します。
        /// </summary>
        public async UniTask WaitForReadyAsync(CancellationToken cancellationToken = default)
        {
            // 既にReady状態なら即座に完了
            if (NetSyncManager.Instance.IsReady)
            {
                return;
            }

            var tcs = new UniTaskCompletionSource();

            void OnReady()
            {
                tcs.TrySetResult();
            }

            NetSyncManager.Instance.OnReady.AddListener(OnReady);

            try
            {
                await tcs.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                NetSyncManager.Instance.OnReady.RemoveListener(OnReady);
            }
        }

        /// <summary>
        /// NetSyncManagerがReady状態になったときに発火するObservableを返します。
        /// Subscribe時に既にReady状態の場合は即座に発火します。
        /// </summary>
        public Observable<Unit> OnReadyAsObservable()
        {
            return Observable.Create<Unit>(observer =>
            {
                // 既にReady状態なら即座に発火して完了
                if (NetSyncManager.Instance.IsReady)
                {
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                    return Disposable.Empty;
                }

                // まだReadyでない場合はOnReadyを待つ
                void OnReady()
                {
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                }

                NetSyncManager.Instance.OnReady.AddListener(OnReady);

                return Disposable.Create(() =>
                {
                    NetSyncManager.Instance.OnReady.RemoveListener(OnReady);
                });
            });
        }
    }
}