using System;
using System.Collections.Generic;
using Styly.NetSync;
using UnityEngine;
using UnityEngine.Events;
using R3;

namespace Styly.NetSync.Utility
{
    public class RpcManagerBase<TRpc> : MonoBehaviour where TRpc : Enum
    {
        public readonly struct RpcData
        {
            public readonly int ClientNo;
            public readonly string[] Parameters;

            public RpcData(int clientNo, string[] parameters)
            {
                ClientNo = clientNo;
                Parameters = parameters;
            }
        }

        Dictionary<TRpc, List<UnityAction<int, string[]>>> rpcListeners = new ();
        Dictionary<TRpc, Subject<RpcData>> rpcSubjects = new ();


        void Start()
        {
            NetSyncManager.Instance.OnRPCReceived.AddListener(OnRpcReceived);
        }

        void OnDestroy()
        {
            if (NetSyncManager.Instance != null)
            {
                NetSyncManager.Instance.OnRPCReceived.RemoveListener(OnRpcReceived);
            }

            foreach (var subject in rpcSubjects.Values)
            {
                subject.Dispose();
            }
            rpcSubjects.Clear();
            rpcListeners.Clear();
        }

        public void Send(TRpc rpc, string[] arg = null)
        {
            NetSyncManager.Instance.Rpc(rpc.ToString(), arg);
        }

        public void Send(TRpc rpc, string[] arg, int targetClientNo)
        {
            NetSyncManager.Instance.Rpc(rpc.ToString(), arg, targetClientNo);
        }

        public void Send(TRpc rpc, string[] arg, int[] targetClientNos)
        {
            NetSyncManager.Instance.Rpc(rpc.ToString(), arg, targetClientNos);
        }
        
        private void OnRpcReceived(int clientNo, string functionName, string[] parameter)
        {
            // システム定義のRPCはコンテンツ側で定義されていないためスキップ
            if (!EnumExtensions.TryParse<TRpc>(functionName, out var rpc))
            {
                return;
            }

            if (rpcListeners.TryGetValue(rpc, out var listeners))
            {
                listeners?.ForEach(x => x.Invoke(clientNo, parameter));
            }

            // R3用のストリーム発行
            if (rpcSubjects.TryGetValue(rpc, out var subject))
            {
                subject.OnNext(new RpcData(clientNo, parameter));
            }
        }

        public Observable<RpcData> AsObservable(TRpc rpc)
        {
            if (!rpcSubjects.TryGetValue(rpc, out var subject))
            {
                subject = new Subject<RpcData>();
                rpcSubjects[rpc] = subject;
            }
            return subject;
        }

        public void AddListener(TRpc rpc, UnityAction<int, string[]> action)
        {
            if (!rpcListeners.TryGetValue(rpc, out var list))
            {
                list = new List<UnityAction<int, string[]>>();
                rpcListeners[rpc] = list;
            }
            list.Add(action);
        }

        public void RemoveListener(TRpc rpc, UnityAction<int, string[]> action)
        {
            if (rpcListeners.TryGetValue(rpc, out var list))
            {
                list.Remove(action);
            }
        }
    }
}