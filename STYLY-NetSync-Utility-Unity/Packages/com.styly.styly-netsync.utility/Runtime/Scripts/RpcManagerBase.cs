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

        
        public void Start()
        {
            NetSyncManager.Instance.OnRPCReceived.AddListener(OnRpcReceived);
        }

        public void Send(TRpc rpc, string[] arg = null)
        {
            NetSyncManager.Instance.Rpc(EnumExtensions.ToStringValue(rpc), arg);
        }
        
        private void OnRpcReceived(int clientNo, string functionName, string[] parameter)
        {
            // システム定義のRPCはコンテンツ側で定義されていないためスキップ
            if (!functionName.TryParse<TRpc>(out var rpc))
            {
                return;
            }

            if (rpcListeners.ContainsKey(rpc))
            {
                rpcListeners[rpc]?.ForEach(x => x.Invoke(clientNo, parameter));
            }

            // R3用のストリーム発行
            if (rpcSubjects.ContainsKey(rpc))
            {
                rpcSubjects[rpc].OnNext(new RpcData(clientNo, parameter));
            }
        }

        public Observable<RpcData> AsObservable(TRpc rpc)
        {
            if (!rpcSubjects.ContainsKey(rpc))
            {
                rpcSubjects[rpc] = new Subject<RpcData>();
            }
            return rpcSubjects[rpc];
        }
        
        public void AddListener(TRpc rpc, UnityAction<int, string[]> action)
        {
            if (!rpcListeners.ContainsKey(rpc))
            {
                rpcListeners[rpc] = new List<UnityAction<int, string[]>>();
            }
            rpcListeners[rpc].Add(action);
        }
    }
}